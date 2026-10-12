using System.Numerics;
using Stardust.Core.Geometry;

namespace Stardust.Core.Session;

/// <summary>Modern offline controller: camera-relative bounded endpoint motion on the retained displayed triangles.</summary>
public sealed class OfflineTerrainRoaming
{
    private readonly TerrainModernPatchSetSurfaceQuery surface;
    private readonly Vector2 start;
    private readonly Func<Vector3,Vector3,bool>? blocked;
    private double x,z;
    public Vector3 Position { get; private set; }
    public float Heading { get; private set; }
    public float Speed { get; private set; }
    public int EdgeHolds { get; private set; }
    public int Updates { get; private set; }
    public int ObstacleHolds {get;private set;}
    public double LastHorizontalDistance {get;private set;}
    public OfflineTerrainRoaming(TerrainModernPatchSetSurfaceQuery surface,Vector2 start=default,Func<Vector3,Vector3,bool>? blocked=null)
    {
        this.surface=surface??throw new ArgumentNullException(nameof(surface));this.start=start;this.blocked=blocked; Reset();
    }
    public void Reset()
    {
        var hit=surface.Query(start.X,start.Y)??throw new InvalidDataException("Starting point has no original-resource terrain.");
        x=start.X;z=start.Y;Position=new(start.X,(float)hit.Hit.Height,start.Y); Heading=0; Speed=0;EdgeHolds=0;ObstacleHolds=0;LastHorizontalDistance=0;
    }
    public void Advance(Vector2 keys,Vector3 cameraForward,bool run,bool active,double seconds)
    {
        if(!double.IsFinite(seconds)||seconds<0||seconds>.1)throw new ArgumentOutOfRangeException(nameof(seconds));
        var movement=TerrainCameraRelativeMovement.Project(keys,cameraForward);
        Speed=active&&movement.LengthSquared()>0 ? run?6:3 : 0;
        LastHorizontalDistance=0;double nextX=x+movement.X*Speed*seconds,nextZ=z+movement.Y*Speed*seconds;
        var candidate=new Vector3((float)nextX,Position.Y,(float)nextZ);
        var hit=surface.Query(candidate.X,candidate.Z);
        if(hit is null){if(Speed>0)EdgeHolds++;Speed=0;}
        else {candidate.Y=(float)hit.Hit.Height;if(Speed>0&&blocked?.Invoke(Position,candidate)==true){ObstacleHolds++;Speed=0;}else{LastHorizontalDistance=Math.Sqrt((nextX-x)*(nextX-x)+(nextZ-z)*(nextZ-z));x=nextX;z=nextZ;Position=candidate;if(Speed>0)Heading=MathF.Atan2(movement.X,movement.Y);}}
        Updates++;
    }
}

