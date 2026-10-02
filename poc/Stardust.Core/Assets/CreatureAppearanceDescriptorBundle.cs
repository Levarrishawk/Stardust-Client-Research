using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record CreatureAppearanceDescriptorBundle(SharedCreatureTemplateProfile Profile,
    SkeletalAppearanceDescription Appearance,IReadOnlyDictionary<string,LodMeshGeneratorDescription> LodMeshes,
    IReadOnlyDictionary<string,AssetSourceIdentity> Sources,
    IReadOnlyDictionary<string,SkeletonTemplateDescription>? Skeletons=null,
    IReadOnlyDictionary<string,SkeletalMeshSkinSourceDescription>? SkinSources=null,
    IReadOnlyDictionary<string,SkeletalMeshGeometryDescription>? Geometries=null,
    IReadOnlyDictionary<string,LogicalAnimationTableDescription>? LogicalTables=null,
    SelectedLogicalAnimationClip? SelectedAnimation=null,PreparedSkeletalAnimation? PreparedAnimation=null,SkeletalMaterialBundle? MaterialBundle=null,IReadOnlyDictionary<string,BlueprintTextureRendererDescription>? FaceBlueprints=null,BlueprintFaceResources? FaceResources=null,BlueprintGeneratedFaces? GeneratedFaces=null,IReadOnlyDictionary<ShaderCustomizationKey,int>? CustomizationValues=null,IReadOnlyDictionary<string,SkeletalBlendShapeDescription>? BlendShapes=null,SkeletalShapeSelection? ShapeSelection=null,IReadOnlyDictionary<string,PreparedSkeletalAnimation>? AdditionalAnimations=null);

