using System.Text.Json;
using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureSkillModsView4Scenarios
{
    private static int negatives,budgets,ownership;
    private static void Check(bool value,string name){if(!value)throw new Exception(name);}
    private static void Reject(Action action,bool budget=false)
    {try{action();}catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException){if(budget)budgets++;else negatives++;return;}throw new Exception("Expected skill rejection");}
    private static CreatureSkillModsView4Limits Message=>new(8388608,4096,65536,1048576,4194304,16777216,16777216);
    private static CreatureSkillModsView4KernelLimits Kernel=>new(65536,65536,1048576,4194304,16777216,262144,33554432,16777216,Message);
    private const ulong Id=0x1122334455667788;
    private static CreatureView4Baseline Seed(CreatureView4SkillMod[] values,uint counter,ulong id=Id)=>new(id,14,new uint[10],0,new(0,Array.Empty<int>()),new(counter,values),new(0,Array.Empty<CreatureView4MissionPair>()));
    private static object[] Entries(CreatureSkillModsView4Kernel state)=>state.Entries.Select(e=>(object)new{Key=Convert.ToHexString(e.Name.CopyBytes()).ToLowerInvariant(),Extended=e.Name.UsesExtendedLength,e.BaseValue,e.Bonus}).ToArray();
    private static object[] Events(CreatureSkillModsView4Change change)=>change.Events.Select(e=>(object)new{e.Opcode,Key=Convert.ToHexString(e.Name.CopyBytes()).ToLowerInvariant(),Extended=e.Name.UsesExtendedLength,e.OldBase,e.OldBonus,e.NewBase,e.NewBonus}).ToArray();
    private static bool Equal(CreatureSkillModsView4Kernel a,CreatureSkillModsView4Kernel b)=>a.ObjectId==b.ObjectId && a.Revision==b.Revision && a.Counter==b.Counter && JsonSerializer.Serialize(Entries(a))==JsonSerializer.Serialize(Entries(b));
    public static void Run(string fixture,string output)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixture));var records=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;var raw=c.GetProperty("Seed").EnumerateArray().Select(e=>new CreatureView4SkillMod(e.GetProperty("RawCommand").GetByte(),ReplicationByteString.Create(Convert.FromHexString(e.GetProperty("Key").GetString()!),e.GetProperty("Extended").GetBoolean()),e.GetProperty("BaseValue").GetInt32(),e.GetProperty("Bonus").GetInt32())).ToArray();
            uint counter=c.GetProperty("SeedCounter").GetUInt32();var baseline=Seed(raw,counter);var state=CreatureSkillModsView4Kernel.FromBaseline(baseline,Kernel);var expected=c.GetProperty("Expected");
            object[] initial=Entries(state);Check(JsonSerializer.Serialize(initial)==JsonSerializer.Serialize(expected.GetProperty("Initial")),name+" snapshot materialization");
            long seedWork=expected.GetProperty("SeedWork").GetInt64();Check(Equal(CreatureSkillModsView4Kernel.FromBaseline(baseline,Kernel with{MaximumWork=seedWork}),state),name+" exact seed work");
            if(seedWork>0)Reject(()=>CreatureSkillModsView4Kernel.FromBaseline(baseline,Kernel with{MaximumWork=seedWork-1}),true);
            if(raw.Length>0)Reject(()=>CreatureSkillModsView4Kernel.FromBaseline(baseline,Kernel with{MaximumSeedEntries=raw.Length-1}),true);
            long rawKeys=raw.Sum(e=>(long)e.Name.Length);if(rawKeys>0)Reject(()=>CreatureSkillModsView4Kernel.FromBaseline(baseline,Kernel with{MaximumTotalKeyBytes=checked((int)rawKeys-1)}),true);
            byte[] wire=Convert.FromHexString(c.GetProperty("Wire").GetString()!);var plan=CreatureSkillModsView4Codec.Decode(wire,Message);byte[] encoded=CreatureSkillModsView4Codec.Encode(plan,Message);Check(wire.SequenceEqual(encoded),name+" raw wire");
            var change=state.Apply(plan);Check(change.Current.Counter==expected.GetProperty("Counter").GetUInt32(),name+" counter");Check(change.WorkUsed==expected.GetProperty("WorkUsed").GetInt64(),name+" work");Check(change.DecodedOutputBytes==expected.GetProperty("DecodedOutputBytes").GetInt64(),name+" output");
            Check(JsonSerializer.Serialize(Entries(change.Current))==JsonSerializer.Serialize(expected.GetProperty("Entries")),name+" entries");Check(JsonSerializer.Serialize(Events(change))==JsonSerializer.Serialize(expected.GetProperty("Events")),name+" events");
            Check(ReferenceEquals(change.Previous,state) && state.Revision==1 && change.Current.Revision==2 && state.Counter==counter && JsonSerializer.Serialize(Entries(state))==JsonSerializer.Serialize(initial),name+" retained");ownership++;
            for(int n=0;n<wire.Length;n+=wire.Length>1024?Math.Max(1,wire.Length/100):1){int cut=n;Reject(()=>CreatureSkillModsView4Codec.Decode(wire.AsSpan(0,cut),Message));}
            byte[] fields=ReplicationEnvelopeCodec.DecodeDelta(wire,8388608).CopyOpaqueFields();
            for(int n=0;n<fields.Length;n+=fields.Length>1024?Math.Max(1,fields.Length/100):1)
            {
                int cut=n;var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,Id,CreatureView4Codec.RawTag,4,65535,fields.AsSpan(0,cut),8388608);
                try{var partial=CreatureSkillModsView4Codec.Decode(envelope,Message);Check(cut>0 && partial.Updates.Count<plan.Updates.Count,"Only complete field boundaries accepted");}catch(InvalidDataException){negatives++;}
            }
            int commands=plan.Updates.Sum(u=>u.Commands.Count);long totalKeys=plan.Updates.Sum(u=>u.Commands.Sum(x=>(long)x.Name.Length));int maxKey=plan.Updates.SelectMany(u=>u.Commands).Select(x=>x.Name.Length).DefaultIfEmpty(0).Max();
            var exact=Message with{MaximumPayloadBytes=fields.Length+2,MaximumFields=plan.Updates.Count,MaximumCommands=commands,MaximumKeyBytes=maxKey,MaximumTotalKeyBytes=checked((int)totalKeys),MaximumDecodedBytes=fields.Length+24L*commands,MaximumWork=plan.Updates.Count+commands+totalKeys};
            Check(CreatureSkillModsView4Codec.Encode(plan,exact).SequenceEqual(wire),name+" exact encode");Check(CreatureSkillModsView4Codec.Decode(wire,exact).Updates.Count==plan.Updates.Count,name+" exact decode");
            var messageLows=new List<CreatureSkillModsView4Limits>{exact with{MaximumPayloadBytes=fields.Length+1},exact with{MaximumFields=plan.Updates.Count-1},exact with{MaximumDecodedBytes=exact.MaximumDecodedBytes-1},exact with{MaximumWork=exact.MaximumWork-1}};
            if(commands>0)messageLows.Add(exact with{MaximumCommands=commands-1});if(maxKey>0)messageLows.Add(exact with{MaximumKeyBytes=maxKey-1});if(totalKeys>0)messageLows.Add(exact with{MaximumTotalKeyBytes=checked((int)totalKeys-1)});
            foreach(var low in messageLows){Reject(()=>CreatureSkillModsView4Codec.Decode(wire,low),true);Reject(()=>CreatureSkillModsView4Codec.Encode(plan,low),true);}
            int peakCount=expected.GetProperty("PeakCount").GetInt32(),peakKeys=expected.GetProperty("PeakKeys").GetInt32(),peakKey=expected.GetProperty("PeakKey").GetInt32();long peakBytes=expected.GetProperty("PeakBytes").GetInt64(),peakOutput=expected.GetProperty("PeakOutput").GetInt64();
            var exactKernel=Kernel with{MaximumEntries=peakCount,MaximumTotalKeyBytes=peakKeys,MaximumKeyBytes=peakKey,MaximumStateBytes=peakBytes,MaximumEvents=change.Events.Count,MaximumOutputBytes=peakOutput,MaximumWork=change.WorkUsed};
            Check(Equal(state.Apply(plan,exactKernel).Current,change.Current),name+" exact kernel");var lows=new List<CreatureSkillModsView4KernelLimits>{exactKernel with{MaximumWork=change.WorkUsed-1}};
            if(peakCount>0)lows.Add(exactKernel with{MaximumEntries=peakCount-1});if(peakKeys>0)lows.Add(exactKernel with{MaximumTotalKeyBytes=peakKeys-1});if(peakKey>0)lows.Add(exactKernel with{MaximumKeyBytes=peakKey-1});if(peakBytes>8)lows.Add(exactKernel with{MaximumStateBytes=peakBytes-1});if(change.Events.Count>0)lows.Add(exactKernel with{MaximumEvents=change.Events.Count-1});if(peakOutput>8)lows.Add(exactKernel with{MaximumOutputBytes=peakOutput-1});
            foreach(var low in lows){Reject(()=>state.Apply(plan,low),true);Check(state.Counter==counter && state.Revision==1 && JsonSerializer.Serialize(Entries(state))==JsonSerializer.Serialize(initial),name+" failed retained");ownership++;}
            byte[] alien=(byte[])wire.Clone();alien[6]^=1;Reject(()=>state.Apply(CreatureSkillModsView4Codec.Decode(alien,Message)));ownership++;
            byte[] wrong=(byte[])wire.Clone();wrong[25]=2;Reject(()=>CreatureSkillModsView4Codec.Decode(wrong,Message));
            foreach(int offset in new[]{2,14,18}){wrong=(byte[])wire.Clone();wrong[offset]^=0x7f;byte[] invalid=wrong;Reject(()=>CreatureSkillModsView4Codec.Decode(invalid,Message));}
            wrong=(byte[])wire.Clone();wrong[23]^=0x7f;Check(CreatureSkillModsView4Codec.Decode(wrong,Message).DeclaredCount!=plan.DeclaredCount,"Raw metadata retained");
            records.Add(new{Name=name,Wire=Convert.ToHexString(encoded).ToLowerInvariant(),Initial=initial,Entries=Entries(change.Current),Counter=change.Current.Counter,Events=Events(change),change.WorkUsed,change.DecodedOutputBytes});
        }
        // Late field/command, owned byte inputs, revision and immutable exposed collections.
        var key=ReplicationByteString.Create([0xff,0],false);var command=new CreatureSkillModsView4Command(0,key,1,2);var aliasCommands=new[]{command};var update=new CreatureSkillModsView4Update(3,1,1,aliasCommands);aliasCommands[0]=new(1,key,9,9);Check(update.Commands[0].Opcode==0,"Own commands");ownership++;
        var updates=new[]{update};var planAlias=new CreatureSkillModsView4Delta(Id,1,updates);updates[0]=new(3,0,0,[]);Check(planAlias.Updates[0].Extent==1,"Own updates");ownership++;
        byte[] copy=key.CopyBytes();copy[0]=0;Check(key.CopyBytes()[0]==255,"Own bytes");ownership++;
        var stateAlias=CreatureSkillModsView4Kernel.FromBaseline(Seed([],0),Kernel);var published=stateAlias.Apply(planAlias);copy=published.Current.Entries[0].Name.CopyBytes();copy[0]=0;Check(published.Current.Entries[0].Name.CopyBytes()[0]==255,"Published bytes copied");ownership++;
        Check(published.Current.Entries is IList<CreatureSkillModsView4Entry> list && list.IsReadOnly,"Readonly entries");Check(published.Events is IList<CreatureSkillModsView4Event> events && events.IsReadOnly,"Readonly events");ownership+=2;
        Check(stateAlias.Reseed(Seed([],7)).Revision==2,"Reseed revision");Reject(()=>stateAlias.Reseed(Seed([],0,0)));Reject(()=>CreatureSkillModsView4Kernel.FromBaseline(Seed([],0),Kernel,ulong.MaxValue));var last=CreatureSkillModsView4Kernel.FromBaseline(Seed([],0),Kernel,ulong.MaxValue-1);Reject(()=>last.Apply(planAlias));
        Reject(()=>new CreatureSkillModsView4Command(3,key,0,0));Reject(()=>new CreatureSkillModsView4Update(2,0,0,[]));Reject(()=>CreatureSkillModsView4Codec.Encode(new(Id,0,[]),Message));Reject(()=>CreatureSkillModsView4Codec.Encode(new(Id,0,[new(3,2,2,[command])]),Message));
        byte[] field=ReplicationEnvelopeCodec.DecodeDelta(CreatureSkillModsView4Codec.Encode(planAlias,Message),8388608).CopyOpaqueFields();
        byte[] late=new byte[field.Length+2];field.CopyTo(late,0);late[^2]=2;Reject(()=>CreatureSkillModsView4Codec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,Id,CreatureView4Codec.RawTag,4,2,late,8388608),Message));
        foreach(byte op in Enumerable.Range(3,253).Select(x=>(byte)x)){byte[] invalid=(byte[])field.Clone();invalid[10]=op;Reject(()=>CreatureSkillModsView4Codec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,Id,CreatureView4Codec.RawTag,4,1,invalid,8388608),Message));}
        var latePlan=new CreatureSkillModsView4Delta(Id,2,[update,new(3,1,2,[new(0,ReplicationByteString.Create(new byte[100],false),3,4)])]);Reject(()=>stateAlias.Apply(latePlan,Kernel with{MaximumKeyBytes=2}),true);Check(stateAlias.Counter==0 && stateAlias.Entries.Count==0 && stateAlias.Revision==1,"Late apply rollback");ownership++;
        Reject(()=>CreatureSkillModsView4Codec.Decode([],Message with{MaximumCommands=-1}));Reject(()=>stateAlias.Apply(planAlias,Kernel with{MaximumEntries=65537}));
        File.WriteAllText(output,JsonSerializer.Serialize(new{Records=records,Negatives=negatives,Budgets=budgets,Ownership=ownership})+"\n");Console.WriteLine($"PASS CREO4 skill map cases={records.Count} negatives={negatives} budgets={budgets} ownership={ownership}");
    }
}
