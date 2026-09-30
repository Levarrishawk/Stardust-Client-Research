using System.Security.Cryptography;

namespace Stardust.Core.Protocol;

/// <summary>One-byte length outer 00 03 blocks; never uses the 00 19 length codec.</summary>
public static class SoeOuterMultiCodec
{
    public static IReadOnlyList<byte[]> Decode(ReadOnlySpan<byte> packet, int maximumPackets = 64, int maximumDepth = 4)
    {
        if (maximumPackets <= 0) throw new ArgumentOutOfRangeException(nameof(maximumPackets));
        if (maximumDepth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        List<byte[]> leaves = new();
        try
        {
            Read(packet, 0, maximumPackets, maximumDepth, leaves);
            return leaves;
        }
        catch
        {
            foreach (byte[] leaf in leaves) CryptographicOperations.ZeroMemory(leaf);
            throw;
        }
    }

    private static void Read(ReadOnlySpan<byte> packet, int depth, int maximumPackets,
        int maximumDepth, List<byte[]> leaves)
    {
        if (depth >= maximumDepth) throw new InvalidDataException("Outer SOE nesting exceeds the local bound.");
        if (packet.Length < 3 || packet[0] != 0 || packet[1] != 0x03)
            throw new InvalidDataException("Invalid outer SOE multipacket header.");
        int cursor = 2;
        while (cursor < packet.Length)
        {
            int length = packet[cursor++];
            if (length < 2 || length > packet.Length - cursor)
                throw new InvalidDataException("Invalid outer SOE block length.");
            ReadOnlySpan<byte> block = packet.Slice(cursor, length);
            if (block[0] == 0 && block[1] == 0x03)
                Read(block, depth + 1, maximumPackets, maximumDepth, leaves);
            else
            {
                if (leaves.Count == maximumPackets)
                    throw new InvalidDataException("Outer SOE multipacket contains too many packets.");
                leaves.Add(block.ToArray());
            }
            cursor += length;
        }
    }
}
