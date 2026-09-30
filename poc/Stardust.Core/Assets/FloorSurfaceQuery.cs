using System.Numerics;
namespace Stardust.Core.Assets;
public sealed record FloorSegmentHit(int TriangleIndex,uint TriangleId,double Parameter,Vector3 Position,Vector3 Barycentric,Vector3 GeometricNormal);

/// <summary>Deterministic modern two-sided segment inspection. Does not implement original floor path walking.</summary>
public sealed class FloorSurfaceQuery
{
    private readonly Vector3[] vertices;
    private readonly Triangle[] triangles;
    private readonly Node[] nodes;
    private readonly int[] order;
    private readonly record struct Triangle(int A,int B,int C,uint Id,bool NonSolid);
    private readonly record struct Point(double X,double Y,double Z)
    {
        public static Point From(Vector3 p)=>new(p.X,p.Y,p.Z);
        public static Point operator -(Point a,Point b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static Point Cross(Point a,Point b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        public static double Dot(Point a,Point b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        public double Axis(int axis)=>axis==0?X:axis==1?Y:Z;
    }
    private readonly record struct Box(Point Minimum,Point Maximum);
    private readonly record struct Node(Box Bounds,int Start,int Count,int Left,int Right);
    public int NodeCount=>nodes.Length;
    public FloorSurfaceQuery(FloorMeshDescription floor)
    {
        ArgumentNullException.ThrowIfNull(floor);
        if(floor.Vertices.Count>1_000_000 || floor.Triangles.Count>1_000_000)throw new InvalidDataException("Floor query geometry budget.");
        vertices=floor.Vertices.ToArray();foreach(var vertex in vertices)Finite(vertex);
        triangles=new Triangle[floor.Triangles.Count];
        for(int i=0;i<triangles.Length;i++)
        {
            FloorTriangle t=floor.Triangles[i];if(t.VertexIndices.Count!=3 || t.VertexIndices.Any(index=>index<0 || index>=vertices.Length))throw new InvalidDataException("Floor query invalid indices.");
            triangles[i]=new(t.VertexIndices[0],t.VertexIndices[1],t.VertexIndices[2],t.Id,t.NonSolidByte!=0);
        }
        order=Enumerable.Range(0,triangles.Length).ToArray();List<Node> build=new();
        Box Bounds(int start,int count)
        {
            Point min=new(double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity),max=new(double.NegativeInfinity,double.NegativeInfinity,double.NegativeInfinity);
            for(int i=start;i<start+count;i++)foreach(int index in new[]{triangles[order[i]].A,triangles[order[i]].B,triangles[order[i]].C})
            {Point p=Point.From(vertices[index]);min=new(Math.Min(min.X,p.X),Math.Min(min.Y,p.Y),Math.Min(min.Z,p.Z));max=new(Math.Max(max.X,p.X),Math.Max(max.Y,p.Y),Math.Max(max.Z,p.Z));}
            return new(min,max);
        }
        double Center(int index,int axis){Triangle t=triangles[index];return (Point.From(vertices[t.A]).Axis(axis)+Point.From(vertices[t.B]).Axis(axis)+Point.From(vertices[t.C]).Axis(axis))/3;}
        int Build(int start,int count)
        {
            Box bounds=Bounds(start,count);int node=build.Count;build.Add(default);
            if(count<=8){build[node]=new(bounds,start,count,-1,-1);return node;}
            Point extent=bounds.Maximum-bounds.Minimum;int axis=extent.Y>extent.X?1:0;if(extent.Z>extent.Axis(axis))axis=2;
            Array.Sort(order,start,count,Comparer<int>.Create((a,b)=>{int comparison=Center(a,axis).CompareTo(Center(b,axis));return comparison==0?a.CompareTo(b):comparison;}));
            int left=Build(start,count/2),right=Build(start+count/2,count-count/2);build[node]=new(bounds,0,0,left,right);return node;
        }
        if(order.Length!=0)Build(0,order.Length);nodes=build.ToArray();
    }
    public FloorSegmentHit? IntersectSegment(Vector3 start,Vector3 end,bool includeNonSolid=false)
    {
        Finite(start);Finite(end);Point origin=Point.From(start),direction=Point.From(end)-origin;
        if(nodes.Length==0 || Point.Dot(direction,direction)==0)return null;
        double nearest=1;int selected=-1;double selectedU=0,selectedV=0;Stack<int> pending=new();pending.Push(0);
        while(pending.Count!=0)
        {
            Node node=nodes[pending.Pop()];if(!Intersects(node.Bounds,origin,direction,nearest))continue;
            if(node.Count==0){pending.Push(node.Right);pending.Push(node.Left);continue;}
            for(int i=node.Start;i<node.Start+node.Count;i++)
            {
                int index=order[i];Triangle triangle=triangles[index];if(triangle.NonSolid && !includeNonSolid)continue;
                Point a=Point.From(vertices[triangle.A]),e1=Point.From(vertices[triangle.B])-a,e2=Point.From(vertices[triangle.C])-a;
                Point p=Point.Cross(direction,e2);double determinant=Point.Dot(e1,p);if(determinant==0)continue;
                Point offset=origin-a;double u=Point.Dot(offset,p)/determinant;if(u<0 || u>1)continue;
                Point q=Point.Cross(offset,e1);double v=Point.Dot(direction,q)/determinant;if(v<0 || u+v>1)continue;
                double parameter=Point.Dot(e2,q)/determinant;if(parameter<0 || parameter>nearest)continue;
                if(parameter==nearest && selected>=0 && index>selected)continue;
                nearest=parameter;selected=index;selectedU=u;selectedV=v;
            }
        }
        if(selected<0)return null;
        Triangle hit=triangles[selected];Point normal=Point.Cross(Point.From(vertices[hit.B])-Point.From(vertices[hit.A]),Point.From(vertices[hit.C])-Point.From(vertices[hit.A]));double length=Math.Sqrt(Point.Dot(normal,normal));
        return new(selected,hit.Id,nearest,new((float)(origin.X+direction.X*nearest),(float)(origin.Y+direction.Y*nearest),(float)(origin.Z+direction.Z*nearest)),
            new((float)(1-selectedU-selectedV),(float)selectedU,(float)selectedV),new((float)(normal.X/length),(float)(normal.Y/length),(float)(normal.Z/length)));
    }
    private static bool Intersects(Box box,Point origin,Point direction,double maximum)
    {
        double minimum=0;
        for(int axis=0;axis<3;axis++)
        {
            double o=origin.Axis(axis),d=direction.Axis(axis),lo=box.Minimum.Axis(axis),hi=box.Maximum.Axis(axis);
            if(d==0){if(o<lo || o>hi)return false;continue;}
            double a=(lo-o)/d,b=(hi-o)/d;if(a>b)(a,b)=(b,a);minimum=Math.Max(minimum,a);maximum=Math.Min(maximum,b);if(minimum>maximum)return false;
        }
        return true;
    }
    private static void Finite(Vector3 p){if(!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))throw new InvalidDataException("Nonfinite floor query vector.");}
}
