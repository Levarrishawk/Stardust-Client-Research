namespace Stardust.Core.Protocol;
public sealed record CreatureView4StateLimits(int MaximumUpdates=4096,int MaximumElements=65536,int MaximumTotalElements=196608,int MaximumStringBytes=65536,int MaximumTotalStringBytes=262144,long MaximumStateBytes=2097152,int MaximumEvents=4096,long MaximumOutputBytes=4194304,long MaximumWork=1048576);
public readonly record struct CreatureView4Event(ushort Index,int Ordinal,ulong OldBits,ulong NewBits);
public sealed class CreatureView4Change
{
    internal CreatureView4Change(CreatureView4State current,ushort count,CreatureView4Event[] events,long work){Current=current;DeclaredCount=count;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureView4State Current{get;} public ushort DeclaredCount{get;} public IReadOnlyList<CreatureView4Event> Events{get;} public long WorkUsed{get;} public long DecodedOutputBytes=>Current.DecodedBytes+24L*Events.Count;
}
public sealed class CreatureView4State
{
    private readonly CreatureView4StateLimits limits;
    private CreatureView4State(CreatureView4Baseline model,ulong revision,long bytes,CreatureView4StateLimits limits){Model=model;Revision=revision;DecodedBytes=bytes;this.limits=limits;}
    public CreatureView4Baseline Model{get;} public ulong ObjectId=>Model.ObjectId;public ulong Revision{get;}public long DecodedBytes{get;}
    private static CreatureView4StateLimits Validate(CreatureView4StateLimits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumUpdates is <0 or >4096||l.MaximumElements is <0 or >65536||l.MaximumTotalElements is <0 or >196608||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumStateBytes is <0 or >16777216||l.MaximumEvents is <0 or >262144||l.MaximumOutputBytes is <0 or >33554432||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));return l with{};}
    private static (long Bytes,long SeedWork) Measure(CreatureView4Baseline m,CreatureView4StateLimits l)
    {
        long elements=m.Encumbrances.Values.Count+(long)m.SkillMods.Values.Count+m.SpaceMissionObjects.Values.Count,strings=0,bytes=72+4L*m.Encumbrances.Values.Count+16L*m.SpaceMissionObjects.Values.Count;
        if(m.Encumbrances.Values.Count>l.MaximumElements||m.SkillMods.Values.Count>l.MaximumElements||m.SpaceMissionObjects.Values.Count>l.MaximumElements||elements>l.MaximumTotalElements)throw new InvalidDataException("CREO4 state collection cap");
        foreach(var v in m.SkillMods.Values){if(v.Name.Length>l.MaximumStringBytes)throw new InvalidDataException("CREO4 state string cap");strings+=v.Name.Length;bytes+=9+(v.Name.UsesExtendedLength?6:2)+(long)v.Name.Length;}
        if(strings>l.MaximumTotalStringBytes||bytes>l.MaximumStateBytes)throw new InvalidDataException("CREO4 global state cap");return(bytes,14+elements+strings);
    }
    public static CreatureView4State FromBaseline(CreatureView4Baseline baseline,CreatureView4StateLimits? limits=null,ulong revisionSeed=0)
    {ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);var cost=Measure(baseline,l);if(cost.SeedWork>l.MaximumWork||cost.Bytes>l.MaximumOutputBytes)throw new InvalidDataException("CREO4 seed work/output cap");return new(baseline,revision,cost.Bytes,l);}
    public CreatureView4State Reseed(CreatureView4Baseline baseline){ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("CREO4 reseed recipient");return FromBaseline(baseline,limits,Revision);}
    public CreatureView4Change Apply(CreatureView4Delta plan,CreatureView4StateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);var l=Validate(operationLimits??limits);if(plan.ObjectId!=ObjectId||plan.Updates.Count==0||plan.Updates.Count>l.MaximumUpdates||plan.Updates.Count>l.MaximumEvents)throw new InvalidDataException("CREO4 recipient/update/event cap");
        // Preflight the complete plan before constructing any candidate.
        _=CreatureView4Codec.MeasureDelta(plan,new(8388608,4096,65536,196608,1048576,4194304,16777216,16777216));ulong revision=checked(Revision+1);var cost=Measure(Model,l);long work=cost.SeedWork;
        if(cost.Bytes>l.MaximumOutputBytes||work>l.MaximumWork)throw new InvalidDataException("CREO4 initial state/output/work cap");
        long completeWork=checked(work+plan.Updates.Count*(1+cost.SeedWork));if(completeWork>l.MaximumWork||cost.Bytes+24L*plan.Updates.Count>l.MaximumOutputBytes)throw new InvalidDataException("CREO4 whole-plan work/output cap");
        var floats=Model.FloatBits.ToArray();ulong listen=Model.ListenTo;var events=new CreatureView4Event[plan.Updates.Count];
        for(int n=0;n<plan.Updates.Count;n++){var u=plan.Updates[n];ulong old;if(u.Index==6){old=listen;listen=u.Bits;}else{int slot=CreatureView4Codec.FloatSlot(u.Index);old=floats[slot];floats[slot]=(uint)u.Bits;}events[n]=new(u.Index,n,old,u.Bits);}
        var model=new CreatureView4Baseline(ObjectId,Model.DeclaredCount,floats,listen,Model.Encumbrances,Model.SkillMods,Model.SpaceMissionObjects);return new(new(model,revision,cost.Bytes,l),plan.DeclaredCount,events,completeWork);
    }
}
