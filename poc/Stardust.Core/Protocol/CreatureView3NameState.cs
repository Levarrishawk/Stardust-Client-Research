namespace Stardust.Core.Protocol;

/// <summary>Owned raw name update; display/path interpretation is separate.</summary>
public sealed class CreatureView3NameUpdate
{
    private CreatureView3NameUpdate(ushort index, ReplicationByteString? file, uint spacer,
        ReplicationByteString? key, IReadOnlyList<ushort>? units)
    { Index = index; NameFile = file; NameSpacer = spacer; NameKey = key; CustomNameUnits = units; }
    public ushort Index { get; }
    public ReplicationByteString? NameFile { get; }
    public uint NameSpacer { get; }
    public ReplicationByteString? NameKey { get; }
    public IReadOnlyList<ushort>? CustomNameUnits { get; }
    public long DecodedBytes => Index == 1 ? NameFile!.Length + 4L + NameKey!.Length : 2L * CustomNameUnits!.Count;
    public static CreatureView3NameUpdate FromStringId(ReplicationByteString file, uint spacer, ReplicationByteString key)
    {
        ArgumentNullException.ThrowIfNull(file); ArgumentNullException.ThrowIfNull(key);
        if (file.Length > 65536 || key.Length > 65536) throw new InvalidDataException("Bounded name strings required.");
        return new(1, file, spacer, key, null);
    }
    public static CreatureView3NameUpdate FromCustomName(ReadOnlySpan<ushort> units)
    {
        if (units.Length > 65536) throw new InvalidDataException("Bounded name units required.");
        return new(2, null, 0, null, Array.AsReadOnly(units.ToArray()));
    }
}

public sealed class CreatureView3NameState
{
    private CreatureView3NameState(ReplicationByteString file, uint spacer, ReplicationByteString key, IReadOnlyList<ushort> units)
    { NameFile = file; NameSpacer = spacer; NameKey = key; CustomNameUnits = units; }
    public ReplicationByteString NameFile { get; }
    public uint NameSpacer { get; }
    public ReplicationByteString NameKey { get; }
    public IReadOnlyList<ushort> CustomNameUnits { get; }
    public static CreatureView3NameState FromBaseline(CreatureView3BaselineSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        // Baseline fields are already owned/immutable. Sharing preserves existing larger baseline limits.
        return new(baseline.NameFile, baseline.NameSpacer, baseline.NameKey, baseline.CustomNameUnits);
    }
    public CreatureView3NameState Apply(CreatureView3NameUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return update.Index == 1 ? new(update.NameFile!, update.NameSpacer, update.NameKey!, CustomNameUnits)
            : new(NameFile, NameSpacer, NameKey, update.CustomNameUnits!);
    }
    public static CreatureView3NameState FromBaseline(TangibleView3BaselineSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return new(baseline.NameFile,baseline.NameSpacer,baseline.NameKey,baseline.CustomNameUnits);
    }
    public long AdditionalDecodedBytes(TangibleView3BaselineSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return checked((ReferenceEquals(NameFile,baseline.NameFile)?0L:NameFile.Length)+
            (ReferenceEquals(NameKey,baseline.NameKey)?0L:NameKey.Length)+
            (ReferenceEquals(CustomNameUnits,baseline.CustomNameUnits)?0L:2L*CustomNameUnits.Count));
    }
    /// <summary>Variable payload retained in addition to historical baseline; reference aliases count once.</summary>
    public long AdditionalDecodedBytes(CreatureView3BaselineSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return checked((ReferenceEquals(NameFile, baseline.NameFile) ? 0L : NameFile.Length) +
            (ReferenceEquals(NameKey, baseline.NameKey) ? 0L : NameKey.Length) +
            (ReferenceEquals(CustomNameUnits, baseline.CustomNameUnits) ? 0L : 2L * CustomNameUnits.Count));
    }
}
