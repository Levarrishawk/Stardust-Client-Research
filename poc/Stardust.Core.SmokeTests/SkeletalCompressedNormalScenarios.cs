using System.Numerics;
using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class SkeletalCompressedNormalScenarios
{
    internal static void Run()
    {
        void Near(Vector3 a,Vector3 b){if(Vector3.Distance(a,b)>1e-6)throw new Exception("Compressed-normal known vector differs.");}
        Near(SkeletalCompressedNormalEvaluator.Decode(new(0,.5f,1,.5f)),Vector3.UnitZ);
        Near(SkeletalCompressedNormalEvaluator.Decode(new(1,.5f,0,1)),Vector3.UnitX);
        Near(SkeletalCompressedNormalEvaluator.Decode(new(1,1,0,.5f)),Vector3.UnitY);
        Near(SkeletalCompressedNormalEvaluator.Decode(new(0,1,0,1)),Vector3.Normalize(Vector3.One));
        Near(SkeletalCompressedNormalEvaluator.ToObject(Vector3.UnitY,Vector3.UnitZ,new(1,0,0,-1)),-Vector3.UnitY);
        Near(SkeletalCompressedNormalEvaluator.ToObject(Vector3.UnitY,-Vector3.UnitZ,new(1,0,0,1)),-Vector3.UnitY);
        Near(SkeletalDiffuseEvaluator.Evaluate(Vector3.One,Vector3.UnitZ,Vector3.UnitZ,new(.2f),new(.6f)),new(.8f));
        Near(SkeletalDiffuseEvaluator.Evaluate(new(.5f),Vector3.UnitZ,-Vector3.UnitZ,new(.2f),new(.6f)),new(.1f));
        Near(SkeletalDiffuseEvaluator.Evaluate(Vector3.One,Vector3.UnitZ,Vector3.UnitZ,new(.8f),new(.6f)),Vector3.One);
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalDiffuseEvaluator.Evaluate(Vector3.One,Vector3.Zero,Vector3.UnitZ,Vector3.Zero,Vector3.One));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalCompressedNormalEvaluator.Decode(new(0,float.NaN,0,0)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalCompressedNormalEvaluator.Decode(new(0,0,0,2)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalCompressedNormalProgramDecoder.Decode(MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",new byte[]{65,0}),MeshScenarios.Chunk("PEXE",BitConverter.GetBytes(0xFFFF0200u).Concat(BitConverter.GetBytes(0xFFFFu)).ToArray())))));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff",new string('0',64),new string('0',64),loadCompressedNormals:true));if(reads!=0)throw new Exception("Invalid compressed-normal option reached lookup.");
        Console.WriteLine("PASS: compressed alpha/green normal known vectors, absolute radicand, reflected tangent handedness and invalid sample/program/option gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle,Func<string,TreLookupResult> resolve)
    {
        var materials=bundle.MaterialBundle!;var normals=materials.CompressedNormals!;
        foreach(var pair in normals.Materials)Console.WriteLine("skeletal_compressed_normal="+JsonSerializer.Serialize(new{path=pair.Key,description=pair.Value}));
        foreach(var pair in normals.Programs)
        {
            var program=pair.Value;var code=program.Metadata.Children[0].Children[1].Payload;
            Console.WriteLine("skeletal_normal_program="+JsonSerializer.Serialize(new{path=pair.Key,program.UsesAlphaHueMask,program.InstructionsSha256,bytecodeSha256=Convert.ToHexString(SHA256.HashData(code.Span))}));
            var bytes=resolve(pair.Key).Payload!;for(int i=0;i<bytes.Length;i++)HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalCompressedNormalProgramDecoder.Decode(bytes.AsSpan(0,i)));
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==pair.Key?new(kind):resolve(p),bundle.Profile.RootPath,bundle.Sources[bundle.Profile.RootPath].Sha256,bundle.Sources[bundle.Profile.AppearancePath].Sha256,loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true,loadAlbedoFactors:true,loadCompressedNormals:true));
            var changed=code.ToArray();changed[^8]^=1;
            var malformed=MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",program.Metadata.Children[0].Children[0].Payload.ToArray()),MeshScenarios.Chunk("PEXE",changed)));
            HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalCompressedNormalProgramDecoder.Decode(malformed));
        }
        var paths=normals.Materials.Keys.SelectMany(path=>materials.Materials[path].References.Textures.Where(t=>t.Tag==0x434E524D).Select(t=>t.AssetPath!))
            .Concat(materials.Materials.Values.SelectMany(m=>m.Customization!.TextureChoices)).Distinct(StringComparer.Ordinal);
        foreach(string path in paths)
        {
            var image=materials.Textures[path];long outside=0;
            for(int mip=0;mip<image.Mips.Count;mip++)
            {
                var rgba=image.Mips[mip].Rgba.Span;int pixels=rgba.Length/4;
                for(int i=0;i<pixels;i++){double x=rgba[i*4+3]/255d*2-1,y=rgba[i*4+1]/255d*2-1;if(x*x+y*y>1)outside++;}
                var indices=Enumerable.Range(0,17).Select(i=>(int)((long)i*(pixels-1)/16)).ToHashSet();
                for(int i=0;i<pixels;i++){double x=rgba[i*4+3]/255d*2-1,y=rgba[i*4+1]/255d*2-1;if(x*x+y*y>1){indices.Add(i);break;}}
                foreach(int index in indices.Order())
                {
                    var sample=new Vector4(rgba[index*4]/255f,rgba[index*4+1]/255f,rgba[index*4+2]/255f,rgba[index*4+3]/255f);var normal=SkeletalCompressedNormalEvaluator.Decode(sample);
                    Console.WriteLine("skeletal_normal_sample="+JsonSerializer.Serialize(new{path,mip,index,rgba=new[]{rgba[index*4],rgba[index*4+1],rgba[index*4+2],rgba[index*4+3]},normal=new[]{normal.X,normal.Y,normal.Z}}));
                }
            }
            Console.WriteLine("skeletal_normal_image="+JsonSerializer.Serialize(new{path,outsideDisk=outside}));
        }
    }
}
