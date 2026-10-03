namespace Stardust.Core.Protocol;

/// <summary>Owned ordered union for the bounded CREO:3 field4 + indices11..16 slice.</summary>
public sealed class CreatureView3CustomizationScalarUpdate
{
    private CreatureView3CustomizationScalarUpdate(ReplicationByteString? customization,CreatureView3ScalarUpdate? scalar)
    { Customization=customization;Scalar=scalar; }
    public ReplicationByteString? Customization {get;}
    public CreatureView3ScalarUpdate? Scalar {get;}
    public ushort Index=>Customization is not null?(ushort)4:Scalar!.Index;
    public static CreatureView3CustomizationScalarUpdate FromCustomization(ReplicationByteString value)
    {ArgumentNullException.ThrowIfNull(value);return new(value,null);}
    public static CreatureView3CustomizationScalarUpdate FromScalar(CreatureView3ScalarUpdate value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(value is not (CreaturePostureUpdate or CreatureRankUpdate or CreatureLinkUpdate or CreatureHeightUpdate or CreatureShockUpdate or CreatureStatesUpdate) || value is CreatureHeightUpdate height&&!float.IsFinite(height.Value))
            throw new InvalidDataException("Known finite creature scalar required.");
        return new(null,value);
    }
}
public sealed class CreatureView3CustomizationScalarDeltaPlan
{
    public CreatureView3CustomizationScalarDeltaPlan(ulong objectId,ushort declaredCount,IReadOnlyList<CreatureView3CustomizationScalarUpdate> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if(updates.Count is <1 or >4096)throw new InvalidDataException("Customization/scalar plan update budget.");
        var owned=updates.ToArray();if(owned.Any(u=>u is null))throw new InvalidDataException("Null customization/scalar update.");
        ObjectId=objectId;DeclaredCount=declaredCount;Updates=Array.AsReadOnly(owned);
    }
    public ulong ObjectId {get;}
    public ushort DeclaredCount {get;}
    public IReadOnlyList<CreatureView3CustomizationScalarUpdate> Updates {get;}
}
