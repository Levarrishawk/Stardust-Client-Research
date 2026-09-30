using System.Buffers.Binary;
using System.Text;
using Stardust.Core.Compression;

namespace Stardust.Core.Assets;

public sealed record TreReadLimits(int MaximumRecords = 1_000_000,
    int MaximumIndexBytes = 64 * 1024 * 1024, int MaximumPayloadBytes = 256 * 1024 * 1024);

public sealed record TreEntry(string Path, uint Checksum, uint UncompressedSize,
    uint Offset, uint CompressionKind, uint CompressedSize)
{
    public bool IsTombstone => UncompressedSize == 0;
}

/// <summary>Read-only TREE 0005 index and lazy payload access over a seekable stream.</summary>
public sealed class TreArchive : IDisposable
{
    private readonly Stream source;
    private readonly bool leaveOpen;
    private readonly TreReadLimits limits;
    private readonly Dictionary<string, TreEntry> entries = new(StringComparer.Ordinal);
    private readonly object gate = new();
    private bool disposed;

    public TreArchive(Stream source, bool leaveOpen = false, TreReadLimits? limits = null)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.leaveOpen = leaveOpen;
        this.limits = limits ?? new();
        if (!source.CanRead || !source.CanSeek) throw new ArgumentException("TRE input must be readable and seekable.", nameof(source));
        if (this.limits.MaximumRecords <= 0 || this.limits.MaximumIndexBytes <= 0 || this.limits.MaximumPayloadBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
        try { ReadIndex(); }
        catch { if (!leaveOpen) source.Dispose(); throw; }
    }

    public int EntryCount => entries.Count;
    public long ArchiveBytes => source.Length;
    public IReadOnlyCollection<TreEntry> Entries => entries.Values;

    public TreEntry? FindEntry(string path)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return entries.GetValueOrDefault(LegacyAssetPath.Normalize(path));
        }
    }

    public byte[] Read(string path)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            TreEntry entry = FindEntry(path) ?? throw new FileNotFoundException("Logical asset is absent from the TRE archive.");
            if (entry.IsTombstone) throw new InvalidOperationException("Logical asset is masked by a TRE tombstone.");
            return ReadBlock(entry.Offset, entry.CompressionKind, entry.CompressedSize,
                entry.UncompressedSize, limits.MaximumPayloadBytes);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            if (!leaveOpen) source.Dispose();
        }
    }

    private void ReadIndex()
    {
        byte[] header = ReadAt(0, 36);
        if (!header.AsSpan(0, 8).SequenceEqual("EERT5000"u8))
            throw new InvalidDataException("Only TREE 0005 archives are supported.");
        uint count = U32(header, 8), offset = U32(header, 12);
        uint recordKind = U32(header, 16), recordSize = U32(header, 20);
        uint nameKind = U32(header, 24), nameSize = U32(header, 28), nameLength = U32(header, 32);
        if (count > limits.MaximumRecords || (ulong)count * 24 > (ulong)limits.MaximumIndexBytes || offset < 36)
            throw new InvalidDataException("TRE record index exceeds local bounds.");
        uint recordLength = checked(count * 24);
        byte[] records = ReadBlock(offset, recordKind, recordSize, recordLength, limits.MaximumIndexBytes);
        long nameOffset = offset + (long)(recordKind == 0 ? recordLength : recordSize);
        byte[] names = ReadBlock(nameOffset, nameKind, nameSize, nameLength, limits.MaximumIndexBytes);
        long digestOffset = nameOffset + (nameKind == 0 ? nameLength : nameSize);
        RequireRange(digestOffset, (long)count * 16); // Digest semantics are not claimed or verified here.
        for (int index = 0; index < count; index++)
        {
            ReadOnlySpan<byte> row = records.AsSpan(index * 24, 24);
            uint checksum = U32(row, 0), size = U32(row, 4), payloadOffset = U32(row, 8);
            uint kind = U32(row, 12), compressed = U32(row, 16), nameStart = U32(row, 20);
            if (nameStart >= names.Length) throw new InvalidDataException("TRE name offset is outside its block.");
            int end = Array.IndexOf(names, (byte)0, (int)nameStart);
            if (end < 0 || end - nameStart > 1024) throw new InvalidDataException("TRE name lacks a bounded terminator.");
            ReadOnlySpan<byte> nameBytes = names.AsSpan((int)nameStart, end - (int)nameStart);
            if (nameBytes.ContainsAnyInRange((byte)0x80, byte.MaxValue))
                throw new InvalidDataException("Non-ASCII TRE names are unsupported.");
            string path;
            try { path = Encoding.ASCII.GetString(nameBytes); _ = LegacyAssetPath.Normalize(path); }
            catch (ArgumentException) { throw new InvalidDataException("Invalid TRE logical name."); }
            // The index stores CRC(raw name). Requests are normalized before lookup;
            // retaining the raw key prevents promoting noncanonical names into reachable aliases.
            if (LegacyAssetPath.ChecksumBytes(nameBytes) != checksum)
                throw new InvalidDataException("TRE filename checksum mismatch.");
            if (kind is not (0 or 2)) throw new InvalidDataException("Unsupported TRE payload compression.");
            if (size > limits.MaximumPayloadBytes || (kind == 2 && compressed > limits.MaximumPayloadBytes))
                throw new InvalidDataException("TRE payload exceeds local bounds.");
            if (size != 0) RequireRange(payloadOffset, kind == 0 ? size : compressed);
            if (!entries.TryAdd(path, new(path, checksum, size, payloadOffset, kind, compressed)))
                throw new InvalidDataException("Duplicate raw path in TRE archive.");
        }
    }

    private byte[] ReadBlock(long offset, uint kind, uint compressedSize, uint expectedSize, int maximum)
    {
        if (kind is not (0 or 2)) throw new InvalidDataException("Unsupported TRE block compression.");
        uint storedSize = kind == 0 ? expectedSize : compressedSize;
        if (expectedSize > maximum || storedSize > maximum)
            throw new InvalidDataException("TRE block exceeds its configured bound.");
        byte[] stored = ReadAt(offset, (int)storedSize);
        if (kind == 0) return stored;
        return BoundedZlib.Decode(stored, maximum, (int)expectedSize);
    }

    private byte[] ReadAt(long offset, int length)
    {
        RequireRange(offset, length);
        byte[] bytes = new byte[length];
        source.Position = offset;
        try { source.ReadExactly(bytes); }
        catch (EndOfStreamException) { throw new InvalidDataException("TRE input was truncated during reading."); }
        return bytes;
    }

    private void RequireRange(long offset, long length)
    {
        if (offset < 0 || length < 0 || offset > source.Length || length > source.Length - offset)
            throw new InvalidDataException("TRE block lies outside the archive.");
    }

    private static uint U32(ReadOnlySpan<byte> bytes, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
}
