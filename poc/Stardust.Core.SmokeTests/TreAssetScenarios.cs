using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Compression;

internal static class TreAssetScenarios
{
    internal sealed record FixtureEntry(string Path, byte[] Bytes, uint Kind = 0);

    public static void Run()
    {
        // Python zlib.compress(b"abc") and zlib.compress(b"") fixed streams.
        byte[] abc = Convert.FromHexString("789C4B4C4A0600024D0127");
        Check(BoundedZlib.Decode(abc, 3).AsSpan().SequenceEqual("abc"u8), "independent zlib stream");
        Check(BoundedZlib.Decode(Convert.FromHexString("789C030000000001"), 0).Length == 0,
            "valid empty zlib stream");
        for (int length = 0; length < abc.Length; length++)
        {
            byte[] prefix = abc[..length];
            Reject(() => BoundedZlib.Decode(prefix, 3));
        }
        Reject(() => BoundedZlib.Decode(abc, 2));
        Check(LegacyAssetPath.Checksum("texture/frst_conf_redwood_bark_d_n.dds") == 0x00B7D0CF,
            "documented filename CRC");
        Check(LegacyAssetPath.Checksum("appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh") == 0x003A7342,
            "documented mesh CRC");
        Check(LegacyAssetPath.Normalize("\\.././TEXTURE\\\\Name.DDS ") == "texture/name.dds ",
            "ASCII fold, leading segments, separators, trailing-space preservation");
        FixtureEntry raw = new("test/raw.bin", new byte[] { 1, 2, 3 });
        using (TreArchive noncanonical = new(new MemoryStream(Build(false,
            new("test/Copy.bin", new byte[] { 7 }), new("test/copy.bin", new byte[] { 9 })))))
        {
            Check(noncanonical.EntryCount == 2 && noncanonical.Entries.Any(entry => entry.Path == "test/Copy.bin"), "raw index names retained");
            Check(noncanonical.Read("test/Copy.bin").Single() == 9, "requests normalize but raw records do not become aliases");
        }
        using (TreArchive noncanonicalOnly = new(new MemoryStream(Build(false, new FixtureEntry("test/Copy.bin", new byte[] { 7 })))))
            Check(noncanonicalOnly.FindEntry("test/Copy.bin") is null, "noncanonical raw-only record is not lookup reachable");
        FixtureEntry compressed = new("test/z.bin", Enumerable.Repeat((byte)0x5A, 1024).ToArray(), 2);
        byte[] fixture = Build(false, raw, compressed, new("test/mask.bin", Array.Empty<byte>()));
        using (TreArchive archive = new(new MemoryStream(fixture)))
        {
            Check(archive.EntryCount == 3, "synthetic entry count");
            Check(archive.FindEntry("test/raw.bin")!.CompressedSize == 0, "stored zero-csize record retained");
            Check(archive.Read("TEST\\RAW.BIN").AsSpan().SequenceEqual(raw.Bytes), "stored payload uses usize");
            Check(archive.Read("test/z.bin").AsSpan().SequenceEqual(compressed.Bytes), "zlib payload");
            Check(archive.FindEntry("test/mask.bin")!.IsTombstone, "usize-zero tombstone");
        }
        using (TreArchive archive = new(new MemoryStream(Build(true, raw, compressed))))
            Check(archive.Read("test/z.bin").AsSpan().SequenceEqual(compressed.Bytes), "compressed index and name blocks");
        using TreArchive low = new(new MemoryStream(Build(false, raw, new("test/mask.bin", new byte[] { 7 }))));
        using TreArchive high = new(new MemoryStream(Build(false,
            new("test/raw.bin", new byte[] { 9 }), new("test/mask.bin", Array.Empty<byte>()))));
        TreOverlay overlay = new();
        overlay.Mount(1, low);
        overlay.Mount(10, high);
        Check(overlay.Lookup("test/raw.bin").Payload!.Single() == 9, "priority overlay");
        Check(overlay.Lookup("test/mask.bin").Kind == TreLookupKind.Masked, "tombstone prevents fallback");
        Check(overlay.Lookup("test/absent.bin").Kind == TreLookupKind.Missing, "missing differs from mask");
        for (int length = 0; length < fixture.Length; length++)
        {
            byte[] truncated = fixture[..length];
            Reject(() => { using TreArchive _ = new(new MemoryStream(truncated)); });
        }
        byte[] badVersion = (byte[])fixture.Clone(); badVersion[4] = (byte)'6';
        Reject(() => { using TreArchive _ = new(new MemoryStream(badVersion)); });
        byte[] badCount = (byte[])fixture.Clone(); Write(badCount, 8, uint.MaxValue);
        Reject(() => { using TreArchive _ = new(new MemoryStream(badCount)); });
        uint indexOffset = BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(12));
        foreach ((int field, uint value) in new[] { (0, 1u), (8, uint.MaxValue), (12, 1u), (20, uint.MaxValue) })
        {
            byte[] invalid = (byte[])fixture.Clone(); Write(invalid, (int)indexOffset + field, value);
            Reject(() => { using TreArchive _ = new(new MemoryStream(invalid)); });
        }
        Reject(() => { using TreArchive _ = new(new MemoryStream(Build(false, raw, raw))); });
        Reject(() => { using TreArchive _ = new(new MemoryStream(fixture), limits: new(MaximumRecords: 1)); });
        byte[] shortExpansion = (byte[])fixture.Clone(); Write(shortExpansion, (int)indexOffset + 24 + 4, 1025);
        using (TreArchive archive = new(new MemoryStream(shortExpansion))) Reject(() => archive.Read("test/z.bin"));
        byte[] longExpansion = (byte[])fixture.Clone(); Write(longExpansion, (int)indexOffset + 24 + 4, 1023);
        using (TreArchive archive = new(new MemoryStream(longExpansion))) Reject(() => archive.Read("test/z.bin"));
        byte[] missingAdler = (byte[])fixture.Clone();
        int compressedSizeOffset = (int)indexOffset + 24 + 16;
        uint advertisedSize = BinaryPrimitives.ReadUInt32LittleEndian(missingAdler.AsSpan(compressedSizeOffset));
        Write(missingAdler, compressedSizeOffset, advertisedSize - 4);
        using (TreArchive archive = new(new MemoryStream(missingAdler))) Reject(() => archive.Read("test/z.bin"));
        Console.WriteLine("PASS: TRE 0005 stored/zlib indices and payloads, path CRC, overlay priority, tombstones, and malformed bounds");
    }

    public static void ProbeOptional(string[] args)
    {
        if (args.Length == 0) return;
        if (args.Length != 6 || args[0] != "--tre" || args[2] != "--member" || args[4] != "--expected-sha256")
            throw new ArgumentException("Optional TRE probe requires --tre PATH --member LOGICAL_PATH --expected-sha256 HASH.");
        using FileStream file = File.OpenRead(args[1]);
        using TreArchive archive = new(file);
        byte[] payload = archive.Read(args[3]);
        string sha = Convert.ToHexString(SHA256.HashData(payload));
        Check(sha.Equals(args[5], StringComparison.OrdinalIgnoreCase), "independent real-member digest");
        Console.WriteLine($"PASS: read-only original TRE probe entries={archive.EntryCount} archive_bytes={archive.ArchiveBytes} " +
            $"payload_bytes={payload.Length} sha256={sha}");
    }

    // Independently authored synthetic archive writer; no proprietary bytes.
    internal static byte[] Build(bool compressIndex, params FixtureEntry[] entries)
    {
        using MemoryStream archive = new();
        archive.Write(new byte[36]);
        using MemoryStream records = new();
        using MemoryStream names = new();
        foreach (FixtureEntry entry in entries)
        {
            uint offset = (uint)archive.Position;
            byte[] stored = entry.Kind == 2 ? Deflate(entry.Bytes) : entry.Bytes;
            archive.Write(stored);
            byte[] row = new byte[24];
            Write(row, 0, ReferenceCrc(entry.Path));
            Write(row, 4, (uint)entry.Bytes.Length);
            Write(row, 8, offset);
            Write(row, 12, entry.Kind);
            Write(row, 16, entry.Kind == 0 ? 0 : (uint)stored.Length);
            Write(row, 20, (uint)names.Length);
            records.Write(row);
            names.Write(Encoding.ASCII.GetBytes(entry.Path));
            names.WriteByte(0);
        }
        uint indexOffset = (uint)archive.Position;
        byte[] recordBlock = compressIndex ? Deflate(records.ToArray()) : records.ToArray();
        byte[] nameBlock = compressIndex ? Deflate(names.ToArray()) : names.ToArray();
        archive.Write(recordBlock);
        archive.Write(nameBlock);
        archive.Write(new byte[16 * entries.Length]);
        byte[] bytes = archive.ToArray();
        "EERT5000"u8.CopyTo(bytes);
        Write(bytes, 8, (uint)entries.Length);
        Write(bytes, 12, indexOffset);
        Write(bytes, 16, compressIndex ? 2u : 0u);
        Write(bytes, 20, compressIndex ? (uint)recordBlock.Length : 0);
        Write(bytes, 24, compressIndex ? 2u : 0u);
        Write(bytes, 28, compressIndex ? (uint)nameBlock.Length : 0);
        Write(bytes, 32, (uint)names.Length);
        return bytes;
    }

    private static uint ReferenceCrc(string name)
    {
        uint value = uint.MaxValue;
        foreach (byte octet in Encoding.ASCII.GetBytes(name))
            for (int bit = 7; bit >= 0; bit--)
            {
                bool feedback = ((value >> 31) ^ (uint)(octet >> bit)) % 2 != 0;
                value <<= 1;
                if (feedback) value ^= 0x04C11DB7;
            }
        return ~value;
    }

    private static byte[] Deflate(byte[] bytes)
    {
        using MemoryStream output = new();
        using (ZLibStream zlib = new(output, CompressionLevel.SmallestSize, leaveOpen: true)) zlib.Write(bytes);
        return output.ToArray();
    }

    private static void Write(byte[] bytes, int offset, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void Reject(Action action)
    {
        try { action(); throw new Exception("Malformed TRE accepted."); }
        catch (InvalidDataException) { }
    }
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception($"TRE scenario failed: {label}.");
    }
}
