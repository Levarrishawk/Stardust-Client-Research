using System.Collections.ObjectModel;
using System.Security.Cryptography;
namespace Stardust.Core.Assets;

/// <summary>Owned pair for the pinned original-human manual detail inspector.</summary>
public sealed record RetainedSkeletalDetailBundle(CreatureAppearanceDescriptorBundle Detail2, CreatureAppearanceDescriptorBundle Detail3,
    IReadOnlyDictionary<string, AssetSourceIdentity> Sources, int ExternalReads, long SourceBytes);
public sealed record RetainedSkeletalDetailSet(IReadOnlyDictionary<int, CreatureAppearanceDescriptorBundle> Details,
    IReadOnlyDictionary<string, AssetSourceIdentity> Sources, int ExternalReads, long SourceBytes);
/// <summary>Prepares pinned original details through unchanged readers and one bounded union cache; Load preserves the R4 pair and LoadAll owns 0..3. Caller owns the resolver.</summary>
public static class RetainedSkeletalDetailBundleReader
{
    public const string ObjectPath = "object/creature/player/shared_human_male.iff";
    public const string ObjectPin = "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243";
    public const string AppearancePin = "951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C";
    public const string ConfigurationPin = "22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";
    public static RetainedSkeletalDetailBundle Load(Func<string, TreLookupResult> resolve, CancellationToken token,
        int maximumResources = 64, long maximumBytes = 16 * 1024 * 1024)
    {
        var set = Prepare(resolve, token, new[] { 2, 3 }, maximumResources, maximumBytes);
        return new(set.Details[2], set.Details[3], set.Sources, set.ExternalReads, set.SourceBytes);
    }
    public static RetainedSkeletalDetailSet LoadAll(Func<string, TreLookupResult> resolve, CancellationToken token,
        int maximumResources = 128, long maximumBytes = 32 * 1024 * 1024)
        => Prepare(resolve, token, new[] { 0, 1, 2, 3 }, maximumResources, maximumBytes);
    private static RetainedSkeletalDetailSet Prepare(Func<string, TreLookupResult> resolve, CancellationToken token,
        int[] details, int maximumResources, long maximumBytes)
    {
        if (maximumResources < 1 || maximumBytes < 1) throw new ArgumentOutOfRangeException(nameof(maximumResources));
        ArgumentNullException.ThrowIfNull(resolve);
        var inputs = new Dictionary<string, TreLookupResult>(StringComparer.Ordinal);
        var identities = new Dictionary<string, AssetSourceIdentity>(StringComparer.Ordinal);
        long bytes = 0; int reads = 0;
        TreLookupResult Read(string path)
        {
            token.ThrowIfCancellationRequested(); path = LegacyAssetPath.Normalize(path);
            if (inputs.TryGetValue(path, out var cached)) return cached;
            if (inputs.Count >= maximumResources) throw new AppearanceGraphBudgetException("Retained detail union source count budget.");
            var source = resolve(path); reads++; token.ThrowIfCancellationRequested();
            if (source.Kind == TreLookupKind.Present && source.Payload is not null)
            {
                if (source.Payload.LongLength > maximumBytes - bytes) throw new AppearanceGraphBudgetException("Retained detail union source byte budget.");
                source = source with { Payload = source.Payload.ToArray() }; bytes += source.Payload.Length;
                identities.Add(path, new(path, source.Priority, source.Payload.Length, Convert.ToHexString(SHA256.HashData(source.Payload))));
            }
            inputs.Add(path, source); return source;
        }
        CreatureAppearanceDescriptorBundle Prepare(int detail) => CreatureAppearanceDescriptorBundleReader.Load(Read, ObjectPath, ObjectPin, AppearancePin,
            meshDetail: detail, limits: new(MaximumResources: 64, MaximumSourceBytes: 16 * 1024 * 1024),
            loadSkeletons: true, loadSkinSources: true, loadGeometry: true, loadLogicalTables: true,
            loadMaterialDefaults: true, loadPaletteDefaults: true, loadAlbedoFactors: true, loadVertexLitSpecular: true, loadTextureChoices: detail < 2, loadCompressedNormals: detail < 2,
            animationSelection: new("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"), prepareSelectedAnimation: true);
        try
        {
            var prepared = new Dictionary<int, CreatureAppearanceDescriptorBundle>();
            foreach (int detail in details) { prepared.Add(detail, Prepare(detail)); token.ThrowIfCancellationRequested(); }
            string[] pins = {
                "93378D74AA75B1C6673C9008914FFB51CF7DC380FF18459DBBFC26EF016A59B0", "84DFF58013C8374D80D691549A67E009A84344649010C9A4365552C9FCE48F19",
                "D477E22985A49F7F2FBA51828351435D41F74E277A186AF177F54FFE5D86E863", "E544585B9E206E9537BAC02967722F67D3768595C9E2E82EEE3B189798C74D8A",
                "4227C5E64B5C490B4A2E0A7D106AEAB5A4F8E84715BDB0D6E7E210114A94EED3", "3F4F4833DC4294D0F0EFBAE8322817357B1FAD4BF0BF1523E1DF92EDA9F6B860",
                "6128DE248A97B3C57F89E2DA883DBF469B0CF1FBDF0EC53B77F5BDD3D62A49C9", "309FFBE0DF0ED8D9AD23AF3F87B0B9C6CEA94221329A581840956CFDCED9E0F1" };
            var materialPins = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["shader/hum_m_body_sm.sht"] = "16471F99D8960C6D481606E477C397C3729B07299FF450AE5D8E760EA462178D",
                ["effect/h_specmap.eft"] = "ED4CE5EADAC3CA080A5E21DF5BABC95739634C41228A0DCDFDA167E57A5E4C4C",
                ["texture/hum_m_sm.dds"] = "93C9063BBA30A4325B87FFC43C367B5F463967FF9ECD9942D4C8498393F7B51E",
                ["palette/pc_skin_hum.pal"] = "09443BDC0DE66A85D0DEB15813CCB9FCBFE6F5F8A6E417CF6D46DC910A494160",
                ["pixel_program/h_specmap_ps20_nodot3.psh"] = "C7A5218B031D9C0CD80C33C7D31611CAC4AFD7A241C794354879070DFD7DDB85"
            };
            foreach (var pin in materialPins)
                if (!identities.TryGetValue(pin.Key, out var identity) || identity.Sha256 != pin.Value) throw new InvalidDataException("Pinned authored detail material source missing/changed.");
            foreach (var pair in prepared)
                foreach (string part in new[] { "body", "head" })
                    if (pair.Value.Sources[$"appearance/mesh/hum_m_{part}_l{pair.Key}.mgn"].Sha256 != pins[pair.Key * 2 + (part == "body" ? 0 : 1)]) throw new InvalidDataException("Selected MGN identity mismatch.");
            long decodedBytes = 0;
            foreach (var pair in prepared)
            {
                var bundle = pair.Value;
                if ((pair.Key >= 2 ? bundle.MaterialBundle?.CompressedNormals is not null : bundle.MaterialBundle?.CompressedNormals is null) || bundle.MaterialBundle?.VertexLitSpecular is null || bundle.PreparedAnimation is null)
                    throw new InvalidDataException("Owned detail material family and idle required.");
                long decoded = bundle.MaterialBundle.Textures.Values.Sum(t => t.Mips.Sum(m => (long)m.Rgba.Length)); decodedBytes += decoded;
                if (decoded > (pair.Key < 2 ? 48L : 2L) * 1024 * 1024)
                    throw new AppearanceGraphBudgetException("Retained detail decoded image budget.");
                foreach (var row in bundle.Sources.Values) if (identities[row.Path] != row) throw new InvalidDataException("Union source identity changed.");
            }
            if (decodedBytes > 100L * 1024 * 1024) throw new AppearanceGraphBudgetException("Retained aggregate decoded image budget.");
            var animation = prepared.Values.First().PreparedAnimation!;
            foreach (var bundle in prepared.Values)
                if (animation.FrameRate != bundle.PreparedAnimation!.FrameRate || animation.EndFrame != bundle.PreparedAnimation.EndFrame || animation.ProxyTimeScale != bundle.PreparedAnimation.ProxyTimeScale)
                    throw new InvalidDataException("Retained idle time bases differ.");
            return new(new ReadOnlyDictionary<int, CreatureAppearanceDescriptorBundle>(prepared), new ReadOnlyDictionary<string, AssetSourceIdentity>(identities), reads, bytes);
        }
        finally { inputs.Clear(); } // Drop shared raw byte copies on success, rejection and cancellation.
    }
}
