using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorPathBoxContact(PortalBarrierContactStatus Status,double Parameter,int SegmentIndex,
    FloorPathPoint Center,Vector3 Normal);

/// <summary>Owned conservative box sweep along precise feet segments in the collider's coordinate space. No sliding or locator adoption.</summary>
public static class FloorPathBoxSweep
{
    public static FloorPathBoxContact Sweep(FloorWalkPath path,PortalBarrierCollider box,Vector3 half,
        bool? passageAllowed,bool allowSeparatingContact=false)
    {
        ArgumentNullException.ThrowIfNull(path);ArgumentNullException.ThrowIfNull(box);
        if(!Finite(half) || half.X<0 || half.Y<0 || half.Z<0 || path.Segments.Count>4097 ||
            !double.IsFinite(path.Result.Parameter) || path.Result.Parameter is <0 or >1 || !Finite(path.Result.Position))
            throw new InvalidDataException("Invalid piecewise sweep bounds.");
        // Validate the complete caller-supplied path before any permission shortcut or contact result.
        for(int i=0;i<path.Segments.Count;i++)
        {
            var s=path.Segments[i];
            if(s.TriangleIndex<0 || !Finite(s.Start) || !Finite(s.End) || !double.IsFinite(s.StartParameter) || !double.IsFinite(s.EndParameter) ||
                s.StartParameter<0 || s.EndParameter<s.StartParameter || s.EndParameter>path.Result.Parameter ||
                i==0 && s.StartParameter!=0 || i>0 && (s.StartParameter!=path.Segments[i-1].EndParameter || DistanceSquared(s.Start,path.Segments[i-1].End)>1e-12))
                throw new InvalidDataException("Invalid or discontinuous piecewise sweep path.");
        }
        if(path.Segments.Count>0 && path.Segments[^1].EndParameter!=path.Result.Parameter)
            throw new InvalidDataException("Piecewise sweep prefix mismatch.");
        FloorPathPoint terminal=path.Segments.Count==0?path.Result.Position:path.Segments[^1].End;
        terminal=terminal with{Y=terminal.Y+half.Y};
        FloorPathBoxContact Clear()=>new(PortalBarrierContactStatus.Clear,path.Result.Parameter,-1,terminal,Vector3.Zero);
        if(passageAllowed==true || path.Segments.Count==0)return Clear();
        AppearanceTransform pose=box.Pose;
        double[] extent={Math.Abs((double)pose.RowX.X)*half.X+Math.Abs((double)pose.RowY.X)*half.Y+Math.Abs((double)pose.RowZ.X)*half.Z,
            Math.Abs((double)pose.RowX.Y)*half.X+Math.Abs((double)pose.RowY.Y)*half.Y+Math.Abs((double)pose.RowZ.Y)*half.Z,
            Math.Abs((double)pose.RowX.Z)*half.X+Math.Abs((double)pose.RowY.Z)*half.Y+Math.Abs((double)pose.RowZ.Z)*half.Z};
        double[] min={box.Minimum.X-extent[0],box.Minimum.Y-extent[1],box.Minimum.Z-extent[2]},
            max={box.Maximum.X+extent[0],box.Maximum.Y+extent[1],box.Maximum.Z+extent[2]};
        double[] Local(FloorPathPoint p)
        {
            double x=p.X-pose.Translation.X,y=p.Y+half.Y-pose.Translation.Y,z=p.Z-pose.Translation.Z;
            return new[]{pose.RowX.X*x+pose.RowY.X*y+pose.RowZ.X*z,pose.RowX.Y*x+pose.RowY.Y*y+pose.RowZ.Y*z,pose.RowX.Z*x+pose.RowY.Z*y+pose.RowZ.Z*z};
        }
        for(int i=0;i<path.Segments.Count;i++)
        {
            var segment=path.Segments[i];double[] start=Local(segment.Start),end=Local(segment.End);
            if(start.Concat(end).Concat(min).Concat(max).Any(v=>!double.IsFinite(v)))throw new InvalidDataException("Piecewise sweep overflow.");
            bool inside=Enumerable.Range(0,3).All(axis=>start[axis]>=min[axis] && start[axis]<=max[axis]);
            if(allowSeparatingContact && Enumerable.Range(0,3).Any(axis=>Math.Abs(start[axis]-min[axis])<=1e-5 && end[axis]<start[axis] || Math.Abs(start[axis]-max[axis])<=1e-5 && end[axis]>start[axis]))continue;
            // The same rounded-face recognition also holds inward motion at a reusable contact.
            // This optional conservative start policy prevents repeated quantization from creeping tangentially.
            if(allowSeparatingContact && !inside && Enumerable.Range(0,3).All(axis=>start[axis]>=min[axis]-1e-4 && start[axis]<=max[axis]+1e-4))
            {
                int touching=-1;float sign=0;
                for(int axis=0;axis<3;axis++)
                {
                    if(Math.Abs(start[axis]-min[axis])<=1e-4 && end[axis]>start[axis]){touching=axis;sign=-1;break;}
                    if(Math.Abs(start[axis]-max[axis])<=1e-4 && end[axis]<start[axis]){touching=axis;sign=1;break;}
                }
                if(touching>=0)
                {
                    Vector3 face=touching switch{0=>new(sign,0,0),1=>new(0,sign,0),_=>new(0,0,sign)};
                    return new(passageAllowed is null?PortalBarrierContactStatus.PermissionUnknown:PortalBarrierContactStatus.InitialOverlap,
                        segment.StartParameter,i,segment.Start with{Y=segment.Start.Y+half.Y},pose.TransformDirection(face));
                }
            }
            double enter=0,exit=1;Vector3 normal=Vector3.Zero;bool clear=false;
            for(int axis=0;axis<3;axis++)
            {
                double delta=end[axis]-start[axis];
                if(delta==0){if(start[axis]<min[axis] || start[axis]>max[axis]){clear=true;break;}continue;}
                double near=(min[axis]-start[axis])/delta,far=(max[axis]-start[axis])/delta;float sign=-1;
                if(near>far){(near,far)=(far,near);sign=1;}
                if(near>enter){enter=near;normal=axis switch{0=>new(sign,0,0),1=>new(0,sign,0),_=>new(0,0,sign)};}
                exit=Math.Min(exit,far);if(enter>exit){clear=true;break;}
            }
            if(clear || exit<0 || enter>1)continue;
            double t=Math.Clamp(enter,0,1),parameter=segment.StartParameter+(segment.EndParameter-segment.StartParameter)*t;
            FloorPathPoint center=new(segment.Start.X+(segment.End.X-segment.Start.X)*t,
                segment.Start.Y+(segment.End.Y-segment.Start.Y)*t+half.Y,segment.Start.Z+(segment.End.Z-segment.Start.Z)*t);
            return new(passageAllowed is null?PortalBarrierContactStatus.PermissionUnknown:inside?PortalBarrierContactStatus.InitialOverlap:PortalBarrierContactStatus.Blocked,
                parameter,i,center,pose.TransformDirection(normal));
        }
        return Clear();
    }
    private static double DistanceSquared(FloorPathPoint a,FloorPathPoint b)=>Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2);
    private static bool Finite(FloorPathPoint p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z);
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
