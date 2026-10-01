using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorPathTriangleContact(TriangleBoxContactStatus Status,double Parameter,int SegmentIndex,
    int? TriangleId,FloorPathPoint Center,Vector3 Normal);

/// <summary>Continuous owned-triangle contact over precise feet segments; no floor locator adoption.</summary>
public static class FloorPathTriangleBoxSweep
{
    public static FloorPathTriangleContact Sweep(FloorWalkPath path,StaticTriangleBoxCollider collider,
        Vector3 half,bool allowSeparatingContact=false)
    {
        ArgumentNullException.ThrowIfNull(path);ArgumentNullException.ThrowIfNull(collider);
        if(!float.IsFinite(half.X)||!float.IsFinite(half.Y)||!float.IsFinite(half.Z)||
            half.X<0||half.Y<0||half.Z<0||half.X>100||half.Y>100||half.Z>100||
            path.Segments.Count>4097||!double.IsFinite(path.Result.Parameter)||path.Result.Parameter is <0 or >1||
            !Finite(path.Result.Position))throw new InvalidDataException("Invalid triangle feet-path bounds.");
        // Snapshot and validate the entire path before returning any early contact.
        var segments=path.Segments.ToArray();
        if((long)segments.Length*collider.TriangleCount>262144)throw new InvalidDataException("Triangle feet-path work budget.");
        FloorPathPoint Center(FloorPathPoint feet)=>feet with{Y=feet.Y+half.Y};
        for(int i=0;i<segments.Length;i++)
        {
            var s=segments[i];
            if(s.TriangleIndex<0||!Finite(s.Start)||!Finite(s.End)||!Finite(Center(s.Start))||!Finite(Center(s.End))||
                !double.IsFinite(s.StartParameter)||!double.IsFinite(s.EndParameter)||s.StartParameter<0||
                s.EndParameter<s.StartParameter||s.EndParameter>path.Result.Parameter||
                Math.Abs(s.End.X-s.Start.X)>100||Math.Abs(s.End.Y-s.Start.Y)>100||Math.Abs(s.End.Z-s.Start.Z)>100||
                i==0&&s.StartParameter!=0||i>0&&(s.StartParameter!=segments[i-1].EndParameter||DistanceSquared(s.Start,segments[i-1].End)>1e-12))
                throw new InvalidDataException("Invalid/discontinuous triangle feet-path segment.");
        }
        if(segments.Length>0&&segments[^1].EndParameter!=path.Result.Parameter)
            throw new InvalidDataException("Triangle feet-path terminal mismatch.");
        var terminal=Center(segments.Length==0?new(path.Result.Position.X,path.Result.Position.Y,path.Result.Position.Z):segments[^1].End);
        if(!Finite(terminal))throw new InvalidDataException("Triangle feet-path center bounds.");
        for(int i=0;i<segments.Length;i++)
        {
            var s=segments[i];var start=Center(s.Start);var end=Center(s.End);
            var hit=collider.SweepPrecise(start,end,half,allowSeparatingContact);
            if(hit.Status==TriangleBoxContactStatus.Clear)continue;
            double t=hit.Parameter;
            return new(hit.Status,s.StartParameter+(s.EndParameter-s.StartParameter)*t,i,hit.TriangleId,
                new(start.X+(end.X-start.X)*t,start.Y+(end.Y-start.Y)*t,start.Z+(end.Z-start.Z)*t),hit.Normal);
        }
        return new(TriangleBoxContactStatus.Clear,path.Result.Parameter,-1,null,terminal,Vector3.Zero);
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6f&&Math.Abs(v.Y)<=1e6f&&Math.Abs(v.Z)<=1e6f;
    private static bool Finite(FloorPathPoint v)=>double.IsFinite(v.X)&&double.IsFinite(v.Y)&&double.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6&&Math.Abs(v.Y)<=1e6&&Math.Abs(v.Z)<=1e6;
    private static double DistanceSquared(FloorPathPoint a,FloorPathPoint b)=>Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2);
}
