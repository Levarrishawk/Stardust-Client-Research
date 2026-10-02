using System.Numerics;

namespace Stardust.Core.Assets;

public enum ChaseCameraObstacle{None,RoomTriangle,DoorBox}
public sealed record ChaseCameraResult(Vector3 Target,Vector3 Desired,Vector3 Position,float RequestedDistance,float Distance,
    ChaseCameraObstacle Obstacle,int? ObstacleIndex,bool InitialOverlap,int CandidateTriangles);

/// <summary>Explicit modern camera sphere boom. Retract before static room triangles/live door boxes; never depenetrate or move an actor.</summary>
public sealed class ChaseCameraObstruction
{
    private readonly StaticTriangleSphereCollider rooms;
    public float Radius{get;}
    public float Padding{get;}
    public int RoomTriangles=>rooms.TriangleCount;
    public ChaseCameraObstruction(IReadOnlyList<CollisionTriangle> localRoomTriangles,float radius,float padding,int maximumTriangles=65536)
    {
        if(!float.IsFinite(radius)||radius<=0||radius>1||!float.IsFinite(padding)||padding<0||padding>1)throw new InvalidDataException("Bounded camera sphere radius/padding required.");
        rooms=new(localRoomTriangles,maximumTriangles);Radius=radius;Padding=padding;
    }
    public ChaseCameraResult Query(Vector3 target,Vector3 lookForward,float distance,AppearanceTransform owner,IReadOnlyList<PortalBarrierCollider> worldDoors)
    {
        ArgumentNullException.ThrowIfNull(worldDoors);PortalBarrierCollider.ValidatePose(owner);
        if(!Finite(target)||!Finite(lookForward)||lookForward.LengthSquared()<1e-12f||!float.IsFinite(distance)||distance<0||distance>100||worldDoors.Count>32||worldDoors.Any(b=>b is null))throw new InvalidDataException("Bounded camera target/direction/zoom/door snapshot required.");
        var direction=Vector3.Normalize(lookForward);var desired=target-direction*distance;if(!Finite(desired))throw new InvalidDataException("Camera endpoint bounds.");
        Vector3 Local(Vector3 point){var p=point-owner.Translation;return new(owner.RowX.X*p.X+owner.RowY.X*p.Y+owner.RowZ.X*p.Z,owner.RowX.Y*p.X+owner.RowY.Y*p.Y+owner.RowZ.Y*p.Z,owner.RowX.Z*p.X+owner.RowY.Z*p.Y+owner.RowZ.Z*p.Z);}
        var room=rooms.SweepWithStatistics(Local(target),Local(desired),Radius);double parameter=room.Contact.Parameter;
        bool overlap=room.Contact.Status==TriangleSphereContactStatus.InitialOverlap;var obstacle=room.Contact.Status==TriangleSphereContactStatus.Clear?ChaseCameraObstacle.None:ChaseCameraObstacle.RoomTriangle;int? index=room.Contact.TriangleId;
        for(int i=0;i<worldDoors.Count;i++)
        {
            var door=new SolidBoxSphereCollider(worldDoors[i]).Sweep(target,desired,Radius);
            if(door.Status!=PortalBarrierContactStatus.Clear&&(obstacle==ChaseCameraObstacle.None||door.Parameter<parameter))
            {parameter=door.Parameter;obstacle=ChaseCameraObstacle.DoorBox;index=i;overlap=door.Status==PortalBarrierContactStatus.InitialOverlap;}
        }
        float resolved=obstacle==ChaseCameraObstacle.None?distance:Math.Max(0,(float)(parameter*distance)-Padding);
        var position=target-direction*resolved;
        return new(target,desired,position,distance,resolved,obstacle,index,overlap,room.Statistics.CandidateTriangles);
    }
    public static ChaseCameraObstruction FromInterior(InteriorSceneTriangleGuard guard,float radius=.12f,float padding=.025f)
    {
        ArgumentNullException.ThrowIfNull(guard);var triangles=new List<CollisionTriangle>();
        foreach(var cell in guard.Geometry.OrderBy(p=>p.Key))foreach(var t in cell.Value.Triangles)triangles.Add(new(triangles.Count,t.A,t.B,t.C));
        return new(triangles,radius,padding);
    }
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z)&&Math.Abs(value.X)<=1e6&&Math.Abs(value.Y)<=1e6&&Math.Abs(value.Z)<=1e6;
}
