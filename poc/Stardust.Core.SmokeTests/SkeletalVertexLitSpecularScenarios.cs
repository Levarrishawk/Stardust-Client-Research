using System.Numerics;
using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class SkeletalVertexLitSpecularScenarios
{
    internal static void Run()
    {
        var metadata=IffDocument.Parse(MeshScenarios.Form("TEST")).Root;var program=new SkeletalVertexLitSpecularProgram(true,false,"synthetic",metadata);
        var factors=new SkeletalAlbedoFactors("synthetic",true,0,0,0xFFFFFFFF,0xFFFFFFFF);
        Vector4 Eval(bool bloom=false,bool fade=false)=>SkeletalVertexLitSpecularEvaluator.Evaluate(new(.5f,.5f,.5f,.25f),.5f,factors,program,new(.4f),new(.2f,.4f,.6f),bloom,fade,.7f);
        void Near(Vector4 a,Vector4 b){if(Vector4.Distance(a,b)>1e-6)throw new Exception("Vertex-lit hand fixture differs.");}
        Near(Eval(),new(.3f,.4f,.5f,.181f));Near(Eval(true),new(.3f,.4f,.5f,.11765f));Near(Eval(true,true),new(.3f,.4f,.5f,.7f));
        var other=new SkeletalVertexLitSpecularProgram(false,true,"synthetic",metadata);var noHue=factors with{UsesAlphaHueMask=false};
        Near(SkeletalVertexLitSpecularEvaluator.Evaluate(new(.5f),.5f,noHue,other,new(.4f),new(.2f,.4f,.6f),true),new(.25f,.3f,.35f,.11765f));
        var hdr=SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,1,factors,program,new(2),new(2));if(hdr.X!=4||hdr.W<1.9)throw new Exception("Vertex-lit output was clamped.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,2,factors,program,Vector3.One,Vector3.One));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,1,noHue,program,Vector3.One,Vector3.One));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularProgramDecoder.Decode(MeshScenarios.Form("PSHP",MeshScenarios.Form("0000"))));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff",new string('0',64),new string('0',64),loadVertexLitSpecular:true));if(reads!=0)throw new Exception("Invalid vertex-lit option reached lookup.");
        Console.WriteLine("PASS: vertex-lit specular supplied colors, two-color unscaled bloom RGB, single-color scaling, alpha/fade/HDR and invalid program/input/option gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle,Func<string,TreLookupResult> resolve)
    {
        var source=bundle.MaterialBundle!;var description=source.VertexLitSpecular!;
        SkeletalTangentLightingScenarios.Probe(source,resolve);
        if(description.VertexSources is not null) Console.WriteLine("mouth_vertex_sources="+JsonSerializer.Serialize(description.VertexSources.Sha256));
        foreach(var pair in description.Programs)
        {
            var program=pair.Value;var code=program.Metadata.Children[0].Children[1].Payload;
            Console.WriteLine("skeletal_vertex_lit_program="+JsonSerializer.Serialize(new{path=pair.Key,program.UsesAlphaHueMask,program.ScalesBloomRgb,program.InstructionsSha256,bytecodeSha256=Convert.ToHexString(SHA256.HashData(code.Span))}));
            var bytes=resolve(pair.Key).Payload!;for(int size=0;size<bytes.Length;size++)HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularProgramDecoder.Decode(bytes.AsSpan(0,size)));
            var changed=bytes.ToArray();changed[^5]^=1;HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularProgramDecoder.Decode(changed));
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==pair.Key?new(kind):resolve(p),bundle.Profile.RootPath,bundle.Sources[bundle.Profile.RootPath].Sha256,bundle.Sources[bundle.Profile.AppearancePath].Sha256,loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true,loadAlbedoFactors:true,loadVertexLitSpecular:true));
        }
        foreach(var pair in description.Materials)
        {
            var material=pair.Value;var program=description.Programs[material.PixelProgram];var refs=source.Materials[pair.Key].References.Textures;
            Console.WriteLine("skeletal_vertex_lit_material="+JsonSerializer.Serialize(new{path=pair.Key,material.PixelProgram,material.TextureTag,material.CoordinateSet,power=material.Material.SpecularPower}));
            for(int i=0;i<64;i++)
            {
                object Pixel(uint tag,out Vector4 sample)
                {
                    string path=refs.Single(t=>t.Tag==tag).AssetPath!;var mip=source.Textures[path].Mips[0];int x=(int)((i%8+.5)/8*mip.Width),y=(int)((i/8+.5)/8*mip.Height),index=y*mip.Width+x;var rgba=mip.Rgba.Span.Slice(index*4,4).ToArray();sample=new(rgba[0]/255f,rgba[1]/255f,rgba[2]/255f,rgba[3]/255f);return new{path,index,rgba};
                }
                var diffuse=Pixel(0x4D41494E,out var sample);var mask=Pixel(material.TextureTag,out var specular);
                var vd=new Vector3(.1f+(i%5)*.2f,.3f,.7f);var vs=new Vector3(.05f,.1f+(i%3)*.15f,.2f);bool bloom=i%2==0,fade=i%4==0;
                var factors=source.AlbedoFactors!.Materials[pair.Key];var output=SkeletalVertexLitSpecularEvaluator.Evaluate(sample,specular.W,factors,program,vd,vs,bloom,fade,.6f);
                Console.WriteLine("skeletal_vertex_lit_sample="+JsonSerializer.Serialize(new{path=pair.Key,index=i,diffuse,mask,vertexDiffuse=new[]{vd.X,vd.Y,vd.Z},vertexSpecular=new[]{vs.X,vs.Y,vs.Z},BloomEnabled=bloom,AlphaFadeEnabled=fade,AlphaFadeOpacity=.6f,first=factors.FirstArgb,second=factors.SecondArgb,output=new[]{output.X,output.Y,output.Z,output.W}}));
            }
        }
    }
}
