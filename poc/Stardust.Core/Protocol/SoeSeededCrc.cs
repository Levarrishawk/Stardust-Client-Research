namespace Stardust.Core.Protocol;

/// <summary>SOE's reflected CRC-32 with the session seed folded into its initial state.</summary>
public static class SoeSeededCrc
{
    private static readonly uint[] Table = CreateTable();

    public static uint Compute(ReadOnlySpan<byte> data, uint seed)
    {
        uint crc = Table[(byte)~seed] ^ 0x00FFFFFFu;
        foreach (int shift in new[] { 8, 16, 24 })
            crc = (crc >> 8) ^ Table[(byte)((seed >> shift) ^ crc)];
        foreach (byte value in data)
            crc = (crc >> 8) ^ Table[(byte)(value ^ crc)];
        return ~crc;
    }

    public static void WriteTrailer(ReadOnlySpan<byte> data, uint seed, Span<byte> trailer)
    {
        if (trailer.Length is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(trailer), "CRC width must be 1 to 4 bytes.");
        uint crc = Compute(data, seed);
        for (int i = trailer.Length - 1; i >= 0; i--)
        {
            trailer[i] = (byte)crc;
            crc >>= 8;
        }
    }

    public static bool VerifyTrailer(ReadOnlySpan<byte> packet, uint seed, int width)
    {
        if (width is < 1 or > 4 || packet.Length < width) return false;
        Span<byte> expected = stackalloc byte[4];
        WriteTrailer(packet[..^width], seed, expected[..width]);
        return packet[^width..].SequenceEqual(expected[..width]);
    }

    private static uint[] CreateTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xEDB88320u);
            table[i] = crc;
        }
        return table;
    }
}
