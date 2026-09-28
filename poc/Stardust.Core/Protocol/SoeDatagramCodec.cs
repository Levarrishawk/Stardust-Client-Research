using System.IO.Compression;

namespace Stardust.Core.Protocol;

/// <summary>Established-session framing for uncompressed SOE datagrams.</summary>
public static class SoeDatagramCodec
{
    private const byte Uncompressed = 0;
    private const byte Compressed = 1;

    public static byte[] EncodeUncompressed(
        ReadOnlySpan<byte> transportPacket, uint seed, int crcWidth, int maximumDatagramSize)
    {
        ValidateConfiguration(crcWidth, maximumDatagramSize);
        if (transportPacket.Length < 2)
            throw new ArgumentException("Transport packet must contain an SOE opcode.", nameof(transportPacket));
        int length = checked(transportPacket.Length + 1 + crcWidth);
        if (length > maximumDatagramSize)
            throw new InvalidDataException("Encoded SOE datagram exceeds the negotiated maximum.");

        byte[] datagram = new byte[length];
        transportPacket.CopyTo(datagram);
        datagram[transportPacket.Length] = Uncompressed;
        Span<byte> transformed = datagram.AsSpan(0, transportPacket.Length + 1);
        SoeSelector4.Encrypt(transformed, seed);
        SoeSeededCrc.WriteTrailer(transformed, seed, datagram.AsSpan(transformed.Length, crcWidth));
        return datagram;
    }

    public static byte[] Decode(
        ReadOnlySpan<byte> datagram, uint seed, int crcWidth, int maximumDatagramSize)
    {
        ValidateConfiguration(crcWidth, maximumDatagramSize);
        if (datagram.Length > maximumDatagramSize)
            throw new InvalidDataException("SOE datagram exceeds the negotiated maximum.");
        if (datagram.Length < 1 + 1 + crcWidth)
            throw new InvalidDataException("SOE datagram is shorter than its framing.");
        if (!SoeSeededCrc.VerifyTrailer(datagram, seed, crcWidth))
            throw new InvalidDataException("SOE datagram CRC mismatch.");

        byte[] decoded = datagram[..^crcWidth].ToArray();
        SoeSelector4.Decrypt(decoded, seed);
        byte compressionFlag = decoded[^1];
        if (compressionFlag == Uncompressed) return decoded[..^1];
        if (compressionFlag != Compressed)
            throw new InvalidDataException("Invalid SOE compression marker.");
        return Inflate(decoded[..^1], maximumDatagramSize - 1 - crcWidth);
    }

    private static void ValidateConfiguration(int crcWidth, int maximumDatagramSize)
    {
        if (crcWidth is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(crcWidth));
        if (maximumDatagramSize < 4) throw new ArgumentOutOfRangeException(nameof(maximumDatagramSize));
    }

    private static byte[] Inflate(ReadOnlySpan<byte> packet, int maximumDecodedLength)
    {
        int prefixLength = packet[0] == 0 ? 2 : 1;
        if (packet.Length <= prefixLength)
            throw new InvalidDataException("Compressed SOE datagram has no zlib payload.");
        using MemoryStream input = new(packet[prefixLength..].ToArray(), writable: false);
        using ZLibStream zlib = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        output.Write(packet[..prefixLength]);
        byte[] buffer = new byte[256];
        while (true)
        {
            int read = zlib.Read(buffer);
            if (read == 0) break;
            if (output.Length + read > maximumDecodedLength)
                throw new InvalidDataException("Inflated SOE datagram exceeds the negotiated maximum.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }
}
