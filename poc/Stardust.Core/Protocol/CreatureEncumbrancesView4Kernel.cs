namespace Stardust.Core.Protocol;

public sealed record CreatureEncumbrancesView4KernelLimits(int MaximumElements=65536,
    long MaximumStateBytes=262152,int MaximumEvents=4096,long MaximumOutputBytes=1048576,
    long MaximumWork=1048576,CreatureEncumbrancesView4Limits? MessageLimits=null);

/// <summary>Modern mutation summary published only after success; never a legacy callback.</summary>
public readonly record struct CreatureEncumbrancesView4Event(byte Opcode,ushort? Index,int? OldValue,int? NewValue);

public sealed class CreatureEncumbrancesView4Change
{
    internal CreatureEncumbrancesView4Change(CreatureEncumbrancesView4Kernel previous,
        CreatureEncumbrancesView4Kernel current,ushort declaredCount,
        CreatureEncumbrancesView4Event[] events,long work)
    {Previous=previous;Current=current;DeclaredCount=declaredCount;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureEncumbrancesView4Kernel Previous{get;}
    public CreatureEncumbrancesView4Kernel Current{get;}
    public ushort DeclaredCount{get;}
    public IReadOnlyList<CreatureEncumbrancesView4Event> Events{get;}
    public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+24L*Events.Count;
}

/// <summary>Owned field2 projection. Limits and accounting are modern policy.</summary>
public sealed class CreatureEncumbrancesView4Kernel
{
    private readonly CreatureEncumbrancesView4KernelLimits limits;
    private CreatureEncumbrancesView4Kernel(ulong id,ulong revision,uint counter,int[] values,
        CreatureEncumbrancesView4KernelLimits limits)
    {ObjectId=id;Revision=revision;Encumbrances=new(counter,values);this.limits=limits;}
    public ulong ObjectId{get;}
    public ulong Revision{get;}
    public CreatureView4Collection<int> Encumbrances{get;}
    public long DecodedBytes=>8+4L*Encumbrances.Values.Count;
    private static CreatureEncumbrancesView4KernelLimits Validate(CreatureEncumbrancesView4KernelLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);
        if(l.MaximumElements is <0 or >65536 || l.MaximumStateBytes is <8 or >16777216 ||
            l.MaximumEvents is <0 or >262144 || l.MaximumOutputBytes is <8 or >33554432 ||
            l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
        var message=l.MessageLimits??new();CreatureEncumbrancesView4Codec.Validate(message);
        return l with{MessageLimits=message with{}};
    }
    private static void Capacity(int count,int events,CreatureEncumbrancesView4KernelLimits l)
    {
        long bytes=8+4L*count;
        if(count>l.MaximumElements || bytes>l.MaximumStateBytes || events>l.MaximumEvents ||
            bytes+24L*events>l.MaximumOutputBytes)throw new InvalidDataException("Encumbrance intermediate capacity");
    }
    public static CreatureEncumbrancesView4Kernel FromBaseline(CreatureView4Baseline baseline,
        CreatureEncumbrancesView4KernelLimits? limits=null,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());
        ulong revision=checked(revisionSeed+1);Capacity(baseline.Encumbrances.Values.Count,0,l);
        if(baseline.Encumbrances.Values.Count>l.MaximumWork)throw new InvalidDataException("Encumbrance seed work");
        return new(baseline.ObjectId,revision,baseline.Encumbrances.Counter,baseline.Encumbrances.Values.ToArray(),l);
    }
    public CreatureEncumbrancesView4Kernel Reseed(CreatureView4Baseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("Encumbrance reseed recipient");
        return FromBaseline(baseline,limits,Revision);
    }
    public CreatureEncumbrancesView4Change Apply(CreatureEncumbrancesView4Delta plan,
        CreatureEncumbrancesView4KernelLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=ObjectId)throw new InvalidDataException("Encumbrance recipient");
        var l=Validate(operationLimits??limits);ulong revision=checked(Revision+1);
        var cost=CreatureEncumbrancesView4Codec.Measure(plan,l.MessageLimits!);
        long work=Encumbrances.Values.Count+(long)plan.Updates.Count+cost.Commands+cost.Values;
        void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Encumbrance logical work");}
        Charge(0);Capacity(Encumbrances.Values.Count,0,l);
        var values=Encumbrances.Values.ToList();uint counter=Encumbrances.Counter;
        var events=new List<CreatureEncumbrancesView4Event>();
        foreach(var update in plan.Updates)
        {
            uint skip=Math.Min(unchecked(update.Extent-update.Counter+counter),update.Extent);ulong cursor=0;
            foreach(var command in update.Commands)
            {
                bool ignored=cursor<skip;cursor+=1UL+(command.Opcode==3?(ulong)command.Values.Count:0);
                if(ignored)continue;
                int index=command.Index;int? old=null,next=null;
                void Cap(int count)=>Capacity(count,checked(events.Count+1),l);
                switch(command.Opcode)
                {
                    case 0:
                        if(index>=values.Count)continue;
                        Cap(values.Count);Charge(values.Count-index);old=values[index];values.RemoveAt(index);
                        counter=unchecked(counter+1);break;
                    case 1:
                        int size=checked(Math.Max(values.Count,index)+1);Cap(size);
                        Charge(size-values.Count+Math.Max(0,values.Count-index));
                        while(values.Count<index)values.Add(0);
                        values.Insert(index,command.Values[0]);next=command.Values[0];counter=unchecked(counter+1);break;
                    case 2:
                        if(index<values.Count && values[index]==command.Values[0])continue;
                        int end=checked(Math.Max(values.Count,index+1));Cap(end);Charge(end-values.Count+1);
                        while(values.Count<=index)values.Add(0);
                        old=values[index];values[index]=command.Values[0];next=command.Values[0];counter=unchecked(counter+1);break;
                    case 3:
                        Cap(command.Values.Count);Charge(values.Count+command.Values.Count);
                        values=command.Values.ToList();counter=unchecked(counter+1+(uint)command.Values.Count);break;
                    case 4:
                        if(values.Count==0)continue;
                        Cap(values.Count);Charge(values.Count);values.Clear();counter=unchecked(counter+1);break;
                    default:throw new InvalidDataException("Encumbrance command");
                }
                events.Add(new(command.Opcode,command.Opcode<=2?command.Index:null,old,next));
            }
        }
        Capacity(values.Count,events.Count,l);Charge(values.Count+events.Count);
        return new(this,new(ObjectId,revision,counter,values.ToArray(),l),plan.DeclaredCount,events.ToArray(),work);
    }
}
