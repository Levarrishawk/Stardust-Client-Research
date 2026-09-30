using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record TexturedDetailAppearance(DetailAppearanceDescription Description,
    IReadOnlyDictionary<string,TexturedStaticMesh> Children, IReadOnlyDictionary<string,AssetSourceIdentity> Sources);

/// <summary>Explicit bounded eager inspector bundle; lazy game instances and fetch-or-default behavior remain separate.</summary>
public static class TexturedDetailAppearanceReader
{
    public static TexturedDetailAppearance Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        path=LegacyAssetPath.Normalize(path);
        Dictionary<string,TreLookupResult> cache=new(StringComparer.Ordinal);
        Dictionary<string,AssetSourceIdentity> sources=new(StringComparer.Ordinal);
        long sourceBytes=0,decodedBytes=0;
        TreLookupResult Read(string resource)
        {
            if(cache.TryGetValue(resource,out TreLookupResult? cached)) return cached;
            if(cache.Count >= 256) throw new InvalidDataException("Detail dependency count exceeded.");
            TreLookupResult value=resolve(resource);
            if(value.Kind != TreLookupKind.Present || value.Payload is null) throw new InvalidDataException("Detail inspector requires all authored resources.");
            sourceBytes+=value.Payload.Length;
            if(sourceBytes>134_217_728) throw new InvalidDataException("Detail aggregate source budget exceeded.");
            sources.Add(resource,new(resource,value.Priority,value.Payload.Length,Convert.ToHexString(SHA256.HashData(value.Payload))));
            cache.Add(resource,value);return value;
        }
        AppearanceResolution resolution=AppearanceResolver.FetchStrict(Read,path,expectedSha256);
        DetailAppearanceDescription description=DetailAppearanceDecoder.Decode(resolution.TerminalBytes.Span);
        if(description.Levels.Count is < 1 or > 8) throw new InvalidDataException("Detail inspector level budget exceeded.");
        Dictionary<string,TexturedStaticMesh> children=new(StringComparer.Ordinal);
        foreach(DetailAppearanceLevel level in description.Levels)
        {
            if(level.ChildPath is null) throw new InvalidDataException("Detail inspector requires a child for each band.");
            if(children.ContainsKey(level.ChildPath)) continue;
            Read(level.ChildPath);
            TexturedStaticMesh child=TexturedStaticMeshReader.Load(Read,level.ChildPath,sources[level.ChildPath].Sha256);
            decodedBytes+=child.Textures.Values.Sum(image=>image.Mips.Sum(mip=>(long)mip.Rgba.Length));
            if(decodedBytes>134_217_728) throw new InvalidDataException("Detail aggregate decoded pixel budget exceeded.");
            children.Add(level.ChildPath,child);
        }
        return new(description,new ReadOnlyDictionary<string,TexturedStaticMesh>(children),new ReadOnlyDictionary<string,AssetSourceIdentity>(sources));
    }
}
