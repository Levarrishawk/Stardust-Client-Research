using System.Buffers.Binary;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum SceneWorldCombinedMotionRoute { World, Compact, FullTransform, DeclinedIdentity }
public sealed record SceneWorldCombinedMotionApplicationResult(SceneWorldCombinedMotionRoute Route, bool Accepted,
    SceneWorldFullTransformApplicationResult? FullResult, SceneWorldObjectMotionSnapshot? Motion);

/// <summary>Borrowed synchronous pump receiver; owns only its two router wrappers.</summary>
public sealed class SceneWorldCombinedMotionApplicationRouter : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldMotionApplicationRouter? compact;
    private SceneWorldFullTransformApplicationRouter? full;
    private readonly ObjectControllerTransformDirection direction;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, reentered, disposed;
    private CancellationToken currentCancellation;
    private SceneWorldCombinedMotionApplicationResult? lastResult;
    public SceneWorldCombinedMotionApplicationRouter(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        Func<CompactSceneTransformMessage, SceneWorldObjectSnapshot, (AppearanceTransform Transform, float Speed)> compactProjection,
        SceneCreateZeroQuaternionPolicy fullZeroPolicy, ObjectControllerTransformDirection direction)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(compactProjection);
        if (direction != ObjectControllerTransformDirection.ServerToClient) throw new ArgumentOutOfRangeException(nameof(direction));
        _ = world.Snapshots();
        if (!motions.IsBoundTo(world)) throw new ArgumentException("Open motion store bound to exact world required.", nameof(motions));
        // Construct the full wrapper first so invalid zero policy cannot retain a compact callback.
        full = new(world, motions, fullZeroPolicy);
        compact = new(world, motions, (packet, snapshot) =>
        {
            var result = compactProjection(packet, snapshot);
            if (reentered) throw new InvalidOperationException("Combined ingress callback attempted reentrancy.");
            currentCancellation.ThrowIfCancellationRequested();
            return result;
        });
        this.world = world; this.motions = motions; this.direction = direction;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Combined ingress owner thread required.");
        if (busy) { reentered = true; throw new InvalidOperationException("Combined ingress reentrancy."); }
        busy = true; reentered = false;
    }
    /// <summary>Last completed receipt; historical, not a promise that Motion remains current.</summary>
    public SceneWorldCombinedMotionApplicationResult? LastResult
    { get { Enter(); try { ObjectDisposedException.ThrowIf(disposed, this); return lastResult; } finally { busy = false; } } }
    public bool Accept(long generation, ReadOnlySpan<byte> application) => Receive(generation, application).Accepted;
    public SceneWorldCombinedMotionApplicationResult Receive(long generation, ReadOnlySpan<byte> application,
        CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            currentCancellation = cancellationToken;
            if (generation != world!.Generation || world.State is ZoneSceneTransitionState.Closed or ZoneSceneTransitionState.Failed || !motions!.IsBoundTo(world))
                return lastResult = new(SceneWorldCombinedMotionRoute.DeclinedIdentity, false, null, null);
            uint opcode = application.Length >= 6 ? BinaryPrimitives.ReadUInt32LittleEndian(application[2..]) : 0;
            if (opcode is CompactSceneTransformCodec.UpdateTransformOpcode or CompactSceneTransformCodec.UpdateTransformWithParentOpcode)
            {
                var raw = CompactSceneTransformCodec.Decode(application); // Own scalar identity before caller projection can mutate its source bytes.
                bool accepted = compact!.Accept(generation, application);
                if (reentered) throw new InvalidOperationException("Combined ingress callback attempted reentrancy.");
                SceneWorldObjectMotionSnapshot? motion = null;
                if (accepted)
                {
                    var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == raw.ObjectId);
                    if (snapshot is not null) motion = motions.Find(snapshot);
                }
                return lastResult = new(SceneWorldCombinedMotionRoute.Compact, accepted, null, motion);
            }
            if (opcode == ObjectControllerTransformCodec.Opcode && (application.Length < 14 ||
                BinaryPrimitives.ReadUInt32LittleEndian(application[10..]) is (uint)ObjectControllerTransformKind.Root or (uint)ObjectControllerTransformKind.WithParent))
            {
                var result = full!.Accept(generation, application, direction, cancellationToken);
                return lastResult = new(SceneWorldCombinedMotionRoute.FullTransform, result.Applied, result, result.Motion);
            }
            bool fallback = world.Accept(generation, application);
            if (reentered) throw new InvalidOperationException("Combined ingress fallback attempted reentrancy; world effects may already have occurred.");
            return lastResult = new(SceneWorldCombinedMotionRoute.World, fallback, null, null);
        } finally { currentCancellation = default; busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return;
            compact!.Dispose(); full!.Dispose(); disposed = true;
            compact = null; full = null; world = null; motions = null; lastResult = null;
        } finally { busy = false; }
    }
}
