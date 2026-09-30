using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace Stardust.Core.Assets;
internal sealed class OwnedAssetSourceCache
{
    private readonly Func<string,TreLookupResult> resolve;
    private readonly AppearanceGraphLimits limits;
    private readonly Dictionary<string,TreLookupResult> inputs=new(StringComparer.Ordinal);
    private readonly Dictionary<string,AssetSourceIdentity> sources=new(StringComparer.Ordinal);
    private readonly HashSet<string> attempted=new(StringComparer.Ordinal);
    private readonly Dictionary<string,InvalidDataException> failed=new(StringComparer.Ordinal);
    private long bytes;
    public IReadOnlyDictionary<string,AssetSourceIdentity> Sources{get;}
    internal OwnedAssetSourceCache(Func<string,TreLookupResult> resolve,AppearanceGraphLimits limits)
    {ArgumentNullException.ThrowIfNull(resolve);if(limits.MaximumResources<1 || limits.MaximumSourceBytes<1)throw new ArgumentOutOfRangeException(nameof(limits));this.resolve=resolve;this.limits=limits;Sources=new ReadOnlyDictionary<string,AssetSourceIdentity>(sources);}
    internal TreLookupResult Read(string path)
    {
        path=LegacyAssetPath.Normalize(path);if(inputs.TryGetValue(path,out TreLookupResult? cached))return cached;
        if(failed.TryGetValue(path,out InvalidDataException? priorFailure))throw priorFailure;
        if(!attempted.Contains(path) && attempted.Count>=limits.MaximumResources)throw new AppearanceGraphBudgetException("Asset source count budget.");attempted.Add(path);
        TreLookupResult source;try{source=resolve(path);}catch(InvalidDataException failure){failed.Add(path,failure);throw;}
        if(source.Kind==TreLookupKind.Present && source.Payload is not null)
        {
            bytes=checked(bytes+source.Payload.Length);if(bytes>limits.MaximumSourceBytes)throw new AppearanceGraphBudgetException("Asset source byte budget.");source=source with{Payload=source.Payload.ToArray()};sources.Add(path,new(path,source.Priority,source.Payload.Length,Convert.ToHexString(SHA256.HashData(source.Payload))));
        }
        inputs.Add(path,source);return source;
    }
    internal byte[] ReadPinned(string path,string expectedSha256)
    {path=LegacyAssetPath.Normalize(path);TreLookupResult source=Read(path);if(source.Kind!=TreLookupKind.Present || source.Payload is null || !sources[path].Sha256.Equals(expectedSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Pinned asset missing/masked or identity mismatch.");return source.Payload;}
}
