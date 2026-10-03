using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Nodes;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class SceneTangibleView6StateScenarios
{
    private static readonly TangibleView6Limits CodecLimits=new();
    private static object Row(TangibleView6State s)=>new{s.ObjectId,s.Passive,s.Counter,Ids=s.Ids.ToArray(),s.Revision,s.DecodedBytes};
    private static object[] Events(TangibleView6StateChange change)=>change.Events.Select(e=>(object)new{Kind=e.Kind.ToString(),e.Index,e.OldId,e.NewId}).ToArray();
    public static void Run(string peerPath,string exportPath)
    {
        if(new FileInfo(peerPath).Length>1048576)throw new InvalidDataException("Bounded state fixture");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(peerPath));var records=new List<object>();int negatives=0,budgets=0,ownership=0;
        void Check(bool v,string reason){if(!v)throw new Exception(reason);}
        void Equal(object v,JsonElement e,string reason)=>Check(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(v),JsonNode.Parse(e.GetRawText())),reason);
        void Reject(Action action){negatives++;try{action();}catch(InvalidDataException){return;}catch(OverflowException){return;}catch(ArgumentOutOfRangeException){return;}throw new Exception("Negative state accepted");}
        byte[] Bytes(JsonElement e,string key)=>Convert.FromHexString(e.GetProperty(key).GetString()!);
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            var baselineWire=Bytes(c,"Baseline");var baseline=TangibleView6Codec.DecodeBaseline(baselineWire,CodecLimits);Check(TangibleView6Codec.EncodeBaseline(baseline,CodecLimits).SequenceEqual(baselineWire),"Exact seed wire");
            var alias=baseline.Ids.ToArray();var frozen=new TangibleView6Baseline(baseline.ObjectId,baseline.DeclaredCount,baseline.Passive,baseline.Counter,alias);Array.Clear(alias);
            var state=TangibleView6State.FromBaseline(frozen);Equal(Row(state),c.GetProperty("Initial"),"Independent seed");Check(state.Ids.Count==0 || !ReferenceEquals(state.Ids,baseline.Ids),"State owns seed IDs");ownership++;
            var initial=Row(state);var steps=new List<object>();
            var seedExact=new TangibleView6StateLimits(MaximumElements:baseline.Ids.Count,MaximumFields:2,MaximumOperations:0,MaximumAssignmentValues:0,MaximumMessageIds:baseline.Ids.Count,
                MaximumMessageBytes:baseline.DecodedBytes+2,MaximumStateBytes:state.DecodedBytes,MaximumEvents:0,MaximumOutputBytes:state.DecodedBytes,MaximumWork:baseline.Ids.Count);
            Equal(Row(TangibleView6State.FromBaseline(baseline,seedExact)),c.GetProperty("Initial"),"Exact seed caps");
            var seedCaps=new List<TangibleView6StateLimits>{seedExact with{MaximumMessageBytes=seedExact.MaximumMessageBytes-1},seedExact with{MaximumFields=1}};
            if(baseline.Ids.Count>0){seedCaps.Add(seedExact with{MaximumElements=baseline.Ids.Count-1});seedCaps.Add(seedExact with{MaximumMessageIds=baseline.Ids.Count-1});seedCaps.Add(seedExact with{MaximumWork=baseline.Ids.Count-1});seedCaps.Add(seedExact with{MaximumStateBytes=state.DecodedBytes-1});seedCaps.Add(seedExact with{MaximumOutputBytes=state.DecodedBytes-1});}
            foreach(var cap in seedCaps){budgets++;Reject(()=>TangibleView6State.FromBaseline(baseline,cap));}
            foreach(var step in c.GetProperty("Steps").EnumerateArray())
            {
                var wire=Bytes(step,"Hex");var plan=TangibleView6Codec.DecodeDelta(wire,CodecLimits);Check(TangibleView6Codec.EncodeDelta(plan,CodecLimits).SequenceEqual(wire),"Exact delta wire");
                var old=state;string before=JsonSerializer.Serialize(Row(old));var change=state.Apply(plan);
                Check(ReferenceEquals(change.Previous,old)&&!ReferenceEquals(change.Current,old),"Stateless branch publication");Check(JsonSerializer.Serialize(Row(old))==before,"Issued state unchanged");ownership+=2;
                Equal(Row(change.Current),step.GetProperty("Expected"),"Independent current state");Equal(Events(change),step.GetProperty("Events"),"Independent ordered descriptions");Check(change.WorkUsed==step.GetProperty("Work").GetInt64(),"Independent work units");Check(change.DeclaredCount==plan.DeclaredCount,"Raw metadata receipt");
                var cost=step.GetProperty("Cost");var exact=new TangibleView6StateLimits(
                    MaximumElements:cost.GetProperty("PeakElements").GetInt32(),MaximumFields:cost.GetProperty("Fields").GetInt32(),MaximumOperations:cost.GetProperty("Operations").GetInt32(),
                    MaximumAssignmentValues:cost.GetProperty("Assignments").GetInt32(),MaximumMessageIds:cost.GetProperty("Ids").GetInt32(),MaximumMessageBytes:cost.GetProperty("Bytes").GetInt64()+2,
                    MaximumStateBytes:cost.GetProperty("PeakStateBytes").GetInt64(),MaximumEvents:cost.GetProperty("Events").GetInt32(),MaximumOutputBytes:cost.GetProperty("PeakOutputBytes").GetInt64(),MaximumWork:change.WorkUsed);
                Equal(Row(old.Apply(plan,exact).Current),step.GetProperty("Expected"),"Exact all caps accepted");
                var caps=new List<TangibleView6StateLimits>{exact with{MaximumFields=exact.MaximumFields-1},exact with{MaximumMessageBytes=exact.MaximumMessageBytes-1},exact with{MaximumWork=exact.MaximumWork-1}};
                if(exact.MaximumElements>0)caps.Add(exact with{MaximumElements=exact.MaximumElements-1});if(exact.MaximumOperations>0)caps.Add(exact with{MaximumOperations=exact.MaximumOperations-1});
                if(exact.MaximumAssignmentValues>0)caps.Add(exact with{MaximumAssignmentValues=exact.MaximumAssignmentValues-1});if(exact.MaximumMessageIds>0)caps.Add(exact with{MaximumMessageIds=exact.MaximumMessageIds-1});
                if(exact.MaximumStateBytes>8)caps.Add(exact with{MaximumStateBytes=exact.MaximumStateBytes-1});if(exact.MaximumEvents>0)caps.Add(exact with{MaximumEvents=exact.MaximumEvents-1});if(exact.MaximumOutputBytes>8)caps.Add(exact with{MaximumOutputBytes=exact.MaximumOutputBytes-1});
                foreach(var cap in caps){budgets++;Reject(()=>old.Apply(plan,cap));Check(JsonSerializer.Serialize(Row(old))==before,"Failed cap preserves exact old state");}
                var wrong=new TangibleView6Delta(plan.ObjectId^1,plan.DeclaredCount,plan.Updates);Reject(()=>old.Apply(wrong));
                foreach(int n in new[]{0,5,23,wire.Length-1})Reject(()=>TangibleView6Codec.DecodeDelta(wire.AsSpan(0,n),CodecLimits));
                foreach(int offset in new[]{0,2,14,18,19}){var bad=wire.ToArray();bad[offset]^=1;Reject(()=>TangibleView6Codec.DecodeDelta(bad,CodecLimits));}
                Array.Clear(wire);var outputIds=change.Current.Ids.ToArray();Array.Clear(outputIds);Equal(Row(change.Current),step.GetProperty("Expected"),"Returned copy cannot mutate state");ownership++;
                var branch=Task.Run(()=>old.Apply(plan)).GetAwaiter().GetResult();Equal(Row(branch.Current),step.GetProperty("Expected"),"Immutable cross-thread branch");ownership++;
                steps.Add(new{Expected=Row(change.Current),Events=Events(change),Work=change.WorkUsed});state=change.Current;
            }
            var reseed=state.Reseed(baseline);Equal(Row(reseed),c.GetProperty("Reseed"),"Genuine reseed restores seed counter/IDs/passive");
            Check(state.Revision+1==reseed.Revision && state.BaselineDeclaredCount==baseline.DeclaredCount,"Reseed revision and baseline metadata");
            Reject(()=>state.Reseed(new(baseline.ObjectId^1,0,0,0,Array.Empty<ulong>())));
            var max=TangibleView6State.FromBaseline(baseline,revisionSeed:ulong.MaxValue-1);
            Reject(()=>max.Reseed(baseline));Reject(()=>max.Apply(TangibleView6Codec.DecodeDelta(Bytes(c.GetProperty("Steps")[0],"Hex"),CodecLimits)));
            Reject(()=>TangibleView6State.FromBaseline(baseline,revisionSeed:ulong.MaxValue));
            Reject(()=>state.Apply(new(state.ObjectId,0,Array.Empty<TangibleView6Update>())));
            Reject(()=>state.Apply(new(state.ObjectId,0,new[]{new TangibleView6Update(2,0,new[]{new TangibleView6Command(4,0,Array.Empty<ulong>())})})));
            Reject(()=>state.Apply(new(state.ObjectId,0,new[]{new TangibleView6Update(0,0,new[]{new TangibleView6Command(4,0,Array.Empty<ulong>())})})));
            records.Add(new{Name=c.GetProperty("Name").GetString(),Initial=initial,Steps=steps,Reseed=Row(reseed)});
        }
        // Per-operation policy replacement can tighten or widen and is retained by the candidate.
        var empty=TangibleView6State.FromBaseline(new(1,65535,0,0,Array.Empty<ulong>()));
        TangibleView6Delta Add(uint counter,ushort index)=>new(1,0,new[]{new TangibleView6Update(1,counter,new[]{new TangibleView6Command(1,index,new ulong[]{7})})});
        var tight=empty.Apply(Add(1,0),new(MaximumElements:2)).Current;
        Reject(()=>tight.Apply(Add(2,3)));
        var wide=tight.Apply(Add(2,3),new(MaximumElements:4)).Current;Check(wide.Ids.SequenceEqual(new ulong[]{7,0,0,7}),"Override can widen");
        Reject(()=>wide.Apply(Add(3,4)));
        var next=wide.Apply(Add(3,4),new(MaximumElements:5)).Current;Check(next.Ids.Count==5 && tight.Ids.Count==1 && empty.Ids.Count==0,"Policy and branch ownership");ownership++;
        foreach(var bad in doc.RootElement.GetProperty("Malformed").EnumerateArray())Reject(()=>TangibleView6Codec.DecodeDelta(Bytes(bad,"Hex"),CodecLimits));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Records=records,NegativeChecks=negatives,BudgetChecks=budgets,OwnershipChecks=ownership}));Console.WriteLine($"TANO6 state: {records.Count} sequences, {negatives} negatives, {budgets} caps, {ownership} ownership checks");
    }
}
