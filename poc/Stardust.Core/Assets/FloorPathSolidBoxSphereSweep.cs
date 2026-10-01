using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorPathSolidBoxSphereContact(PortalBarrierContactStatus Status,double Parameter,int SegmentIndex,
    FloorPathPoint Center,Vector3 Normal);

/// <summary>Continuous solid-box contact over precise feet segments; no floor locator adoption.</summary>
public static class FloorPathSolidBoxSphereSweep
{
    public static FloorPathSolidBoxSphereContact Sweep(FloorWalkPath path,SolidBoxSphereCollider collider,
        Vector3 centerOffset,float radius,bool? passageAllowed,bool allowSeparatingContact=false)
    {
        ArgumentNullException.ThrowIfNull(path);ArgumentNullException.ThrowIfNull(collider);
        if(!Finite(centerOffset)||!float.IsFinite(radius)||radius<=0||radius>100||
            path.Segments.Count>4097||!double.IsFinite(path.Result.Parameter)||path.Result.Parameter is <0 or >1||
            !Finite(path.Result.Position))throw new InvalidDataException("Invalid solid sphere feet-path bounds.");
        // Snapshot and validate the entire path before returning any early contact.
        var segments=path.Segments.ToArray();
        FloorPathPoint Center(FloorPathPoint feet)=>new(feet.X+centerOffset.X,feet.Y+centerOffset.Y,feet.Z+centerOffset.Z);
        for(int i=0;i<segments.Length;i++)
        {
            var s=segments[i];
            if(s.TriangleIndex<0||!Finite(s.Start)||!Finite(s.End)||!Finite(Center(s.Start))||!Finite(Center(s.End))||
                !double.IsFinite(s.StartParameter)||!double.IsFinite(s.EndParameter)||s.StartParameter<0||
                s.EndParameter<s.StartParameter||s.EndParameter>path.Result.Parameter||
                Math.Abs(s.End.X-s.Start.X)>100||Math.Abs(s.End.Y-s.Start.Y)>100||Math.Abs(s.End.Z-s.Start.Z)>100||
                i==0&&s.StartParameter!=0||i>0&&(s.StartParameter!=segments[i-1].EndParameter||DistanceSquared(s.Start,segments[i-1].End)>1e-12))
                throw new InvalidDataException("Invalid/discontinuous solid sphere feet-path segment.");
        }
        if(segments.Length>0&&segments[^1].EndParameter!=path.Result.Parameter)
            throw new InvalidDataException("Triangle feet-path terminal mismatch.");
        var terminal=Center(segments.Length==0?new(path.Result.Position.X,path.Result.Position.Y,path.Result.Position.Z):segments[^1].End);
        if(!Finite(terminal))throw new InvalidDataException("Triangle feet-path center bounds.");
        for(int i=0;i<segments.Length;i++)
        {
            var s=segments[i];var start=Center(s.Start);var end=Center(s.End);
            var hit=collider.SweepPrecise(start,end,radius,passageAllowed,allowSeparatingContact);
            if(hit.Status==PortalBarrierContactStatus.Clear)continue;
            double t=hit.Parameter;
            return new(hit.Status,s.StartParameter+(s.EndParameter-s.StartParameter)*t,i,
                new(start.X+(end.X-start.X)*t,start.Y+(end.Y-start.Y)*t,start.Z+(end.Z-start.Z)*t),hit.Normal);
        }
        return new(PortalBarrierContactStatus.Clear,path.Result.Parameter,-1,terminal,Vector3.Zero);
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6f&&Math.Abs(v.Y)<=1e6f&&Math.Abs(v.Z)<=1e6f;
    private static bool Finite(FloorPathPoint v)=>double.IsFinite(v.X)&&double.IsFinite(v.Y)&&double.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6&&Math.Abs(v.Y)<=1e6&&Math.Abs(v.Z)<=1e6;
    private static double DistanceSquared(FloorPathPoint a,FloorPathPoint b)=>Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2);
}
