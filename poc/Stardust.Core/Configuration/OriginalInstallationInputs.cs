using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Stardust.Core.Assets;

namespace Stardust.Core.Configuration;

public sealed record OriginalInstallationResource(string LogicalPath, TreLookupKind Kind,
    int? Priority, int? ByteCount, string? Sha256);

public sealed record OriginalInstallationReceipt(string ConfigurationSha256, int ConfigurationBytes,
    int MountCount, IReadOnlyList<OriginalInstallationResource> Resources)
{
    public bool AllResourcesPresent => Resources.All(resource => resource.Kind == TreLookupKind.Present);
}

/// <summary>Read-only explicit installation preflight. Owns the overlay; does not expand config includes.</summary>
public sealed class OriginalInstallationInputs : IDisposable
{
    public const int MaximumConfigurationBytes = 65_536;
    public const int MaximumResources = 128;
    private readonly ConfiguredTreOverlay overlay;
    private bool disposed;

    private OriginalInstallationInputs(ConfiguredTreOverlay overlay, OriginalInstallationReceipt receipt)
    {
        this.overlay = overlay;
        Receipt = receipt;
    }

    /// <summary>Immutable historical metadata, valid after disposal; contains no asset or config contents.</summary>
    public OriginalInstallationReceipt Receipt { get; }

    /// <summary>Borrowed service. Do not dispose it or retain it beyond this owner's lifetime.</summary>
    public ConfiguredTreOverlay BorrowedOverlay
    {
        get { ObjectDisposedException.ThrowIf(disposed, this); return overlay; }
    }

    public static OriginalInstallationInputs Open(string assetRoot, string configurationPath,
        IEnumerable<string> logicalResources)
    {
        string root = LocalPath(assetRoot);
        string config = LocalPath(configurationPath);
        ArgumentNullException.ThrowIfNull(logicalResources);
        List<string> paths = new();
        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (string path in logicalResources)
        {
            if (paths.Count >= MaximumResources) throw new InvalidDataException("Too many explicit resources.");
            if (string.IsNullOrWhiteSpace(path) || path.Length > 1024 || path != path.Trim() ||
                path.Any(c => c < 32 || c > 126) || path.Contains(':') || path.StartsWith('/') || path.StartsWith('\\'))
                throw new InvalidDataException("Resource must be a bounded relative logical path.");
            string[] parts = path.Replace('\\', '/').Split('/');
            if (parts.Any(part => part.Length == 0 || part is "." or ".."))
                throw new InvalidDataException("Resource path has an unsupported segment.");
            string normalized = LegacyAssetPath.Normalize(path);
            if (!unique.Add(normalized)) throw new InvalidDataException("Duplicate normalized resource.");
            paths.Add(normalized);
        }
        if (paths.Count == 0) throw new InvalidDataException("At least one explicit resource is required.");
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Asset root does not exist.");
        RejectLinks(root);
        RejectLinks(config);
        byte[] bytes;
        using (FileStream input = new(config, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (input.Length > MaximumConfigurationBytes) throw new InvalidDataException("Configuration exceeds byte limit.");
            bytes = new byte[checked((int)input.Length)];
            input.ReadExactly(bytes);
        }
        string text = new UTF8Encoding(false, true).GetString(bytes);
        if (text.StartsWith('\uFEFF')) text = text[1..];
        // Security admission only: the existing overlay remains authoritative for mount grammar and priority.
        foreach (string line in text.Split('\n'))
        {
            if (!line.TrimStart().StartsWith("searchTree_00_", StringComparison.Ordinal)) continue;
            Match match = Regex.Match(line.TrimEnd('\r'), @"^\s*searchTree_00_([0-9]+)\s*=\s*([^\s;]+)\s*(?:;.*)?$",
                RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            if (!match.Success) throw new InvalidDataException("Invalid TRE mount declaration.");
            string name = match.Groups[2].Value;
            if (name.Contains('/') || name.Contains('\\') || name.Contains(':') || name.Any(char.IsControl))
                throw new InvalidDataException("Mount must be a local archive filename.");
            RejectLinks(Path.Combine(root, name));
        }
        ConfiguredTreOverlay mounted = ConfiguredTreOverlay.Open(root, new StringReader(text));
        try
        {
            List<OriginalInstallationResource> resources = new();
            foreach (string path in paths)
            {
                TreLookupResult result = mounted.Lookup(path);
                resources.Add(new(path, result.Kind, result.Priority, result.Payload?.Length,
                    result.Payload is null ? null : Convert.ToHexString(SHA256.HashData(result.Payload))));
            }
            return new(mounted, new(Convert.ToHexString(SHA256.HashData(bytes)), bytes.Length,
                mounted.MountCount, resources.AsReadOnly()));
        }
        catch { mounted.Dispose(); throw; }
    }

    public void RequireAllPresent()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!Receipt.AllResourcesPresent) throw new InvalidDataException("An explicit resource is missing or masked.");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        overlay.Dispose();
    }

    private static string LocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 4096 || path != path.Trim() || path.Any(char.IsControl) ||
            path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal) ||
            !Path.IsPathFullyQualified(path) || path.Contains("://", StringComparison.Ordinal))
            throw new InvalidDataException("A fully qualified local filesystem path is required.");
        string remainder = path.Length >= 2 && path[1] == ':' ? path[2..] : path;
        if (remainder.Contains(':') || remainder.Replace('\\', '/').Split('/').Any(part => part is "." or ".."))
            throw new InvalidDataException("Filesystem path has an unsupported segment.");
        return Path.GetFullPath(path);
    }

    private static void RejectLinks(string path)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Reparse paths are unsupported for local installation inputs.");
        }
    }
}
