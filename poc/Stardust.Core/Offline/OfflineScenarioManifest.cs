using System.Text;
using System.Text.Json;

namespace Stardust.Core.Offline;

/// <summary>Strict authored offline configuration, independent of engine and transport.</summary>
public sealed class OfflineScenarioManifest
{
    public const int MaximumJsonBytes = 262144;
    private static readonly string[] InputNames = ["asset-root", "asset-config", "mixed-peer", "catalog-profile",
        "motion-peer", "face-profile", "customization-peer", "customization-dictionary", "customization-asset-context",
        "customization-name-context", "tangible-peer", "tangible6-peer", "creature6-full-peer",
        "creature4-partial-peer", "terrain-peer", "terrain-material-profile"];
    private static readonly string[] OptionNames = ["local-input", "customization-wire", "customization-scalars",
        "customization-full", "customization-extended", "customization-named", "terminal-animation", "terminal-clock"];
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly string[] inputs;
    private readonly string[] profiles;
    private readonly bool[] options;
    private OfflineScenarioManifest(string[] inputs, string[] profiles, bool[] options)
    { this.inputs = inputs; this.profiles = profiles; this.options = options; }
    public int RunCount => profiles.Length;

    public string[] ArgumentsForRun(int oneBased)
    {
        if (oneBased < 1 || oneBased > RunCount) throw new ArgumentOutOfRangeException(nameof(oneBased));
        var result = new List<string>(42);
        for (int i = 0; i < InputNames.Length; i++) { result.Add("--" + InputNames[i]); result.Add(inputs[i]); }
        result.Add("--terrain-point-target-profile"); result.Add(profiles[oneBased - 1]);
        for (int i = 0; i < OptionNames.Length; i++) if (options[i]) result.Add("--" + OptionNames[i]);
        return result.ToArray();
    }

    public static OfflineScenarioManifest Parse(byte[] utf8, string manifestDirectory)
    {
        ArgumentNullException.ThrowIfNull(utf8);
        if (utf8.Length is 0 or > MaximumJsonBytes) throw Invalid();
        try
        {
            _ = StrictUtf8.GetCharCount(utf8);
            using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions
                { MaxDepth = 4, AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
            NoDuplicates(document.RootElement);
            var root = document.RootElement;
            ExactKeys(root, ["Schema", "RunCount", "Inputs", "Options", "PointTargetProfiles"]);
            if (Integer(root.GetProperty("Schema")) != 1) throw Invalid();
            int count = Integer(root.GetProperty("RunCount"));
            if (count is < 1 or > 32) throw Invalid();
            var inputObject = root.GetProperty("Inputs"); var optionObject = root.GetProperty("Options");
            ExactKeys(inputObject, InputNames); ExactKeys(optionObject, OptionNames);
            var profileArray = root.GetProperty("PointTargetProfiles");
            if (profileArray.ValueKind != JsonValueKind.Array || profileArray.GetArrayLength() != count) throw Invalid();
            // Validate directory explicitly: a relative base would silently depend on process cwd.
            PathText(manifestDirectory);
            if (!Path.IsPathFullyQualified(manifestDirectory) || !Directory.Exists(manifestDirectory)) throw Invalid();
            string directory = Path.GetFullPath(manifestDirectory);
            var inputs = new string[InputNames.Length]; var options = new bool[OptionNames.Length];
            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = Resolve(Text(inputObject.GetProperty(InputNames[i])), directory, i == 0);
            for (int i = 0; i < options.Length; i++)
            {
                var value = optionObject.GetProperty(OptionNames[i]);
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid();
                options[i] = value.GetBoolean();
            }
            var profiles = new string[count];
            for (int i = 0; i < count; i++) profiles[i] = Resolve(Text(profileArray[i]), directory, false);
            return new(inputs, profiles, options);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException
            or OverflowException or NotSupportedException)
        { throw Invalid(); }
    }

    private static void NoDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in value.EnumerateObject())
            { if (!names.Add(item.Name)) throw Invalid(); NoDuplicates(item.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) NoDuplicates(item);
    }
    private static void ExactKeys(JsonElement value, string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid();
        var keys = new HashSet<string>(expected, StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject()) if (!keys.Remove(property.Name)) throw Invalid();
        if (keys.Count != 0) throw Invalid();
    }
    private static int Integer(JsonElement value)
    { if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result)) throw Invalid(); return result; }
    private static string Text(JsonElement value)
    { if (value.ValueKind != JsonValueKind.String) throw Invalid(); string result = value.GetString()!; PathText(result); return result; }
    private static void PathText(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 4096 || value.Contains('\0')) throw Invalid();
        // Reject schemes, resource URIs, UNC/network/device paths and drive-relative forms before existence I/O.
        if (value.StartsWith("//", StringComparison.Ordinal) || value.StartsWith(@"\\", StringComparison.Ordinal)) throw Invalid();
        int colon = value.IndexOf(':');
        if (colon >= 0 && !(colon == 1 && char.IsAsciiLetter(value[0]) && value.Length > 2
            && (value[2] == '/' || value[2] == '\\') && value.LastIndexOf(':') == colon)) throw Invalid();
    }
    private static string Resolve(string value, string directory, bool isDirectory)
    {
        string result = Path.GetFullPath(value, directory);
        if (result.Length > 4096) throw Invalid();
        if (!(isDirectory ? Directory.Exists(result) : File.Exists(result))) throw Invalid();
        return result;
    }
    private static InvalidDataException Invalid() => new("Invalid offline scenario manifest.");
}
