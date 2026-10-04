using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureMissionObjectsView4Scenarios
{
    private static int negatives,budgets,ownership;
    private static void Check(bool value,string name){if(!value)throw new Exception(name);}
    private static void Reject(Action action,bool budget=false)
    {try{action();}catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException){if(budget)budgets++;else negatives++;return;}throw new Exception("Expected mission rejection");}
    private static CreatureMissionObjectsView4Limits Message=>new(8388608,4096,65536,16777216,16777216);
    private static CreatureMissionObjectsView4KernelLimits Kernel=>new(65536,65536,16777216,262144,33554432,16777216,Message);
    private const ulong Id=0x1122334455667788;
    private static CreatureView4Baseline Seed(CreatureView4MissionPair[] pairs,uint counter,ulong id=Id)=>new(id,14,new uint[10],0,new(0,Array.Empty<int>()),new(0,Array.Empty<CreatureView4SkillMod>()),new(counter,pairs));
    private static bool Equal(CreatureMissionObjectsView4Kernel a,CreatureMissionObjectsView4Kernel b)=>a.ObjectId==b.ObjectId && a.Revision==b.Revision && a.Counter==b.Counter && a.Pairs.SequenceEqual(b.Pairs);
    public static void Run(string fixture,string output)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixture));var records=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;var raw=c.GetProperty("Seed").EnumerateArray().Select(e=>new CreatureView4MissionPair(e.GetProperty("OwnerId").GetUInt64(),e.GetProperty("MissionId").GetUInt64())).ToArray();uint counter=c.GetProperty("SeedCounter").GetUInt32();
            var baseline=Seed(raw,counter);var state=CreatureMissionObjectsView4Kernel.FromBaseline(baseline,Kernel);var expected=c.GetProperty("Expected");var initial=state.Pairs;
            Check(JsonSerializer.Serialize(initial)==JsonSerializer.Serialize(expected.GetProperty("Initial")),name+" signed full-pair snapshot set");long seedWork=expected.GetProperty("SeedWork").GetInt64();Check(Equal(CreatureMissionObjectsView4Kernel.FromBaseline(baseline,Kernel with{MaximumWork=seedWork}),state),name+" exact seed work");
            if(seedWork>0)Reject(()=>CreatureMissionObjectsView4Kernel.FromBaseline(baseline,Kernel with{MaximumWork=seedWork-1}),true);if(raw.Length>0)Reject(()=>CreatureMissionObjectsView4Kernel.FromBaseline(baseline,Kernel with{MaximumSeedPairs=raw.Length-1}),true);
            byte[] wire=Convert.FromHexString(c.GetProperty("Wire").GetString()!);var plan=CreatureMissionObjectsView4Codec.Decode(wire,Message);byte[] encoded=CreatureMissionObjectsView4Codec.Encode(plan,Message);Check(wire.SequenceEqual(encoded),name+" wire");var change=state.Apply(plan);
            Check(change.Current.Counter==expected.GetProperty("Counter").GetUInt32(),name+" counter");Check(change.WorkUsed==expected.GetProperty("WorkUsed").GetInt64(),name+" work");Check(change.DecodedOutputBytes==expected.GetProperty("DecodedOutputBytes").GetInt64(),name+" output");
            Check(JsonSerializer.Serialize(change.Current.Pairs)==JsonSerializer.Serialize(expected.GetProperty("Pairs")),name+" pairs");Check(JsonSerializer.Serialize(change.Events)==JsonSerializer.Serialize(expected.GetProperty("Events")),name+" events");
            Check(ReferenceEquals(change.Previous,state) && state.Revision==1 && change.Current.Revision==2 && state.Counter==counter && state.Pairs.SequenceEqual(initial),name+" retained");ownership++;
            for(int n=0;n<wire.Length;n+=wire.Length>1024?Math.Max(1,wire.Length/100):1){int cut=n;Reject(()=>CreatureMissionObjectsView4Codec.Decode(wire.AsSpan(0,cut),Message));}
            byte[] fields=ReplicationEnvelopeCodec.DecodeDelta(wire,8388608).CopyOpaqueFields();
            for(int n=0;n<fields.Length;n+=fields.Length>1024?Math.Max(1,fields.Length/100):1)
            {
                int cut=n;var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,Id,CreatureView4Codec.RawTag,4,65535,fields.AsSpan(0,cut),8388608);
                try{var partial=CreatureMissionObjectsView4Codec.Decode(envelope,Message);Check(cut>0 && partial.Updates.Count<plan.Updates.Count,"Only complete record boundaries accepted");}catch(InvalidDataException){negatives++;}
            }
            int commands=plan.Updates.Sum(u=>u.Commands.Count);var exact=Message with{MaximumPayloadBytes=fields.Length+2,MaximumFields=plan.Updates.Count,MaximumCommands=commands,MaximumDecodedBytes=fields.Length+24L*commands,MaximumWork=plan.Updates.Count+commands};
            Check(CreatureMissionObjectsView4Codec.Encode(plan,exact).SequenceEqual(wire),name+" exact encode");Check(CreatureMissionObjectsView4Codec.Decode(wire,exact).Updates.Count==plan.Updates.Count,name+" exact decode");
            var messageLows=new List<CreatureMissionObjectsView4Limits>{exact with{MaximumPayloadBytes=fields.Length+1},exact with{MaximumFields=plan.Updates.Count-1},exact with{MaximumDecodedBytes=exact.MaximumDecodedBytes-1},exact with{MaximumWork=exact.MaximumWork-1}};if(commands>0)messageLows.Add(exact with{MaximumCommands=commands-1});
            foreach(var low in messageLows){Reject(()=>CreatureMissionObjectsView4Codec.Decode(wire,low),true);Reject(()=>CreatureMissionObjectsView4Codec.Encode(plan,low),true);}
            int peak=expected.GetProperty("Peak").GetInt32();long peakBytes=expected.GetProperty("PeakBytes").GetInt64(),peakOutput=expected.GetProperty("PeakOutput").GetInt64();
            var exactKernel=Kernel with{MaximumPairs=peak,MaximumStateBytes=peakBytes,MaximumEvents=change.Events.Count,MaximumOutputBytes=peakOutput,MaximumWork=change.WorkUsed};Check(Equal(state.Apply(plan,exactKernel).Current,change.Current),name+" exact kernel");
            var lows=new List<CreatureMissionObjectsView4KernelLimits>{exactKernel with{MaximumWork=change.WorkUsed-1}};if(peak>0)lows.Add(exactKernel with{MaximumPairs=peak-1});if(peakBytes>8)lows.Add(exactKernel with{MaximumStateBytes=peakBytes-1});if(change.Events.Count>0)lows.Add(exactKernel with{MaximumEvents=change.Events.Count-1});if(peakOutput>8)lows.Add(exactKernel with{MaximumOutputBytes=peakOutput-1});
            foreach(var low in lows){Reject(()=>state.Apply(plan,low),true);Check(state.Counter==counter && state.Revision==1 && state.Pairs.SequenceEqual(initial),name+" failed retained");ownership++;}
            byte[] alien=(byte[])wire.Clone();alien[6]^=1;Reject(()=>state.Apply(CreatureMissionObjectsView4Codec.Decode(alien,Message)));ownership++;
            byte[] wrong=(byte[])wire.Clone();wrong[25]=3;Reject(()=>CreatureMissionObjectsView4Codec.Decode(wrong,Message));
            foreach(int offset in new[]{2,14,18}){wrong=(byte[])wire.Clone();wrong[offset]^=0x7f;byte[] invalid=wrong;Reject(()=>CreatureMissionObjectsView4Codec.Decode(invalid,Message));}
            wrong=(byte[])wire.Clone();wrong[23]^=0x7f;Check(CreatureMissionObjectsView4Codec.Decode(wrong,Message).DeclaredCount!=plan.DeclaredCount,"Raw metadata retained");
            records.Add(new{Name=name,Wire=Convert.ToHexString(encoded).ToLowerInvariant(),Initial=initial,Pairs=change.Current.Pairs,Counter=change.Current.Counter,change.Events,change.WorkUsed,change.DecodedOutputBytes});
        }
        var pair=new CreatureView4MissionPair(ulong.MaxValue,1UL<<63);var command=new CreatureMissionObjectsView4Command(1,pair);var aliasCommands=new[]{command};var update=new CreatureMissionObjectsView4Update(13,1,1,aliasCommands);aliasCommands[0]=new(2,null);Check(update.Commands[0].Opcode==1,"Own commands");ownership++;
        var aliasUpdates=new[]{update};var delta=new CreatureMissionObjectsView4Delta(Id,1,aliasUpdates);aliasUpdates[0]=new(13,0,0,[]);Check(delta.Updates[0].Extent==1,"Own updates");ownership++;
        var aliases=new[]{pair};var baselineAlias=Seed(aliases,0);aliases[0]=new(0,0);var stateAlias=CreatureMissionObjectsView4Kernel.FromBaseline(baselineAlias,Kernel);Check(stateAlias.Pairs[0]==pair,"Own raw pair values");ownership++;
        var published=CreatureMissionObjectsView4Kernel.FromBaseline(Seed([],0),Kernel).Apply(delta);Check(published.Current.Pairs is IList<CreatureView4MissionPair> pairs && pairs.IsReadOnly,"Readonly pairs");Check(published.Events is IList<CreatureMissionObjectsView4Event> events && events.IsReadOnly,"Readonly events");ownership+=2;
        Check(stateAlias.Reseed(Seed([],7)).Revision==2,"Reseed revision");Reject(()=>stateAlias.Reseed(Seed([],0,0)));Reject(()=>CreatureMissionObjectsView4Kernel.FromBaseline(Seed([],0),Kernel,ulong.MaxValue));var last=CreatureMissionObjectsView4Kernel.FromBaseline(Seed([],0),Kernel,ulong.MaxValue-1);Reject(()=>last.Apply(delta));
        Reject(()=>new CreatureMissionObjectsView4Command(3,null));Reject(()=>new CreatureMissionObjectsView4Command(2,pair));Reject(()=>new CreatureMissionObjectsView4Command(0,null));Reject(()=>new CreatureMissionObjectsView4Update(3,0,0,[]));Reject(()=>CreatureMissionObjectsView4Codec.Encode(new(Id,0,[]),Message));Reject(()=>CreatureMissionObjectsView4Codec.Encode(new(Id,0,[new(13,2,2,[command])]),Message));
        byte[] field=ReplicationEnvelopeCodec.DecodeDelta(CreatureMissionObjectsView4Codec.Encode(delta,Message),8388608).CopyOpaqueFields();byte[] late=new byte[field.Length+2];field.CopyTo(late,0);late[^2]=3;Reject(()=>CreatureMissionObjectsView4Codec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,Id,CreatureView4Codec.RawTag,4,2,late,8388608),Message));
        foreach(byte op in Enumerable.Range(3,253).Select(x=>(byte)x)){byte[] invalid=(byte[])field.Clone();invalid[10]=op;Reject(()=>CreatureMissionObjectsView4Codec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,Id,CreatureView4Codec.RawTag,4,1,invalid,8388608),Message));}
        var empty=CreatureMissionObjectsView4Kernel.FromBaseline(Seed([],0),Kernel);var latePlan=new CreatureMissionObjectsView4Delta(Id,2,[update,new(13,1,2,[new(1,new CreatureView4MissionPair(1,2))])]);Reject(()=>empty.Apply(latePlan,Kernel with{MaximumPairs=1}),true);Check(empty.Counter==0 && empty.Pairs.Count==0 && empty.Revision==1,"Late rollback");ownership++;
        Reject(()=>CreatureMissionObjectsView4Codec.Decode([],Message with{MaximumCommands=-1}));Reject(()=>empty.Apply(delta,Kernel with{MaximumPairs=65537}));
        File.WriteAllText(output,JsonSerializer.Serialize(new{Records=records,Negatives=negatives,Budgets=budgets,Ownership=ownership})+"\n");Console.WriteLine($"PASS CREO4 mission set cases={records.Count} negatives={negatives} budgets={budgets} ownership={ownership}");
    }
}
