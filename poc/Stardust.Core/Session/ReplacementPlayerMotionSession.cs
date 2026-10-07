using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Selected full-root movement boundary; borrowed session/world/store, no cadence.</summary>
public sealed class ReplacementPlayerMotionSession : IDisposable
{
    private readonly ReplacementClientSession session;
    private readonly SceneWorldCoordinator world;
    private readonly SceneWorldClientRootMotionBuilder builder;
    private readonly SceneWorldFullTransformApplicationRouter router;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private long generation;
    private ulong selectedObjectId;
    private bool bound, busy, disposed;
    public ReplacementPlayerMotionSession(ReplacementClientSession session, SceneWorldCoordinator world,
        SceneWorldObjectMotionStore motions, SceneWorldClientRootMotionPolicy policy, SceneCreateZeroQuaternionPolicy zeroPolicy)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        router = new(world, motions, zeroPolicy);
        try { builder = new(world, motions, policy); }
        catch { router.Dispose(); throw; }
    }
    public SceneWorldFullTransformApplicationStatus? LastInboundStatus { get; private set; }
    public int AppliedCorrections { get; private set; }
    public int CounterRejectedCorrections { get; private set; }
    public void Bind(long generation, ulong selectedObjectId)
    {
        Enter(); try
        {
            Open();
            if (bound || generation <= 0 || generation != world.Generation || selectedObjectId == 0
                || selectedObjectId != session.SelectedObjectId || session.Phase is not
                (ReplacementClientPhase.ZoneHandshake or ReplacementClientPhase.ZonePermissions or ReplacementClientPhase.Selected))
                throw new InvalidOperationException("Exact fresh admitted selection required.");
            this.generation = generation; this.selectedObjectId = selectedObjectId; bound = true;
        } finally { busy = false; }
    }
    public bool TryHandleIncoming(long generation, ReadOnlyMemory<byte> application)
    {
        Enter(); try
        {
            Open(); Current(generation); var bytes = application.Span;
            if (bytes.Length < 6) throw new InvalidDataException("Application header truncated.");
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..]) != ObjectControllerTransformCodec.Opcode) return false;
            if (bytes.Length < 14) throw new InvalidDataException("ObjectController discriminator truncated.");
            var kind = (ObjectControllerTransformKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[10..]);
            if (kind is not (ObjectControllerTransformKind.Root or ObjectControllerTransformKind.WithParent)) return false;
            var raw = ObjectControllerTransformCodec.Decode(bytes, ObjectControllerTransformDirection.ServerToClient);
            if (raw.Kind != ObjectControllerTransformKind.Root || raw.ObjectId != selectedObjectId) return false;
            if (session.Phase != ReplacementClientPhase.Selected) throw new InvalidOperationException("Selected session required for correction.");
            var result = router.Accept(generation, bytes, ObjectControllerTransformDirection.ServerToClient);
            LastInboundStatus = result.Status;
            if (result.Status == SceneWorldFullTransformApplicationStatus.Applied) AppliedCorrections++;
            if (result.Status == SceneWorldFullTransformApplicationStatus.CounterRejected) CounterRejectedCorrections++;
            return true;
        } finally { busy = false; }
    }
    public ReplacementClientExchange? TryQueueLocal(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion,
        uint flags, uint movementCounter, uint timestampBits, long nowMilliseconds)
    {
        Enter(); try
        {
            Open(); Current(generation);
            if (session.Phase != ReplacementClientPhase.Selected || world.State != ZoneSceneTransitionState.Ready)
                throw new InvalidOperationException("Selected session and ready world required.");
            if (session.ZonePendingMessages != 0) return null;
            ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(motion);
            if (snapshot.Object is null || snapshot.Object.Handle.ObjectId != selectedObjectId) throw new InvalidDataException("Selected snapshot required.");
            var packet = builder.Build(snapshot, motion, flags, movementCounter, timestampBits);
            try { return session.QueueZoneApplication(packet.Application, nowMilliseconds); }
            finally { CryptographicOperations.ZeroMemory(packet.Application); }
        } finally { busy = false; }
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread || busy) throw new InvalidOperationException("Motion owner thread without reentry required.");
        busy = true;
    }
    private void Open() { ObjectDisposedException.ThrowIf(disposed, this); }
    private void Current(long value)
    {
        if (!bound || value != generation || generation != world.Generation || session.SelectedObjectId != selectedObjectId
            || session.Phase is ReplacementClientPhase.Closed or ReplacementClientPhase.Faulted || world.State == ZoneSceneTransitionState.Closed)
            throw new InvalidOperationException("Current open selected binding required.");
    }
    public void Dispose()
    {
        Enter(); try { if (disposed) return; disposed = true; try { builder.Dispose(); } finally { router.Dispose(); } }
        finally { busy = false; }
    }
}
