using System.Buffers.Binary;

namespace Stardust.Core.Protocol;

/// <summary>Portable implementation of SOE transform selector 4.</summary>
public static class SoeSelector4
{
    public static void Encrypt(Span<byte> packet, uint seed) => Transform(packet, seed, decrypt: false);

    public static void Decrypt(Span<byte> packet, uint seed) => Transform(packet, seed, decrypt: true);

    private static void Transform(Span<byte> packet, uint seed, bool decrypt)
    {
        if (packet.IsEmpty) throw new InvalidDataException("Cannot transform an empty SOE packet.");
        int cursor = packet[0] == 0 ? 2 : 1;
        if (packet.Length < cursor) throw new InvalidDataException("SOE packet is shorter than its preserved opcode.");

        uint chain = seed;
        while (cursor + sizeof(uint) <= packet.Length)
        {
            uint input = BinaryPrimitives.ReadUInt32LittleEndian(packet[cursor..]);
            uint output = input ^ chain;
            BinaryPrimitives.WriteUInt32LittleEndian(packet[cursor..], output);
            chain = decrypt ? input : output;
            cursor += sizeof(uint);
        }

        while (cursor < packet.Length)
            packet[cursor++] ^= (byte)chain;
    }
}
