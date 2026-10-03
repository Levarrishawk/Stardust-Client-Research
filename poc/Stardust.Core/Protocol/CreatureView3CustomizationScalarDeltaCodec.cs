using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3CustomizationScalarDeltaLimits(int MaximumPayloadBytes, int MaximumFields,
    int MaximumScalarUpdates, int MaximumRawUpdates, int MaximumStringBytes, long MaximumDecodedBytes);

/// <summary>Ordered field4/11..16 union; raw customization remains opaque.</summary>
public static class CreatureView3CustomizationScalarDeltaCodec
{
    public const uint RawTag = CreatureView3ScalarDeltaCodec.RawTag;
    public const byte View = CreatureView3ScalarDeltaCodec.View;
    private static void Validate(CreatureView3CustomizationScalarDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits); ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumFields is < 0 or > 4096 || limits.MaximumScalarUpdates is < 0 or > 4096 ||
            limits.MaximumRawUpdates is < 0 or > 4096 || limits.MaximumStringBytes < 0 || limits.MaximumDecodedBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3CustomizationScalarDeltaPlan Decode(ReadOnlySpan<byte> message, CreatureView3CustomizationScalarDeltaLimits limits)
    { Validate(limits); return Decode(ReplicationEnvelopeCodec.DecodeDelta(message, limits.MaximumPayloadBytes), limits); }
    public static CreatureView3CustomizationScalarDeltaPlan Decode(ReplicationEnvelope envelope, CreatureView3CustomizationScalarDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(envelope); Validate(limits);
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != RawTag || envelope.View != View || envelope.PayloadSize > limits.MaximumPayloadBytes)
            throw new InvalidDataException("Expected bounded CREO:3 customization/scalar delta.");
        Reader reader = new(envelope.Fields, limits); var updates = new List<CreatureView3CustomizationScalarUpdate>();
        int scalarCount = 0, rawCount = 0;
        while (!reader.AtEnd)
        {
            if (updates.Count >= limits.MaximumFields) throw new InvalidDataException("Mixed field budget.");
            ushort index = reader.Index();
            if (index == CreatureView3CustomizationDeltaCodec.FieldIndex)
            {
                if (rawCount >= limits.MaximumRawUpdates) throw new InvalidDataException("Mixed raw-update budget.");
                rawCount++; updates.Add(CreatureView3CustomizationScalarUpdate.FromCustomization(reader.String()));
            }
            else if (index is >= 11 and <= 16)
            {
                if (scalarCount >= limits.MaximumScalarUpdates) throw new InvalidDataException("Mixed scalar-update budget.");
                scalarCount++;
                CreatureView3ScalarUpdate scalar = index switch
                {
                    11 => new CreaturePostureUpdate(reader.Byte()), 12 => new CreatureRankUpdate(reader.Byte()),
                    13 => new CreatureLinkUpdate(reader.U64()), 14 => new CreatureHeightUpdate(reader.Float()),
                    15 => new CreatureShockUpdate(reader.U32()), 16 => new CreatureStatesUpdate(reader.U64()),
                    _ => throw new InvalidDataException("Unsupported mixed scalar.")
                };
                updates.Add(CreatureView3CustomizationScalarUpdate.FromScalar(scalar));
            }
            else throw new InvalidDataException("Unsupported mixed field index.");
        }
        if (updates.Count == 0) throw new InvalidDataException("Mixed delta requires an update.");
        return new(envelope.ObjectId, envelope.DeclaredCount, updates);
    }
    private static int ScalarWidth(CreatureView3ScalarUpdate scalar) => scalar switch
    {
        CreaturePostureUpdate or CreatureRankUpdate => 1,
        CreatureLinkUpdate or CreatureStatesUpdate => 8,
        CreatureShockUpdate => 4,
        CreatureHeightUpdate h when float.IsFinite(h.Value) => 4,
        _ => throw new InvalidDataException("Known finite scalar required.")
    };
    public static byte[] Encode(CreatureView3CustomizationScalarDeltaPlan plan, CreatureView3CustomizationScalarDeltaLimits limits)
        => ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan, limits), limits.MaximumPayloadBytes);
    public static ReplicationEnvelope EncodeEnvelope(CreatureView3CustomizationScalarDeltaPlan plan, CreatureView3CustomizationScalarDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(plan); Validate(limits);
        if (plan.Updates.Count is < 1 or > 4096 || plan.Updates.Count > limits.MaximumFields) throw new InvalidDataException("Mixed field budget.");
        int length = 0, scalars = 0, raws = 0; long decoded = 0;
        foreach (var update in plan.Updates)
        {
            if (update is null) throw new InvalidDataException("Null mixed update.");
            int valueLength, overhead;
            if (update.Customization is { } raw)
            {
                if (++raws > limits.MaximumRawUpdates || raw.Length > limits.MaximumStringBytes) throw new InvalidDataException("Mixed raw/string budget.");
                valueLength = raw.Length; overhead = raw.UsesExtendedLength ? 8 : 4;
            }
            else if (update.Scalar is { } scalar)
            {
                if (++scalars > limits.MaximumScalarUpdates) throw new InvalidDataException("Mixed scalar budget.");
                valueLength = ScalarWidth(scalar); overhead = 2;
            }
            else throw new InvalidDataException("Empty mixed update.");
            if (valueLength > limits.MaximumDecodedBytes - decoded) throw new InvalidDataException("Mixed decoded budget.");
            decoded += valueLength; long required = (long)valueLength + overhead;
            if (required > (long)limits.MaximumPayloadBytes - 2 - length) throw new InvalidDataException("Mixed payload budget.");
            length += (int)required;
        }
        byte[] fields = new byte[length]; int cursor = 0;
        foreach (var update in plan.Updates)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(cursor), update.Index); cursor += 2;
            if (update.Customization is { } raw)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(cursor), raw.UsesExtendedLength ? ushort.MaxValue : checked((ushort)raw.Length)); cursor += 2;
                if (raw.UsesExtendedLength) { BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(cursor), (uint)raw.Length); cursor += 4; }
                raw.Bytes.CopyTo(fields.AsSpan(cursor)); cursor += raw.Length;
            }
            else
            {
                var scalar = update.Scalar!; int width = ScalarWidth(scalar);
                switch (scalar)
                {
                    case CreaturePostureUpdate v: fields[cursor] = v.Value; break;
                    case CreatureRankUpdate v: fields[cursor] = v.Value; break;
                    case CreatureLinkUpdate v: BinaryPrimitives.WriteUInt64LittleEndian(fields.AsSpan(cursor), v.Value); break;
                    case CreatureHeightUpdate v: BinaryPrimitives.WriteSingleLittleEndian(fields.AsSpan(cursor), v.Value); break;
                    case CreatureShockUpdate v: BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(cursor), v.Value); break;
                    case CreatureStatesUpdate v: BinaryPrimitives.WriteUInt64LittleEndian(fields.AsSpan(cursor), v.Value); break;
                }
                cursor += width;
            }
        }
        return new(ReplicationEnvelopeKind.Delta, plan.ObjectId, RawTag, View, plan.DeclaredCount, fields, limits.MaximumPayloadBytes);
    }
    private ref struct Reader(ReadOnlySpan<byte> source, CreatureView3CustomizationScalarDeltaLimits limits)
    {
        private readonly ReadOnlySpan<byte> source = source;
        private readonly CreatureView3CustomizationScalarDeltaLimits limits = limits;
        private int cursor; private long decoded;
        public bool AtEnd => cursor == source.Length;
        private ReadOnlySpan<byte> Bytes(int count, bool charge = true)
        {
            if (count < 0 || count > source.Length - cursor) throw new InvalidDataException("Truncated mixed field.");
            if (charge)
            {
                if (count > limits.MaximumDecodedBytes - decoded) throw new InvalidDataException("Mixed decoded budget.");
                decoded += count;
            }
            var result = source.Slice(cursor, count); cursor += count; return result;
        }
        public ushort Index() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2, false));
        public byte Byte() => Bytes(1)[0];
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Bytes(8));
        public float Float()
        { float value = BinaryPrimitives.ReadSingleLittleEndian(Bytes(4)); if (!float.IsFinite(value)) throw new InvalidDataException("Nonfinite mixed Height."); return value; }
        public ReplicationByteString String()
        {
            ushort prefix = Index(); bool extended = prefix == ushort.MaxValue;
            uint length = extended ? BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4, false)) : prefix;
            if (length > limits.MaximumStringBytes) throw new InvalidDataException("Mixed string budget.");
            return ReplicationByteString.Create(Bytes((int)length), extended);
        }
    }
}
