using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3WoundsDeltaLimits(int MaximumPayloadBytes, int MaximumFields, int MaximumCommands, int MaximumValues, int MaximumElements);
public enum CreatureWoundsApplicationPolicy { StrictInterval, LegacyBounded }
public enum CreatureWoundsCommandKind : byte { Remove = 0, Insert = 1, Replace = 2, Assign = 3, Clear = 4 }
public sealed class CreatureWoundsCommand
{
    internal CreatureWoundsCommand(CreatureWoundsCommandKind kind, ushort index, int[] values)
    { Kind = kind; Index = index; Values = Array.AsReadOnly(values); }
    public CreatureWoundsCommandKind Kind { get; }
    public ushort Index { get; } // Only remove/insert/replace have a wire index.
    public IReadOnlyList<int> Values { get; }
}
public sealed class CreatureWoundsUpdate
{
    internal CreatureWoundsUpdate(uint counter, CreatureWoundsCommand[] commands, uint? commandCount = null)
    { Counter = counter; Commands = Array.AsReadOnly(commands); CommandCount = commandCount ?? (uint)commands.Length; }
    public uint Counter { get; }
    public uint CommandCount { get; } // Raw wire loop bound; legacy assignments contribute extra loop steps.
    public IReadOnlyList<CreatureWoundsCommand> Commands { get; }
}
public sealed class CreatureView3WoundsState
{
    public CreatureView3WoundsState(ulong objectId, uint counter, ReadOnlySpan<int> values, int maximumElements)
    {
        if (maximumElements < 0) throw new ArgumentOutOfRangeException(nameof(maximumElements));
        if (values.Length > maximumElements) throw new InvalidDataException("Wounds state element budget exceeded.");
        ObjectId = objectId; Counter = counter; Values = Array.AsReadOnly(values.ToArray());
    }
    public ulong ObjectId { get; }
    public uint Counter { get; }
    public IReadOnlyList<int> Values { get; }
    public static CreatureView3WoundsState FromBaseline(CreatureView3BaselineSnapshot baseline, int maximumElements)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        if (maximumElements < 0) throw new ArgumentOutOfRangeException(nameof(maximumElements));
        if (baseline.Wounds.Count > maximumElements) throw new InvalidDataException("Baseline wounds exceed state budget.");
        return new(baseline.ObjectId, baseline.Wounds.Counter, baseline.Wounds.Values.ToArray(), maximumElements);
    }
}
public sealed class CreatureView3WoundsDeltaPlan
{
    internal CreatureView3WoundsDeltaPlan(ulong objectId, ushort declaredCount, CreatureWoundsUpdate[] updates, int maximumElements, CreatureWoundsApplicationPolicy policy)
    { ObjectId = objectId; DeclaredCount = declaredCount; Updates = Array.AsReadOnly(updates); MaximumElements = maximumElements; Policy = policy; }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureWoundsUpdate> Updates { get; }
    public int MaximumElements { get; }
    public CreatureWoundsApplicationPolicy Policy { get; }
    /// <summary>Selected bounded policy; returns a new state only after every field succeeds.</summary>
    public CreatureView3WoundsState Apply(CreatureView3WoundsState initial) => Apply(initial,null);
    internal CreatureView3WoundsState Apply(CreatureView3WoundsState initial,Action<int>? observeElementCount)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (initial.ObjectId != ObjectId) throw new InvalidDataException("Wounds object identity mismatch.");
        if (initial.Values.Count > MaximumElements) throw new InvalidDataException("Initial wounds exceed budget.");
        if (Policy == CreatureWoundsApplicationPolicy.LegacyBounded) return ApplyLegacy(initial,observeElementCount);
        List<int> values = new(initial.Values); uint counter = initial.Counter;
        foreach (var update in Updates)
        {
            uint start = unchecked(update.Counter - update.CommandCount);
            uint skip = unchecked(counter - start);
            if (skip > update.CommandCount) throw new InvalidDataException("Counter outside packet interval.");
            for (int n = (int)skip; n < update.Commands.Count; n++)
            {
                var command = update.Commands[n]; int index = command.Index;
                switch (command.Kind)
                {
                    case CreatureWoundsCommandKind.Remove:
                        if (index >= values.Count) throw new InvalidDataException("Remove index outside wounds.");
                        values.RemoveAt(index); break;
                    case CreatureWoundsCommandKind.Insert:
                        if (index > values.Count || values.Count >= MaximumElements) throw new InvalidDataException("Insert index/growth exceeds wounds budget.");
                        values.Insert(index, command.Values[0]); break;
                    case CreatureWoundsCommandKind.Replace:
                        if (index >= values.Count) throw new InvalidDataException("Replace index outside wounds.");
                        values[index] = command.Values[0]; break;
                    case CreatureWoundsCommandKind.Assign:
                        if (command.Values.Count > MaximumElements) throw new InvalidDataException("Assignment exceeds wounds budget.");
                        values.Clear(); values.AddRange(command.Values); break;
                    case CreatureWoundsCommandKind.Clear: values.Clear(); break;
                    default: throw new InvalidDataException("Unknown wounds command.");
                }
                observeElementCount?.Invoke(values.Count);
            }
            counter = update.Counter;
        }
        return new(ObjectId, counter, values.ToArray(), MaximumElements);
    }
    private CreatureView3WoundsState ApplyLegacy(CreatureView3WoundsState initial,Action<int>? observeElementCount)
    {
        List<int> values = new(initial.Values); uint counter = initial.Counter;
        foreach (var update in Updates)
        {
            uint skip = Math.Min(unchecked(update.CommandCount - update.Counter + counter), update.CommandCount);
            ulong cursor = 0;
            foreach (var command in update.Commands)
            {
                bool apply = cursor >= skip;
                cursor += command.Kind == CreatureWoundsCommandKind.Assign ? (ulong)command.Values.Count + 1 : 1;
                if (!apply) continue;
                int index = command.Index;
                switch (command.Kind)
                {
                    case CreatureWoundsCommandKind.Remove:
                        if (index >= values.Count) break;
                        values.RemoveAt(index); counter = unchecked(counter + 1); break;
                    case CreatureWoundsCommandKind.Insert:
                        long insertSize = Math.Max((long)index, values.Count) + 1;
                        if (insertSize > MaximumElements) throw new InvalidDataException("Legacy insertion growth exceeds budget.");
                        while (values.Count < index) values.Add(0);
                        values.Insert(index, command.Values[0]); counter = unchecked(counter + 1); break;
                    case CreatureWoundsCommandKind.Replace:
                        if (index < values.Count && values[index] == command.Values[0]) break;
                        if (index >= MaximumElements) throw new InvalidDataException("Legacy replacement growth exceeds budget.");
                        while (values.Count <= index) values.Add(0);
                        values[index] = command.Values[0]; counter = unchecked(counter + 1); break;
                    case CreatureWoundsCommandKind.Assign:
                        if (command.Values.Count > MaximumElements) throw new InvalidDataException("Legacy assignment exceeds budget.");
                        values.Clear(); values.AddRange(command.Values);
                        counter = unchecked(counter + (uint)command.Values.Count + 1); break;
                    case CreatureWoundsCommandKind.Clear:
                        if (values.Count == 0) break;
                        values.Clear(); counter = unchecked(counter + 1); break;
                    default: throw new InvalidDataException("Unknown wounds command.");
                }
                observeElementCount?.Invoke(values.Count);
            }
        }
        return new(ObjectId, counter, values.ToArray(), MaximumElements);
    }
}
/// <summary>Only CREO:3 field 17, with complete command parsing before atomic application.</summary>
public static class CreatureView3WoundsDeltaCodec
{
    public const ushort FieldIndex = 17;
    private static void Validate(CreatureView3WoundsDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ReplicationEnvelopeCodec.ValidateBudget(limits.MaximumPayloadBytes);
        if (limits.MaximumFields < 0 || limits.MaximumCommands < 0 || limits.MaximumValues < 0 || limits.MaximumElements < 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static CreatureView3WoundsDeltaPlan Decode(ReadOnlySpan<byte> message, CreatureView3WoundsDeltaLimits limits, CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits);
        return Decode(ReplicationEnvelopeCodec.DecodeDelta(message, limits.MaximumPayloadBytes), limits, policy);
    }
    public static CreatureView3WoundsDeltaPlan Decode(ReplicationEnvelope envelope, CreatureView3WoundsDeltaLimits limits, CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(envelope);
        if (policy is not CreatureWoundsApplicationPolicy.StrictInterval and not CreatureWoundsApplicationPolicy.LegacyBounded) throw new ArgumentOutOfRangeException(nameof(policy));
        if (envelope.Kind != ReplicationEnvelopeKind.Delta || envelope.RawTag != CreatureView3BaselineCodec.RawTag || envelope.View != 3)
            throw new InvalidDataException("Expected CREO:3 wounds delta.");
        if (envelope.PayloadSize > limits.MaximumPayloadBytes) throw new InvalidDataException("Wounds payload budget exceeded.");
        Reader reader = new(envelope.Fields); List<CreatureWoundsUpdate> updates = new(); int totalCommands = 0, totalValues = 0;
        while (!reader.AtEnd)
        {
            if (updates.Count >= limits.MaximumFields) throw new InvalidDataException("Wounds field budget exceeded.");
            if (reader.U16() != FieldIndex) throw new InvalidDataException("Unsupported mixed CREO:3 field.");
            uint count = reader.U32(), counter = reader.U32();
            if (count > limits.MaximumCommands - totalCommands) throw new InvalidDataException("Wounds command budget exceeded.");
            reader.Require((int)count); // Every command needs at least one byte, before any command allocation.
            totalCommands += (int)count; List<CreatureWoundsCommand> commands = new();
            for (ulong n = 0; n < count; n++)
            {
                var kind = (CreatureWoundsCommandKind)reader.Byte(); ushort index = 0; int valueCount;
                switch (kind)
                {
                    case CreatureWoundsCommandKind.Remove: index = reader.U16(); valueCount = 0; break;
                    case CreatureWoundsCommandKind.Insert:
                    case CreatureWoundsCommandKind.Replace: index = reader.U16(); valueCount = 1; break;
                    case CreatureWoundsCommandKind.Assign: valueCount = reader.U16(); break;
                    case CreatureWoundsCommandKind.Clear: valueCount = 0; break;
                    default: throw new InvalidDataException("Unknown wounds command.");
                }
                if (valueCount > limits.MaximumValues - totalValues) throw new InvalidDataException("Wounds decoded-value budget exceeded.");
                reader.Require(checked(valueCount * 4)); totalValues += valueCount;
                int[] values = new int[valueCount]; for (int i = 0; i < valueCount; i++) values[i] = reader.S32();
                commands.Add(new(kind, index, values));
                if (policy == CreatureWoundsApplicationPolicy.LegacyBounded && kind == CreatureWoundsCommandKind.Assign) n += (uint)valueCount;
            }
            updates.Add(new(counter, commands.ToArray(), count));
        }
        return new(envelope.ObjectId, envelope.DeclaredCount, updates.ToArray(), limits.MaximumElements, policy);
    }
    public static byte[] Encode(CreatureView3WoundsDeltaPlan plan, CreatureView3WoundsDeltaLimits limits)
    {
        Validate(limits); ArgumentNullException.ThrowIfNull(plan);
        if (plan.Updates.Count > limits.MaximumFields) throw new InvalidDataException("Wounds field budget exceeded.");
        long length = 0, totalCommands = 0, totalValues = 0;
        foreach (var update in plan.Updates)
        {
            length += 10; totalCommands += update.CommandCount;
            foreach (var command in update.Commands)
            {
                totalValues += command.Values.Count;
                length += 1 + (command.Kind == CreatureWoundsCommandKind.Clear ? 0 : 2) + 4L * command.Values.Count;
            }
        }
        if (totalCommands > limits.MaximumCommands || totalValues > limits.MaximumValues || length > limits.MaximumPayloadBytes - 2)
            throw new InvalidDataException("Wounds encode budget exceeded.");
        byte[] fields = new byte[(int)length]; Writer writer = new(fields);
        foreach (var update in plan.Updates)
        {
            writer.U16(FieldIndex); writer.U32(update.CommandCount); writer.U32(update.Counter);
            foreach (var command in update.Commands)
            {
                writer.Byte((byte)command.Kind);
                if (command.Kind != CreatureWoundsCommandKind.Clear) writer.U16(command.Kind == CreatureWoundsCommandKind.Assign ? checked((ushort)command.Values.Count) : command.Index);
                foreach (int value in command.Values) writer.S32(value);
            }
        }
        return ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta, plan.ObjectId, CreatureView3BaselineCodec.RawTag, 3, plan.DeclaredCount, fields, limits.MaximumPayloadBytes), limits.MaximumPayloadBytes);
    }
    private ref struct Reader(ReadOnlySpan<byte> source)
    {
        private readonly ReadOnlySpan<byte> source = source; private int cursor = 0;
        public bool AtEnd => cursor == source.Length;
        public void Require(int count) { if (count < 0 || count > source.Length - cursor) throw new InvalidDataException("Truncated wounds delta."); }
        private ReadOnlySpan<byte> Bytes(int count) { Require(count); var result = source.Slice(cursor, count); cursor += count; return result; }
        public byte Byte() => Bytes(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
        public int S32() => BinaryPrimitives.ReadInt32LittleEndian(Bytes(4));
    }
    private ref struct Writer(Span<byte> target)
    {
        private readonly Span<byte> target = target; private int cursor = 0;
        private Span<byte> Bytes(int count) { var result = target.Slice(cursor, count); cursor += count; return result; }
        public void Byte(byte value) => Bytes(1)[0] = value;
        public void U16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Bytes(2), value);
        public void U32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Bytes(4), value);
        public void S32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Bytes(4), value);
    }
}
