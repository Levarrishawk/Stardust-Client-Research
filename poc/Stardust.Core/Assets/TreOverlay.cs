namespace Stardust.Core.Assets;

public enum TreLookupKind { Missing, Present, Masked }
public sealed record TreLookupResult(TreLookupKind Kind, byte[]? Payload = null, int? Priority = null);

/// <summary>Borrowed archive mounts. Higher numeric priority wins; tombstones stop fallback.</summary>
public sealed class TreOverlay
{
    private readonly SortedDictionary<int, TreArchive> mounts = new(Comparer<int>.Create((a, b) => b.CompareTo(a)));

    public void Mount(int priority, TreArchive archive)
    {
        ArgumentNullException.ThrowIfNull(archive);
        if (!mounts.TryAdd(priority, archive))
            throw new ArgumentException("Equal-priority TRE mounts require a separately specified tie policy.", nameof(priority));
    }

    public TreLookupResult Lookup(string path)
    {
        string normalized = LegacyAssetPath.Normalize(path);
        foreach ((int priority, TreArchive archive) in mounts)
        {
            TreEntry? entry = archive.FindEntry(normalized);
            if (entry is null) continue;
            return entry.IsTombstone ? new(TreLookupKind.Masked, Priority: priority) :
                new(TreLookupKind.Present, archive.Read(normalized), priority);
        }
        return new(TreLookupKind.Missing);
    }
}
