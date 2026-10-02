using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public enum DdsFormat { Bc1, Bc2, Bc3, Bc4, Bc5, Bgra32, Rgba32, R8, Rg8, Bc7, Bgr24, Bgrx32 }
public enum DdsColorSpace { Unspecified, Linear, Srgb }
public enum DdsAlphaMode { Unknown, Straight, Premultiplied, Opaque, Custom }
public sealed record DdsMip(int Width, int Height, ReadOnlyMemory<byte> Rgba);
public sealed record DdsImage(DdsFormat Format, IReadOnlyList<DdsMip> Mips,
    DdsColorSpace ColorSpace = DdsColorSpace.Unspecified, DdsAlphaMode AlphaMode = DdsAlphaMode.Unknown, uint? DxgiFormat = null,
    ReadOnlyMemory<byte> RetainedTerminalBlock = default)
{
    public int Width => Mips[0].Width;
    public int Height => Mips[0].Height;
}
public sealed record DdsReadLimits(int MaximumDimension = 8192, int MaximumInputBytes = 67_108_864,
    int MaximumDecodedBytes = 134_217_728, bool AllowLegacyTerminalBlockTail = false);

/// <summary>Owned RGBA8 mips, top-to-bottom rows. Bounded 2D legacy/DX10 DDS; no gamma, alpha or normal reconstruction.</summary>
public static class DdsDecoder
{
    public static DdsImage Decode(ReadOnlySpan<byte> source, DdsReadLimits? limits = null)
    {
        limits ??= new();
        if (limits.MaximumDimension is < 1 or > 32768 || limits.MaximumInputBytes < 128 || limits.MaximumDecodedBytes < 4)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Require(source.Length >= 128 && source.Length <= limits.MaximumInputBytes && source[..4].SequenceEqual("DDS "u8), "Invalid DDS size/magic.");
        Require(U32(source, 4) == 124 && U32(source, 76) == 32, "Invalid DDS header sizes.");
        uint flags = U32(source, 8), height = U32(source, 12), width = U32(source, 16);
        Require((flags & 0x1007) == 0x1007 && (U32(source, 108) & 0x1000) != 0, "Missing DDS texture/header fields.");
        Require(width > 0 && height > 0 && width <= limits.MaximumDimension && height <= limits.MaximumDimension, "DDS dimension limit.");
        Require(U32(source, 24) <= 1 && U32(source, 112) == 0, "DDS cube, volume or other extended surface is unsupported.");
        uint pixelFlags = U32(source, 80), fourCc = U32(source, 84);
        DdsFormat format;
        DdsColorSpace colorSpace = DdsColorSpace.Unspecified;
        DdsAlphaMode alphaMode = DdsAlphaMode.Unknown;
        uint? dxgiFormat = null;
        int payloadStart = 128;
        if ((pixelFlags & 4) != 0)
        {
            Require((pixelFlags & ~5u) == 0, "Unsupported DDS compressed pixel flags.");
            if (fourCc == 0x30315844)
            {
                Require(source.Length >= 148, "Truncated DDS DX10 extension.");
                uint dxgi = U32(source, 128), alpha = U32(source, 144);
                Require(U32(source, 132) == 3 && U32(source, 136) == 0 && U32(source, 140) == 1,
                    "Only one 2D DDS DX10 surface is supported; arrays/cubes/volumes are unsupported.");
                Require(alpha <= 4, "Invalid DDS DX10 alpha mode/reserved bits.");
                dxgiFormat = dxgi; alphaMode = (DdsAlphaMode)alpha; payloadStart = 148;
                colorSpace = dxgi is 29 or 72 or 75 or 78 or 91 or 93 or 99 ? DdsColorSpace.Srgb : DdsColorSpace.Linear;
                format = dxgi switch
                {
                    28 or 29 => DdsFormat.Rgba32,
                    49 => DdsFormat.Rg8,
                    61 => DdsFormat.R8,
                    71 or 72 => DdsFormat.Bc1,
                    74 or 75 => DdsFormat.Bc2,
                    77 or 78 => DdsFormat.Bc3,
                    80 => DdsFormat.Bc4,
                    83 => DdsFormat.Bc5,
                    87 or 91 => DdsFormat.Bgra32,
                    88 or 93 => DdsFormat.Bgrx32,
                    95 or 96 => throw new InvalidDataException("DDS BC6H requires DdsHdrDecoder; RGBA8 cannot preserve its values."),
                    98 or 99 => DdsFormat.Bc7,
                    81 or 84 => throw new InvalidDataException("DDS signed BC4/BC5 require a signed-channel output contract."),
                    _ => throw new InvalidDataException("Unsupported or typeless DDS DXGI format.")
                };
            }
            else format = fourCc switch
            {
                0x31545844 => DdsFormat.Bc1,
                0x33545844 => DdsFormat.Bc2,
                0x35545844 => DdsFormat.Bc3,
                0x31495441 or 0x55344342 => DdsFormat.Bc4,
                0x32495441 or 0x55354342 => DdsFormat.Bc5,
                _ => throw new InvalidDataException("Unsupported DDS FourCC.")
            };
        }
        else
        {
            Require(fourCc == 0 && U32(source, 92) == 0x00FF0000 && U32(source, 96) == 0x0000FF00 &&
                U32(source, 100) == 0x000000FF, "Unsupported legacy DDS color masks/FourCC.");
            uint bits = U32(source, 88), alphaMask = U32(source, 104);
            format = (pixelFlags, bits, alphaMask) switch
            {
                (0x41, 32, 0xFF000000) => DdsFormat.Bgra32,
                (0x40, 24, 0) => DdsFormat.Bgr24,
                (0x40, 32, 0) => DdsFormat.Bgrx32,
                _ => throw new InvalidDataException("Unsupported legacy DDS color flags/bits/alpha mask.")
            };
        }
        int bytesPerPixel = format switch { DdsFormat.Bgra32 or DdsFormat.Rgba32 or DdsFormat.Bgrx32 => 4,
            DdsFormat.Bgr24 => 3, DdsFormat.Rg8 => 2, DdsFormat.R8 => 1, _ => 0 };
        if (format == DdsFormat.Bgrx32 && payloadStart == 148)
            Require(pixelFlags == 4 && U32(source, 88) == 0 && U32(source, 92) == 0 && U32(source, 96) == 0 &&
                U32(source, 100) == 0 && U32(source, 104) == 0, "Inconsistent DX10 opaque pixel fields.");
        if (format is DdsFormat.Bgr24 or DdsFormat.Bgrx32)
        {
            Require((flags & ~(0x1007u | 8u | 0x20000u | 0x80000u)) == 0 && (flags & 0x80008) != 0x80008,
                "Unsupported or conflicting opaque DDS header flags.");
            // Original tightly packed BGR24 uses LINEARSIZE for the whole top mip, not PITCH.
            Require((flags & 0x80000) == 0 || U32(source, 20) == (long)width * height * bytesPerPixel,
                "Opaque DDS top-level linear size mismatch.");
        }
        Require(bytesPerPixel == 0 || (flags & 8) == 0 || U32(source, 20) == width * bytesPerPixel,
            "Padded uncompressed DDS pitch is unsupported.");
        uint mipCount = Math.Max(1u, U32(source, 28));
        int maximumMips = 1;
        for (uint dimension = Math.Max(width, height); dimension > 1; dimension >>= 1) maximumMips++;
        Require(mipCount <= maximumMips, "DDS mip count exceeds dimension-derived chain.");
        List<(int Width, int Height, int Offset, int Length)> ranges = new();
        long decodedBytes = 0;
        int cursor = payloadStart;
        for (int level = 0; level < mipCount; level++)
        {
            int w = (int)Math.Max(1u, width >> level), h = (int)Math.Max(1u, height >> level);
            long bytes = bytesPerPixel != 0 ? (long)w * h * bytesPerPixel :
                (long)((w + 3) / 4) * ((h + 3) / 4) * (format is DdsFormat.Bc1 or DdsFormat.Bc4 ? 8 : 16);
            decodedBytes += (long)w * h * 4;
            Require(decodedBytes <= limits.MaximumDecodedBytes && bytes <= source.Length - cursor, "DDS mip truncated or decode budget exceeded.");
            ranges.Add((w, h, cursor, (int)bytes)); cursor += (int)bytes;
        }
        bool terminalTail = cursor != source.Length && limits.AllowLegacyTerminalBlockTail && payloadStart == 128 &&
            format == DdsFormat.Bc2 && mipCount == maximumMips - 1 && (flags & 0x20000) != 0 &&
            ((width >> ((int)mipCount - 1) == 2 && height >> ((int)mipCount - 1) == 1) ||
             (height >> ((int)mipCount - 1) == 2 && width >> ((int)mipCount - 1) == 1)) && source.Length - cursor == 16;
        Require(cursor == source.Length || terminalTail, "DDS has unsupported trailing bytes.");
        List<DdsMip> mips = new();
        foreach (var range in ranges)
        {
            byte[] rgba = new byte[range.Width * range.Height * 4];
            ReadOnlySpan<byte> data = source.Slice(range.Offset, range.Length);
            if (bytesPerPixel != 0)
                for (int offset = 0; offset < rgba.Length; offset += 4)
                {
                    int input = offset / 4 * bytesPerPixel;
                    bool bgr = format is DdsFormat.Bgra32 or DdsFormat.Bgr24 or DdsFormat.Bgrx32;
                    rgba[offset] = data[input + (bgr ? 2 : 0)];
                    rgba[offset + 1] = bytesPerPixel >= 2 ? data[input + 1] : (byte)0;
                    rgba[offset + 2] = bytesPerPixel >= 3 ? data[input + (bgr ? 0 : 2)] : (byte)0;
                    rgba[offset + 3] = format is DdsFormat.Bgra32 or DdsFormat.Rgba32 ? data[input + 3] : (byte)255;
                }
            else if(format == DdsFormat.Bc7) Bc7Pixels.Decode(data,range.Width,range.Height,rgba);
            else DecodeBlocks(data, range.Width, range.Height, format, rgba);
            mips.Add(new(range.Width, range.Height, rgba));
        }
        return new(format, mips.AsReadOnly(), colorSpace, alphaMode, dxgiFormat,
            terminalTail ? source[cursor..].ToArray() : ReadOnlyMemory<byte>.Empty);
    }

