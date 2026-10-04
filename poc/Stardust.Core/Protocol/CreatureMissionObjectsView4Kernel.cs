namespace Stardust.Core.Protocol;
public sealed record CreatureMissionObjectsView4KernelLimits(int MaximumPairs=65536,int MaximumSeedPairs=65536,
    long MaximumStateBytes=1048584,int MaximumEvents=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576,
    CreatureMissionObjectsView4Limits? MessageLimits=null);
/// <summary>Modern complete-pair mutation summary; clear uses null, no original callbacks.</summary>
public readonly record struct CreatureMissionObjectsView4Event(byte Opcode,CreatureView4MissionPair? Pair);
public sealed class CreatureMissionObjectsView4Change
{
    internal CreatureMissionObjectsView4Change(CreatureMissionObjectsView4Kernel previous,CreatureMissionObjectsView4Kernel current,
        ushort declared,CreatureMissionObjectsView4Event[] events,long work)
    {Previous=previous;Current=current;DeclaredCount=declared;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureMissionObjectsView4Kernel Previous{get;}public CreatureMissionObjectsView4Kernel Current{get;}
    public ushort DeclaredCount{get;}public IReadOnlyList<CreatureMissionObjectsView4Event> Events{get;}public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+32L*Events.Count;
}
public sealed class CreatureMissionObjectsView4Kernel
{
    private readonly CreatureMissionObjectsView4KernelLimits limits;
    private CreatureMissionObjectsView4Kernel(ulong id,ulong revision,uint counter,CreatureView4MissionPair[] pairs,CreatureMissionObjectsView4KernelLimits l)
    {ObjectId=id;Revision=revision;Counter=counter;Pairs=Array.AsReadOnly(pairs);limits=l;}
    public ulong ObjectId{get;}public ulong Revision{get;}public uint Counter{get;}
    public IReadOnlyList<CreatureView4MissionPair> Pairs{get;}public long DecodedBytes=>8+16L*Pairs.Count;
    private static CreatureMissionObjectsView4KernelLimits Validate(CreatureMissionObjectsView4KernelLimits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumPairs is <0 or >65536 || l.MaximumSeedPairs is <0 or >65536 || l.MaximumStateBytes is <8 or >16777216 || l.MaximumEvents is <0 or >262144 || l.MaximumOutputBytes is <8 or >33554432 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));var message=l.MessageLimits??new();CreatureMissionObjectsView4Codec.Validate(message);return l with{MessageLimits=message with{}};}
    private static void Capacity(int pairs,int events,CreatureMissionObjectsView4KernelLimits l)
    {long bytes=8+16L*pairs;if(pairs>l.MaximumPairs || bytes>l.MaximumStateBytes || events>l.MaximumEvents || bytes+32L*events>l.MaximumOutputBytes)throw new InvalidDataException("Mission intermediate capacity");}
    private static int Find(List<CreatureView4MissionPair> pairs,CreatureView4MissionPair pair,Action<long> charge,out bool found)
    {
        for(int n=0;n<pairs.Count;n++)
        {
            charge(pairs[n].OwnerId==pair.OwnerId?2:1);
            int compare=unchecked((long)pairs[n].OwnerId).CompareTo(unchecked((long)pair.OwnerId));
            if(compare==0)compare=unchecked((long)pairs[n].MissionId).CompareTo(unchecked((long)pair.MissionId));
            if(compare>=0){found=compare==0;return n;}
        }
        found=false;return pairs.Count;
    }
    public static CreatureMissionObjectsView4Kernel FromBaseline(CreatureView4Baseline baseline,CreatureMissionObjectsView4KernelLimits? limits=null,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);var raw=baseline.SpaceMissionObjects.Values;
        if(raw.Count>l.MaximumSeedPairs)throw new InvalidDataException("Mission raw seed count");long work=raw.Count;
        void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Mission seed work");}
        Charge(0);Capacity(0,0,l);var pairs=new List<CreatureView4MissionPair>();
        foreach(var pair in raw){int n=Find(pairs,pair,Charge,out bool found);Charge(1);if(found)continue;Capacity(pairs.Count+1,0,l);Charge(pairs.Count-n);pairs.Insert(n,pair);}
        Charge(pairs.Count);return new(baseline.ObjectId,revision,baseline.SpaceMissionObjects.Counter,pairs.ToArray(),l);
    }
    public CreatureMissionObjectsView4Kernel Reseed(CreatureView4Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("Mission reseed recipient");return FromBaseline(baseline,limits,Revision);}
    public CreatureMissionObjectsView4Change Apply(CreatureMissionObjectsView4Delta plan,CreatureMissionObjectsView4KernelLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=ObjectId)throw new InvalidDataException("Mission recipient");var l=Validate(operationLimits??limits);ulong revision=checked(Revision+1);var cost=CreatureMissionObjectsView4Codec.Measure(plan,l.MessageLimits!);
        long work=Pairs.Count+cost.Work;void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Mission logical work");}
        Charge(0);Capacity(Pairs.Count,0,l);var pairs=Pairs.ToList();uint counter=Counter;var events=new List<CreatureMissionObjectsView4Event>();
        foreach(var u in plan.Updates)
        {
            uint sum=unchecked(counter+u.Extent);uint skip=sum<=u.Counter?0:Math.Min(unchecked(counter-u.Counter+u.Extent),u.Extent);
            for(int ordinal=0;ordinal<u.Commands.Count;ordinal++)
            {
                if((uint)ordinal<skip)continue;var c=u.Commands[ordinal];
                if(c.Opcode==2){if(pairs.Count==0)continue;Capacity(pairs.Count,checked(events.Count+1),l);Charge(pairs.Count);pairs.Clear();}
                else
                {
                    var pair=c.Pair!.Value;int n=Find(pairs,pair,Charge,out bool found);
                    if((c.Opcode==0 && !found)||(c.Opcode==1 && found))continue;
                    int candidate=pairs.Count+(c.Opcode==1?1:0);Capacity(candidate,checked(events.Count+1),l);
                    if(c.Opcode==0){Charge(pairs.Count-n);pairs.RemoveAt(n);}else{Charge(1+pairs.Count-n);pairs.Insert(n,pair);}
                }
                counter=unchecked(counter+1);events.Add(new(c.Opcode,c.Pair));
            }
            if(counter<u.Counter)counter=u.Counter;
        }
        Capacity(pairs.Count,events.Count,l);Charge(pairs.Count+events.Count);
        return new(this,new(ObjectId,revision,counter,pairs.ToArray(),l),plan.DeclaredCount,events.ToArray(),work);
    }
}
