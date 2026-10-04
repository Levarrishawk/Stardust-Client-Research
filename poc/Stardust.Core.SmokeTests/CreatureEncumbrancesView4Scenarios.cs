using System.Text.Json;
using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;

public static class CreatureEncumbrancesView4Scenarios
{
    private static int negatives,budgets,ownership;
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Reject(Action action,bool budget=false)
    {
        try{action();}catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException)
        {if(budget)budgets++;else negatives++;return;}throw new Exception("Expected bounded rejection");
    }
    private static CreatureView4Baseline Seed(int[] values,uint counter,ulong id=0x1122334455667788)=>
        new(id,14,new uint[10],0,new(counter,values),new(0,Array.Empty<CreatureView4SkillMod>()),new(0,Array.Empty<CreatureView4MissionPair>()));
    private static CreatureEncumbrancesView4Limits Message=>new(8388608,4096,65536,1048576,16777216);
    private static CreatureEncumbrancesView4KernelLimits Kernel=>new(65536,16777216,262144,33554432,16777216,Message);
    private static bool Equal(CreatureEncumbrancesView4Kernel a,CreatureEncumbrancesView4Kernel b)=>
        a.ObjectId==b.ObjectId && a.Revision==b.Revision && a.Encumbrances.Counter==b.Encumbrances.Counter && a.Encumbrances.Values.SequenceEqual(b.Encumbrances.Values);
    public static void Run(string fixture,string output)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixture));var records=new List<object>();
        foreach(var c in document.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;byte[] wire=Convert.FromHexString(c.GetProperty("Wire").GetString()!);
            int[] seed=c.GetProperty("Seed").EnumerateArray().Select(x=>x.GetInt32()).ToArray();uint counter=c.GetProperty("SeedCounter").GetUInt32();
            var baseline=Seed(seed,counter);var state=CreatureEncumbrancesView4Kernel.FromBaseline(baseline,Kernel);
            var plan=CreatureEncumbrancesView4Codec.Decode(wire,Message);var encoded=CreatureEncumbrancesView4Codec.Encode(plan,Message);
            Check(wire.SequenceEqual(encoded),name+" wire");var change=state.Apply(plan);
            var expected=c.GetProperty("Expected");Check(change.Current.Encumbrances.Values.SequenceEqual(expected.GetProperty("Values").EnumerateArray().Select(x=>x.GetInt32())),name+" model");
            Check(change.Current.Encumbrances.Counter==expected.GetProperty("Counter").GetUInt32(),name+" counter");
            Check(change.WorkUsed==expected.GetProperty("WorkUsed").GetInt64(),name+" work");
            Check(change.DecodedOutputBytes==expected.GetProperty("DecodedOutputBytes").GetInt64(),name+" output");
            Check(JsonSerializer.Serialize(change.Events)==JsonSerializer.Serialize(expected.GetProperty("Events")),name+" events");
            Check(ReferenceEquals(change.Previous,state) && state.Revision==1 && change.Current.Revision==2 && state.Encumbrances.Values.SequenceEqual(seed),name+" retained");ownership++;
            for(int n=0;n<wire.Length;n+=wire.Length>1024?Math.Max(1,wire.Length/100):1){int cut=n;Reject(()=>CreatureEncumbrancesView4Codec.Decode(wire.AsSpan(0,cut),Message));}
            byte[] fields=ReplicationEnvelopeCodec.DecodeDelta(wire,8388608).CopyOpaqueFields();
            // Every inner truncation bypasses the outer length check and exercises the narrow scanner.
            for(int n=0;n<fields.Length;n+=fields.Length>1024?Math.Max(1,fields.Length/100):1)
            {
                int cut=n;var e=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView4Codec.RawTag,4,65535,fields.AsSpan(0,cut),8388608);
                try{var partial=CreatureEncumbrancesView4Codec.Decode(e,Message);Check(cut>0 && partial.Updates.Count<plan.Updates.Count,"Only complete update boundaries may decode");}
                catch(InvalidDataException){negatives++;}
            }
            long values=plan.Updates.Sum(u=>(long)u.Commands.Sum(x=>x.Values.Count));int commands=plan.Updates.Sum(u=>u.Commands.Count);
            var exact=Message with{MaximumPayloadBytes=fields.Length+2,MaximumFields=plan.Updates.Count,MaximumCommands=commands,MaximumValues=checked((int)values),MaximumDecodedBytes=fields.Length};
            Check(CreatureEncumbrancesView4Codec.Encode(plan,exact).SequenceEqual(wire),name+" exact codec");
            Check(CreatureEncumbrancesView4Codec.Decode(wire,exact).Updates.Count==plan.Updates.Count,name+" exact scanner");
            foreach(var low in new[]{exact with{MaximumPayloadBytes=fields.Length+1},exact with{MaximumFields=plan.Updates.Count-1},exact with{MaximumDecodedBytes=fields.Length-1}})
            {Reject(()=>CreatureEncumbrancesView4Codec.Decode(wire,low),true);Reject(()=>CreatureEncumbrancesView4Codec.Encode(plan,low),true);}
            if(commands>0){var low=exact with{MaximumCommands=commands-1};Reject(()=>CreatureEncumbrancesView4Codec.Decode(wire,low),true);Reject(()=>CreatureEncumbrancesView4Codec.Encode(plan,low),true);}
            if(values>0){var low=exact with{MaximumValues=checked((int)values-1)};Reject(()=>CreatureEncumbrancesView4Codec.Decode(wire,low),true);Reject(()=>CreatureEncumbrancesView4Codec.Encode(plan,low),true);}
            int peak=expected.GetProperty("Peak").GetInt32(),eventCount=change.Events.Count;long peakOutput=expected.GetProperty("PeakOutput").GetInt64();
            var exactKernel=Kernel with{MaximumElements=peak,MaximumStateBytes=8+4L*peak,MaximumEvents=eventCount,MaximumOutputBytes=peakOutput,MaximumWork=change.WorkUsed};
            Check(Equal(state.Apply(plan,exactKernel).Current,change.Current),name+" exact kernel");
            var lows=new List<CreatureEncumbrancesView4KernelLimits>{exactKernel with{MaximumWork=change.WorkUsed-1}};
            if(peak>0){lows.Add(exactKernel with{MaximumElements=peak-1});lows.Add(exactKernel with{MaximumStateBytes=7+4L*peak});}
            if(eventCount>0)lows.Add(exactKernel with{MaximumEvents=eventCount-1});
            if(peakOutput>8)lows.Add(exactKernel with{MaximumOutputBytes=peakOutput-1});
            foreach(var low in lows){Reject(()=>state.Apply(plan,low),true);Check(state.Revision==1 && state.Encumbrances.Counter==counter && state.Encumbrances.Values.SequenceEqual(seed),name+" failed apply retained");ownership++;}
            byte[] alien=(byte[])wire.Clone();alien[6]^=1;Reject(()=>state.Apply(CreatureEncumbrancesView4Codec.Decode(alien,Message)));ownership++;
            byte[] wrongField=(byte[])wire.Clone();wrongField[25]=3;Reject(()=>CreatureEncumbrancesView4Codec.Decode(wrongField,Message));
            foreach(int offset in new[]{2,14,18,23}){byte[] wrong=(byte[])wire.Clone();wrong[offset]^=0x7f;if(offset==23){Check(CreatureEncumbrancesView4Codec.Decode(wrong,Message).DeclaredCount!=plan.DeclaredCount,"Metadata preserved, not iteration count");}else Reject(()=>CreatureEncumbrancesView4Codec.Decode(wrong,Message));}
            records.Add(new{Name=name,Wire=Convert.ToHexString(encoded).ToLowerInvariant(),Values=change.Current.Encumbrances.Values,Counter=change.Current.Encumbrances.Counter,change.Events,change.WorkUsed,change.DecodedOutputBytes});
        }
        var full=CreatureEncumbrancesView4Kernel.FromBaseline(Seed(new int[65536],0),Kernel);
        var overflowInsert=new CreatureEncumbrancesView4Delta(full.ObjectId,1,[new(2,1,1,[new(1,65535,[9])])]);
        Reject(()=>full.Apply(overflowInsert),true);Check(full.Encumbrances.Values.Count==65536 && full.Encumbrances.Counter==0,"Reject growth beyond modern max, retain full seed");ownership++;
        Reject(()=>new CreatureEncumbrancesView4Command(3,0,new int[65536]));
        // Hostile DTOs, snapshot identity/revision, caller aliases, malformed command stream.
        Reject(()=>new CreatureEncumbrancesView4Command(5,0,[]));Reject(()=>new CreatureEncumbrancesView4Command(0,0,[1]));Reject(()=>new CreatureEncumbrancesView4Command(1,0,[]));Reject(()=>new CreatureEncumbrancesView4Command(3,1,[]));Reject(()=>new CreatureEncumbrancesView4Update(3,0,0,[]));
        var empty=new CreatureEncumbrancesView4Delta(0,0,[]);Reject(()=>CreatureEncumbrancesView4Codec.Encode(empty,Message));
        var extra=new CreatureEncumbrancesView4Delta(0,0,[new(2,0,0,[new(4,0,[])])]);Reject(()=>CreatureEncumbrancesView4Codec.Encode(extra,Message));
        var missing=new CreatureEncumbrancesView4Delta(0,0,[new(2,1,0,[])]);Reject(()=>CreatureEncumbrancesView4Codec.Encode(missing,Message));
        int[] alias=[7];var command=new CreatureEncumbrancesView4Command(2,0,alias);alias[0]=9;Check(command.Values[0]==7,"Command owns values");ownership++;
        CreatureEncumbrancesView4Command[] cmds=[command];var update=new CreatureEncumbrancesView4Update(2,1,1,cmds);cmds[0]=new(4,0,[]);Check(update.Commands[0].Opcode==2,"Update owns commands");ownership++;
        CreatureEncumbrancesView4Update[] updates=[update];var delta=new CreatureEncumbrancesView4Delta(0x1122334455667788,0,updates);updates[0]=new(2,0,0,[]);Check(delta.Updates[0].Extent==1,"Delta owns updates");ownership++;
        int[] seedAlias=[1];var own=CreatureEncumbrancesView4Kernel.FromBaseline(Seed(seedAlias,0),Kernel);seedAlias[0]=999;Check(own.Encumbrances.Values[0]==1,"Seed owns values");ownership++;
        Check(own.Reseed(Seed([5],8)).Revision==2,"Reseed revision");Reject(()=>own.Reseed(Seed([],0,0)));
        Reject(()=>CreatureEncumbrancesView4Kernel.FromBaseline(Seed([],0),Kernel,ulong.MaxValue));var last=CreatureEncumbrancesView4Kernel.FromBaseline(Seed([1],0),Kernel,ulong.MaxValue-1);Reject(()=>last.Apply(delta));
        Reject(()=>CreatureEncumbrancesView4Kernel.FromBaseline(Seed([1],0),Kernel with{MaximumElements=0}),true);
        Reject(()=>CreatureEncumbrancesView4Kernel.FromBaseline(Seed([1],0),Kernel with{MaximumWork=0}),true);
        foreach(byte op in Enumerable.Range(5,251).Select(x=>(byte)x))
        {
            byte[] fields=new byte[11];BinaryPrimitives.WriteUInt16LittleEndian(fields,2);BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(2),1);fields[10]=op;
            Reject(()=>CreatureEncumbrancesView4Codec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,0,CreatureView4Codec.RawTag,4,1,fields,8388608),Message));
        }
        Reject(()=>CreatureEncumbrancesView4Codec.Decode([],Message with{MaximumCommands=-1}));Reject(()=>own.Apply(delta,Kernel with{MaximumElements=65537}));
        File.WriteAllText(output,JsonSerializer.Serialize(new{Records=records,Negatives=negatives,Budgets=budgets,Ownership=ownership})+"\n");
        Console.WriteLine($"PASS CREO4 field2: cases={records.Count} negatives={negatives} budgets={budgets} ownership={ownership}");
    }
}
