using System.Text.Json;
using System.Text.Json.Nodes;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class TangibleView6CodecScenarios
{
    public static void Run(string peerPath,string exportPath)
    {
        if(new FileInfo(peerPath).Length>1048576)throw new InvalidDataException("Fixture bound");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(peerPath));var records=new List<object>();int negatives=0,budgets=0;
        void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
        void Reject(Action action){negatives++;try{action();}catch(InvalidDataException){return;}throw new Exception("Negative accepted");}
        object Project(object value)=>value switch
        {
            TangibleView6Baseline b=>new{b.Passive,b.Counter,Ids=b.Ids.ToArray()},
            TangibleView6Delta d=>d.Updates.Select(u=>u.Index==0?(object)new{u.Index,u.Passive}:new{u.Index,u.DeclaredOperations,u.Counter,Commands=u.Commands.Select(c=>new{c.Opcode,c.Index,Values=c.Values.ToArray()}).ToArray()}).ToArray(),
            _=>throw new Exception("Unexpected DTO")
        };
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            byte[] wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);bool baseline=c.GetProperty("Kind").GetString()=="Baseline";var cost=c.GetProperty("Cost");
            var exact=new TangibleView6Limits(wire.Length-23,cost.GetProperty("Fields").GetInt32(),cost.GetProperty("Operations").GetInt32(),cost.GetProperty("Assignments").GetInt32(),cost.GetProperty("Ids").GetInt32(),cost.GetProperty("Bytes").GetInt64());
            object Decode(byte[] b,TangibleView6Limits l)=>baseline?TangibleView6Codec.DecodeBaseline(b,l):TangibleView6Codec.DecodeDelta(b,l);
            byte[] Encode(object v,TangibleView6Limits l)=>baseline?TangibleView6Codec.EncodeBaseline((TangibleView6Baseline)v,l):TangibleView6Codec.EncodeDelta((TangibleView6Delta)v,l);
            var alias=wire.ToArray();var value=Decode(alias,exact);Array.Clear(alias);var encoded=Encode(value,exact);Check(encoded.SequenceEqual(wire),"Owned exact wire");Array.Clear(encoded);Check(Encode(value,exact).SequenceEqual(wire),"Output detached");
            var expected=Project(value);Check(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(expected),JsonNode.Parse(c.GetProperty("Expected").GetRawText())),"Independent DTO grammar");
            ushort metadata=baseline?((TangibleView6Baseline)value).DeclaredCount:((TangibleView6Delta)value).DeclaredCount;Check(metadata==BitConverter.ToUInt16(wire,23),"Declared metadata retained");
            if(value is TangibleView6Baseline b)
            {
                var source=b.Ids.ToArray();var copy=new TangibleView6Baseline(b.ObjectId,b.DeclaredCount,b.Passive,b.Counter,source);Array.Clear(source);Check(Encode(copy,exact).SequenceEqual(wire),"Baseline constructor freeze");
                Check(TangibleView6Codec.EncodeBaseline(TangibleView6Codec.DecodeBaseline(ReplicationEnvelopeCodec.DecodeBaseline(wire,exact.MaximumPayloadBytes),exact),exact).SequenceEqual(wire),"Envelope baseline");
                Reject(()=>TangibleView6Codec.DecodeDelta(wire,exact));
            }
            else
            {
                var d=(TangibleView6Delta)value;var source=d.Updates.ToArray();var copy=new TangibleView6Delta(d.ObjectId,d.DeclaredCount,source);Array.Clear(source);Check(Encode(copy,exact).SequenceEqual(wire),"Delta constructor freeze");
                foreach(var cmd in d.Updates.SelectMany(u=>u.Commands)){var ids=cmd.Values.ToArray();var owned=new TangibleView6Command(cmd.Opcode,cmd.Index,ids);Array.Clear(ids);Check(owned.Values.SequenceEqual(cmd.Values),"Command values frozen");}
                Check(TangibleView6Codec.EncodeDelta(TangibleView6Codec.DecodeDelta(ReplicationEnvelopeCodec.DecodeDelta(wire,exact.MaximumPayloadBytes),exact),exact).SequenceEqual(wire),"Envelope delta");
                Reject(()=>TangibleView6Codec.DecodeBaseline(wire,exact));
            }
            var caps=new List<TangibleView6Limits>{exact with{MaximumPayloadBytes=exact.MaximumPayloadBytes-1},exact with{MaximumFields=exact.MaximumFields-1},exact with{MaximumDecodedBytes=exact.MaximumDecodedBytes-1}};
            if(exact.MaximumOperations>0)caps.Add(exact with{MaximumOperations=exact.MaximumOperations-1});if(exact.MaximumAssignmentValues>0)caps.Add(exact with{MaximumAssignmentValues=exact.MaximumAssignmentValues-1});if(exact.MaximumIds>0)caps.Add(exact with{MaximumIds=exact.MaximumIds-1});
            foreach(var cap in caps){budgets+=2;Reject(()=>Decode(wire,cap));Reject(()=>Encode(value,cap));}
            foreach(int n in new[]{0,1,5,13,23,24,wire.Length-1}.Distinct())Reject(()=>Decode(wire[..n],exact));
            foreach(int offset in new[]{0,2,14,18,19}){var bad=wire.ToArray();bad[offset]^=1;Reject(()=>Decode(bad,exact));}
            Reject(()=>Decode(wire.Concat(new byte[]{0}).ToArray(),exact));
            records.Add(new{Hex=Convert.ToHexString(Encode(value,exact)),Expected=expected});
        }
        foreach(var c in doc.RootElement.GetProperty("Malformed").EnumerateArray())Reject(()=>TangibleView6Codec.DecodeDelta(Convert.FromHexString(c.GetProperty("Hex").GetString()!),new()));
        // Encode preflight rejects incomplete and extra raw-loop command models.
        Reject(()=>TangibleView6Codec.EncodeDelta(new(1,0,new[]{new TangibleView6Update(2,1,new[]{new TangibleView6Command(4,0,Array.Empty<ulong>())})}),new()));
        Reject(()=>TangibleView6Codec.EncodeDelta(new(1,0,new[]{new TangibleView6Update(0,1,new[]{new TangibleView6Command(4,0,Array.Empty<ulong>())})}),new()));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Records=records,NegativeChecks=negatives,BudgetChecks=budgets}));Console.WriteLine($"TANO6: {records.Count} exact, {negatives} negatives, {budgets} global cap checks");
    }
}
