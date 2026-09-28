using System.Buffers.Binary;

namespace Stardust.Core.Protocol;

public enum SoeControlKind : byte
{
    OutOfOrder = 0x11,
    Acknowledgement = 0x15
}

public readonly record struct SoeControlPacket(SoeControlKind Kind, ushort Sequence, byte Channel = 0);

public static class SoeControlCodec
{
    public const int PacketLength = 4;

    public static byte[] Encode(SoeControlPacket control)
    {
        if (control.Kind is not SoeControlKind.OutOfOrder and not SoeControlKind.Acknowledgement || control.Channel > 3)
            throw new ArgumentOutOfRangeException(nameof(control));
        byte[] packet = new byte[PacketLength];
        packet[1] = checked((byte)((byte)control.Kind + control.Channel));
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), control.Sequence);
        return packet;
    }

    public static SoeControlPacket Decode(ReadOnlySpan<byte> packet)
    {
        if (packet.Length != PacketLength || packet[0] != 0)
            throw new InvalidDataException("Invalid SOE reliability control packet.");
        SoeControlKind kind;
        byte channel;
        if (packet[1] is >= 0x11 and <= 0x14)
        {
            kind = SoeControlKind.OutOfOrder;
            channel = (byte)(packet[1] - 0x11);
        }
        else if (packet[1] is >= 0x15 and <= 0x18)
        {
            kind = SoeControlKind.Acknowledgement;
            channel = (byte)(packet[1] - 0x15);
        }
        else
            throw new InvalidDataException("Unsupported SOE reliability control opcode.");
        return new(kind, BinaryPrimitives.ReadUInt16BigEndian(packet[2..]), channel);
    }
}
