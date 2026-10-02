namespace Stardust.Core.Assets;

public sealed record InteriorSceneContentBundle(InteriorDoorSceneBundle Scene,PortalSceneBundle? Rooms,
    AppearanceGeometryGraph Geometry,TexturedAppearanceGraph? Materials,IReadOnlyDictionary<string,AssetSourceIdentity> Sources,
    SharedCreatureTemplateProfile? ActorProfile=null,InteriorSceneTriangleGuard? TriangleGuard=null,
    CreatureAppearanceDescriptorBundle? Character=null,FreeChaseCameraProfile? CameraProfile=null,IReadOnlyDictionary<int,IReadOnlyList<PortalCellLight>>? CellLights=null,StaticRoomLightingBundle? RoomLighting=null,StaticNormalBundle? StaticNormals=null);

/// <summary>One owned source cache across selected actor, room and material loading stages.</summary>
public static class InteriorSceneContentBundleReader
{
    public static InteriorSceneContentBundle Load(Func<string,TreLookupResult> resolve,ReadOnlySpan<byte> specification,
        string specificationSha256,string tableSha256,string layoutPath,string layoutSha256,
        bool loadAppearances=false,bool loadMaterials=false,AppearanceGraphLimits? limits=null,long maximumDecodedBytes=134_217_728,
        string? actorTemplatePath=null,string? actorTemplateSha256=null,bool createTriangleGuard=false,
        uint collisionSelector=1,int maximumCollisionTriangles=2048,string? characterAppearanceSha256=null,LogicalAnimationClipSelection? characterAnimationSelection=null,bool characterMaterialDefaults=false,bool characterPaletteDefaults=false,
        bool characterAlbedoFactors=false,IReadOnlyDictionary<BlueprintCustomizationKey,int>? characterFaceValues=null,CancellationToken cancellation=default,IReadOnlyDictionary<ShaderCustomizationKey,int>? characterCustomizationValues=null,IReadOnlyDictionary<string,int>? characterShapeValues=null,bool characterNormalDiffuse=false,IReadOnlyDictionary<ShaderCustomizationKey,int>? characterTextureValues=null,bool characterVertexLitSpecular=false,IReadOnlyDictionary<string,LogicalAnimationClipSelection>? characterAdditionalAnimations=null,string? cameraProfileSha256=null,bool loadCellLights=false,bool loadRoomLighting=false,bool loadStaticNormals=false)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        cancellation.ThrowIfCancellationRequested();
        if(loadStaticNormals&&!loadMaterials)throw new ArgumentException("Static normals require owned materials.");
        if(loadRoomLighting&&(!loadMaterials||!loadCellLights))throw new ArgumentException("Room lighting requires selected materials and owned cell lights.");
        if(loadCellLights&&(!loadAppearances||!characterVertexLitSpecular))throw new ArgumentException("Cell lighting requires selected rooms and specular character resources.");
        if(cameraProfileSha256 is not null&&(!createTriangleGuard||cameraProfileSha256.Length!=64||!cameraProfileSha256.All(Uri.IsHexDigit)))throw new ArgumentException("Pinned camera profile requires owned room triangle guard.");
        if(characterAdditionalAnimations is not null&&characterAnimationSelection is null)throw new ArgumentException("Movement bank requires selected idle animation.");
        if(characterVertexLitSpecular&&!characterAlbedoFactors)throw new ArgumentException("Vertex-lit specular requires albedo factors.");
        if(characterNormalDiffuse&&!characterAlbedoFactors)throw new ArgumentException("Normal diffuse requires albedo factors.");
        if(characterTextureValues is not null&&!characterNormalDiffuse)throw new ArgumentException("Texture selection requires normal diffuse.");
        if(characterShapeValues is not null&&characterAppearanceSha256 is null)throw new ArgumentException("Shape selection requires owned character geometry.");
        if(characterCustomizationValues is not null&&(!characterAlbedoFactors||characterFaceValues is not null))throw new ArgumentException("Shared character customization requires albedo factors and one profile.");
        bool generatedFace=characterFaceValues is not null||characterCustomizationValues is not null;
        if(characterAlbedoFactors&&!characterPaletteDefaults)throw new ArgumentException("Character albedo factors require palette defaults.");
        if(characterFaceValues is not null&&!characterMaterialDefaults)throw new ArgumentException("Generated character face requires material defaults.");
        if(characterPaletteDefaults&&!characterMaterialDefaults)throw new ArgumentException("Character palette defaults require owned material defaults.");
        if(characterMaterialDefaults&&characterAppearanceSha256 is null)throw new ArgumentException("Character material defaults require owned geometry.");
        if(characterAnimationSelection is not null&&characterAppearanceSha256 is null)throw new ArgumentException("Character animation requires owned character geometry.");
        if((actorTemplatePath is null)!=(actorTemplateSha256 is null))throw new ArgumentException("Actor template path and pin are required together.");
        if(characterAppearanceSha256 is not null&&(actorTemplatePath is null||characterAppearanceSha256.Length!=64||!characterAppearanceSha256.All(Uri.IsHexDigit)))
            throw new ArgumentException("Character geometry requires actor profile and appearance SHA-256 pin.");
        if(createTriangleGuard&&(!loadAppearances||!loadMaterials||actorTemplatePath is null))
            throw new ArgumentException("Scene triangle guard requires actor profile and selected room/material content.");
        if(collisionSelector>1||maximumCollisionTriangles is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(collisionSelector));
        if(actorTemplatePath is not null)
        {
            actorTemplatePath=LegacyAssetPath.Normalize(actorTemplatePath);
            if(!actorTemplatePath.StartsWith("object/",StringComparison.Ordinal)||actorTemplateSha256!.Length!=64||
                !actorTemplateSha256.All(Uri.IsHexDigit))throw new ArgumentException("Pinned object-root actor template required.");
        }
        if(loadMaterials&&!loadAppearances)throw new ArgumentException("Scene materials require selected appearances.");
        if(loadMaterials&&maximumDecodedBytes<4)throw new ArgumentOutOfRangeException(nameof(maximumDecodedBytes));
        limits??=new();if(limits.MaximumDepth<1||limits.MaximumEdges<1)throw new ArgumentOutOfRangeException(nameof(limits));var cache=new OwnedAssetSourceCache(path=>{cancellation.ThrowIfCancellationRequested();return resolve(path);},limits);
        var cameraProfile=cameraProfileSha256 is null?null:FreeChaseCameraProfileDecoder.Decode(cache.ReadPinned("camera/freechasecamera.iff",cameraProfileSha256));
        var scene=InteriorDoorSceneBundleReader.Load(cache.Read,specification,specificationSha256,tableSha256);
        var actorProfile=actorTemplatePath is null?null:SharedCreatureTemplateProfileReader.Load(cache.Read,actorTemplatePath,actorTemplateSha256!);
        var character=characterAppearanceSha256 is null?null:CreatureAppearanceDescriptorBundleReader.Load(cache.Read,actorTemplatePath!,actorTemplateSha256!,characterAppearanceSha256,
            limits:!generatedFace?null:new(MaximumResources:128,MaximumSourceBytes:32*1024*1024),
            loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadLogicalTables:characterAnimationSelection is not null,
            animationSelection:characterAnimationSelection,prepareSelectedAnimation:characterAnimationSelection is not null,loadMaterialDefaults:characterMaterialDefaults,loadPaletteDefaults:characterPaletteDefaults,
            loadFaceBlueprints:generatedFace,loadFaceResources:generatedFace,loadFacePixelPrograms:generatedFace,loadAlbedoFactors:characterAlbedoFactors,loadBlendShapes:characterShapeValues is not null,loadTextureChoices:characterNormalDiffuse,loadCompressedNormals:characterNormalDiffuse,loadVertexLitSpecular:characterVertexLitSpecular,additionalAnimationSelections:characterAdditionalAnimations);
        if(characterFaceValues is not null)character=character! with{GeneratedFaces=BlueprintGeneratedFaceBuilder.Create(character!.Geometries!,character.FaceBlueprints!,character.FaceResources!,characterFaceValues,cancellation)};
        if(characterCustomizationValues is not null)character=SkeletalAppearanceCustomization.Apply(character!,characterCustomizationValues,cancellation);
        if(characterShapeValues is not null)character=SkeletalAppearanceShapes.Apply(character!,characterShapeValues,cancellation);
        if(characterTextureValues is not null)character=character! with{MaterialBundle=SkeletalTextureChoices.Apply(character!.MaterialBundle!,characterTextureValues,cancellation)};
        layoutPath=LegacyAssetPath.Normalize(layoutPath);
        if(layoutPath!=scene.LayoutPath||!scene.Sources.TryGetValue(layoutPath,out var identity)||!identity.Sha256.Equals(layoutSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Content layout differs from pinned actor scene.");
        PortalSceneBundle? rooms=loadAppearances?PortalSceneBundleReader.Load(cache.Read,layoutPath,layoutSha256,limits,selectedAppearanceCells:scene.Floors.Keys.ToArray()):null;
        AppearanceGeometryTemplate Empty(string suffix,IReadOnlyList<AppearanceGeometryChild> children)=>new(layoutPath+suffix,null,null,null,children,Array.Empty<string>());
        var leaves=Empty("#door-instances",Array.AsReadOnly(scene.Doorways.Select((door,index)=>new AppearanceGeometryChild(index,AppearanceTransform.Identity,door.Leaf.Geometry.Root)).ToArray()));
        var roomRoot=rooms?.Geometry.Root??Empty("#unloaded-rooms",Array.Empty<AppearanceGeometryChild>());
        var geometry=new AppearanceGeometryGraph(Empty("#selected-interior-content",new[]{new AppearanceGeometryChild(0,AppearanceTransform.Identity,roomRoot),new AppearanceGeometryChild(1,AppearanceTransform.Identity,leaves)}),cache.Sources);
        var materials=loadMaterials?TexturedAppearanceGraphReader.Attach(cache.Read,geometry,cache.Sources,maximumDecodedBytes):null;
        foreach(var source in scene.Sources.Values.Concat(materials?.Sources.Values??rooms?.Sources.Values??Array.Empty<AssetSourceIdentity>()))
            if(!cache.Sources.TryGetValue(source.Path,out var shared)||shared!=source)throw new InvalidDataException("Scene content source identity mismatch.");
        if(actorProfile is not null)foreach(var source in actorProfile.Sources.Values)
            if(!cache.Sources.TryGetValue(source.Path,out var shared)||shared!=source)throw new InvalidDataException("Actor profile source identity mismatch.");
        if(character is not null)foreach(var source in character.Sources.Values)
            if(!cache.Sources.TryGetValue(source.Path,out var shared)||shared!=source)throw new InvalidDataException("Character source identity mismatch.");
        IReadOnlyDictionary<int,IReadOnlyList<PortalCellLight>>? cellLights=null;
        if(loadCellLights)
        {
            var selected=new Dictionary<int,IReadOnlyList<PortalCellLight>>();
            foreach(int cell in rooms!.CellAppearances.Keys.Order())
            {
                var records=PortalCellLightsDecoder.Decode(scene.Layout.Cells[cell],CellLightingPreview.MaximumLights);
                _=new CellLightingPreview(records,AppearanceTransform.Identity,new(.1f));selected.Add(cell,records);
            }
            cellLights=new System.Collections.ObjectModel.ReadOnlyDictionary<int,IReadOnlyList<PortalCellLight>>(selected);
        }
        var roomLighting=loadRoomLighting?StaticRoomLightingReader.Load(cache.Read,materials!.Meshes):null;
        var staticNormals=loadStaticNormals?StaticNormalMaterialReader.Load(cache.Read,materials!.Meshes):null;
        var content=new InteriorSceneContentBundle(scene,rooms,geometry,materials,cache.Sources,actorProfile,Character:character,CameraProfile:cameraProfile,CellLights:cellLights,RoomLighting:roomLighting,StaticNormals:staticNormals);
        if(createTriangleGuard)content=content with{TriangleGuard=InteriorSceneTriangleGuardReader.Create(content,collisionSelector,maximumCollisionTriangles)};
        cancellation.ThrowIfCancellationRequested();return content;
    }
}
