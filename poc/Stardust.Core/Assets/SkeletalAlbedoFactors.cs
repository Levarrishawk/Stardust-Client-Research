using System.Collections.ObjectModel;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record SkeletalAlbedoFactors(string PixelProgram,bool UsesAlphaHueMask,uint FirstTag,uint SecondTag,uint FirstArgb,uint SecondArgb);
public sealed record SkeletalAlbedoFactorBundle(IReadOnlyDictionary<string,SkeletalAlbedoProgram> Programs,
    IReadOnlyDictionary<string,SkeletalAlbedoFactors> Materials);

/// <summary>Validated albedo-factor slice; original lighting/specular/opacity are not translated.</summary>
internal static class SkeletalAlbedoFactorReader
{
    internal static SkeletalAlbedoFactorBundle Load(SkeletalMaterialBundle bundle,OwnedAssetSourceCache sources)
    {
        if(bundle.PaletteDefaults is null)throw new InvalidDataException("Albedo factors require owned palette defaults.");
        var programs=new Dictionary<string,SkeletalAlbedoProgram>(StringComparer.Ordinal);var materials=new Dictionary<string,SkeletalAlbedoFactors>(StringComparer.Ordinal);
        foreach(var pair in bundle.Materials)
        {
            var material=pair.Value;var effect=bundle.Effects[material.References.EffectPath];
            // Explicit PS1.1 inspection candidate; no hardware capability selection is implied.
            var passes=effect.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0])
                .Where(p=>p.PixelProgram?.EndsWith("_ps11.psh",StringComparison.Ordinal)==true).ToArray();
            Need(passes.Length==1,"Albedo factor policy requires one PS1.1 candidate.");var pass=passes[0];string path=pass.PixelProgram!;
            if(!programs.TryGetValue(path,out var program))
            {
                Need(programs.Count<64,"Albedo program count budget.");var source=sources.Read(path);
                Need(source.Kind==TreLookupKind.Present&&source.Payload is not null,"Albedo program missing/masked.");
                program=SkeletalAlbedoProgramDecoder.Decode(source.Payload!);programs.Add(path,program);
            }
            uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out uint value),"Albedo pass field absent.");return value;}
            Need(Field(0x19)==1&&Field(0x1A)==1&&Field(0x20)==0&&Field(0x30)==0&&Field(0x3C)==15,"Albedo policy requires opaque depth-tested MAIN.");
            Need(pass.PixelTextures.Count>=1&&pass.PixelTextures[0].Stage==0&&pass.PixelTextures[0].TextureTag==0x4D41494E,"Albedo stage0 is not MAIN.");
            uint first=Field(0x40),second=Field(0x44);Need(first==0x4D41494E&&second==(program.UsesAlphaHueMask?0x48554542u:0u),"Albedo factor identity differs.");
            bundle.PaletteDefaults.TryGetValue(pair.Key,out var palette);
            materials.Add(pair.Key,WithPalette(material,palette,new(path,program.UsesAlphaHueMask,first,second,0,0)));
        }
        return new(new ReadOnlyDictionary<string,SkeletalAlbedoProgram>(programs),new ReadOnlyDictionary<string,SkeletalAlbedoFactors>(materials));
    }
    internal static SkeletalAlbedoFactors WithPalette(StaticMeshMaterial material,CustomizableShaderPaletteBundle? palette,SkeletalAlbedoFactors description)
    {
        var factors=StaticShaderFactors.Decode(material.References).ToDictionary(p=>p.Key,p=>p.Value);
        if(palette is not null)
        {
            var unique=new HashSet<uint>();
            foreach(var selected in palette.AuthoredFactors)
            {
                Need(unique.Add(selected.FactorTag)&&selected.Color is not null,"Duplicate/empty selected palette factor.");
                factors[selected.FactorTag]=selected.Color!.OpaqueArgb;
            }
        }
        Need(factors.TryGetValue(description.FirstTag,out uint firstColor),"First albedo factor absent.");uint secondColor=firstColor;
        if(description.UsesAlphaHueMask)Need(factors.TryGetValue(description.SecondTag,out secondColor),"Second albedo factor absent.");
        return description with{FirstArgb=firstColor,SecondArgb=secondColor};
    }
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
}

public static class SkeletalAlbedoEvaluator
{
    /// <summary>Normalized sampled channels in their source encoding; gamma conversion belongs to the engine adapter.</summary>
    public static Vector3 Evaluate(Vector4 sample,SkeletalAlbedoFactors factors)
    {
        ArgumentNullException.ThrowIfNull(factors);
        if(!float.IsFinite(sample.X)||!float.IsFinite(sample.Y)||!float.IsFinite(sample.Z)||!float.IsFinite(sample.W)||sample.X<0||sample.Y<0||sample.Z<0||sample.W<0||sample.X>1||sample.Y>1||sample.Z>1||sample.W>1)throw new InvalidDataException("Invalid albedo sample.");
        Vector3 Color(uint argb)=>new(((argb>>16)&255)/255f,((argb>>8)&255)/255f,(argb&255)/255f);
        var first=Color(factors.FirstArgb);var hue=factors.UsesAlphaHueMask?Vector3.Lerp(first,Color(factors.SecondArgb),sample.W):first;
        return new Vector3(sample.X,sample.Y,sample.Z)*hue;
    }
}
