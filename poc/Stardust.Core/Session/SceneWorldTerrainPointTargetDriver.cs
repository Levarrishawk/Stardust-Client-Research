using System.Numerics;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.Session;

public enum SceneWorldTerrainPointTargetMode { Idle, Moving, Arrived, Unavailable, Faulted, Disposed, Paused, Manual }
public sealed class SceneWorldTerrainPointTargetReceipt
{
    internal SceneWorldTerrainPointTargetReceipt(TerrainModernPatchSetRayQuery query, TerrainModernPatchSetRayHit hit,
        float x, float z, long clock, int revision)
    { Query = query; Hit = hit; X = x; Z = z; TimestampMicroseconds = clock; Revision = revision; }
    public TerrainModernPatchSetRayQuery Query { get; }
    public TerrainModernPatchSetRayHit Hit { get; }
    public float X { get; }
    public float Z { get; }
    public long TimestampMicroseconds { get; }
    public int Revision { get; }
}
public sealed record SceneWorldTerrainPointTargetState(SceneWorldTerrainPointTargetMode Mode, int AttemptedUpdates,
    int CompletedUpdates, int SuccessfulTargets, SceneWorldTerrainPointTargetReceipt? Target,
    SceneWorldLocalMotionDriverState DriverResult, string? FaultType);

