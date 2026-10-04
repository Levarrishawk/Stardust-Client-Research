namespace Stardust.Core.Protocol;
public sealed record CreatureView6CompositeStateLimits(CreatureView6StateLimits? ModelLimits=null,CreatureView6CompositeLimits? MessageLimits=null);
public enum CreatureView6CompositeEventKind{Assignment,HamMutation}
public readonly record struct CreatureView6CompositeEvent(CreatureView6CompositeEventKind Kind,ushort Field,int UpdateOrdinal,byte? Opcode,ushort? ElementIndex,int? OldValue,int? NewValue);
public sealed class CreatureView6CompositeChange
{
    internal CreatureView6CompositeChange(CreatureView6CompositeState current,ushort metadata,CreatureView6CompositeEvent[] events,long work)
    {Current=current;DeclaredCount=metadata;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureView6CompositeState Current{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureView6CompositeEvent> Events{get;}public long WorkUsed{get;}public long DecodedOutputBytes=>Current.DecodedBytes+24L*Events.Count;
}
public sealed class CreatureView6CompositeState
{
    private readonly CreatureView6CompositeStateLimits limits;
    private CreatureView6CompositeState(CreatureView6Baseline model,ulong revision,long bytes,CreatureView6CompositeStateLimits limits){Model=model;Revision=revision;DecodedBytes=bytes;this.limits=limits;}
    public CreatureView6Baseline Model{get;}public ulong ObjectId=>Model.ObjectId;public ulong Revision{get;}public long DecodedBytes{get;}
    private static CreatureView6CompositeStateLimits Validate(CreatureView6CompositeStateLimits l)
    {ArgumentNullException.ThrowIfNull(l);var model=l.ModelLimits??new();var message=l.MessageLimits??new();CreatureView6State.Validate(model);CreatureView6CompositeCodec.Validate(message);return new(model with{},message with{});}
    public static CreatureView6CompositeState FromBaseline(CreatureView6Baseline baseline,CreatureView6CompositeStateLimits? limits=null,ulong revisionSeed=0)
    {ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());var seed=CreatureView6State.FromBaseline(baseline,l.ModelLimits,revisionSeed);return new(seed.Model,seed.Revision,seed.DecodedBytes,l);}
    public CreatureView6CompositeState Reseed(CreatureView6Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("Composite reseed recipient");return FromBaseline(baseline,limits,Revision);}
    private static CreatureView6Baseline WithHam(CreatureView6Baseline m,CreatureHamView6State h)=>new(m.ObjectId,m.DeclaredCount,m.Passive,m.Defenders,m.Level,m.PerformanceAnimation,m.MoodString,m.WeaponId,m.GroupId,m.InviterId,m.InviteCounter,m.Guild,m.TargetId,m.MoodId,m.PerformanceStart,m.PerformanceType,h.Ham,h.MaximumHam,m.Wearables,m.AlternateAppearance,m.Frozen);
    public CreatureView6CompositeChange Apply(CreatureView6CompositeDelta plan,CreatureView6CompositeStateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);var l=Validate(operationLimits??limits);var ml=l.ModelLimits!;var cl=l.MessageLimits!;
        if(plan.ObjectId!=ObjectId||plan.Updates.Count>ml.MaximumUpdates)throw new InvalidDataException("Composite recipient/update count");_=CreatureView6CompositeCodec.Measure(plan,cl);ulong revision=checked(Revision+1);
        // Validate the initial full model under a replacement policy; scan overhead is independently bounded.
        var seed=CreatureView6State.FromBaseline(Model,ml with{MaximumWork=16777216});var current=Model;long bytes=seed.DecodedBytes,work=0;var events=new List<CreatureView6CompositeEvent>();CreatureHamView6State? ham=null;
        void Charge(long n){work=checked(work+n);if(work>ml.MaximumWork)throw new InvalidDataException("Composite state work");}
        long RemainingOutput()=>ml.MaximumOutputBytes-24L*events.Count;
        CreatureHamView6StateLimits HamLimits()
        {
            int others=current.Defenders.Values.Count+current.Wearables.Values.Count;long hamBytes=16+4L*(current.Ham.Values.Count+current.MaximumHam.Values.Count);long otherBytes=bytes-hamBytes;
            long stateRoom=ml.MaximumStateBytes-otherBytes,outputRoom=RemainingOutput()-otherBytes;
            if(stateRoom<16||outputRoom<16||ml.MaximumTotalElements<others||ml.MaximumEvents<events.Count)throw new InvalidDataException("Composite HAM reserved capacity");
            return new(ml.MaximumElements,Math.Min(131072,ml.MaximumTotalElements-others),stateRoom,ml.MaximumEvents-events.Count,outputRoom,ml.MaximumWork-work,
                new(cl.MaximumPayloadBytes,cl.MaximumFields,cl.MaximumCommands,cl.MaximumValues,cl.MaximumDecodedBytes));
        }
        if(plan.Updates.Any(u=>u.Ham is not null)){Charge(current.Ham.Values.Count+(long)current.MaximumHam.Values.Count);ham=CreatureHamView6State.FromBaseline(current,HamLimits() with{MaximumWork=16777216});}
        for(int ordinal=0;ordinal<plan.Updates.Count;ordinal++)
        {
            var u=plan.Updates[ordinal];
            if(u.Scalar is {} scalar)
            {
                if(events.Count>=ml.MaximumEvents||RemainingOutput()<16)throw new InvalidDataException("Composite assignment event/output capacity");
                var previous=CreatureView6State.FromBaseline(current,ml with{MaximumWork=16777216,MaximumOutputBytes=RemainingOutput()});
                var scalarLimits=ml with{MaximumUpdates=1,MaximumEvents=1,MaximumOutputBytes=RemainingOutput()-16,MaximumWork=ml.MaximumWork-work};
                var result=previous.Apply(new(plan.ObjectId,plan.DeclaredCount,[scalar]),scalarLimits);Charge(result.Work);current=result.Current.Model;bytes=result.Current.DecodedBytes;
                events.Add(new(CreatureView6CompositeEventKind.Assignment,u.Index,ordinal,null,null,null,null));
            }
            else
            {
                var h=u.Ham!;long oldHamBytes=ham!.DecodedBytes;var result=ham.Apply(new(plan.ObjectId,plan.DeclaredCount,[h]),HamLimits());Charge(result.WorkUsed);ham=result.Current;current=WithHam(current,ham);bytes=bytes-oldHamBytes+ham.DecodedBytes;
                foreach(var e in result.Events)events.Add(new(CreatureView6CompositeEventKind.HamMutation,e.Field,ordinal,e.Opcode,e.Index,e.OldValue,e.NewValue));
            }
            if(bytes>ml.MaximumStateBytes||bytes+24L*events.Count>ml.MaximumOutputBytes||events.Count>ml.MaximumEvents)throw new InvalidDataException("Composite publication capacity");
        }
        return new(new(current,revision,bytes,l),plan.DeclaredCount,events.ToArray(),work);
    }
}
