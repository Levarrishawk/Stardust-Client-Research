using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalSpecularScenarios
{
    internal static void Run()
    {
        var scalars=Enumerable.Range(1,17).Select(i=>(float)i).ToArray();var fields=StaticShaderMaterialDecoder.Decode(scalars);scalars[0]=999;
        if(fields.AmbientRgba!=new Vector4(2,3,4,1)||fields.DiffuseRgba!=new Vector4(6,7,8,5)||fields.EmissiveRgba!=new Vector4(10,11,12,9)||fields.SpecularRgba!=new Vector4(14,15,16,13)||fields.VertexConstants()[2]!=fields.SpecularRgba||fields.PixelPowerConstant!=new Vector4(17,0,0,0))throw new Exception("Asymmetric MATL/constant order differs.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>StaticShaderMaterialDecoder.Decode(new float[16]));scalars[0]=float.NaN;Reject(()=>StaticShaderMaterialDecoder.Decode(scalars));
        var material=new StaticShaderMaterial(Vector4.One,Vector4.One,Vector4.Zero,new(.2f,.3f,.4f,1),20);
        var factors=new SkeletalAlbedoFactors("synthetic",false,0,0,0xFFFFFFFF,0xFFFFFFFF);
        var light=new SkeletalPixelLighting(-Vector3.UnitZ,Vector3.UnitZ,Vector3.One,Vector3.One,new(.25f),Vector3.Zero);
        Vector4 Eval(float mask=1,SkeletalPixelLighting? lights=null,StaticShaderMaterial? mat=null)=>SkeletalSpecularEvaluator.Evaluate(Vector4.One,new(0,.5f,0,.5f),mask,factors,mat??material,lights??light);
        void Near(Vector4 a,Vector4 b){if(Vector4.Distance(a,b)>1e-6)throw new Exception("Specular hand-calculated fixture differs.");}
        Near(Eval(),new(.45f,.55f,.65f,.281f)); // Negative light direction does not gate the compiled specular term.
        Near(Eval(0),new(.25f,.25f,.25f,0));
        Near(Eval(lights:light with{BloomEnabled=true}),new(.35f,.4f,.45f,.18265f));
        Near(Eval(lights:light with{BloomEnabled=true,AlphaFadeEnabled=true,AlphaFadeOpacity=.7f}),new(.35f,.4f,.45f,.7f));
        var hdr=Eval(lights:light with{VertexSpecular=new(2)});if(hdr.X<=1||hdr.W<=1)throw new Exception("Specular output was prematurely clamped.");
        Reject(()=>Eval(float.NaN));Reject(()=>Eval(lights:light with{TangentHalfAngle=Vector3.Zero}));Reject(()=>Eval(mat:material with{SpecularPower=0}));Reject(()=>Eval(mat:material with{SpecularPower=-1}));
        Console.WriteLine("PASS: asymmetric MATL ownership/ARGB/constant order and specular mask/bloom/fade/HDR/no-light-gate fixtures; negative input gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var source=bundle.MaterialBundle!;
        float[] V(Vector4 v)=>new[]{v.X,v.Y,v.Z,v.W};float[] D(Vector3 v)=>new[]{v.X,v.Y,v.Z};
        foreach(var pair in source.Materials)
        {
            var raw=pair.Value.Parameters.Materials[0x4D41494E];var material=StaticShaderMaterialDecoder.Decode(raw);
            Console.WriteLine("skeletal_material_fields="+JsonSerializer.Serialize(new{path=pair.Key,tag=0x4D41494E,raw,ambient=V(material.AmbientRgba),diffuse=V(material.DiffuseRgba),emissive=V(material.EmissiveRgba),specular=V(material.SpecularRgba),power=material.SpecularPower,vertexConstants=material.VertexConstants().Select(V),pixelSpecular=V(material.PixelSpecularConstant),pixelPower=V(material.PixelPowerConstant)}));
        }
        var descriptions=SkeletalSpecularMaterials.Describe(source);
        var firstShader=source.Materials.First(p=>descriptions.ContainsKey(p.Key));
        var invalidMaterials=source.Materials.ToDictionary(p=>p.Key,p=>p.Value);
        invalidMaterials[firstShader.Key]=firstShader.Value with{Parameters=firstShader.Value.Parameters with{Materials=new Dictionary<uint,IReadOnlyList<float>>()}};
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalSpecularMaterials.Describe(source with{Materials=invalidMaterials}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalSpecularMaterials.Describe(source with{CompressedNormals=null}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalSpecularMaterials.Describe(source with{Textures=new Dictionary<string,DdsImage>()}));
        var effectPath=firstShader.Value.References.EffectPath;var effect=source.Effects[effectPath];var program=descriptions[firstShader.Key].PixelProgram;
        var badEffects=source.Effects.ToDictionary(p=>p.Key,p=>p.Value);
        badEffects[effectPath]=effect with{Implementations=effect.Implementations.Select(i=>i with{Passes=i.Passes.Select(p=>p.PixelProgram==program?p with{PixelTextures=p.PixelTextures.Where(t=>t.Stage!=2).ToArray()}:p).ToArray()}).ToArray()};
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalSpecularMaterials.Describe(source with{Effects=badEffects}));
        foreach(var pair in descriptions)
        {
            var description=pair.Value;var refs=source.Materials[pair.Key].References.Textures;
            string Path(uint tag)=>refs.Single(t=>t.Tag==tag).AssetPath!;
            Console.WriteLine("skeletal_specular_material="+JsonSerializer.Serialize(new{path=pair.Key,description.PixelProgram,description.SpecularTextureTag,description.MaskUsesMainAlpha}));
            for(int i=0;i<64;i++)
            {
                object Pixel(string path,out Vector4 sample)
                {
                    var mip=source.Textures[path].Mips[0];int x=(int)((i%8+.5)/8*mip.Width),y=(int)((i/8+.5)/8*mip.Height),index=y*mip.Width+x;var rgba=mip.Rgba.Span.Slice(index*4,4).ToArray();sample=new(rgba[0]/255f,rgba[1]/255f,rgba[2]/255f,rgba[3]/255f);return new{path,index,rgba};
                }
                var diffuse=Pixel(Path(0x4D41494E),out var ds);var normal=Pixel(Path(0x434E524D),out var ns);var mask=Pixel(Path(description.SpecularTextureTag),out var ss);
                var lighting=new SkeletalPixelLighting(Vector3.Normalize(new((i%3)-1,.5f,(i%5)-2)),new((i%7)-3,.7f,2),new(.7f,.5f,.3f),new(.8f,.6f,.4f),new(.2f,.15f,.1f),new(.05f,.02f,.01f),i%2==0,i%4==0,.6f);
                var factors=source.AlbedoFactors!.Materials[pair.Key];var output=SkeletalSpecularEvaluator.Evaluate(ds,ns,ss.W,factors,description.Material,lighting);
                Console.WriteLine("skeletal_specular_sample="+JsonSerializer.Serialize(new{path=pair.Key,index=i,diffuse,normal,mask,light=D(lighting.TangentLightDirection),half=D(lighting.TangentHalfAngle),diffuseLight=D(lighting.Dot3DiffuseColor),specularLight=D(lighting.Dot3SpecularColor),vertexDiffuse=D(lighting.VertexDiffuse),vertexSpecular=D(lighting.VertexSpecular),lighting.BloomEnabled,lighting.AlphaFadeEnabled,lighting.AlphaFadeOpacity,first=factors.FirstArgb,second=factors.SecondArgb,hue=factors.UsesAlphaHueMask,output=V(output)}));
            }
        }
    }
}
