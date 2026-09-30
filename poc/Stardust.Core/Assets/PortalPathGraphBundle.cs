namespace Stardust.Core.Assets;
public sealed record PortalPathGraphBundle(PortalLayoutDescription Layout,IReadOnlyDictionary<int,FloorMeshDescription> CellFloors,
    IReadOnlyDictionary<int,PathGraphDescription> CellPathGraphs,PathGraphDescription? LayoutPathGraph,IReadOnlyDictionary<string,AssetSourceIdentity> Sources,IReadOnlyList<string> Diagnostics);
public static class PortalPathGraphBundleReader
{
    public static PortalPathGraphBundle Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,AppearanceGraphLimits? limits=null)
    {
        OwnedAssetSourceCache cache=new(resolve,limits??new());PortalLayoutDescription layout=PortalLayoutDecoder.Decode(cache.ReadPinned(path,expectedSha256));
        PortalFloorResources floors=PortalFloorResources.Load(layout,cache.Read,true,true);
        return new(layout,floors.Floors,floors.Graphs,floors.LayoutGraph,cache.Sources,layout.Diagnostics.Concat(floors.Diagnostics).ToArray());
    }
}
