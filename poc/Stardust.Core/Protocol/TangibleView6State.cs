namespace Stardust.Core.Protocol;

public sealed record TangibleView6StateLimits(int MaximumElements=64,int MaximumFields=256,
    int MaximumOperations=4096,int MaximumAssignmentValues=65535,int MaximumMessageIds=65536,
    long MaximumMessageBytes=16384,long MaximumStateBytes=1048576,int MaximumEvents=4096,
    long MaximumOutputBytes=1048576,long MaximumWork=65536);
public enum TangibleView6EventKind { Removed,Added,Set,Changed }
public readonly record struct TangibleView6StateEvent(TangibleView6EventKind Kind,ushort? Index,ulong? OldId,ulong? NewId);
public sealed class TangibleView6StateChange
{
    internal TangibleView6StateChange(TangibleView6State previous,TangibleView6State current,ushort declaredCount,
        TangibleView6StateEvent[] events,long work)
    {Previous=previous;Current=current;DeclaredCount=declaredCount;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public TangibleView6State Previous{get;} public TangibleView6State Current{get;}
    public ushort DeclaredCount{get;} public IReadOnlyList<TangibleView6StateEvent> Events{get;} public long WorkUsed{get;}
    public long DecodedOutputBytes=>checked(Current.DecodedBytes+24L*Events.Count);
}
/// <summary>Immutable raw TANO6 state; Apply privately builds a whole-plan candidate and never executes callbacks.</summary>
public sealed class TangibleView6State
{
    private readonly TangibleView6StateLimits limits;
    private TangibleView6State(ulong id,ushort baselineCount,uint passive,uint counter,ulong revision,
        ulong[] ownedIds,TangibleView6StateLimits limits)
    {ObjectId=id;BaselineDeclaredCount=baselineCount;Passive=passive;Counter=counter;Revision=revision;Ids=Array.AsReadOnly(ownedIds);this.limits=limits;}
    public ulong ObjectId{get;} public ushort BaselineDeclaredCount{get;} public uint Passive{get;}
    public uint Counter{get;} public ulong Revision{get;} public IReadOnlyList<ulong> Ids{get;}
    public long DecodedBytes=>8+8L*Ids.Count;
    private static TangibleView6StateLimits Validate(TangibleView6StateLimits value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(value.MaximumElements is <0 or >65536 || value.MaximumFields is <0 or >4096 || value.MaximumOperations is <0 or >65536 ||
            value.MaximumAssignmentValues is <0 or >65535 || value.MaximumMessageIds is <0 or >1048576 || value.MaximumMessageBytes is <0 or >8388608 ||
            value.MaximumStateBytes is <8 or >16777216 || value.MaximumEvents is <0 or >262144 || value.MaximumOutputBytes is <8 or >33554432 ||
            value.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(value));
        return value with {};
    }
    private static void Capacity(int count,int events,TangibleView6StateLimits l)
    {if(count>l.MaximumElements || 8+8L*count>l.MaximumStateBytes || events>l.MaximumEvents || 8+8L*count+24L*events>l.MaximumOutputBytes)throw new InvalidDataException("TANO6 intermediate state/output capacity");}
    public static TangibleView6State FromBaseline(TangibleView6Baseline baseline,TangibleView6StateLimits? limits=null,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);
        if(l.MaximumFields<2 || baseline.DecodedBytes+2>l.MaximumMessageBytes || baseline.Ids.Count>l.MaximumMessageIds || baseline.Ids.Count>l.MaximumWork)throw new InvalidDataException("TANO6 seed message/work capacity");
        Capacity(baseline.Ids.Count,0,l);
        return new(baseline.ObjectId,baseline.DeclaredCount,baseline.Passive,baseline.Counter,revision,baseline.Ids.ToArray(),l);
    }
    public TangibleView6State Reseed(TangibleView6Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("TANO6 reseed recipient");return FromBaseline(baseline,limits,Revision);}
    private long Preflight(TangibleView6Delta plan,TangibleView6StateLimits l)
    {
        if(plan.ObjectId!=ObjectId || plan.Updates.Count is 0 || plan.Updates.Count>l.MaximumFields)throw new InvalidDataException("TANO6 plan recipient/fields");
        long ops=0,assigned=0,ids=0,bytes=0;
        foreach(var u in plan.Updates)
        {
            bytes+=u.Index==0?6:10;if(u.Index==0)continue;
            ops+=u.Commands.Count;if(ops>l.MaximumOperations)throw new InvalidDataException("TANO6 command capacity");ulong cursor=0;
            foreach(var c in u.Commands)
            {
                if(cursor>=u.DeclaredOperations)throw new InvalidDataException("TANO6 extra raw-loop command");
                cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);ids+=c.Values.Count;assigned+=c.Opcode==3?c.Values.Count:0;
                bytes+=(c.Opcode==4?1:3)+8L*c.Values.Count;
                if(ids>l.MaximumMessageIds || assigned>l.MaximumAssignmentValues || bytes+2>l.MaximumMessageBytes)throw new InvalidDataException("TANO6 aggregate plan capacity");
            }
            if(cursor<u.DeclaredOperations)throw new InvalidDataException("TANO6 incomplete raw-loop commands");
        }
        if(bytes+2>l.MaximumMessageBytes)throw new InvalidDataException("TANO6 message capacity");
        long initial=checked(Ids.Count+plan.Updates.Count+ops+ids);
        if(initial>l.MaximumWork)throw new InvalidDataException("TANO6 preflight work capacity");return initial;
    }
    public TangibleView6StateChange Apply(TangibleView6Delta plan,TangibleView6StateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);ulong revision=checked(Revision+1);var l=Validate(operationLimits??limits);long work=Preflight(plan,l);Capacity(Ids.Count,0,l);
        var ids=Ids.ToList();var events=new List<TangibleView6StateEvent>();uint passive=Passive,counter=Counter;
        void Charge(long amount){work=checked(work+amount);if(work>l.MaximumWork)throw new InvalidDataException("TANO6 mutation work capacity");}
        void CheckCapacity(int count,int eventCount)=>Capacity(count,eventCount,l);
        void Emit(TangibleView6EventKind kind,ushort? index=null,ulong? oldId=null,ulong? newId=null)=>events.Add(new(kind,index,oldId,newId));
        foreach(var update in plan.Updates)
        {
            if(update.Index==0){passive=update.Passive;continue;}
            uint rawSkip=unchecked(counter-update.Counter+update.DeclaredOperations);uint skip=Math.Min(rawSkip,update.DeclaredOperations);ulong cursor=0;
            foreach(var c in update.Commands)
            {
                bool ignored=cursor<skip;cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);if(ignored)continue;
                int index=c.Index;
                switch(c.Opcode)
                {
                    case 0:
                        if(index>=ids.Count)break;
                        CheckCapacity(ids.Count,checked(events.Count+2));Charge(ids.Count-index);ulong removed=ids[index];ids.RemoveAt(index);counter=unchecked(counter+1);
                        Emit(TangibleView6EventKind.Removed,c.Index,removed);Emit(TangibleView6EventKind.Changed);break;
                    case 1:
                        int addedSize=checked(Math.Max(ids.Count,index)+1);CheckCapacity(addedSize,checked(events.Count+2));Charge(checked(addedSize-ids.Count+Math.Max(0,ids.Count-index)));
                        while(ids.Count<index)ids.Add(0);ids.Insert(index,c.Values[0]);counter=unchecked(counter+1);
                        Emit(TangibleView6EventKind.Added,c.Index,null,c.Values[0]);Emit(TangibleView6EventKind.Changed);break;
                    case 2:
                        if(index<ids.Count && ids[index]==c.Values[0])break;
                        int setSize=checked(Math.Max(ids.Count,index+1));CheckCapacity(setSize,checked(events.Count+2));Charge(checked(setSize-ids.Count+1));
                        while(ids.Count<=index)ids.Add(0);ulong old=ids[index];ids[index]=c.Values[0];counter=unchecked(counter+1);
                        Emit(TangibleView6EventKind.Set,c.Index,old,c.Values[0]);Emit(TangibleView6EventKind.Changed);break;
                    case 3:
                        CheckCapacity(c.Values.Count,checked(events.Count+1));Charge(checked(ids.Count+c.Values.Count));
                        ids=c.Values.ToList();counter=unchecked(counter+1+(uint)c.Values.Count);Emit(TangibleView6EventKind.Changed);break;
                    case 4:
                        if(ids.Count==0)break;
                        CheckCapacity(ids.Count,checked(events.Count+ids.Count+1));Charge(ids.Count);
                        for(int i=0;i<ids.Count;i++)Emit(TangibleView6EventKind.Removed,checked((ushort)i),ids[i]);
                        ids.Clear();counter=unchecked(counter+1);Emit(TangibleView6EventKind.Changed);break;
                    default:throw new InvalidDataException("Unsupported TANO6 state command");
                }
            }
        }
        CheckCapacity(ids.Count,events.Count);Charge(ids.Count+events.Count);
        var current=new TangibleView6State(ObjectId,BaselineDeclaredCount,passive,counter,revision,ids.ToArray(),l);
        return new(this,current,plan.DeclaredCount,events.ToArray(),work);
    }
}
