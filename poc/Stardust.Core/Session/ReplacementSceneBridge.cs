using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Borrowed session/world bridge; owns only the minimum-resource task and cancellation.</summary>
public sealed class ReplacementSceneBridge : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly ReplacementClientSession session;
    private readonly SceneWorldCoordinator world;
    private readonly Func<CmdStartSceneMessage, CancellationToken, Task> minimumResources;
    private CancellationTokenSource? cancellation;
    private Task? job;
    private Task drain = Task.CompletedTask;
    private bool bound, busy, disposed, failed;
    public ReplacementSceneBridge(ReplacementClientSession session, SceneWorldCoordinator world,
        Func<CmdStartSceneMessage, CancellationToken, Task> minimumResources)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.minimumResources = minimumResources ?? throw new ArgumentNullException(nameof(minimumResources));
    }
    public Task DrainCompletion => drain;
    public long Generation { get; private set; }
    public int ReadySubmissions { get; private set; }
    public void BindSelection()
    {
        Enter();
        try
        {
            Open();
            if (bound || session.SelectedObjectId == 0 || session.Phase is not
                (ReplacementClientPhase.ZoneHandshake or ReplacementClientPhase.ZonePermissions or ReplacementClientPhase.Selected))
                throw new InvalidOperationException("Admitted fresh zone selection required.");
            Generation = world.BeginSelection(session.SelectedObjectId).Generation; bound = true;
        }
        catch { Fail(); throw; }
        finally { busy = false; }
    }
    public ReplacementClientExchange ObserveExchange(ReplacementClientExchange exchange)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        Enter(); var unhandled = new List<byte[]>();
        try
        {
            Open(); Current();
            if (exchange.FailureType is not null || session.Phase is ReplacementClientPhase.Faulted or ReplacementClientPhase.Closed)
                throw new InvalidDataException("Borrowed session failed.");
            if (exchange.ZoneApplications.Count > 1024 || exchange.ZoneApplications.Sum(message => (long)message.Length) > 8L * 1024 * 1024)
                throw new InvalidDataException("Bridge application batch budget exceeded.");
            foreach (byte[] message in exchange.ZoneApplications)
            {
                if (session.Phase != ReplacementClientPhase.Selected || message.Length < 6 || message.Length > SoeClientTransport.MaximumMessageSize)
                    throw new InvalidDataException("Selected complete application required.");
                uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(message.AsSpan(2));
                if (!WorldMessage(opcode, message)) { unhandled.Add((byte[])message.Clone()); continue; }
                if (!world.Accept(Generation, message)) throw new InvalidDataException("World application rejected.");
                if (opcode == ZoneSceneProtocolCodec.CmdStartSceneOpcode)
                {
                    if (job is not null) throw new InvalidDataException("One scene initialization job required.");
                    cancellation = new();
                    job = minimumResources(world.Scene!, cancellation.Token)
                        ?? throw new InvalidOperationException("Minimum resources must return a task.");
                    drain = Observe(job);
                }
            }
            return new(exchange.Datagrams, unhandled.AsReadOnly(), exchange.RejectedFraming);
        }
        catch (Exception error)
        {
            foreach (byte[] message in unhandled) CryptographicOperations.ZeroMemory(message);
            unhandled.Clear(); Fail();
            return new(exchange.Datagrams, unhandled.AsReadOnly(), exchange.RejectedFraming, exchange.FailureType ?? error.GetType().Name);
        }
        finally
        {
            foreach (byte[] message in exchange.ZoneApplications) CryptographicOperations.ZeroMemory(message);
            busy = false;
        }
    }
    public ReplacementClientExchange Poll(long nowMilliseconds)
    {
        Enter();
        ReplacementClientExchange? polled = null;
        try
        {
            Open(); Current();
            var exchange = session.Poll(nowMilliseconds); polled = exchange;
            if (job is null || !job.IsCompleted || ReadySubmissions != 0) return exchange;
            // Observation and all mutable session/world operations remain on this caller thread.
            job.GetAwaiter().GetResult();
            if (session.Phase != ReplacementClientPhase.Selected) throw new InvalidOperationException("Selected session required at scene barrier.");
            if (session.ZonePendingMessages != 0) return exchange;
            byte[] ready = world.CompleteMinimumSceneBarrier(Generation, true)
                ?? throw new InvalidDataException("Current minimum scene barrier rejected.");
            try
            {
                var queued = session.QueueZoneApplication(ready, nowMilliseconds); ReadySubmissions++;
                return new(exchange.Datagrams.Concat(queued.Datagrams).ToArray(), exchange.ZoneApplications, exchange.RejectedFraming, exchange.FailureType);
            }
            finally { CryptographicOperations.ZeroMemory(ready); }
        }
        catch
        {
            if (polled is not null)
            {
                foreach (var datagram in polled.Datagrams) CryptographicOperations.ZeroMemory(datagram.Bytes);
                foreach (byte[] application in polled.ZoneApplications) CryptographicOperations.ZeroMemory(application);
            }
            Fail(); throw;
        }
        finally { busy = false; }
    }
    public void Dispose()
    {
        Enter();
        try { if (disposed) return; disposed = true; RetireJob(); }
        finally { busy = false; }
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread || busy)
            throw new InvalidOperationException("Bridge owner thread without reentry required.");
        busy = true;
    }
    private void Open() { if (disposed || failed) throw new InvalidOperationException("Bridge is retired."); }
    private void Current() { if (!bound || Generation != world.Generation || session.SelectedObjectId == 0) throw new InvalidOperationException("Current bound selection required."); }
    private void Fail()
    {
        if (failed || disposed) return;
        failed = true;
        try { RetireJob(); }
        finally { try { session.Dispose(); } finally { world.Close(); } }
    }
    private void RetireJob()
    {
        if (cancellation is null) return;
        var source = cancellation; cancellation = null;
        try { if (job is null || !job.IsCompleted) source.Cancel(); }
        finally { drain = Retire(job ?? Task.CompletedTask, source); }
    }
    private static async Task Observe(Task task) { try { await task.ConfigureAwait(false); } catch { /* Owner Poll observes the task outcome. */ } }
    private static async Task Retire(Task task, CancellationTokenSource source)
    { try { await Observe(task).ConfigureAwait(false); } finally { source.Dispose(); } }
    private static bool WorldMessage(uint opcode, ReadOnlySpan<byte> message)
    {
        if (opcode is ReplicationEnvelopeCodec.BaselineOpcode or ReplicationEnvelopeCodec.DeltaOpcode)
        {
            if (message.Length < 18) throw new InvalidDataException("Replication family header truncated.");
            uint tag = BinaryPrimitives.ReadUInt32LittleEndian(message[14..]);
            return tag is 0x4352454F or 0x54414E4F; // Required CREO/TANO families; others explicitly returned to caller.
        }
        return opcode is ZoneSceneProtocolCodec.CmdStartSceneOpcode or SceneReadyProtocolCodec.Opcode
            or SceneObjectLifecycleCodec.CreateOpcode or SceneObjectLifecycleCodec.DestroyOpcode
            or SceneObjectLifecycleCodec.ContainmentOpcode or SceneObjectLifecycleCodec.EndBaselinesOpcode;
    }
}
