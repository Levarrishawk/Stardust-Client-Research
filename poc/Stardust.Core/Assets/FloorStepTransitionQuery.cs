using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorStepPolicy(double MaximumHeight=.2,double MaximumHorizontalWidth=.001,int MaximumStripTriangles=8,bool UseLocalRayCoverage=false)
{
    internal void Validate()
    {
        if(!double.IsFinite(MaximumHeight)||MaximumHeight<=0||MaximumHeight>1 ||
            !double.IsFinite(MaximumHorizontalWidth)||MaximumHorizontalWidth<0||MaximumHorizontalWidth>.01 || MaximumStripTriangles is <1 or >64)
            throw new InvalidDataException("Invalid bounded floor step policy.");
    }
}
public enum FloorStepStatus { Accepted, NotVerticalBoundary, HeightLimit, StripLimit, InvalidTopology, NoLanding, AmbiguousLanding, NonSolid, PortalBoundary }
public sealed record FloorStepTransition(FloorStepStatus Status,int TargetTriangle,Vector3 Landing,FloorPathPoint SourceContact,FloorPathPoint TargetContact,IReadOnlyList<int> StripTriangles);

/// <summary>Owned modern step proposal through reciprocal solid near-vertical floor adjacency. Does not move an actor or bypass box collision.</summary>
public sealed class FloorStepTransitionQuery
{
    private readonly Vector3[] vertices;
    private readonly FloorTriangle[] triangles;
    private readonly SingleCellFloorWalker walker;
    public FloorStepTransitionQuery(FloorMeshDescription floor)
    {
        ArgumentNullException.ThrowIfNull(floor);walker=new(floor);
        vertices=floor.Vertices.ToArray();triangles=floor.Triangles.Select(t=>t with{VertexIndices=t.VertexIndices.ToArray(),Neighbors=t.Neighbors.ToArray(),EdgeFlags=t.EdgeFlags.ToArray(),PortalIds=t.PortalIds.ToArray()}).ToArray();
    }
    public FloorStepTransition Probe(FloorWalkPath path,Vector3 desired,FloorStepPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(path);ArgumentNullException.ThrowIfNull(policy);
        policy.Validate();
        if(!float.IsFinite(desired.X)||!float.IsFinite(desired.Y)||!float.IsFinite(desired.Z))
            throw new InvalidDataException("Invalid bounded floor step policy/input.");
        var stop=path.Result;FloorPathPoint contact=path.Segments.Count>0?path.Segments[^1].End:stop.Position;List<int> strip=new();
        if(path.Segments.Count>4097 || !double.IsFinite(contact.X)||!double.IsFinite(contact.Y)||!double.IsFinite(contact.Z) ||
            !double.IsFinite(stop.Parameter)||stop.Parameter is <0 or >1)
            throw new InvalidDataException("Invalid floor step prefix.");
        FloorStepTransition Reject(FloorStepStatus status)=>new(status,stop.TriangleIndex,stop.Position,contact,contact,strip.ToArray());
        if(stop.Status==FloorWalkStatus.InvalidTopology)return Reject(FloorStepStatus.InvalidTopology);
        if(stop.Status!=FloorWalkStatus.UnsupportedSurface || stop.TriangleIndex<0 || stop.TriangleIndex>=triangles.Length || stop.EdgeIndex is <0 or >2 || path.Segments.Count==0)
            return Reject(FloorStepStatus.NotVerticalBoundary);
        var source=triangles[stop.TriangleIndex];int first=source.Neighbors[stop.EdgeIndex];
        var last=path.Segments[^1];
        if(last.TriangleIndex!=stop.TriangleIndex || last.EndParameter!=stop.Parameter ||
            !Coordinates(stop.TriangleIndex,contact.X,contact.Z,out var sourceWeights,out double sourceHeight) ||
            sourceWeights.Any(w=>w< -1e-8) || Math.Abs(sourceHeight-contact.Y)>1e-7)
            return Reject(FloorStepStatus.InvalidTopology);
        if(first<0 || Supported(first) || source.EdgeFlags[stop.EdgeIndex]!=1 || source.PortalIds[stop.EdgeIndex]!=-1)return Reject(FloorStepStatus.NotVerticalBoundary);
        if(walker.Walk(stop.TriangleIndex,stop.Position,stop.Position).Status!=FloorWalkStatus.Accepted)return Reject(FloorStepStatus.InvalidTopology);
        Vector3 a=vertices[source.VertexIndices[stop.EdgeIndex]],b=vertices[source.VertexIndices[(stop.EdgeIndex+1)%3]];
        double nx=-((double)b.Z-a.Z),nz=(double)b.X-a.X,length=Math.Sqrt(nx*nx+nz*nz);
        if(length==0)return Reject(FloorStepStatus.InvalidTopology);nx/=length;nz/=length;
        double edgeLength=((double)b.X-a.X)*((double)b.X-a.X)+((double)b.Z-a.Z)*((double)b.Z-a.Z);
        double edgeParameter=((contact.X-a.X)*(b.X-a.X)+(contact.Z-a.Z)*(b.Z-a.Z))/edgeLength;
        if(Math.Abs((contact.X-a.X)*nx+(contact.Z-a.Z)*nz)>1e-7 || edgeParameter is <0 or >1)return Reject(FloorStepStatus.InvalidTopology);
        double dx=desired.X-contact.X,dz=desired.Z-contact.Z,motion=Math.Sqrt(dx*dx+dz*dz);
        if(motion==0)return Reject(FloorStepStatus.NoLanding);
        var queue=new Queue<int>();queue.Enqueue(first);var visited=new HashSet<int>();var candidates=new HashSet<int>();
        while(queue.Count>0)
        {
            int index=queue.Dequeue();if(!visited.Add(index))continue;
            if(visited.Count>policy.MaximumStripTriangles)return Reject(FloorStepStatus.StripLimit);
            var t=triangles[index];if(t.NonSolidByte!=0)return Reject(FloorStepStatus.NonSolid);strip.Add(index);
            foreach(int v in t.VertexIndices)
            {
                var p=vertices[v];if(Math.Abs(p.Y-contact.Y)>policy.MaximumHeight)return Reject(FloorStepStatus.HeightLimit);
                if(!policy.UseLocalRayCoverage&&Math.Abs((p.X-contact.X)*nx+(p.Z-contact.Z)*nz)>policy.MaximumHorizontalWidth)return Reject(FloorStepStatus.StripLimit);
            }
            for(int edge=0;edge<3;edge++)
            {
                int neighbor=t.Neighbors[edge];if(neighbor<0 || t.EdgeFlags[edge]==0)continue;
                if(t.EdgeFlags[edge]!=1)return Reject(FloorStepStatus.InvalidTopology);
                if(t.PortalIds[edge]!=-1)return Reject(FloorStepStatus.PortalBoundary);
                if(!Reciprocal(index,edge,neighbor))return Reject(FloorStepStatus.InvalidTopology);
                if(neighbor==stop.TriangleIndex)continue;
                if(Supported(neighbor))candidates.Add(neighbor);
                else if(!policy.UseLocalRayCoverage||TryRayInterval(neighbor,contact,policy,out _,out _))queue.Enqueue(neighbor);
            }
        }
        if(!Reciprocal(stop.TriangleIndex,stop.EdgeIndex,first))return Reject(FloorStepStatus.InvalidTopology);
        List<(int Triangle,FloorPathPoint Contact,Vector3 Landing)> landings=new();
        foreach(int index in candidates)
        {
            if(triangles[index].NonSolidByte!=0)continue;
            if(!Coordinates(index,contact.X,contact.Z,out var weights,out double height) || weights.Any(w=>w< -1e-8))continue;
            if(Math.Abs(height-contact.Y)>policy.MaximumHeight || Math.Abs(height-contact.Y)<=1e-6)continue;
            if(policy.UseLocalRayCoverage&&!Covered(strip,contact,height,policy))continue;
            if(!Coordinates(index,contact.X+dx/motion*1e-5,contact.Z+dz/motion*1e-5,out var inside,out _) || inside.Any(w=>w< -1e-8))continue;
            FloorPathPoint target=new(contact.X,height,contact.Z);
            if(!walker.TryCanonicalContact(index,target.ToVector3(),out var landing))continue;
            if(walker.Walk(index,landing,landing).Status!=FloorWalkStatus.Accepted)continue;
            landings.Add((index,target,landing));
        }
        if(landings.Count==0)return Reject(FloorStepStatus.NoLanding);
        if(landings.Count!=1)return Reject(FloorStepStatus.AmbiguousLanding);
        var selected=landings[0];return new(FloorStepStatus.Accepted,selected.Triangle,selected.Landing,contact,selected.Contact,strip.ToArray());
    }
    private bool Reciprocal(int index,int edge,int neighbor)
    {
        var t=triangles[index];int a=t.VertexIndices[edge],b=t.VertexIndices[(edge+1)%3];var other=triangles[neighbor];
        return Enumerable.Range(0,3).Any(e=>other.VertexIndices[e]==b&&other.VertexIndices[(e+1)%3]==a&&other.Neighbors[e]==index&&other.EdgeFlags[e]==1&&other.PortalIds[e]==-1);
    }
    private bool Covered(IEnumerable<int> strip,FloorPathPoint contact,double height,FloorStepPolicy policy)
    {
        double begin=Math.Min(contact.Y,height),end=Math.Max(contact.Y,height),reached=begin;
        var intervals=new List<(double Minimum,double Maximum)>();
        foreach(int index in strip)if(TryRayInterval(index,contact,policy,out double lo,out double hi))intervals.Add((lo,hi));
        foreach(var interval in intervals.OrderBy(i=>i.Minimum))
        {
            if(interval.Maximum<reached)continue;if(interval.Minimum>reached+1e-6)return false;
            reached=Math.Max(reached,interval.Maximum);if(reached>=end-1e-6)return true;
        }
        return false;
    }
    private bool TryRayInterval(int index,FloorPathPoint contact,FloorStepPolicy policy,out double minimum,out double maximum)
    {
        var t=triangles[index];var a=vertices[t.VertexIndices[0]];var b=vertices[t.VertexIndices[1]];var c=vertices[t.VertexIndices[2]];
        double[] u={(double)b.X-a.X,(double)b.Y-a.Y,(double)b.Z-a.Z},v={(double)c.X-a.X,(double)c.Y-a.Y,(double)c.Z-a.Z},d={contact.X-a.X,-a.Y,contact.Z-a.Z};
        double Dot(double[] p,double[] q)=>p.Zip(q,(x,y)=>x*y).Sum();
        double uu=Dot(u,u),uv=Dot(u,v),vv=Dot(v,v),det=uu*vv-uv*uv;
        minimum=Math.Max(contact.Y-policy.MaximumHeight,Math.Min(a.Y,Math.Min(b.Y,c.Y)));
        maximum=Math.Min(contact.Y+policy.MaximumHeight,Math.Max(a.Y,Math.Max(b.Y,c.Y)));
        if(!double.IsFinite(det)||det<=0||minimum>maximum)return false;
        double p=(Dot(d,u)*vv-Dot(d,v)*uv)/det,q=(Dot(d,v)*uu-Dot(d,u)*uv)/det,
            py=(u[1]*vv-v[1]*uv)/det,qy=(v[1]*uu-u[1]*uv)/det;
        double lo=minimum,hi=maximum;
        bool AtLeast(double intercept,double slope,double lower)
        {
            if(Math.Abs(slope)<1e-12)return intercept>=lower;
            double bound=(lower-intercept)/slope;if(slope>0)lo=Math.Max(lo,bound);else hi=Math.Min(hi,bound);return lo<=hi;
        }
        if(!AtLeast(p,py,-1e-8)||!AtLeast(q,qy,-1e-8)||!AtLeast(1-p-q,-py-qy,-1e-8))return false;
        double nx=u[1]*v[2]-u[2]*v[1],ny=u[2]*v[0]-u[0]*v[2],nz=u[0]*v[1]-u[1]*v[0],norm=Math.Sqrt(nx*nx+ny*ny+nz*nz);
        double distance=Math.Min(1e-5,policy.MaximumHorizontalWidth)*norm,offset=nx*d[0]+ny*d[1]+nz*d[2];
        if(!AtLeast(offset,ny,-distance)||!AtLeast(-offset,-ny,-distance))return false;
        minimum=lo;maximum=hi;return minimum<=maximum;
    }
    private bool Supported(int index)=>Coordinates(index,vertices[triangles[index].VertexIndices[0]].X,vertices[triangles[index].VertexIndices[0]].Z,out _,out _);
    private bool Coordinates(int index,double x,double z,out double[] weights,out double height)
    {
        var t=triangles[index];var a=vertices[t.VertexIndices[0]];var b=vertices[t.VertexIndices[1]];var c=vertices[t.VertexIndices[2]];
        double bx=(double)b.X-a.X,bz=(double)b.Z-a.Z,cx=(double)c.X-a.X,cz=(double)c.Z-a.Z,by=(double)b.Y-a.Y,cy=(double)c.Y-a.Y;
        double det=bx*cz-bz*cx,nx=by*cz-bz*cy,nz=bx*cy-by*cx;weights=new double[3];height=0;
        if(det==0||Math.Abs(det)<1e-4*Math.Sqrt(nx*nx+det*det+nz*nz))return false;
        double v=((x-a.X)*cz-(z-a.Z)*cx)/det,w=(bx*(z-a.Z)-bz*(x-a.X))/det;weights=new[]{1-v-w,v,w};height=a.Y+v*by+w*cy;return true;
    }
}
