using System.Numerics;
using Stardust.Core.Assets;

internal static class SkeletonBindPoseScenarios
{
    internal static void Run()
    {
        var raw=SkeletonTemplateDecoder.Decode(SkeletonTemplateScenarios.Basic()).SerializedLevels[0];var translated=SkeletonBindPoseBuilder.Create(raw);
        Near(translated.InverseBind[1].Translation,new(0,-1,0));
        float h=(float)Math.Sqrt(.5);var identity=new ScalarFirstQuaternion(1,0,0,0);
        var parent=raw.Joints[0] with{Bptr=new(2,0,0),Rpst=new(h,0,0,h),Rpre=identity,Bpro=identity};
        var child=raw.Joints[1] with{Bptr=new(1,0,0),Rpst=identity,Rpre=identity,Bpro=identity};
        var pose=SkeletonBindPoseBuilder.Create(new(Array.AsReadOnly(new[]{parent,child})));
        // Legacy parent forward * child local places the child at (2,1,0), not (3,0,0).
        Near(SkeletonBindPoseBuilder.RigidInverse(pose.InverseBind[1]).Translation,new(2,1,0));
        var external=Matrix4x4.CreateRotationY(.7f)*Matrix4x4.CreateTranslation(4,2,-3);
        var attached=SkeletonBindPoseBuilder.Create(raw,SkeletonBindPoseBuilder.RigidInverse(external));
        Near(SkeletonBindPoseBuilder.RigidInverse(attached.InverseBind[1]).Translation,Vector3.Transform(new(0,1,0),external));
        var rotations=parent with{Rpre=new(h,h,0,0),Rpst=new(h,0,h,0),Bpro=new(h,0,0,h),Bptr=Vector3.Zero,Parent=-1};
        var composed=SkeletonBindPoseBuilder.Create(new(Array.AsReadOnly(new[]{rotations})));
        Vector3 point=new(.2f,-.3f,.7f);
        Vector3 expected=Vector3.Transform(Vector3.Transform(Vector3.Transform(point,new Quaternion(h,0,0,h)),new Quaternion(0,0,h,h)),new Quaternion(0,h,0,h));
        Near(Vector3.Transform(point,composed.LocalBind[0]),expected);
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletonBindPoseBuilder.Create(raw,Matrix4x4.CreateScale(2)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletonBindPoseBuilder.Create(new(Array.AsReadOnly(new[]{child with{Parent=0}}))));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletonBindPoseBuilder.Create(new(Array.AsReadOnly(new[]{parent with{Bptr=new(float.NaN,0,0)}}))));
        if(SkeletonBindPoseBuilder.Create(SkeletonTemplateDecoder.Decode(SkeletonTemplateScenarios.Basic(0)).SerializedLevels[0]).InverseBind.Count!=0)throw new Exception("Zero pose mismatch.");
        Console.WriteLine("PASS: recovered scalar-first bind order, transposed parent/external affine composition, noncommuting known poses and invalid topology/rigidity gates");
    }
    private static void Near(Vector3 actual,Vector3 expected){if(Vector3.Distance(actual,expected)>2e-5f)throw new Exception("Known bind pose mismatch: "+actual+" vs "+expected);}
    internal static float[] LegacyValues(Matrix4x4 m)=>new[]{m.M11,m.M21,m.M31,m.M41,m.M12,m.M22,m.M32,m.M42,m.M13,m.M23,m.M33,m.M43};
}
