using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record SkeletalVertexLitSourceIdentity(IReadOnlyDictionary<string,string> Sha256);

/// <summary>Bounded original mouth binding/include identity; no compiler or retained original source.</summary>
public static class SkeletalVertexLitLightingSources
{
    public const string VertexPath="vertex_program/a_specmap_vs20_nodot3.vsh";
    public static IReadOnlyDictionary<string,string> ExpectedSha256 {get;}=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(StringComparer.Ordinal)
    {
        [VertexPath]="AD5D1F09358DA7B9F20A82E126538297C333A502E291CB9F0DD005C9B6B5E408",
        [TangentLightingSourcesReader.FunctionsPath]="CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15",
        [TangentLightingSourcesReader.ConstantsPath]="C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9",
        [TangentLightingSourcesReader.SharedFunctionsPath]="A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A"
    });
    public static SkeletalVertexLitSourceIdentity Load(StaticEffectDescription effect,string pixelProgram,Func<string,TreLookupResult> resolve)
    {
        ArgumentNullException.ThrowIfNull(effect);ArgumentNullException.ThrowIfNull(resolve);
        var passes=effect.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram==pixelProgram).ToArray();
        if(pixelProgram is not ("pixel_program/h_color2_specmap_ps20_nodot3.psh" or "pixel_program/h_specmap_ps20_nodot3.psh")||passes.Length!=1||passes[0].VertexProgram!=VertexPath||effect.ContainsPrecalculatedVertexLighting)
            throw new InvalidDataException("Selected mouth EFCT/PVSH binding differs.");
        foreach(var pair in ExpectedSha256)
        {
            var source=resolve(pair.Key);
            if(source.Kind!=TreLookupKind.Present||source.Payload is null||source.Payload.Length>65536||Convert.ToHexString(SHA256.HashData(source.Payload))!=pair.Value)
                throw new InvalidDataException("Selected mouth source/include missing, masked, truncated, oversized or changed.");
        }
        return new(ExpectedSha256);
    }
}
