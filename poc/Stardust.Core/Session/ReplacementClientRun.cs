using System.Security.Cryptography;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed record ReplacementClientRunTick(IReadOnlyList<byte[]> UnhandledApplications, string? FailureType = null);

/// <summary>Explicit caller-driven application loop; borrows owners and owns admission/motion/barrier adapters.</summary>
public sealed class ReplacementClientRun : IDisposable
{
    private readonly ReplacementClientSession session;
    private readonly ReplacementDatagramPump pump;
    private readonly SceneWorldCoordinator world;
    private readonly ReplacementSelectedPlayerIngress ingress;
    private readonly ReplacementPlayerMotionSession motion;
    private readonly ReplacementSceneBridge bridge;
    private readonly List<Task> cleanupFailures = [];
    private readonly int owner = Environment.CurrentManagedThreadId;
    private long clock = -1;
    private ulong selectedObjectId;
    private string? failure;
    private bool bound, busy, disposed, ownedReleased;

    public ReplacementClientRun(ReplacementClientSession session, ReplacementDatagramPump pump,
        SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        Func<CmdStartSceneMessage, CancellationToken, Task> minimumResources,
        SceneWorldClientRootMotionPolicy rootPolicy, SceneCreateZeroQuaternionPolicy zeroPolicy)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(pump);
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        ArgumentNullException.ThrowIfNull(minimumResources); ArgumentNullException.ThrowIfNull(rootPolicy);
        if (!pump.IsBoundTo(session) || !motions.IsBoundTo(world) || session.Phase != ReplacementClientPhase.Offline
            || world.State != ZoneSceneTransitionState.Idle) throw new ArgumentException("Fresh exact borrowed owners required.");
        _ = world.Snapshots();
        this.session = session; this.pump = pump; this.world = world;
        ingress = new();
        try { motion = new(session, world, motions, rootPolicy, zeroPolicy); }
        catch { ingress.Dispose(); throw; }
        bridge = new(session, world, minimumResources, ingress, motion);
    }
    public long Generation { get { Owner(); return bridge.Generation; } }
    public ulong SelectedObjectId { get { Owner(); return selectedObjectId; } }
    public int ReadySubmissions { get { Owner(); return bridge.ReadySubmissions; } }
    public SceneWorldFullTransformApplicationStatus? LastInboundStatus { get { Owner(); return motion.LastInboundStatus; } }
    public int AppliedCorrections { get { Owner(); return motion.AppliedCorrections; } }
    public int CounterRejectedCorrections { get { Owner(); return motion.CounterRejectedCorrections; } }
    public bool DisconnectRequested { get { Owner(); return pump.DisconnectRequested; } }
    public bool DisconnectSubmitted { get { Owner(); return pump.DisconnectSubmitted; } }
    public bool DisconnectSendCompleted { get { Owner(); return pump.DisconnectSendCompleted; } }
    public string? FailureType { get { Owner(); return failure; } }
    public Task DrainCompletion
    {
        get { Owner(); return Task.WhenAll(new[] { bridge.DrainCompletion, pump.DrainCompletion, world.DrainCompletion }.Concat(cleanupFailures)); }
    }
    public override string ToString() { Owner(); return $"replacement_run_ready={bridge.ReadySubmissions} failed={failure is not null}"; }

    public void StartLogin(uint id, long now) => Operation(now, () => pump.StartLogin(id, now));
    public void Authenticate(string user, string password, long now) => Operation(now, () => pump.Authenticate(user, password, now));
    public void SelectCharacter(ulong objectId, uint zoneId, IDatagramPort zonePort, DatagramPeer peer, long now)
    {
        Enter(now);
        try
        {
            Open(); if (bound) throw new InvalidOperationException("One selected run required.");
            try
            {
                pump.BeginZone(objectId, zoneId, zonePort, peer, now);
                bridge.BindSelection(); selectedObjectId = objectId; bound = true;
            }
            catch (Exception error) { Fault(error.GetType().Name); throw; }
        }
        finally { busy = false; }
    }
    public ReplacementClientRunTick Tick(long now)
    {
        Enter(now); var unhandled = new List<byte[]>(); IReadOnlyList<ReplacementClientExchange> exchanges = [];
        try
        {
            if (failure is not null) return new([], failure);
            if (pump.DisconnectRequested)
            {
                if (!pump.DisconnectSendCompleted) exchanges = pump.Tick(now);
                string? pumpFailure = pump.FailureType ?? exchanges.FirstOrDefault(e => e.FailureType is not null)?.FailureType;
                if (pumpFailure is not null) Fault(pumpFailure);
                if (DrainCompletion is { IsCompleted: true } drain) drain.GetAwaiter().GetResult();
                return new([], failure);
            }
            if (session.Phase == ReplacementClientPhase.Offline) return new([]);
            if (bound) Current();
            exchanges = pump.Tick(now);
            foreach (var exchange in exchanges)
            {
                if (exchange.FailureType is not null) { Fault(exchange.FailureType); return new([], failure); }
                if (!bound)
                {
                    if (exchange.ZoneApplications.Count != 0) throw new InvalidDataException("Applications before run selection.");
                    continue;
                }
                Current(); var routed = bridge.ObserveExchange(exchange);
                if (routed.FailureType is not null) { Fault(routed.FailureType); return new([], failure); }
                unhandled.AddRange(routed.ZoneApplications);
            }
            if (bound && session.Phase == ReplacementClientPhase.Selected)
            {
                Current(); pump.Submit(bridge.Poll(now));
            }
            return new(unhandled.AsReadOnly());
        }
        catch (Exception error) { Fault(error.GetType().Name); return new([], failure); }
        finally
        {
            foreach (var exchange in exchanges) foreach (byte[] app in exchange.ZoneApplications) CryptographicOperations.ZeroMemory(app);
            if (failure is not null) foreach (byte[] app in unhandled) CryptographicOperations.ZeroMemory(app);
            busy = false;
        }
    }
    public bool TryQueueLocal(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion,
        uint flags, uint counter, uint timestampBits, long now)
    {
        Enter(now);
        try
        {
            Open(); Current();
            try
            {
                var exchange = this.motion.TryQueueLocal(snapshot, motion, flags, counter, timestampBits, now);
                if (exchange is null) return false;
                pump.Submit(exchange); return true;
            }
            catch (Exception error) { Fault(error.GetType().Name); throw; }
        }
        finally { busy = false; }
    }
    public void BeginDisconnect(long now, long timeoutMilliseconds = 3000, ushort reasonWord = 0x0006)
    {
        Enter(now);
        try
        {
            Open(); Current();
            try { pump.BeginDisconnect(now, timeoutMilliseconds, reasonWord); }
            catch (Exception error)
            {
                if (pump.FailureType is not null || session.Phase == ReplacementClientPhase.Faulted)
                    Fault(pump.FailureType ?? session.FailureType ?? error.GetType().Name);
                throw;
            }
            try { bridge.Dispose(); }
            catch (Exception error) { Fault(error.GetType().Name); throw; }
        }
        finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(null, true);
        try
        {
            if (disposed) return; disposed = true;
            try { ReleaseOwned(); }
            catch (Exception error)
            {
                failure ??= error.GetType().Name;
                cleanupFailures.Add(Task.FromException(new InvalidOperationException(error.GetType().Name))); throw;
            }
        }
        finally { busy = false; }
    }
    private void Operation(long now, Action action)
    {
        Enter(now);
        try
        {
            Open(); if (pump.DisconnectRequested) throw new InvalidOperationException("Disconnect run forbids protocol work.");
            try { action(); } catch (Exception error) { Fault(error.GetType().Name); throw; }
        }
        finally { busy = false; }
    }
    private void Current()
    {
        if (!bound || bridge.Generation != world.Generation || selectedObjectId != session.SelectedObjectId)
            throw new InvalidOperationException("Current selected run generation required.");
    }
    private void Owner() { if (Environment.CurrentManagedThreadId != owner) throw new InvalidOperationException("Run owner thread required."); }
    private void Enter(long? now, bool allowDisposed = false)
    {
        Owner(); if (busy) throw new InvalidOperationException("Run reentry.");
        if (!allowDisposed) ObjectDisposedException.ThrowIf(disposed, this);
        if (now is long value) { if (value < 0 || value < clock) throw new ArgumentOutOfRangeException(nameof(now)); clock = value; }
        busy = true;
    }
    private void Open() { if (failure is not null) throw new InvalidOperationException("Run failed."); }
    private void Fault(string type)
    {
        failure ??= type;
        Cleanup(ReleaseOwned); Cleanup(pump.Dispose);
        if (session.Phase != ReplacementClientPhase.Closed) Cleanup(session.Dispose);
        Cleanup(world.Close);
    }
    private void Cleanup(Action action)
    {
        try { action(); } catch (Exception error) { cleanupFailures.Add(Task.FromException(new InvalidOperationException(error.GetType().Name))); }
    }
    private void ReleaseOwned()
    {
        if (ownedReleased) return; ownedReleased = true;
        try { bridge.Dispose(); } finally { try { motion.Dispose(); } finally { ingress.Dispose(); } }
    }
}
