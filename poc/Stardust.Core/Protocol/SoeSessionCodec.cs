using System.Buffers.Binary;

namespace Stardust.Core.Protocol;

public readonly record struct SoeSessionRequest(uint ConnectionId, uint MaximumDatagramSize);

public readonly record struct SoeSessionResponse(
    uint ConnectionId, uint Seed, byte CrcWidth, byte FirstTransformSelector,
    byte SecondTransformSelector, uint MaximumDatagramSize);

/// <summary>Raw SOE session packets; these do not carry the established-session trailer.</summary>
public static class SoeSessionCodec
{
    public const int RequestLength = 14;
    public const int ResponseLength = 17;
    public const uint ProtocolVersion = 2;

    public static byte[] EncodeRequest(SoeSessionRequest request)
    {
        if (request.MaximumDatagramSize == 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Datagram size must be positive.");
        byte[] packet = new byte[RequestLength];
        packet[1] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(2), ProtocolVersion);
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(6), request.ConnectionId);
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(10), request.MaximumDatagramSize);
        return packet;
    }

    public static SoeSessionRequest DecodeRequest(ReadOnlySpan<byte> packet)
    {
        CheckHeader(packet, RequestLength, 1);
        if (BinaryPrimitives.ReadUInt32BigEndian(packet[2..]) != ProtocolVersion)
            throw new InvalidDataException("Unsupported SOE session version.");
        uint maximum = BinaryPrimitives.ReadUInt32BigEndian(packet[10..]);
        if (maximum == 0) throw new InvalidDataException("Zero requested datagram size.");
        return new SoeSessionRequest(BinaryPrimitives.ReadUInt32BigEndian(packet[6..]), maximum);
    }

    public static SoeSessionResponse DecodeResponse(ReadOnlySpan<byte> packet, uint expectedConnectionId)
    {
        CheckHeader(packet, ResponseLength, 2);
        uint connectionId = BinaryPrimitives.ReadUInt32BigEndian(packet[2..]);
        if (connectionId != expectedConnectionId)
            throw new InvalidDataException("SOE session response connection ID mismatch.");
        byte crcWidth = packet[10];
        if (crcWidth is < 1 or > 4)
            throw new InvalidDataException("Invalid SOE CRC width.");
        uint maximum = BinaryPrimitives.ReadUInt32BigEndian(packet[13..]);
        if (maximum == 0) throw new InvalidDataException("Zero negotiated datagram size.");
        return new SoeSessionResponse(connectionId,
            BinaryPrimitives.ReadUInt32BigEndian(packet[6..]), crcWidth,
            packet[11], packet[12], maximum);
    }

    private static void CheckHeader(ReadOnlySpan<byte> packet, int length, byte opcode)
    {
        if (packet.Length != length) throw new InvalidDataException("Invalid SOE session packet length.");
        if (packet[0] != 0 || packet[1] != opcode)
            throw new InvalidDataException("Invalid SOE session opcode.");
    }
}
