namespace Stardust.Core.Protocol;

/// <summary>Owned fixed-width common view3 fields. Names remain outside this slice.</summary>
public sealed class CreatureView3TangibleScalarUpdate
{
    private CreatureView3TangibleScalarUpdate(ushort index, float complexity, uint value, byte visibility)
    { Index = index; Complexity = complexity; Value = value; Visibility = visibility; }
    public ushort Index { get; }
    public float Complexity { get; }
    public uint Value { get; }
    public byte Visibility { get; }
    public static CreatureView3TangibleScalarUpdate FromComplexity(float value)
    {
        if (!float.IsFinite(value)) throw new InvalidDataException("Finite complexity required.");
        return new(0, value, 0, 0);
    }
    public static CreatureView3TangibleScalarUpdate FromUnsigned(ushort index, uint value)
    {
        if (index is not (3 or >= 6 and <= 9)) throw new InvalidDataException("Known common U32 field required.");
        return new(index, 0, value, 0);
    }
    public static CreatureView3TangibleScalarUpdate FromVisibility(byte value) => new(10, 0, 0, value);
}

public sealed class CreatureView3TangibleScalarState
{
    public CreatureView3TangibleScalarState(float complexity, uint volume, uint options, uint useCount,
        uint conditionDamage, uint maximumCondition, byte visibility)
    {
        if (!float.IsFinite(complexity)) throw new InvalidDataException("Finite complexity required.");
        Complexity = complexity; Volume = volume; Options = options; UseCount = useCount;
        ConditionDamage = conditionDamage; MaximumCondition = maximumCondition; Visibility = visibility;
    }
    public float Complexity { get; }
    public uint Volume { get; }
    public uint Options { get; }
    public uint UseCount { get; }
    public uint ConditionDamage { get; }
    public uint MaximumCondition { get; }
    public byte Visibility { get; }
    public static CreatureView3TangibleScalarState FromBaseline(CreatureView3BaselineSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return new(baseline.Complexity, baseline.Volume, baseline.Options, baseline.UseCount,
            baseline.ConditionDamage, baseline.MaximumCondition, baseline.Visibility);
    }
    public CreatureView3TangibleScalarState Apply(CreatureView3TangibleScalarUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return new(update.Index == 0 ? update.Complexity : Complexity, update.Index == 3 ? update.Value : Volume,
            update.Index == 6 ? update.Value : Options, update.Index == 7 ? update.Value : UseCount,
            update.Index == 8 ? update.Value : ConditionDamage, update.Index == 9 ? update.Value : MaximumCondition,
            update.Index == 10 ? update.Visibility : Visibility);
    }
    public static CreatureView3TangibleScalarState FromBaseline(TangibleView3BaselineSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return new(baseline.Complexity,baseline.Volume,baseline.Options,baseline.UseCount,
            baseline.ConditionDamage,baseline.MaximumCondition,baseline.Visibility);
    }
}
