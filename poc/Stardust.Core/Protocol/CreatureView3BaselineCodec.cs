using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3BaselineLimits(int MaximumPayloadBytes, int MaximumStringBytes, int MaximumUnicodeUnits, int MaximumContainerElements, long MaximumDecodedBytes);
public sealed class ReplicationByteString
{
    private readonly byte[] bytes;
    internal ReplicationByteString(byte[] bytes, bool extended) { this.bytes = bytes; UsesExtendedLength = extended; }
    public bool UsesExtendedLength { get; }
    public int Length => bytes.Length;
    public byte[] CopyBytes() => (byte[])bytes.Clone();
    internal ReadOnlySpan<byte> Bytes => bytes;
}
public sealed class ReplicationIntSnapshot
{
    internal ReplicationIntSnapshot(uint counter, int[] values) { Counter = counter; Values = Array.AsReadOnly(values); }
    public uint Count => (uint)Values.Count;
    public uint Counter { get; }
    public IReadOnlyList<int> Values { get; }
}
public sealed class CreatureView3BaselineSnapshot
{
    internal CreatureView3BaselineSnapshot() { }
    public ulong ObjectId { get; internal init; }
    public ushort DeclaredCount { get; internal init; }
    public float Complexity { get; internal init; }
    public ReplicationByteString NameFile { get; internal init; } = null!;
    public uint NameSpacer { get; internal init; }
    public ReplicationByteString NameKey { get; internal init; } = null!;
    public IReadOnlyList<ushort> CustomNameUnits { get; internal init; } = null!;
    public uint Volume { get; internal init; }
    public ReplicationByteString Customization { get; internal init; } = null!;
    public ReplicationIntSnapshot Components { get; internal init; } = null!;
    public uint Options { get; internal init; }
    public uint UseCount { get; internal init; }
    public uint ConditionDamage { get; internal init; }
    public uint MaximumCondition { get; internal init; }
    public byte Visibility { get; internal init; }
    public CreatureView3ScalarState Scalars { get; internal init; } = null!;
    public ReplicationIntSnapshot Wounds { get; internal init; } = null!;
    public long DecodedBytes { get; internal init; }
    internal int FieldWireLength { get; init; }
}

