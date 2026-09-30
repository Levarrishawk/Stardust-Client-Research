using System.Buffers.Binary;

namespace Stardust.Core.Protocol;

/// <summary>Raw status observations; word indices describe wire positions, not semantics.</summary>
public sealed record SoeStatusPacket(byte Opcode, ushort Tick, IReadOnlyList<uint> RawWords);

public readonly record struct SoeStatusSnapshot(uint Timestamp, ulong PacketsSent, ulong PacketsReceived);

public readonly record struct SoeStatusResponse(ushort Tick, uint Timestamp,
    ulong EchoedPeerPacketsSent, ulong EchoedPeerPacketsReceived,
    ulong LocalPacketsSent, ulong LocalPacketsReceived);

public static class SoeStatusCodec
{
    /// <summary>Build the recovered extended-request reply using a pre-send caller snapshot.</summary>
    public static byte[] CreateResponse(ReadOnlySpan<byte> request, SoeStatusSnapshot local)
    {
        if (request.Length != 40 || request[0] != 0 || request[1] != 7)
            throw new InvalidDataException("Status reply requires a complete extended request.");
        byte[] response = new byte[40];
        response[1] = 8;
        request.Slice(2, 2).CopyTo(response.AsSpan(2));
        BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(4), local.Timestamp);
        request.Slice(24, 16).CopyTo(response.AsSpan(8));
        BinaryPrimitives.WriteUInt64BigEndian(response.AsSpan(24), local.PacketsSent);
        BinaryPrimitives.WriteUInt64BigEndian(response.AsSpan(32), local.PacketsReceived);
        return response;
    }

    public static SoeStatusResponse DecodeResponse(ReadOnlySpan<byte> packet)
    {
        if (packet.Length != 40 || packet[0] != 0 || packet[1] != 8)
            throw new InvalidDataException("Invalid extended status response.");
        return new(BinaryPrimitives.ReadUInt16BigEndian(packet[2..]),
            BinaryPrimitives.ReadUInt32BigEndian(packet[4..]),
            BinaryPrimitives.ReadUInt64BigEndian(packet[8..]),
            BinaryPrimitives.ReadUInt64BigEndian(packet[16..]),
            BinaryPrimitives.ReadUInt64BigEndian(packet[24..]),
            BinaryPrimitives.ReadUInt64BigEndian(packet[32..]));
    }

    public static SoeStatusPacket Decode(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 4 || packet[0] != 0 || packet[1] is not (7 or 8))
            throw new InvalidDataException("Invalid SOE status packet.");
        if (packet.Length != 40 && !(packet[1] == 7 && packet.Length == 4))
            throw new InvalidDataException("Unsupported SOE status packet length.");
        uint[] words = new uint[(packet.Length - 4) / 4];
        for (int index = 0; index < words.Length; index++)
            words[index] = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(4 + index * 4, 4));
        return new(packet[1], BinaryPrimitives.ReadUInt16BigEndian(packet[2..]), Array.AsReadOnly(words));
    }
}
