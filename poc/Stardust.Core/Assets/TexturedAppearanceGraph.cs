using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record TexturedAppearanceGraph(AppearanceGeometryGraph Geometry,IReadOnlyDictionary<string,TexturedStaticMesh> Meshes,
    IReadOnlyDictionary<string,AssetSourceIdentity> Sources,IReadOnlyList<string> Diagnostics);

/// <summary>Explicit mixed inspection bundle: supported material graphs attach to shared geometry; failures retain neutral geometry.</summary>
public static class TexturedAppearanceGraphReader
{
    public static TexturedAppearanceGraph Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,
        AppearanceGraphLimits? limits=null,long maximumDecodedBytes=134_217_728,CustomizableShaderInspectionMode customizationMode=CustomizableShaderInspectionMode.Reject,SwitchTextureFrameSelection? switchTextureFrame=null)
    {
        ArgumentNullException.ThrowIfNull(resolve);switchTextureFrame?.Validate();limits??=new();OwnedAssetSourceCache sources=new(resolve,limits);
        AppearanceGeometryGraph geometry=AppearanceGeometryGraphReader.Load(sources.Read,path,expectedSha256,limits);
        return Attach(sources.Read,geometry,sources.Sources,maximumDecodedBytes,customizationMode,switchTextureFrame);
    }
    internal static TexturedAppearanceGraph Attach(Func<string,TreLookupResult> Read,AppearanceGeometryGraph geometry,IReadOnlyDictionary<string,AssetSourceIdentity> sources,long maximumDecodedBytes,CustomizableShaderInspectionMode customizationMode=CustomizableShaderInspectionMode.Reject,SwitchTextureFrameSelection? switchTextureFrame=null)
    {
        if(!Enum.IsDefined(customizationMode))throw new ArgumentOutOfRangeException(nameof(customizationMode));
        if(maximumDecodedBytes<4)throw new ArgumentOutOfRangeException(nameof(maximumDecodedBytes));
        Dictionary<string,TexturedStaticMesh> meshes=new(StringComparer.Ordinal);List<string> diagnostics=new();
        switchTextureFrame?.Validate();
        HashSet<string> visited=new(StringComparer.Ordinal);StaticMeshDecodeCache cache=new(maximumDecodedBytes,customizationMode,switchTextureFrame);
        void Visit(AppearanceGeometryTemplate template)
        {
            if(!visited.Add(template.Path))return;
            if(template.Mesh is not null)
            {
                string meshPath=template.MeshSourcePath??template.Path;
                try{meshes.Add(template.Path,TexturedStaticMeshReader.LoadShared(Read,meshPath,sources[meshPath].Sha256,cache));}
                catch(InvalidDataException failure){diagnostics.Add($"UnavailableMaterialBundle:{template.Path}:{failure.Message}");}
            }
            foreach(AppearanceGeometryChild child in template.Children)Visit(child.Template);
        }
        Visit(geometry.Root);
        return new(geometry,new ReadOnlyDictionary<string,TexturedStaticMesh>(meshes),sources,diagnostics.AsReadOnly());
    }
}
