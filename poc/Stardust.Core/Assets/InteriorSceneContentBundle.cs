namespace Stardust.Core.Assets;

public sealed record InteriorSceneContentBundle(InteriorDoorSceneBundle Scene,PortalSceneBundle? Rooms,
    AppearanceGeometryGraph Geometry,TexturedAppearanceGraph? Materials,IReadOnlyDictionary<string,AssetSourceIdentity> Sources);

/// <summary>One owned source cache across selected actor, room and material loading stages.</summary>
public static class InteriorSceneContentBundleReader
{
    public static InteriorSceneContentBundle Load(Func<string,TreLookupResult> resolve,ReadOnlySpan<byte> specification,
        string specificationSha256,string tableSha256,string layoutPath,string layoutSha256,
        bool loadAppearances=false,bool loadMaterials=false,AppearanceGraphLimits? limits=null,long maximumDecodedBytes=134_217_728)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        if(loadMaterials&&!loadAppearances)throw new ArgumentException("Scene materials require selected appearances.");
        if(loadMaterials&&maximumDecodedBytes<4)throw new ArgumentOutOfRangeException(nameof(maximumDecodedBytes));
        limits??=new();if(limits.MaximumDepth<1||limits.MaximumEdges<1)throw new ArgumentOutOfRangeException(nameof(limits));var cache=new OwnedAssetSourceCache(resolve,limits);
        var scene=InteriorDoorSceneBundleReader.Load(cache.Read,specification,specificationSha256,tableSha256);
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
        return new(scene,rooms,geometry,materials,cache.Sources);
    }
}
