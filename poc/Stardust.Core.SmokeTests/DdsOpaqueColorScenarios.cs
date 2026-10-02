using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class DdsOpaqueColorScenarios
{
    internal const string SamplePath = "texture/nebula_dant_back.dds";
    internal const string SampleHash = "A2627501B233EE3A795BEB5B42446BF2F9B2A60854AD3781366720B5631FDC9D";
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Emit(string kind, object value) => Console.WriteLine(kind + "=" + JsonSerializer.Serialize(value));
    private static void Put(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    internal static byte[] Fixture(bool modern, int bpp, uint dxgi = 88, uint alpha = 0, int mode = 1)
    {
        const int width = 3, height = 2, mips = 2;
        byte[] bytes = new byte[(modern ? 148 : 128) + 7 * bpp];
        "DDS "u8.CopyTo(bytes); Put(bytes, 4, 124); Put(bytes, 8, 0x21007u | (mode == 1 ? 8u : mode == 2 ? 0x80000u : 0));
        Put(bytes, 12, height); Put(bytes, 16, width); Put(bytes, 20, (uint)(mode == 2 ? width * height * bpp : width * bpp));
        Put(bytes, 28, mips); Put(bytes, 76, 32); Put(bytes, 108, 0x401008);
        if (modern) { Put(bytes, 80, 4); "DX10"u8.CopyTo(bytes.AsSpan(84)); Put(bytes, 128, dxgi); Put(bytes, 132, 3); Put(bytes, 140, 1); Put(bytes, 144, alpha); }
        else { Put(bytes, 80, 0x40); Put(bytes, 88, (uint)(bpp * 8)); Put(bytes, 92, 0xFF0000); Put(bytes, 96, 0xFF00); Put(bytes, 100, 0xFF); }
        int start = modern ? 148 : 128;
        for (int pixel = 0; pixel < 7; pixel++)
        { bytes[start + pixel * bpp] = (byte)(11 + pixel * 7); bytes[start + pixel * bpp + 1] = (byte)(31 + pixel * 9); bytes[start + pixel * bpp + 2] = (byte)(201 - pixel * 13); if (bpp == 4) bytes[start + pixel * bpp + 3] = (byte)(pixel * 41); }
        return bytes;
    }
    private static void Check(DdsImage image, bool modern, int bpp, uint dxgi, uint alpha)
    {
        if (image.Format != (bpp == 3 ? DdsFormat.Bgr24 : DdsFormat.Bgrx32) || image.DxgiFormat != (modern ? dxgi : null) ||
            image.ColorSpace != (modern ? dxgi == 93 ? DdsColorSpace.Srgb : DdsColorSpace.Linear : DdsColorSpace.Unspecified) ||
            image.AlphaMode != (modern ? (DdsAlphaMode)alpha : DdsAlphaMode.Unknown) || image.RetainedTerminalBlock.Length != 0 || image.Mips.Count != 2)
            throw new Exception("Opaque DDS metadata mismatch.");
        int pixel = 0;
        for (int level = 0; level < 2; level++)
        {
            var mip = image.Mips[level]; if (mip.Width != (level == 0 ? 3 : 1) || mip.Height != (level == 0 ? 2 : 1)) throw new Exception("Opaque mip extent mismatch.");
            for (int offset = 0; offset < mip.Rgba.Length; offset += 4, pixel++)
                if (!mip.Rgba.Span.Slice(offset, 4).SequenceEqual(new byte[] { (byte)(201 - pixel * 13), (byte)(31 + pixel * 9), (byte)(11 + pixel * 7), 255 })) throw new Exception("Opaque channel/X/row/mip mismatch.");
        }
    }
    internal static void Run()
    {
        if ((int)DdsFormat.Bc7 != 9 || (int)DdsFormat.Bgr24 != 10 || (int)DdsFormat.Bgrx32 != 11) throw new Exception("DDS enum identity changed.");
        int rejects = 0, prefixes = 0, cases = 0;
        void Reject(byte[] bytes, DdsReadLimits? limits = null, bool emit = true)
        {
            try { DdsDecoder.Decode(bytes, limits); } catch (InvalidDataException) { rejects++; if (emit) Emit("opaque_bad", new { hex = Convert.ToHexString(bytes), limits }); return; }
            throw new Exception("Malformed opaque DDS accepted.");
        }
        foreach (bool modern in new[] { false, true })
        foreach (int bpp in (modern ? new[] { 4 } : new[] { 3, 4 }))
        foreach (uint dxgi in (modern ? new uint[] { 88, 93 } : new uint[] { 0 }))
        foreach (uint alpha in (modern ? new uint[] { 0, 1, 2, 3, 4 } : new uint[] { 0 }))
        foreach (int mode in new[] { 0, 1, 2 })
        {
            byte[] bytes = Fixture(modern, bpp, dxgi, alpha, mode); DdsImage image = DdsDecoder.Decode(bytes); Check(image, modern, bpp, dxgi, alpha);
            Emit("opaque_fixture", new { hex = Convert.ToHexString(bytes), format = image.Format.ToString(), dxgi = image.DxgiFormat, colorSpace = image.ColorSpace.ToString(), alphaMode = image.AlphaMode.ToString(), mips = image.Mips.Select(m => new { m.Width, m.Height, sha256 = Hash(m.Rgba.Span) }).ToArray() }); cases++;
            Array.Clear(bytes); Check(image, modern, bpp, dxgi, alpha);
            if (image.Mips is IList<DdsMip> list) { try { list.Clear(); throw new Exception("Writable DDS mip list."); } catch (NotSupportedException) { } }
        }
        foreach (byte[] valid in new[] { Fixture(false, 3), Fixture(false, 4), Fixture(true, 4, 88), Fixture(true, 4, 93) })
        {
            for (int length = 0; length < valid.Length; length++) { Reject(valid[..length], emit: false); prefixes++; }
            Reject(valid.Concat(new byte[] { 0 }).ToArray());
            Reject(valid.Concat(new byte[16]).ToArray(), new(AllowLegacyTerminalBlockTail: true));
            foreach ((int offset, uint value) in new[] { (4, 123u), (8, 0u), (8, 0xA100Fu), (8, 0x102100Fu), (20, 0u), (20, 16u), (12, 0u), (16, uint.MaxValue), (24, 2u), (28, 3u), (76, 31u), (80, 0x42u), (108, 0u), (112, 0x200u) })
            { byte[] bad = (byte[])valid.Clone(); Put(bad, offset, value); Reject(bad); }
            Reject(valid, new(MaximumDimension: 2)); Reject(valid, new(MaximumInputBytes: 128)); Reject(valid, new(MaximumDecodedBytes: 27));
            byte[] wrongLinear = (byte[])valid.Clone(); Put(wrongLinear, 8, 0xA1007); Put(wrongLinear, 20, 1); Reject(wrongLinear);
            if (valid.Length < 170)
                foreach ((int offset, uint value) in new[] { (84, 1u), (88, 16u), (92, 0xFFu), (96, 0u), (100, 0xFF0000u), (104, 0xFF000000u), (80, 0x41u), (80, 0x240u) })
                { byte[] bad = (byte[])valid.Clone(); Put(bad, offset, value); Reject(bad); }
            else
                foreach ((int offset, uint value) in new[] { (128, 89u), (128, 92u), (132, 4u), (136, 4u), (140, 0u), (140, 2u), (144, 5u), (144, 0x100u), (88, 32u), (92, 0xFF0000u), (96, 0xFF00u), (100, 0xFFu), (104, 0xFF000000u), (80, 5u) })
                { byte[] bad = (byte[])valid.Clone(); Put(bad, offset, value); Reject(bad); }
        }
        Emit("opaque_validation", new { cases, rejects, prefixes, owned = true, readonlyMips = true, enumStable = true });
        Console.WriteLine("PASS R12-ASSETS-01: tightly packed opaque BGR24/BGRX32 pixels, metadata, mips, ownership and negative gates.");
    }
    internal static void Probe(string root, string configuration)
    {
        if (Hash(File.ReadAllBytes(configuration)) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration identity differs.");
        DdsImage image; byte[] source; string[] hashes;
        using (var config = File.OpenText(configuration))
        using (var overlay = ConfiguredTreOverlay.Open(root, config))
        {
            var result = overlay.Lookup(SamplePath); source = result.Payload ?? throw new InvalidDataException("Original DDS absent.");
            if (result.Kind != TreLookupKind.Present || result.Priority != 35 || source.Length != 16511 || Hash(source) != SampleHash) throw new InvalidDataException("Original DDS priority/identity differs.");
            image = DdsDecoder.Decode(source);
            if (image.Format != DdsFormat.Bgr24 || image.Width != 64 || image.Height != 64 || image.Mips.Count != 7) throw new Exception("Original DDS classification differs.");
            Emit("opaque_original", new { path = SamplePath, priority = result.Priority, bytes = source.Length, sha256 = Hash(source), configurationSha256 = Hash(File.ReadAllBytes(configuration)), format = image.Format.ToString(), mips = image.Mips.Select(m => new { m.Width, m.Height, sha256 = Hash(m.Rgba.Span) }).ToArray() });
            hashes = image.Mips.Select(m => Hash(m.Rgba.Span)).ToArray();
            Array.Clear(source);
        }
        if (!hashes.SequenceEqual(image.Mips.Select(m => Hash(m.Rgba.Span)))) throw new Exception("Original mip pixels borrowed cleared source.");
        if (image.Mips.Any(m => m.Rgba.Span.ToArray().Where((_, i) => i % 4 == 3).Any(a => a != 255))) throw new Exception("Original opaque alpha differs.");
        Emit("opaque_cleanup", new { sourceMutationSafe = true, archiveDisposed = true, mips = image.Mips.Count });
    }
}
