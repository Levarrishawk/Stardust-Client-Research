using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Stardust.Core.Assets;

public sealed record ObjectTemplateCatalogProfileLimits(int MaximumBytes = 1048576, int MaximumDepth = 4,
    int MaximumEntries = 256, int MaximumPathCharacters = 262144);

/// <summary>Strict caller-authored JSON configuration, not an original global template table.</summary>
public static class ObjectTemplateCatalogProfile
{
    public const int Version = 1;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static ObjectTemplateCatalog Decode(ReadOnlySpan<byte> source, ObjectTemplateCatalogProfileLimits? limits = null)
    {
        limits ??= new(); Validate(limits);
        Require(source.Length <= limits.MaximumBytes, "Catalog profile byte budget exceeded.");
        try
        {
            _ = StrictUtf8.GetCharCount(source);
            using var document = JsonDocument.Parse(source.ToArray(), new JsonDocumentOptions
                { MaxDepth = limits.MaximumDepth, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
            JsonElement root = document.RootElement;
            var fields = Fields(root, "version", "entries");
            Require(Unsigned(fields["version"]) == Version, "Unsupported catalog profile version.");
            JsonElement array = fields["entries"];
            Require(array.ValueKind == JsonValueKind.Array, "Catalog entries array required.");
            Require(array.GetArrayLength() <= limits.MaximumEntries, "Catalog profile entry budget exceeded.");
            var entries = new List<ObjectTemplateCatalogEntry>();
            foreach (JsonElement value in array.EnumerateArray())
            {
                var entry = Fields(value, "templateCrc", "objectPath", "objectSha256", "appearanceSha256", "recipe");
                string path = String(entry["objectPath"]);
                Require(!path.Replace('\\', '/').Split('/').Any(s => s is "." or ".."), "Literal dot path segments are unsupported.");
                string recipe = String(entry["recipe"]);
                SceneObjectResourceRecipe kind = recipe switch
                {
                    "creatureDetail0" => SceneObjectResourceRecipe.CreatureDetail0,
                    "staticAppearance" => SceneObjectResourceRecipe.StaticAppearance,
                    _ => throw new InvalidDataException("Unsupported symbolic catalog recipe.")
                };
                entries.Add(new(Unsigned(entry["templateCrc"]), path, String(entry["objectSha256"]), String(entry["appearanceSha256"]), kind));
            }
            return new(entries, limits.MaximumEntries, limits.MaximumPathCharacters);
        }
        catch (JsonException error) { throw new InvalidDataException("Malformed catalog profile JSON.", error); }
        catch (DecoderFallbackException error) { throw new InvalidDataException("Malformed catalog profile UTF-8.", error); }
        catch (InvalidOperationException error) { throw new InvalidDataException("Invalid catalog profile JSON string.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Invalid catalog profile entry.", error); }
    }

    /// <summary>Canonical compact UTF-8, fixed property order and ascending unsigned CRC. Returned bytes are caller-owned.</summary>
    public static byte[] Encode(ObjectTemplateCatalog catalog, ObjectTemplateCatalogProfileLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(catalog); limits ??= new(); Validate(limits);
        // Reuse the accepted entry/path gate before allocating serialized output.
        try { _ = new ObjectTemplateCatalog(catalog.Entries.Values, limits.MaximumEntries, limits.MaximumPathCharacters); }
        catch (ArgumentException error) { throw new InvalidDataException("Catalog profile output entry/path budget exceeded.", error); }
        Require(limits.MaximumDepth >= (catalog.Entries.Count == 0 ? 2 : 3), "Catalog profile output depth budget exceeded.");
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject(); writer.WriteNumber("version", Version); writer.WriteStartArray("entries");
            foreach (var entry in catalog.Entries.Values.OrderBy(e => e.TemplateCrc))
            {
                writer.WriteStartObject(); writer.WriteNumber("templateCrc", entry.TemplateCrc);
                writer.WriteString("objectPath", entry.ObjectPath); writer.WriteString("objectSha256", entry.ObjectSha256);
                writer.WriteString("appearanceSha256", entry.AppearanceSha256);
                writer.WriteString("recipe", entry.Recipe == SceneObjectResourceRecipe.CreatureDetail0 ? "creatureDetail0" : "staticAppearance");
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject(); writer.Flush();
        }
        Require(output.Length <= limits.MaximumBytes, "Catalog profile output byte budget exceeded.");
        return output.ToArray();
    }

    private static Dictionary<string, JsonElement> Fields(JsonElement value, params string[] names)
    {
        Require(value.ValueKind == JsonValueKind.Object, "Catalog profile object required.");
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
            Require(names.Contains(property.Name, StringComparer.Ordinal) && result.TryAdd(property.Name, property.Value), "Unknown or duplicate catalog profile property.");
        Require(result.Count == names.Length, "Missing catalog profile property.");
        return result;
    }
    private static string String(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.String, "Catalog profile string required.");
        return value.GetString()!;
    }
    private static uint Unsigned(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9'), "Unsigned decimal integer required.");
        Require(value.TryGetUInt32(out uint result), "Catalog profile integer outside uint32."); return result;
    }
    private static void Validate(ObjectTemplateCatalogProfileLimits limits)
    {
        if (limits.MaximumBytes is < 1 or > 16777216 || limits.MaximumDepth is < 1 or > 16 ||
            limits.MaximumEntries is < 1 or > 4096 || limits.MaximumPathCharacters is < 1 or > 4194304)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
