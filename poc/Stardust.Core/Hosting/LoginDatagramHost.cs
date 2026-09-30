using System.Security.Cryptography;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.Hosting;

/// <summary>Single-run login owner. One outstanding receive survives timer ticks; no engine dependencies.</summary>
public sealed class LoginDatagramHost : IAsyncDisposable
{
    private readonly LoginClientCoordinator coordinator;
    private readonly IDatagramPort port;
    private readonly DatagramPeer peer;
    private readonly MonotonicClockGuard clock;
    private readonly IHostTickSource ticks;
    private readonly CancellationTokenSource lifetime = new();
    private readonly DatagramAccounting accounting = new();
    private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object lifecycle = new();
    private readonly string clientVersion;
    private readonly Action<LoginDiagnosticEvent>? diagnostics;
    private int hostDiagnosticFailures;
    private int started;
    private int disposed;

    public LoginDatagramHost(LoginClientOptions options, DatagramPeer peer, IDatagramPort port,
        IMonotonicClock? clock = null, IHostTickSource? ticks = null,
        Action<LoginDiagnosticEvent>? diagnostics = null)
    {
        if (peer.Port == 0) throw new ArgumentOutOfRangeException(nameof(peer));
        coordinator = new(options, diagnostics);
        this.diagnostics = diagnostics;
        if (peer.Port != options.Port) throw new ArgumentException("Pinned peer port differs from configured port.", nameof(peer));
        clientVersion = options.ClientVersion;
        this.peer = peer;
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.clock = new(clock ?? new StopwatchMonotonicClock());
        this.ticks = ticks ?? new PeriodicHostTickSource();
    }

    public LoginHostOutcome Outcome { get; private set; }
    public LoginClientState State => coordinator.Session.State;
    public DatagramCounters Counters => accounting.Counters;
    public int DiagnosticFailures => (int)Math.Min(int.MaxValue, (long)coordinator.DiagnosticFailures + hostDiagnosticFailures);
    public int CleanupFailures { get; private set; }

