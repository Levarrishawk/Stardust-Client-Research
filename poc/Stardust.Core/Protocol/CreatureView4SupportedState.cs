namespace Stardust.Core.Protocol;
public sealed record CreatureView4SupportedStateLimits(CreatureView4StateLimits? ModelLimits=null,CreatureView4SupportedLimits? MessageLimits=null);
public enum CreatureView4SupportedEventKind { Assignment,EncumbranceMutation,SkillMutation }
public sealed record CreatureView4SupportedEvent(CreatureView4SupportedEventKind Kind,ushort Field,int UpdateOrdinal,
    byte? Opcode=null,ushort? Index=null,ulong? OldBits=null,ulong? NewBits=null,int? OldValue=null,int? NewValue=null,
    ReplicationByteString? Name=null,int? OldBase=null,int? OldBonus=null,int? NewBase=null,int? NewBonus=null)
{public long LogicalBytes=>Kind==CreatureView4SupportedEventKind.SkillMutation?64L+Name!.Length:48;}
public sealed class CreatureView4SupportedChange
{
    internal CreatureView4SupportedChange(CreatureView4SupportedState current,ushort count,CreatureView4SupportedEvent[] events,long work)
    {Current=current;DeclaredCount=count;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureView4SupportedState Current{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureView4SupportedEvent> Events{get;}public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+Events.Sum(e=>e.LogicalBytes);
}
public sealed class CreatureView4SupportedState
{
    private readonly CreatureView4SupportedStateLimits limits;private readonly CreatureSkillModsView4Kernel map;
    private CreatureView4SupportedState(CreatureView4Baseline model,CreatureSkillModsView4Kernel map,ulong revision,CreatureView4SupportedStateLimits limits)
    {Model=model;this.map=map;Revision=revision;this.limits=limits;}
    public CreatureView4Baseline Model{get;}public ulong ObjectId=>Model.ObjectId;public ulong Revision{get;}
    public long DecodedBytes=>Other(Model)+map.DecodedBytes;
    private static long Other(CreatureView4Baseline m)=>64+4L*m.Encumbrances.Values.Count+16L*m.SpaceMissionObjects.Values.Count;
    private static long Seed(CreatureView4Baseline m)=>14L+m.Encumbrances.Values.Count+m.SkillMods.Values.Count+m.SpaceMissionObjects.Values.Count+m.SkillMods.Values.Sum(e=>(long)e.Name.Length);
    private static CreatureView4SupportedStateLimits Validate(CreatureView4SupportedStateLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);var m=l.ModelLimits??new();var c=l.MessageLimits??new();
        if(m.MaximumUpdates is <0 or >4096||m.MaximumElements is <0 or >65536||m.MaximumTotalElements is <0 or >196608||m.MaximumStringBytes is <0 or >1048576||m.MaximumTotalStringBytes is <0 or >4194304||m.MaximumStateBytes is <0 or >16777216||m.MaximumEvents is <0 or >262144||m.MaximumOutputBytes is <0 or >33554432||m.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
        CreatureView4SupportedCodec.Validate(c);return new(m with{},c with{});
    }
    private static void Gate(CreatureView4Baseline m,int count,long keys,long bytes,int events,long eventBytes,CreatureView4StateLimits l)
    {
        if(count>l.MaximumElements||m.Encumbrances.Values.Count>l.MaximumElements||m.SpaceMissionObjects.Values.Count>l.MaximumElements||count+(long)m.Encumbrances.Values.Count+m.SpaceMissionObjects.Values.Count>l.MaximumTotalElements||keys>l.MaximumTotalStringBytes||bytes>l.MaximumStateBytes||events>l.MaximumEvents||bytes+eventBytes>l.MaximumOutputBytes)throw new InvalidDataException("Supported full capacity");
    }
    private static CreatureView4Baseline Project(CreatureView4Baseline m,CreatureSkillModsView4Kernel map)
        =>new(m.ObjectId,m.DeclaredCount,m.FloatBits,m.ListenTo,m.Encumbrances,new(map.Counter,map.Entries.Select(e=>new CreatureView4SkillMod(0,e.Name,e.BaseValue,e.Bonus))),m.SpaceMissionObjects);
    private sealed record Shadow(byte[] Key,int Base,int Bonus);
    private static int Find(List<Shadow> entries,byte[] key,Action<long> charge,out bool found)
    {for(int i=0;i<entries.Count;i++){charge(1L+Math.Min(entries[i].Key.Length,key.Length));int c=entries[i].Key.AsSpan().SequenceCompareTo(key);if(c>=0){found=c==0;return i;}}found=false;return entries.Count;}
    public static CreatureView4SupportedState FromBaseline(CreatureView4Baseline raw,CreatureView4SupportedStateLimits? limits=null,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(raw);var l=Validate(limits??new());var ml=l.ModelLimits!;ulong revision=checked(revisionSeed+1);
        long keys=raw.SkillMods.Values.Sum(e=>(long)e.Name.Length),other=Other(raw);
        foreach(var e in raw.SkillMods.Values)if(e.Name.Length>ml.MaximumStringBytes)throw new InvalidDataException("Supported raw seed key cap");
        Gate(raw,raw.SkillMods.Values.Count,keys,other+8+16L*raw.SkillMods.Values.Count+keys,0,0,ml);
        long fullRaw=Seed(raw),childWork=raw.SkillMods.Values.Count+keys;
        void Charge(long n){childWork=checked(childWork+n);if(fullRaw+childWork>ml.MaximumWork)throw new InvalidDataException("Supported seed work");}
        Charge(0);var shadow=new List<Shadow>();
        foreach(var e in raw.SkillMods.Values){var key=e.Name.CopyBytes();int index=Find(shadow,key,Charge,out bool found);Charge(1);if(found)shadow[index]=new(key,e.BaseValue,e.Bonus);else{Charge(shadow.Count-index);shadow.Insert(index,new(key,e.BaseValue,e.Bonus));}}
        long normalizedKeys=shadow.Sum(e=>(long)e.Key.Length);Charge(shadow.Count+normalizedKeys);
        long finalWork=14L+raw.Encumbrances.Values.Count+raw.SpaceMissionObjects.Values.Count+shadow.Count+normalizedKeys;
        if(fullRaw+childWork+finalWork>ml.MaximumWork)throw new InvalidDataException("Supported seed final work");
        var map=CreatureSkillModsView4Kernel.FromBaseline(raw,new(ml.MaximumElements,ml.MaximumElements,ml.MaximumStringBytes,ml.MaximumTotalStringBytes,ml.MaximumStateBytes-other,ml.MaximumEvents,ml.MaximumOutputBytes-other,childWork));
        return new(Project(raw,map),map,revision,l);
    }
    public CreatureView4SupportedState Reseed(CreatureView4Baseline raw)
    {ArgumentNullException.ThrowIfNull(raw);if(raw.ObjectId!=ObjectId)throw new InvalidDataException("Supported reseed recipient");return FromBaseline(raw,limits,Revision);}
    public CreatureView4SupportedChange Apply(CreatureView4SupportedDelta plan,CreatureView4SupportedStateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);var l=Validate(operationLimits??limits);var ml=l.ModelLimits!;
        if(plan.ObjectId!=ObjectId||plan.Updates.Count>ml.MaximumUpdates)throw new InvalidDataException("Supported recipient/update cap");
        var message=CreatureView4SupportedCodec.Measure(plan,l.MessageLimits!);ulong revision=checked(Revision+1);
        var current=Model;var semantic=map;long work=0,eventBytes=0;var events=new List<CreatureView4SupportedEvent>();
        long Remaining()=>ml.MaximumWork-work;
        void Charge(long n){work=checked(work+n);if(work>ml.MaximumWork)throw new InvalidDataException("Supported work");}
        Charge(Seed(current)+message.Work);
        long Bytes()=>Other(current)+semantic.DecodedBytes;
        Gate(current,semantic.Entries.Count,semantic.Entries.Sum(e=>(long)e.Name.Length),Bytes(),0,0,ml);
        foreach(var e in semantic.Entries)if(e.Name.Length>ml.MaximumStringBytes)throw new InvalidDataException("Supported retained key cap");
        for(int ordinal=0;ordinal<plan.Updates.Count;ordinal++)
        {
            var u=plan.Updates[ordinal];
            if(u.Skill is not {} skill)
            {
                long extra=semantic.Entries.Sum(e=>16L-(9+(e.Name.UsesExtendedLength?6:2)));
                var modelPolicy=ml with{MaximumStateBytes=ml.MaximumStateBytes-extra,MaximumOutputBytes=ml.MaximumOutputBytes-eventBytes-extra,MaximumEvents=ml.MaximumEvents-events.Count,MaximumWork=Remaining()};
                Charge(Seed(current));modelPolicy=modelPolicy with{MaximumWork=Remaining()};
                var previous=CreatureView4ScalarEncumbranceState.FromBaseline(current,new(modelPolicy,new(8388608,4096,65536,1048576,16777216,16777216)));
                CreatureView4ScalarEncumbranceUpdate child=u.Scalar is {} s?new(s):new(u.Encumbrance!);
                var r=previous.Apply(new(ObjectId,plan.DeclaredCount,[child]),new(modelPolicy,new(8388608,4096,65536,1048576,16777216,16777216)));
                Charge(r.WorkUsed);current=r.Current.Model;
                foreach(var e in r.Events){events.Add(new(e.Kind==CreatureView4ScalarEncumbranceEventKind.Assignment?CreatureView4SupportedEventKind.Assignment:CreatureView4SupportedEventKind.EncumbranceMutation,e.Field,ordinal,e.Opcode,e.Index,e.OldBits,e.NewBits,e.OldValue,e.NewValue));eventBytes+=48;}
                continue;
            }
            // Bounded shadow predicts only reservations; actual mutation is delegated to the frozen kernel.
            long keys=semantic.Entries.Sum(e=>(long)e.Name.Length),other=Other(current),childWork=semantic.Entries.Count+keys+1+skill.Commands.Count+skill.Commands.Sum(c=>(long)c.Name.Length);
            void Local(long n){childWork=checked(childWork+n);if(childWork>Remaining())throw new InvalidDataException("Supported map predicted work");}
            Local(0);var shadow=semantic.Entries.Select(e=>new Shadow(e.Bytes.ToArray(),e.BaseValue,e.Bonus)).ToList();uint counter=semantic.Counter;
            uint skip=unchecked(counter+skill.Extent)<=skill.Counter?0:Math.Min(unchecked(counter-skill.Counter+skill.Extent),skill.Extent);
            int added=0;long addedKeys=0;
            for(int index=0;index<skill.Commands.Count;index++)
            {
                var c=skill.Commands[index];if((uint)index<skip)continue;
                if(c.Name.Length>ml.MaximumStringBytes)throw new InvalidDataException("Supported candidate key cap");
                var key=c.Name.CopyBytes();int at=Find(shadow,key,Local,out bool found);var old=found?shadow[at]:null;
                if(c.Opcode==1&&!found||c.Opcode!=1&&old is not null&&old.Base==c.BaseValue&&old.Bonus==c.Bonus)continue;
                int count=c.Opcode==1?shadow.Count-1:shadow.Count+(found?0:1);long nextKeys=keys+(c.Opcode==1?-key.Length:found?0:key.Length);
                int mappedCount=c.Opcode==1?shadow.Count:count;long mappedKeys=c.Opcode==1?keys:nextKeys;
                Gate(current,mappedCount,mappedKeys,other+8+16L*mappedCount+mappedKeys,events.Count+added+1,eventBytes+64L*(added+1)+addedKeys+key.Length,ml);
                int constructionCount=c.Opcode==1?shadow.Count:count;long constructionKeys=c.Opcode==1?keys:nextKeys;
                if(other+8+16L*constructionCount+constructionKeys+eventBytes+32L*(added+1)+addedKeys+key.Length>ml.MaximumOutputBytes)throw new InvalidDataException("Supported map construction output");
                Local(c.Opcode==1?shadow.Count-at:1+(found?0:shadow.Count-at));
                if(c.Opcode==1)shadow.RemoveAt(at);else if(found)shadow[at]=new(key,c.BaseValue,c.Bonus);else shadow.Insert(at,new(key,c.BaseValue,c.Bonus));
                keys=nextKeys;added++;addedKeys+=key.Length;counter=unchecked(counter+1);
            }
            counter=Math.Max(counter,skill.Counter);Local(shadow.Count+keys+added+addedKeys);Charge(childWork);
            // Full mapped peaks were reserved above; the frozen child retains its32+key construction phases.
            var mapPolicy=new CreatureSkillModsView4KernelLimits(Math.Min(ml.MaximumElements,ml.MaximumTotalElements-current.Encumbrances.Values.Count-current.SpaceMissionObjects.Values.Count),ml.MaximumElements,ml.MaximumStringBytes,ml.MaximumTotalStringBytes,ml.MaximumStateBytes-other,ml.MaximumEvents-events.Count,ml.MaximumOutputBytes-other-eventBytes,childWork,new(8388608,4096,65536,1048576,4194304,16777216,16777216));
            var result=semantic.Apply(new(ObjectId,plan.DeclaredCount,[skill]),mapPolicy);
            if(result.WorkUsed!=childWork||result.Events.Count!=added||result.Current.Counter!=counter)throw new InvalidDataException("Supported map prediction mismatch");
            semantic=result.Current;current=Project(current,semantic);
            foreach(var e in result.Events){var mapped=new CreatureView4SupportedEvent(CreatureView4SupportedEventKind.SkillMutation,3,ordinal,e.Opcode,Name:e.Name,OldBase:e.OldBase,OldBonus:e.OldBonus,NewBase:e.NewBase,NewBonus:e.NewBonus);events.Add(mapped);eventBytes+=mapped.LogicalBytes;}
        }
        Charge(Seed(current)+events.Count);Gate(current,semantic.Entries.Count,semantic.Entries.Sum(e=>(long)e.Name.Length),Bytes(),events.Count,eventBytes,ml);
        return new(new(current,semantic,revision,l),plan.DeclaredCount,events.ToArray(),work);
    }
}
