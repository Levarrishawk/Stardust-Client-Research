namespace Stardust.Core.Assets;
public sealed record TexturedPortalScene(PortalSceneBundle Scene,TexturedAppearanceGraph Materials,IReadOnlyDictionary<string,AssetSourceIdentity> Sources,IReadOnlyList<string> Diagnostics);
public static class TexturedPortalSceneReader
{
    public static TexturedPortalScene Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,AppearanceGraphLimits? limits=null,
        long maximumDecodedBytes=134_217_728,bool loadFloors=false,bool loadPathGraphs=false,CustomizableShaderInspectionMode customizationMode=CustomizableShaderInspectionMode.Reject,IReadOnlyCollection<int>? selectedAppearanceCells=null)
    {
        OwnedAssetSourceCache cache=new(resolve,limits??new());PortalSceneBundle scene=PortalSceneBundleReader.Load(cache.Read,path,expectedSha256,limits,loadFloors,loadPathGraphs,selectedAppearanceCells);
        TexturedAppearanceGraph materials=TexturedAppearanceGraphReader.Attach(cache.Read,scene.Geometry,cache.Sources,maximumDecodedBytes,customizationMode);
        return new(scene,materials,cache.Sources,scene.Diagnostics.Concat(materials.Diagnostics).ToArray());
    }
}
