using System.Globalization;
using System.Text;
using Stardust.Core.Protocol;

namespace Stardust.Core.Assets;

public enum LocalizedObjectNameOrigin { Custom, PrimaryTable, FallbackTable, Empty, InvalidStringId, MissingTable, MissingKey }
public sealed record LocalizedObjectNameResult(string DisplayText, LocalizedObjectNameOrigin Origin,
    bool Truncated, string? TablePath = null, uint? MatchedId = null);

/// <summary>Stateless owned-table lookup with explicit modern display and path policy.</summary>
public static class LocalizedObjectNameResolver
{
    private sealed class Frame { public bool Violated; }
    [ThreadStatic] private static Frame? active;
    public static LocalizedObjectNameResult Resolve(CreatureView3NameState names,
        Func<string, LocalizedStringTable?> parsedTableResolver, string primaryDirectory = "string/en",
        string? fallbackDirectory = null, int maximumDisplayUnits = 256)
    {
        // Check first so even an invalid nested call poisons a swallowed recursive callback.
        if (active is not null) { active.Violated = true; throw new InvalidOperationException("Recursive object-name resolution."); }
        ArgumentNullException.ThrowIfNull(names); ArgumentNullException.ThrowIfNull(parsedTableResolver);
        if (maximumDisplayUnits is < 0 or > 4096) throw new ArgumentOutOfRangeException(nameof(maximumDisplayUnits));
        if (!DirectoryValid(primaryDirectory) || (fallbackDirectory is not null && !DirectoryValid(fallbackDirectory)))
            throw new ArgumentException("Bounded relative ASCII name directories required.");
        var frame = new Frame(); active = frame;
        try
        {
            if (names.CustomNameUnits.Count != 0) return Display(names.CustomNameUnits, LocalizedObjectNameOrigin.Custom, maximumDisplayUnits);
            if (names.NameFile.Length == 0 && names.NameKey.Length == 0) return Status(LocalizedObjectNameOrigin.Empty, "", maximumDisplayUnits);
            if (names.NameFile.Length is < 1 or > 128 || names.NameKey.Length > 65536)
                return Status(LocalizedObjectNameOrigin.InvalidStringId, "(invalid name)", maximumDisplayUnits);
            // Borrow internal immutable spans, never clone a large baseline name before checking its bounds.
            ReadOnlySpan<byte> file = names.NameFile.Bytes;
            foreach (byte b in file) if (!ComponentByte(b)) return Status(LocalizedObjectNameOrigin.InvalidStringId, "(invalid name)", maximumDisplayUnits);
            string identifier = Encoding.ASCII.GetString(file);
            string path = primaryDirectory + "/" + identifier + ".stf";
            var table = parsedTableResolver(path); Guard(frame);
            var origin = LocalizedObjectNameOrigin.PrimaryTable;
            if (table is null && fallbackDirectory is not null && fallbackDirectory != primaryDirectory)
            {
                path = fallbackDirectory + "/" + identifier + ".stf";
                table = parsedTableResolver(path); Guard(frame); origin = LocalizedObjectNameOrigin.FallbackTable;
            }
            if (table is null) return Status(LocalizedObjectNameOrigin.MissingTable, "(missing table)", maximumDisplayUnits, path);
            if (!table.TryGetByKey(names.NameKey.Bytes, out var record))
                return Status(LocalizedObjectNameOrigin.MissingKey, "(missing key)", maximumDisplayUnits, path);
            return Display(record!.Utf16Units, origin, maximumDisplayUnits, path, record.Id);
        }
        finally { active = null; }
    }
    private static void Guard(Frame frame) { if (frame.Violated) throw new InvalidOperationException("Recursive object-name callback rejected."); }
    private static bool ComponentByte(byte b) => b is >= (byte)'a' and <= (byte)'z' or >= (byte)'A' and <= (byte)'Z' or >= (byte)'0' and <= (byte)'9' or (byte)'_' or (byte)'-';
    private static bool DirectoryValid(string? directory)
    {
        if (directory is null || directory.Length is < 1 or > 256) return false;
        bool component = false;
        foreach (char c in directory)
        {
            if (c == '/') { if (!component) return false; component = false; }
            else { if (c > 127 || !ComponentByte((byte)c)) return false; component = true; }
        }
        return component;
    }
    private static LocalizedObjectNameResult Status(LocalizedObjectNameOrigin origin, string text, int maximum, string? path = null)
        => new(text[..Math.Min(text.Length, maximum)], origin, text.Length > maximum, path);
    private static LocalizedObjectNameResult Display(IReadOnlyList<ushort> units, LocalizedObjectNameOrigin origin,
        int maximum, string? path = null, uint? id = null)
    {
        var text = new StringBuilder(Math.Min(maximum, units.Count)); int i = 0;
        while (i < units.Count && text.Length < maximum)
        {
            char c = (char)units[i]; string token; int consumed = 1;
            if (char.IsHighSurrogate(c) && i + 1 < units.Count && char.IsLowSurrogate((char)units[i + 1]))
            { token = new string(new[] { c, (char)units[i + 1] }); consumed = 2; }
            else if (char.IsSurrogate(c)) token = "\uFFFD";
            else if (c == '\\') token = "\\\\";
            else if (char.IsControl(c)) token = "\\u" + units[i].ToString("X4", CultureInfo.InvariantCulture);
            else token = c.ToString();
            if (token.Length > maximum - text.Length) break;
            text.Append(token); i += consumed;
        }
        return new(text.ToString(), origin, i < units.Count, path, id);
    }
}
