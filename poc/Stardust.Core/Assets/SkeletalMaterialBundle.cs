using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

/// <summary>Owned authored texture defaults; palette factors and generated textures remain descriptors.</summary>
public sealed record SkeletalMaterialBundle(IReadOnlyDictionary<string,StaticMeshMaterial> Materials,
    IReadOnlyDictionary<string,DdsImage> Textures,IReadOnlyDictionary<string,StaticEffectDescription> Effects,
    IReadOnlyDictionary<string,CustomizableShaderPaletteBundle>? PaletteDefaults=null,SkeletalAlbedoFactorBundle? AlbedoFactors=null,SkeletalTextureChoiceSelection? TextureSelection=null,SkeletalCompressedNormalBundle? CompressedNormals=null,SkeletalVertexLitSpecularBundle? VertexLitSpecular=null,SkeletalTangentLightingBundle? TangentLighting=null);

internal static class SkeletalMaterialBundleReader
{
    internal static SkeletalMaterialBundle Load(IReadOnlyDictionary<string,SkeletalMeshGeometryDescription> geometry,OwnedAssetSourceCache sources,bool loadPaletteDefaults=false,bool loadAlbedoFactors=false,bool loadTextureChoices=false,bool loadCompressedNormals=false,bool loadVertexLitSpecular=false)
    {
        var materials=new Dictionary<string,StaticMeshMaterial>(StringComparer.Ordinal);
        var textures=new Dictionary<string,DdsImage>(StringComparer.Ordinal);
        var effects=new Dictionary<string,StaticEffectDescription>(StringComparer.Ordinal);
        var decoded=new StaticMeshDecodeCache(134_217_728,CustomizableShaderInspectionMode.AuthoredTextureDefaults);
        byte[] Read(string path)
        {
            var source=sources.Read(path);
            if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Skeletal material dependency missing/masked.");
            return source.Payload;
        }
        foreach(string path in geometry.Values.SelectMany(mesh=>mesh.Shaders).Select(shader=>shader.ShaderPath).Distinct(StringComparer.Ordinal))
        {
            if(materials.Count>=64)throw new AppearanceGraphBudgetException("Skeletal material count budget.");
            var material=decoded.Material(Read(path));materials.Add(path,material);
            string effect=material.References.EffectPath;
            if(!effects.ContainsKey(effect))effects.Add(effect,decoded.Effect(Read(effect)));
            foreach(var binding in material.References.Textures)
            {
                if(binding.SourceFlag!=0||binding.AssetPath is not string texture)throw new InvalidDataException("Skeletal material requires authored named textures.");
                if(textures.ContainsKey(texture))continue;
                if(textures.Count>=64)throw new AppearanceGraphBudgetException("Skeletal texture count budget.");
                textures.Add(texture,decoded.Texture(Read(texture),134_217_728));
            }
            if(loadTextureChoices&&material.Customization is not null)foreach(string texture in material.Customization.TextureChoices)
            {
                if(textures.ContainsKey(texture))continue;
                if(textures.Count>=64)throw new AppearanceGraphBudgetException("Skeletal texture-choice count budget.");
                textures.Add(texture,decoded.Texture(Read(texture),134_217_728));
            }
        }
        Dictionary<string,CustomizableShaderPaletteBundle>? palettes=null;
        if(loadPaletteDefaults)
        {
            palettes=new(StringComparer.Ordinal);
            foreach(var pair in materials.Where(pair=>pair.Value.Customization is not null))
                palettes.Add(pair.Key,CustomizableShaderPaletteBundleReader.Load(sources.Read,pair.Key,sources.Sources[pair.Key].Sha256));
        }
        var result=new SkeletalMaterialBundle(new ReadOnlyDictionary<string,StaticMeshMaterial>(materials),new ReadOnlyDictionary<string,DdsImage>(textures),new ReadOnlyDictionary<string,StaticEffectDescription>(effects),
            palettes is null?null:new ReadOnlyDictionary<string,CustomizableShaderPaletteBundle>(palettes));
        if(loadAlbedoFactors)result=result with{AlbedoFactors=SkeletalAlbedoFactorReader.Load(result,sources)};
        if(loadCompressedNormals)result=result with{CompressedNormals=SkeletalCompressedNormalReader.Load(result,sources)};
        if(loadVertexLitSpecular)result=result with{VertexLitSpecular=SkeletalVertexLitSpecularReader.Load(result,sources)};
        if(loadCompressedNormals&&loadVertexLitSpecular)result=result with{TangentLighting=SkeletalTangentLightingReader.Load(result,sources.Read)};
        return result;
    }
}
