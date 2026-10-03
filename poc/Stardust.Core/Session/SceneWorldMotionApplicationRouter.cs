using System.Buffers.Binary;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Synchronous modern ingress; parent-local compact motion is deliberately unsupported.</summary>
public sealed class SceneWorldMotionApplicationRouter : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private Func<CompactSceneTransformMessage, SceneWorldObjectSnapshot, (AppearanceTransform Transform, float Speed)>? worldProjection;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, reentered, disposed;

    public SceneWorldMotionApplicationRouter(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        Func<CompactSceneTransformMessage, SceneWorldObjectSnapshot, (AppearanceTransform Transform, float Speed)> worldProjection)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(worldProjection);
        _ = world.Snapshots();
        this.world = world; this.motions = motions; this.worldProjection = worldProjection;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Motion ingress owner thread required.");
        if (busy) { reentered = true; throw new InvalidOperationException("Motion ingress reentrancy."); }
        busy = true; reentered = false;
    }
    private bool Active => world!.State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
    public bool Accept(long generation, ReadOnlySpan<byte> application)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (generation != world!.Generation) return false;
            uint opcode = application.Length >= 6 ? BinaryPrimitives.ReadUInt32LittleEndian(application[2..]) : 0;
            if (opcode != CompactSceneTransformCodec.UpdateTransformOpcode && opcode != CompactSceneTransformCodec.UpdateTransformWithParentOpcode)
                return world.Accept(generation, application);
            var packet = CompactSceneTransformCodec.Decode(application);
            if (packet.ParentId.HasValue || !Active) return false;
            var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == packet.ObjectId);
            if (snapshot is not { Object.Phase: SceneObjectPhase.Finalized }) return false;
            var batch = world.DrainChanges(generation)!; // Read-only mutation stamp; never acknowledge here.
            var state = world.State;
            var previousMotion = motions!.Find(snapshot);
            var projected = worldProjection!(packet, snapshot);
            // A caught nested call still invalidates this callback result.
            if (reentered) throw new InvalidOperationException("Motion ingress callback attempted reentrancy.");
            if (generation != world.Generation || !Active || state != world.State || !world.IsCurrent(batch)) return false;
            if (!ReferenceEquals(previousMotion, motions!.Find(snapshot))) return false;
            // Store checks canonical object/placement/envelopes and typed-state freshness after callback.
            return motions!.PublishAuthoritative(snapshot, projected.Transform, projected.Speed, packet.MovementCounter);
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return;
            disposed = true; world = null; motions = null; worldProjection = null;
        } finally { busy = false; }
    }
}
