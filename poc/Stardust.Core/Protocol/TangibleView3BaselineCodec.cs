using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

/// <summary>Complete independent TANO:3 baseline; retains raw bytes/units/collection entries without runtime normalization.</summary>
public static class TangibleView3BaselineCodec
{
    public const uint RawTag = 0x54414E4F;
    public const byte View = 3;
    public const long FixedDecodedBytes = 47; // ID/count + common values + one collection count/counter pair.
    private static void Validate(CreatureView3BaselineLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumStringBytes < 0 || limits.MaximumUnicodeUnits < 0 || limits.MaximumUnicodeUnits > int.MaxValue / 2 || limits.MaximumContainerElements < 0 || limits.MaximumContainerElements > int.MaxValue / 4 || limits.MaximumDecodedBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static TangibleView3BaselineSnapshot Decode(ReadOnlySpan<byte> message, CreatureView3BaselineLimits limits)
    {
        Validate(limits);
        return Decode(ReplicationEnvelopeCodec.DecodeBaseline(message, limits.MaximumPayloadBytes),limits);
    }
    public static TangibleView3BaselineSnapshot Decode(ReplicationEnvelope envelope,CreatureView3BaselineLimits limits)
    {
        Validate(limits);ArgumentNullException.ThrowIfNull(envelope);
        if(envelope.Kind!=ReplicationEnvelopeKind.Baseline||envelope.RawTag!=RawTag||envelope.View!=3||envelope.PayloadSize>limits.MaximumPayloadBytes)throw new InvalidDataException("Expected bounded TANO:3 baseline.");
        Preflight(envelope.Fields,limits);
        Reader r = new(envelope.Fields, limits);
        float complexity = r.Float();
        var file = r.String(); uint spacer = r.U32(); var key = r.String();
        var name = r.Unicode(); uint volume = r.U32(); var customization = r.String();
        var components = r.Ints();
        uint options = r.U32(), use = r.U32(), condition = r.U32(), maximum = r.U32();
        byte visible = r.Byte();r.End();
        return new() { ObjectId = envelope.ObjectId, DeclaredCount = envelope.DeclaredCount, Complexity = complexity,
            NameFile = file, NameSpacer = spacer, NameKey = key, CustomNameUnits = name, Volume = volume, Customization = customization,
            Components = components, Options = options, UseCount = use, ConditionDamage = condition, MaximumCondition = maximum, Visibility = visible,
            DecodedBytes = r.DecodedBytes, FieldWireLength = envelope.OpaqueFieldLength };
    }
    public static byte[] Encode(TangibleView3BaselineSnapshot value,CreatureView3BaselineLimits limits)
        =>ReplicationEnvelopeCodec.EncodeBaseline(EncodeEnvelope(value,limits),limits.MaximumPayloadBytes);
    public static ReplicationEnvelope EncodeEnvelope(TangibleView3BaselineSnapshot value,CreatureView3BaselineLimits limits)
    {
        ArgumentNullException.ThrowIfNull(value); Validate(limits);
        if(!float.IsFinite(value.Complexity))throw new InvalidDataException("Finite TANO complexity required.");
        long variable=value.NameFile.Length+ (long)value.NameKey.Length+value.Customization.Length+2L*value.CustomNameUnits.Count+4L*value.Components.Count;
        long decoded=FixedDecodedBytes+variable;
        long length=47+variable+4L*((value.NameFile.UsesExtendedLength?1:0)+(value.NameKey.UsesExtendedLength?1:0)+(value.Customization.UsesExtendedLength?1:0));
        if(length>limits.MaximumPayloadBytes-2L||decoded>limits.MaximumDecodedBytes||length!=value.FieldWireLength||decoded!=value.DecodedBytes||value.CustomNameUnits.Count>limits.MaximumUnicodeUnits||value.Components.Count>limits.MaximumContainerElements||value.NameFile.Length>limits.MaximumStringBytes||value.NameKey.Length>limits.MaximumStringBytes||value.Customization.Length>limits.MaximumStringBytes)throw new InvalidDataException("TANO baseline limits/invariants.");
        byte[] fields = new byte[value.FieldWireLength]; Writer w = new(fields);
        w.Float(value.Complexity); w.String(value.NameFile); w.U32(value.NameSpacer); w.String(value.NameKey);
        w.U32((uint)value.CustomNameUnits.Count); foreach (ushort unit in value.CustomNameUnits) w.U16(unit);
        w.U32(value.Volume); w.String(value.Customization); w.Ints(value.Components);
        w.U32(value.Options); w.U32(value.UseCount); w.U32(value.ConditionDamage); w.U32(value.MaximumCondition); w.Byte(value.Visibility);
        w.End();
        return new(ReplicationEnvelopeKind.Baseline,value.ObjectId,RawTag,3,value.DeclaredCount,fields,limits.MaximumPayloadBytes);
    }
    private static void Preflight(ReadOnlySpan<byte> fields,CreatureView3BaselineLimits limits)
    {
        Reader r=new(fields,limits);r.Float();r.SkipString();r.U32();r.SkipString();r.SkipUnicode();r.U32();r.SkipString();r.SkipInts();r.U32();r.U32();r.U32();r.U32();r.Byte();r.End();
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
        public float Float() { float value = BinaryPrimitives.ReadSingleLittleEndian(Bytes(4)); if (!float.IsFinite(value)) throw new InvalidDataException("Nonfinite baseline float."); return value; }
        public void SkipString(){ushort prefix=U16();uint n=prefix==65535?U32():prefix;if(n>limits.MaximumStringBytes)throw new InvalidDataException("TANO string budget.");Require((int)n);Charge(n);Bytes((int)n);}
        public void SkipUnicode(){uint n=U32();if(n>limits.MaximumUnicodeUnits)throw new InvalidDataException("TANO unit budget.");int count=(int)n*2;Require(count);Charge(count);Bytes(count);}
        public void SkipInts(){uint n=U32();U32();if(n>limits.MaximumContainerElements)throw new InvalidDataException("TANO component budget.");int count=(int)n*4;Require(count);Charge(count);Bytes(count);}
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
        public void End() { if (cursor != source.Length) throw new InvalidDataException("Unexpected TANO:3 baseline tail."); }
    }
    private ref struct Writer(Span<byte> target)
    {
        private readonly Span<byte> target = target;
        private int cursor = 0;
        private Span<byte> Bytes(int count) { var result = target.Slice(cursor, count); cursor += count; return result; }
        public void Byte(byte value) => Bytes(1)[0] = value;
        public void U16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Bytes(2), value);
        public void U32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Bytes(4), value);
        public void Float(float value) => BinaryPrimitives.WriteSingleLittleEndian(Bytes(4), value);
        public void String(ReplicationByteString value)
        { U16(value.UsesExtendedLength ? ushort.MaxValue : checked((ushort)value.Length)); if (value.UsesExtendedLength) U32((uint)value.Length); value.Bytes.CopyTo(Bytes(value.Length)); }
        public void Ints(ReplicationIntSnapshot value)
        { U32(value.Count); U32(value.Counter); foreach (int item in value.Values) BinaryPrimitives.WriteInt32LittleEndian(Bytes(4), item); }
        public void End() { if (cursor != target.Length) throw new InvalidDataException("Baseline encode size invariant."); }
    }
}
