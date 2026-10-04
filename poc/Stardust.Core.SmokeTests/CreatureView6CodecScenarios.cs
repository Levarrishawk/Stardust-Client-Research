using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Nodes;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView6CodecScenarios
{
    private static object Text(ReplicationByteString s)=>new{Hex=Convert.ToHexString(s.CopyBytes()),Extended=s.UsesExtendedLength};
    private static object Vector<T>(CreatureView6Vector<T> v)=>new{v.Counter,Values=v.Values.ToArray()};
    private static object Row(CreatureView6Baseline s)=>new{s.ObjectId,s.DeclaredCount,s.Passive,Defenders=Vector(s.Defenders),s.Level,
        PerformanceAnimation=Text(s.PerformanceAnimation),MoodString=Text(s.MoodString),s.WeaponId,s.GroupId,s.InviterId,s.InviteCounter,s.Guild,s.TargetId,s.MoodId,s.PerformanceStart,s.PerformanceType,
        Ham=Vector(s.Ham),MaximumHam=Vector(s.MaximumHam),Wearables=new{s.Wearables.Counter,Values=s.Wearables.Values.Select(w=>new{Customization=Text(w.Customization),w.Arrangement,w.ObjectId,w.TemplateCrc}).ToArray()},AlternateAppearance=Text(s.AlternateAppearance),s.Frozen};
    private static object Row(CreatureView6Delta s)=>new{s.ObjectId,s.DeclaredCount,Updates=s.Updates.Select(u=>new{u.Index,u.Bits,Text=u.Text is null?null:Text(u.Text),u.SecondId}).ToArray()};
    public static void Run(string peerPath,string exportPath)
    {
        if(new FileInfo(peerPath).Length>4194304)throw new InvalidDataException("Bounded synthetic CREO6 fixture");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(peerPath));var records=new List<object>();int negatives=0,budgets=0,ownership=0;
        var limits=new CreatureView6Limits();
        void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        void Equal(object row,JsonElement expected,string reason)=>Check(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(row),JsonNode.Parse(expected.GetRawText())),reason);
        void Reject(Action action){negatives++;try{action();}catch(InvalidDataException){return;}catch(OverflowException){return;}catch(ArgumentException){return;}throw new Exception("Negative CREO6 accepted");}
        object Decode(byte[] wire,bool baseline,CreatureView6Limits l)=>baseline?CreatureView6Codec.DecodeBaseline(wire,l):CreatureView6Codec.DecodeDelta(wire,l);
        byte[] Encode(object model,bool baseline,CreatureView6Limits l)=>baseline?CreatureView6Codec.EncodeBaseline((CreatureView6Baseline)model,l):CreatureView6Codec.EncodeDelta((CreatureView6Delta)model,l);
        object Project(object model,bool baseline)=>baseline?Row((CreatureView6Baseline)model):Row((CreatureView6Delta)model);
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            bool baseline=c.GetProperty("Kind").GetString()=="Baseline";byte[] wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);var model=Decode(wire,baseline,limits);
            Equal(Project(model,baseline),c.GetProperty("Expected"),"Independent complete model");Check(Encode(model,baseline,limits).SequenceEqual(wire),"Exact bytes");
            var envelope=baseline?ReplicationEnvelopeCodec.DecodeBaseline(wire,limits.MaximumPayloadBytes):ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes);
            var fromEnvelope=baseline?(object)CreatureView6Codec.DecodeBaseline(envelope,limits):CreatureView6Codec.DecodeDelta(envelope,limits);Equal(Project(fromEnvelope,baseline),c.GetProperty("Expected"),"Owned envelope overload");
            var encodedEnvelope=baseline?CreatureView6Codec.EncodeBaselineEnvelope((CreatureView6Baseline)model,limits):CreatureView6Codec.EncodeDeltaEnvelope((CreatureView6Delta)model,limits);
            Check(encodedEnvelope.CopyOpaqueFields().SequenceEqual(envelope.CopyOpaqueFields()),"Envelope encoding");ownership++;
            var old=JsonSerializer.Serialize(Project(model,baseline));var alias=(byte[])wire.Clone();var owned=Decode(alias,baseline,limits);Array.Clear(alias);Check(JsonSerializer.Serialize(Project(owned,baseline))==old,"Wire ownership");ownership++;
            if(baseline)
            {
                var s=(CreatureView6Baseline)model;foreach(var str in new[]{s.PerformanceAnimation,s.MoodString,s.AlternateAppearance}.Concat(s.Wearables.Values.Select(w=>w.Customization))){var copy=str.CopyBytes();Array.Clear(copy);Check(JsonSerializer.Serialize(Project(model,true))==old,"Raw string copy ownership");ownership++;}
                var values=s.Defenders.Values.ToArray();var vector=new CreatureView6Vector<ulong>(s.Defenders.Counter,values);Array.Clear(values);Check(vector.Values.SequenceEqual(s.Defenders.Values),"Vector owns input");ownership++;
                if(vector.Values.Count>0){try{((IList<ulong>)vector.Values)[0]=123;throw new Exception("Mutable vector");}catch(NotSupportedException){ownership++;}}
            }
            else
            {
                var s=(CreatureView6Delta)model;var values=s.Updates.ToArray();var frozen=new CreatureView6Delta(s.ObjectId,s.DeclaredCount,values);Array.Clear(values);Check(JsonSerializer.Serialize(Row(frozen))==old,"Plan owns update collection");ownership++;
                foreach(var u in s.Updates.Where(u=>u.Text is not null)){var copy=u.Text!.CopyBytes();Array.Clear(copy);Check(JsonSerializer.Serialize(Row(s))==old,"Delta string owns bytes");ownership++;}
                foreach(var u in s.Updates.Where(u=>u.Index==12))Check(u.PerformanceType==unchecked((int)(uint)u.Bits),"Signed performance bits");
            }
            var cost=c.GetProperty("Cost");int N(string key)=>cost.GetProperty(key).GetInt32();long L(string key)=>cost.GetProperty(key).GetInt64();
            var exact=new CreatureView6Limits(N("PayloadBytes"),N("Fields"),N("StringBytes"),N("TotalStringBytes"),N("Elements"),N("TotalElements"),L("DecodedBytes"),L("Work"));
            _=Decode(wire,baseline,exact);Check(Encode(model,baseline,exact).SequenceEqual(wire),"Exact independent budgets");budgets+=2;
            var tighter=new List<CreatureView6Limits>{exact with{MaximumPayloadBytes=exact.MaximumPayloadBytes-1},exact with{MaximumFields=exact.MaximumFields-1},exact with{MaximumDecodedBytes=exact.MaximumDecodedBytes-1},exact with{MaximumWork=exact.MaximumWork-1}};
            if(exact.MaximumStringBytes>0)tighter.Add(exact with{MaximumStringBytes=exact.MaximumStringBytes-1});if(exact.MaximumTotalStringBytes>0)tighter.Add(exact with{MaximumTotalStringBytes=exact.MaximumTotalStringBytes-1});
            if(exact.MaximumElements>0)tighter.Add(exact with{MaximumElements=exact.MaximumElements-1});if(exact.MaximumTotalElements>0)tighter.Add(exact with{MaximumTotalElements=exact.MaximumTotalElements-1});
            foreach(var tight in tighter){Reject(()=>Decode(wire,baseline,tight));Reject(()=>Encode(model,baseline,tight));budgets+=2;}
            Check(JsonSerializer.Serialize(Project(model,baseline))==old,"Failure leaves published model unchanged");
            var cuts=new HashSet<int>();if(wire.Length<1024){for(int n=0;n<wire.Length;n++)cuts.Add(n);}else{for(int n=0;n<80;n++)cuts.Add(n);for(int n=wire.Length-80;n<wire.Length;n++)cuts.Add(n);}
            foreach(int n in cuts){var truncated=wire[..n];Reject(()=>Decode(truncated,baseline,limits));if(n>=25){BinaryPrimitives.WriteUInt32LittleEndian(truncated.AsSpan(19),(uint)(n-23));var valid=c.GetProperty("ValidPrefixes").EnumerateArray().Where(p=>p.GetProperty("Length").GetInt32()==n).ToArray();if(valid.Length==0)Reject(()=>Decode(truncated,baseline,limits));else{var prefix=Decode(truncated,false,limits);Equal(Project(prefix,false),valid[0].GetProperty("Expected"),"Independent complete delta prefix/count tolerance");Check(Encode(prefix,false,limits).SequenceEqual(truncated),"Exact complete delta prefix");}}}
            foreach(var wrong in new[]{new ReplicationEnvelope(envelope.Kind,envelope.ObjectId,0x54414e4f,6,envelope.DeclaredCount,envelope.CopyOpaqueFields(),limits.MaximumPayloadBytes),new ReplicationEnvelope(envelope.Kind,envelope.ObjectId,CreatureView6Codec.RawTag,3,envelope.DeclaredCount,envelope.CopyOpaqueFields(),limits.MaximumPayloadBytes),new ReplicationEnvelope(baseline?ReplicationEnvelopeKind.Delta:ReplicationEnvelopeKind.Baseline,envelope.ObjectId,CreatureView6Codec.RawTag,6,envelope.DeclaredCount,envelope.CopyOpaqueFields(),limits.MaximumPayloadBytes)})
                Reject(()=>{if(baseline)CreatureView6Codec.DecodeBaseline(wrong,limits);else CreatureView6Codec.DecodeDelta(wrong,limits);});
            records.Add(new{Name=c.GetProperty("Name").GetString(),Kind=c.GetProperty("Kind").GetString(),Expected=Project(model,baseline),Hex=Convert.ToHexString(Encode(model,baseline,limits))});
        }
        foreach(var c in doc.RootElement.GetProperty("Malformed").EnumerateArray()){bool baseline=c.GetProperty("Kind").GetString()=="Baseline";byte[] wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);Reject(()=>Decode(wire,baseline,limits));}
        foreach(ushort index in new ushort[]{1,3,4,7,13,14,15,16,18,65535})Reject(()=>CreatureView6Update.Scalar(index,0));
        Reject(()=>CreatureView6Update.Scalar(2,65536));Reject(()=>CreatureView6Update.Scalar(10,256));Reject(()=>CreatureView6Update.Scalar(0,0x100000000));
        Reject(()=>CreatureView6Update.String(0,ReplicationByteString.Create([])));Reject(()=>new CreatureView6Delta(1,0,[null!]));Reject(()=>new CreatureView6Vector<CreatureView6Wearable>(0,[null!]));Reject(()=>new CreatureView6Vector<int>(0,Enumerable.Range(0,65537)));
        var source=new byte[]{0,255,128};var ownedString=ReplicationByteString.Create(source,true);Array.Clear(source);Check(ownedString.CopyBytes().SequenceEqual(new byte[]{0,255,128}),"Factory copies bytes");ownership++;
        var invalid=new[]{limits with{MaximumPayloadBytes=1},limits with{MaximumFields=-1},limits with{MaximumStringBytes=-1},limits with{MaximumTotalStringBytes=-1},limits with{MaximumElements=-1},limits with{MaximumTotalElements=-1},limits with{MaximumDecodedBytes=-1},limits with{MaximumWork=-1},limits with{MaximumPayloadBytes=8388609},limits with{MaximumFields=4097},limits with{MaximumStringBytes=1048577},limits with{MaximumTotalStringBytes=4194305},limits with{MaximumElements=65537},limits with{MaximumTotalElements=262145},limits with{MaximumDecodedBytes=16777217},limits with{MaximumWork=16777217}};
        foreach(var bad in invalid){Reject(()=>CreatureView6Codec.DecodeBaseline([],bad));Reject(()=>CreatureView6Codec.DecodeDelta([],bad));}
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Records=records,NegativeChecks=negatives,BudgetChecks=budgets,OwnershipChecks=ownership},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS CREO6 {records.Count} exact wires, {negatives} negative, {budgets} budget, {ownership} ownership checks");
    }
}
