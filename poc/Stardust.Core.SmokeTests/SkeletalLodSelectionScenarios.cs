using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalLodSelectionScenarios
{
    const string ObjectPath="object/creature/player/shared_human_male.iff";
    const string ObjectPin="E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243";
    const string SatPin="951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C";
    const string ConfigPin="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";
    internal static void Run(string[] args,int maximumDetail=1)
    {
        if(maximumDetail is not (1 or 3))throw new ArgumentOutOfRangeException(nameof(maximumDetail));
        if(args.Length!=2)throw new ArgumentException("Usage: Assets ROOT CONFIGURATION");
        if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])))!=ConfigPin)throw new InvalidDataException("Configuration pin mismatch.");
        using var config=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],config);
        CreatureAppearanceDescriptorBundle Load(Func<string,TreLookupResult> read,int detail,AppearanceGraphLimits? limits=null)=>
            CreatureAppearanceDescriptorBundleReader.Load(read,ObjectPath,ObjectPin,SatPin,limits??new(MaximumResources:128,MaximumSourceBytes:32*1024*1024),
                loadSkeletons:true,loadSkinSources:true,meshDetail:detail,loadGeometry:true,loadMaterialDefaults:true);
        void Reject<T>(Action a) where T:Exception {try{a();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
        int invalidReads=0;
        foreach(int detail in new[]{-1,4})Reject<ArgumentOutOfRangeException>(()=>Load(p=>{invalidReads++;return overlay.Lookup(p);},detail));
        if(invalidReads!=0)throw new Exception("Invalid detail performed lookup.");
        var selected=new List<HashSet<string>>();
        for(int detail=0;detail<=maximumDetail;detail++)
        {
            var counts=new Dictionary<string,int>(StringComparer.Ordinal);var borrowed=new List<byte[]>();
            TreLookupResult Read(string p){counts[p]=counts.GetValueOrDefault(p)+1;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;}
            var bundle=Load(Read,detail);
            if(counts.Values.Any(v=>v!=1)||!counts.Keys.ToHashSet().SetEquals(bundle.Sources.Keys))throw new Exception("Read-once closure mismatch.");
            var expected=bundle.LodMeshes.Values.Select(l=>l.ExposedPaths()[detail]).ToHashSet(StringComparer.Ordinal);
            if(!expected.SetEquals(bundle.SkinSources!.Keys))throw new Exception("Selected MGN mismatch.");selected.Add(expected);
            var levels=bundle.Appearance.Skeletons.ToDictionary(s=>s.Path,_=>0);
            var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons!,levels);
            var before=bundle.Geometries!.ToDictionary(p=>p.Key,p=>SkeletalGeometryScenarios.CanonicalHash(p.Value));
            foreach(var source in bundle.Sources.Values)Console.WriteLine("lod_source="+JsonSerializer.Serialize(new{detail,source}));
            Console.WriteLine("lod_descriptor="+JsonSerializer.Serialize(new{detail,bundle.Profile.RootPath,bundle.Profile.AppearancePath,bundle.Appearance,bundle.LodMeshes,counts}));
            foreach(var pair in bundle.Geometries!)
            {
                var g=pair.Value;var skin=g.Skin;var map=binding.Resolve(skin);
                var deformed=SkeletalJointBinding.DeformPositions(skin,binding.CreatePalette(binding.Joints.Select(j=>j.WorldBind).ToArray(),map));
                float error=deformed.Select((v,i)=>System.Numerics.Vector3.Distance(v,skin.Positions[i])).Max();
                if(error>0.00001f)throw new Exception("Bind deformation mismatch.");
                Console.WriteLine("lod_geometry="+JsonSerializer.Serialize(new{detail,path=pair.Key,source=g.SourceTriangles,degenerate=g.DegenerateTriangles,triangles=g.Triangles,vertices=g.Shaders.Sum(s=>s.PositionIndices.Count),shaders=g.Shaders.Count,tangents=g.Tangents.Count,renderers=g.TextureRenderers.Count,sha256=before[pair.Key],skinSha256=SkeletalSkinSourceScenarios.CanonicalHash(skin),positions=skin.Positions.Count,weights=skin.Weights.Count,normals=skin.Normals.Count,skin.TransformNames,skin.SkeletonPaths,joints=map,jointNames=binding.Joints.Select(j=>new{j.SkeletonPath,j.Name}),bindError=error}));
            }
            Console.WriteLine("lod_materials_begin="+detail);SkeletalDescriptorScenarios.DumpMaterials(bundle.MaterialBundle!);Console.WriteLine("lod_materials_end="+detail);
            var textureHashes=bundle.MaterialBundle!.Textures.ToDictionary(p=>p.Key,p=>p.Value.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span))).ToArray());
            foreach(var bytes in borrowed)Array.Clear(bytes);
            foreach(var pair in bundle.MaterialBundle.Textures)if(!textureHashes[pair.Key].SequenceEqual(pair.Value.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span)))))throw new Exception("Borrowed source mutation changed owned pixels.");
            foreach(var pair in bundle.Geometries!)if(before[pair.Key]!=SkeletalGeometryScenarios.CanonicalHash(pair.Value))throw new Exception("Borrowed source mutation changed owned geometry.");
            if(detail>=1)
            {
                foreach(string path in expected.Concat(bundle.Appearance.Skeletons.Select(s=>s.Path)).Concat(bundle.MaterialBundle!.Materials.Keys).Concat(bundle.MaterialBundle.Textures.Keys).Concat(bundle.MaterialBundle.Effects.Keys))
                    foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})Reject<InvalidDataException>(()=>Load(p=>p==path?new(kind):overlay.Lookup(p),detail));
                foreach(string path in expected)Reject<InvalidDataException>(()=>Load(p=>p==path?new(TreLookupKind.Present,Payload:new byte[24]):overlay.Lookup(p),detail));
                Reject<AppearanceGraphBudgetException>(()=>Load(overlay.Lookup,detail,new(MaximumResources:bundle.Sources.Count-1,MaximumSourceBytes:32*1024*1024)));
                Reject<AppearanceGraphBudgetException>(()=>Load(overlay.Lookup,detail,new(MaximumResources:128,MaximumSourceBytes:bundle.Sources.Values.Sum(s=>(long)s.Bytes)-1)));
                var defaultLimits=CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,ObjectPath,ObjectPin,SatPin,loadSkeletons:true,loadSkinSources:true,meshDetail:detail,loadGeometry:true,loadMaterialDefaults:true);
                if(!defaultLimits.Sources.Keys.ToHashSet().SetEquals(bundle.Sources.Keys))throw new Exception("Default aggregate closure differs.");
                string generator=bundle.Appearance.MeshGenerators[0];
                var singleton=MeshScenarios.Form("MLOD",MeshScenarios.Form("0000",MeshScenarios.Chunk("INFO",BitConverter.GetBytes((short)1)),MeshScenarios.Chunk("NAME",System.Text.Encoding.ASCII.GetBytes(expected.First()+"\0"))));
                Reject<InvalidDataException>(()=>Load(p=>p==generator?new(TreLookupKind.Present,Payload:singleton):overlay.Lookup(p),detail));
            }
        }
        if(selected.SelectMany(s=>s).Distinct(StringComparer.Ordinal).Count()!=selected.Sum(s=>s.Count))throw new Exception("Original details selected identical MGN paths.");
        Console.WriteLine("PASS R1-ASSETS-01: details 0/"+maximumDetail+", read-once owned closure, bind geometry, missing/masked/malformed/unavailable/invalid detail and aggregate budget gates.");
    }
}