public static class CreatureAppearanceDescriptorBundleReader
{
    public static CreatureAppearanceDescriptorBundle Load(Func<string,TreLookupResult> resolve,string objectPath,
        string objectSha256,string appearanceSha256,AppearanceGraphLimits? limits=null,bool loadSkeletons=false,bool loadSkinSources=false,int meshDetail=0,bool loadGeometry=false,bool loadLogicalTables=false,LogicalAnimationClipSelection? animationSelection=null,bool prepareSelectedAnimation=false,bool loadMaterialDefaults=false,bool loadPaletteDefaults=false,bool loadFaceBlueprints=false,bool loadFaceResources=false,bool loadFacePixelPrograms=false,bool loadAlbedoFactors=false,bool loadBlendShapes=false,bool loadTextureChoices=false,bool loadCompressedNormals=false,bool loadVertexLitSpecular=false,IReadOnlyDictionary<string,LogicalAnimationClipSelection>? additionalAnimationSelections=null)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        if(prepareSelectedAnimation&&animationSelection is null)throw new ArgumentException("Prepared animation requires explicit clip selection.");
        ValidatePin(objectSha256);ValidatePin(appearanceSha256);
        if(additionalAnimationSelections is not null)
        {
            if(!prepareSelectedAnimation||!loadLogicalTables||additionalAnimationSelections.Count is <1 or >8)throw new ArgumentException("Additional animations require prepared idle/tables and a bounded bank.");
            foreach(var pair in additionalAnimationSelections)
            {
                LogicalAnimationTableDecoder.ValidateName(pair.Key);ValidatePin(pair.Value.ClipSha256);
                LegacyAssetPath.Normalize(pair.Value.TablePath);LogicalAnimationTableDecoder.ValidateName(pair.Value.LogicalName);
                if(pair.Value.ExplicitChildRoute is {Count:>8})throw new ArgumentException("Explicit animation route depth budget.");
            }
        }
        if(meshDetail is <0 or >3)throw new ArgumentOutOfRangeException(nameof(meshDetail));
        if(loadSkinSources&&!loadSkeletons)throw new ArgumentException("Skin-source loading requires owned skeletons.");
        if(loadGeometry&&!loadSkinSources)throw new ArgumentException("Geometry loading requires owned skin sources.");
        if(loadMaterialDefaults&&!loadGeometry)throw new ArgumentException("Material default loading requires owned geometry.");
        if(loadBlendShapes&&!loadGeometry)throw new ArgumentException("Blend descriptions require owned geometry.");
        if(loadTextureChoices&&!loadMaterialDefaults)throw new ArgumentException("Texture choices require owned material defaults.");
        if(loadVertexLitSpecular&&!loadAlbedoFactors)throw new ArgumentException("Vertex-lit specular requires owned albedo factors.");
        if(loadCompressedNormals&&!loadAlbedoFactors)throw new ArgumentException("Compressed-normal preview requires owned albedo factors.");
        if(loadFaceBlueprints&&!loadGeometry)throw new ArgumentException("Face blueprints require owned geometry.");
        if(loadFaceResources&&!loadFaceBlueprints)throw new ArgumentException("Face resources require owned blueprints.");
        if(loadFacePixelPrograms&&!loadFaceResources)throw new ArgumentException("Face pixel programs require owned resources.");
        if(loadAlbedoFactors&&!loadPaletteDefaults)throw new ArgumentException("Albedo factors require owned palette defaults.");
        if(loadPaletteDefaults&&!loadMaterialDefaults)throw new ArgumentException("Palette defaults require owned material defaults.");
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
        if(animationSelection is not null)selected=ReadSelection(animationSelection);
        SelectedLogicalAnimationClip ReadSelection(LogicalAnimationClipSelection selection)
        {
            string tablePath=LegacyAssetPath.Normalize(selection.TablePath);
            if(!tables!.TryGetValue(tablePath,out var table))throw new InvalidDataException("Selected table absent from appearance.");
            var entry=table.Find(selection.LogicalName);
            if(!entry.Name.Equals(selection.LogicalName.TrimEnd(' '),StringComparison.OrdinalIgnoreCase)||entry.Template is null)throw new InvalidDataException("Selected logical animation absent/null; fallback playback is not implicit.");
            var proxy=LogicalAnimationProxyDecoder.Decode(entry.Template,selection.ExplicitChildRoute);
            var clip=IffDocument.Parse(cache.ReadPinned(proxy.ClipPath,selection.ClipSha256),new(MaximumBytes:8*1024*1024,MaximumNodes:100000,MaximumDepth:32)).Root;
            if(clip.Children.Count!=1||!(clip.IsForm("CKAT")&&clip.Children[0].IsForm("0001")||clip.IsForm("KFAT")&&clip.Children[0].IsForm("0003")))throw new InvalidDataException("Unsupported selected clip envelope/version.");
            return new(selection with{TablePath=tablePath,ExplicitChildRoute=selection.ExplicitChildRoute is null?null:Array.AsReadOnly(selection.ExplicitChildRoute.ToArray())},proxy,clip);
        }
        var result=new CreatureAppearanceDescriptorBundle(profile,appearance,new ReadOnlyDictionary<string,LodMeshGeneratorDescription>(meshes),cache.Sources,
            skeletons is null?null:new ReadOnlyDictionary<string,SkeletonTemplateDescription>(skeletons),
            skins is null?null:new ReadOnlyDictionary<string,SkeletalMeshSkinSourceDescription>(skins),
            geometry is null?null:new ReadOnlyDictionary<string,SkeletalMeshGeometryDescription>(geometry),
            tables is null?null:new ReadOnlyDictionary<string,LogicalAnimationTableDescription>(tables),selected);
        if(loadBlendShapes)result=result with{BlendShapes=new ReadOnlyDictionary<string,SkeletalBlendShapeDescription>(result.Geometries!.ToDictionary(p=>p.Key,p=>SkeletalBlendShapeDecoder.Create(p.Value),StringComparer.Ordinal))};
        if(loadMaterialDefaults)result=result with{MaterialBundle=SkeletalMaterialBundleReader.Load(result.Geometries!,cache,loadPaletteDefaults,loadAlbedoFactors,loadTextureChoices,loadCompressedNormals,loadVertexLitSpecular)};
        if(loadFaceBlueprints)
        {
            var blueprints=new Dictionary<string,BlueprintTextureRendererDescription>(StringComparer.Ordinal);
            foreach(string path in result.Geometries!.Values.SelectMany(g=>g.TextureRenderers).Select(r=>r.Path).Distinct(StringComparer.Ordinal))
            {
                var source=cache.Read(path);if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Required face blueprint missing/masked.");
                blueprints.Add(path,BlueprintTextureRendererDecoder.Decode(source.Payload));
            }
            result=result with{FaceBlueprints=new ReadOnlyDictionary<string,BlueprintTextureRendererDescription>(blueprints)};
            if(loadFaceResources)result=result with{FaceResources=BlueprintFaceResourceReader.Load(blueprints.Values,cache,loadFacePixelPrograms)};
        }
        if(prepareSelectedAnimation)result=result with{PreparedAnimation=PreparedSkeletalAnimation.Create(result,MissingAnimationTransformPolicy.BindPose)};
        if(additionalAnimationSelections is not null)
        {
            var animations=new Dictionary<string,PreparedSkeletalAnimation>(StringComparer.Ordinal);
            foreach(var pair in additionalAnimationSelections)
            {
                var selectedBundle=result with{SelectedAnimation=ReadSelection(pair.Value)};
                var prepared=PreparedSkeletalAnimation.Create(selectedBundle,MissingAnimationTransformPolicy.BindPose,allowTranslationSuffix:true);
                if(prepared.SkeletonPath!=result.PreparedAnimation!.SkeletonPath)throw new InvalidDataException("Additional animation skeleton differs from selected idle.");
                animations.Add(pair.Key,prepared);
            }
            result=result with{AdditionalAnimations=new ReadOnlyDictionary<string,PreparedSkeletalAnimation>(animations)};
        }
        return result;
    }
    private static void ValidatePin(string value)
    {
        if(value is null||value.Length!=64||value.Any(c=>!Uri.IsHexDigit(c)))throw new ArgumentException("Explicit SHA-256 pin required.");
    }
}
