using System.Numerics;

namespace Stardust.Core.Assets;

public enum TriangleSphereContactStatus {Clear,Blocked,InitialOverlap}
public sealed record TriangleSphereContact(TriangleSphereContactStatus Status,double Parameter,int? TriangleId,Vector3 Normal);
public readonly record struct TriangleSphereSweepStatistics(int SourceTriangles,int CandidateTriangles);
public readonly record struct TriangleSphereSweepResult(TriangleSphereContact Contact,TriangleSphereSweepStatistics Statistics);

/// <summary>Owned stationary triangles and continuous translating sphere; modern exact feature query, no sliding.</summary>
public sealed class StaticTriangleSphereCollider
{
    private readonly record struct D(double X,double Y,double Z)
    {
        internal static D From(Vector3 p)=>new(p.X,p.Y,p.Z);
        public static D operator+(D a,D b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static D operator-(D a,D b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static D operator*(D a,double b)=>new(a.X*b,a.Y*b,a.Z*b);
        internal double Dot(D b)=>X*b.X+Y*b.Y+Z*b.Z;
        internal D Cross(D b)=>new(Y*b.Z-Z*b.Y,Z*b.X-X*b.Z,X*b.Y-Y*b.X);
        internal double LengthSquared=>Dot(this);
        internal D Unit()=>this*(1/Math.Sqrt(LengthSquared));
        internal Vector3 Vector()=>new((float)X,(float)Y,(float)Z);
    }
    private sealed record Prepared(int Id,D A,D B,D C,D Normal,D Minimum,D Maximum);
    private readonly Prepared[] triangles;
    private readonly bool useBounds;
    internal int TriangleCount=>triangles.Length;
    public StaticTriangleSphereCollider(IReadOnlyList<CollisionTriangle> source,int maximumTriangles=2048,bool useBounds=true)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(maximumTriangles is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(maximumTriangles));
        if(source.Count>maximumTriangles)throw new InvalidDataException("Sphere triangle resource budget.");
        var snapshot=source.ToArray();if(snapshot.Any(t=>t.Id<0)||snapshot.Select(t=>t.Id).Distinct().Count()!=snapshot.Length)throw new InvalidDataException("Invalid sphere triangle IDs.");
        this.useBounds=useBounds;List<Prepared> prepared=new();
        foreach(var t in snapshot.OrderBy(t=>t.Id))
        {
            if(!Finite(D.From(t.A))||!Finite(D.From(t.B))||!Finite(D.From(t.C)))throw new InvalidDataException("Invalid sphere triangle vertex.");
            D a=D.From(t.A),b=D.From(t.B),c=D.From(t.C),normal=(b-a).Cross(c-a);
            if(normal.LengthSquared==0)throw new InvalidDataException("Degenerate sphere triangle.");
            prepared.Add(new(t.Id,a,b,c,normal.Unit(),new(Math.Min(a.X,Math.Min(b.X,c.X)),Math.Min(a.Y,Math.Min(b.Y,c.Y)),Math.Min(a.Z,Math.Min(b.Z,c.Z))),
                new(Math.Max(a.X,Math.Max(b.X,c.X)),Math.Max(a.Y,Math.Max(b.Y,c.Y)),Math.Max(a.Z,Math.Max(b.Z,c.Z)))));
        }
        triangles=prepared.ToArray();
    }
    public TriangleSphereContact Sweep(Vector3 start,Vector3 end,float radius,bool allowSeparatingContact=false)=>SweepWithStatistics(start,end,radius,allowSeparatingContact).Contact;
    public TriangleSphereSweepResult SweepWithStatistics(Vector3 start,Vector3 end,float radius,bool allowSeparatingContact=false)=>
        SweepCore(D.From(start),D.From(end),radius,allowSeparatingContact);
    internal TriangleSphereContact SweepPrecise(FloorPathPoint start,FloorPathPoint end,float radius,bool allowSeparatingContact)=>
        SweepCore(new(start.X,start.Y,start.Z),new(end.X,end.Y,end.Z),radius,allowSeparatingContact).Contact;
    private TriangleSphereSweepResult SweepCore(D start,D end,float radius,bool allowSeparatingContact)
    {
        D delta=end-start;
        if(!Finite(start)||!Finite(end)||!float.IsFinite(radius)||radius<=0||radius>100||Math.Abs(delta.X)>100||Math.Abs(delta.Y)>100||Math.Abs(delta.Z)>100)
            throw new ArgumentException("Finite bounded positive-radius sphere sweep required.");
        D minimum=new(Math.BitDecrement(Math.Min(start.X,end.X)-radius-1e-9),Math.BitDecrement(Math.Min(start.Y,end.Y)-radius-1e-9),Math.BitDecrement(Math.Min(start.Z,end.Z)-radius-1e-9));
        D maximum=new(Math.BitIncrement(Math.Max(start.X,end.X)+radius+1e-9),Math.BitIncrement(Math.Max(start.Y,end.Y)+radius+1e-9),Math.BitIncrement(Math.Max(start.Z,end.Z)+radius+1e-9));
        var best=new TriangleSphereContact(TriangleSphereContactStatus.Clear,1,null,Vector3.Zero);int candidates=0;
        foreach(var t in triangles)
        {
            if(useBounds&&(t.Minimum.X>maximum.X||t.Maximum.X<minimum.X||t.Minimum.Y>maximum.Y||t.Maximum.Y<minimum.Y||t.Minimum.Z>maximum.Z||t.Maximum.Z<minimum.Z))continue;
            candidates++;D nearest=Closest(start,t),outward=start-nearest;double distance=Math.Sqrt(outward.LengthSquared);
            if(distance<=radius)
            {
                bool strict=distance<radius-1e-9;
                if(allowSeparatingContact&&!strict&&outward.Dot(delta)>=0)continue;
                D normal=distance>0?outward.Unit():t.Normal*(t.Normal.Dot(delta)>0?-1:1);
                var hit=new TriangleSphereContact(strict?TriangleSphereContactStatus.InitialOverlap:TriangleSphereContactStatus.Blocked,0,t.Id,normal.Vector());
                if(best.Status==TriangleSphereContactStatus.Clear||best.Parameter>0)best=hit;
                continue;
            }
            double earliest=double.PositiveInfinity;
            void Candidate(double parameter)
            {
                if(double.IsFinite(parameter)&&parameter>=0&&parameter<=1&&parameter<earliest)earliest=parameter;
            }
            // Offset face planes, bounded edge cylinders and vertex balls form the exact rounded triangle.
            double d0=t.Normal.Dot(start-t.A),velocity=t.Normal.Dot(delta);
            if(velocity!=0)foreach(int sign in new[]{-1,1})
            {
                double parameter=(sign*radius-d0)/velocity;
                if(parameter<0||parameter>1)continue;
                D projection=start+delta*parameter-t.Normal*(sign*radius);
                if(Inside(projection,t))Candidate(parameter);
            }
            foreach(var edge in new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)})
            {
                D e=edge.Item2-edge.Item1,p=start-edge.Item1;double ee=e.LengthSquared,q0=p.Dot(e)/ee,qv=delta.Dot(e)/ee;
                D r0=p-e*q0,rv=delta-e*qv;
                foreach(double parameter in Roots(rv.LengthSquared,2*r0.Dot(rv),r0.LengthSquared-(double)radius*radius))
                {double q=q0+qv*parameter;if(q>=0&&q<=1)Candidate(parameter);}
            }
            foreach(D vertex in new[]{t.A,t.B,t.C})
            {
                D p=start-vertex;foreach(double parameter in Roots(delta.LengthSquared,2*p.Dot(delta),p.LengthSquared-(double)radius*radius))Candidate(parameter);
            }
            if(double.IsPositiveInfinity(earliest))continue;
            D center=start+delta*earliest,direction=center-Closest(center,t);
            D contactNormal=direction.LengthSquared>0?direction.Unit():t.Normal*(t.Normal.Dot(delta)>0?-1:1);
            var contact=new TriangleSphereContact(TriangleSphereContactStatus.Blocked,earliest,t.Id,contactNormal.Vector());
            if(best.Status==TriangleSphereContactStatus.Clear||contact.Parameter<best.Parameter)best=contact;
        }
        return new(best,new(triangles.Length,candidates));
    }
    private static IEnumerable<double> Roots(double a,double b,double c)
    {
        if(a==0)yield break;double discriminant=b*b-4*a*c;if(discriminant<0)yield break;
        double root=Math.Sqrt(discriminant),q=-.5*(b+Math.CopySign(root,b));
        if(q==0){yield return -b/(2*a);yield break;}
        double first=q/a,last=c/q;if(first>last)(first,last)=(last,first);yield return first;if(last!=first)yield return last;
    }
    private static D Closest(D p,Prepared t)
    {
        D projection=p-t.Normal*t.Normal.Dot(p-t.A);if(Inside(projection,t))return projection;
        D best=t.A;double distance=(p-best).LengthSquared;
        foreach(var edge in new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)})
        {
            D e=edge.Item2-edge.Item1;D point=edge.Item1+e*Math.Clamp((p-edge.Item1).Dot(e)/e.LengthSquared,0,1);
            double candidate=(p-point).LengthSquared;if(candidate<distance){best=point;distance=candidate;}
        }
        return best;
    }
    private static bool Inside(D p,Prepared t)
    {
        D ab=t.B-t.A,ac=t.C-t.A,ap=p-t.A,n=ab.Cross(ac);double denominator=n.LengthSquared;
        double u=ap.Cross(ac).Dot(n)/denominator,v=ab.Cross(ap).Dot(n)/denominator;
        return u>=0&&v>=0&&u+v<=1;
    }
    private static bool Finite(D p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z)&&Math.Abs(p.X)<=1e6&&Math.Abs(p.Y)<=1e6&&Math.Abs(p.Z)<=1e6;
}
