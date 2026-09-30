using System.Text;

namespace Stardust.Core.Assets;

public static class LegacyAssetPath
{
    public static string Normalize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length > 1024 || path.Any(c => c < 0x20 || c > 0x7E))
            throw new ArgumentException("Legacy logical paths must be bounded printable ASCII.", nameof(path));
        string value = path.Replace('\\', '/');
        while (true)
        {
            value = value.TrimStart('/');
            if (value.StartsWith("./", StringComparison.Ordinal)) value = value[2..];
            else if (value.StartsWith("../", StringComparison.Ordinal)) value = value[3..];
            else break;
        }
        StringBuilder result = new();
        foreach (char c in value)
        {
            if (c == '/' && result.Length != 0 && result[^1] == '/') continue;
            result.Append(c is >= 'A' and <= 'Z' ? (char)(c + ('a' - 'A')) : c);
        }
        if (result.Length == 0) throw new ArgumentException("Legacy logical path is empty.", nameof(path));
        return result.ToString();
    }

    public static uint Checksum(string path)
        => ChecksumBytes(Encoding.ASCII.GetBytes(Normalize(path)));

    public static uint ChecksumBytes(ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (byte c in bytes)
        {
            crc ^= (uint)c << 24;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc << 1) ^ ((crc & 0x80000000) != 0 ? 0x04C11DB7u : 0u);
        }
        return ~crc;
    }
}
