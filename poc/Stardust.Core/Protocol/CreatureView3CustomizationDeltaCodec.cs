using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

/// <summary>Independent wire, update-count, per-string and aggregate retained-byte caps.</summary>
public sealed record CreatureView3CustomizationDeltaLimits(
    int MaximumPayloadBytes, int MaximumFields, int MaximumStringBytes, long MaximumDecodedBytes);

/// <summary>Field4-only CREO:3 replacements; inner packed data is opaque.</summary>
public static class CreatureView3CustomizationDeltaCodec
{
    public const uint RawTag = 0x4352454F;
    public const byte View = 3;
    public const ushort FieldIndex = 4;
    private static void Validate(CreatureView3CustomizationDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumFields is < 0 or > 4096 || limits.MaximumStringBytes < 0 || limits.MaximumDecodedBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3CustomizationDeltaPlan Decode(ReadOnlySpan<byte> message, CreatureView3CustomizationDeltaLimits limits)
    {
        Validate(limits);
        return Decode(ReplicationEnvelopeCodec.DecodeDelta(message, limits.MaximumPayloadBytes), limits);
    }
    public static CreatureView3CustomizationDeltaPlan Decode(ReplicationEnvelope envelope, CreatureView3CustomizationDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(envelope); Validate(limits);
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != RawTag || envelope.View != View || envelope.PayloadSize > limits.MaximumPayloadBytes)
            throw new InvalidDataException("Expected bounded CREO:3 customization delta.");
        Reader reader = new(envelope.Fields, limits);
        var updates = new List<ReplicationByteString>();
        while (!reader.AtEnd)
        {
            if (updates.Count >= limits.MaximumFields) throw new InvalidDataException("Customization field budget exceeded.");
            if (reader.U16() != FieldIndex) throw new InvalidDataException("Unsupported customization field index.");
            updates.Add(reader.String());
        }
        if (updates.Count == 0) throw new InvalidDataException("Customization delta requires an update.");
        return new(envelope.ObjectId, envelope.DeclaredCount, updates);
    }
    public static byte[] Encode(CreatureView3CustomizationDeltaPlan plan, CreatureView3CustomizationDeltaLimits limits)
        => ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan, limits), limits.MaximumPayloadBytes);
    public static ReplicationEnvelope EncodeEnvelope(CreatureView3CustomizationDeltaPlan plan, CreatureView3CustomizationDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(plan); Validate(limits);
        if (plan.Updates.Count is < 1 or > 4096 || plan.Updates.Count > limits.MaximumFields)
            throw new InvalidDataException("Customization field budget exceeded.");
        int wireLength = 0; long decoded = 0;
        foreach (var update in plan.Updates)
        {
            if (update is null || update.Length > limits.MaximumStringBytes || update.Length > limits.MaximumDecodedBytes - decoded)
                throw new InvalidDataException("Customization string/aggregate budget exceeded.");
            decoded += update.Length;
            int overhead = update.UsesExtendedLength ? 8 : 4; // index + length prefix.
            long required = (long)overhead + update.Length;
            if (required > (long)limits.MaximumPayloadBytes - 2 - wireLength)
                throw new InvalidDataException("Customization payload budget exceeded.");
            wireLength += (int)required;
        }
        byte[] fields = new byte[wireLength]; int cursor = 0;
        foreach (var update in plan.Updates)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(cursor), FieldIndex); cursor += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(cursor), update.UsesExtendedLength ? ushort.MaxValue : checked((ushort)update.Length)); cursor += 2;
            if (update.UsesExtendedLength) { BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(cursor), (uint)update.Length); cursor += 4; }
            update.Bytes.CopyTo(fields.AsSpan(cursor)); cursor += update.Length;
        }
        return new(ReplicationEnvelopeKind.Delta, plan.ObjectId, RawTag, View, plan.DeclaredCount, fields, limits.MaximumPayloadBytes);
    }
    private ref struct Reader(ReadOnlySpan<byte> source, CreatureView3CustomizationDeltaLimits limits)
    {
        private readonly ReadOnlySpan<byte> source = source;
        private readonly CreatureView3CustomizationDeltaLimits limits = limits;
        private int cursor;
        private long decoded;
        public bool AtEnd => cursor == source.Length;
        private ReadOnlySpan<byte> Bytes(int count)
        {
            if (count < 0 || count > source.Length - cursor) throw new InvalidDataException("Truncated customization field.");
            var result = source.Slice(cursor, count); cursor += count; return result;
        }
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
        private uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
        public ReplicationByteString String()
        {
            ushort prefix = U16(); bool extended = prefix == ushort.MaxValue;
            uint length = extended ? U32() : prefix;
            if (length > limits.MaximumStringBytes || length > limits.MaximumDecodedBytes - decoded)
                throw new InvalidDataException("Customization string/aggregate budget exceeded.");
            var bytes = Bytes((int)length); decoded += length;
            return ReplicationByteString.Create(bytes, extended);
        }
    }
}
