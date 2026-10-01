using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record CreatureAppearanceDescriptorBundle(SharedCreatureTemplateProfile Profile,
    SkeletalAppearanceDescription Appearance,IReadOnlyDictionary<string,LodMeshGeneratorDescription> LodMeshes,
    IReadOnlyDictionary<string,AssetSourceIdentity> Sources,
    IReadOnlyDictionary<string,SkeletonTemplateDescription>? Skeletons=null,
    IReadOnlyDictionary<string,SkeletalMeshSkinSourceDescription>? SkinSources=null,
    IReadOnlyDictionary<string,SkeletalMeshGeometryDescription>? Geometries=null,
    IReadOnlyDictionary<string,LogicalAnimationTableDescription>? LogicalTables=null,
    SelectedLogicalAnimationClip? SelectedAnimation=null);

public static class CreatureAppearanceDescriptorBundleReader
{
    public static CreatureAppearanceDescriptorBundle Load(Func<string,TreLookupResult> resolve,string objectPath,
        string objectSha256,string appearanceSha256,AppearanceGraphLimits? limits=null,bool loadSkeletons=false,bool loadSkinSources=false,int meshDetail=0,bool loadGeometry=false,bool loadLogicalTables=false,LogicalAnimationClipSelection? animationSelection=null)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        ValidatePin(objectSha256);ValidatePin(appearanceSha256);
        if(meshDetail is <0 or >3)throw new ArgumentOutOfRangeException(nameof(meshDetail));
        if(loadSkinSources&&!loadSkeletons)throw new ArgumentException("Skin-source loading requires owned skeletons.");
        if(loadGeometry&&!loadSkinSources)throw new ArgumentException("Geometry loading requires owned skin sources.");
        if(loadLogicalTables&&!loadSkeletons)throw new ArgumentException("Logical-table loading requires owned skeletons.");
        if(animationSelection is not null)
        {
            if(!loadLogicalTables)throw new ArgumentException("Animation selection requires owned logical tables.");ValidatePin(animationSelection.ClipSha256);
            LegacyAssetPath.Normalize(animationSelection.TablePath);LogicalAnimationTableDecoder.ValidateName(animationSelection.LogicalName);
        }
        objectPath=LegacyAssetPath.Normalize(objectPath);
        if(!objectPath.StartsWith("object/",StringComparison.Ordinal))throw new ArgumentException("Object-root path required.");
        var cache=new OwnedAssetSourceCache(resolve,limits??new(MaximumResources:64,MaximumSourceBytes:16*1024*1024));
        var profile=SharedCreatureTemplateProfileReader.Load(cache.Read,objectPath,objectSha256);
        var appearance=SkeletalAppearanceDecoder.Decode(cache.ReadPinned(profile.AppearancePath,appearanceSha256));
        Dictionary<string,LodMeshGeneratorDescription> meshes=new(StringComparer.Ordinal);
        foreach(string path in appearance.MeshGenerators)
        {
            if(meshes.ContainsKey(path))continue;
            var source=cache.Read(path);
            if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Required mesh descriptor missing/masked.");
            meshes.Add(path,SkeletalAppearanceDecoder.DecodeLodMesh(source.Payload));
        }
        Dictionary<string,SkeletonTemplateDescription>? skeletons=null;
        if(loadSkeletons)
        {
            skeletons=new(StringComparer.Ordinal);
            foreach(var reference in appearance.Skeletons)
            {
                if(skeletons.ContainsKey(reference.Path))continue;
                var source=cache.Read(reference.Path);
                if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Required skeleton missing/masked.");
                skeletons.Add(reference.Path,SkeletonTemplateDecoder.Decode(source.Payload));
            }
        }
        Dictionary<string,SkeletalMeshSkinSourceDescription>? skins=null;
        if(loadSkinSources)
        {
            skins=new(StringComparer.Ordinal);
            foreach(string generator in appearance.MeshGenerators)
            {
                var levels=meshes[generator].ExposedPaths();
                if(meshDetail>=levels.Count)throw new InvalidDataException("Selected mesh detail unavailable.");
                string path=levels[meshDetail];if(skins.ContainsKey(path))continue;
                var source=cache.Read(path);
                if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Required skin source missing/masked.");
                var decoded=SkeletalMeshSkinSourceDecoder.Decode(source.Payload);
                if(decoded.SkeletonPaths.Any(p=>!skeletons!.ContainsKey(p)))throw new InvalidDataException("Skin source references a skeleton absent from the appearance.");
                skins.Add(path,decoded);
            }
        }
        Dictionary<string,SkeletalMeshGeometryDescription>? geometry=null;
        if(loadGeometry)geometry=skins!.ToDictionary(p=>p.Key,p=>SkeletalMeshGeometryDecoder.Create(p.Value),StringComparer.Ordinal);
        Dictionary<string,LogicalAnimationTableDescription>? tables=null;
        if(loadLogicalTables)
        {
            tables=new(StringComparer.Ordinal);
            foreach(var reference in appearance.AnimationTables)
            {
                if(!skeletons!.ContainsKey(reference.SkeletonPath))throw new InvalidDataException("Logical table references absent appearance skeleton.");
                if(tables.ContainsKey(reference.AnimationTablePath))continue;
                var source=cache.Read(reference.AnimationTablePath);if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Required logical table missing/masked.");
                tables.Add(reference.AnimationTablePath,LogicalAnimationTableDecoder.Decode(source.Payload));
            }
        }
        SelectedLogicalAnimationClip? selected=null;
        if(animationSelection is not null)
        {
            string tablePath=LegacyAssetPath.Normalize(animationSelection.TablePath);
            if(!tables!.TryGetValue(tablePath,out var table))throw new InvalidDataException("Selected table absent from appearance.");
            var entry=table.Find(animationSelection.LogicalName);
            if(!entry.Name.Equals(animationSelection.LogicalName.TrimEnd(' '),StringComparison.OrdinalIgnoreCase)||entry.Template is null)throw new InvalidDataException("Selected logical animation absent/null; fallback playback is not implicit.");
            var proxy=LogicalAnimationProxyDecoder.Decode(entry.Template);
            var clip=IffDocument.Parse(cache.ReadPinned(proxy.ClipPath,animationSelection.ClipSha256),new(MaximumBytes:8*1024*1024,MaximumNodes:100000,MaximumDepth:32)).Root;
            if(clip.Children.Count!=1||!(clip.IsForm("CKAT")&&clip.Children[0].IsForm("0001")||clip.IsForm("KFAT")&&clip.Children[0].IsForm("0003")))throw new InvalidDataException("Unsupported selected clip envelope/version.");
            selected=new(animationSelection with{TablePath=tablePath},proxy,clip);
        }
        return new(profile,appearance,new ReadOnlyDictionary<string,LodMeshGeneratorDescription>(meshes),cache.Sources,
            skeletons is null?null:new ReadOnlyDictionary<string,SkeletonTemplateDescription>(skeletons),
            skins is null?null:new ReadOnlyDictionary<string,SkeletalMeshSkinSourceDescription>(skins),
            geometry is null?null:new ReadOnlyDictionary<string,SkeletalMeshGeometryDescription>(geometry),
            tables is null?null:new ReadOnlyDictionary<string,LogicalAnimationTableDescription>(tables),selected);
    }
    private static void ValidatePin(string value)
    {
        if(value is null||value.Length!=64||value.Any(c=>!Uri.IsHexDigit(c)))throw new ArgumentException("Explicit SHA-256 pin required.");
    }
}
