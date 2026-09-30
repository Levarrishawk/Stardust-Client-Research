using System.Globalization;
using System.Text.RegularExpressions;

namespace Stardust.Core.Assets;

/// <summary>Owns explicitly configured TREE 0005 mounts; no loose files, TOC, or config include expansion.</summary>
public sealed class ConfiguredTreOverlay : IDisposable
{
    private readonly List<TreArchive> archives = new();
    private readonly TreOverlay overlay = new();
    private bool disposed;
    private ConfiguredTreOverlay() { }
    public int MountCount => archives.Count;

    public static ConfiguredTreOverlay Open(string archiveRoot, TextReader configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string root = Path.GetFullPath(archiveRoot);
        char[] buffer = new char[65_537];
        int length = configuration.ReadBlock(buffer, 0, buffer.Length);
        if (length == buffer.Length) throw new InvalidDataException("TRE mount configuration exceeds the character limit.");
        List<(int Priority, string Name)> mounts = new();
        HashSet<int> priorities = new();
        foreach (string line in new string(buffer, 0, length).Split('\n'))
        {
            if (!line.TrimStart().StartsWith("searchTree_00_", StringComparison.Ordinal)) continue;
            Match match = Regex.Match(line.TrimEnd('\r'), @"^\s*searchTree_00_([0-9]+)\s*=\s*([^\s;]+)\s*(?:;.*)?$",
                RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int priority))
                throw new InvalidDataException("Invalid TRE mount declaration.");
            string name = match.Groups[2].Value;
            if (name.Length > 255 || name.Contains('/') || name.Contains('\\') || name.Contains(':') ||
                !name.EndsWith(".tre", StringComparison.OrdinalIgnoreCase) || name.Any(char.IsControl) ||
                !priorities.Add(priority) || mounts.Count >= 256)
                throw new InvalidDataException("Unsupported, duplicate or excessive TRE mount declaration.");
            mounts.Add((priority, name));
        }
        if (mounts.Count == 0) throw new InvalidDataException("No TREE mounts were configured.");
        ConfiguredTreOverlay result = new();
        try
        {
            foreach ((int priority, string name) in mounts.OrderByDescending(mount => mount.Priority))
            {
                TreArchive archive;
                try { archive = new(File.OpenRead(Path.Combine(root, name))); }
                catch (InvalidDataException error) { throw new InvalidDataException($"Configured TRE archive rejected: {name}.", error); }
                result.archives.Add(archive);
                result.overlay.Mount(priority, archive);
            }
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    public TreLookupResult Lookup(string logicalPath)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return overlay.Lookup(logicalPath);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (TreArchive archive in archives) archive.Dispose();
    }
}
