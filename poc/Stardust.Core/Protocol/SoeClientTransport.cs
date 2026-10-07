using System.Security.Cryptography;

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
    int ReleasedOutboundPackets,
    bool RejectedFraming = false,
    bool PeerDisconnected = false)
{
    public IReadOnlyList<byte[]> ControlDatagrams { get; init; } = ControlDatagram is null
        ? Array.Empty<byte[]>() : new[] { ControlDatagram };
}

/// <summary>Socket-independent SOE client transport state for one connection.</summary>
public sealed class SoeClientTransport
{
    public const int MaximumMessageSize = 1_500_000;
    public const int MaximumWireDatagramSize = 65_507;
    private sealed class PendingMessage(byte[] bytes)
    {
        public byte[] Bytes { get; } = bytes;
        public int Cursor { get; set; }
    }
    private readonly PendingMessage?[] pending = new PendingMessage?[4];
    private readonly SoeReliableChannel[] reliable = Enumerable.Range(0, 4).Select(_ => new SoeReliableChannel()).ToArray();
    private readonly SoeFragmentReassembler[] fragments = Enumerable.Range(0, 4).Select(_ => new SoeFragmentReassembler()).ToArray();
    private uint connectionId;
    private uint seed;
    private int crcWidth;
    private int maximumDatagramSize;
    private uint requestedMaximumDatagramSize;

    public SoeClientTransportState State { get; private set; }
    public int RetainedOutboundPackets => reliable.Sum(channel => channel.RetainedCount);
    public int PendingMessageCount => pending.Count(message => message is not null);

