using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalPoseTransitionScenarios
{
    internal static void Run()
    {
        var a=Matrix4x4.CreateRotationY(MathF.PI*170/180);a.Translation=new(2,4,6);
        var b=Matrix4x4.CreateRotationY(-MathF.PI*170/180);b.Translation=new(6,8,10);
        var first=new[]{a};var second=new[]{b};var middle=SkeletalPoseBlender.Blend(first,second,.5f)[0];
        if(Vector3.Distance(middle.Translation,new(4,6,8))>1e-6||Vector3.Distance(Vector3.TransformNormal(Vector3.UnitZ,middle),-Vector3.UnitZ)>1e-5)throw new Exception("Rigid shortest-arc/translation blend mismatch.");
        if(SkeletalPoseBlender.Blend(first,second,0)[0]!=a||SkeletalPoseBlender.Blend(first,second,1)[0]!=b)throw new Exception("Blend endpoints lost exact matrices.");
        var transition=new SkeletalPoseTransition(first,"idle",.2);var start=transition.Prepare("walk",second,0);transition.Commit(start);
        if(transition.CurrentPose[0]!=a||!transition.Transitioning)throw new Exception("Transition starts discontinuously.");
        var halfway=transition.Prepare("walk",second,.1);if(transition.CurrentPose[0]!=a)throw new Exception("Prepared pose mutated publication.");transition.Commit(halfway);
        var interrupted=transition.Prepare("run",new[]{Matrix4x4.Identity},0);if(!interrupted.LocalPose.SequenceEqual(transition.CurrentPose))throw new Exception("Interrupted transition lost current published pose.");transition.Commit(interrupted);
        var finished=transition.Prepare("run",new[]{Matrix4x4.Identity},.2);transition.Commit(finished);if(transition.Transitioning||transition.CurrentPose[0]!=Matrix4x4.Identity)throw new Exception("Transition failed exact target endpoint.");
        var held=transition.CurrentPose;var invalid=Matrix4x4.CreateScale(2);
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        foreach(float weight in new[]{float.NaN,-.1f,1.1f})Reject(()=>SkeletalPoseBlender.Blend(first,second,weight));
        Reject(()=>SkeletalPoseBlender.Blend(first,Array.Empty<Matrix4x4>(),.5f));Reject(()=>SkeletalPoseBlender.Blend(first,new[]{invalid},0));Reject(()=>transition.Prepare("walk",new[]{invalid},.1));Reject(()=>transition.Prepare("",first,0));Reject(()=>transition.Prepare("walk",first,double.NaN));
        if(!ReferenceEquals(held,transition.CurrentPose))throw new Exception("Invalid transition changed published pose.");
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>transition.Commit(finished));var other=new SkeletalPoseTransition(first,"idle",.2);HostAccountingScenarios.Reject<InvalidOperationException>(()=>other.Commit(start));
        Array.Clear(first);if(halfway.LocalPose[0]!=middle)throw new Exception("Prepared transition did not own its source pose.");
        Console.WriteLine("PASS: rigid shortest-arc pose blend/exact endpoints; owned prepare/commit, interruption continuity, stale/foreign and transactional negative gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons!,bundle.Appearance.Skeletons.ToDictionary(s=>s.Path,_=>0));
        var idle=bundle.PreparedAnimation!;var walk=bundle.AdditionalAnimations!["walk"];var run=bundle.AdditionalAnimations["run"];
        var poses=new[]{idle.CreateLocalPose(binding,idle.EndFrame*.25),walk.CreateLocalPose(binding,walk.EndFrame*.25),run.CreateLocalPose(binding,run.EndFrame*.25)};
        int records=0;
        foreach(var pair in new[]{(A:0,B:1),(A:1,B:2),(A:2,B:0)})foreach(float weight in new[]{0,.25f,.5f,.75f,1})
        {
            var output=SkeletalPoseBlender.Blend(poses[pair.A],poses[pair.B],weight);binding.CreateWorldPose(output);
            for(int joint=0;joint<output.Count;joint++)Console.WriteLine("skeletal_pose_blend="+JsonSerializer.Serialize(new{pair=pair.A,joint,weight,first=Values(poses[pair.A][joint]),second=Values(poses[pair.B][joint]),output=Values(output[joint])}));records+=output.Count;
        }
        var interrupted=new SkeletalPoseTransition(poses[0],"idle",.15);interrupted.Commit(interrupted.Prepare("walk",poses[1],.05));var before=interrupted.CurrentPose;var candidate=interrupted.Prepare("run",poses[2],0);
        if(!before.SequenceEqual(candidate.LocalPose))throw new Exception("Original interrupted pose blend lost continuity.");interrupted.Commit(candidate);interrupted.Commit(interrupted.Prepare("run",poses[2],.15));if(!interrupted.CurrentPose.SequenceEqual(poses[2]))throw new Exception("Original transition endpoint mismatch.");
        Console.WriteLine($"PASS: original pose blends matrices={records} rigid_world_composition=true interrupted_continuity=true exact_endpoint=true");
    }
    private static float[] Values(Matrix4x4 m)=>new[]{m.M11,m.M12,m.M13,m.M14,m.M21,m.M22,m.M23,m.M24,m.M31,m.M32,m.M33,m.M34,m.M41,m.M42,m.M43,m.M44};
}
