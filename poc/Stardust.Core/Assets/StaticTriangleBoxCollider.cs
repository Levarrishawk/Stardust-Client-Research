using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct CollisionTriangle(int Id,Vector3 A,Vector3 B,Vector3 C);
public enum TriangleBoxContactStatus {Clear,Blocked,InitialOverlap}
public sealed record TriangleBoxContact(TriangleBoxContactStatus Status,double Parameter,int? TriangleId,Vector3 Normal);

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
    private sealed record Prepared(int Id,Axis[] Axes);
    private readonly Prepared[] triangles;
    internal int TriangleCount=>triangles.Length;
    public StaticTriangleBoxCollider(IReadOnlyList<CollisionTriangle> source)
    {
        ArgumentNullException.ThrowIfNull(source);if(source.Count>2048)throw new InvalidDataException("Triangle collider resource budget.");
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
            prepared.Add(new(triangle.Id,axes.ToArray()));
        }
        triangles=prepared.ToArray();
    }
    public TriangleBoxContact Sweep(Vector3 start,Vector3 end,Vector3 half,bool allowSeparatingContact=false)
    {
        if(!Finite(start)||!Finite(end)||!Finite(half)||half.X<0||half.Y<0||half.Z<0||half.X>100||half.Y>100||half.Z>100||Math.Abs((double)end.X-start.X)>100||Math.Abs((double)end.Y-start.Y)>100||Math.Abs((double)end.Z-start.Z)>100)throw new ArgumentException("Finite bounded triangle-box sweep required.");
        return SweepCore(D.From(start),D.From(end),half,allowSeparatingContact);
    }
    internal TriangleBoxContact SweepPrecise(FloorPathPoint start,FloorPathPoint end,Vector3 half,bool allowSeparatingContact)=>
        SweepCore(new(start.X,start.Y,start.Z),new(end.X,end.Y,end.Z),half,allowSeparatingContact);
    private TriangleBoxContact SweepCore(D origin,D end,Vector3 half,bool allowSeparatingContact)
    {
        D delta=end-origin;var best=new TriangleBoxContact(TriangleBoxContactStatus.Clear,1,null,Vector3.Zero);
        foreach(var triangle in triangles)
        {
            double enter=double.NegativeInfinity,exit=double.PositiveInfinity;Vector3 normal=Vector3.Zero;bool rejected=false,inside=true,strict=true,separating=false;
            foreach(var axis in triangle.Axes)
            {
                D n=axis.Direction;double radius=Math.Abs(n.X)*half.X+Math.Abs(n.Y)*half.Y+Math.Abs(n.Z)*half.Z;
                double low=axis.Minimum-radius,high=axis.Maximum+radius,position=n.Dot(origin),velocity=n.Dot(delta);
                if(position<low||position>high)inside=false;
                if(position<=low+1e-9||position>=high-1e-9)strict=false;
                if(Math.Abs(position-low)<=1e-9&&velocity<=0||Math.Abs(position-high)<=1e-9&&velocity>=0)separating=true;
                if(velocity==0){if(position<low||position>high){rejected=true;break;}continue;}
                double first=(low-position)/velocity,last=(high-position)/velocity;
                if(first>last)(first,last)=(last,first);
                if(first>enter){enter=first;normal=n.Vector(velocity>0?-1:1);}exit=Math.Min(exit,last);
                if(enter>exit){rejected=true;break;}
            }
            if(rejected||exit<0||enter>1)continue;
            if(inside&&allowSeparatingContact&&!strict&&separating)continue;
            var contact=new TriangleBoxContact(inside&&strict?TriangleBoxContactStatus.InitialOverlap:TriangleBoxContactStatus.Blocked,inside?0:Math.Max(0,enter),triangle.Id,normal);
            if(best.Status==TriangleBoxContactStatus.Clear||contact.Parameter<best.Parameter)best=contact;
        }
        return best;
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6f&&Math.Abs(v.Y)<=1e6f&&Math.Abs(v.Z)<=1e6f;
}
