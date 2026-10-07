using System.Security.Cryptography;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.Hosting;

/// <summary>Owner-thread, nonblocking driver over caller-injected ports; no endpoint resolution.</summary>
public sealed class ReplacementDatagramPump : IDisposable
{
    private const int MaximumBuffers = 256, MaximumBytes = 1048576;
    private readonly int owner = Environment.CurrentManagedThreadId;
    private readonly ReplacementClientSession session;
    private readonly List<Task> retirements = [];
    private readonly List<ReplacementClientExchange> notices = [];
    private Lane? active;
    private bool busy, disposed, zoneWaiting;
    private long clock = -1;
    private Task drain = Task.CompletedTask;
    public string? FailureType { get; private set; }
    public Task DrainCompletion => drain;

    private sealed class Lane(IDatagramPort port, DatagramPeer peer, ReplacementConnection connection)
    {
        public readonly IDatagramPort Port = port;
        public readonly DatagramPeer Peer = peer;
        public readonly ReplacementConnection Connection = connection;
        public readonly CancellationTokenSource Cancellation = new();
        public readonly Queue<byte[]> Queue = new();
        public byte[]? Sending;
        public Task? Send;
        public Task<InboundDatagram>? Receive;
        public int Bytes;
    }

    public ReplacementDatagramPump(ReplacementClientSession session, IDatagramPort loginPort, DatagramPeer loginPeer)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        ArgumentNullException.ThrowIfNull(loginPort);
        if (loginPeer.Port == 0) throw new ArgumentOutOfRangeException(nameof(loginPeer));
        active = new(loginPort, loginPeer, ReplacementConnection.Login);
    }

    public void StartLogin(uint id, long now) => Operation(now, () => Capture(session.StartLogin(id, now)));
    public void Authenticate(string user, string password, long now) => Operation(now, () => Capture(session.Authenticate(user, password, now)));

    public void BeginZone(ulong oid, uint id, IDatagramPort zonePort, DatagramPeer zonePeer, long now)
    {
        Enter(now);
        ReplacementClientExchange? exchange = null;
        try
        {
            EnsureActive(); ArgumentNullException.ThrowIfNull(zonePort);
            if (active!.Connection != ReplacementConnection.Login || ReferenceEquals(zonePort, active.Port) || zonePeer.Port == 0)
                throw new ArgumentException("Separate explicit zone port required.");
            exchange = session.BeginZone(oid, id, now);
            if (session.ZoneEndpoint is not { } endpoint || zonePeer.Port != endpoint.Port)
                throw new ArgumentException("Explicit peer port must match admitted zone port.");
            // Port becomes owned only after successful session/peer admission.
            Lane previous = active;
            active = new(zonePort, zonePeer, ReplacementConnection.Zone); zoneWaiting = true;
            Retire(previous); Capture(exchange); exchange = null;
        }
        catch (Exception error) { ClearWire(exchange); Fail(error); throw; }
        finally { busy = false; }
    }

    public void Submit(ReplacementClientExchange exchange)
    {
        Enter(null);
        try { EnsureActive(); ArgumentNullException.ThrowIfNull(exchange); TakeWire(exchange); }
        catch (Exception error) { ClearWire(exchange); Fail(error); throw; }
        finally { busy = false; }
    }

    public IReadOnlyList<ReplacementClientExchange> Tick(long now)
    {
        Enter(now);
        var result = new List<ReplacementClientExchange>();
        try
        {
            EnsureActive();
            if (zoneWaiting)
            {
                if (!drain.IsCompleted) return Flush(result);
                drain.GetAwaiter().GetResult(); zoneWaiting = false;
            }
            Lane lane = active!;
            if (lane.Send is { IsCompleted: true })
            {
                lane.Send.GetAwaiter().GetResult();
                CryptographicOperations.ZeroMemory(lane.Sending!); lane.Bytes -= lane.Sending!.Length;
                lane.Sending = null; lane.Send = null;
            }
            if (lane.Receive is { IsCompleted: true })
            {
                InboundDatagram value = lane.Receive.GetAwaiter().GetResult(); lane.Receive = null;
                try
                {
                    if (value.Source == lane.Peer)
                    {
                        if (value.Bytes.Length > SoeClientTransport.MaximumWireDatagramSize)
                            throw new InvalidDataException("Received datagram exceeds wire bound.");
                        Capture(session.AcceptIncoming(lane.Connection, value.Bytes, now));
                    }
                }
                finally { CryptographicOperations.ZeroMemory(value.Bytes); }
            }
            Capture(session.Poll(now));
            if (lane.Send is null && lane.Queue.TryDequeue(out byte[]? bytes))
            {
                lane.Sending = bytes;
                lane.Send = lane.Port.SendAsync(lane.Peer, bytes, lane.Cancellation.Token).AsTask();
            }
            lane.Receive ??= lane.Port.ReceiveAsync(lane.Cancellation.Token).AsTask();
            return Flush(result);
        }
        catch (Exception error)
        {
            Fail(error); result.Add(new([], [], FailureType: FailureType)); return Flush(result);
        }
        finally { busy = false; }
    }

    public void Dispose()
    {
        Enter(null, true);
        try
        {
            if (disposed) return;
            disposed = true;
            if (active is { } lane) { active = null; Retire(lane); }
            foreach (var notice in notices) foreach (byte[] app in notice.ZoneApplications) CryptographicOperations.ZeroMemory(app);
            notices.Clear();
        }
        finally { busy = false; }
    }

    private void Operation(long now, Action action)
    {
        Enter(now);
        try { EnsureActive(); action(); }
        catch (Exception error) { Fail(error); throw; }
        finally { busy = false; }
    }
    private void Capture(ReplacementClientExchange exchange)
    {
        try
        {
            TakeWire(exchange);
            if (notices.Count >= MaximumBuffers) throw new InvalidOperationException("Exchange queue capacity exceeded.");
            notices.Add(new([], exchange.ZoneApplications, exchange.RejectedFraming, exchange.FailureType));
            if (exchange.FailureType is not null)
            { FailureType ??= exchange.FailureType; throw new InvalidDataException("Session reported failure."); }
        }
        catch
        {
            ClearWire(exchange);
            foreach (byte[] app in exchange.ZoneApplications) CryptographicOperations.ZeroMemory(app);
            throw;
        }
    }
    private void TakeWire(ReplacementClientExchange exchange)
    {
        Lane lane = active!;
        int count = lane.Queue.Count + (lane.Sending is null ? 0 : 1), bytes = lane.Bytes;
        var unique = new HashSet<byte[]>(ReferenceEqualityComparer.Instance);
        foreach (var wire in exchange.Datagrams)
        {
            if (wire is null || wire.Bytes is null || wire.Connection != lane.Connection ||
                wire.Bytes.Length is < 1 or > SoeClientTransport.MaximumWireDatagramSize || !unique.Add(wire.Bytes) ||
                ReferenceEquals(wire.Bytes, lane.Sending) || lane.Queue.Any(b => ReferenceEquals(b, wire.Bytes)))
                throw new InvalidDataException("Invalid owned wire batch.");
            count++; bytes = checked(bytes + wire.Bytes.Length);
        }
        if (count > MaximumBuffers || bytes > MaximumBytes) throw new InvalidOperationException("Wire queue capacity exceeded.");
        foreach (var wire in exchange.Datagrams) lane.Queue.Enqueue(wire.Bytes);
        lane.Bytes = bytes;
    }
    private IReadOnlyList<ReplacementClientExchange> Flush(List<ReplacementClientExchange> result)
    { result.AddRange(notices); notices.Clear(); return result.AsReadOnly(); }
    private void Enter(long? now, bool allowDisposed = false)
    {
        if (Environment.CurrentManagedThreadId != owner || busy) throw new InvalidOperationException("Pump owner thread without reentry required.");
        if (!allowDisposed) ObjectDisposedException.ThrowIf(disposed, this);
        if (now is long value) { if (value < 0 || value < clock) throw new ArgumentOutOfRangeException(nameof(now)); clock = value; }
        busy = true;
    }
    private void EnsureActive() { if (FailureType is not null || active is null) throw new InvalidOperationException("Active pump required."); }
    private void Fail(Exception error)
    {
        FailureType ??= error.GetType().Name;
        foreach (var notice in notices) foreach (byte[] app in notice.ZoneApplications) CryptographicOperations.ZeroMemory(app);
        notices.Clear();
        if (active is { } lane) { active = null; Retire(lane); }
        // Borrowed session lifecycle remains caller-owned, including transport/credential release.
    }
    private void ClearWire(ReplacementClientExchange? exchange)
    {
        if (exchange is not null) foreach (var wire in exchange.Datagrams)
            if (wire?.Bytes is { } bytes && !(ReferenceEquals(bytes, active?.Sending) && active?.Send is { IsCompleted: false }))
                CryptographicOperations.ZeroMemory(bytes);
    }
    private void Retire(Lane lane)
    {
        var errors = new List<Exception>();
        try { lane.Cancellation.Cancel(); } catch (Exception error) { errors.Add(SafeRetirementError(error)); }
        Task disposal;
        try { disposal = lane.Port.DisposeAsync().AsTask(); }
        catch (Exception error) { errors.Add(SafeRetirementError(error)); disposal = Task.CompletedTask; }
        while (lane.Queue.TryDequeue(out byte[]? bytes)) CryptographicOperations.ZeroMemory(bytes);
        retirements.Add(DrainLane(lane, disposal, errors)); drain = Task.WhenAll(retirements);
    }
    private static async Task DrainLane(Lane lane, Task disposal, List<Exception> errors)
    {
        // Detached cleanup owns this lane only: no continuation touches pump/session successor state.
        if (lane.Send is { } send)
        {
            try { await send.ConfigureAwait(false); } catch (OperationCanceledException) { } catch (Exception error) { errors.Add(SafeRetirementError(error)); }
            finally { if (lane.Sending is { } bytes) CryptographicOperations.ZeroMemory(bytes); }
        }
        else if (lane.Sending is { } unsent) CryptographicOperations.ZeroMemory(unsent);
        if (lane.Receive is { } receive)
        {
            try { var value = await receive.ConfigureAwait(false); CryptographicOperations.ZeroMemory(value.Bytes); }
            catch (OperationCanceledException) { } catch (Exception error) { errors.Add(SafeRetirementError(error)); }
        }
        try { await disposal.ConfigureAwait(false); } catch (OperationCanceledException) { } catch (Exception error) { errors.Add(SafeRetirementError(error)); }
        lane.Send = null; lane.Receive = null; lane.Sending = null;
        lane.Cancellation.Dispose();
        if (errors.Count != 0) throw new AggregateException("Injected port retirement failed.", errors);
    }
    private static Exception SafeRetirementError(Exception error) => new InvalidOperationException(error.GetType().Name);
}
