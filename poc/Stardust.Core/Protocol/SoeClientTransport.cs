namespace Stardust.Core.Protocol;

public enum SoeClientTransportState
{
    Offline,
    SessionRequested,
    Established,
    Closed
}

public sealed record SoeInboundDatagramResult(
    IReadOnlyList<byte[]> ApplicationPayloads,
    byte[]? ControlDatagram,
    int ReleasedOutboundPackets);

/// <summary>Socket-independent SOE client transport state for one connection.</summary>
public sealed class SoeClientTransport
{
    private readonly SoeReliableChannel[] reliable = Enumerable.Range(0, 4).Select(_ => new SoeReliableChannel()).ToArray();
    private readonly SoeFragmentReassembler[] fragments = Enumerable.Range(0, 4).Select(_ => new SoeFragmentReassembler()).ToArray();
    private uint connectionId;
    private uint seed;
    private int crcWidth;
    private int maximumDatagramSize;

    public SoeClientTransportState State { get; private set; }
    public int RetainedOutboundPackets => reliable.Sum(channel => channel.RetainedCount);

    public byte[] BeginSession(uint requestedConnectionId, uint requestedMaximumDatagramSize)
    {
        if (State != SoeClientTransportState.Offline)
            throw new InvalidOperationException("SOE session has already started.");
        connectionId = requestedConnectionId;
        State = SoeClientTransportState.SessionRequested;
        return SoeSessionCodec.EncodeRequest(new(connectionId, requestedMaximumDatagramSize));
    }

    public void AcceptSessionResponse(ReadOnlySpan<byte> packet)
    {
        if (State != SoeClientTransportState.SessionRequested)
            throw new InvalidOperationException("SOE session response is not expected.");
        SoeSessionResponse response = SoeSessionCodec.DecodeResponse(packet, connectionId);
        if (response.FirstTransformSelector != 1 || response.SecondTransformSelector != 4)
            throw new NotSupportedException("Unsupported SOE transform selector negotiation.");
        if (response.MaximumDatagramSize > int.MaxValue)
            throw new InvalidDataException("Negotiated datagram size exceeds local bounds.");
        if (response.MaximumDatagramSize < 3u + response.CrcWidth)
            throw new InvalidDataException("Negotiated datagram size cannot contain SOE framing.");
        seed = response.Seed;
        crcWidth = response.CrcWidth;
        maximumDatagramSize = (int)response.MaximumDatagramSize;
        State = SoeClientTransportState.Established;
    }

    public byte[] CreateReliableDatagram(ReadOnlySpan<byte> applicationPayload, long nowMilliseconds, int channel = 0)
    {
        RequireEstablished();
        ValidateChannel(channel);
        int encodedLength = checked(4 + applicationPayload.Length + 1 + crcWidth);
        if (encodedLength > maximumDatagramSize)
            throw new InvalidDataException("Reliable payload exceeds the negotiated datagram size.");
        byte[] transportPacket = reliable[channel].CreateSequencedPacket(
            checked((byte)(0x09 + channel)), applicationPayload, nowMilliseconds);
        return SoeDatagramCodec.EncodeUncompressed(
            transportPacket, seed, crcWidth, maximumDatagramSize);
    }

    public IReadOnlyList<byte[]> CreateReliableDatagrams(
        ReadOnlySpan<byte> applicationPayload, long nowMilliseconds, int channel = 0)
    {
        RequireEstablished();
        ValidateChannel(channel);
        int ordinaryCapacity = maximumDatagramSize - 4 - 1 - crcWidth;
        if (applicationPayload.Length <= ordinaryCapacity)
            return new[] { CreateReliableDatagram(applicationPayload, nowMilliseconds, channel) };

        int firstCapacity = maximumDatagramSize - 8 - 1 - crcWidth;
        int followingCapacity = maximumDatagramSize - 4 - 1 - crcWidth;
        if (firstCapacity <= 0 || followingCapacity <= 0)
            throw new InvalidOperationException("Negotiated datagram size cannot carry fragments.");
        int remainingAfterFirst = applicationPayload.Length - firstCapacity;
        int count = 1 + checked((remainingAfterFirst + followingCapacity - 1) / followingCapacity);
        if (count > reliable[channel].AvailableOutboundSlots)
            throw new InvalidOperationException("Reliable send window cannot retain all fragments atomically.");

        List<byte[]> datagrams = new(count);
        int cursor = 0;
        int firstLength = Math.Min(firstCapacity, applicationPayload.Length);
        byte[] firstPayload = new byte[4 + firstLength];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(firstPayload, checked((uint)applicationPayload.Length));
        applicationPayload[..firstLength].CopyTo(firstPayload.AsSpan(4));
        cursor += firstLength;
        datagrams.Add(ProtectFragment(firstPayload, nowMilliseconds, channel));
        while (cursor < applicationPayload.Length)
        {
            int length = Math.Min(followingCapacity, applicationPayload.Length - cursor);
            datagrams.Add(ProtectFragment(applicationPayload.Slice(cursor, length), nowMilliseconds, channel));
            cursor += length;
        }
        return datagrams;
    }

