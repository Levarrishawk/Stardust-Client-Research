namespace Stardust.Core.Protocol;
public sealed record CreatureSkillModsView4KernelLimits(int MaximumEntries=4096,int MaximumSeedEntries=65536,
    int MaximumKeyBytes=65536,int MaximumTotalKeyBytes=262144,long MaximumStateBytes=1048576,
    int MaximumEvents=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576,
    CreatureSkillModsView4Limits? MessageLimits=null);
/// <summary>Canonical byte key: shortest prefix is modern representation, never key identity.</summary>
public sealed class CreatureSkillModsView4Entry
{
    private readonly byte[] bytes;
    public CreatureSkillModsView4Entry(ReplicationByteString name,int baseValue,int bonus)
    {ArgumentNullException.ThrowIfNull(name);bytes=name.CopyBytes();Name=ReplicationByteString.Create(bytes,bytes.Length>=65535);BaseValue=baseValue;Bonus=bonus;}
    public ReplicationByteString Name{get;}public int BaseValue{get;}public int Bonus{get;}
    internal ReadOnlySpan<byte> Bytes=>bytes;
}
/// <summary>Modern post-publication summary, not a legacy callback or pending command.</summary>
public sealed record CreatureSkillModsView4Event(byte Opcode,ReplicationByteString Name,int? OldBase,int? OldBonus,int? NewBase,int? NewBonus);
public sealed class CreatureSkillModsView4Change
{
    internal CreatureSkillModsView4Change(CreatureSkillModsView4Kernel previous,CreatureSkillModsView4Kernel current,
        ushort declared,CreatureSkillModsView4Event[] events,long work)
    {Previous=previous;Current=current;DeclaredCount=declared;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureSkillModsView4Kernel Previous{get;}public CreatureSkillModsView4Kernel Current{get;}
    public ushort DeclaredCount{get;}public IReadOnlyList<CreatureSkillModsView4Event> Events{get;}public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+Events.Sum(e=>32L+e.Name.Length);
}
public sealed class CreatureSkillModsView4Kernel
{
    private readonly CreatureSkillModsView4KernelLimits limits;
    private CreatureSkillModsView4Kernel(ulong id,ulong revision,uint counter,CreatureSkillModsView4Entry[] entries,CreatureSkillModsView4KernelLimits l)
    {ObjectId=id;Revision=revision;Counter=counter;Entries=Array.AsReadOnly(entries);limits=l;}
    public ulong ObjectId{get;}public ulong Revision{get;}public uint Counter{get;}
    public IReadOnlyList<CreatureSkillModsView4Entry> Entries{get;}
    public long DecodedBytes=>8+16L*Entries.Count+Entries.Sum(e=>(long)e.Name.Length);
    private static CreatureSkillModsView4KernelLimits Validate(CreatureSkillModsView4KernelLimits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumEntries is <0 or >65536 || l.MaximumSeedEntries is <0 or >65536 || l.MaximumKeyBytes is <0 or >1048576 || l.MaximumTotalKeyBytes is <0 or >4194304 || l.MaximumStateBytes is <8 or >16777216 || l.MaximumEvents is <0 or >262144 || l.MaximumOutputBytes is <8 or >33554432 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));var message=l.MessageLimits??new();CreatureSkillModsView4Codec.Validate(message);return l with{MessageLimits=message with{}};}
    private static void Capacity(int count,long keyBytes,int events,long eventKeys,CreatureSkillModsView4KernelLimits l)
    {long bytes=8+16L*count+keyBytes;if(count>l.MaximumEntries || keyBytes>l.MaximumTotalKeyBytes || bytes>l.MaximumStateBytes || events>l.MaximumEvents || bytes+32L*events+eventKeys>l.MaximumOutputBytes)throw new InvalidDataException("Skill intermediate capacity");}
    private static int Find(List<CreatureSkillModsView4Entry> entries,byte[] key,Action<long> charge,out bool found)
    {
        for(int i=0;i<entries.Count;i++){charge(1L+Math.Min(entries[i].Name.Length,key.Length));int compare=entries[i].Bytes.SequenceCompareTo(key);if(compare>=0){found=compare==0;return i;}}
        found=false;return entries.Count;
    }
    public static CreatureSkillModsView4Kernel FromBaseline(CreatureView4Baseline baseline,CreatureSkillModsView4KernelLimits? limits=null,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);
        var raw=baseline.SkillMods.Values;if(raw.Count>l.MaximumSeedEntries)throw new InvalidDataException("Skill raw seed count");long rawKeys=0;
        foreach(var e in raw){if(e.Name.Length>l.MaximumKeyBytes)throw new InvalidDataException("Skill seed key cap");rawKeys+=e.Name.Length;if(rawKeys>l.MaximumTotalKeyBytes)throw new InvalidDataException("Skill raw seed key aggregate");}
        long work=raw.Count+rawKeys;void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Skill seed work");}
        Charge(0);Capacity(0,0,0,0,l);var entries=new List<CreatureSkillModsView4Entry>();long keys=0;
        foreach(var e in raw)
        {
            int index=Find(entries,e.Name.CopyBytes(),Charge,out bool found);Charge(1);
            if(found)entries[index]=new(entries[index].Name,e.BaseValue,e.Bonus);
            else{Capacity(entries.Count+1,keys+e.Name.Length,0,0,l);Charge(entries.Count-index);entries.Insert(index,new(e.Name,e.BaseValue,e.Bonus));keys+=e.Name.Length;}
        }
        Charge(entries.Count+keys);return new(baseline.ObjectId,revision,baseline.SkillMods.Counter,entries.ToArray(),l);
    }
    public CreatureSkillModsView4Kernel Reseed(CreatureView4Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("Skill reseed recipient");return FromBaseline(baseline,limits,Revision);}
    public CreatureSkillModsView4Change Apply(CreatureSkillModsView4Delta plan,CreatureSkillModsView4KernelLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=ObjectId)throw new InvalidDataException("Skill recipient");var l=Validate(operationLimits??limits);ulong revision=checked(Revision+1);
        var cost=CreatureSkillModsView4Codec.Measure(plan,l.MessageLimits!);long keys=Entries.Sum(e=>(long)e.Name.Length);
        long work=Entries.Count+keys+cost.Work;void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Skill logical work");}
        Charge(0);foreach(var entry in Entries)if(entry.Name.Length>l.MaximumKeyBytes)throw new InvalidDataException("Skill retained key cap");Capacity(Entries.Count,keys,0,0,l);
        var entries=Entries.ToList();uint counter=Counter;var events=new List<CreatureSkillModsView4Event>();long eventKeys=0;
        foreach(var u in plan.Updates)
        {
            uint sum=unchecked(counter+u.Extent);uint skip=sum<=u.Counter?0:Math.Min(unchecked(counter-u.Counter+u.Extent),u.Extent);
            for(int ordinal=0;ordinal<u.Commands.Count;ordinal++)
            {
                var c=u.Commands[ordinal];if((uint)ordinal<skip)continue;
                if(c.Name.Length>l.MaximumKeyBytes)throw new InvalidDataException("Skill candidate key cap");int index=Find(entries,c.Name.CopyBytes(),Charge,out bool found);
                if(c.Opcode==1 && !found)continue;
                var old=found?entries[index]:null;
                if(c.Opcode!=1 && old is not null && old.BaseValue==c.BaseValue && old.Bonus==c.Bonus)continue;
                int count=c.Opcode==1?entries.Count:entries.Count+(found?0:1);long candidateKeys=keys+(found?0:c.Name.Length);
                Capacity(count,candidateKeys,checked(events.Count+1),eventKeys+c.Name.Length,l);
                CreatureSkillModsView4Entry? next=null;
                if(c.Opcode==1){Charge(entries.Count-index);entries.RemoveAt(index);keys-=old!.Name.Length;}
                else{Charge(1+(found?0:entries.Count-index));next=new(old?.Name??c.Name,c.BaseValue,c.Bonus);if(found)entries[index]=next;else{entries.Insert(index,next);keys+=c.Name.Length;}}
                counter=unchecked(counter+1);eventKeys+=c.Name.Length;
                events.Add(new(c.Opcode,(old??next)!.Name,old?.BaseValue,old?.Bonus,next?.BaseValue,next?.Bonus));
            }
            if(counter<u.Counter)counter=u.Counter;
        }
        Capacity(entries.Count,keys,events.Count,eventKeys,l);Charge(entries.Count+keys+events.Count+eventKeys);
        return new(this,new(ObjectId,revision,counter,entries.ToArray(),l),plan.DeclaredCount,events.ToArray(),work);
    }
}
