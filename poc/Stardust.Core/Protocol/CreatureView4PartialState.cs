namespace Stardust.Core.Protocol;
public sealed record CreatureView4PartialLimits(int MaximumUpdates=256,int MaximumKnownFields=11,int MaximumEvents=256,
    long MaximumStateBytes=512,long MaximumOutputBytes=32768,long MaximumWork=8192,CreatureView4Limits? MessageLimits=null);
public enum CreatureView4PartialOrigin { ReceivedScalarDelta=1 }
public readonly record struct CreatureView4PartialProvenance(ulong Revision,ushort DeclaredCount,int Ordinal)
{public CreatureView4PartialOrigin Origin=>CreatureView4PartialOrigin.ReceivedScalarDelta;}
/// <summary>Only a received field; source revision is separate from the exact current-state receipt.</summary>
public sealed class CreatureView4PartialField
{
    internal CreatureView4PartialField(object owner,ulong objectId,ulong stateRevision,ushort index,ulong bits,CreatureView4PartialProvenance provenance)
    {Owner=owner;ObjectId=objectId;StateRevision=stateRevision;Index=index;Bits=bits;Provenance=provenance;}
    internal object Owner{get;}public ulong ObjectId{get;}public ulong StateRevision{get;}public ushort Index{get;}
    public ulong Bits{get;}public CreatureView4PartialProvenance Provenance{get;}
}
/// <summary>Every received assignment is described, including equal values; absent old data is null.</summary>
public readonly record struct CreatureView4PartialEvent(ushort Index,int Ordinal,bool OldWasKnown,ulong? OldBits,ulong NewBits,
    CreatureView4PartialProvenance? OldProvenance,CreatureView4PartialProvenance Provenance);
public sealed class CreatureView4PartialChange
{
    internal CreatureView4PartialChange(CreatureView4PartialState previous,CreatureView4PartialState current,ushort declared,
        CreatureView4PartialEvent[] events,long work)
    {Previous=previous;Current=current;DeclaredCount=declared;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureView4PartialState Previous{get;}public CreatureView4PartialState Current{get;}public ushort DeclaredCount{get;}
    public IReadOnlyList<CreatureView4PartialEvent> Events{get;}public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+64L*Events.Count;
}
public sealed class CreatureView4PartialState
{
    private readonly object identity=new();private readonly ulong[] bits;private readonly CreatureView4PartialProvenance?[] provenance;
    private readonly CreatureView4PartialLimits limits;
    private CreatureView4PartialState(ulong objectId,ulong revision,ushort mask,ulong[] bits,CreatureView4PartialProvenance?[] provenance,CreatureView4PartialLimits limits)
    {
        ObjectId=objectId;Revision=revision;KnownMask=mask;this.bits=bits;this.provenance=provenance;this.limits=limits;
        var fields=new List<CreatureView4PartialField>(11);
        for(ushort f=0;f<=12;f++){if(f is 2 or 3||(mask&(1<<f))==0)continue;int slot=Slot(f);fields.Add(new(identity,objectId,revision,f,bits[slot],provenance[slot]!.Value));}
        KnownFields=Array.AsReadOnly(fields.ToArray());
    }
    public ulong ObjectId{get;}public ulong Revision{get;}public ushort KnownMask{get;}
    public IReadOnlyList<CreatureView4PartialField> KnownFields{get;}public int KnownCount=>KnownFields.Count;
    public long DecodedBytes=>16+32L*KnownCount;
    private static int Slot(ushort index)=>index==6?10:CreatureView4Codec.FloatSlot(index);
    private static CreatureView4PartialLimits Validate(CreatureView4PartialLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);
        if(l.MaximumUpdates is <0 or >4096||l.MaximumKnownFields is <0 or >11||l.MaximumEvents is <0 or >4096||l.MaximumStateBytes is <0 or >16777216||l.MaximumOutputBytes is <0 or >33554432||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
        var message=l.MessageLimits??new();CreatureView4Codec.Validate(message);return l with{MessageLimits=message with{}};
    }
    private static void Capacity(int known,int events,long work,CreatureView4PartialLimits l)
    {
        long state=16+32L*known;
        if(known>l.MaximumKnownFields||state>l.MaximumStateBytes||events>l.MaximumEvents||state+64L*events>l.MaximumOutputBytes||work>l.MaximumWork)throw new InvalidDataException("CREO4 partial aggregate capacity");
    }
    /// <summary>Creates no observed/default values; revisionSeed is modern identity metadata only.</summary>
    public static CreatureView4PartialState Empty(ulong objectId,CreatureView4PartialLimits? limits=null,ulong revisionSeed=0)
    {var l=Validate(limits??new());Capacity(0,0,11,l);return new(objectId,revisionSeed,0,new ulong[11],new CreatureView4PartialProvenance?[11],l);}
    public bool IsKnown(ushort index){_=Slot(index);return(KnownMask&(1<<index))!=0;}
    public bool TryGetField(ushort index,out CreatureView4PartialField? field)
    {_=Slot(index);field=KnownFields.FirstOrDefault(f=>f.Index==index);return field is not null;}
    public bool IsCurrent(CreatureView4PartialField receipt)
    {ArgumentNullException.ThrowIfNull(receipt);return ReferenceEquals(receipt.Owner,identity)&&receipt.ObjectId==ObjectId&&receipt.StateRevision==Revision;}
    public CreatureView4PartialChange Apply(CreatureView4Delta plan,CreatureView4PartialLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);var l=Validate(operationLimits??limits);
        if(plan.ObjectId!=ObjectId||plan.Updates.Count==0||plan.Updates.Count>l.MaximumUpdates||plan.Updates.Count>l.MaximumEvents)throw new InvalidDataException("CREO4 partial recipient/update/event cap");
        // Frozen owned-plan measurement validates every scalar type/width and complete message budget.
        var message=CreatureView4Codec.MeasureDelta(plan,l.MessageLimits!);ulong next=checked(Revision+1);
        ushort mask=KnownMask;foreach(var u in plan.Updates){_=Slot(u.Index);mask|=(ushort)(1<<u.Index);}
        int known=System.Numerics.BitOperations.PopCount((uint)mask);long work=checked(22L+message.Work+2L*plan.Updates.Count+known);
        Capacity(KnownCount,0,11,l);Capacity(known,plan.Updates.Count,work,l);
        // No candidate arrays, provenance receipts or events exist before all full-plan checks above.
        var candidate=(ulong[])bits.Clone();var sources=(CreatureView4PartialProvenance?[])provenance.Clone();var events=new CreatureView4PartialEvent[plan.Updates.Count];
        for(int ordinal=0;ordinal<plan.Updates.Count;ordinal++)
        {
            var u=plan.Updates[ordinal];int slot=Slot(u.Index);bool old=sources[slot] is not null;var source=new CreatureView4PartialProvenance(next,plan.DeclaredCount,ordinal);
            events[ordinal]=new(u.Index,ordinal,old,old?candidate[slot]:null,u.Bits,sources[slot],source);candidate[slot]=u.Bits;sources[slot]=source;
        }
        return new(this,new(ObjectId,next,mask,candidate,sources,l),plan.DeclaredCount,events,work);
    }
}