    private static void DecodeBlocks(ReadOnlySpan<byte> source, int width, int height, DdsFormat format, Span<byte> output)
    {
        int blockBytes = format is DdsFormat.Bc1 or DdsFormat.Bc4 ? 8 : 16, cursor = 0;
        Span<byte> colors = stackalloc byte[16];
        Span<byte> alpha = stackalloc byte[8];
        Span<byte> red = stackalloc byte[16], green = stackalloc byte[16];
        for (int by = 0; by < (height + 3) / 4; by++)
            for (int bx = 0; bx < (width + 3) / 4; bx++)
            {
                ReadOnlySpan<byte> block = source.Slice(cursor, blockBytes); cursor += blockBytes;
                if (format is DdsFormat.Bc4 or DdsFormat.Bc5)
                {
                    DecodeChannel(block[..8], red);
                    if (format == DdsFormat.Bc5) DecodeChannel(block[8..], green);
                    for (int pixel = 0; pixel < 16; pixel++)
                    {
                        int x = bx * 4 + pixel % 4, y = by * 4 + pixel / 4;
                        if (x >= width || y >= height) continue;
                        int offset = (y * width + x) * 4;
                        output[offset] = red[pixel]; output[offset + 1] = format == DdsFormat.Bc5 ? green[pixel] : (byte)0;
                        output[offset + 2] = 0; output[offset + 3] = 255;
                    }
                    continue;
                }
                ReadOnlySpan<byte> colorBlock = format == DdsFormat.Bc1 ? block : block[8..];
                ushort c0 = BinaryPrimitives.ReadUInt16LittleEndian(colorBlock), c1 = BinaryPrimitives.ReadUInt16LittleEndian(colorBlock[2..]);
                Expand565(c0, colors); Expand565(c1, colors[4..]);
                bool fourColors = format != DdsFormat.Bc1 || c0 > c1;
                for (int channel = 0; channel < 3; channel++)
                {
                    colors[8 + channel] = (byte)(fourColors ? (2 * colors[channel] + colors[4 + channel]) / 3 : (colors[channel] + colors[4 + channel]) / 2);
                    colors[12 + channel] = (byte)(fourColors ? (colors[channel] + 2 * colors[4 + channel]) / 3 : 0);
                }
                colors[11] = 255; colors[15] = fourColors ? (byte)255 : (byte)0;
                uint colorIndices = U32(colorBlock, 4);
                ulong alphaBits = 0;
                if (format == DdsFormat.Bc2) alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(block);
                else if (format == DdsFormat.Bc3)
                {
                    alpha[0] = block[0]; alpha[1] = block[1];
                    int denominator = alpha[0] > alpha[1] ? 7 : 5;
                    for (int i = 1; i < denominator; i++)
                        alpha[i + 1] = (byte)(((denominator - i) * alpha[0] + i * alpha[1]) / denominator);
                    if (denominator == 5) { alpha[6] = 0; alpha[7] = 255; }
                    for (int i = 0; i < 6; i++) alphaBits |= (ulong)block[2 + i] << (i * 8);
                }
                for (int pixel = 0; pixel < 16; pixel++)
                {
                    int x = bx * 4 + pixel % 4, y = by * 4 + pixel / 4;
                    if (x >= width || y >= height) continue;
                    int offset = (y * width + x) * 4, choice = (int)((colorIndices >> (pixel * 2)) & 3) * 4;
                    colors.Slice(choice, 4).CopyTo(output[offset..]);
                    if (format == DdsFormat.Bc2) output[offset + 3] = (byte)(((alphaBits >> (pixel * 4)) & 15) * 17);
                    else if (format == DdsFormat.Bc3) output[offset + 3] = alpha[(int)((alphaBits >> (pixel * 3)) & 7)];
                }
            }
    }
    private static void DecodeChannel(ReadOnlySpan<byte> block, Span<byte> values)
    {
        Span<byte> palette = stackalloc byte[8]; palette[0] = block[0]; palette[1] = block[1];
        int denominator = palette[0] > palette[1] ? 7 : 5;
        for (int i = 1; i < denominator; i++) palette[i + 1] = (byte)(((denominator - i) * palette[0] + i * palette[1]) / denominator);
        if (denominator == 5) { palette[6] = 0; palette[7] = 255; }
        ulong indices = 0;
        for (int i = 0; i < 6; i++) indices |= (ulong)block[2 + i] << (i * 8);
        for (int i = 0; i < 16; i++) values[i] = palette[(int)((indices >> (i * 3)) & 7)];
    }
    private static void Expand565(ushort value, Span<byte> target)
    {
        int r = (value >> 11) & 31, g = (value >> 5) & 63, b = value & 31;
        target[0] = (byte)((r << 3) | (r >> 2)); target[1] = (byte)((g << 2) | (g >> 4));
        target[2] = (byte)((b << 3) | (b >> 2)); target[3] = 255;
    }
    private static uint U32(ReadOnlySpan<byte> source, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(source[offset..]);
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidDataException(message); }
}
