using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

namespace Stardust.Core.Configuration;

public sealed record ReplacementLaunchProfile(int Version, string AssetRoot, string ConfigurationPath,
    string ObjectPath, InstalledAnimationChoice Idle, InstalledAnimationChoice Walk, InstalledAnimationChoice Run);

public static class ReplacementLaunchProfileCodec
{
    public static ReplacementLaunchProfile Decode(ReadOnlySpan<byte> json)
    {
        if (json.Length is < 1 or > 65536) throw new InvalidDataException("Launch profile byte budget.");
        try
        {
            _ = new UTF8Encoding(false, true).GetCharCount(json);
            using JsonDocument document = JsonDocument.Parse(json.ToArray(), new() { MaxDepth = 8 });
            JsonElement root = document.RootElement;
            Keys(root, ["Version", "AssetRoot", "ConfigurationPath", "ObjectPath", "Idle", "Walk", "Run"]);
            return CopyValidated(new(Integer(root.GetProperty("Version")), Text(root, "AssetRoot"),
                Text(root, "ConfigurationPath"), Text(root, "ObjectPath"), Choice(root.GetProperty("Idle")),
                Choice(root.GetProperty("Walk")), Choice(root.GetProperty("Run"))));
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new InvalidDataException("Invalid launch profile JSON."); }
    }

    internal static ReplacementLaunchProfile CopyValidated(ReplacementLaunchProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Version != 1) throw new InvalidDataException("Unsupported launch profile version.");
        Physical(profile.AssetRoot); Physical(profile.ConfigurationPath);
        string source = Logical(profile.ObjectPath, "object/", ".iff");
        return new(1, profile.AssetRoot, profile.ConfigurationPath, source,
            CopyChoice(profile.Idle), CopyChoice(profile.Walk), CopyChoice(profile.Run));
    }

    private static InstalledAnimationChoice Choice(JsonElement value)
    {
        Keys(value, ["TablePath", "LogicalName"], "ExplicitChildRoute");
        int[]? route = null;
        if (value.TryGetProperty("ExplicitChildRoute", out JsonElement children) && children.ValueKind != JsonValueKind.Null)
        {
            if (children.ValueKind != JsonValueKind.Array || children.GetArrayLength() > 32)
                throw new InvalidDataException("Launch route array budget.");
            route = children.EnumerateArray().Select(Integer).ToArray();
        }
        return new(Text(value, "TablePath"), Text(value, "LogicalName"), route);
    }

    private static InstalledAnimationChoice CopyChoice(InstalledAnimationChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);
        string table = Logical(choice.TablePath, "appearance/lat/", ".lat");
        if (string.IsNullOrEmpty(choice.LogicalName) || choice.LogicalName.Length > 2047 ||
            choice.LogicalName.Any(c => c < 32 || c > 126) || choice.LogicalName.TrimEnd(' ').Length == 0 ||
            choice.LogicalName.TrimEnd(' ').Equals("default", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Explicit nondefault ASCII logical name required.");
        int[]? route = null;
        if (choice.ExplicitChildRoute is { } supplied)
        {
            int count = supplied.Count;
            if (count is < 0 or > 32) throw new InvalidDataException("Launch route depth budget.");
            route = new int[count];
            for (int index = 0; index < count; index++)
            {
                int child = supplied[index];
                if (child is < 0 or > 4095) throw new InvalidDataException("Launch route index budget.");
                route[index] = child;
            }
        }
        return new(table, choice.LogicalName, route is null ? null : Array.AsReadOnly(route));
    }

    private static string Text(JsonElement value, string property)
    {
        JsonElement field = value.GetProperty(property);
        if (field.ValueKind != JsonValueKind.String) throw new InvalidDataException("Launch string field required.");
        return field.GetString()!;
    }
    private static int Integer(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
            throw new InvalidDataException("Launch integer field required.");
        return result;
    }
    private static void Keys(JsonElement value, string[] required, string? optional = null)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Launch object required.");
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
            if (!seen.Add(property.Name) || (!required.Contains(property.Name, StringComparer.Ordinal) && property.Name != optional))
                throw new InvalidDataException("Duplicate or unknown launch property.");
        if (required.Any(property => !seen.Contains(property))) throw new InvalidDataException("Missing launch property.");
    }
    private static void Physical(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4096 || value != value.Trim() || value.Any(char.IsControl) ||
            value.StartsWith(@"\\", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal) || !Path.IsPathFullyQualified(value))
            throw new InvalidDataException("Fully qualified local installation path required.");
        string rest = value.Length >= 2 && value[1] == ':' ? value[2..] : value;
        if (rest.Contains(':') || rest.Replace('\\', '/').Split('/').Any(part => part is "." or ".."))
            throw new InvalidDataException("Invalid local installation path segment.");
    }
    private static string Logical(string value, string prefix, string suffix)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value != value.Trim() || value.Any(c => c < 32 || c > 126) ||
            value.Contains(':') || value.StartsWith('/') || value.StartsWith('\\') ||
            value.Replace('\\', '/').Split('/').Any(part => part.Length == 0 || part is "." or ".."))
            throw new InvalidDataException("Explicit relative logical resource required.");
        string path = LegacyAssetPath.Normalize(value);
        if (!path.StartsWith(prefix, StringComparison.Ordinal) || !path.EndsWith(suffix, StringComparison.Ordinal))
            throw new InvalidDataException("Invalid launch resource family.");
        return path;
    }
}