    public SoeInboundDatagramResult AcceptDatagram(ReadOnlySpan<byte> datagram)
    {
        RequireEstablished();
        byte[] packet = SoeDatagramCodec.Decode(datagram, seed, crcWidth, maximumDatagramSize);
        if (packet.Length < 2 || packet[0] != 0)
            throw new InvalidDataException("Unsupported SOE transport opcode shape.");

        return packet[1] switch
        {
            >= 0x09 and <= 0x10 => AcceptReliableData(packet),
            >= 0x11 and <= 0x18 => AcceptControl(packet),
            _ => throw new InvalidDataException($"Unsupported SOE transport opcode 0x{packet[1]:X2}.")
        };
    }

    public IReadOnlyList<byte[]> GetRetransmissionDatagrams(long nowMilliseconds, long timeoutMilliseconds)
    {
        RequireEstablished();
        return reliable.SelectMany(channel => channel.GetPacketsDueForRetransmission(nowMilliseconds, timeoutMilliseconds))
            .Select(packet => SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagramSize))
            .ToList();
    }

    public void Close()
    {
        foreach (SoeReliableChannel channel in reliable) channel.Clear();
        foreach (SoeFragmentReassembler reassembler in fragments) reassembler.Reset();
        connectionId = 0;
        seed = 0;
        crcWidth = 0;
        maximumDatagramSize = 0;
        State = SoeClientTransportState.Closed;
    }

    private SoeInboundDatagramResult AcceptReliableData(ReadOnlySpan<byte> packet)
    {
        bool isFragment = packet[1] >= 0x0D;
        int channel = isFragment ? packet[1] - 0x0D : packet[1] - 0x09;
        SoeReliableReceiveResult received = reliable[channel].ReceiveSequencedPacket(packet);
        SoeControlKind kind = received.Disposition == SoeReliableReceiveDisposition.Future
            ? SoeControlKind.OutOfOrder
            : SoeControlKind.Acknowledgement;
        byte[] control = SoeControlCodec.Encode(new(kind, received.ControlSequence, (byte)channel));
        byte[] datagram = SoeDatagramCodec.EncodeUncompressed(control, seed, crcWidth, maximumDatagramSize);
        List<byte[]> applications = new();
        foreach (SoeSequencedPayload delivered in received.DeliveredPackets)
        {
            byte[]? payload;
            if (delivered.Opcode >= 0x0D)
            {
                payload = fragments[channel].Accept(delivered.Payload);
                if (payload is null) continue;
            }
            else
            {
                if (fragments[channel].IsActive)
                {
                    fragments[channel].Reset();
                    throw new InvalidDataException("Data packet interrupted fragmented message reassembly.");
                }
                payload = delivered.Payload;
            }
            if (payload.Length >= 2 && payload[0] == 0 && payload[1] == 0x19)
                applications.AddRange(SoeMultiPayloadCodec.Decode(payload, 64, maximumDatagramSize));
            else
                applications.Add(payload);
        }
        return new(applications, datagram, 0);
    }

    private SoeInboundDatagramResult AcceptControl(ReadOnlySpan<byte> packet)
    {
        SoeControlPacket control = SoeControlCodec.Decode(packet);
        SoeReliableChannel channel = reliable[control.Channel];
        int released = control.Kind == SoeControlKind.OutOfOrder
            ? channel.ApplySelectiveOutOfOrderAcknowledgement(control.Sequence) ? 1 : 0
            : channel.ApplyCumulativeAcknowledgement(control.Sequence);
        return new(Array.Empty<byte[]>(), null, released);
    }

    private void RequireEstablished()
    {
        if (State != SoeClientTransportState.Established)
            throw new InvalidOperationException("SOE session is not established.");
    }

    private byte[] ProtectFragment(ReadOnlySpan<byte> payload, long nowMilliseconds, int channel)
    {
        byte[] packet = reliable[channel].CreateSequencedPacket(
            checked((byte)(0x0D + channel)), payload, nowMilliseconds);
        return SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagramSize);
    }

    private static void ValidateChannel(int channel)
    {
        if (channel is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(channel));
    }
}