    /// <summary>Owned unsequenced disconnect; trailing reason bits have no inferred semantics.</summary>
    public byte[] CreateDisconnectDatagram(ushort reasonWord = 0x0006)
    {
        RequireEstablished();
        Span<byte> packet = stackalloc byte[8]; packet[0] = 0; packet[1] = 5;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(packet[2..], connectionId);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(packet[6..], reasonWord);
        try { return SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagramSize); }
        finally { CryptographicOperations.ZeroMemory(packet); }
    }

    /// <summary>Owns one bounded message per channel until its fragments enter the send window.</summary>
    public void QueueReliableMessage(ReadOnlySpan<byte> applicationPayload, int channel = 0)
    {
        RequireEstablished();
        ValidateChannel(channel);
        if (pending[channel] is not null)
            throw new InvalidOperationException("Channel already has a queued message.");
        if (applicationPayload.Length > MaximumMessageSize)
            throw new InvalidDataException("Reliable message exceeds the local message limit.");
        if (maximumDatagramSize - 8 - 1 - crcWidth <= 0)
            throw new InvalidOperationException("Negotiated datagram size cannot carry fragments.");
        pending[channel] = new(applicationPayload.ToArray());
    }

    /// <summary>Call after receive/ACK and on ticks; leaves blocked fragments queued.</summary>
    public IReadOnlyList<byte[]> PumpQueuedDatagrams(long nowMilliseconds)
    {
        RequireEstablished();
        List<byte[]> datagrams = new();
        int capacity = maximumDatagramSize - 4 - 1 - crcWidth;
        for (int channel = 0; channel < pending.Length; channel++)
        {
            PendingMessage? message = pending[channel];
            if (message is null) continue;
            while (reliable[channel].AvailableOutboundSlots > 0)
            {
                bool fragmented = message.Bytes.Length > capacity;
                int header = fragmented && message.Cursor == 0 ? 4 : 0;
                int length = Math.Min(capacity - header, message.Bytes.Length - message.Cursor);
                byte[] payload = new byte[header + length];
                if (header != 0)
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(payload, (uint)message.Bytes.Length);
                message.Bytes.AsSpan(message.Cursor, length).CopyTo(payload.AsSpan(header));
                byte[] packet = reliable[channel].CreateSequencedPacket(
                    (byte)((fragmented ? 0x0D : 0x09) + channel), payload, nowMilliseconds);
                try
                {
                    datagrams.Add(SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagramSize));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(payload);
                    CryptographicOperations.ZeroMemory(packet);
                }
                message.Cursor += length;
                if (message.Cursor == message.Bytes.Length)
                {
                    CryptographicOperations.ZeroMemory(message.Bytes);
                    pending[channel] = null;
                    break;
                }
            }
        }
        return datagrams;
    }

    public byte[] BeginSession(uint requestedConnectionId, uint requestedMaximumDatagramSize)
    {
        if (State != SoeClientTransportState.Offline)
            throw new InvalidOperationException("SOE session has already started.");
        if (requestedMaximumDatagramSize is < 14 or > MaximumWireDatagramSize)
            throw new ArgumentOutOfRangeException(nameof(requestedMaximumDatagramSize));
        byte[] request = SoeSessionCodec.EncodeRequest(new(requestedConnectionId, requestedMaximumDatagramSize));
        connectionId = requestedConnectionId;
        this.requestedMaximumDatagramSize = requestedMaximumDatagramSize;
        State = SoeClientTransportState.SessionRequested;
        return request;
    }

    public void AcceptSessionResponse(ReadOnlySpan<byte> packet)
    {
        if (State != SoeClientTransportState.SessionRequested)
            throw new InvalidOperationException("SOE session response is not expected.");
        SoeSessionResponse response = SoeSessionCodec.DecodeResponse(packet, connectionId);
        if (response.FirstTransformSelector != 1 || response.SecondTransformSelector != 4)
            throw new NotSupportedException("Unsupported SOE transform selector negotiation.");
        if (response.MaximumDatagramSize > MaximumWireDatagramSize ||
            response.MaximumDatagramSize > requestedMaximumDatagramSize)
            throw new InvalidDataException("Negotiated datagram size exceeds local bounds.");
        if (response.MaximumDatagramSize < 10u + response.CrcWidth)
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
        if (pending[channel] is not null)
            throw new InvalidOperationException("Pump the queued message before sending another message on this channel.");
        int encodedLength = checked(4 + applicationPayload.Length + 1 + crcWidth);
        if (encodedLength > maximumDatagramSize)
            throw new InvalidDataException("Reliable payload exceeds the negotiated datagram size.");
        byte[] transportPacket = reliable[channel].CreateSequencedPacket(
            checked((byte)(0x09 + channel)), applicationPayload, nowMilliseconds);
        try
        {
            return SoeDatagramCodec.EncodeUncompressed(transportPacket, seed, crcWidth, maximumDatagramSize);
        }
        finally { CryptographicOperations.ZeroMemory(transportPacket); }
    }

    public IReadOnlyList<byte[]> CreateReliableDatagrams(
        ReadOnlySpan<byte> applicationPayload, long nowMilliseconds, int channel = 0)
    {
        RequireEstablished();
        ValidateChannel(channel);
        if (pending[channel] is not null)
            throw new InvalidOperationException("Pump the queued message before sending another message on this channel.");
        if (applicationPayload.Length > MaximumMessageSize)
            throw new InvalidDataException("Reliable message exceeds the local message limit.");
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
        try { datagrams.Add(ProtectFragment(firstPayload, nowMilliseconds, channel)); }
        finally { CryptographicOperations.ZeroMemory(firstPayload); }
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
        if (!SoeDatagramCodec.TryDecode(datagram, seed, crcWidth, maximumDatagramSize, out byte[] packet))
            return new(Array.Empty<byte[]>(), null, 0, RejectedFraming: true);
        try
        {
            if (packet.Length >= 2 && packet[0] == 0 && packet[1] == 0x03)
                return AcceptOuterMulti(packet);
            ValidatePacket(packet);
            return DispatchPacket(packet);
        }
        finally { CryptographicOperations.ZeroMemory(packet); }
    }

    private SoeInboundDatagramResult DispatchPacket(ReadOnlySpan<byte> packet)
        => packet[1] switch
        {
            >= 0x09 and <= 0x10 => AcceptReliableData(packet),
            >= 0x11 and <= 0x18 => AcceptControl(packet),
            0x06 => new(Array.Empty<byte[]>(), null, 0),
            0x07 or 0x08 => AcceptStatus(packet),
            0x05 => AcceptDisconnect(packet),
            _ => throw new InvalidDataException("Unsupported SOE transport opcode.")
        };

    private static void ValidatePacket(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 2 || packet[0] != 0)
            throw new InvalidDataException("Unsupported SOE transport opcode shape.");
        bool valid = packet[1] switch
        {
            >= 0x09 and <= 0x10 => packet.Length >= 4,
            >= 0x11 and <= 0x18 => packet.Length == 4,
            0x06 => true, // Ignored liveness packet; no speculative payload schema.
            0x07 => packet.Length is 4 or 40,
            0x08 => packet.Length == 40,
            0x05 => packet.Length == 8,
            _ => false
        };
        if (!valid) throw new InvalidDataException("Unsupported or malformed SOE transport packet.");
    }

    private SoeInboundDatagramResult AcceptDisconnect(ReadOnlySpan<byte> packet)
    {
        // The server reads the request ID with parseInt and re-emits that raw value
        // with insertInt: its internal endian interpretation cancels on the wire.
        if (System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(packet[2..]) != connectionId)
            throw new InvalidDataException("Disconnect connection ID mismatch.");
        Close();
        return new(Array.Empty<byte[]>(), null, 0, PeerDisconnected: true);
    }

    /// <summary>Last CRC-validated status observation; no automatic response or timeout reset.</summary>
    public SoeStatusPacket? LastStatusPacket { get; private set; }

    /// <summary>Explicit unsequenced reply; caller supplies a snapshot taken before this send.</summary>
    public byte[] CreateStatusResponseDatagram(ReadOnlySpan<byte> request, SoeStatusSnapshot local)
    {
        RequireEstablished();
        byte[] response = SoeStatusCodec.CreateResponse(request, local);
        try { return SoeDatagramCodec.EncodeUncompressed(response, seed, crcWidth, maximumDatagramSize); }
        finally { CryptographicOperations.ZeroMemory(response); }
    }

    private SoeInboundDatagramResult AcceptStatus(ReadOnlySpan<byte> packet)
    {
        LastStatusPacket = SoeStatusCodec.Decode(packet);
        return new(Array.Empty<byte[]>(), null, 0);
    }

    private SoeInboundDatagramResult AcceptOuterMulti(ReadOnlySpan<byte> packet)
    {
        IReadOnlyList<byte[]> blocks = SoeOuterMultiCodec.Decode(packet);
        List<byte[]> applications = new();
        List<byte[]> controls = new();
        int released = 0;
        bool disconnected = false;
        try
        {
            // Validate all framing before any reliable-channel state advances.
            foreach (byte[] block in blocks) ValidatePacket(block);
            foreach (byte[] block in blocks)
            {
                SoeInboundDatagramResult result = DispatchPacket(block);
                applications.AddRange(result.ApplicationPayloads);
                controls.AddRange(result.ControlDatagrams);
                released += result.ReleasedOutboundPackets;
                if (result.PeerDisconnected) { disconnected = true; break; }
            }
            return new(applications, controls.FirstOrDefault(), released, PeerDisconnected: disconnected)
                { ControlDatagrams = controls };
        }
        catch
        {
            foreach (byte[] application in applications) CryptographicOperations.ZeroMemory(application);
            foreach (byte[] control in controls) CryptographicOperations.ZeroMemory(control);
            throw;
        }
        finally
        {
            foreach (byte[] block in blocks) CryptographicOperations.ZeroMemory(block);
        }
    }

    public IReadOnlyList<byte[]> GetRetransmissionDatagrams(long nowMilliseconds, long timeoutMilliseconds)
    {
        RequireEstablished();
        List<byte[]> datagrams = new();
        foreach (SoeReliableChannel channel in reliable)
        {
            IReadOnlyList<byte[]> packets = channel.GetPacketsDueForRetransmission(nowMilliseconds, timeoutMilliseconds);
            try
            {
                foreach (byte[] packet in packets)
                    datagrams.Add(SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagramSize));
            }
            finally
            {
                foreach (byte[] packet in packets) CryptographicOperations.ZeroMemory(packet);
            }
        }
        return datagrams;
    }

    public void Close()
    {
        LastStatusPacket = null;
        for (int channel = 0; channel < pending.Length; channel++)
        {
            if (pending[channel] is PendingMessage message)
                CryptographicOperations.ZeroMemory(message.Bytes);
            pending[channel] = null;
        }
        foreach (SoeReliableChannel channel in reliable) channel.Clear();
        foreach (SoeFragmentReassembler reassembler in fragments) reassembler.Reset();
        connectionId = 0;
        seed = 0;
        crcWidth = 0;
        maximumDatagramSize = 0;
        requestedMaximumDatagramSize = 0;
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
        try
        {
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
                {
                    try { applications.AddRange(SoeMultiPayloadCodec.Decode(payload, 64, MaximumMessageSize)); }
                    finally { CryptographicOperations.ZeroMemory(payload); }
                }
                else
                    applications.Add(delivered.Opcode >= 0x0D ? payload : (byte[])payload.Clone());
            }
            return new(applications, datagram, 0);
        }
        catch
        {
            foreach (byte[] application in applications) CryptographicOperations.ZeroMemory(application);
            throw;
        }
        finally
        {
            foreach (SoeSequencedPayload delivered in received.DeliveredPackets)
                CryptographicOperations.ZeroMemory(delivered.Payload);
        }
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
        try { return SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagramSize); }
        finally { CryptographicOperations.ZeroMemory(packet); }
    }

    private static void ValidateChannel(int channel)
    {
        if (channel is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(channel));
    }
}
