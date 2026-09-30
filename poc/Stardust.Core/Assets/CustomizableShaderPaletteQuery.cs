using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SelectedShaderPaletteFactor(int AuthoredIndex,string VariableName,bool Private,uint FactorTag,
    string PalettePath,int RequestedIndex,PaletteColorSelection? Color,bool UsedAuthoredDefault,bool MissingPalette);
public sealed record CustomizableShaderPaletteBundle(CustomizableShaderDescription Shader,
    IReadOnlyDictionary<string,PaletteDescription> Palettes,IReadOnlyList<string> MissingPalettePaths,
    IReadOnlyList<SelectedShaderPaletteFactor> AuthoredFactors,IReadOnlyDictionary<string,AssetSourceIdentity> Sources,
    IReadOnlyList<string> Diagnostics);

/// <summary>Explicit supplied/authored color inspection. Does not mutate static material factors or translate shading.</summary>
public static class CustomizableShaderPaletteQuery
{
    public static IReadOnlyList<SelectedShaderPaletteFactor> Select(CustomizableShaderDescription shader,
        Func<string,PaletteDescription?> resolve,IReadOnlyDictionary<ShaderCustomizationKey,int>? values=null)
    {
        ArgumentNullException.ThrowIfNull(shader);ArgumentNullException.ThrowIfNull(resolve);
        if(values?.Count>4096 || shader.PaletteFactors.Count>4096)throw new InvalidDataException("Palette factor query budget exceeded.");
        var snapshot=values is null?new Dictionary<ShaderCustomizationKey,int>():values.ToDictionary(pair=>pair.Key,pair=>pair.Value);
        var factors=shader.PaletteFactors.ToArray();Dictionary<string,PaletteDescription?> palettes=new(StringComparer.Ordinal);
        List<SelectedShaderPaletteFactor> result=new();
        for(int i=0;i<factors.Length;i++)
        {
            var factor=factors[i];
            if(!palettes.TryGetValue(factor.PalettePath,out PaletteDescription? palette)){palette=resolve(factor.PalettePath);palettes.Add(factor.PalettePath,palette);}
            bool supplied=snapshot.TryGetValue(new(factor.VariableName,factor.RawPrivateFlag!=0),out int index);
            if(!supplied)index=factor.AuthoredDefault;
            var color=PaletteDecoder.Select(palette??PaletteDecoder.CreateMissingFallback(),index);
            result.Add(new(i,factor.VariableName,factor.RawPrivateFlag!=0,factor.FactorTag,factor.PalettePath,index,color,!supplied,palette is null));
        }
        return result.AsReadOnly();
    }
}

public static class CustomizableShaderPaletteBundleReader
{
    public static CustomizableShaderPaletteBundle Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,
        AppearanceGraphLimits? limits=null)
    {
        OwnedAssetSourceCache cache=new(resolve,limits??new());path=LegacyAssetPath.Normalize(path);
        var input=cache.Read(path);StaticEffectDecoder.Require(input.Kind==TreLookupKind.Present && input.Payload is not null,"Shader palette root missing/masked.");
        StaticEffectDecoder.Require(cache.Sources[path].Sha256.Equals(expectedSha256,StringComparison.OrdinalIgnoreCase),"Shader palette root identity mismatch.");
        var shader=CustomizableShaderDecoder.Decode(input.Payload!);
        Dictionary<string,PaletteDescription> palettes=new(StringComparer.Ordinal);List<string> missing=new();List<string> diagnostics=new();
        PaletteDescription? Read(string member)
        {
            var selected=cache.Read(member);
            if(selected.Kind!=TreLookupKind.Present)
            {missing.Add(member);diagnostics.Add($"UnavailablePalette:{member}:{selected.Kind}");palettes.Add(member,PaletteDecoder.CreateMissingFallback());return null;}
            var palette=PaletteDecoder.Decode(selected.Payload!);palettes.Add(member,palette);
            diagnostics.AddRange(palette.Diagnostics.Select(value=>member+":"+value));return palette;
        }
        var factors=CustomizableShaderPaletteQuery.Select(shader,Read);
        foreach(var factor in factors)
        {
            if(factor.Color is null)diagnostics.Add($"EmptyPalette:{factor.PalettePath}:{factor.AuthoredIndex}");
            else if(factor.Color.SelectedIndex!=factor.RequestedIndex)diagnostics.Add($"PaletteIndexFallback:{factor.AuthoredIndex}:{factor.RequestedIndex}:0");
        }
        return new(shader,new ReadOnlyDictionary<string,PaletteDescription>(palettes),missing.AsReadOnly(),factors,cache.Sources,diagnostics.AsReadOnly());
    }
}
