using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record TangentLightingSourceIdentity(IReadOnlyDictionary<string,string> Sha256)
{
    public string VertexProgramSha256=>Sha256[TangentLightingSourcesReader.VertexPath];
    public string FunctionsSha256=>Sha256[TangentLightingSourcesReader.FunctionsPath];
}

/// <summary>Exact selected VSH and complete authored include graph; this owns identity, not a legacy compiler.</summary>
public static class TangentLightingSourcesReader
{
    public const string VertexPath="vertex_program/a_specmap_bump_vs20_for_ps20.vsh";
    public const string FunctionsPath="vertex_program/include/functions.inc";
    public const string ConstantsPath="vertex_program/include/vertex_shader_constants.inc";
    public const string SharedFunctionsPath="shared_program/functions.inc";
    private static readonly IReadOnlyDictionary<string,string> identities=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>(StringComparer.Ordinal)
    {
        [VertexPath]="0159BA23D91FA517A6BDF16488B4C95A0CA599DC448AF12CEACC48C3448362D6",
        [FunctionsPath]="CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15",
        [ConstantsPath]="C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9",
        [SharedFunctionsPath]="A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A"
    });
    public static TangentLightingSourceIdentity Load(Func<string,TreLookupResult> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        foreach(var pair in identities)
        {
            var input=resolve(pair.Key);
            if(input.Kind!=TreLookupKind.Present||input.Payload is null||input.Payload.Length>65536||Convert.ToHexString(SHA256.HashData(input.Payload))!=pair.Value)
                throw new InvalidDataException("Selected tangent vertex source/include identity missing, masked, oversized or changed.");
        }
        return new(identities);
    }
}
