using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public static class MessageCodec
{
    public const int HeaderSize = sizeof(uint) + sizeof(ushort);
    public static MessageEnvelope Decode(ReadOnlyMemory<byte> packet)
    {
        if (packet.Length < HeaderSize) throw new InvalidDataException("Application message is shorter than its fixed header.");
        ReadOnlySpan<byte> span = packet.Span;
        uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(span);
        ushort length = BinaryPrimitives.ReadUInt16LittleEndian(span[sizeof(uint)..]);
        if (length != packet.Length - HeaderSize) throw new InvalidDataException("Application message length does not match its payload.");
        return new MessageEnvelope(opcode, packet[HeaderSize..]);
    }
}
