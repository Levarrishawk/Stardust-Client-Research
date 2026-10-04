namespace Stardust.Core.Protocol;

public sealed record CreatureView4ScalarEncumbranceStateLimits(CreatureView4StateLimits? ModelLimits=null,
    CreatureView4ScalarEncumbranceLimits? MessageLimits=null);
public enum CreatureView4ScalarEncumbranceEventKind { Assignment,EncumbranceMutation }
/// <summary>Modern descriptions: 48 logical bytes, not legacy callbacks or managed layout.</summary>
public readonly record struct CreatureView4ScalarEncumbranceEvent(CreatureView4ScalarEncumbranceEventKind Kind,
    ushort Field,int UpdateOrdinal,byte? Opcode,ushort? Index,ulong? OldBits,ulong? NewBits,int? OldValue,int? NewValue);
public sealed class CreatureView4ScalarEncumbranceChange
{
    internal CreatureView4ScalarEncumbranceChange(CreatureView4ScalarEncumbranceState current,ushort declaredCount,
        CreatureView4ScalarEncumbranceEvent[] events,long work)
    {Current=current;DeclaredCount=declaredCount;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureView4ScalarEncumbranceState Current{get;} public ushort DeclaredCount{get;}
    public IReadOnlyList<CreatureView4ScalarEncumbranceEvent> Events{get;} public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+48L*Events.Count;
}
public sealed class CreatureView4ScalarEncumbranceState
{
    private readonly CreatureView4ScalarEncumbranceStateLimits limits;
    private CreatureView4ScalarEncumbranceState(CreatureView4Baseline model,ulong revision,long bytes,
        CreatureView4ScalarEncumbranceStateLimits limits){Model=model;Revision=revision;DecodedBytes=bytes;this.limits=limits;}
    public CreatureView4Baseline Model{get;} public ulong ObjectId=>Model.ObjectId;
    public ulong Revision{get;} public long DecodedBytes{get;}
    private static CreatureView4ScalarEncumbranceStateLimits Validate(CreatureView4ScalarEncumbranceStateLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);var m=l.ModelLimits??new();var c=l.MessageLimits??new();
        if(m.MaximumUpdates is <0 or >4096||m.MaximumElements is <0 or >65536||m.MaximumTotalElements is <0 or >196608||m.MaximumStringBytes is <0 or >1048576||m.MaximumTotalStringBytes is <0 or >4194304||m.MaximumStateBytes is <0 or >16777216||m.MaximumEvents is <0 or >262144||m.MaximumOutputBytes is <0 or >33554432||m.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
        CreatureView4ScalarEncumbranceCodec.Validate(c);return new(m with{},c with{});
    }
    private static long SeedWork(CreatureView4Baseline m)=>14L+m.Encumbrances.Values.Count+m.SkillMods.Values.Count+
        m.SpaceMissionObjects.Values.Count+m.SkillMods.Values.Sum(s=>(long)s.Name.Length);
    private static CreatureView4Baseline Vector(CreatureView4Baseline m,CreatureView4Collection<int> enc)
        =>new(m.ObjectId,m.DeclaredCount,m.FloatBits,m.ListenTo,enc,m.SkillMods,m.SpaceMissionObjects);
    public static CreatureView4ScalarEncumbranceState FromBaseline(CreatureView4Baseline baseline,
        CreatureView4ScalarEncumbranceStateLimits? limits=null,ulong revisionSeed=0)
    {ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());var seed=CreatureView4State.FromBaseline(baseline,l.ModelLimits,revisionSeed);return new(seed.Model,seed.Revision,seed.DecodedBytes,l);}
    public CreatureView4ScalarEncumbranceState Reseed(CreatureView4Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("Composed CREO4 reseed recipient");return FromBaseline(baseline,limits,Revision);}
    public CreatureView4ScalarEncumbranceChange Apply(CreatureView4ScalarEncumbranceDelta plan,
        CreatureView4ScalarEncumbranceStateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);var l=Validate(operationLimits??limits);var ml=l.ModelLimits!;
        if(plan.ObjectId!=ObjectId||plan.Updates.Count>ml.MaximumUpdates)throw new InvalidDataException("Composed CREO4 recipient/update capacity");
        var message=CreatureView4ScalarEncumbranceCodec.Measure(plan,l.MessageLimits!);ulong revision=checked(Revision+1);
        long work=checked(SeedWork(Model)+message.Work);
        if(work>ml.MaximumWork)throw new InvalidDataException("Composed CREO4 initial logical work");
        var seed=CreatureView4State.FromBaseline(Model,ml with{MaximumWork=16777216});
        var current=Model;long bytes=seed.DecodedBytes;var events=new List<CreatureView4ScalarEncumbranceEvent>();
        void Charge(long n){work=checked(work+n);if(work>ml.MaximumWork)throw new InvalidDataException("Composed CREO4 logical work");}
        long Output()=>ml.MaximumOutputBytes-48L*events.Count;
        void Gate(int count,long candidateBytes,int added)
        {
            long others=current.SkillMods.Values.Count+(long)current.SpaceMissionObjects.Values.Count;
            if(count>ml.MaximumElements||count+others>ml.MaximumTotalElements||candidateBytes>ml.MaximumStateBytes||
                events.Count+(long)added>ml.MaximumEvents||candidateBytes+48L*(events.Count+(long)added)>ml.MaximumOutputBytes)
                throw new InvalidDataException("Composed CREO4 intermediate capacity");
        }
        CreatureEncumbrancesView4KernelLimits ChildLimits(long childWork)
        {
            int n=current.Encumbrances.Values.Count,other=current.SkillMods.Values.Count+current.SpaceMissionObjects.Values.Count;
            long otherBytes=bytes-(8+4L*n);
            return new(Math.Min(ml.MaximumElements,ml.MaximumTotalElements-other),ml.MaximumStateBytes-otherBytes,
                ml.MaximumEvents-events.Count,Output()-otherBytes,childWork,new(8388608,4096,65536,1048576,16777216));
        }
        for(int ordinal=0;ordinal<plan.Updates.Count;ordinal++)
        {
            var u=plan.Updates[ordinal];
            if(u.Scalar is {} scalar)
            {
                Gate(current.Encumbrances.Values.Count,bytes,1);long seedWork=SeedWork(current);
                Charge(seedWork);var previous=CreatureView4State.FromBaseline(current,ml with{MaximumWork=seedWork,MaximumOutputBytes=Output()});
                long childWork=checked(2*seedWork+1);Charge(childWork);
                var r=previous.Apply(new(ObjectId,plan.DeclaredCount,[scalar]),ml with{MaximumUpdates=1,MaximumEvents=1,MaximumOutputBytes=Output(),MaximumWork=childWork});
                if(r.WorkUsed!=childWork)throw new InvalidDataException("Scalar work prediction mismatch");
                var e=r.Events[0];current=r.Current.Model;bytes=r.Current.DecodedBytes;
                events.Add(new(CreatureView4ScalarEncumbranceEventKind.Assignment,u.Index,ordinal,null,null,e.OldBits,e.NewBits,null,null));
            }
            else
            {
                var v=u.Encumbrance!;int initialCount=current.Encumbrances.Values.Count;
                Charge(initialCount);var state=CreatureEncumbrancesView4Kernel.FromBaseline(current,ChildLimits(initialCount));
                uint skip=Math.Min(unchecked(v.Extent-v.Counter+state.Encumbrances.Counter),v.Extent);ulong cursor=0;
                foreach(var c in v.Commands)
                {
                    bool ignored=cursor<skip;uint extent=1+(c.Opcode==3?(uint)c.Values.Count:0);cursor+=extent;if(ignored)continue;
                    var values=current.Encumbrances.Values;int n=values.Count,index=c.Index,count=n,added=0;long mutation=0;
                    switch(c.Opcode)
                    {
                        case 0:if(index<n){count=n-1;added=1;mutation=n-index;}break;
                        case 1:count=checked(Math.Max(n,index)+1);added=1;mutation=count-n+Math.Max(0,n-index);break;
                        case 2:if(index>=n||values[index]!=c.Values[0]){count=Math.Max(n,index+1);added=1;mutation=count-n+1;}break;
                        case 3:count=c.Values.Count;added=1;mutation=n+(long)count;break;
                        case 4:if(n>0){count=0;added=1;mutation=n;}break;
                    }
                    Gate(count,bytes+4L*(count-n),added);
                    int constructionCount=c.Opcode is 0 or 4?n:count;
                    if(bytes+4L*(constructionCount-n)+48L*events.Count+24L*added>ml.MaximumOutputBytes)
                        throw new InvalidDataException("Composed CREO4 narrow construction output");
                    long childWork=n+2L+c.Values.Count+mutation+count+added;Charge(childWork);
                    var r=state.Apply(new(ObjectId,plan.DeclaredCount,[new(2,extent,unchecked(state.Encumbrances.Counter+extent),[c])]),ChildLimits(childWork));
                    if(r.Events.Count!=added||r.WorkUsed!=childWork)throw new InvalidDataException("Encumbrance capacity/work prediction mismatch");
                    long oldBytes=state.DecodedBytes;state=r.Current;bytes+=state.DecodedBytes-oldBytes;current=Vector(current,state.Encumbrances);
                    foreach(var e in r.Events)events.Add(new(CreatureView4ScalarEncumbranceEventKind.EncumbranceMutation,2,ordinal,e.Opcode,e.Index,null,null,e.OldValue,e.NewValue));
                }
            }
            Gate(current.Encumbrances.Values.Count,bytes,0);
        }
        Charge(SeedWork(current)+events.Count);
        return new(new(current,revision,bytes,l),plan.DeclaredCount,events.ToArray(),work);
    }
}
