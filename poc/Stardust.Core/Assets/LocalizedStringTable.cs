using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record LocalizedStringTableLimits(int MaximumBytes = 16777216, int MaximumRecords = 65536,
    int MaximumValueUnits = 65536, int MaximumKeyBytes = 4096,
    int MaximumTotalValueUnits = 4194304, int MaximumTotalKeyBytes = 4194304);
public sealed record LocalizedStringTableHeader(ushort Magic, ushort ReservedRaw, byte Variant,
    uint MaxNextIdentityRaw, uint RecordCount);

/// <summary>Owned exact narrow bytes, with no assumed character encoding or NUL semantics.</summary>
public sealed class LocalizedStringKey : IEquatable<LocalizedStringKey>
{
    private readonly byte[] bytes; private readonly int hash;
    public LocalizedStringKey(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 65536) throw new ArgumentOutOfRangeException(nameof(bytes));
        this.bytes = bytes.ToArray(); uint h = 2166136261;
        foreach (byte value in this.bytes) h = unchecked((h ^ value) * 16777619); hash = unchecked((int)h);
    }
    public int Length => bytes.Length;
    public byte[] CopyBytes() => (byte[])bytes.Clone();
    public bool Equals(LocalizedStringKey? other) => other is not null && bytes.AsSpan().SequenceEqual(other.bytes);
    public override bool Equals(object? other) => other is LocalizedStringKey key && Equals(key);
    public override int GetHashCode() => hash;
    public string GetDisplayKey()
    {
        var result = new StringBuilder();
        foreach (byte value in bytes)
            if (value == 92) result.Append("\\\\");
            else if (value is >= 32 and <= 126) result.Append((char)value);
            else result.Append("\\x").Append(value.ToString("X2", CultureInfo.InvariantCulture));
        return result.ToString();
    }
}

public sealed class LocalizedStringRecord
{
    private readonly ushort[] units;
    internal LocalizedStringRecord(uint id, uint valueCrcRaw, LocalizedStringKey key, ushort[] units)
    { Id = id; ValueCrcRaw = valueCrcRaw; Key = key; this.units = units; Utf16Units = Array.AsReadOnly(units); }
    public uint Id { get; }
    public uint ValueCrcRaw { get; }
    public LocalizedStringKey Key { get; }
    public IReadOnlyList<ushort> Utf16Units { get; }
    public ushort[] CopyUtf16Units() => (ushort[])units.Clone();
    /// <summary>Display only: replace unpaired surrogates, escape controls/backslashes; raw units remain unchanged.</summary>
    public string GetDisplayText()
    {
        var result = new StringBuilder();
        for (int i = 0; i < units.Length; i++)
        {
            char c = (char)units[i];
            if (char.IsHighSurrogate(c) && i + 1 < units.Length && char.IsLowSurrogate((char)units[i + 1]))
            { result.Append(c).Append((char)units[++i]); }
            else if (char.IsSurrogate(c)) result.Append('\uFFFD');
            else if (c == '\\') result.Append("\\\\");
            else if (char.IsControl(c)) result.Append("\\u").Append(units[i].ToString("X4", CultureInfo.InvariantCulture));
            else result.Append(c);
        }
        return result.ToString();
    }
}

/// <summary>Owned bounded STF variant1, two-pass exact ID/value/key relation; variant0 remains unsupported.</summary>
public sealed class LocalizedStringTable
{
    private LocalizedStringTable(LocalizedStringTableHeader header, List<LocalizedStringRecord> records,
        Dictionary<uint, LocalizedStringRecord> byId, Dictionary<LocalizedStringKey, LocalizedStringRecord> byKey,
        uint[] keyPassIds, long totalUnits, long totalKeyBytes)
    {
        Header = header; Records = records.AsReadOnly(); ById = new ReadOnlyDictionary<uint, LocalizedStringRecord>(byId);
        ByKey = new ReadOnlyDictionary<LocalizedStringKey, LocalizedStringRecord>(byKey); KeyPassIds = Array.AsReadOnly(keyPassIds);
        TotalValueUnits = totalUnits; TotalKeyBytes = totalKeyBytes;
    }
    public LocalizedStringTableHeader Header { get; }
    public IReadOnlyList<LocalizedStringRecord> Records { get; }
    public IReadOnlyDictionary<uint, LocalizedStringRecord> ById { get; }
    public IReadOnlyDictionary<LocalizedStringKey, LocalizedStringRecord> ByKey { get; }
    public IReadOnlyList<uint> KeyPassIds { get; }
    public long TotalValueUnits { get; }
    public long TotalKeyBytes { get; }
    public bool TryGetByKey(ReadOnlySpan<byte> key, out LocalizedStringRecord? record) => ByKey.TryGetValue(new(key), out record);

