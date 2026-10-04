namespace Stardust.Core.Protocol;
public sealed record CreatureHamView6StateLimits(int MaximumElements=64,int MaximumCombinedElements=128,
    long MaximumStateBytes=1048576,int MaximumEvents=4096,long MaximumOutputBytes=1048576,long MaximumWork=65536,
    CreatureHamView6Limits? MessageLimits=null);
/// <summary>Modern post-publication mutation description; no original callback is invoked.</summary>
public readonly record struct CreatureHamView6Event(ushort Field,byte Opcode,ushort? Index,int? OldValue,int? NewValue);
public sealed class CreatureHamView6Change
{
    internal CreatureHamView6Change(CreatureHamView6State previous,CreatureHamView6State current,ushort count,CreatureHamView6Event[] events,long work)
    {Previous=previous;Current=current;DeclaredCount=count;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureHamView6State Previous{get;}public CreatureHamView6State Current{get;}public ushort DeclaredCount{get;}
    public IReadOnlyList<CreatureHamView6Event> Events{get;}public long WorkUsed{get;}public long DecodedOutputBytes=>Current.DecodedBytes+24L*Events.Count;
}
public sealed class CreatureHamView6State
{
    private readonly CreatureHamView6StateLimits limits;
    private CreatureHamView6State(ulong id,ulong revision,uint hamCounter,int[] ham,uint maximumCounter,int[] maximum,CreatureHamView6StateLimits l)
    {ObjectId=id;Revision=revision;Ham=new(hamCounter,ham);MaximumHam=new(maximumCounter,maximum);limits=l;}
    public ulong ObjectId{get;}public ulong Revision{get;}public CreatureView6Vector<int> Ham{get;}public CreatureView6Vector<int> MaximumHam{get;}
    public long DecodedBytes=>16+4L*(Ham.Values.Count+MaximumHam.Values.Count);
    private static CreatureHamView6StateLimits Validate(CreatureHamView6StateLimits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumElements is <0 or >65536 || l.MaximumCombinedElements is <0 or >131072 || l.MaximumStateBytes is <16 or >16777216 || l.MaximumEvents is <0 or >262144 || l.MaximumOutputBytes is <16 or >33554432 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));var message=l.MessageLimits??new();CreatureHamView6Codec.Validate(message);return l with{MessageLimits=message with{}};}
    private static void Capacity(int ham,int maximum,int events,CreatureHamView6StateLimits l)
    {long total=(long)ham+maximum;if(ham>l.MaximumElements || maximum>l.MaximumElements || total>l.MaximumCombinedElements || 16+4*total>l.MaximumStateBytes || events>l.MaximumEvents || 16+4*total+24L*events>l.MaximumOutputBytes)throw new InvalidDataException("HAM combined intermediate capacity");}
    public static CreatureHamView6State FromBaseline(CreatureView6Baseline baseline,CreatureHamView6StateLimits? limits=null,ulong revisionSeed=0)
    {ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);Capacity(baseline.Ham.Values.Count,baseline.MaximumHam.Values.Count,0,l);if(baseline.Ham.Values.Count+(long)baseline.MaximumHam.Values.Count>l.MaximumWork)throw new InvalidDataException("HAM seed work");return new(baseline.ObjectId,revision,baseline.Ham.Counter,baseline.Ham.Values.ToArray(),baseline.MaximumHam.Counter,baseline.MaximumHam.Values.ToArray(),l);}
    public CreatureHamView6State Reseed(CreatureView6Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("HAM reseed recipient");return FromBaseline(baseline,limits,Revision);}
    public CreatureHamView6Change Apply(CreatureHamView6Delta plan,CreatureHamView6StateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=ObjectId)throw new InvalidDataException("HAM recipient");var l=Validate(operationLimits??limits);ulong revision=checked(Revision+1);var cost=CreatureHamView6Codec.Measure(plan,l.MessageLimits!);
        long work=Ham.Values.Count+(long)MaximumHam.Values.Count+plan.Updates.Count+cost.Commands+cost.Values;
        void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("HAM logical work");}
        Charge(0);Capacity(Ham.Values.Count,MaximumHam.Values.Count,0,l);var ham=Ham.Values.ToList();var maximum=MaximumHam.Values.ToList();uint hc=Ham.Counter,mc=MaximumHam.Counter;var events=new List<CreatureHamView6Event>();
        foreach(var u in plan.Updates)
        {
            var values=u.Field==13?ham:maximum;uint counter=u.Field==13?hc:mc;uint skip=Math.Min(unchecked(u.Extent-u.Counter+counter),u.Extent);ulong cursor=0;
            void Cap(int count){Capacity(u.Field==13?count:ham.Count,u.Field==14?count:maximum.Count,checked(events.Count+1),l);}
            foreach(var c in u.Commands)
            {
                bool ignored=cursor<skip;cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);if(ignored)continue;int index=c.Index;int? old=null,next=null;ushort? eventIndex=c.Opcode<=2?c.Index:null;
                switch(c.Opcode)
                {
                    case 0:if(index>=values.Count)continue;Cap(values.Count);Charge(values.Count-index);old=values[index];values.RemoveAt(index);counter=unchecked(counter+1);break;
                    case 1:int size=checked(Math.Max(values.Count,index)+1);Cap(size);Charge(size-values.Count+Math.Max(0,values.Count-index));while(values.Count<index)values.Add(0);values.Insert(index,c.Values[0]);next=c.Values[0];counter=unchecked(counter+1);break;
                    case 2:if(index<values.Count && values[index]==c.Values[0])continue;int end=checked(Math.Max(values.Count,index+1));Cap(end);Charge(end-values.Count+1);while(values.Count<=index)values.Add(0);old=values[index];values[index]=c.Values[0];next=c.Values[0];counter=unchecked(counter+1);break;
                    case 3:Cap(c.Values.Count);Charge(values.Count+c.Values.Count);values=c.Values.ToList();counter=unchecked(counter+1+(uint)c.Values.Count);break;
                    case 4:if(values.Count==0)continue;Cap(values.Count);Charge(values.Count);values.Clear();counter=unchecked(counter+1);break;
                    default:throw new InvalidDataException("HAM command");
                }
                events.Add(new(u.Field,c.Opcode,eventIndex,old,next));if(u.Field==13)ham=values;else maximum=values;
            }
            if(u.Field==13){ham=values;hc=counter;}else{maximum=values;mc=counter;}
        }
        Capacity(ham.Count,maximum.Count,events.Count,l);Charge(ham.Count+maximum.Count+events.Count);
        return new(this,new(ObjectId,revision,hc,ham.ToArray(),mc,maximum.ToArray(),l),plan.DeclaredCount,events.ToArray(),work);
    }
}
