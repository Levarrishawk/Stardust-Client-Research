using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalLowerDetailShadingScenarios
{
    internal static void Run(string[] args)
    {
        if(args.Length!=2)throw new ArgumentException("Usage: Assets ROOT CONFIGURATION");
        if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])))!="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524")throw new InvalidDataException("Configuration pin mismatch.");
        using var config=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],config);
        const string root="object/creature/player/shared_human_male.iff",objectPin="E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243",satPin="951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C",shaderPath="shader/hum_m_body_sm.sht",effectPath="effect/h_specmap.eft",texturePath="texture/hum_m_sm.dds",programPath="pixel_program/h_specmap_ps20_nodot3.psh";
        CreatureAppearanceDescriptorBundle Load(Func<string,TreLookupResult> read,int detail,AppearanceGraphLimits? limits=null)=>CreatureAppearanceDescriptorBundleReader.Load(read,root,objectPin,satPin,limits??new(MaximumResources:64,MaximumSourceBytes:16*1024*1024),loadSkeletons:true,loadSkinSources:true,meshDetail:detail,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true,loadAlbedoFactors:true,loadVertexLitSpecular:true,loadCompressedNormals:false);
        void Reject<T>(Action a) where T:Exception {try{a();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
        float[] V(Vector4 v)=>new[]{v.X,v.Y,v.Z,v.W};
        float[] D(Vector3 v)=>new[]{v.X,v.Y,v.Z};
        IEnumerable<IffNode> Nodes(IffNode n){yield return n;foreach(var child in n.Children)foreach(var item in Nodes(child))yield return item;}
        for(int detail=2;detail<=3;detail++)
        {
            var counts=new Dictionary<string,int>(StringComparer.Ordinal);var borrowed=new List<byte[]>();
            TreLookupResult Read(string p){counts[p]=counts.GetValueOrDefault(p)+1;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;}
            var bundle=Load(Read,detail);var materialBundle=bundle.MaterialBundle??throw new Exception("Material absent.");var description=materialBundle.VertexLitSpecular??throw new Exception("Program absent.");var shader=materialBundle.Materials[shaderPath];var material=description.Materials[shaderPath];var program=description.Programs[programPath];var factors=materialBundle.AlbedoFactors!.Materials[shaderPath];
            if(materialBundle.Materials.Count!=1||materialBundle.Textures.Count!=1||program.UsesAlphaHueMask||!program.ScalesBloomRgb||factors.UsesAlphaHueMask||counts.Values.Any(n=>n!=1)||bundle.Geometries!.Values.Any(g=>g.Tangents.Count!=0))throw new Exception("Lower-detail family/read-once mismatch.");
            if(shader.References.Textures.Count!=2||shader.References.Textures.Any(t=>t.AssetPath!=texturePath)||!shader.References.Textures.Select(t=>t.Tag).ToHashSet().SetEquals(new[]{0x4D41494Eu,0x53504543u}))throw new Exception("MAIN/SPEC source identity differs.");
            foreach(var source in bundle.Sources.Values)Console.WriteLine("lower_shading_source="+JsonSerializer.Serialize(new{detail,source}));
            Console.WriteLine("lower_shading_descriptor="+JsonSerializer.Serialize(new{detail,bundle.Profile.RootPath,bundle.Profile.AppearancePath,bundle.Appearance,bundle.LodMeshes,counts}));
            Console.WriteLine("lower_shading_materials_begin="+detail);SkeletalDescriptorScenarios.DumpMaterials(materialBundle);Console.WriteLine("lower_shading_materials_end="+detail);
            foreach(var pair in bundle.Geometries!)Console.WriteLine("lower_shading_geometry="+JsonSerializer.Serialize(new{detail,path=pair.Key,sha256=SkeletalGeometryScenarios.CanonicalHash(pair.Value),tangents=pair.Value.Tangents.Count}));
            Console.WriteLine("lower_shading_material="+JsonSerializer.Serialize(new{detail,path=shaderPath,material.PixelProgram,material.TextureTag,material.CoordinateSet,raw=shader.Parameters.Materials[0x4D41494E],ambient=V(material.Material.AmbientRgba),diffuse=V(material.Material.DiffuseRgba),emissive=V(material.Material.EmissiveRgba),specular=V(material.Material.SpecularRgba),power=material.Material.SpecularPower,factors,sourceIdentity=description.VertexSources!.Sha256,program.UsesAlphaHueMask,program.ScalesBloomRgb,program.InstructionsSha256,bytecodeSha256=Convert.ToHexString(SHA256.HashData(program.Metadata.Children[0].Children[1].Payload.Span)),bindingMetadata=shader.References.Textures.Select(t=>new{t.Tag,t.Version,metadata=Convert.ToHexString(t.BindingMetadata.Span)}),albedoPrograms=materialBundle.AlbedoFactors.Programs.Keys}));
            foreach(var impl in materialBundle.Effects[effectPath].Implementations)Console.WriteLine("lower_shading_implementation="+JsonSerializer.Serialize(new{detail,impl.Capabilities,impl.Options,impl.PhaseTag,impl.Flag5,impl.Flag6,impl.Version,passes=impl.Passes.Count}));
            foreach(var impl in materialBundle.Effects[effectPath].Implementations)foreach(var pass in impl.Passes)Console.WriteLine("lower_shading_pass="+JsonSerializer.Serialize(new{detail,impl.Capabilities,impl.Options,impl.PhaseTag,impl.Flag5,impl.Flag6,impl.Version,pass.VertexProgram,pass.PixelProgram,passVersion=pass.Version,pass.FixedStageCount,fixedVertexLighting=Convert.ToHexString(pass.FixedVertexLighting.Span),fixedStages=pass.FixedStages.Select(t=>new{t.TextureTag,t.CoordinateTag,metadata=Convert.ToHexString(t.Metadata.Span)}),pass.Fields,textures=pass.PixelTextures.Select(t=>new{t.Stage,t.TextureTag,t.Version,metadata=Convert.ToHexString(t.Metadata.Span)})}));
            var geometryHashes=bundle.Geometries!.ToDictionary(p=>p.Key,p=>SkeletalGeometryScenarios.CanonicalHash(p.Value));var image=materialBundle.Textures[texturePath];var pixelHashes=image.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span))).ToArray();var before=Convert.ToHexString(SHA256.HashData(program.Metadata.Children[0].Children[1].Payload.Span));
            if(detail==2)
            {
                for(int i=0;i<64;i++)
                {
                    var mip=image.Mips[0];int x=(i%8)*mip.Width/8+mip.Width/16,y=(i/8)*mip.Height/8+mip.Height/16,index=y*mip.Width+x;var rgba=mip.Rgba.Span.Slice(index*4,4).ToArray();var sample=new Vector4(rgba[0]/255f,rgba[1]/255f,rgba[2]/255f,rgba[3]/255f);var vd=new Vector3((i%7)*.17f,(i%5)*.23f,.2f+(i%3)*.31f);var vs=new Vector3(.1f+(i%4)*.3f,(i%5)*.19f,.2f+(i%7)*.21f);bool bloom=(i&1)!=0,fade=(i&2)!=0;float opacity=(i%9)/8f;
                    var output=SkeletalVertexLitSpecularEvaluator.Evaluate(sample,sample.W,factors,program,vd,vs,bloom,fade,opacity);
                    Console.WriteLine("lower_shading_sample="+JsonSerializer.Serialize(new{path=shaderPath,index=i,texture=texturePath,pixel=index,rgba,vertexDiffuse=D(vd),vertexSpecular=D(vs),bloom,fade,opacity,first=factors.FirstArgb,output=V(output)}));
                }
                Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,1,factors with{UsesAlphaHueMask=true},program,Vector3.One,Vector3.One));
                Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,float.NaN,factors,program,Vector3.One,Vector3.One));
                Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(new(float.NaN),1,factors,program,Vector3.One,Vector3.One));
                Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,1,factors,program,Vector3.One,new(float.PositiveInfinity)));
                Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,1,factors,program,Vector3.One,Vector3.One,true,true,2));
            }
            foreach(string path in new[]{shaderPath,effectPath,texturePath,programPath}.Concat(materialBundle.AlbedoFactors.Programs.Keys).Concat(description.VertexSources!.Sha256.Keys))
            {
                foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})Reject<InvalidDataException>(()=>Load(p=>p==path?new(kind):overlay.Lookup(p),detail));
                if(path==programPath||description.VertexSources.Sha256.ContainsKey(path))
                {
                    byte[] changed=overlay.Lookup(path).Payload!.ToArray();changed[path==programPath?changed.Length-5:0]^=1;
                    Reject<InvalidDataException>(()=>Load(p=>p==path?new(TreLookupKind.Present,Payload:changed):overlay.Lookup(p),detail));
                    Reject<InvalidDataException>(()=>Load(p=>p==path?new(TreLookupKind.Present,Payload:new byte[65537]):overlay.Lookup(p),detail));
                }
            }
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})
            {
                var paletteReads=new Dictionary<string,int>(StringComparer.Ordinal);
                var fallback=Load(p=>{paletteReads[p]=paletteReads.GetValueOrDefault(p)+1;return p=="palette/pc_skin_hum.pal"?new(kind):overlay.Lookup(p);},detail);
                var palette=fallback.MaterialBundle!.PaletteDefaults![shaderPath];
                if(fallback.MaterialBundle.AlbedoFactors!.Materials[shaderPath].FirstArgb!=0xFF00FFFF||!palette.AuthoredFactors[0].MissingPalette||!palette.MissingPalettePaths.Contains("palette/pc_skin_hum.pal")||paletteReads.Values.Any(n=>n!=1)||fallback.Sources.Count!=bundle.Sources.Count-1)throw new Exception("Explicit missing-palette fallback/read-once policy differs.");
            }
            Reject<InvalidDataException>(()=>Load(p=>p=="palette/pc_skin_hum.pal"?new(TreLookupKind.Present,Payload:new byte[24]):overlay.Lookup(p),detail));
            byte[] bytes=overlay.Lookup(programPath).Payload!;
            foreach(int size in new[]{0,8,bytes.Length-1})Reject<InvalidDataException>(()=>SkeletalVertexLitSpecularProgramDecoder.Decode(bytes.AsSpan(0,size)));
            byte[] shaderBytes=overlay.Lookup(shaderPath).Payload!;var shaderRoot=IffDocument.Parse(shaderBytes).Root;
            foreach(string target in new[]{"TFNS","TCSS","MATS"})
            {
                var form=Nodes(shaderRoot).Single(n=>n.IsForm(target));var node=target=="MATS"?form.Children[0].Children[0]:form.Children[0];var changed=shaderBytes.ToArray();int offset=node.Offset+8;
                if(target=="TFNS")changed[node.Offset+3]=49;else if(target=="TCSS")changed[offset+4]=1;else BinaryPrimitives.WriteUInt32LittleEndian(changed.AsSpan(offset),0x53504543);
                Reject<InvalidDataException>(()=>Load(p=>p==shaderPath?new(TreLookupKind.Present,Payload:changed):overlay.Lookup(p),detail));
            }
            var selectedPass=materialBundle.Effects[effectPath].Implementations.SelectMany(i=>i.Passes).Single(p=>p.PixelProgram==programPath);var sampler=Nodes(selectedPass.Metadata).Single(n=>n.Tag=="0002"&&n.Payload.Span[0]==1);var changedEffect=overlay.Lookup(effectPath).Payload!.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(changedEffect.AsSpan(sampler.Offset+9),0x4D41494E);
            Reject<InvalidDataException>(()=>Load(p=>p==effectPath?new(TreLookupKind.Present,Payload:changedEffect):overlay.Lookup(p),detail));
            Reject<AppearanceGraphBudgetException>(()=>Load(overlay.Lookup,detail,new(MaximumResources:bundle.Sources.Count-1,MaximumSourceBytes:16*1024*1024)));
            Reject<AppearanceGraphBudgetException>(()=>Load(overlay.Lookup,detail,new(MaximumResources:64,MaximumSourceBytes:bundle.Sources.Values.Sum(s=>(long)s.Bytes)-1)));
            var ownedResult=SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,.4f,factors,program,new(.3f),new(.7f),true);
            foreach(var source in borrowed)Array.Clear(source);
            if(before!=Convert.ToHexString(SHA256.HashData(program.Metadata.Children[0].Children[1].Payload.Span))||ownedResult!=SkeletalVertexLitSpecularEvaluator.Evaluate(Vector4.One,.4f,factors,program,new(.3f),new(.7f),true)||!pixelHashes.SequenceEqual(image.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span)))))throw new Exception("Borrowed source mutation changed material/program/pixels.");
            foreach(var pair in bundle.Geometries!)if(geometryHashes[pair.Key]!=SkeletalGeometryScenarios.CanonicalHash(pair.Value))throw new Exception("Borrowed source mutation changed geometry.");
        }
        Console.WriteLine("PASS R3-ASSETS-01: original details2/3 shared single-color nodot3 closure,64 shared pixel fixtures, source/program/tag/budget/ownership gates; CPU only.");
    }
}
