using System.Numerics;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Explicit modern native-world bounds. No unit, cadence or engine reflection is inferred.</summary>
public sealed record SceneWorldClientRootMotionPolicy(float MaximumPositionComponent, float MaximumSpeed);
/// <summary>Application is exclusively caller-owned; SourceMotion is historical issued identity.</summary>
public sealed record SceneWorldClientRootMotionPacket(ObjectControllerTransformMessage Raw, byte[] Application,
    SceneWorldObjectSnapshot SourceSnapshot, SceneWorldObjectMotionSnapshot SourceMotion);

public sealed class SceneWorldClientRootMotionBuilder : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private readonly SceneWorldClientRootMotionPolicy policy;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, disposed;
    public SceneWorldClientRootMotionBuilder(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        SceneWorldClientRootMotionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(policy);
        if (!float.IsFinite(policy.MaximumPositionComponent) || policy.MaximumPositionComponent <= 0
            || !float.IsFinite(policy.MaximumSpeed) || policy.MaximumSpeed < 0) throw new ArgumentOutOfRangeException(nameof(policy));
        _ = world.Snapshots();
        if (!motions.IsBoundTo(world)) throw new ArgumentException("Open exact-world motion store required.", nameof(motions));
        this.world = world; this.motions = motions; this.policy = policy with { };
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Client motion owner thread required.");
        if (busy) throw new InvalidOperationException("Client motion reentrancy.");
        busy = true;
    }
    private void RequireCurrent(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion)
    {
        if (!motions!.IsBoundTo(world!) || world!.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready))
            throw new InvalidDataException("Active world and open bound store required.");
        var latest = world.Snapshots().SingleOrDefault(s => s.Object.Incarnation == snapshot.Object.Incarnation);
        if (latest is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } || latest.Object.ContainerId != 0
            || !ReferenceEquals(latest.Object, snapshot.Object) || !ReferenceEquals(latest.Placement, snapshot.Placement)
            || !latest.Envelopes.SequenceEqual(snapshot.Envelopes) || snapshot.Replication is null || !world.IsCurrent(snapshot.Replication)
            || latest.Replication.Revision != snapshot.Replication.Revision || motion.Authoritative || motion.MovementCounter is not null
            || motion.Incarnation != latest.Object.Incarnation || !ReferenceEquals(motions.Find(snapshot), motion) || !motions.IsCurrent(motion))
            throw new InvalidDataException("Exact current finalized uncontained typed LOCAL motion required.");
    }
    public SceneWorldClientRootMotionPacket Build(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion,
        uint flags, uint movementCounter, uint timestamp, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(motion);
            if (snapshot.Object is null || snapshot.Placement is null || snapshot.Envelopes is null) throw new InvalidDataException("Malformed snapshot.");
            RequireCurrent(snapshot, motion);
            long generation = world!.Generation;
            var batch = world.DrainChanges(generation)!;
            Validate(motion.Transform, motion.Speed);
            var quaternion = QuaternionFromRows(motion.Transform);
            var raw = ObjectControllerTransformMessage.ClientToServer(ObjectControllerTransformKind.Root, flags,
                snapshot.Object.Handle.ObjectId, timestamp, movementCounter, null, quaternion, motion.Transform.Translation, motion.Speed);
            byte[] encoded = ObjectControllerTransformCodec.Encode(raw);
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != world.Generation || !world.IsCurrent(batch)) throw new InvalidDataException("World changed during construction.");
            RequireCurrent(snapshot, motion);
            return new(raw, encoded, snapshot, motion);
        } finally { busy = false; }
    }
    private void Validate(AppearanceTransform transform, float speed)
    {
        static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
        static double Dot(Vector3 a, Vector3 b) => (double)a.X*b.X+(double)a.Y*b.Y+(double)a.Z*b.Z;
        var x=transform.RowX;var y=transform.RowY;var z=transform.RowZ;var p=transform.Translation;
        double det=(double)x.X*((double)y.Y*z.Z-(double)y.Z*z.Y)-(double)x.Y*((double)y.X*z.Z-(double)y.Z*z.X)+(double)x.Z*((double)y.X*z.Y-(double)y.Y*z.X);
        const double tolerance=1e-4;
        if(!Finite(x)||!Finite(y)||!Finite(z)||!Finite(p)||!float.IsFinite(speed)||speed<0||speed>policy.MaximumSpeed
            ||Math.Abs(p.X)>policy.MaximumPositionComponent||Math.Abs(p.Y)>policy.MaximumPositionComponent||Math.Abs(p.Z)>policy.MaximumPositionComponent
            ||Math.Abs(Dot(x,x)-1)>tolerance||Math.Abs(Dot(y,y)-1)>tolerance||Math.Abs(Dot(z,z)-1)>tolerance
            ||Math.Abs(Dot(x,y))>tolerance||Math.Abs(Dot(x,z))>tolerance||Math.Abs(Dot(y,z))>tolerance||Math.Abs(det-1)>tolerance)
            throw new InvalidDataException("Finite bounded proper rigid native pose and nonnegative speed required.");
    }
    private static Vector4 QuaternionFromRows(AppearanceTransform m)
    {
        double x,y,z,w,trace=(double)m.RowX.X+m.RowY.Y+m.RowZ.Z;
        if(trace>0)
        { double s=2*Math.Sqrt(trace+1);w=s/4;x=((double)m.RowZ.Y-m.RowY.Z)/s;y=((double)m.RowX.Z-m.RowZ.X)/s;z=((double)m.RowY.X-m.RowX.Y)/s; }
        else if(m.RowX.X>=m.RowY.Y&&m.RowX.X>=m.RowZ.Z)
        { double s=2*Math.Sqrt(1+(double)m.RowX.X-m.RowY.Y-m.RowZ.Z);x=s/4;y=((double)m.RowX.Y+m.RowY.X)/s;z=((double)m.RowX.Z+m.RowZ.X)/s;w=((double)m.RowZ.Y-m.RowY.Z)/s; }
        else if(m.RowY.Y>=m.RowZ.Z)
        { double s=2*Math.Sqrt(1+(double)m.RowY.Y-m.RowX.X-m.RowZ.Z);y=s/4;x=((double)m.RowX.Y+m.RowY.X)/s;z=((double)m.RowY.Z+m.RowZ.Y)/s;w=((double)m.RowX.Z-m.RowZ.X)/s; }
        else
        { double s=2*Math.Sqrt(1+(double)m.RowZ.Z-m.RowX.X-m.RowY.Y);z=s/4;x=((double)m.RowX.Z+m.RowZ.X)/s;y=((double)m.RowY.Z+m.RowZ.Y)/s;w=((double)m.RowY.X-m.RowX.Y)/s; }
        double norm=Math.Sqrt(x*x+y*y+z*z+w*w);
        if(!double.IsFinite(norm)||norm==0)throw new InvalidDataException("Quaternion conversion failed.");
        return new((float)(x/norm),(float)(y/norm),(float)(z/norm),(float)(w/norm));
    }
    public void Dispose()
    { Enter();try{if(disposed)return;disposed=true;world=null;motions=null;}finally{busy=false;} }
}
