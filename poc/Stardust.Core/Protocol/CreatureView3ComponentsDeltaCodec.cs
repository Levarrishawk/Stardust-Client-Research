using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3ComponentsDeltaLimits(int MaximumPayloadBytes, int MaximumFields, int MaximumCommands, int MaximumKeys, int MaximumElements);
public enum CreatureComponentsCommandKind : byte { Drop = 0, Add = 1, Clear = 2 }
public readonly record struct CreatureComponentsCommand(CreatureComponentsCommandKind Kind, int Key);
public sealed class CreatureView3ComponentsSnapshot
{
    private readonly int[] rawKeys;
    internal CreatureView3ComponentsSnapshot(ulong objectId, uint counter, int[] keys)
    { ObjectId = objectId; Counter = counter; rawKeys = keys; RawKeys = Array.AsReadOnly(keys); }
    public ulong ObjectId { get; }
    public uint Counter { get; }
    public uint RawCount => (uint)rawKeys.Length;
    public IReadOnlyList<int> RawKeys { get; }
    public CreatureView3ComponentsState ToState(int maximumElements) => new(ObjectId, Counter, rawKeys, maximumElements);
    public static CreatureView3ComponentsSnapshot FromBaseline(CreatureView3BaselineSnapshot baseline, int maximumRawKeys)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        if (maximumRawKeys < 0) throw new ArgumentOutOfRangeException(nameof(maximumRawKeys));
        if (baseline.Components.Count > maximumRawKeys) throw new InvalidDataException("Raw component snapshot exceeds key budget.");
        return new(baseline.ObjectId, baseline.Components.Counter, baseline.Components.Values.ToArray());
    }
}
public sealed class CreatureView3ComponentsState
{
    private readonly int[] keys;
    public CreatureView3ComponentsState(ulong objectId, uint counter, ReadOnlySpan<int> inputKeys, int maximumElements)
    {
        if (maximumElements < 0) throw new ArgumentOutOfRangeException(nameof(maximumElements));
        SortedSet<int> normalized = new();
        foreach (int key in inputKeys)
        {
            if (!normalized.Contains(key) && normalized.Count >= maximumElements) throw new InvalidDataException("Normalized component state exceeds element budget.");
            normalized.Add(key);
        }
        ObjectId = objectId; Counter = counter; keys = normalized.ToArray(); Keys = Array.AsReadOnly(keys);
    }
    public ulong ObjectId { get; }
    public uint Counter { get; }
    public IReadOnlyList<int> Keys { get; }
    public bool Contains(int key) => Array.BinarySearch(keys, key) >= 0;
}
public sealed class CreatureComponentsUpdate
{
    internal CreatureComponentsUpdate(uint counter, CreatureComponentsCommand[] commands) { Counter = counter; Commands = Array.AsReadOnly(commands); }
    public uint Counter { get; }
    public uint CommandCount => (uint)Commands.Count;
    public IReadOnlyList<CreatureComponentsCommand> Commands { get; }
}
public sealed class CreatureView3ComponentsDeltaPlan
{
    internal CreatureView3ComponentsDeltaPlan(ulong objectId, ushort declaredCount, CreatureComponentsUpdate[] updates, int maximumElements)
    { ObjectId = objectId; DeclaredCount = declaredCount; Updates = Array.AsReadOnly(updates); MaximumElements = maximumElements; }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureComponentsUpdate> Updates { get; }
    public int MaximumElements { get; }
    public CreatureView3ComponentsState Apply(CreatureView3ComponentsState initial) => Apply(initial,null);
    internal CreatureView3ComponentsState Apply(CreatureView3ComponentsState initial,Action<int>? observeElementCount)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (initial.ObjectId != ObjectId || initial.Keys.Count > MaximumElements) throw new InvalidDataException("Component state identity/limit mismatch.");
        SortedSet<int> keys = new(initial.Keys); uint counter = initial.Counter;
        foreach (var update in Updates)
        {
            uint skip = unchecked(counter + update.CommandCount) <= update.Counter ? 0 : Math.Min(unchecked(counter - update.Counter + update.CommandCount), update.CommandCount);
            for (int n = (int)skip; n < update.Commands.Count; n++)
            {
                var command = update.Commands[n]; bool changed;
                switch (command.Kind)
                {
                    case CreatureComponentsCommandKind.Drop: changed = keys.Remove(command.Key); break;
                    case CreatureComponentsCommandKind.Add:
                        if (!keys.Contains(command.Key) && keys.Count >= MaximumElements) throw new InvalidDataException("Component add exceeds element budget.");
                        changed = keys.Add(command.Key); break;
                    case CreatureComponentsCommandKind.Clear: changed = keys.Count != 0; keys.Clear(); break;
                    default: throw new InvalidDataException("Unknown component command.");
                }
                if (changed) counter = unchecked(counter + 1);
                observeElementCount?.Invoke(keys.Count);
            }
            counter = Math.Max(counter, update.Counter);
        }
        return new(ObjectId, counter, keys.ToArray(), MaximumElements);
    }
}
/// <summary>Standalone CREO:3 field5 set kernel with owned raw snapshots and modern bounds/atomicity.</summary>
public static class CreatureView3ComponentsDeltaCodec
{
    public const ushort FieldIndex = 5;
    private static void Validate(CreatureView3ComponentsDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits); ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumFields < 0 || limits.MaximumCommands < 0 || limits.MaximumKeys < 0 || limits.MaximumElements < 0) throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3ComponentsSnapshot DecodeSnapshot(ulong objectId, ReadOnlySpan<byte> fieldPayload, int maximumRawKeys)
    {
        if (maximumRawKeys < 0) throw new ArgumentOutOfRangeException(nameof(maximumRawKeys));
        if (fieldPayload.Length < 8) throw new InvalidDataException("Truncated component snapshot.");
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(fieldPayload), counter = BinaryPrimitives.ReadUInt32LittleEndian(fieldPayload[4..]);
        if (count > maximumRawKeys || 4L * count != fieldPayload.Length - 8) throw new InvalidDataException("Component snapshot count/size mismatch.");
        int[] keys = new int[(int)count]; for (int n = 0; n < keys.Length; n++) keys[n] = BinaryPrimitives.ReadInt32LittleEndian(fieldPayload[(8 + n * 4)..]);
        return new(objectId, counter, keys);
    }
    public static byte[] EncodeSnapshot(CreatureView3ComponentsSnapshot snapshot, int maximumRawKeys)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (maximumRawKeys < 0) throw new ArgumentOutOfRangeException(nameof(maximumRawKeys));
        if (snapshot.RawCount > maximumRawKeys || snapshot.RawCount > (int.MaxValue - 8) / 4) throw new InvalidDataException("Component snapshot budget exceeded.");
        byte[] bytes = new byte[8 + (int)snapshot.RawCount * 4]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, snapshot.RawCount); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), snapshot.Counter);
        for (int n = 0; n < snapshot.RawKeys.Count; n++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8 + n * 4), snapshot.RawKeys[n]); return bytes;
    }
    public static CreatureView3ComponentsDeltaPlan Decode(ReadOnlySpan<byte> message, CreatureView3ComponentsDeltaLimits limits)
    { Validate(limits); return Decode(ReplicationEnvelopeCodec.DecodeDelta(message, limits.MaximumPayloadBytes), limits); }
    public static CreatureView3ComponentsDeltaPlan Decode(ReplicationEnvelope envelope, CreatureView3ComponentsDeltaLimits limits)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != CreatureView3BaselineCodec.RawTag || envelope.View != 3 || envelope.PayloadSize > limits.MaximumPayloadBytes) throw new InvalidDataException("Expected bounded CREO:3 component delta.");
        Reader reader = new(envelope.Fields); List<CreatureComponentsUpdate> updates = new(); int commands = 0, keys = 0;
        while (!reader.AtEnd)
        {
            if (updates.Count >= limits.MaximumFields || reader.U16() != FieldIndex) throw new InvalidDataException("Unsupported component field or field budget.");
            uint count = reader.U32(), counter = reader.U32();
            if (count > limits.MaximumCommands - commands) throw new InvalidDataException("Component command budget exceeded.");
            reader.Require((int)count); commands += (int)count; CreatureComponentsCommand[] operations = new CreatureComponentsCommand[(int)count];
            for (int n = 0; n < operations.Length; n++)
            {
                var kind = (CreatureComponentsCommandKind)reader.Byte(); int key = 0;
                if (kind is CreatureComponentsCommandKind.Drop or CreatureComponentsCommandKind.Add)
                {
                    if (keys >= limits.MaximumKeys) throw new InvalidDataException("Component key budget exceeded.");
                    key = reader.S32(); keys++;
                }
                else if (kind != CreatureComponentsCommandKind.Clear) throw new InvalidDataException("Unknown component command.");
                operations[n] = new(kind, key);
            }
            updates.Add(new(counter, operations));
        }
        return new(envelope.ObjectId, envelope.DeclaredCount, updates.ToArray(), limits.MaximumElements);
    }
    public static byte[] Encode(CreatureView3ComponentsDeltaPlan plan, CreatureView3ComponentsDeltaLimits limits)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(plan); long length = 0, commands = 0, keys = 0;
        if (plan.Updates.Count > limits.MaximumFields) throw new InvalidDataException("Component field budget exceeded.");
        foreach (var update in plan.Updates)
        {
            length += 10; commands += update.CommandCount;
            foreach (var command in update.Commands) { length++; if (command.Kind != CreatureComponentsCommandKind.Clear) { length += 4; keys++; } }
        }
        if (commands > limits.MaximumCommands || keys > limits.MaximumKeys || length > limits.MaximumPayloadBytes - 2) throw new InvalidDataException("Component encode budget exceeded.");
        byte[] fields = new byte[(int)length]; Writer writer = new(fields);
        foreach (var update in plan.Updates)
        {
            writer.U16(FieldIndex); writer.U32(update.CommandCount); writer.U32(update.Counter);
            foreach (var command in update.Commands) { writer.Byte((byte)command.Kind); if (command.Kind != CreatureComponentsCommandKind.Clear) writer.S32(command.Key); }
        }
        return ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta, plan.ObjectId, CreatureView3BaselineCodec.RawTag, 3, plan.DeclaredCount, fields, limits.MaximumPayloadBytes), limits.MaximumPayloadBytes);
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes = bytes; private int cursor = 0;
        public bool AtEnd => cursor == bytes.Length;
        public void Require(int length) { if (length < 0 || length > bytes.Length - cursor) throw new InvalidDataException("Truncated component field."); }
        private ReadOnlySpan<byte> Read(int length) { Require(length); var result = bytes.Slice(cursor, length); cursor += length; return result; }
        public byte Byte() => Read(1)[0]; public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Read(2)); public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Read(4)); public int S32() => BinaryPrimitives.ReadInt32LittleEndian(Read(4));
    }
    private ref struct Writer(Span<byte> bytes)
    {
        private readonly Span<byte> bytes = bytes; private int cursor = 0;
        private Span<byte> Write(int length) { var result = bytes.Slice(cursor, length); cursor += length; return result; }
        public void Byte(byte value) => Write(1)[0] = value; public void U16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Write(2), value); public void U32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Write(4), value); public void S32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Write(4), value);
    }
}