/// <summary>Complete CREO:3 baseline; retains raw bytes/units/collection entries without runtime normalization.</summary>
public static class CreatureView3BaselineCodec
{
    public const uint RawTag = 0x4352454F;
    public const long FixedDecodedBytes = 81; // ID/count + scalar values + two collection count/counter pairs.
    private static void Validate(CreatureView3BaselineLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumStringBytes < 0 || limits.MaximumUnicodeUnits < 0 || limits.MaximumUnicodeUnits > int.MaxValue / 2 || limits.MaximumContainerElements < 0 || limits.MaximumContainerElements > int.MaxValue / 4 || limits.MaximumDecodedBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3BaselineSnapshot Decode(ReadOnlySpan<byte> message, CreatureView3BaselineLimits limits)
    {
        Validate(limits);
        var envelope = ReplicationEnvelopeCodec.DecodeBaseline(message, limits.MaximumPayloadBytes);
        if (envelope.RawTag != RawTag || envelope.View != 3) throw new InvalidDataException("Expected CREO:3 baseline.");
        Reader r = new(envelope.Fields, limits);
        float complexity = r.Float();
        var file = r.String(); uint spacer = r.U32(); var key = r.String();
        var name = r.Unicode(); uint volume = r.U32(); var customization = r.String();
        var components = r.Ints();
        uint options = r.U32(), use = r.U32(), condition = r.U32(), maximum = r.U32();
        byte visible = r.Byte(), posture = r.Byte(), rank = r.Byte();
        ulong link = r.U64(); float height = r.Float(); uint shock = r.U32(); ulong states = r.U64();
        var wounds = r.Ints(); r.End();
        return new() { ObjectId = envelope.ObjectId, DeclaredCount = envelope.DeclaredCount, Complexity = complexity,
            NameFile = file, NameSpacer = spacer, NameKey = key, CustomNameUnits = name, Volume = volume, Customization = customization,
            Components = components, Options = options, UseCount = use, ConditionDamage = condition, MaximumCondition = maximum, Visibility = visible,
            Scalars = new(posture, rank, link, height, shock, states), Wounds = wounds, DecodedBytes = r.DecodedBytes, FieldWireLength = envelope.OpaqueFieldLength };
    }
    public static byte[] Encode(CreatureView3BaselineSnapshot value, CreatureView3BaselineLimits limits)
    {
        ArgumentNullException.ThrowIfNull(value); Validate(limits);
        if (value.FieldWireLength > limits.MaximumPayloadBytes - 2 || value.DecodedBytes > limits.MaximumDecodedBytes || value.CustomNameUnits.Count > limits.MaximumUnicodeUnits || value.Components.Count > limits.MaximumContainerElements || value.Wounds.Count > limits.MaximumContainerElements || new[] { value.NameFile, value.NameKey, value.Customization }.Any(s => s.Length > limits.MaximumStringBytes))
            throw new InvalidDataException("Baseline exceeds configured limits.");
        byte[] fields = new byte[value.FieldWireLength]; Writer w = new(fields);
        w.Float(value.Complexity); w.String(value.NameFile); w.U32(value.NameSpacer); w.String(value.NameKey);
        w.U32((uint)value.CustomNameUnits.Count); foreach (ushort unit in value.CustomNameUnits) w.U16(unit);
        w.U32(value.Volume); w.String(value.Customization); w.Ints(value.Components);
        w.U32(value.Options); w.U32(value.UseCount); w.U32(value.ConditionDamage); w.U32(value.MaximumCondition); w.Byte(value.Visibility);
        w.Byte(value.Scalars.Posture); w.Byte(value.Scalars.Rank); w.U64(value.Scalars.LinkId); w.Float(value.Scalars.Height); w.U32(value.Scalars.Shock); w.U64(value.Scalars.States); w.Ints(value.Wounds); w.End();
        return ReplicationEnvelopeCodec.EncodeBaseline(new(ReplicationEnvelopeKind.Baseline, value.ObjectId, RawTag, 3, value.DeclaredCount, fields, limits.MaximumPayloadBytes), limits.MaximumPayloadBytes);
    }
    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> source;
        private readonly CreatureView3BaselineLimits limits;
        private int cursor;
        public long DecodedBytes { get; private set; }
        public Reader(ReadOnlySpan<byte> source, CreatureView3BaselineLimits limits)
        { this.source = source; this.limits = limits; cursor = 0; DecodedBytes = 0; Charge(FixedDecodedBytes); }
        private void Charge(long bytes)
        { if (bytes > limits.MaximumDecodedBytes - DecodedBytes) throw new InvalidDataException("Decoded aggregate budget exceeded."); DecodedBytes += bytes; }
        private void Require(int count) { if (count < 0 || count > source.Length - cursor) throw new InvalidDataException("Truncated baseline field."); }
        public ReadOnlySpan<byte> Bytes(int count) { Require(count); var result = source.Slice(cursor, count); cursor += count; return result; }
        public byte Byte() => Bytes(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Bytes(8));
        public float Float() { float value = BinaryPrimitives.ReadSingleLittleEndian(Bytes(4)); if (!float.IsFinite(value)) throw new InvalidDataException("Nonfinite baseline float."); return value; }
        public ReplicationByteString String()
        {
            ushort prefix = U16(); bool extended = prefix == 65535; uint length = extended ? U32() : prefix;
            if (length > limits.MaximumStringBytes) throw new InvalidDataException("String byte budget exceeded.");
            Require((int)length); Charge(length); return new(Bytes((int)length).ToArray(), extended);
        }
        public IReadOnlyList<ushort> Unicode()
        {
            uint count = U32(); if (count > limits.MaximumUnicodeUnits) throw new InvalidDataException("Unicode unit budget exceeded.");
            int bytes = checked((int)count * 2); Require(bytes); Charge(bytes);
            ushort[] units = new ushort[(int)count]; for (int i = 0; i < units.Length; i++) units[i] = U16(); return Array.AsReadOnly(units);
        }
        public ReplicationIntSnapshot Ints()
        {
            uint count = U32(), counter = U32(); if (count > limits.MaximumContainerElements) throw new InvalidDataException("Container count budget exceeded.");
            int bytes = checked((int)count * 4); Require(bytes); Charge(bytes);
            int[] values = new int[(int)count]; for (int i = 0; i < values.Length; i++) values[i] = BinaryPrimitives.ReadInt32LittleEndian(Bytes(4)); return new(counter, values);
        }
        public void End() { if (cursor != source.Length) throw new InvalidDataException("Unexpected CREO:3 baseline tail."); }
    }
    private ref struct Writer(Span<byte> target)
    {
        private readonly Span<byte> target = target;
        private int cursor = 0;
        private Span<byte> Bytes(int count) { var result = target.Slice(cursor, count); cursor += count; return result; }
        public void Byte(byte value) => Bytes(1)[0] = value;
        public void U16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Bytes(2), value);
        public void U32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Bytes(4), value);
        public void U64(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(Bytes(8), value);
        public void Float(float value) => BinaryPrimitives.WriteSingleLittleEndian(Bytes(4), value);
        public void String(ReplicationByteString value)
        { U16(value.UsesExtendedLength ? ushort.MaxValue : checked((ushort)value.Length)); if (value.UsesExtendedLength) U32((uint)value.Length); value.Bytes.CopyTo(Bytes(value.Length)); }
        public void Ints(ReplicationIntSnapshot value)
        { U32(value.Count); U32(value.Counter); foreach (int item in value.Values) BinaryPrimitives.WriteInt32LittleEndian(Bytes(4), item); }
        public void End() { if (cursor != target.Length) throw new InvalidDataException("Baseline encode size invariant."); }
    }
}
