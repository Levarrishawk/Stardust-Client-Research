using System.Numerics;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.Session;

public sealed record SceneWorldTerrainMotionLimits(int MaximumPatches = 64,
    int MaximumTriangleTestsPerCandidate = 512, float MaximumPositionComponent = 1_000_000f,
    float MaximumFootOffset = 1000f);

/// <summary>Modern supplied-terrain grounding over the frozen planar integer scheduler.</summary>
public sealed class SceneWorldTerrainMotionDriver : ISceneWorldLocalMotionDriver
{
    private sealed record Pose(AppearanceTransform Transform, float Speed);
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private FixedStepActorHost<Pose>? host;
    private PortalActorInputTimeline<Pose>? timeline;
    private TerrainPatchSurfaceQuery[]? surfaces;
    private TerrainModernPatchSetSurfaceQuery? aggregateSurface;
    private readonly SceneWorldTerrainMotionLimits limits;
    private readonly float footOffset;
    private readonly SceneObjectIncarnation incarnation;
    private readonly AppearanceTransform initialTransform;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private Pose pose;
    private Pose resetPose = null!;
    private SceneWorldObjectMotionSnapshot? lastPublication;
    private ulong observedAuthoritativeRevision;
    private string? fault;
    private bool busy, disposed;
    private SceneWorldLocalMotionDriverState state;
    public SceneWorldTerrainMotionDriver(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions, ulong objectId,
        IReadOnlyList<TerrainPatchSurfaceQuery> surfaces, float footOffset = 0,
        long initialTimestampMicroseconds = 0, long stepMicroseconds = 16667, int maximumStepsPerUpdate = 8,
        SceneWorldTerrainMotionLimits? limits = null)
        : this(world,motions,objectId,surfaces,footOffset,initialTimestampMicroseconds,stepMicroseconds,maximumStepsPerUpdate,limits,null) { }
    public SceneWorldTerrainMotionDriver(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions, ulong objectId,
        TerrainModernPatchSetSurfaceQuery surface, float footOffset = 0,
        long initialTimestampMicroseconds = 0, long stepMicroseconds = 16667, int maximumStepsPerUpdate = 8,
        SceneWorldTerrainMotionLimits? limits = null)
        : this(world,motions,objectId,AggregateSurfaces(surface,limits),footOffset,initialTimestampMicroseconds,stepMicroseconds,maximumStepsPerUpdate,limits,surface) { }
    private static TerrainPatchSurfaceQuery[] AggregateSurfaces(TerrainModernPatchSetSurfaceQuery surface,SceneWorldTerrainMotionLimits? limits)
    {
        ArgumentNullException.ThrowIfNull(surface);var policy=limits??new();
        if(surface.PatchCount<1||surface.PatchCount>policy.MaximumPatches||surface.TotalTriangles>policy.MaximumTriangleTestsPerCandidate)
            throw new ArgumentOutOfRangeException(nameof(surface),"Complete retained query set exceeds motion admission limits.");
        _=surface.GetQueryReservation(1);
        return surface.Children.Select(c=>c.Query).ToArray();
    }
    private SceneWorldTerrainMotionDriver(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions, ulong objectId,
        IReadOnlyList<TerrainPatchSurfaceQuery> surfaces,float footOffset,long initialTimestampMicroseconds,
        long stepMicroseconds,int maximumStepsPerUpdate,SceneWorldTerrainMotionLimits? limits,TerrainModernPatchSetSurfaceQuery? aggregate)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        if (initialTimestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(initialTimestampMicroseconds));
        if (stepMicroseconds is < 1000 or > 100000) throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        if (maximumStepsPerUpdate is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(maximumStepsPerUpdate));
        ArgumentNullException.ThrowIfNull(surfaces);
        var policy = limits ?? new();
        if (policy.MaximumPatches is < 1 or > 256 || policy.MaximumTriangleTestsPerCandidate is < 1 or > 2048 ||
            !float.IsFinite(policy.MaximumPositionComponent) || policy.MaximumPositionComponent <= 0 || policy.MaximumPositionComponent > 1e9f ||
            !float.IsFinite(policy.MaximumFootOffset) || policy.MaximumFootOffset < 0 || policy.MaximumFootOffset > 1000f)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if (!float.IsFinite(footOffset) || Math.Abs(footOffset) > policy.MaximumFootOffset)
            throw new ArgumentOutOfRangeException(nameof(footOffset));
        int surfaceCount = surfaces.Count;
        if (surfaceCount is < 1 || surfaceCount > policy.MaximumPatches)
            throw new ArgumentOutOfRangeException(nameof(surfaces));
        var owned = new TerrainPatchSurfaceQuery[surfaceCount];
        for (int i = 0; i < owned.Length; i++) owned[i] = surfaces[i] ?? throw new ArgumentException("Null supplied surface.", nameof(surfaces));
        this.limits = policy with {}; this.footOffset = footOffset; this.surfaces = owned; aggregateSurface=aggregate;
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == objectId);
        if (world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready) ||
            snapshot is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } || !world.IsCurrent(snapshot.Replication))
            throw new InvalidDataException("Local motion requires a current finalized typed CREO incarnation.");
        var existing = motions.Find(snapshot); // Checks store owner affinity before retaining borrowed dependencies.
        incarnation = snapshot.Object.Incarnation; initialTransform = snapshot.Placement.Transform;
        pose = new(Ground(existing?.Transform ?? initialTransform), existing?.Speed ?? 0);
        this.world = world; this.motions = motions;
        host = new(() => pose, Tick, () => pose = resetPose, (int)stepMicroseconds, maximumStepsPerUpdate);
        timeline = new(host, initialTimestampMicroseconds, new(Vector3.Zero, null));
        if (!motions.PublishLocal(snapshot, pose.Transform, pose.Speed)) throw new ArgumentException("Motion store must belong to this world.", nameof(motions));
        lastPublication = motions.Find(snapshot) ?? throw new InvalidOperationException("Initial local publication missing.");
        observedAuthoritativeRevision = existing is { Authoritative: true } ? existing.Revision : 0;
        state = Map(timeline.State);
    }
    private void ValidatePosition(Vector3 position)
    {
        float cap = limits.MaximumPositionComponent;
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) ||
            Math.Abs(position.X) > cap || Math.Abs(position.Y) > cap || Math.Abs(position.Z) > cap)
            throw new InvalidDataException("Terrain motion requires a finite bounded native position.");
    }
    private AppearanceTransform Ground(AppearanceTransform candidate)
    {
        // Validate X/Z and the incoming Y before replacing height; no invalid transform is masked.
        ValidatePosition(candidate.Translation);
        if(aggregateSurface is not null){
            var selected=aggregateSurface.Query(candidate.Translation.X,candidate.Translation.Z)
                ??throw new InvalidDataException("Local motion candidate outside retained terrain coverage.");
            return GroundHeight(candidate,selected.Hit.Height);
        }
        int reserved = 0;
        foreach (var surface in surfaces!)
        {
            reserved = checked(reserved + surface.TriangleCount);
            if (reserved > limits.MaximumTriangleTestsPerCandidate)
                throw new InvalidDataException("Terrain candidate triangle-test budget.");
            if (surface.Query(candidate.Translation.X, candidate.Translation.Z) is not { } hit) continue;
            return GroundHeight(candidate,hit.Height);
        }
        throw new InvalidDataException("Local motion candidate outside supplied terrain coverage.");
    }
    private AppearanceTransform GroundHeight(AppearanceTransform candidate,double surfaceHeight)
    {
        double height=surfaceHeight+(double)footOffset;
        if(!double.IsFinite(height)||Math.Abs(height)>limits.MaximumPositionComponent)
            throw new InvalidDataException("Grounded terrain height outside finite position cap.");
        var grounded=candidate with { Translation=new(candidate.Translation.X,(float)height,candidate.Translation.Z) };
        ValidatePosition(grounded.Translation);return grounded;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Local motion owner thread required.");
        if (busy) throw new InvalidOperationException("Local motion driver reentrancy.");
        busy = true;
    }
    public SceneWorldLocalMotionDriverState State { get { Enter(); try { return state; } finally { busy = false; } } }
    private SceneWorldObjectSnapshot? Current()
    {
        if (world!.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) return null;
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Incarnation == incarnation);
        return snapshot is { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } && world.IsCurrent(snapshot.Replication) ? snapshot : null;
    }
    private SceneWorldObjectSnapshot RequireCurrent()
    {
        if (Current() is { } snapshot) return snapshot;
        throw new InvalidOperationException("Selected local motion incarnation is no longer current.");
    }
    private void Latch(Exception error) { fault ??= error.GetType().Name; state = state with { Active = false, FaultType = fault }; }
    private SceneWorldLocalMotionDriverState Map(FixedStepActorState<Pose> clock) => new(incarnation.ObjectId,
        pose.Transform, pose.Speed, clock.TimestampMicroseconds!.Value, clock.StepsThisUpdate,
        clock.DroppedMicroseconds, clock.SuspendedMicroseconds, !disposed && fault is null && clock.Active, fault ?? clock.FaultType);
    private static void Validate(PortalActorInput input)
    {
        FixedStepActorHost<Pose>.ValidateInput(input);
        if (input.WorldVelocity.Y != 0) throw new ArgumentOutOfRangeException(nameof(input), "Planar input requires velocity Y zero.");
    }
    public void Submit(long timestampMicroseconds, PortalActorInput input)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); Validate(input);
            if (timestampMicroseconds < state.TimestampMicroseconds ||
                timestampMicroseconds - state.TimestampMicroseconds > PortalActorInputTimeline<Pose>.MaximumFutureMicroseconds)
                throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds), "Nonlate bounded input time required.");
            if (fault is not null) throw new InvalidOperationException("Local motion is faulted; explicit reset required.");
            try { _ = RequireCurrent(); } catch (Exception error) { Latch(error); throw; }
            timeline!.Submit(new(timestampMicroseconds, input)); // Admission rejects before modifying timeline history.
        } finally { busy = false; }
    }
    private void Tick(float seconds, Vector3 displacement, bool? entryAllowed)
    {
        var snapshot = RequireCurrent();
        var velocity = timeline!.CurrentInput.WorldVelocity;
        float speed = velocity.Length(); var transform = pose.Transform with { Translation = pose.Transform.Translation + displacement };
        if (speed > 0)
        {
            float sine = velocity.X / speed, cosine = velocity.Z / speed;
            transform = transform with { RowX = new(cosine, 0, sine), RowY = Vector3.UnitY, RowZ = new(-sine, 0, cosine) };
        }
        transform = Ground(transform);
        if (!motions!.PublishLocal(snapshot, transform, speed)) throw new InvalidOperationException("Local motion publication became stale.");
        var publication = motions.Find(snapshot) ?? throw new InvalidOperationException("Local motion publication missing.");
        pose = new(transform, speed); lastPublication = publication;
    }
    public SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds, bool active = true)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (timestampMicroseconds < state.TimestampMicroseconds) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds), "Monotonic time required.");
            try
            {
                if (fault is null)
                {
                    var snapshot = RequireCurrent(); var motion = motions!.Find(snapshot);
                    if (motion is { Authoritative: true } && !ReferenceEquals(motion, lastPublication) && motion.Revision > observedAuthoritativeRevision)
                    { ValidatePosition(motion.Transform.Translation); pose = new(motion.Transform, motion.Speed); observedAuthoritativeRevision = motion.Revision; }
                }
                var clock = timeline!.AdvanceTo(timestampMicroseconds, active && fault is null);
                if (clock.FaultType is not null) fault ??= clock.FaultType;
                state = Map(clock); return state;
            }
            catch (Exception error)
            {
                Latch(error);
                // No implicit tick retry. Preserve any completed publications and last observed clock state.
                state = Map(timeline!.State); return state;
            }
        } finally { busy = false; }
    }
    public void Reset(long timestampMicroseconds)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (timestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
            try
            {
                var snapshot = RequireCurrent();
                var candidate = new Pose(Ground(initialTransform), 0);
                if (!motions!.PublishLocal(snapshot, candidate.Transform, 0)) throw new InvalidOperationException("Reset motion publication became stale.");
                var publication = motions.Find(snapshot) ?? throw new InvalidOperationException("Reset publication missing.");
                resetPose = candidate; timeline!.Reset(timestampMicroseconds, new(Vector3.Zero, null));
                lastPublication = publication; observedAuthoritativeRevision = 0; fault = null; state = Map(timeline.State);
            }
            catch (Exception error) { Latch(error); throw; }
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return; disposed = true;
            state = state with { Active = false }; timeline = null; host = null; world = null; motions = null; lastPublication = null; surfaces = null; aggregateSurface=null; resetPose = null!;
        } finally { busy = false; }
    }
}
