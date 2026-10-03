namespace Stardust.Core.Protocol;

public sealed record PackedCustomizationLimits(int MaximumEncodedBytes = 1536, int MaximumRecords = 255, int MaximumDecodedBytes = 767);

/// <summary>Owned raw value octets; width is independent of namespace or factory flag.</summary>
public sealed class PackedCustomizationRecord
{
    private readonly byte[] value;
    public PackedCustomizationRecord(byte id, ReadOnlySpan<byte> rawValue)
    {
        if (id is < 1 or > 127) throw new ArgumentOutOfRangeException(nameof(id));
        if (rawValue.Length is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(rawValue));
        Id = id; value = rawValue.ToArray();
    }
    public byte Id { get; }
    public int ValueWidth => value.Length;
    public byte[] CopyRawValue() => (byte[])value.Clone();
    internal ReadOnlySpan<byte> RawValue => value;
}

/// <summary>Immutable ordered records, retaining duplicates and the outer-empty distinction.</summary>
public sealed class PackedCustomizationSnapshot
{
    private readonly byte[]? original;
    public PackedCustomizationSnapshot(bool isOuterEmpty, IReadOnlyList<PackedCustomizationRecord> records)
        : this(isOuterEmpty, records, null) { }
    internal PackedCustomizationSnapshot(bool isOuterEmpty, IReadOnlyList<PackedCustomizationRecord> records, byte[]? original)
    {
        ArgumentNullException.ThrowIfNull(records);
        int count = records.Count;
        if (count is < 0 or > 255 || isOuterEmpty && count != 0) throw new ArgumentOutOfRangeException(nameof(records));
        var owned = new PackedCustomizationRecord[count];
        for (int i = 0; i < count; i++)
            owned[i] = records[i] ?? throw new ArgumentException("Null customization record.", nameof(records));
        IsOuterEmpty = isOuterEmpty; Records = Array.AsReadOnly(owned);
        DecodedByteCount = isOuterEmpty ? 0 : 2 + owned.Sum(r => 1 + r.ValueWidth);
        this.original = original;
    }
    public bool IsOuterEmpty { get; }
    public byte? Version => IsOuterEmpty ? null : (byte)1;
    public IReadOnlyList<PackedCustomizationRecord> Records { get; }
    public int DecodedByteCount { get; }
    internal ReadOnlySpan<byte> Original => original;
    internal bool HasOriginal => original is not null;
}

/// <summary>Bounded atomic raw v1 codec; strict final EOS is explicit replacement policy.</summary>
public static class PackedCustomizationCodec
{
    private static void Validate(PackedCustomizationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (limits.MaximumEncodedBytes is < 0 or > 1048576 || limits.MaximumRecords is < 0 or > 255 || limits.MaximumDecodedBytes is < 0 or > 767)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    public static PackedCustomizationSnapshot Decode(ReadOnlySpan<byte> source, PackedCustomizationLimits? limits = null)
    {
        var policy = limits ?? new(); Validate(policy);
        if (source.Length > policy.MaximumEncodedBytes) throw new InvalidDataException("Packed customization byte budget.");
        if (source.IsEmpty) return new(true, Array.Empty<PackedCustomizationRecord>(), Array.Empty<byte>());
        Reader reader = new(source, policy.MaximumDecodedBytes);
        if (reader.Read() != 1) throw new InvalidDataException("Unsupported packed customization version.");
        int count = reader.Read();
        if (count > policy.MaximumRecords) throw new InvalidDataException("Packed customization record budget.");
        var records = new PackedCustomizationRecord[count];
        for (int i = 0; i < count; i++)
        {
            byte tag = reader.Read(), id = (byte)(tag & 127);
            if (id == 0) throw new InvalidDataException("Packed customization ID zero.");
            byte low = reader.Read();
            records[i] = (tag & 128) == 0 ? new(id, new[] { low }) : new(id, new[] { low, reader.Read() });
        }
        reader.End();
        // Nothing is published until all fields, budgets and the exact tail pass.
        return new(false, records, source.ToArray());
    }
    public static byte[] Encode(PackedCustomizationSnapshot snapshot, PackedCustomizationLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot); var policy = limits ?? new(); Validate(policy);
        if (snapshot.Records.Count > policy.MaximumRecords || snapshot.DecodedByteCount > policy.MaximumDecodedBytes)
            throw new InvalidDataException("Packed customization decoded budget.");
        if (snapshot.HasOriginal)
        {
            if (snapshot.Original.Length > policy.MaximumEncodedBytes) throw new InvalidDataException("Packed customization byte budget.");
            return snapshot.Original.ToArray();
        }
        if (snapshot.IsOuterEmpty) return Array.Empty<byte>();
        static int Size(byte b) => b is 0 or 255 ? 2 : 1;
        int size = Size(1) + Size((byte)snapshot.Records.Count) + 2;
        foreach (var r in snapshot.Records)
        {
            size += Size((byte)(r.Id | (r.ValueWidth == 2 ? 128 : 0)));
            foreach (byte b in r.RawValue) size += Size(b);
        }
        if (size > policy.MaximumEncodedBytes) throw new InvalidDataException("Packed customization byte budget.");
        byte[] result = new byte[size]; int cursor = 0;
        void Write(byte b)
        {
            if (b is 0 or 255) { result[cursor++] = 255; result[cursor++] = b == 0 ? (byte)1 : (byte)2; }
            else result[cursor++] = b;
        }
        Write(1); Write((byte)snapshot.Records.Count);
        foreach (var r in snapshot.Records)
        {
            Write((byte)(r.Id | (r.ValueWidth == 2 ? 128 : 0)));
            foreach (byte b in r.RawValue) Write(b);
        }
        result[cursor++] = 255; result[cursor] = 3; return result;
    }
    private ref struct Reader(ReadOnlySpan<byte> source, int maximumDecoded)
    {
        private readonly ReadOnlySpan<byte> source = source;
        private readonly int maximumDecoded = maximumDecoded;
        private int cursor, decoded;
        public byte Read()
        {
            if (decoded >= maximumDecoded) throw new InvalidDataException("Packed customization decoded byte budget.");
            if (cursor >= source.Length) throw new InvalidDataException("Truncated packed customization.");
            byte value = source[cursor++];
            if (value == 255)
            {
                if (cursor >= source.Length) throw new InvalidDataException("Truncated packed escape.");
                value = source[cursor++] switch { 1 => (byte)0, 2 => (byte)255, _ => throw new InvalidDataException("Invalid or premature packed escape/EOS.") };
            }
            decoded++; return value;
        }
        public void End()
        {
            if (source.Length - cursor != 2 || source[cursor] != 255 || source[cursor + 1] != 3)
                throw new InvalidDataException("Packed customization requires final exact EOS.");
        }
    }
}