    public static LocalizedStringTable Load(ReadOnlySpan<byte> source, LocalizedStringTableLimits? limits = null)
    {
        limits ??= new();
        if (limits.MaximumBytes is < 13 or > 67108864 || limits.MaximumRecords is < 0 or > 1000000 ||
            limits.MaximumValueUnits is < 0 or > 1000000 || limits.MaximumKeyBytes is < 0 or > 65536 ||
            limits.MaximumTotalValueUnits is < 0 or > 16777216 || limits.MaximumTotalKeyBytes is < 0 or > 16777216)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Need(source.Length >= 13 && source.Length <= limits.MaximumBytes, "STF source size/header rejected.");
        byte[] owned = source.ToArray(); var reader = new Reader(owned);
        ushort magic = reader.U16(), reserved = reader.U16(); byte variant = reader.U8(); uint next = reader.U32(), count = reader.U32();
        Need(magic == 0xABCD && variant == 1, "Unsupported STF magic/variant (only variant1 is verified).");
        Need(count <= limits.MaximumRecords && (long)count * 20 <= reader.Remaining, "STF record count/minimum extents rejected.");
        var values = new Dictionary<uint, (uint Crc, ushort[] Units)>(); var valueOrder = new List<uint>(); long totalUnits = 0;
        for (uint i = 0; i < count; i++)
        {
            uint id = reader.U32(), crc = reader.U32(), length = reader.U32();
            totalUnits = checked(totalUnits + length);
            Need(length <= limits.MaximumValueUnits && totalUnits <= limits.MaximumTotalValueUnits && length <= reader.Remaining / 2,
                "STF value unit length/aggregate/truncation rejected.");
            Need(!values.ContainsKey(id), "Duplicate STF value identity.");
            var bytes = reader.Bytes(checked((int)length * 2)); var units = new ushort[(int)length];
            for (int j = 0; j < units.Length; j++) units[j] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(j * 2, 2));
            values.Add(id, (crc, units)); valueOrder.Add(id);
        }
        var keys = new Dictionary<uint, LocalizedStringKey>(); var uniqueKeys = new HashSet<LocalizedStringKey>(); var keyPass = new uint[(int)count]; long totalKeys = 0;
        for (int i = 0; i < keyPass.Length; i++)
        {
            uint id = reader.U32(), length = reader.U32(); totalKeys = checked(totalKeys + length);
            Need(length <= limits.MaximumKeyBytes && totalKeys <= limits.MaximumTotalKeyBytes && length <= reader.Remaining,
                "STF narrow key length/aggregate/truncation rejected.");
            Need(values.ContainsKey(id) && !keys.ContainsKey(id), "Orphan/duplicate STF key identity.");
            var key = new LocalizedStringKey(reader.Bytes((int)length)); Need(uniqueKeys.Add(key), "Duplicate exact STF key bytes.");
            keys.Add(id, key); keyPass[i] = id;
        }
        Need(reader.Remaining == 0 && keys.Count == values.Count, "STF trailing bytes/orphan values rejected.");
        var records = new List<LocalizedStringRecord>(); var byId = new Dictionary<uint, LocalizedStringRecord>(); var byKey = new Dictionary<LocalizedStringKey, LocalizedStringRecord>();
        foreach (uint id in valueOrder)
        { var value = values[id]; var record = new LocalizedStringRecord(id, value.Crc, keys[id], value.Units); records.Add(record); byId.Add(id, record); byKey.Add(record.Key, record); }
        return new(new(magic, reserved, variant, next, count), records, byId, byKey, keyPass, totalUnits, totalKeys);
    }
    private ref struct Reader(ReadOnlySpan<byte> source)
    {
        private readonly ReadOnlySpan<byte> source = source; private int cursor;
        public int Remaining => source.Length - cursor;
        public ReadOnlySpan<byte> Bytes(int count) { Need(count >= 0 && count <= Remaining, "Truncated STF field."); var result = source.Slice(cursor, count); cursor += count; return result; }
        public byte U8() => Bytes(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
    }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
