using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record PortalSceneBundle(PortalLayoutDescription Layout,AppearanceGeometryGraph Geometry,
    IReadOnlyDictionary<int,AppearanceGeometryTemplate> CellAppearances,IReadOnlyDictionary<int,FloorMeshDescription> CellFloors,IReadOnlyDictionary<int,PathGraphDescription> CellPathGraphs,PathGraphDescription? LayoutPathGraph,IReadOnlyDictionary<string,AssetSourceIdentity> Sources,IReadOnlyList<string> Diagnostics);

/// <summary>Offline cell appearance composition; floor/door/visibility runtime behavior remains separate.</summary>
public static class PortalSceneBundleReader
{
    public static PortalSceneBundle Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,AppearanceGraphLimits? limits=null,bool loadFloors=false,bool loadPathGraphs=false)
    {
        ArgumentNullException.ThrowIfNull(resolve);limits??=new();
        if(limits.MaximumResources<1 || limits.MaximumSourceBytes<1 || limits.MaximumDepth<1 || limits.MaximumEdges<1)throw new ArgumentOutOfRangeException(nameof(limits));
        OwnedAssetSourceCache cache=new(resolve,limits);IReadOnlyDictionary<string,AssetSourceIdentity> sources=cache.Sources;
        TreLookupResult Read(string resource)=>cache.Read(resource);
        path=LegacyAssetPath.Normalize(path);PortalLayoutDescription layout=PortalLayoutDecoder.Decode(cache.ReadPinned(path,expectedSha256));
        List<(int Cell,AppearanceGeometryRequest Request)> requested=new();
        for(int cell=0;cell<layout.Cells.Count;cell++)
            if(layout.Cells[cell].AppearancePath.Length!=0)requested.Add((cell,new(layout.Cells[cell].AppearancePath)));
        Dictionary<int,AppearanceGeometryTemplate> appearances=new();List<string> diagnostics=new(layout.Diagnostics);List<AppearanceGeometryChild> children=new();
        AppearanceGeometryCollection? collection=requested.Count==0?null:AppearanceGeometryGraphReader.LoadMany(Read,requested.Select(value=>value.Request).ToArray(),limits,true);
        if(collection is not null)
        {
            diagnostics.AddRange(collection.Diagnostics);
            for(int index=0;index<requested.Count;index++)
            {
                AppearanceGeometryTemplate? template=collection.Roots[index].Template;
                if(template is null){diagnostics.Add($"UnavailableCellAppearance:{requested[index].Cell}:{requested[index].Request.Path}");continue;}
                appearances.Add(requested[index].Cell,template);children.Add(new(requested[index].Cell,AppearanceTransform.Identity,template));
            }
        }
        PortalFloorResources floors=PortalFloorResources.Load(layout,Read,loadFloors,loadPathGraphs);diagnostics.AddRange(floors.Diagnostics);
        // This synthetic composite is adapter input, not a recovered serialized appearance family.
        AppearanceGeometryTemplate composite=new(path+"#cell-inspection",null,null,null,children.AsReadOnly(),diagnostics.AsReadOnly());
        return new(layout,new(composite,sources),new ReadOnlyDictionary<int,AppearanceGeometryTemplate>(appearances),floors.Floors,floors.Graphs,floors.LayoutGraph,
            sources,diagnostics.AsReadOnly());
    }
}