    public async Task<LoginConnectionResult> RunAsync(uint connectionId, string username, string password,
        CancellationToken cancellationToken = default)
    {
        lock (lifecycle)
        {
            ObjectDisposedException.ThrowIf(disposed != 0, this);
            if (started != 0) throw new InvalidOperationException("Login host can run only once.");
            started = 1;
        }
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        CancellationToken token = linked.Token;
        Task<InboundDatagram>? receive = null;
        try
        {
            token.ThrowIfCancellationRequested();
            byte[] preflight;
            try { preflight = LoginProtocolCodec.EncodeLoginClientId(username, password, clientVersion); }
            catch (ArgumentException) { throw new LoginHostFailureException(LoginHostOutcome.InvalidInput); }
            CryptographicOperations.ZeroMemory(preflight);
            await SendAsync(coordinator.Start(connectionId, ReadClock()), token).ConfigureAwait(false);
            while (true)
            {
                token.ThrowIfCancellationRequested();
                long now = ReadClock();
                if (State == LoginClientState.ReadyToAuthenticate)
                    await SendAsync(coordinator.Authenticate(username, password, now), token).ConfigureAwait(false);
                if (State == LoginClientState.Failed)
                    throw new LoginHostFailureException(coordinator.Session.LastError is null
                        ? LoginHostOutcome.PeerDisconnected : LoginHostOutcome.RemoteRejected);
                if (State == LoginClientState.Complete)
                {
                    LoginConnectionResult result = new(coordinator.Session, accounting.Counters);
                    Outcome = LoginHostOutcome.Completed;
                    return result;
                }
                await SendBatchAsync(coordinator.Poll(now), token).ConfigureAwait(false);
                receive ??= port.ReceiveAsync(token).AsTask();
                using CancellationTokenSource tickCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                Task tick = ticks.WaitAsync(tickCancellation.Token).AsTask();
                Task winner = await Task.WhenAny(receive, tick).ConfigureAwait(false);
                if (winner == tick)
                {
                    await tick.ConfigureAwait(false);
                    continue;
                }
                tickCancellation.Cancel();
                try { await tick.ConfigureAwait(false); }
                catch (OperationCanceledException) when (tickCancellation.IsCancellationRequested) { }
                InboundDatagram inbound = await receive.ConfigureAwait(false);
                receive = null;
                try
                {
                    // Filter before framing/session/accounting; unrelated senders cannot advance state.
                    if (inbound.Source != peer)
                    {
                        accounting.RecordForeignReceive();
                        Emit(new(LoginDiagnosticKind.ForeignSource, State, LoginDiagnosticOutcome.Dropped));
                        continue;
                    }
                    if (inbound.Bytes.Length > SoeClientTransport.MaximumWireDatagramSize)
                        throw new LoginHostFailureException(LoginHostOutcome.Malformed);
                    now = ReadClock();
                    accounting.RecordPeerReceive(inbound.Bytes.Length);
                    await SendBatchAsync(coordinator.Poll(now), token).ConfigureAwait(false);
                    try { await SendBatchAsync(coordinator.AcceptIncoming(inbound.Bytes), token).ConfigureAwait(false); }
                    catch (InvalidDataException) when (State == LoginClientState.NegotiatingTransport)
                    {
                        // A malformed response must not suppress the outstanding handshake deadline.
                    }
                }
                finally { CryptographicOperations.ZeroMemory(inbound.Bytes); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Outcome = LoginHostOutcome.Cancelled;
            throw new OperationCanceledException("Login host cancelled.", token);
        }
        catch (TimeoutException)
        {
            Outcome = LoginHostOutcome.TimedOut;
            throw new LoginHostFailureException(Outcome);
        }
        catch (LoginHostFailureException failure)
        {
            Outcome = failure.Outcome;
            throw;
        }
        catch (InvalidDataException)
        {
            Outcome = LoginHostOutcome.Malformed;
            throw new LoginHostFailureException(Outcome);
        }
        catch (Exception)
        {
            Outcome = LoginHostOutcome.IoFailure;
            throw new LoginHostFailureException(Outcome);
        }
        finally
        {
            try { linked.Cancel(); }
            catch (Exception) { CleanupFailures++; }
            coordinator.Dispose();
            Emit(new(LoginDiagnosticKind.HostOutcome, State, Outcome switch
            {
                LoginHostOutcome.Completed => LoginDiagnosticOutcome.Accepted,
                LoginHostOutcome.Cancelled => LoginDiagnosticOutcome.Cancelled,
                LoginHostOutcome.TimedOut => LoginDiagnosticOutcome.TimedOut,
                LoginHostOutcome.RemoteRejected => LoginDiagnosticOutcome.RemoteFailure,
                LoginHostOutcome.PeerDisconnected => LoginDiagnosticOutcome.PeerDisconnected,
                LoginHostOutcome.Malformed => LoginDiagnosticOutcome.Malformed,
                LoginHostOutcome.ClockFailure => LoginDiagnosticOutcome.ClockFailed,
                LoginHostOutcome.InvalidInput => LoginDiagnosticOutcome.InvalidInput,
                _ => LoginDiagnosticOutcome.IoFailed
            }));
            try { await port.DisposeAsync().ConfigureAwait(false); }
            catch (Exception) { CleanupFailures++; }
            if (receive is not null)
            {
                try
                {
                    InboundDatagram late = await receive.ConfigureAwait(false);
                    CryptographicOperations.ZeroMemory(late.Bytes);
                }
                catch (Exception) when (token.IsCancellationRequested) { }
                catch (Exception) { CleanupFailures++; }
            }
            stopped.TrySetResult();
        }
    }

    private long ReadClock()
    {
        try { return clock.ReadMilliseconds(); }
        catch (Exception) { throw new LoginHostFailureException(LoginHostOutcome.ClockFailure); }
    }

    private async ValueTask SendAsync(LoginOutboundDatagram datagram, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            accounting.RecordSendAttempt(datagram.Bytes.Length);
            Emit(new(LoginDiagnosticKind.HostSend, State, LoginDiagnosticOutcome.Accepted, datagram.Bytes.Length));
            try { await port.SendAsync(peer, datagram.Bytes, token).ConfigureAwait(false); }
            catch { accounting.RecordSendFailure(); throw; }
        }
        finally { CryptographicOperations.ZeroMemory(datagram.Bytes); }
    }

    private async ValueTask SendBatchAsync(IReadOnlyList<LoginOutboundDatagram> datagrams, CancellationToken token)
    {
        try
        {
            foreach (LoginOutboundDatagram datagram in datagrams)
                await SendAsync(datagram, token).ConfigureAwait(false);
        }
        finally
        {
            foreach (LoginOutboundDatagram datagram in datagrams) CryptographicOperations.ZeroMemory(datagram.Bytes);
        }
    }

    private void Emit(LoginDiagnosticEvent value)
    {
        try { diagnostics?.Invoke(value); }
        catch (Exception) { if (hostDiagnosticFailures < int.MaxValue) hostDiagnosticFailures++; }
    }

    public async ValueTask DisposeAsync()
    {
        bool hasStarted;
        bool first;
        lock (lifecycle)
        {
            first = disposed == 0;
            disposed = 1;
            hasStarted = started != 0;
            if (first)
            {
                try { lifetime.Cancel(); }
                catch (Exception) { CleanupFailures++; }
            }
        }
        if (hasStarted)
            await stopped.Task.ConfigureAwait(false);
        else if (first)
        {
            try
            {
                coordinator.Dispose();
                await port.DisposeAsync().ConfigureAwait(false);
            }
            finally { stopped.TrySetResult(); }
        }
        else await stopped.Task.ConfigureAwait(false);
        if (first) lifetime.Dispose();
    }
}
