using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3CompositeDeltaLimits(int MaximumPayloadBytes, int MaximumFields, int MaximumScalarUpdates, int MaximumWoundsCommands, int MaximumWoundsValues, int MaximumWoundsElements, int MaximumComponentsCommands, int MaximumComponentsKeys, int MaximumComponentsElements);
public sealed class CreatureView3CompositeState
{
    public CreatureView3CompositeState(ulong objectId, CreatureView3ScalarState scalars, CreatureView3WoundsState wounds, CreatureView3ComponentsState components, CreatureView3ComponentsSnapshot rawComponents)
    {
        ArgumentNullException.ThrowIfNull(scalars); ArgumentNullException.ThrowIfNull(wounds); ArgumentNullException.ThrowIfNull(components); ArgumentNullException.ThrowIfNull(rawComponents);
        if (!float.IsFinite(scalars.Height) || wounds.ObjectId != objectId || components.ObjectId != objectId || rawComponents.ObjectId != objectId) throw new InvalidDataException("Invalid composite seed.");
        ObjectId = objectId; Scalars = scalars; Wounds = wounds; Components = components; RawComponents = rawComponents;
    }
    public ulong ObjectId { get; }
    public CreatureView3ScalarState Scalars { get; }
    public CreatureView3WoundsState Wounds { get; }
    public CreatureView3ComponentsState Components { get; }
    public CreatureView3ComponentsSnapshot RawComponents { get; }
    public static CreatureView3CompositeState FromBaseline(CreatureView3BaselineSnapshot baseline, int maximumRawComponentsKeys, int maximumComponentsElements, int maximumWoundsElements)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        var raw = CreatureView3ComponentsSnapshot.FromBaseline(baseline, maximumRawComponentsKeys);
        return new(baseline.ObjectId, baseline.Scalars, CreatureView3WoundsState.FromBaseline(baseline, maximumWoundsElements), raw.ToState(maximumComponentsElements), raw);
    }
}
public sealed class CreatureView3CompositeUpdate
{
    internal CreatureView3CompositeUpdate(CreatureView3ScalarUpdate scalar) { Scalar = scalar; }
    internal CreatureView3CompositeUpdate(CreatureWoundsUpdate wounds) { Wounds = wounds; }
    public CreatureView3ScalarUpdate? Scalar { get; }
    internal CreatureView3CompositeUpdate(CreatureComponentsUpdate components) { Components = components; }
    public CreatureWoundsUpdate? Wounds { get; }
    public CreatureComponentsUpdate? Components { get; }
    public ushort Index => Components is not null ? (ushort)5 : Scalar?.Index ?? 17;
}
public sealed class CreatureView3CompositeDeltaPlan
{
    internal CreatureView3CompositeDeltaPlan(ulong objectId, ushort declaredCount, CreatureView3CompositeUpdate[] updates, int maximumElements, int maximumComponentsElements, CreatureWoundsApplicationPolicy policy)
    { ObjectId = objectId; DeclaredCount = declaredCount; Updates = Array.AsReadOnly(updates); MaximumWoundsElements = maximumElements; MaximumComponentsElements = maximumComponentsElements; Policy = policy; }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureView3CompositeUpdate> Updates { get; }
    public int MaximumWoundsElements { get; }
    public int MaximumComponentsElements { get; }
    public CreatureWoundsApplicationPolicy Policy { get; }
    public CreatureView3CompositeState Apply(CreatureView3CompositeState initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (initial.ObjectId != ObjectId || initial.Wounds.Values.Count > MaximumWoundsElements || initial.Components.Keys.Count > MaximumComponentsElements) throw new InvalidDataException("Composite state identity/element limit mismatch.");
        var scalars = initial.Scalars; var wounds = initial.Wounds; var components = initial.Components;
        foreach (var update in Updates)
        {
            if (update.Components is { } component)
                components = new CreatureView3ComponentsDeltaPlan(ObjectId, DeclaredCount, new[] { component }, MaximumComponentsElements).Apply(components);
            else if (update.Scalar is { } scalar)
                scalars = new CreatureView3ScalarDeltaPlan(ObjectId, DeclaredCount, new List<CreatureView3ScalarUpdate> { scalar }).Apply(scalars);
            else if (update.Wounds is { } wound)
                wounds = new CreatureView3WoundsDeltaPlan(ObjectId, DeclaredCount, new[] { wound }, MaximumWoundsElements, Policy).Apply(wounds);
            else throw new InvalidDataException("Empty composite update.");
        }
        return new(ObjectId, scalars, wounds, components, initial.RawComponents);
    }
}
/// <summary>Owned ordered CREO:3 field5 and indices 11..17; selected wounds grammar before transactional application.</summary>
public static class CreatureView3CompositeDeltaCodec
{
    private static void Validate(CreatureView3CompositeDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits); ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumFields < 0 || limits.MaximumScalarUpdates < 0 || limits.MaximumWoundsCommands < 0 || limits.MaximumWoundsValues < 0 || limits.MaximumWoundsElements < 0 || limits.MaximumComponentsCommands < 0 || limits.MaximumComponentsKeys < 0 || limits.MaximumComponentsElements < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3CompositeDeltaPlan Decode(ReadOnlySpan<byte> message, CreatureView3CompositeDeltaLimits limits, CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits); return Decode(ReplicationEnvelopeCodec.DecodeDelta(message, limits.MaximumPayloadBytes), limits, policy);
    }
    public static CreatureView3CompositeDeltaPlan Decode(ReplicationEnvelope envelope, CreatureView3CompositeDeltaLimits limits, CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(envelope);
        if (policy is not CreatureWoundsApplicationPolicy.StrictInterval and not CreatureWoundsApplicationPolicy.LegacyBounded) throw new ArgumentOutOfRangeException(nameof(policy));
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != CreatureView3BaselineCodec.RawTag || envelope.View != 3 || envelope.PayloadSize > limits.MaximumPayloadBytes)
            throw new InvalidDataException("Expected bounded CREO:3 composite delta.");
        Reader reader = new(envelope.Fields); List<CreatureView3CompositeUpdate> updates = new(); int scalarCount = 0, commands = 0, values = 0, componentCommands = 0, componentKeys = 0;
        while (!reader.AtEnd)
        {
            if (updates.Count >= limits.MaximumFields) throw new InvalidDataException("Composite field limit exceeded.");
            ushort index = reader.U16();
            if (index == 5)
            {
                uint count = reader.U32(), counter = reader.U32();
                if (count > limits.MaximumComponentsCommands - componentCommands) throw new InvalidDataException("Composite components command limit exceeded.");
                reader.Require((int)count); componentCommands += (int)count;
                var operations = new CreatureComponentsCommand[(int)count];
                for (int n = 0; n < operations.Length; n++)
                {
                    var kind = (CreatureComponentsCommandKind)reader.Byte(); int key = 0;
                    if (kind is CreatureComponentsCommandKind.Drop or CreatureComponentsCommandKind.Add)
                    {
                        if (componentKeys >= limits.MaximumComponentsKeys) throw new InvalidDataException("Composite component key limit exceeded.");
                        key = reader.S32(); componentKeys++;
                    }
                    else if (kind != CreatureComponentsCommandKind.Clear) throw new InvalidDataException("Unknown composite component command.");
                    operations[n] = new(kind, key);
                }
                updates.Add(new(new CreatureComponentsUpdate(counter, operations)));
            }
            else if (index is >= 11 and <= 16)
            {
                if (scalarCount >= limits.MaximumScalarUpdates) throw new InvalidDataException("Composite scalar limit exceeded.");
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
                if (count > limits.MaximumWoundsCommands - commands) throw new InvalidDataException("Composite wounds command limit exceeded.");
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
                        default: throw new InvalidDataException("Unknown composite wounds command.");
                    }
                    if (length > limits.MaximumWoundsValues - values) throw new InvalidDataException("Composite wounds value limit exceeded.");
                    reader.Require(length * 4); values += length; int[] entries = new int[length];
                    for (int i = 0; i < length; i++) entries[i] = reader.S32();
                    operations.Add(new(kind, slot, entries));
                    if (policy == CreatureWoundsApplicationPolicy.LegacyBounded && kind == CreatureWoundsCommandKind.Assign) cursor += (uint)length;
                }
                updates.Add(new(new CreatureWoundsUpdate(counter, operations.ToArray(), count)));
            }
            else throw new InvalidDataException("Unsupported CREO:3 composite index.");
        }
        return new(envelope.ObjectId, envelope.DeclaredCount, updates.ToArray(), limits.MaximumWoundsElements, limits.MaximumComponentsElements, policy);
    }
    public static byte[] Encode(CreatureView3CompositeDeltaPlan plan, CreatureView3CompositeDeltaLimits limits)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(plan);
        long length = 0, scalarCount = 0, commandCount = 0, valueCount = 0, componentCommands = 0, componentKeys = 0;
        if (plan.Updates.Count > limits.MaximumFields) throw new InvalidDataException("Composite field limit exceeded.");
        foreach (var update in plan.Updates)
        {
            if (update.Components is { } components)
            {
                length += 10; componentCommands += components.CommandCount;
                foreach (var command in components.Commands) { length++; if (command.Kind != CreatureComponentsCommandKind.Clear) { length += 4; componentKeys++; } }
            }
            else if (update.Scalar is { } scalar) { scalarCount++; length += 2 + (scalar.Index is 11 or 12 ? 1 : scalar.Index is 13 or 16 ? 8 : 4); }
            else if (update.Wounds is { } wounds)
            {
                length += 10; commandCount += wounds.CommandCount;
                foreach (var command in wounds.Commands)
                { valueCount += command.Values.Count; length += 1 + (command.Kind == CreatureWoundsCommandKind.Clear ? 0 : 2) + 4L * command.Values.Count; }
            }
        }
        if (scalarCount > limits.MaximumScalarUpdates || commandCount > limits.MaximumWoundsCommands || valueCount > limits.MaximumWoundsValues || componentCommands > limits.MaximumComponentsCommands || componentKeys > limits.MaximumComponentsKeys || length > limits.MaximumPayloadBytes - 2)
            throw new InvalidDataException("Composite encode budget exceeded.");
        byte[] fields = new byte[(int)length]; Writer writer = new(fields);
        foreach (var update in plan.Updates)
        {
            writer.U16(update.Index);
            if (update.Components is { } component)
            {
                writer.U32(component.CommandCount); writer.U32(component.Counter);
                foreach (var command in component.Commands) { writer.Byte((byte)command.Kind); if (command.Kind != CreatureComponentsCommandKind.Clear) writer.S32(command.Key); }
                continue;
            }
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
                default: throw new InvalidDataException("Unsupported composite scalar.");
            }
        }
        writer.End();
        return ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta, plan.ObjectId, CreatureView3BaselineCodec.RawTag, 3, plan.DeclaredCount, fields, limits.MaximumPayloadBytes), limits.MaximumPayloadBytes);
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes = bytes; private int cursor = 0;
        public bool AtEnd => cursor == bytes.Length;
        public void Require(int length) { if (length < 0 || length > bytes.Length - cursor) throw new InvalidDataException("Truncated composite field."); }
        private ReadOnlySpan<byte> Read(int length) { Require(length); var result = bytes.Slice(cursor,length); cursor += length; return result; }
        public byte Byte() => Read(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Read(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
        public int S32() => BinaryPrimitives.ReadInt32LittleEndian(Read(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Read(8));
        public float Float() { float value = BinaryPrimitives.ReadSingleLittleEndian(Read(4)); if (!float.IsFinite(value)) throw new InvalidDataException("Nonfinite composite height."); return value; }
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
        public void End() { if (cursor != bytes.Length) throw new InvalidDataException("Composite encode length mismatch."); }
    }
}
