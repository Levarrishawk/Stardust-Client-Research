using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletonAnimationPoseScenarios
{
    internal static void Run()
    {
        float h=(float)Math.Sqrt(.5);var identity=new ScalarFirstQuaternion(1,0,0,0);
        var root=new SkeletonJoint("Root",-1,new(h,0,0,h),new(h,h,0,0),new(2,3,4),new(h,0,h,0),0);
        var child=new SkeletonJoint("Child",0,identity,identity,new(1,0,0),identity,0);
        var skeleton=new BasicSkeletonDescription(new[]{root,child});
        var samples=new[]{new SkeletalTransformSample("ROOT",new(0,0,h,h),new(.1f,.2f,.3f),false),new SkeletalTransformSample("rifle",Quaternion.Identity,Vector3.Zero,false)};
        var pose=SkeletonAnimationPoseBuilder.Create(skeleton,samples,MissingAnimationTransformPolicy.BindPose);
        Near(Vector3.TransformNormal(Vector3.UnitX,pose.LocalPose[0]),-Vector3.UnitX);
        Near(Vector3.TransformNormal(Vector3.UnitY,pose.LocalPose[0]),-Vector3.UnitY);
        Near(pose.LocalPose[0].Translation,new(2.1f,3.2f,4.3f));
        if(!pose.UnusedAnimationTransforms.SequenceEqual(new[]{"rifle"})||!pose.MissingSkeletonTransforms.SequenceEqual(new[]{"Child"})||pose.LocalPose[1]!=SkeletonBindPoseBuilder.Create(skeleton).LocalBind[1])throw new Exception("Explicit unused/missing pose policy mismatch.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>SkeletonAnimationPoseBuilder.Create(skeleton,samples));
        Reject(()=>SkeletonAnimationPoseBuilder.Create(skeleton,samples.Concat(new[]{samples[0] with{Name="root"}}).ToArray(),MissingAnimationTransformPolicy.BindPose));
        Reject(()=>SkeletonAnimationPoseBuilder.Create(skeleton,new[]{samples[0] with{Rotation=new(0,0,0,2)}},MissingAnimationTransformPolicy.BindPose));
        Reject(()=>SkeletonAnimationPoseBuilder.Create(skeleton,new[]{samples[0] with{Translation=new(float.NaN,0,0)}},MissingAnimationTransformPolicy.BindPose));
        Console.WriteLine("PASS: instruction-backed noncommuting animated local rotation, additive translation, explicit missing/unused names and malformed sample gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        string path=bundle.Appearance.Skeletons[0].Path;var skeleton=bundle.Skeletons![path].ExposedLevels()[0];
        var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons,bundle.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
        var sampler=new CompressedKeyframeSampler(CompressedKeyframeDecoder.Create(bundle.SelectedAnimation!.Clip));
        var prepared=bundle.PreparedAnimation??throw new Exception("Prepared worker animation required.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>PreparedSkeletalAnimation.Create(bundle));
        HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>throw new Exception("Invalid selection reached external lookup."),bundle.Profile.RootPath,new string('0',64),new string('0',64),prepareSelectedAnimation:true));
        foreach(double frame in new[]{0d,30,59.5,90,120})
        {
            var pose=SkeletonAnimationPoseBuilder.Create(skeleton,sampler.SampleFrame(frame),MissingAnimationTransformPolicy.BindPose);
            Console.WriteLine("skeleton_animation_names="+JsonSerializer.Serialize(new{frame,path,unused=pose.UnusedAnimationTransforms,missing=pose.MissingSkeletonTransforms}));
            var local=prepared.CreateLocalPose(binding,frame);var direct=SkeletonAnimationPoseBuilder.Apply(binding,path,skeleton,pose);
            if(!local.SequenceEqual(direct))throw new Exception("Prepared selected pose differs from explicit composition.");var world=binding.CreateWorldPose(local);
            for(int joint=0;joint<world.Count;joint++)Console.WriteLine("skeleton_animation_pose="+JsonSerializer.Serialize(new{frame,joint,path=binding.Joints[joint].SkeletonPath,source=binding.Joints[joint].SourceJoint,local=SkeletonBindPoseScenarios.LegacyValues(local[joint]),world=SkeletonBindPoseScenarios.LegacyValues(world[joint])}));
            foreach(var pair in bundle.Geometries!)
            {
                var palette=binding.CreatePalette(world,binding.Resolve(pair.Value.Skin));
                var batch=SkeletalShaderDeformation.CreateAll(pair.Value,palette);
                for(int shader=0;shader<pair.Value.Shaders.Count;shader++)
                {
                    var deformed=batch[shader];
                    for(int vertex=0;vertex<deformed.Positions.Count;vertex++)
                    {
                        var p=deformed.Positions[vertex];var n=deformed.Normals![vertex];Console.WriteLine("skeleton_animation_vertex="+JsonSerializer.Serialize(new{frame,path=pair.Key,shader,vertex,position=new[]{p.X,p.Y,p.Z},normal=new[]{n.X,n.Y,n.Z}}));
                    }
                }
            }
        }
    }
    private static void Near(Vector3 actual,Vector3 expected){if(Vector3.Distance(actual,expected)>2e-5)throw new Exception("Animated local known-vector mismatch.");}
}