/// <summary>Modern straight-line bounded point targeting over an owned actual terrain driver.</summary>
public sealed class SceneWorldTerrainPointTargetDriver : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly SceneObjectIncarnation incarnation;
    private readonly long generation, stepMicroseconds;
    private readonly int maximumUpdates, maximumTargets;
    private float speed;
    private readonly float radius, positionCap, footOffset;
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private TerrainModernPatchSetRayQuery? query;
    private TerrainModernPatchSetSurfaceQuery? destinationSurface;
    private TerrainOfflineDestinationReceipt? lastDestinationReceipt;
    private SceneWorldTerrainMotionDriver? driver;
    private SceneWorldTerrainPointTargetReceipt? target;
    private Vector2 manualInput;
    private SceneWorldTerrainPointTargetState state;
    private bool busy, disposed, paused, reentered;
    private SceneWorldObjectMotionSnapshot? lastPublication;
    private ulong observedAuthoritativeRevision;
    public SceneWorldTerrainPointTargetDriver(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        ulong objectId, TerrainModernPatchSetRayQuery rayQuery, float speed, float arrivalRadius,
        long initialTimestampMicroseconds, long stepMicroseconds, float footOffset = 0,
        int maximumUpdates = 128, int maximumTargets = 16, SceneWorldTerrainMotionLimits? terrainLimits = null, CancellationToken cancellationToken = default, TerrainOfflineSessionBudget? budget = null,
        bool holdMissingTerrainDestination = false)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(rayQuery);
        cancellationToken.ThrowIfCancellationRequested();
        if (!float.IsFinite(speed) || speed <= 0 || speed > 10 || !float.IsFinite(arrivalRadius) || arrivalRadius < 1e-4f || arrivalRadius > 1)
            throw new ArgumentOutOfRangeException(nameof(speed));
        if (maximumUpdates < 1 || maximumUpdates > (budget?.MaximumUpdates ?? 128) || maximumTargets < 1 || maximumTargets > (budget?.MaximumTargets ?? 16) || stepMicroseconds is < 1000 or > 100000 ||
            initialTimestampMicroseconds < 0 || initialTimestampMicroseconds > long.MaxValue - checked((long)maximumUpdates * stepMicroseconds))
            throw new ArgumentOutOfRangeException(nameof(maximumUpdates));
        if (!motions.IsBoundTo(world)) throw new ArgumentException("Exact open world/store binding required.", nameof(motions));
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == objectId);
        if (world.State is not (ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady) ||
            snapshot is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } || !world.IsCurrent(snapshot.Replication))
            throw new InvalidDataException("Current finalized typed incarnation required.");
        var existing = motions.Find(snapshot);
        if (existing is not null && (!float.IsFinite(existing.Speed) || existing.Speed < 0 || existing.Speed > 10))
            throw new InvalidDataException("Existing motion speed admission before initial publication.");
        this.world = world; this.motions = motions; query = rayQuery; this.speed = speed; radius = arrivalRadius; this.footOffset = footOffset;
        generation = world.Generation; incarnation = snapshot.Object.Incarnation; this.stepMicroseconds = stepMicroseconds;
        this.maximumUpdates = maximumUpdates; this.maximumTargets = maximumTargets; positionCap = (terrainLimits ?? new()).MaximumPositionComponent;
        // Frozen driver validates all terrain options, pose and grounding before its initial publication.
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (holdMissingTerrainDestination)
            {
                var policy = terrainLimits ?? new();
                var retained = rayQuery.Source;
                if (retained.PatchCount < 1 || retained.PatchCount > policy.MaximumPatches ||
                    retained.TotalTriangles > policy.MaximumTriangleTestsPerCandidate)
                    throw new ArgumentOutOfRangeException(nameof(rayQuery), "Destination query exceeds frozen per-candidate caps.");
                _ = retained.GetQueryReservation(1);
                cancellationToken.ThrowIfCancellationRequested();
                destinationSurface = retained;
            }
            driver = new(world, motions, objectId, rayQuery.Source, footOffset, initialTimestampMicroseconds, stepMicroseconds, 1, terrainLimits);
            cancellationToken.ThrowIfCancellationRequested();
            state = new(SceneWorldTerrainPointTargetMode.Idle, 0, 0, 0, null, driver.State, null);
            lastPublication = motions.Find(snapshot); observedAuthoritativeRevision = existing is { Authoritative: true } ? existing.Revision : 0;
        }
        catch { driver?.Dispose(); driver = null; this.world = null; this.motions = null; query = null; destinationSurface = null; throw; }
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Point target owner thread required.");
        if (busy) { reentered = true; throw new InvalidOperationException("Point target reentry."); }
        busy = true; reentered = false;
    }
    private void CheckReentry() { if (reentered) throw new InvalidOperationException("Point target operation poisoned by reentry."); }
    private T Read<T>(Func<T> value) { Enter(); try { var result = value(); CheckReentry(); return result; } finally { busy = false; } }
    public SceneWorldTerrainPointTargetState State => Read(() => state);
    public SceneWorldTerrainMotionDriver? Driver => Read(() => driver);
    public TerrainModernPatchSetRayQuery? Query => Read(() => query);
    public SceneWorldTerrainPointTargetReceipt? Target => Read(() => target);
    public TerrainOfflineDestinationReceipt? LastDestinationReceipt => Read(() => lastDestinationReceipt);
    public bool IsPaused => Read(() => paused);
    public float MovementSpeed => Read(() => speed);
    public void SetMovementSpeed(float speed, long exactCurrentClock, CancellationToken token = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); token.ThrowIfCancellationRequested();
            if (state.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable || !Current())
                throw new InvalidOperationException("Point target incarnation unavailable.");
            if (exactCurrentClock != state.DriverResult.TimestampMicroseconds || state.AttemptedUpdates >= maximumUpdates)
                throw new ArgumentOutOfRangeException(nameof(exactCurrentClock));
            if (!float.IsFinite(speed) || speed <= 0 || speed > 10)
                throw new ArgumentOutOfRangeException(nameof(speed));
            token.ThrowIfCancellationRequested(); CheckReentry(); this.speed = speed;
        } finally { busy = false; }
    }
    public Vector2 ManualInput => Read(() => manualInput);
    private bool HasManual => manualInput != Vector2.Zero;
    public void SetManualInput(Vector2 nativeXZDirection, long exactCurrentClock, CancellationToken token = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); token.ThrowIfCancellationRequested();
            if (state.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable || !Current())
                throw new InvalidOperationException("Point target incarnation unavailable.");
            if (exactCurrentClock != state.DriverResult.TimestampMicroseconds || state.AttemptedUpdates >= maximumUpdates)
                throw new ArgumentOutOfRangeException(nameof(exactCurrentClock));
            if (!float.IsFinite(nativeXZDirection.X) || !float.IsFinite(nativeXZDirection.Y) ||
                Math.Abs(nativeXZDirection.X) > 1 || Math.Abs(nativeXZDirection.Y) > 1)
                throw new ArgumentOutOfRangeException(nameof(nativeXZDirection));
            var next = state with { Target = null, Mode = paused ? SceneWorldTerrainPointTargetMode.Paused :
                nativeXZDirection == Vector2.Zero ? SceneWorldTerrainPointTargetMode.Idle : SceneWorldTerrainPointTargetMode.Manual };
            token.ThrowIfCancellationRequested(); CheckReentry();
            manualInput = nativeXZDirection; target = null; state = next;
        } finally { busy = false; }
    }
    public void SetPaused(bool value, long timestampMicroseconds, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            if (state.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable || !Current())
                throw new InvalidOperationException("Point target incarnation unavailable.");
            if (timestampMicroseconds != state.DriverResult.TimestampMicroseconds || state.AttemptedUpdates >= maximumUpdates)
                throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
            cancellationToken.ThrowIfCancellationRequested(); CheckReentry(); paused = value;
            var pose = state.DriverResult.Transform.Translation;
            state = state with { Mode = paused ? SceneWorldTerrainPointTargetMode.Paused : target is null ? HasManual ? SceneWorldTerrainPointTargetMode.Manual : SceneWorldTerrainPointTargetMode.Idle :
                Distance(pose.X,pose.Z,target.X,target.Z) <= radius ? SceneWorldTerrainPointTargetMode.Arrived : SceneWorldTerrainPointTargetMode.Moving };
        } finally { busy = false; }
    }
    public void ClearTarget(long exactCurrentClock, CancellationToken token = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); token.ThrowIfCancellationRequested();
            if (state.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable || !Current())
                throw new InvalidOperationException("Point target incarnation unavailable.");
            if (exactCurrentClock != state.DriverResult.TimestampMicroseconds || state.AttemptedUpdates >= maximumUpdates)
                throw new ArgumentOutOfRangeException(nameof(exactCurrentClock));
            token.ThrowIfCancellationRequested(); CheckReentry();
            if (target is null && !HasManual) return;
            var cleared = state with { Target = null, Mode = paused ? SceneWorldTerrainPointTargetMode.Paused : SceneWorldTerrainPointTargetMode.Idle };
            token.ThrowIfCancellationRequested(); CheckReentry(); manualInput = Vector2.Zero; target = null; state = cleared;
        } finally { busy = false; }
    }
    private bool Current()
    {
        if (!motions!.IsBoundTo(world!) || world!.Generation != generation ||
            world.State is not (ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady)) return false;
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Incarnation == incarnation);
        return snapshot is { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } && world.IsCurrent(snapshot.Replication);
    }
    private static double Distance(double x, double z, double tx, double tz) => Math.Sqrt((tx-x)*(tx-x)+(tz-z)*(tz-z));
    private void TargetAdmission(long timestampMicroseconds, CancellationToken cancellation)
    {
        ObjectDisposedException.ThrowIf(disposed, this); cancellation.ThrowIfCancellationRequested();
        if (state.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable || !Current())
            throw new InvalidOperationException("Point target incarnation unavailable.");
        if (timestampMicroseconds != state.DriverResult.TimestampMicroseconds || state.SuccessfulTargets >= maximumTargets)
            throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
        cancellation.ThrowIfCancellationRequested(); CheckReentry();
    }
    public void ValidateTargetAdmission(long timestampMicroseconds, CancellationToken cancellation = default)
    { Enter(); try { TargetAdmission(timestampMicroseconds, cancellation); } finally { busy = false; } }
    public SceneWorldTerrainPointTargetReceipt SubmitTarget(TerrainModernPatchSetRayHit hit, long timestampMicroseconds,
        CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); ArgumentNullException.ThrowIfNull(hit);
            TargetAdmission(timestampMicroseconds, cancellationToken);
            if (!ReferenceEquals(hit.Source, query!.Source) || !query.Source.Children.Any(c => ReferenceEquals(c, hit.Membership)) ||
                !double.IsFinite(hit.Position.X) || !double.IsFinite(hit.Position.Y) || !double.IsFinite(hit.Position.Z) ||
                Math.Abs(hit.Position.X) > positionCap || Math.Abs(hit.Position.Y) > positionCap || Math.Abs(hit.Position.Z) > positionCap)
                throw new InvalidDataException("Actual retained source/member and bounded hit position required.");
            var indices = hit.Geometry.CopyIndices(); var positions = hit.Geometry.CopyPositions();
            if (hit.PatternIndex != hit.Geometry.PatternIndex || hit.TriangleIndex < 0 || hit.TriangleIndex >= indices.Length/3 ||
                !double.IsFinite(hit.T) || hit.T < 0 || !double.IsFinite(hit.WeightA) || !double.IsFinite(hit.WeightB) || !double.IsFinite(hit.WeightC) ||
                hit.WeightA < 0 || hit.WeightA > 1 || hit.WeightB < 0 || hit.WeightB > 1 || hit.WeightC < 0 || hit.WeightC > 1 ||
                Math.Abs(hit.WeightA+hit.WeightB+hit.WeightC-1) > 1e-12)
                throw new InvalidDataException("Actual hit topology and finite barycentric coordinates required.");
            int triangle = hit.TriangleIndex*3;
            if (indices[triangle] != hit.VertexA || indices[triangle+1] != hit.VertexB || indices[triangle+2] != hit.VertexC)
                throw new InvalidDataException("Actual retained triangle vertex identity required.");
            var a = positions[hit.VertexA]; var b = positions[hit.VertexB]; var c = positions[hit.VertexC];
            static bool Near(double expected, double actual) => Math.Abs(expected-actual) <= 1e-9+1e-12*Math.Max(Math.Abs(expected),Math.Abs(actual));
            if (!Near(hit.WeightA*a.X+hit.WeightB*b.X+hit.WeightC*c.X,hit.Position.X) ||
                !Near(hit.WeightA*a.Y+hit.WeightB*b.Y+hit.WeightC*c.Y,hit.Position.Y) ||
                !Near(hit.WeightA*a.Z+hit.WeightB*b.Z+hit.WeightC*c.Z,hit.Position.Z))
                throw new InvalidDataException("Hit position must lie on its actual retained triangle.");
            float x = (float)hit.Position.X, z = (float)hit.Position.Z;
            var eligible = query.Source.Query(x,z);
            if (!float.IsFinite(x) || !float.IsFinite(z) || Math.Abs(x) > positionCap || Math.Abs(z) > positionCap || eligible is null ||
                !double.IsFinite(eligible.Hit.Height+footOffset) || Math.Abs(eligible.Hit.Height+footOffset) > positionCap)
                throw new InvalidDataException("Cast native target must have eligible actual supplied surface.");
            cancellationToken.ThrowIfCancellationRequested();
            var receipt = new SceneWorldTerrainPointTargetReceipt(query, hit, x, z, timestampMicroseconds, checked(state.SuccessfulTargets + 1));
            cancellationToken.ThrowIfCancellationRequested(); CheckReentry();
            var pose = driver!.State.Transform.Translation;
            CheckReentry();
            manualInput = Vector2.Zero; target = receipt; state = state with { Target = receipt, SuccessfulTargets = receipt.Revision,
                Mode = paused ? SceneWorldTerrainPointTargetMode.Paused : Distance(pose.X,pose.Z,x,z) <= radius ? SceneWorldTerrainPointTargetMode.Arrived : SceneWorldTerrainPointTargetMode.Moving };
            return receipt;
        } finally { busy = false; }
    }
    private Vector3 Velocity(Vector3 pose)
    {
        if (paused) return Vector3.Zero;
        if (HasManual) return ManualVelocity();
        if (target is null) return Vector3.Zero;
        double dx = (double)target.X-pose.X, dz = (double)target.Z-pose.Z, distance = Distance(pose.X,pose.Z,target.X,target.Z);
        if (distance <= radius) return Vector3.Zero;
        float seconds = (float)(stepMicroseconds / 1_000_000.0);
        double travel = Math.Min((double)speed*seconds, distance-radius*.5) * (1.0-1.0/1048576);
        var velocity = new Vector3((float)(dx/distance*travel/seconds),0,(float)(dz/distance*travel/seconds));
        // Check the actual binary32 candidate projection; bounded halvings prevent rounding past the target plane.
        for (int attempt=0; attempt<8; attempt++)
        {
            var candidate = pose + velocity*seconds;
            double projection = ((double)candidate.X-pose.X)*dx + ((double)candidate.Z-pose.Z)*dz;
            if (projection >= 0 && projection <= distance*distance) return velocity;
            velocity *= .5f;
        }
        return Vector3.Zero;
    }
    private void Fault(string why) => state = state with { Mode = SceneWorldTerrainPointTargetMode.Faulted, FaultType = why };
    private Vector3 ManualVelocity()
    {
        double x = manualInput.X, z = manualInput.Y, length = Math.Sqrt(x*x+z*z);
        double scale = length > 1 ? speed/length : speed;
        var velocity = new Vector3((float)(x*scale),0,(float)(z*scale));
        static float TowardZero(float value) => value > 0 ? MathF.BitDecrement(value) : value < 0 ? MathF.BitIncrement(value) : value;
        for (int attempt = 0; attempt < 4 && velocity.Length() > speed; attempt++)
            velocity = new(TowardZero(velocity.X),0,TowardZero(velocity.Z));
        if (velocity.Length() > speed) throw new InvalidDataException("Manual binary32 magnitude admission.");
        return velocity;
    }
    public SceneWorldTerrainPointTargetState AdvanceNext(long submitTimestampMicroseconds, long advanceTimestampMicroseconds,
        CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            if (state.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable) return state;
            long clock = state.DriverResult.TimestampMicroseconds;
            if (state.AttemptedUpdates >= maximumUpdates || clock > long.MaxValue-stepMicroseconds ||
                submitTimestampMicroseconds != clock || advanceTimestampMicroseconds != clock+stepMicroseconds)
                throw new ArgumentOutOfRangeException(nameof(advanceTimestampMicroseconds));
            if (!Current()) return state = state with { Mode = SceneWorldTerrainPointTargetMode.Unavailable };
            var selected = world!.Snapshots().Single(s => s.Object.Incarnation == incarnation);
            var motion = motions!.Find(selected); var poseBefore = driver!.State.Transform.Translation;
            if (motion is { Authoritative: true } && !ReferenceEquals(motion,lastPublication) && motion.Revision > observedAuthoritativeRevision)
            {
                poseBefore = motion.Transform.Translation;
                if (!float.IsFinite(poseBefore.X) || !float.IsFinite(poseBefore.Y) || !float.IsFinite(poseBefore.Z) ||
                    Math.Abs(poseBefore.X) > positionCap || Math.Abs(poseBefore.Y) > positionCap || Math.Abs(poseBefore.Z) > positionCap ||
                    !float.IsFinite(motion.Speed) || motion.Speed < 0 || motion.Speed > 10)
                    throw new InvalidDataException("Authoritative correction pose/speed admission before Submit.");
                if (destinationSurface is not null && destinationSurface.Query(poseBefore.X, poseBefore.Z, cancellationToken) is null)
                    throw new InvalidDataException("Authoritative correction has no retained current ground.");
            }
            var velocity = Velocity(poseBefore); cancellationToken.ThrowIfCancellationRequested(); CheckReentry();
            TerrainOfflineDestinationReceipt? destination = null;
            if (destinationSurface is not null)
            {
                // Match the frozen planar tick's binary32 multiplication and addition exactly.
                var proposed = poseBefore + velocity * (stepMicroseconds / 1_000_000f);
                if (!float.IsFinite(proposed.X) || !float.IsFinite(proposed.Y) || !float.IsFinite(proposed.Z) ||
                    Math.Abs(proposed.X) > positionCap || Math.Abs(proposed.Y) > positionCap || Math.Abs(proposed.Z) > positionCap)
                    throw new InvalidDataException("Destination must preserve the frozen finite position cap.");
                bool held = destinationSurface.Query(proposed.X, proposed.Z, cancellationToken) is null;
                if (held) velocity = Vector3.Zero;
                destination = new(proposed, velocity, held);
                cancellationToken.ThrowIfCancellationRequested(); CheckReentry();
            }
            lastDestinationReceipt = destination;
            state = state with { AttemptedUpdates = checked(state.AttemptedUpdates+1) };
            try
            {
                driver!.Submit(clock,new(velocity,null)); cancellationToken.ThrowIfCancellationRequested(); CheckReentry();
                var actual = driver.Advance(advanceTimestampMicroseconds,true); state = state with { DriverResult = actual };
                if (!actual.Active || actual.FaultType is not null || actual.StepsThisUpdate != 1 || actual.TimestampMicroseconds != advanceTimestampMicroseconds ||
                    actual.DroppedMicroseconds != 0 || actual.SuspendedMicroseconds != 0)
                { Fault(actual.FaultType ?? "UnhealthyStep"); return state; }
                cancellationToken.ThrowIfCancellationRequested(); CheckReentry();
                if (!Current()) { Fault("ChangedAfterStep"); return state; }
                lastPublication = motions.Find(selected);
                if (motion is { Authoritative: true }) observedAuthoritativeRevision = Math.Max(observedAuthoritativeRevision,motion.Revision);
                CheckReentry();
                var pose = actual.Transform.Translation;
                state = state with { CompletedUpdates = checked(state.CompletedUpdates+1), Mode = paused ? SceneWorldTerrainPointTargetMode.Paused : target is null ? HasManual ? SceneWorldTerrainPointTargetMode.Manual : SceneWorldTerrainPointTargetMode.Idle :
                    Distance(pose.X,pose.Z,target.X,target.Z) <= radius ? SceneWorldTerrainPointTargetMode.Arrived : SceneWorldTerrainPointTargetMode.Moving };
                return state;
            }
            catch (OperationCanceledException) { Fault(nameof(OperationCanceledException)); throw; }
            catch (Exception error) { Fault(error.GetType().Name); return state; }
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldTerrainPointTargetReceipt receipt)
    { Enter(); try { ArgumentNullException.ThrowIfNull(receipt); return !disposed && ReferenceEquals(receipt,target) && Current(); } finally { busy = false; } }
    public void Retire() => Dispose();
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return; disposed = true; paused = false; manualInput = Vector2.Zero;
            try { driver?.Dispose(); CheckReentry(); } finally
            { driver = null; world = null; motions = null; query = null; destinationSurface = null; target = null; lastPublication = null; state = state with { Mode = SceneWorldTerrainPointTargetMode.Disposed, Target = null }; }
        } finally { busy = false; }
    }
}
