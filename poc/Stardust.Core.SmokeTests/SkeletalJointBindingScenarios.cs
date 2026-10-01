using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalJointBindingScenarios
{
    internal static void Run()
    {
        var identity=new ScalarFirstQuaternion(1,0,0,0);float h=(float)Math.Sqrt(.5);
        SkeletonJoint Joint(string name,int parent,Vector3 offset)=>new(name,parent,identity,identity,offset,identity,0);
        var body=new BasicSkeletonDescription(Array.AsReadOnly(new[]{Joint("Root",-1,new(2,0,0)) with{Rpst=new(h,0,0,h)},Joint("Head",0,new(1,0,0))}));
        var face=new BasicSkeletonDescription(Array.AsReadOnly(new[]{Joint("Face",-1,new(0,.5f,0))}));
        var references=new[]{new SkeletalSkeletonReference("appearance/skeleton/body.skt",""),new SkeletalSkeletonReference("appearance/skeleton/face.skt","head")};
        var appearance=new SkeletalAppearanceDescription(true,Array.Empty<string>(),references,Array.Empty<SkeletalAnimationReference>(),Array.Empty<SkeletalDistanceInterval>(),null,null);
        var skeletons=new Dictionary<string,SkeletonTemplateDescription>{{references[0].Path,new(new[]{body})},{references[1].Path,new(new[]{face})}};
        var levels=references.ToDictionary(r=>r.Path,_=>0);var binding=SkeletalJointBindingBuilder.Create(appearance,skeletons,levels);
        Near(binding.Joints[1].WorldBind.Translation,new(2,1,0));Near(binding.Joints[2].WorldBind.Translation,new(1.5f,1,0));
        if(binding.Joints[2].Parent!=1)throw new Exception("Attached global parent mismatch.");
        var mesh=new SkeletalMeshSkinSourceDescription(Array.Empty<int>(),Array.Empty<short>(),references.Select(r=>r.Path).ToArray(),new[]{"head","FACE"},new[]{new Vector3(2,1,0)},new[]{new SkeletalWeightRange(0,2)},new[]{new SkeletalTransformWeight(0,.25f),new SkeletalTransformWeight(1,.75f)},Array.Empty<Vector3>(),Array.Empty<IffNode>());
        var map=binding.Resolve(mesh);if(!map.SequenceEqual(new[]{1,2}))throw new Exception("Scoped folded transform resolution mismatch.");
        var worlds=binding.Joints.Select(j=>j.WorldBind).ToArray();var palette=binding.CreatePalette(worlds,map);Near(SkeletalJointBinding.DeformPositions(mesh,palette)[0],mesh.Positions[0]);
        var locals=binding.Joints.Select(j=>j.LocalBind).ToArray();Near(binding.CreateWorldPose(locals)[2].Translation,new(1.5f,1,0));
        locals[1]=Matrix4x4.CreateRotationZ(MathF.PI/2)*locals[1];Near(binding.CreateWorldPose(locals)[2].Translation,new(2,.5f,0));
        worlds[1]*=Matrix4x4.CreateTranslation(4,0,0);worlds[2]*=Matrix4x4.CreateTranslation(0,2,0);
        Near(SkeletalJointBinding.DeformPositions(mesh,binding.CreatePalette(worlds,map))[0],new(3,2.5f,0));
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>binding.Resolve(mesh with{TransformNames=new[]{"absent"}}));Reject(()=>binding.Resolve(mesh with{SkeletonPaths=new[]{"missing"}}));
        Reject(()=>binding.Resolve(mesh with{SkeletonPaths=new[]{references[0].Path,references[0].Path}}));
        Reject(()=>SkeletalJointBindingBuilder.Create(appearance,skeletons,new Dictionary<string,int>()));
        Reject(()=>SkeletalJointBindingBuilder.Create(appearance with{Skeletons=references.Reverse().ToArray()},skeletons,levels));
        Reject(()=>SkeletalJointBindingBuilder.Create(appearance with{Skeletons=new[]{references[0],references[1] with{AttachmentTransform="absent"}}},skeletons,levels));
        var ambiguous=new SkeletalJointBinding(binding.Joints.Concat(new[]{binding.Joints[1]}).ToArray());Reject(()=>ambiguous.Resolve(mesh));
        Reject(()=>binding.CreatePalette(worlds,new[]{53}));Reject(()=>binding.CreatePalette(worlds.Take(2).ToArray(),map));
        var invalid=worlds.ToArray();invalid[0]=Matrix4x4.CreateScale(2);Reject(()=>binding.CreatePalette(invalid,map));
        Reject(()=>binding.CreateWorldPose(invalid));Reject(()=>binding.CreateWorldPose(locals.Take(2).ToArray()));
        Reject(()=>SkeletalJointBinding.DeformPositions(mesh with{WeightRanges=new[]{new SkeletalWeightRange(1,2)}},palette));
        Reject(()=>SkeletalJointBinding.DeformPositions(mesh with{Weights=new[]{new SkeletalTransformWeight(0,float.NaN),new SkeletalTransformWeight(1,.75f)}},palette));
        Reject(()=>SkeletalJointBinding.DeformPositions(mesh with{Weights=new[]{new SkeletalTransformWeight(0,.1f),new SkeletalTransformWeight(1,.75f)}},palette));
        Console.WriteLine("PASS: explicit attached skeleton palette, scoped folded names, noncommuting world binds, bind identity, weighted modern position diagnostics and malformed topology/pose/weight gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var levels=bundle.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0);var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons!,levels);
        for(int i=0;i<binding.Joints.Count;i++){var j=binding.Joints[i];Console.WriteLine("skeletal_joint_binding="+JsonSerializer.Serialize(new{joint=i,path=j.SkeletonPath,source=j.SourceJoint,parent=j.Parent,name=j.Name,world=SkeletonBindPoseScenarios.LegacyValues(j.WorldBind),inverse=SkeletonBindPoseScenarios.LegacyValues(j.InverseBind)}));}
        foreach(var pair in bundle.SkinSources!)
        {
            var map=binding.Resolve(pair.Value);Console.WriteLine("skeletal_transform_binding="+JsonSerializer.Serialize(new{path=pair.Key,joints=map}));
            foreach(string mode in new[]{"bind","root-rigid","split"})
            {
                var worlds=binding.Joints.Select(j=>j.WorldBind).ToArray();
                if(mode=="root-rigid")for(int i=0;i<worlds.Length;i++)worlds[i]*=Matrix4x4.CreateRotationY(.4f)*Matrix4x4.CreateTranslation(.3f,-.2f,.7f);
                if(mode=="split")for(int i=0;i<worlds.Length;i++)worlds[i]*=Matrix4x4.CreateTranslation((i%3)*.01f,(i%5)*-.02f,0);
                var result=SkeletalJointBinding.DeformPositions(pair.Value,binding.CreatePalette(worlds,map));
                for(int i=0;i<result.Count;i++)Console.WriteLine("skeletal_position_probe="+JsonSerializer.Serialize(new{path=pair.Key,mode,position=i,value=new[]{result[i].X,result[i].Y,result[i].Z}}));
                if(mode=="bind"&&result.Select((p,i)=>Vector3.Distance(p,pair.Value.Positions[i])).Max()>2e-5f)throw new Exception("Original bind position diagnostic mismatch.");
            }
        }
    }
    private static void Near(Vector3 a,Vector3 b){if(Vector3.Distance(a,b)>2e-5f)throw new Exception("Binding known-vector mismatch.");}
}
