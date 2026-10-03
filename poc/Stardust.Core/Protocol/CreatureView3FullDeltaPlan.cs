namespace Stardust.Core.Protocol;

/// <summary>Owned ordered union for CREO:3 indices4,5,11..17; no packed interpretation.</summary>
public sealed class CreatureView3FullDeltaUpdate
{
    private CreatureView3FullDeltaUpdate(ReplicationByteString? raw=null,CreatureView3ScalarUpdate? scalar=null,CreatureWoundsUpdate? wounds=null,CreatureComponentsUpdate? components=null)
    {Customization=raw;Scalar=scalar;Wounds=wounds;Components=components;}
    public ReplicationByteString? Customization {get;}
    public CreatureView3ScalarUpdate? Scalar {get;}
    public CreatureWoundsUpdate? Wounds {get;}
    public CreatureComponentsUpdate? Components {get;}
    public ushort Index=>Customization is not null?(ushort)4:Components is not null?(ushort)5:Scalar?.Index??17;
    public static CreatureView3FullDeltaUpdate FromCustomization(ReplicationByteString value)
    {ArgumentNullException.ThrowIfNull(value);return new(raw:value);}
    public static CreatureView3FullDeltaUpdate FromScalar(CreatureView3ScalarUpdate value)
    { _=CreatureView3CustomizationScalarUpdate.FromScalar(value);return new(scalar:value);}
    public static CreatureView3FullDeltaUpdate FromComponents(CreatureComponentsUpdate value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(value.Commands.Count>4096||value.Commands.Any(c=>c.Kind is not (CreatureComponentsCommandKind.Drop or CreatureComponentsCommandKind.Add or CreatureComponentsCommandKind.Clear)))throw new InvalidDataException("Known bounded component commands required.");
        return new(components:new(value.Counter,value.Commands.ToArray()));
    }
    public static CreatureView3FullDeltaUpdate FromWounds(CreatureWoundsUpdate value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(value.Commands.Count>4096||value.CommandCount>65536||value.Commands.Count>value.CommandCount)throw new InvalidDataException("Bounded wounds command count required.");
        long values=0;var owned=new CreatureWoundsCommand[value.Commands.Count];
        for(int i=0;i<owned.Length;i++)
        {
            var c=value.Commands[i];if(c is null)throw new InvalidDataException("Null wounds command.");
            bool valid=c.Kind switch {CreatureWoundsCommandKind.Remove or CreatureWoundsCommandKind.Clear=>c.Values.Count==0,CreatureWoundsCommandKind.Insert or CreatureWoundsCommandKind.Replace=>c.Values.Count==1,CreatureWoundsCommandKind.Assign=>c.Values.Count<=65535,_=>false};
            if(!valid||(values+=c.Values.Count)>65536)throw new InvalidDataException("Known bounded wounds values required.");
            owned[i]=new(c.Kind,c.Index,c.Values.ToArray());
        }
        return new(wounds:new(value.Counter,owned,value.CommandCount));
    }
}
public sealed class CreatureView3FullDeltaPlan
{
    public CreatureView3FullDeltaPlan(ulong objectId,ushort declaredCount,IReadOnlyList<CreatureView3FullDeltaUpdate> updates,
        CreatureWoundsApplicationPolicy woundsPolicy,int maximumWoundsElements,int maximumComponentsElements)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if(updates.Count is <1 or >4096||woundsPolicy is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded)||maximumWoundsElements is <0 or >65536||maximumComponentsElements is <0 or >65536)throw new InvalidDataException("Bounded full creature plan policy required.");
        var owned=updates.ToArray();long commands=0,values=0;
        foreach(var u in owned)
        {
            if(u is null)throw new InvalidDataException("Null full creature update.");
            commands+=u.Components?.Commands.Count??0;commands+=u.Wounds?.CommandCount??0;
            if(u.Wounds is {} w)foreach(var c in w.Commands)values+=c.Values.Count;
            if(commands>65536||values>65536)throw new InvalidDataException("Full plan aggregate command/value budget.");
        }
        ObjectId=objectId;DeclaredCount=declaredCount;Updates=Array.AsReadOnly(owned);WoundsPolicy=woundsPolicy;MaximumWoundsElements=maximumWoundsElements;MaximumComponentsElements=maximumComponentsElements;
    }
    public ulong ObjectId {get;}
    public ushort DeclaredCount {get;}
    public IReadOnlyList<CreatureView3FullDeltaUpdate> Updates {get;}
    public CreatureWoundsApplicationPolicy WoundsPolicy {get;}
    public int MaximumWoundsElements {get;}
    public int MaximumComponentsElements {get;}
}
