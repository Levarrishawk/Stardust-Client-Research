using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

/// <summary>Owned blueprint dependencies, without implicit customization or rendering.</summary>
public sealed record BlueprintFaceResources(
    IReadOnlyDictionary<string,StaticShaderReferenceDescription> Shaders,
    IReadOnlyDictionary<string,StaticEffectDescription> EffectsByShader,
    IReadOnlyDictionary<string,DdsImage> Textures,
    IReadOnlyDictionary<string,PaletteDescription> Palettes,
    IReadOnlyDictionary<string,BlueprintFacePixelProgram>? PixelPrograms=null);

public static class BlueprintFaceResourceReader
{
    public static BlueprintFaceResources Load(BlueprintTextureRendererDescription blueprint,Func<string,TreLookupResult> resolve,
        AppearanceGraphLimits? limits=null,bool loadPixelPrograms=false)
        => Load(new[]{blueprint},new OwnedAssetSourceCache(resolve,limits??new(MaximumResources:64,MaximumSourceBytes:16*1024*1024)),loadPixelPrograms);

    internal static BlueprintFaceResources Load(IEnumerable<BlueprintTextureRendererDescription> blueprints,OwnedAssetSourceCache sources,bool loadPixelPrograms=false)
    {
        var shaders=new Dictionary<string,StaticShaderReferenceDescription>(StringComparer.Ordinal);
        var effects=new Dictionary<string,StaticEffectDescription>(StringComparer.Ordinal);
        var textures=new Dictionary<string,DdsImage>(StringComparer.Ordinal);
        var palettes=new Dictionary<string,PaletteDescription>(StringComparer.Ordinal);
        var programs=loadPixelPrograms?new Dictionary<string,BlueprintFacePixelProgram>(StringComparer.Ordinal):null;
        long decodedBytes=0;
        byte[] Required(string path)
        {
            var source=sources.Read(path);
            if(source.Kind!=TreLookupKind.Present||source.Payload is null)throw new InvalidDataException("Blueprint dependency missing/masked.");
            return source.Payload;
        }
        foreach(var blueprint in blueprints)
        {
            foreach(string path in blueprint.ShaderPaths.Distinct(StringComparer.Ordinal))
            {
                if(shaders.ContainsKey(path))continue;
                if(shaders.Count>=64)throw new AppearanceGraphBudgetException("Blueprint shader count budget.");
                var shader=StaticShaderReferences.DecodeIncludingInlineEffect(Required(path));
                shaders.Add(path,shader);
                effects.Add(path,shader.InlineEffect is null?StaticEffectDecoder.Decode(Required(shader.EffectPath)):StaticEffectDecoder.DecodeInlineEffect(shader));
            }
            foreach(string path in blueprint.TexturePaths.Concat(blueprint.ShaderPaths.SelectMany(path=>shaders[path].Textures).Select(binding=>binding.AssetPath).OfType<string>()).Distinct(StringComparer.Ordinal))
            {
                if(textures.ContainsKey(path))continue;
                if(textures.Count>=64)throw new AppearanceGraphBudgetException("Blueprint texture count budget.");
                long remaining=134_217_728-decodedBytes;
                if(remaining<4)throw new AppearanceGraphBudgetException("Blueprint decoded pixel budget.");
                DdsImage image;
                try { image=DdsDecoder.Decode(Required(path),new(MaximumDecodedBytes:(int)remaining,AllowLegacyTerminalBlockTail:true)); }
                catch(InvalidDataException error) { throw new InvalidDataException($"Blueprint texture {path}: {error.Message}",error); }
                decodedBytes+=image.Mips.Sum(mip=>(long)mip.Rgba.Length);
                textures.Add(path,image);
            }
            foreach(string path in blueprint.PrepareLists.SelectMany(list=>list).Select(op=>op.PalettePath).OfType<string>().Distinct(StringComparer.Ordinal))
            {
                if(palettes.ContainsKey(path))continue;
                if(palettes.Count>=64)throw new AppearanceGraphBudgetException("Blueprint palette count budget.");
                palettes.Add(path,PaletteDecoder.Decode(Required(path)));
            }
        }
        if(programs is not null)
            foreach(string path in effects.Values.SelectMany(effect=>effect.Implementations).SelectMany(impl=>impl.Passes).Select(pass=>pass.PixelProgram).OfType<string>().Distinct(StringComparer.Ordinal))
            {
                if(programs.Count>=64)throw new AppearanceGraphBudgetException("Blueprint pixel-program count budget.");
                programs.Add(path,BlueprintFacePixelProgramDecoder.Decode(Required(path)));
            }
        return new(new ReadOnlyDictionary<string,StaticShaderReferenceDescription>(shaders),
            new ReadOnlyDictionary<string,StaticEffectDescription>(effects),new ReadOnlyDictionary<string,DdsImage>(textures),
            new ReadOnlyDictionary<string,PaletteDescription>(palettes),programs is null?null:new ReadOnlyDictionary<string,BlueprintFacePixelProgram>(programs));
    }
}
