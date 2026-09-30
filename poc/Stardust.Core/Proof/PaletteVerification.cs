using System.Security.Cryptography;
using Stardust.Core.Assets;

namespace Stardust.Core.Proof;
public static class PaletteVerification
{
    public static string ComputeFactorsSha256(CustomizableShaderPaletteBundle bundle,int? forcedValue=null)
    {
        var values=forcedValue.HasValue?bundle.Shader.PaletteFactors.Select(factor=>new ShaderCustomizationKey(factor.VariableName,factor.RawPrivateFlag!=0)).Distinct().ToDictionary(key=>key,_=>forcedValue.Value):null;
        var factors=CustomizableShaderPaletteQuery.Select(bundle.Shader,path=>bundle.MissingPalettePaths.Contains(path,StringComparer.Ordinal)?null:bundle.Palettes[path],values);
        using MemoryStream bytes=new();using BinaryWriter writer=new(bytes);writer.Write(factors.Count);
        foreach(var factor in factors)
        {writer.Write(factor.AuthoredIndex);writer.Write(factor.FactorTag);writer.Write(factor.RequestedIndex);writer.Write(factor.Color is not null);if(factor.Color is not null){writer.Write(factor.Color.SelectedIndex);writer.Write(factor.Color.OpaqueArgb);writer.Write(factor.Color.RawFlags);}writer.Write(factor.UsedAuthoredDefault);writer.Write(factor.MissingPalette);}
        writer.Flush();return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
    }
    public static string ComputeSha256(PaletteDescription palette)
    {
        using MemoryStream bytes=new();using BinaryWriter writer=new(bytes);writer.Write(palette.Version);writer.Write(palette.Entries.Count);
        foreach(var entry in palette.Entries){writer.Write(entry.Red);writer.Write(entry.Green);writer.Write(entry.Blue);writer.Write(entry.RawFlags);writer.Write(entry.OpaqueArgb);}
        writer.Write(palette.IgnoredTrailingBytes.Length);writer.Write(palette.IgnoredTrailingBytes.Span);writer.Flush();return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
    }
    public static string ComputeSelectionsSha256(PaletteDescription palette)
    {
        using MemoryStream bytes=new();using BinaryWriter writer=new(bytes);
        foreach(int index in Enumerable.Range(0,palette.Entries.Count).Concat(new[]{int.MinValue,-1,palette.Entries.Count,palette.Entries.Count+1,int.MaxValue}))
        {
            PaletteColorSelection? selected=PaletteDecoder.Select(palette,index);writer.Write(index);writer.Write(selected is not null);
            if(selected is not null){writer.Write(selected.SelectedIndex);writer.Write(selected.OpaqueArgb);writer.Write(selected.RawFlags);}
        }
        writer.Flush();return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
    }
}
