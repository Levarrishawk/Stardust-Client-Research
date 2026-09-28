using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Stardust.Core.Protocol;

public enum SoeReliableReceiveDisposition
{
    InOrder,
    Duplicate,
    Future
}

public sealed record SoeReliableReceiveResult(
    SoeReliableReceiveDisposition Disposition,
    ushort ControlSequence,
    IReadOnlyList<SoeSequencedPayload> DeliveredPackets)
{
    public IReadOnlyList<byte[]> DeliveredPayloads => DeliveredPackets.Select(packet => packet.Payload).ToList();
}

public sealed record SoeSequencedPayload(byte Opcode, byte[] Payload);

/// <summary>Deterministic channel-zero sequence, retention, ACK, and reorder state.</summary>
public sealed class SoeReliableChannel
{
    private sealed record Retained(byte[] Packet, long SentAtMilliseconds);

    private readonly SortedDictionary<long, Retained> outbound = new();
    private readonly SortedDictionary<long, SoeSequencedPayload> inboundFuture = new();
    private readonly int maximumOutstanding;
    private readonly int maximumReorderDistance;
    private long nextOutboundSequence;
    private long expectedInboundSequence;

    public SoeReliableChannel(int maximumOutstanding = 50, int maximumReorderDistance = 50)
    {
        if (maximumOutstanding <= 0) throw new ArgumentOutOfRangeException(nameof(maximumOutstanding));
        if (maximumReorderDistance <= 0) throw new ArgumentOutOfRangeException(nameof(maximumReorderDistance));
        this.maximumOutstanding = maximumOutstanding;
        this.maximumReorderDistance = maximumReorderDistance;
    }

    public int RetainedCount => outbound.Count;
    public int AvailableOutboundSlots => maximumOutstanding - outbound.Count;
    public long NextOutboundSequence => nextOutboundSequence;
    public long ExpectedInboundSequence => expectedInboundSequence;

    public byte[] CreateDataPacket(ReadOnlySpan<byte> applicationPayload, long nowMilliseconds)
        => CreateSequencedPacket(0x09, applicationPayload, nowMilliseconds);

    public byte[] CreateSequencedPacket(byte opcode, ReadOnlySpan<byte> payload, long nowMilliseconds)
    {
        if (opcode is < 0x09 or > 0x10) throw new ArgumentOutOfRangeException(nameof(opcode));
        if (outbound.Count >= maximumOutstanding)
            throw new InvalidOperationException("Reliable send window is full.");
        ushort wireSequence = unchecked((ushort)nextOutboundSequence);
        byte[] packet = new byte[4 + payload.Length];
        packet[1] = opcode;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), wireSequence);
        payload.CopyTo(packet.AsSpan(4));
        outbound.Add(nextOutboundSequence, new Retained(packet, nowMilliseconds));
        nextOutboundSequence++;
        return (byte[])packet.Clone();
    }

    public SoeReliableReceiveResult ReceiveDataPacket(ReadOnlySpan<byte> packet)
        => ReceiveSequencedPacket(packet);

    public SoeReliableReceiveResult ReceiveSequencedPacket(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 4 || packet[0] != 0 || packet[1] is < 0x09 or > 0x10)
            throw new InvalidDataException("Invalid reliable-channel sequenced packet.");
        ushort wireSequence = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        long logicalSequence = ExpandNear(expectedInboundSequence, wireSequence);

        if (logicalSequence < expectedInboundSequence)
            return new(SoeReliableReceiveDisposition.Duplicate, wireSequence, Array.Empty<SoeSequencedPayload>());

        SoeSequencedPayload payload = new(packet[1], packet[4..].ToArray());
        if (logicalSequence > expectedInboundSequence)
        {
            if (logicalSequence - expectedInboundSequence > maximumReorderDistance)
            {
                CryptographicOperations.ZeroMemory(payload.Payload);
                throw new InvalidDataException("Reliable sequence exceeds the reorder window.");
            }
            if (!inboundFuture.TryAdd(logicalSequence, payload))
                CryptographicOperations.ZeroMemory(payload.Payload);
            return new(SoeReliableReceiveDisposition.Future, wireSequence, Array.Empty<SoeSequencedPayload>());
        }

        List<SoeSequencedPayload> delivered = new() { payload };
        expectedInboundSequence++;
        while (inboundFuture.Remove(expectedInboundSequence, out SoeSequencedPayload? successor))
        {
            delivered.Add(successor);
            expectedInboundSequence++;
        }
        return new(SoeReliableReceiveDisposition.InOrder,
            unchecked((ushort)(expectedInboundSequence - 1)), delivered);
    }

    public int ApplyCumulativeAcknowledgement(ushort wireSequence)
    {
        if (outbound.Count == 0) return 0;
        long logicalSequence = ExpandNear(outbound.First().Key, wireSequence);
        if (logicalSequence >= nextOutboundSequence) return 0;
        List<long> released = outbound.Keys.Where(key => key <= logicalSequence).ToList();
        foreach (long key in released) outbound.Remove(key);
        return released.Count;
    }

    public bool ApplySelectiveOutOfOrderAcknowledgement(ushort wireSequence)
    {
        if (outbound.Count == 0) return false;
        long logicalSequence = ExpandNear(outbound.First().Key, wireSequence);
        return outbound.Remove(logicalSequence);
    }

    public IReadOnlyList<byte[]> GetPacketsDueForRetransmission(long nowMilliseconds, long timeoutMilliseconds)
    {
        if (timeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        List<byte[]> due = new();
        foreach ((long sequence, Retained retained) in outbound.ToArray())
        {
            if (nowMilliseconds < retained.SentAtMilliseconds ||
                nowMilliseconds - retained.SentAtMilliseconds < timeoutMilliseconds) continue;
            due.Add((byte[])retained.Packet.Clone());
            outbound[sequence] = retained with { SentAtMilliseconds = nowMilliseconds };
        }
        return due;
    }

    public void Clear()
    {
        foreach (Retained retained in outbound.Values)
            CryptographicOperations.ZeroMemory(retained.Packet);
        foreach (SoeSequencedPayload payload in inboundFuture.Values)
            CryptographicOperations.ZeroMemory(payload.Payload);
        outbound.Clear();
        inboundFuture.Clear();
        nextOutboundSequence = 0;
        expectedInboundSequence = 0;
    }

    public static long ExpandNear(long reference, ushort wireSequence)
    {
        long candidate = (reference & ~0xFFFFL) | wireSequence;
        if (candidate - reference > 0x7FFF) candidate -= 0x10000;
        else if (reference - candidate > 0x8000) candidate += 0x10000;
        return candidate;
    }
}
