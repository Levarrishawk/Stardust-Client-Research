using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalShaderDeformationScenarios
{
    internal static void Run()
    {
        var skin=new SkeletalMeshSkinSourceDescription(Array.Empty<int>(),Array.Empty<short>(),Array.Empty<string>(),new[]{"a","b"},
            new[]{new Vector3(1,0,0),new Vector3(0,2,0)},new[]{new SkeletalWeightRange(0,1),new SkeletalWeightRange(1,2)},
            new[]{new SkeletalTransformWeight(0,1),new SkeletalTransformWeight(0,.25f),new SkeletalTransformWeight(1,.75f)},
            new[]{Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ},Array.Empty<IffNode>());
        var shader=new SkeletalShaderGeometry("synthetic",new uint[]{1,0},new uint[]{0,2},new uint[]{1,0},null,Array.Empty<SkeletalTextureCoordinates>(),Array.Empty<SkeletalMeshTriangle>(),Array.Empty<SkeletalMeshTriangle>());
        var geometry=new SkeletalMeshGeometryDescription(skin,new[]{new Vector4(1,0,0,-1),new Vector4(0,1,0,1)},new[]{shader},Array.Empty<SkeletalTextureRendererReference>(),0,0,0);
        var palette=new[]{Matrix4x4.CreateTranslation(3,4,5),Matrix4x4.CreateRotationZ(MathF.PI/2)*Matrix4x4.CreateTranslation(-1,2,0)};
        var result=SkeletalShaderDeformation.Create(geometry,0,palette);
        Near(result.Positions[0],new(-1.5f,3,1.25f));Near(result.Positions[1],new(4,4,5));
        Near(result.Normals![0],new(.25f,.75f,0));Near(result.Normals[1],Vector3.UnitZ);
        Near(new(result.Tangents![0].X,result.Tangents[0].Y,result.Tangents[0].Z),new(-.75f,.25f,0));
        if(result.Tangents[0].W!=1||result.Tangents[1].W!=-1||Math.Abs(result.Normals[0].LengthSquared()-.625f)>1e-5f)throw new Exception("Raw weighted directions/fourth component mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        Reject(()=>SkeletalShaderDeformation.Create(geometry with{Shaders=new[]{shader with{NormalIndices=new uint[]{0}}}},0,palette));
        Reject(()=>SkeletalShaderDeformation.Create(geometry with{Shaders=new[]{shader with{PositionIndices=new uint[]{2,0}}}},0,palette));
        Reject(()=>SkeletalShaderDeformation.Create(geometry with{Shaders=new[]{shader with{NormalIndices=new uint[]{3,0}}}},0,palette));
        Reject(()=>SkeletalShaderDeformation.Create(geometry with{Shaders=new[]{shader with{TangentIndices=new uint[]{2,0}}}},0,palette));
        Reject(()=>SkeletalShaderDeformation.Create(geometry with{Tangents=new[]{new Vector4(1,0,0,float.NaN),Vector4.One}},0,palette));
        var absent=SkeletalShaderDeformation.Create(geometry with{Shaders=new[]{shader with{NormalIndices=null,TangentIndices=null}}},0,palette);
        if(absent.Normals is not null||absent.Tangents is not null)throw new Exception("Absent direction maps must remain absent.");
        var batch=SkeletalShaderDeformation.CreateAll(geometry with{Shaders=new[]{shader,shader with{NormalIndices=null,TangentIndices=null}}},palette);
        Near(batch[0].Positions[0],new(-1.5f,3,1.25f));Near(batch[1].Positions[1],new(4,4,5));Near(batch[0].Normals![0],new(.25f,.75f,0));
        if(batch[1].Normals is not null||batch[1].Tangents is not null)throw new Exception("Batch absent map mismatch.");
        Reject(()=>SkeletalShaderDeformation.CreateAll(geometry with{Shaders=new[]{shader,shader with{NormalIndices=new uint[]{3,0}}}},palette));
        Reject(()=>SkeletalShaderDeformation.CreateAll(geometry with{Shaders=Enumerable.Repeat(shader,2049).ToArray()},palette));
        var singular=palette.ToArray();singular[0]=Matrix4x4.CreateScale(2);Reject(()=>SkeletalShaderDeformation.CreateAll(geometry,singular));
        Console.WriteLine("PASS: complete PIDX-owned shader deformation, distinct normal/tangent maps, translation-free raw weighted directions/fourth component and invalid map gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons!,bundle.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
        foreach(var pair in bundle.Geometries!)
        {
            var map=binding.Resolve(pair.Value.Skin);
            foreach(string mode in new[]{"bind","root-rigid","split-rotation"})
            {
                var worlds=binding.Joints.Select((j,i)=>mode=="bind"?j.WorldBind:j.WorldBind*(mode=="root-rigid"?Matrix4x4.CreateRotationY(.4f)*Matrix4x4.CreateTranslation(.3f,-.2f,.7f):Matrix4x4.CreateRotationZ((i%3)*.15f)*Matrix4x4.CreateTranslation(0,(i%5)*-.02f,0))).ToArray();
                var palette=binding.CreatePalette(worlds,map);
                var batch=SkeletalShaderDeformation.CreateAll(pair.Value,palette);
                for(int shader=0;shader<pair.Value.Shaders.Count;shader++)
                {
                    var result=batch[shader];
                    for(int vertex=0;vertex<result.Positions.Count;vertex++)
                    {
                        var p=result.Positions[vertex];var n=result.Normals![vertex];Vector4? t=result.Tangents?[vertex];
                        Console.WriteLine("skeletal_shader_probe="+JsonSerializer.Serialize(new{path=pair.Key,mode,shader,vertex,position=new[]{p.X,p.Y,p.Z},normal=new[]{n.X,n.Y,n.Z},tangent=t is null?null:new[]{t.Value.X,t.Value.Y,t.Value.Z,t.Value.W}}));
                    }
                }
            }
        }
    }
    private static void Near(Vector3 a,Vector3 b){if(Vector3.Distance(a,b)>2e-5f)throw new Exception("Shader known-vector mismatch: "+a+" vs "+b);}
}
