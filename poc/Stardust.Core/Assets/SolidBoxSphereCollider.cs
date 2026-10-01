using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record SolidBoxSphereContact(PortalBarrierContactStatus Status,double Parameter,FloorPathPoint Center,Vector3 Normal);

/// <summary>Continuous translating sphere against an owned captured rigid solid box. No sliding or depenetration.</summary>
public sealed class SolidBoxSphereCollider
{
    private readonly PortalBarrierCollider box;
    public SolidBoxSphereCollider(PortalBarrierCollider box)
    {
        ArgumentNullException.ThrowIfNull(box);
        this.box=new(box.Minimum,box.Maximum,box.Pose);
        if(!Finite(box.Minimum)||!Finite(box.Maximum)||!Finite(box.Pose.Translation))throw new InvalidDataException("Sphere box coordinate bounds.");
    }
    public SolidBoxSphereContact Sweep(Vector3 start,Vector3 end,float radius,bool? passageAllowed=false,bool allowSeparatingContact=false)
        =>SweepPrecise(start,end,radius,passageAllowed,allowSeparatingContact);
    internal SolidBoxSphereContact SweepPrecise(FloorPathPoint start,FloorPathPoint end,float radius,bool? passageAllowed,bool allowSeparatingContact)
    {
        if(!Finite(start)||!Finite(end)||!float.IsFinite(radius)||radius<=0||radius>100||
            Math.Abs(end.X-start.X)>100||Math.Abs(end.Y-start.Y)>100||Math.Abs(end.Z-start.Z)>100)
            throw new InvalidDataException("Finite bounded sphere box query required.");
        SolidBoxSphereContact Clear()=>new(PortalBarrierContactStatus.Clear,1,end,Vector3.Zero);
        if(passageAllowed==true)return Clear();
        double[] Local(FloorPathPoint p)
        {
            var t=box.Pose;double x=p.X-t.Translation.X,y=p.Y-t.Translation.Y,z=p.Z-t.Translation.Z;
            return new[]{t.RowX.X*x+t.RowY.X*y+t.RowZ.X*z,t.RowX.Y*x+t.RowY.Y*y+t.RowZ.Y*z,t.RowX.Z*x+t.RowY.Z*y+t.RowZ.Z*z};
        }
        double[] a=Local(start),b=Local(end),lo={box.Minimum.X,box.Minimum.Y,box.Minimum.Z},hi={box.Maximum.X,box.Maximum.Y,box.Maximum.Z};
        double[] delta=Enumerable.Range(0,3).Select(i=>b[i]-a[i]).ToArray();
        double[] Outward(double t)=>Enumerable.Range(0,3).Select(i=>{double v=a[i]+delta[i]*t;return v-Math.Clamp(v,lo[i],hi[i]);}).ToArray();
        double Dot(double[] x,double[] y)=>x[0]*y[0]+x[1]*y[1]+x[2]*y[2];
        SolidBoxSphereContact Contact(double t,bool overlap)
        {
            var n=Outward(t);double length=Math.Sqrt(Dot(n,n));
            if(length==0)
            {
                // An interior sphere already intersects the solid. Choose the nearest exit face only as a diagnostic normal.
                int axis=0;double sign=-1,distance=double.PositiveInfinity;
                for(int i=0;i<3;i++)foreach(double side in new[]{-1d,1d})
                {
                    double v=a[i]+delta[i]*t,d=side<0?v-lo[i]:hi[i]-v;
                    if(d<distance){distance=d;axis=i;sign=side;}
                }
                n=new double[3];n[axis]=sign;length=1;
            }
            var normal=Vector3.Normalize(box.Pose.TransformDirection(new((float)(n[0]/length),(float)(n[1]/length),(float)(n[2]/length))));
            return new(passageAllowed is null?PortalBarrierContactStatus.PermissionUnknown:overlap?PortalBarrierContactStatus.InitialOverlap:PortalBarrierContactStatus.Blocked,
                t,new(start.X+(end.X-start.X)*t,start.Y+(end.Y-start.Y)*t,start.Z+(end.Z-start.Z)*t),normal);
        }
        var initial=Outward(0);double initialDistance=Math.Sqrt(Dot(initial,initial));
        if(initialDistance<=radius)
        {
            bool strict=initialDistance<radius-1e-9;
            if(allowSeparatingContact&&!strict&&Dot(initial,delta)>=0)return Clear();
            return Contact(0,strict);
        }
        // The clamped closest point changes only when a center coordinate crosses a box face.
        var cuts=new SortedSet<double>{0,1};
        for(int i=0;i<3;i++)if(delta[i]!=0)foreach(double face in new[]{lo[i],hi[i]})
        {double t=(face-a[i])/delta[i];if(t>0&&t<1)cuts.Add(t);}
        var times=cuts.ToArray();
        for(int k=0;k<times.Length-1;k++)
        {
            double first=times[k],last=times[k+1],middle=(first+last)/2,A=0,B=0,C=-(double)radius*radius;
            for(int i=0;i<3;i++)
            {
                double v=a[i]+delta[i]*middle;
                if(v>=lo[i]&&v<=hi[i])continue;
                double offset=a[i]-(v<lo[i]?lo[i]:hi[i]);
                A+=delta[i]*delta[i];B+=2*offset*delta[i];C+=offset*offset;
            }
            if(A==0){if(C<=0)return Contact(first,false);continue;}
            double discriminant=B*B-4*A*C;
            if(discriminant<0)continue;
            double q=-.5*(B+Math.CopySign(Math.Sqrt(discriminant),B));
            double x=q==0?-B/(2*A):q/A,y=q==0?x:C/q;
            double enter=Math.Min(x,y),exit=Math.Max(x,y),hit=Math.Max(first,enter);
            if(hit<=last&&hit<=exit)return Contact(hit,false);
        }
        return Clear();
    }
    private static bool Finite(Vector3 p)=>Finite((FloorPathPoint)p);
    private static bool Finite(FloorPathPoint p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z)&&Math.Abs(p.X)<=1e6&&Math.Abs(p.Y)<=1e6&&Math.Abs(p.Z)<=1e6;
}
