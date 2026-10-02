using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3ScalarState(byte Posture, byte Rank, ulong LinkId, float Height, uint Shock, ulong States);
public abstract record CreatureView3ScalarUpdate { public abstract ushort Index { get; } }
public sealed record CreaturePostureUpdate(byte Value) : CreatureView3ScalarUpdate { public override ushort Index => 11; }
public sealed record CreatureRankUpdate(byte Value) : CreatureView3ScalarUpdate { public override ushort Index => 12; }
public sealed record CreatureLinkUpdate(ulong Value) : CreatureView3ScalarUpdate { public override ushort Index => 13; }
public sealed record CreatureHeightUpdate(float Value) : CreatureView3ScalarUpdate { public override ushort Index => 14; }
public sealed record CreatureShockUpdate(uint Value) : CreatureView3ScalarUpdate { public override ushort Index => 15; }
public sealed record CreatureStatesUpdate(ulong Value) : CreatureView3ScalarUpdate { public override ushort Index => 16; }

public sealed class CreatureView3ScalarDeltaPlan
{
    internal CreatureView3ScalarDeltaPlan(ulong objectId, ushort declaredCount, List<CreatureView3ScalarUpdate> updates)
    { ObjectId = objectId; DeclaredCount = declaredCount; Updates = Array.AsReadOnly(updates.ToArray()); }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureView3ScalarUpdate> Updates { get; }
    public CreatureView3ScalarState Apply(CreatureView3ScalarState initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (!float.IsFinite(initial.Height)) throw new InvalidDataException("Initial scalar height must be finite.");
        var result = initial;
        foreach (var update in Updates)
            result = update switch {
                CreaturePostureUpdate v => result with { Posture = v.Value },
                CreatureRankUpdate v => result with { Rank = v.Value },
                CreatureLinkUpdate v => result with { LinkId = v.Value },
                CreatureHeightUpdate v => result with { Height = v.Value },
                CreatureShockUpdate v => result with { Shock = v.Value },
                CreatureStatesUpdate v => result with { States = v.Value },
                _ => throw new InvalidDataException("Unsupported scalar plan update.")
            };
        return result;
    }
}

/// <summary>CREO:3 indices 11..16 only. Complete immutable plan before any state application.</summary>
public static class CreatureView3ScalarDeltaCodec
{
    public const uint RawTag = 0x4352454F;
    public const byte View = 3;
    public static CreatureView3ScalarDeltaPlan Decode(ReadOnlySpan<byte> message, int maximumPayloadBytes, int maximumUpdates)
    {
        CheckUpdateBudget(maximumUpdates);
        var envelope = ReplicationEnvelopeCodec.DecodeDelta(message, maximumPayloadBytes);
        return Decode(envelope, maximumUpdates);
    }
    public static CreatureView3ScalarDeltaPlan Decode(ReplicationEnvelope envelope, int maximumUpdates)
    {
        ArgumentNullException.ThrowIfNull(envelope); CheckUpdateBudget(maximumUpdates);
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != RawTag || envelope.View != View)
            throw new InvalidDataException("Expected CREO:3 delta envelope.");
        Reader reader = new(envelope.Fields);
        List<CreatureView3ScalarUpdate> updates = new();
        while (!reader.AtEnd)
        {
            if (updates.Count >= maximumUpdates) throw new InvalidDataException("Scalar update budget exceeded.");
            ushort index = BinaryPrimitives.ReadUInt16LittleEndian(reader.Bytes(2));
            CreatureView3ScalarUpdate update = index switch {
                11 => new CreaturePostureUpdate(reader.Bytes(1)[0]),
                12 => new CreatureRankUpdate(reader.Bytes(1)[0]),
                13 => new CreatureLinkUpdate(BinaryPrimitives.ReadUInt64LittleEndian(reader.Bytes(8))),
                14 => new CreatureHeightUpdate(reader.FiniteFloat()),
                15 => new CreatureShockUpdate(BinaryPrimitives.ReadUInt32LittleEndian(reader.Bytes(4))),
                16 => new CreatureStatesUpdate(BinaryPrimitives.ReadUInt64LittleEndian(reader.Bytes(8))),
                _ => throw new InvalidDataException("Unsupported CREO:3 scalar index.")
            };
            updates.Add(update);
        }
        return new(envelope.ObjectId, envelope.DeclaredCount, updates);
    }
    public static byte[] Encode(CreatureView3ScalarDeltaPlan plan, int maximumPayloadBytes, int maximumUpdates)
    {
        ArgumentNullException.ThrowIfNull(plan); CheckUpdateBudget(maximumUpdates);
        if (plan.Updates.Count > maximumUpdates) throw new InvalidDataException("Scalar update budget exceeded.");
        ReplicationEnvelopeCodec.ValidateBudget(maximumPayloadBytes);
        int length = 0;
        foreach (var update in plan.Updates)
        {
            int width = update.Index is 11 or 12 ? 1 : update.Index is 13 or 16 ? 8 : 4;
            if (2 + width > maximumPayloadBytes - 2 - length) throw new InvalidDataException("Scalar payload budget exceeded.");
            length = checked(length + 2 + width);
        }
        byte[] fields = new byte[length]; int cursor = 0;
        foreach (var update in plan.Updates)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(cursor), update.Index); cursor += 2;
            switch (update)
            {
                case CreaturePostureUpdate v: fields[cursor++] = v.Value; break;
                case CreatureRankUpdate v: fields[cursor++] = v.Value; break;
                case CreatureLinkUpdate v: BinaryPrimitives.WriteUInt64LittleEndian(fields.AsSpan(cursor), v.Value); cursor += 8; break;
                case CreatureHeightUpdate v: BinaryPrimitives.WriteSingleLittleEndian(fields.AsSpan(cursor), v.Value); cursor += 4; break;
                case CreatureShockUpdate v: BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(cursor), v.Value); cursor += 4; break;
                case CreatureStatesUpdate v: BinaryPrimitives.WriteUInt64LittleEndian(fields.AsSpan(cursor), v.Value); cursor += 8; break;
                default: throw new InvalidDataException("Unsupported scalar update.");
            }
        }
        var envelope = new ReplicationEnvelope(ReplicationEnvelopeKind.Delta, plan.ObjectId, RawTag, View, plan.DeclaredCount, fields, maximumPayloadBytes);
        return ReplicationEnvelopeCodec.EncodeDelta(envelope, maximumPayloadBytes);
    }
    private static void CheckUpdateBudget(int maximumUpdates)
    { if (maximumUpdates < 0) throw new ArgumentOutOfRangeException(nameof(maximumUpdates)); }
    private ref struct Reader(ReadOnlySpan<byte> source)
    {
        private readonly ReadOnlySpan<byte> source = source;
        private int cursor = 0;
        public bool AtEnd => cursor == source.Length;
        public ReadOnlySpan<byte> Bytes(int count)
        {
            if (count > source.Length - cursor) throw new InvalidDataException("Partial scalar index/value.");
            var value = source.Slice(cursor, count); cursor += count; return value;
        }
        public float FiniteFloat()
        {
            float value = BinaryPrimitives.ReadSingleLittleEndian(Bytes(4));
            if (!float.IsFinite(value)) throw new InvalidDataException("Scalar height must be finite.");
            return value;
        }
    }
}
