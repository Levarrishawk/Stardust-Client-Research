using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Stardust.Core.Compression;

/// <summary>Bounded zlib decode with explicit header and Adler-32 checks.</summary>
public static class BoundedZlib
{
    public static byte[] Decode(ReadOnlySpan<byte> compressed, int maximumDecodedBytes, int? expectedBytes = null)
    {
        if (maximumDecodedBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumDecodedBytes));
        if (expectedBytes is < 0 || expectedBytes > maximumDecodedBytes)
            throw new InvalidDataException("Zlib declared output exceeds its configured bound.");
        if (compressed.Length < 8 || (compressed[0] & 0x0F) != 8 || compressed[0] >> 4 > 7 ||
            ((compressed[0] << 8) | compressed[1]) % 31 != 0 || (compressed[1] & 0x20) != 0)
            throw new InvalidDataException("Invalid or unsupported zlib header.");
        uint expectedAdler = BinaryPrimitives.ReadUInt32BigEndian(compressed[^4..]);
        byte[] inputBytes = compressed.ToArray();
        byte[]? decoded = null;
        try
        {
            using MemoryStream input = new(inputBytes, writable: false);
            using ZLibStream zlib = new(input, CompressionMode.Decompress);
            if (expectedBytes is int length)
            {
                decoded = new byte[length];
                try { zlib.ReadExactly(decoded); }
                catch (EndOfStreamException) { throw new InvalidDataException("Zlib output is shorter than its declared size."); }
                if (zlib.ReadByte() != -1) throw new InvalidDataException("Zlib output exceeds its declared size.");
            }
            else
            {
                using MemoryStream output = new();
                byte[] buffer = new byte[4096];
                try
                {
                    while (true)
                    {
                        int read = zlib.Read(buffer);
                        if (read == 0) break;
                        if (output.Length + read > maximumDecodedBytes)
                            throw new InvalidDataException("Zlib output exceeds its configured bound.");
                        output.Write(buffer, 0, read);
                    }
                    decoded = output.ToArray();
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(buffer);
                    CryptographicOperations.ZeroMemory(output.GetBuffer());
                }
            }
            if (Adler32(decoded) != expectedAdler)
                throw new InvalidDataException("Zlib Adler-32 trailer mismatch or truncation.");
            return decoded;
        }
        catch
        {
            if (decoded is not null) CryptographicOperations.ZeroMemory(decoded);
            throw;
        }
        finally { CryptographicOperations.ZeroMemory(inputBytes); }
    }

    private static uint Adler32(ReadOnlySpan<byte> bytes)
    {
        uint low = 1, high = 0;
        foreach (byte value in bytes)
        {
            low = (low + value) % 65521;
            high = (high + low) % 65521;
        }
        return (high << 16) | low;
    }
}
