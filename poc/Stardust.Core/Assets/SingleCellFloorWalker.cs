using System.Numerics;
namespace Stardust.Core.Assets;

public enum FloorWalkStatus { Accepted, BlockedEdge, OpenBoundary, PortalBoundary, UnsupportedEdgePolicy, UnsupportedSurface, InvalidStart, InvalidTopology, AmbiguousVertex, TraversalLimit, UnrepresentableContact }
public sealed record FloorWalkResult(FloorWalkStatus Status,int TriangleIndex,Vector3 Position,double Parameter,int EdgeIndex,int PortalId,int Crossings);
public readonly record struct FloorPathPoint(double X,double Y,double Z)
{
    public static implicit operator FloorPathPoint(Vector3 point)=>new(point.X,point.Y,point.Z);
    public Vector3 ToVector3()=>new((float)X,(float)Y,(float)Z);
}
public sealed record FloorWalkSegment(int TriangleIndex,double StartParameter,double EndParameter,FloorPathPoint Start,FloorPathPoint End);
public sealed record FloorWalkPath(FloorWalkResult Result,IReadOnlyList<FloorWalkSegment> Segments);

/// <summary>Owned horizontal single-cell inspection walk. Numerical policy is modern; cross-floor collision and cell transitions are deliberately unresolved.</summary>
public sealed class SingleCellFloorWalker
{
    private const double Tolerance=1e-8;
    private readonly Vector3[] vertices;
    private readonly FloorTriangle[] triangles;
    public SingleCellFloorWalker(FloorMeshDescription floor)
    {
        ArgumentNullException.ThrowIfNull(floor);
        if(floor.Vertices.Count>1_000_000 || floor.Triangles.Count>1_000_000)throw new InvalidDataException("Floor walk resource budget.");
        vertices=floor.Vertices.ToArray();
        if(vertices.Any(v=>!Finite(v)))throw new InvalidDataException("Nonfinite floor vertex.");
        triangles=floor.Triangles.Select(t=>t with{VertexIndices=t.VertexIndices.ToArray(),Neighbors=t.Neighbors.ToArray(),EdgeFlags=t.EdgeFlags.ToArray(),PortalIds=t.PortalIds.ToArray()}).ToArray();
        foreach(FloorTriangle t in triangles)
            if(t.VertexIndices.Count!=3 || t.Neighbors.Count!=3 || t.EdgeFlags.Count!=3 || t.PortalIds.Count!=3 ||
                t.VertexIndices.Any(i=>i<0 || i>=vertices.Length) || t.Neighbors.Any(i=>i< -1 || i>=triangles.Length))throw new InvalidDataException("Invalid floor walk topology record.");
    }
    public FloorWalkResult Walk(int triangleIndex,Vector3 start,Vector3 desired,int maximumCrossings=4096)
        =>WalkCore(triangleIndex,start,desired,maximumCrossings,null);
    public FloorWalkPath Trace(int triangleIndex,Vector3 start,Vector3 desired,int maximumCrossings=4096)
    {
        List<FloorWalkSegment> segments=new();
        FloorWalkResult result=WalkCore(triangleIndex,start,desired,maximumCrossings,segments.Add);
        if(result.Status==FloorWalkStatus.UnrepresentableContact)segments.Clear();
        return new(result,segments.AsReadOnly());
    }
    private FloorWalkResult WalkCore(int triangleIndex,Vector3 start,Vector3 desired,int maximumCrossings,Action<FloorWalkSegment>? observe)
    {
        if(!Finite(start) || !Finite(desired))throw new InvalidDataException("Nonfinite floor movement.");
        if(maximumCrossings is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumCrossings));
        if(triangleIndex<0 || triangleIndex>=triangles.Length)return new(FloorWalkStatus.InvalidStart,-1,start,0,-1,-1,0);
        int current=triangleIndex,crossings=0;double parameter=0;Vector3 position=start,canonicalStart=start;
        FloorWalkResult Stop(FloorWalkStatus status,int edge=-1,int portal=-1)
        {
            Vector3 valid=position;
            if(edge>=0 && !TryCanonicalContact(current,position,out valid))
                return new(FloorWalkStatus.UnrepresentableContact,triangleIndex,canonicalStart,0,-1,-1,0);
            return new(status,current,edge>=0?valid:position,parameter,edge,portal,crossings);
        }
        if(!Coordinates(current,start,out double[] initial))return Stop(FloorWalkStatus.UnsupportedSurface);
        if(initial.Any(v=>v< -Tolerance))return Stop(FloorWalkStatus.InvalidStart);
        Vector3 projectedStart=Project(current,start,initial);
        if(Math.Abs((double)projectedStart.Y-start.Y)>1e-4)return Stop(FloorWalkStatus.InvalidStart);
        position=canonicalStart=projectedStart;
        HashSet<int> visited=new();
        while(true)
        {
            if(triangles[current].NonSolidByte!=0)return Stop(FloorWalkStatus.UnsupportedSurface);
            if(!visited.Add(current))return Stop(FloorWalkStatus.InvalidTopology);
            if(!Coordinates(current,start,out double[] from) || !Coordinates(current,desired,out double[] to))return Stop(FloorWalkStatus.UnsupportedSurface);
            double segmentParameter=parameter;
            double sx=start.X+((double)desired.X-start.X)*parameter,sz=start.Z+((double)desired.Z-start.Z)*parameter;
            Coordinates(current,sx,sz,out double[] segmentWeights);FloorPathPoint segmentStart=PrecisePoint(current,sx,sz,segmentWeights);
            if(to.All(v=>v>=-Tolerance)){parameter=1;position=Project(current,desired,to);observe?.Invoke(new(current,segmentParameter,parameter,segmentStart,PrecisePoint(current,desired.X,desired.Z,to)));return Stop(FloorWalkStatus.Accepted);}
            double exit=double.PositiveInfinity;int opposite=-1;bool tied=false;
            for(int i=0;i<3;i++)
            {
                double delta=to[i]-from[i];if(delta>=0 || to[i]>=-Tolerance)continue;
                double candidate=-from[i]/delta;if(candidate<parameter-Tolerance || candidate>1+Tolerance)continue;
                if(candidate<exit-Tolerance){exit=candidate;opposite=i;tied=false;}
                else if(Math.Abs(candidate-exit)<=Tolerance)tied=true;
            }
            if(opposite<0)return Stop(FloorWalkStatus.InvalidTopology);
            parameter=Math.Clamp(exit,parameter,1);Vector3 point=new((float)(start.X+((double)desired.X-start.X)*parameter),0,(float)(start.Z+((double)desired.Z-start.Z)*parameter));
            double px=start.X+((double)desired.X-start.X)*parameter,pz=start.Z+((double)desired.Z-start.Z)*parameter;
            Coordinates(current,px,pz,out double[] at);position=Project(current,point,at);
            observe?.Invoke(new(current,segmentParameter,parameter,segmentStart,PrecisePoint(current,px,pz,at)));
            int edge=(opposite+1)%3;
            if(tied)return Stop(FloorWalkStatus.AmbiguousVertex,edge);
            FloorTriangle triangle=triangles[current];int portal=triangle.PortalIds[edge];
            byte flag=triangle.EdgeFlags[edge];
            if(portal==-1 && flag==0)return Stop(FloorWalkStatus.BlockedEdge,edge);
            if(portal==-1 && flag!=1)return Stop(FloorWalkStatus.UnsupportedEdgePolicy,edge);
            int next=triangle.Neighbors[edge];if(next==-1)return Stop(portal==-1?FloorWalkStatus.OpenBoundary:FloorWalkStatus.PortalBoundary,edge,portal);
            int a=triangle.VertexIndices[edge],b=triangle.VertexIndices[(edge+1)%3];
            int reciprocal=-1;
            for(int e=0;e<3;e++)if(triangles[next].VertexIndices[e]==b && triangles[next].VertexIndices[(e+1)%3]==a && triangles[next].Neighbors[e]==current)reciprocal=e;
            if(reciprocal<0 || next==current)return Stop(FloorWalkStatus.InvalidTopology,edge);
            if(!Coordinates(next,px,pz,out double[] target))return Stop(FloorWalkStatus.UnsupportedSurface,edge);
            if(target.Any(v=>v< -Tolerance))return Stop(FloorWalkStatus.InvalidTopology,edge);
            if(triangles[next].NonSolidByte!=0)return Stop(FloorWalkStatus.UnsupportedSurface,edge);
            if(crossings==maximumCrossings)return Stop(FloorWalkStatus.TraversalLimit,edge);
            current=next;crossings++;
        }
    }
    private bool Coordinates(int index,Vector3 p,out double[] weights)
        =>Coordinates(index,p.X,p.Z,out weights);
    private bool Coordinates(int index,double x,double z,out double[] weights)
    {
        FloorTriangle t=triangles[index];Vector3 a=vertices[t.VertexIndices[0]],b=vertices[t.VertexIndices[1]],c=vertices[t.VertexIndices[2]];
        double bx=(double)b.X-a.X,bz=(double)b.Z-a.Z,cx=(double)c.X-a.X,cz=(double)c.Z-a.Z,px=x-a.X,pz=z-a.Z;
        double determinant=bx*cz-bz*cx;
        double by=(double)b.Y-a.Y,cy=(double)c.Y-a.Y,nx=by*cz-bz*cy,nz=bx*cy-by*cx;
        if(determinant==0 || Math.Abs(determinant)<1e-4*Math.Sqrt(nx*nx+determinant*determinant+nz*nz)){weights=new double[3];return false;}
        double v=(px*cz-pz*cx)/determinant,w=(bx*pz-bz*px)/determinant;weights=new[]{1-v-w,v,w};return true;
    }
    private Vector3 Project(int index,Vector3 p,double[] weights)
    {
        FloorTriangle t=triangles[index];double y=0;for(int i=0;i<3;i++)y+=vertices[t.VertexIndices[i]].Y*weights[i];
        return new(p.X,(float)y,p.Z);
    }
    private FloorPathPoint PrecisePoint(int index,double x,double z,double[] weights)
    {
        FloorTriangle t=triangles[index];double y=0;for(int i=0;i<3;i++)y+=vertices[t.VertexIndices[i]].Y*weights[i];
        return new(x,y,z);
    }
    internal bool TryCanonicalContact(int index,Vector3 point,out Vector3 valid,double maximumCorrection=1e-4)
    {
        valid=point;
        if(!Coordinates(index,point,out double[] at))return false;
        if(at.All(v=>v>=-Tolerance))
        {
            // Rounded XZ can be inside a steep triangle while the unrounded-intersection height is no longer on its plane.
            valid=Project(index,point,at);double height=(double)valid.Y-point.Y;
            return height*height<=maximumCorrection*maximumCorrection;
        }
        // A double edge intersection rounded to float may lie outside its own triangle.
        // Move only toward the triangle interior, within a strict geometric budget; never relax start validation.
        FloorTriangle triangle=triangles[index];
        double cx=triangle.VertexIndices.Sum(i=>(double)vertices[i].X)/3,cz=triangle.VertexIndices.Sum(i=>(double)vertices[i].Z)/3;
        double dx=cx-point.X,dz=cz-point.Z,length=Math.Sqrt(dx*dx+dz*dz);if(length==0)return false;
        List<(double X,double Z)> directions=new();
        for(int i=0;i<3;i++)
        {
            if(at[i]>=-Tolerance)continue;
            Vector3 a=vertices[triangle.VertexIndices[(i+1)%3]],b=vertices[triangle.VertexIndices[(i+2)%3]],inside=vertices[triangle.VertexIndices[i]];
            double nx=-((double)b.Z-a.Z),nz=(double)b.X-a.X,norm=Math.Sqrt(nx*nx+nz*nz);
            if(norm==0)continue;
            double sign=(inside.X-a.X)*nx+(inside.Z-a.Z)*nz;if(sign<0){nx=-nx;nz=-nz;}
            directions.Add((nx/norm,nz/norm));
        }
        directions.Add((dx/length,dz/length));
        foreach(var direction in directions)
        foreach(double distance in new[]{1e-6,2e-6,5e-6,1e-5,2e-5,5e-5,1e-4})
        {
            Vector3 candidate=new((float)(point.X+direction.X*distance),point.Y,(float)(point.Z+direction.Z*distance));
            if(!Coordinates(index,candidate,out double[] weights) || weights.Any(v=>v< -Tolerance))continue;
            candidate=Project(index,candidate,weights);
            double x=(double)candidate.X-point.X,y=(double)candidate.Y-point.Y,z=(double)candidate.Z-point.Z;
            if(x*x+y*y+z*z>maximumCorrection*maximumCorrection)continue;
            valid=candidate;return true;
        }
        return false;
    }
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
