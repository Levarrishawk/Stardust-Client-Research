using System.Numerics;
namespace Stardust.Core.Assets;

public enum FloorWalkStatus { Accepted, BlockedEdge, OpenBoundary, PortalBoundary, UnsupportedEdgePolicy, UnsupportedSurface, InvalidStart, InvalidTopology, AmbiguousVertex, TraversalLimit }
public sealed record FloorWalkResult(FloorWalkStatus Status,int TriangleIndex,Vector3 Position,double Parameter,int EdgeIndex,int PortalId,int Crossings);

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
    {
        if(!Finite(start) || !Finite(desired))throw new InvalidDataException("Nonfinite floor movement.");
        if(maximumCrossings is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumCrossings));
        if(triangleIndex<0 || triangleIndex>=triangles.Length)return new(FloorWalkStatus.InvalidStart,-1,start,0,-1,-1,0);
        int current=triangleIndex,crossings=0;double parameter=0;Vector3 position=start;
        FloorWalkResult Stop(FloorWalkStatus status,int edge=-1,int portal=-1)=>new(status,current,position,parameter,edge,portal,crossings);
        if(!Coordinates(current,start,out double[] initial))return Stop(FloorWalkStatus.UnsupportedSurface);
        if(initial.Any(v=>v< -Tolerance))return Stop(FloorWalkStatus.InvalidStart);
        Vector3 projectedStart=Project(current,start,initial);
        if(Math.Abs((double)projectedStart.Y-start.Y)>1e-4)return Stop(FloorWalkStatus.InvalidStart);
        position=projectedStart;
        HashSet<int> visited=new();
        while(true)
        {
            if(triangles[current].NonSolidByte!=0)return Stop(FloorWalkStatus.UnsupportedSurface);
            if(!visited.Add(current))return Stop(FloorWalkStatus.InvalidTopology);
            if(!Coordinates(current,start,out double[] from) || !Coordinates(current,desired,out double[] to))return Stop(FloorWalkStatus.UnsupportedSurface);
            if(to.All(v=>v>=-Tolerance)){parameter=1;position=Project(current,desired,to);return Stop(FloorWalkStatus.Accepted);}
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
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
