namespace Stardust.Core.Protocol;

/// <summary>Owned historic TANO3 baseline. Raw container order/duplicates stay in this snapshot.</summary>
public sealed class TangibleView3BaselineSnapshot
{
    internal TangibleView3BaselineSnapshot() { }
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
    public long DecodedBytes { get; internal init; }
    internal int FieldWireLength { get; init; }
    /// <summary>Explicit common-field projection of an owned CREO snapshot, not TANO wire decoding.</summary>
    public static TangibleView3BaselineSnapshot FromCreatureCommonFields(CreatureView3BaselineSnapshot value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() {ObjectId=value.ObjectId,DeclaredCount=value.DeclaredCount,Complexity=value.Complexity,
            NameFile=value.NameFile,NameSpacer=value.NameSpacer,NameKey=value.NameKey,CustomNameUnits=value.CustomNameUnits,
            Volume=value.Volume,Customization=value.Customization,Components=value.Components,Options=value.Options,
            UseCount=value.UseCount,ConditionDamage=value.ConditionDamage,MaximumCondition=value.MaximumCondition,Visibility=value.Visibility,
            DecodedBytes=checked(value.DecodedBytes-34-4L*value.Wounds.Values.Count),
            FieldWireLength=checked(value.FieldWireLength-34-4*value.Wounds.Values.Count)};
    }
}

/// <summary>Owned common view3 union, restricted to TANO indices0..10; no creature extension.</summary>
public sealed class TangibleView3DeltaPlan
{
    public TangibleView3DeltaPlan(ulong objectId, ushort declaredCount,
        IReadOnlyList<CreatureView3NamedDeltaUpdate> updates, int maximumComponentsElements)
    {
        ArgumentNullException.ThrowIfNull(updates);
        var owned=new CreatureView3NamedDeltaPlan(objectId,declaredCount,updates,
            CreatureWoundsApplicationPolicy.StrictInterval,0,maximumComponentsElements);
        if(owned.Updates.Any(u=>u.Index>10))throw new InvalidDataException("TANO3 common fields only.");
        ObjectId=objectId;DeclaredCount=declaredCount;Updates=owned.Updates;
        MaximumComponentsElements=maximumComponentsElements;
    }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureView3NamedDeltaUpdate> Updates { get; }
    public int MaximumComponentsElements { get; }
}
