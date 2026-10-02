using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public enum ReplicationEnvelopeKind { Baseline, Delta }

/// <summary>Owned opaque schema bytes; count/tag/view are raw protocol scalars.</summary>
public sealed class ReplicationEnvelope
{
    private readonly byte[] fields;
    public ReplicationEnvelope(ReplicationEnvelopeKind kind, ulong objectId, uint rawTag, byte view, ushort declaredCount, ReadOnlySpan<byte> opaqueFields, int maximumPayloadBytes)
    {
        if (kind is not ReplicationEnvelopeKind.Baseline and not ReplicationEnvelopeKind.Delta) throw new ArgumentOutOfRangeException(nameof(kind));
        ReplicationEnvelopeCodec.ValidateBudget(maximumPayloadBytes);
        if (opaqueFields.Length > maximumPayloadBytes - 2) throw new InvalidDataException("Replication payload exceeds configured budget.");
        Kind = kind; ObjectId = objectId; RawTag = rawTag; View = view; DeclaredCount = declaredCount;
        fields = opaqueFields.ToArray();
    }
    public ReplicationEnvelopeKind Kind { get; }
    public ulong ObjectId { get; }
    public uint RawTag { get; }
    public byte View { get; }
    public ushort DeclaredCount { get; }
    public uint PayloadSize => checked((uint)(2 + fields.Length));
    public int OpaqueFieldLength => fields.Length;
    public byte[] CopyOpaqueFields() => (byte[])fields.Clone();
    internal ReadOnlySpan<byte> Fields => fields;
}

public static class ReplicationEnvelopeCodec
{
    public const int OuterHeaderLength = 23; // Server's 27 includes four SOE opcode/sequence bytes.
    public const int MinimumMessageLength = 25;
    public const uint BaselineOpcode = 0x68A75F0C;
    public const uint DeltaOpcode = 0x12862153;
    public static ReplicationEnvelope DecodeBaseline(ReadOnlySpan<byte> message, int maximumPayloadBytes) => Decode(message, ReplicationEnvelopeKind.Baseline, maximumPayloadBytes);
    public static ReplicationEnvelope DecodeDelta(ReadOnlySpan<byte> message, int maximumPayloadBytes) => Decode(message, ReplicationEnvelopeKind.Delta, maximumPayloadBytes);
    public static byte[] EncodeBaseline(ReplicationEnvelope value, int maximumPayloadBytes) => Encode(value, ReplicationEnvelopeKind.Baseline, maximumPayloadBytes);
    public static byte[] EncodeDelta(ReplicationEnvelope value, int maximumPayloadBytes) => Encode(value, ReplicationEnvelopeKind.Delta, maximumPayloadBytes);
    internal static void ValidateBudget(int maximumPayloadBytes)
    {
        if (maximumPayloadBytes < 2 || maximumPayloadBytes > int.MaxValue - OuterHeaderLength)
            throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes), "Payload budget must include count and fit a managed application message.");
    }
    private static uint Opcode(ReplicationEnvelopeKind kind) => kind == ReplicationEnvelopeKind.Baseline ? BaselineOpcode : DeltaOpcode;
    private static ReplicationEnvelope Decode(ReadOnlySpan<byte> message, ReplicationEnvelopeKind kind, int maximumPayloadBytes)
    {
        ValidateBudget(maximumPayloadBytes);
        if (message.Length < MinimumMessageLength) throw new InvalidDataException("Replication envelope is truncated.");
        if (BinaryPrimitives.ReadUInt16LittleEndian(message) != 5 || BinaryPrimitives.ReadUInt32LittleEndian(message[2..]) != Opcode(kind))
            throw new InvalidDataException("Unexpected replication count/opcode.");
        uint size = BinaryPrimitives.ReadUInt32LittleEndian(message[19..]);
        if (size < 2 || size > maximumPayloadBytes || size != message.Length - OuterHeaderLength)
            throw new InvalidDataException("Replication payload size mismatch or budget exceeded.");
        return new(kind, BinaryPrimitives.ReadUInt64LittleEndian(message[6..]), BinaryPrimitives.ReadUInt32LittleEndian(message[14..]), message[18], BinaryPrimitives.ReadUInt16LittleEndian(message[23..]), message[25..], maximumPayloadBytes);
    }
    private static byte[] Encode(ReplicationEnvelope value, ReplicationEnvelopeKind kind, int maximumPayloadBytes)
    {
        ArgumentNullException.ThrowIfNull(value); ValidateBudget(maximumPayloadBytes);
        if (value.Kind != kind) throw new InvalidDataException("Replication envelope kind mismatch.");
        if (value.PayloadSize > maximumPayloadBytes) throw new InvalidDataException("Replication payload exceeds configured budget.");
        byte[] message = new byte[checked(OuterHeaderLength + (int)value.PayloadSize)];
        BinaryPrimitives.WriteUInt16LittleEndian(message, 5);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(2), Opcode(kind));
        BinaryPrimitives.WriteUInt64LittleEndian(message.AsSpan(6), value.ObjectId);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(14), value.RawTag);
        message[18] = value.View;
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(19), value.PayloadSize);
        BinaryPrimitives.WriteUInt16LittleEndian(message.AsSpan(23), value.DeclaredCount);
        value.Fields.CopyTo(message.AsSpan(25));
        return message;
    }
}
