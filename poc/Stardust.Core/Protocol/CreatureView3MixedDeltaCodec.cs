using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3MixedDeltaLimits(int MaximumPayloadBytes, int MaximumFields, int MaximumScalarUpdates, int MaximumWoundsCommands, int MaximumWoundsValues, int MaximumWoundsElements);
public sealed class CreatureView3MixedState
{
    public CreatureView3MixedState(ulong objectId, CreatureView3ScalarState scalars, CreatureView3WoundsState wounds)
    {
        ArgumentNullException.ThrowIfNull(scalars); ArgumentNullException.ThrowIfNull(wounds);
        if (!float.IsFinite(scalars.Height) || wounds.ObjectId != objectId) throw new InvalidDataException("Invalid mixed seed.");
        ObjectId = objectId; Scalars = scalars; Wounds = wounds;
    }
    public ulong ObjectId { get; }
    public CreatureView3ScalarState Scalars { get; }
    public CreatureView3WoundsState Wounds { get; }
    public static CreatureView3MixedState FromBaseline(CreatureView3BaselineSnapshot baseline, int maximumWoundsElements)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return new(baseline.ObjectId, baseline.Scalars, CreatureView3WoundsState.FromBaseline(baseline, maximumWoundsElements));
    }
}
public sealed class CreatureView3MixedUpdate
{
    internal CreatureView3MixedUpdate(CreatureView3ScalarUpdate scalar) { Scalar = scalar; }
    internal CreatureView3MixedUpdate(CreatureWoundsUpdate wounds) { Wounds = wounds; }
    public CreatureView3ScalarUpdate? Scalar { get; }
    public CreatureWoundsUpdate? Wounds { get; }
    public ushort Index => Scalar?.Index ?? 17;
}
public sealed class CreatureView3MixedDeltaPlan
{
    internal CreatureView3MixedDeltaPlan(ulong objectId, ushort declaredCount, CreatureView3MixedUpdate[] updates, int maximumElements, CreatureWoundsApplicationPolicy policy)
    { ObjectId = objectId; DeclaredCount = declaredCount; Updates = Array.AsReadOnly(updates); MaximumWoundsElements = maximumElements; Policy = policy; }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureView3MixedUpdate> Updates { get; }
    public int MaximumWoundsElements { get; }
    public CreatureWoundsApplicationPolicy Policy { get; }
    public CreatureView3MixedState Apply(CreatureView3MixedState initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (initial.ObjectId != ObjectId || initial.Wounds.Values.Count > MaximumWoundsElements) throw new InvalidDataException("Mixed state identity/element limit mismatch.");
        var scalars = initial.Scalars; var wounds = initial.Wounds;
        foreach (var update in Updates)
        {
            if (update.Scalar is { } scalar)
                scalars = new CreatureView3ScalarDeltaPlan(ObjectId, DeclaredCount, new List<CreatureView3ScalarUpdate> { scalar }).Apply(scalars);
            else if (update.Wounds is { } wound)
                wounds = new CreatureView3WoundsDeltaPlan(ObjectId, DeclaredCount, new[] { wound }, MaximumWoundsElements, Policy).Apply(wounds);
            else throw new InvalidDataException("Empty mixed update.");
        }
        return new(ObjectId, scalars, wounds);
    }
}
/// <summary>Owned ordered CREO:3 indices 11..17; selected wounds grammar before transactional application.</summary>
public static class CreatureView3MixedDeltaCodec
{
    private static void Validate(CreatureView3MixedDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits); ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumFields < 0 || limits.MaximumScalarUpdates < 0 || limits.MaximumWoundsCommands < 0 || limits.MaximumWoundsValues < 0 || limits.MaximumWoundsElements < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3MixedDeltaPlan Decode(ReadOnlySpan<byte> message, CreatureView3MixedDeltaLimits limits, CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits); return Decode(ReplicationEnvelopeCodec.DecodeDelta(message, limits.MaximumPayloadBytes), limits, policy);
    }
    public static CreatureView3MixedDeltaPlan Decode(ReplicationEnvelope envelope, CreatureView3MixedDeltaLimits limits, CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(envelope);
        if (policy is not CreatureWoundsApplicationPolicy.StrictInterval and not CreatureWoundsApplicationPolicy.LegacyBounded) throw new ArgumentOutOfRangeException(nameof(policy));
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != CreatureView3BaselineCodec.RawTag || envelope.View != 3 || envelope.PayloadSize > limits.MaximumPayloadBytes)
            throw new InvalidDataException("Expected bounded CREO:3 mixed delta.");
        Reader reader = new(envelope.Fields); List<CreatureView3MixedUpdate> updates = new(); int scalarCount = 0, commands = 0, values = 0;
        while (!reader.AtEnd)
        {
            if (updates.Count >= limits.MaximumFields) throw new InvalidDataException("Mixed field limit exceeded.");
            ushort index = reader.U16();
            if (index is >= 11 and <= 16)
            {
                if (scalarCount >= limits.MaximumScalarUpdates) throw new InvalidDataException("Mixed scalar limit exceeded.");
                scalarCount++;
                CreatureView3ScalarUpdate scalar = index switch {
                    11 => new CreaturePostureUpdate(reader.Byte()), 12 => new CreatureRankUpdate(reader.Byte()),
                    13 => new CreatureLinkUpdate(reader.U64()), 14 => new CreatureHeightUpdate(reader.Float()),
                    15 => new CreatureShockUpdate(reader.U32()), 16 => new CreatureStatesUpdate(reader.U64()),
                    _ => throw new InvalidDataException("Unsupported scalar.") };
                updates.Add(new(scalar));
            }
            else if (index == 17)
            {
                uint count = reader.U32(), counter = reader.U32();
                if (count > limits.MaximumWoundsCommands - commands) throw new InvalidDataException("Mixed wounds command limit exceeded.");
                reader.Require((int)count); commands += (int)count; List<CreatureWoundsCommand> operations = new();
                for (ulong cursor = 0; cursor < count; cursor++)
                {
                    var kind = (CreatureWoundsCommandKind)reader.Byte(); ushort slot = 0; int length;
                    switch (kind)
                    {
                        case CreatureWoundsCommandKind.Remove: slot = reader.U16(); length = 0; break;
                        case CreatureWoundsCommandKind.Insert:
                        case CreatureWoundsCommandKind.Replace: slot = reader.U16(); length = 1; break;
                        case CreatureWoundsCommandKind.Assign: length = reader.U16(); break;
                        case CreatureWoundsCommandKind.Clear: length = 0; break;
                        default: throw new InvalidDataException("Unknown mixed wounds command.");
                    }
                    if (length > limits.MaximumWoundsValues - values) throw new InvalidDataException("Mixed wounds value limit exceeded.");
                    reader.Require(length * 4); values += length; int[] entries = new int[length];
                    for (int i = 0; i < length; i++) entries[i] = reader.S32();
                    operations.Add(new(kind, slot, entries));
                    if (policy == CreatureWoundsApplicationPolicy.LegacyBounded && kind == CreatureWoundsCommandKind.Assign) cursor += (uint)length;
                }
                updates.Add(new(new CreatureWoundsUpdate(counter, operations.ToArray(), count)));
            }
            else throw new InvalidDataException("Unsupported CREO:3 mixed index.");
        }
        return new(envelope.ObjectId, envelope.DeclaredCount, updates.ToArray(), limits.MaximumWoundsElements, policy);
    }
    public static byte[] Encode(CreatureView3MixedDeltaPlan plan, CreatureView3MixedDeltaLimits limits)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(plan);
        long length = 0, scalarCount = 0, commandCount = 0, valueCount = 0;
        if (plan.Updates.Count > limits.MaximumFields) throw new InvalidDataException("Mixed field limit exceeded.");
        foreach (var update in plan.Updates)
        {
            if (update.Scalar is { } scalar) { scalarCount++; length += 2 + (scalar.Index is 11 or 12 ? 1 : scalar.Index is 13 or 16 ? 8 : 4); }
            else if (update.Wounds is { } wounds)
            {
                length += 10; commandCount += wounds.CommandCount;
                foreach (var command in wounds.Commands)
                { valueCount += command.Values.Count; length += 1 + (command.Kind == CreatureWoundsCommandKind.Clear ? 0 : 2) + 4L * command.Values.Count; }
            }
        }
        if (scalarCount > limits.MaximumScalarUpdates || commandCount > limits.MaximumWoundsCommands || valueCount > limits.MaximumWoundsValues || length > limits.MaximumPayloadBytes - 2)
            throw new InvalidDataException("Mixed encode budget exceeded.");
        byte[] fields = new byte[(int)length]; Writer writer = new(fields);
        foreach (var update in plan.Updates)
        {
            writer.U16(update.Index);
            switch (update.Scalar)
            {
                case CreaturePostureUpdate v: writer.Byte(v.Value); break;
                case CreatureRankUpdate v: writer.Byte(v.Value); break;
                case CreatureLinkUpdate v: writer.U64(v.Value); break;
                case CreatureHeightUpdate v: writer.Float(v.Value); break;
                case CreatureShockUpdate v: writer.U32(v.Value); break;
                case CreatureStatesUpdate v: writer.U64(v.Value); break;
                case null:
                    var wounds = update.Wounds!; writer.U32(wounds.CommandCount); writer.U32(wounds.Counter);
                    foreach (var command in wounds.Commands)
                    {
                        writer.Byte((byte)command.Kind);
                        if (command.Kind != CreatureWoundsCommandKind.Clear) writer.U16(command.Kind == CreatureWoundsCommandKind.Assign ? checked((ushort)command.Values.Count) : command.Index);
                        foreach (int value in command.Values) writer.S32(value);
                    }
                    break;
                default: throw new InvalidDataException("Unsupported mixed scalar.");
            }
        }
        writer.End();
        return ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta, plan.ObjectId, CreatureView3BaselineCodec.RawTag, 3, plan.DeclaredCount, fields, limits.MaximumPayloadBytes), limits.MaximumPayloadBytes);
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes = bytes; private int cursor = 0;
        public bool AtEnd => cursor == bytes.Length;
        public void Require(int length) { if (length < 0 || length > bytes.Length - cursor) throw new InvalidDataException("Truncated mixed field."); }
        private ReadOnlySpan<byte> Read(int length) { Require(length); var result = bytes.Slice(cursor,length); cursor += length; return result; }
        public byte Byte() => Read(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Read(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
        public int S32() => BinaryPrimitives.ReadInt32LittleEndian(Read(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Read(8));
        public float Float() { float value = BinaryPrimitives.ReadSingleLittleEndian(Read(4)); if (!float.IsFinite(value)) throw new InvalidDataException("Nonfinite mixed height."); return value; }
    }
    private ref struct Writer(Span<byte> bytes)
    {
        private readonly Span<byte> bytes = bytes; private int cursor = 0;
        private Span<byte> Write(int length) { var result = bytes.Slice(cursor,length); cursor += length; return result; }
        public void Byte(byte value) => Write(1)[0] = value;
        public void U16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Write(2),value);
        public void U32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Write(4),value);
        public void S32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Write(4),value);
        public void U64(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(Write(8),value);
        public void Float(float value) => BinaryPrimitives.WriteSingleLittleEndian(Write(4),value);
        public void End() { if (cursor != bytes.Length) throw new InvalidDataException("Mixed encode length mismatch."); }
    }
}
