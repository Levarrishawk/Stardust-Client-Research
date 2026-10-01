using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct CollisionTriangle(int Id,Vector3 A,Vector3 B,Vector3 C);
public enum TriangleBoxContactStatus {Clear,Blocked,InitialOverlap}
public sealed record TriangleBoxContact(TriangleBoxContactStatus Status,double Parameter,int? TriangleId,Vector3 Normal);
public readonly record struct TriangleBoxSweepStatistics(int NodeVisits,int CandidateTriangles,int NarrowPhaseTriangles);
public readonly record struct TriangleBoxSweepResult(TriangleBoxContact Contact,TriangleBoxSweepStatistics Statistics);

/// <summary>Owned stationary triangle set; continuous world-axis box translation, no sliding or locator adoption.</summary>
public sealed class StaticTriangleBoxCollider
{
    private readonly record struct D(double X,double Y,double Z)
    {
        internal static D From(Vector3 v)=>new(v.X,v.Y,v.Z);
        public static D operator-(D a,D b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        internal double Dot(D b)=>X*b.X+Y*b.Y+Z*b.Z;
        internal D Cross(D b)=>new(Y*b.Z-Z*b.Y,Z*b.X-X*b.Z,X*b.Y-Y*b.X);
        internal double Length=>Math.Sqrt(Dot(this));
        internal D Unit()=>new(X/Length,Y/Length,Z/Length);
        internal Vector3 Vector(float sign=1)=>new((float)(X*sign),(float)(Y*sign),(float)(Z*sign));
    }
    private sealed record Axis(D Direction,double Minimum,double Maximum);
    private sealed record Prepared(int Id,Axis[] Axes,D Minimum,D Maximum);
    private sealed record Node(D Minimum,D Maximum,int Left,int Right,int Start,int Count);
    private readonly Prepared[] triangles;
    private readonly int[] spatialOrder;
    private readonly Node[] nodes;
    private readonly bool useBoundsIndex;
    public const int MaximumSupportedTriangles=65536;
    internal int TriangleCount=>triangles.Length;
    public StaticTriangleBoxCollider(IReadOnlyList<CollisionTriangle> source,int maximumTriangles=2048,bool useBoundsIndex=true)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(maximumTriangles is <0 or >MaximumSupportedTriangles)throw new ArgumentOutOfRangeException(nameof(maximumTriangles));
        if(source.Count>maximumTriangles)throw new InvalidDataException("Triangle collider resource budget.");
        this.useBoundsIndex=useBoundsIndex;
        var snapshot=source.ToArray();if(snapshot.Any(t=>t.Id<0)||snapshot.Select(t=>t.Id).Distinct().Count()!=snapshot.Length)throw new InvalidDataException("Triangle collider IDs invalid/duplicate.");
        var prepared=new List<Prepared>();var boxAxes=new[]{new D(1,0,0),new D(0,1,0),new D(0,0,1)};
        foreach(var triangle in snapshot.OrderBy(t=>t.Id))
        {
            if(!Finite(triangle.A)||!Finite(triangle.B)||!Finite(triangle.C))throw new InvalidDataException("Triangle collider vertex invalid.");
            D a=D.From(triangle.A),b=D.From(triangle.B),c=D.From(triangle.C);var edges=new[]{b-a,c-b,a-c};D normal=(b-a).Cross(c-a);
            if(normal.Length==0)throw new InvalidDataException("Degenerate triangle outside collider slice.");
            var directions=new List<D>(boxAxes){normal};foreach(var edge in edges)foreach(var boxAxis in boxAxes)directions.Add(edge.Cross(boxAxis));
            var axes=new List<Axis>();
            foreach(var direction in directions)
            {
                if(direction.Length==0)continue;var unit=direction.Unit();double pa=unit.Dot(a),pb=unit.Dot(b),pc=unit.Dot(c);
                axes.Add(new(unit,Math.Min(pa,Math.Min(pb,pc)),Math.Max(pa,Math.Max(pb,pc))));
            }
            prepared.Add(new(triangle.Id,axes.ToArray(),
                new(Math.Min(a.X,Math.Min(b.X,c.X)),Math.Min(a.Y,Math.Min(b.Y,c.Y)),Math.Min(a.Z,Math.Min(b.Z,c.Z))),
                new(Math.Max(a.X,Math.Max(b.X,c.X)),Math.Max(a.Y,Math.Max(b.Y,c.Y)),Math.Max(a.Z,Math.Max(b.Z,c.Z)))));
        }
        triangles=prepared.ToArray();
        spatialOrder=Enumerable.Range(0,triangles.Length).ToArray();var built=new List<Node>();
        if(useBoundsIndex&&triangles.Length!=0)Build(0,triangles.Length,built);
        nodes=built.ToArray();
    }
    public TriangleBoxContact Sweep(Vector3 start,Vector3 end,Vector3 half,bool allowSeparatingContact=false)
        =>SweepWithStatistics(start,end,half,allowSeparatingContact).Contact;
    public TriangleBoxSweepResult SweepWithStatistics(Vector3 start,Vector3 end,Vector3 half,bool allowSeparatingContact=false)
    {
        if(!Finite(start)||!Finite(end)||!Finite(half)||half.X<0||half.Y<0||half.Z<0||half.X>100||half.Y>100||half.Z>100||Math.Abs((double)end.X-start.X)>100||Math.Abs((double)end.Y-start.Y)>100||Math.Abs((double)end.Z-start.Z)>100)throw new ArgumentException("Finite bounded triangle-box sweep required.");
        var contact=SweepCore(D.From(start),D.From(end),half,allowSeparatingContact,out var statistics);
        return new(contact,statistics);
    }
    internal TriangleBoxContact SweepPrecise(FloorPathPoint start,FloorPathPoint end,Vector3 half,bool allowSeparatingContact)=>
        SweepCore(new(start.X,start.Y,start.Z),new(end.X,end.Y,end.Z),half,allowSeparatingContact,out _);
    private TriangleBoxContact SweepCore(D origin,D end,Vector3 half,bool allowSeparatingContact,out TriangleBoxSweepStatistics statistics)
    {
        D delta=end-origin;var best=new TriangleBoxContact(TriangleBoxContactStatus.Clear,1,null,Vector3.Zero);
        var candidates=Candidates(origin,end,half,out int visits);statistics=new(visits,candidates.Count,candidates.Count);
        foreach(int index in candidates)
        {
            var triangle=triangles[index];
            double enter=double.NegativeInfinity,exit=double.PositiveInfinity;Vector3 normal=Vector3.Zero;bool rejected=false,inside=true,strict=true,separating=false,tangentThroughout=false;
            foreach(var axis in triangle.Axes)
            {
                D n=axis.Direction;double radius=Math.Abs(n.X)*half.X+Math.Abs(n.Y)*half.Y+Math.Abs(n.Z)*half.Z;
                double low=axis.Minimum-radius,high=axis.Maximum+radius,position=n.Dot(origin),velocity=n.Dot(delta);
                if(position<low||position>high)inside=false;
                if(position<=low+1e-9||position>=high-1e-9)strict=false;
                if(Math.Abs(position-low)<=1e-9&&velocity<=0||Math.Abs(position-high)<=1e-9&&velocity>=0)separating=true;
                if(velocity==0)
                {
                    if(position<low||position>high){rejected=true;break;}
                    if(Math.Abs(position-low)<=1e-9||Math.Abs(position-high)<=1e-9)tangentThroughout=true;
                    continue;
                }
                double first=(low-position)/velocity,last=(high-position)/velocity;
                if(first>last)(first,last)=(last,first);
                if(first>enter){enter=first;normal=n.Vector(velocity>0?-1:1);}exit=Math.Min(exit,last);
                if(enter>exit){rejected=true;break;}
            }
            if(rejected||exit<0||enter>1)continue;
            // A fixed separating plane prevents penetration even when a finite
            // triangle edge is first reached later in the sweep (e.g. floor tiles).
            if(allowSeparatingContact&&tangentThroughout)continue;
            if(inside&&allowSeparatingContact&&!strict&&separating)continue;
            var contact=new TriangleBoxContact(inside&&strict?TriangleBoxContactStatus.InitialOverlap:TriangleBoxContactStatus.Blocked,inside?0:Math.Max(0,enter),triangle.Id,normal);
            if(best.Status==TriangleBoxContactStatus.Clear||contact.Parameter<best.Parameter)best=contact;
        }
        return best;
    }
    private int Build(int start,int count,List<Node> built)
    {
        D minimum=new(double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity),maximum=new(double.NegativeInfinity,double.NegativeInfinity,double.NegativeInfinity);
        for(int i=start;i<start+count;i++)
        {
            var t=triangles[spatialOrder[i]];
            minimum=new(Math.Min(minimum.X,t.Minimum.X),Math.Min(minimum.Y,t.Minimum.Y),Math.Min(minimum.Z,t.Minimum.Z));
            maximum=new(Math.Max(maximum.X,t.Maximum.X),Math.Max(maximum.Y,t.Maximum.Y),Math.Max(maximum.Z,t.Maximum.Z));
        }
        int index=built.Count;built.Add(new(minimum,maximum,-1,-1,start,count));
        if(count<=8)return index;
        D span=maximum-minimum;int axis=span.X>=span.Y&&span.X>=span.Z?0:span.Y>=span.Z?1:2;
        double Center(Prepared p)=>axis==0?(p.Minimum.X+p.Maximum.X)*.5:axis==1?(p.Minimum.Y+p.Maximum.Y)*.5:(p.Minimum.Z+p.Maximum.Z)*.5;
        Array.Sort(spatialOrder,start,count,Comparer<int>.Create((a,b)=>{int c=Center(triangles[a]).CompareTo(Center(triangles[b]));return c!=0?c:triangles[a].Id.CompareTo(triangles[b].Id);}));
        int half=count/2,left=Build(start,half,built),right=Build(start+half,count-half,built);
        built[index]=new(minimum,maximum,left,right,0,0);return index;
    }
    private List<int> Candidates(D start,D end,Vector3 half,out int visits)
    {
        visits=0;
        if(!useBoundsIndex)return Enumerable.Range(0,triangles.Length).ToList();
        var result=new List<int>();if(nodes.Length==0)return result;
        // Outward rounding and a conservative pad affect broad phase only.
        D minimum=new(Math.BitDecrement(Math.Min(start.X,end.X)-half.X-1e-9),Math.BitDecrement(Math.Min(start.Y,end.Y)-half.Y-1e-9),Math.BitDecrement(Math.Min(start.Z,end.Z)-half.Z-1e-9));
        D maximum=new(Math.BitIncrement(Math.Max(start.X,end.X)+half.X+1e-9),Math.BitIncrement(Math.Max(start.Y,end.Y)+half.Y+1e-9),Math.BitIncrement(Math.Max(start.Z,end.Z)+half.Z+1e-9));
        bool Overlap(D lo,D hi)=>lo.X<=maximum.X&&hi.X>=minimum.X&&lo.Y<=maximum.Y&&hi.Y>=minimum.Y&&lo.Z<=maximum.Z&&hi.Z>=minimum.Z;
        var stack=new Stack<int>();stack.Push(0);
        while(stack.Count!=0)
        {
            var node=nodes[stack.Pop()];visits++;
            if(!Overlap(node.Minimum,node.Maximum))continue;
            if(node.Count==0){stack.Push(node.Right);stack.Push(node.Left);continue;}
            for(int i=node.Start;i<node.Start+node.Count;i++)
            {
                int t=spatialOrder[i];if(Overlap(triangles[t].Minimum,triangles[t].Maximum))result.Add(t);
            }
        }
        // Restore source ID ordering so spatial traversal cannot change equal-time contact selection.
        result.Sort();return result;
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6f&&Math.Abs(v.Y)<=1e6f&&Math.Abs(v.Z)<=1e6f;
}
