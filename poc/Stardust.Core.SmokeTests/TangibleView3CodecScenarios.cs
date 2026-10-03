using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class TangibleView3CodecScenarios
{
    private static readonly CreatureView3BaselineLimits BaseLimits=new(8388608,131072,131072,65536,8388608);
    private static readonly TangibleView3DeltaLimits DeltaLimits=new(8388608,4096,4096,4096,131072,8388608,65536,65536,65536,4096,65536,65536);
    public static void Run(string peerPath,string exportPath)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(peerPath));var exports=new List<object>();int baselines=0,deltas=0,negative=0,checks=0;
        void Check(bool v){checks++;if(!v)throw new Exception("TANO check failed");}
        void Reject(Action a){negative++;try{a();}catch(InvalidDataException){return;}catch(ArgumentException){return;}throw new Exception("TANO negative accepted");}
        byte[] Append(byte[] wire,byte[] tail){var b=wire.Concat(tail).ToArray();BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(19),(uint)(b.Length-23));return b;}
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            byte[] wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);bool baseline=c.GetProperty("Kind").GetString()=="Baseline";var expected=c.GetProperty("Expected");long charge;byte[] encoded;
            if(baseline)
            {
                var v=TangibleView3BaselineCodec.Decode(wire,BaseLimits);charge=v.DecodedBytes;encoded=TangibleView3BaselineCodec.Encode(v,BaseLimits);
                Check(charge==expected.GetProperty("Decoded").GetInt64());Check(v.ObjectId==expected.GetProperty("ObjectId").GetUInt64());Check(v.DeclaredCount==expected.GetProperty("DeclaredCount").GetUInt16());Check(BitConverter.SingleToInt32Bits(v.Complexity)==unchecked((int)expected.GetProperty("ComplexityBits").GetUInt32()));
                foreach(var (bytes,property) in new[]{(v.NameFile,"File"),(v.NameKey,"Key"),(v.Customization,"Raw")}){var e=expected.GetProperty(property);Check(bytes.CopyBytes().SequenceEqual(Convert.FromHexString(e.GetProperty("Hex").GetString()!)));Check(bytes.UsesExtendedLength==e.GetProperty("Extended").GetBoolean());var copy=bytes.CopyBytes();Array.Clear(copy);}
                Check(v.NameSpacer==expected.GetProperty("Spacer").GetUInt32());Check(v.CustomNameUnits.SequenceEqual(expected.GetProperty("Units").EnumerateArray().Select(x=>x.GetUInt16())));Check(v.Components.Values.SequenceEqual(expected.GetProperty("Components").EnumerateArray().Select(x=>x.GetInt32())));Check(v.Components.Counter==expected.GetProperty("Counter").GetUInt32());
                Check(v.Volume==expected.GetProperty("Volume").GetUInt32());Check(v.Options==expected.GetProperty("Options").GetUInt32());Check(v.UseCount==expected.GetProperty("Use").GetUInt32());Check(v.ConditionDamage==expected.GetProperty("Condition").GetUInt32());Check(v.MaximumCondition==expected.GetProperty("Maximum").GetUInt32());Check(v.Visibility==expected.GetProperty("Visibility").GetByte());
                var names=CreatureView3NameState.FromBaseline(v);Check(ReferenceEquals(names.NameFile,v.NameFile)&&ReferenceEquals(names.NameKey,v.NameKey)&&ReferenceEquals(names.CustomNameUnits,v.CustomNameUnits));
                int maximumString=Math.Max(v.NameFile.Length,Math.Max(v.NameKey.Length,v.Customization.Length));var exact=new CreatureView3BaselineLimits(wire.Length-23,maximumString,v.CustomNameUnits.Count,v.Components.Values.Count,charge);
                Check(TangibleView3BaselineCodec.Encode(v,exact).SequenceEqual(wire));Check(TangibleView3BaselineCodec.Decode(wire,exact).DecodedBytes==charge);
                var caps=new List<CreatureView3BaselineLimits>{exact with{MaximumPayloadBytes=exact.MaximumPayloadBytes-1},exact with{MaximumDecodedBytes=charge-1}};
                if(maximumString>0)caps.Add(exact with{MaximumStringBytes=maximumString-1});if(v.CustomNameUnits.Count>0)caps.Add(exact with{MaximumUnicodeUnits=v.CustomNameUnits.Count-1});if(v.Components.Count>0)caps.Add(exact with{MaximumContainerElements=v.Components.Values.Count-1});
                foreach(var cap in caps){Reject(()=>TangibleView3BaselineCodec.Decode(wire,cap));Reject(()=>TangibleView3BaselineCodec.Encode(v,cap));}
                var env=ReplicationEnvelopeCodec.DecodeBaseline(wire,BaseLimits.MaximumPayloadBytes);Check(TangibleView3BaselineCodec.Encode(TangibleView3BaselineCodec.Decode(env,BaseLimits),BaseLimits).SequenceEqual(wire));Check(ReplicationEnvelopeCodec.EncodeBaseline(TangibleView3BaselineCodec.EncodeEnvelope(v,BaseLimits),BaseLimits.MaximumPayloadBytes).SequenceEqual(wire));Reject(()=>TangibleView3DeltaCodec.Decode(env,DeltaLimits));
                foreach(uint bits in new uint[]{0x7f800000,0xff800000,0x7fc00000,0x7f800001}){var bad=(byte[])wire.Clone();BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(25),bits);Reject(()=>TangibleView3BaselineCodec.Decode(bad,BaseLimits));}
                Reject(()=>TangibleView3BaselineCodec.Decode(Append(wire,new byte[]{0}),BaseLimits));var alias=(byte[])wire.Clone();var owned=TangibleView3BaselineCodec.Decode(alias,BaseLimits);Array.Clear(alias);var output=TangibleView3BaselineCodec.Encode(owned,BaseLimits);Array.Clear(output);Check(TangibleView3BaselineCodec.Encode(owned,BaseLimits).SequenceEqual(wire));baselines++;
            }
            else
            {
                var plan=TangibleView3DeltaCodec.Decode(wire,DeltaLimits);encoded=TangibleView3DeltaCodec.Encode(plan,DeltaLimits);var rows=expected.EnumerateArray().ToArray();charge=rows.Sum(r=>r.GetProperty("Decoded").GetInt64());Check(rows.Length==plan.Updates.Count);Check(plan.DeclaredCount==BinaryPrimitives.ReadUInt16LittleEndian(wire.AsSpan(23)));Check(plan.Updates.All(u=>u.Index<=10));
                int names=0,tangible=0,raws=0,commands=0,keys=0,nameString=0,unicode=0,rawString=0;
                for(int i=0;i<rows.Length;i++)
                {
                    var u=plan.Updates[i];Check(u.Index==rows[i].GetProperty("Index").GetUInt16());byte[] single=Append(wire[..25],Convert.FromHexString(rows[i].GetProperty("Hex").GetString()!));var projected=(byte[])single.Clone();BinaryPrimitives.WriteUInt32LittleEndian(projected.AsSpan(14),CreatureView3NamedDeltaCodec.RawTag);
                    var common=new CreatureView3NamedDeltaLimits(new(new(8388608,4096,0,4096,131072,8388608,65536,65536,65536,0,0,0),4096),4096,65536,65536);var sibling=CreatureView3NamedDeltaCodec.Decode(projected,common);Check(CreatureView3NamedDeltaCodec.Encode(sibling,common).SequenceEqual(projected));Check(TangibleView3DeltaCodec.Encode(TangibleView3DeltaCodec.Decode(single,DeltaLimits),DeltaLimits).SequenceEqual(single));
                    if(u.Name is {} name){names++;if(name.Index==1){nameString=Math.Max(nameString,Math.Max(name.NameFile!.Length,name.NameKey!.Length));Check(name.NameSpacer==sibling.Updates[0].Name!.NameSpacer);}else{unicode=Math.Max(unicode,name.CustomNameUnits!.Count);Check(name.CustomNameUnits.SequenceEqual(sibling.Updates[0].Name!.CustomNameUnits!));}}
                    if(u.Extended?.Tangible is not null)tangible++;
                    if(u.Extended?.Full?.Customization is {} raw){raws++;rawString=Math.Max(rawString,raw.Length);Check(raw.CopyBytes().SequenceEqual(sibling.Updates[0].Extended!.Full!.Customization!.CopyBytes()));}
                    if(u.Extended?.Full?.Components is {} component){commands+=component.Commands.Count;keys+=component.Commands.Count(cmd=>cmd.Kind!=CreatureComponentsCommandKind.Clear);Check(component.Counter==sibling.Updates[0].Extended!.Full!.Components!.Counter);}
                }
                var exact=DeltaLimits with{MaximumPayloadBytes=wire.Length-23,MaximumFields=rows.Length,MaximumDecodedBytes=charge,MaximumNameUpdates=names,MaximumTangibleUpdates=tangible,MaximumRawUpdates=raws,MaximumStringBytes=rawString,MaximumNameStringBytes=nameString,MaximumUnicodeUnits=unicode,MaximumComponentCommands=commands,MaximumComponentKeys=keys};Check(TangibleView3DeltaCodec.Encode(plan,exact).SequenceEqual(wire));Check(TangibleView3DeltaCodec.Decode(wire,exact).Updates.Count==rows.Length);
                var caps=new List<TangibleView3DeltaLimits>{exact with{MaximumPayloadBytes=exact.MaximumPayloadBytes-1},exact with{MaximumFields=rows.Length-1}};
                if(charge>0)caps.Add(exact with{MaximumDecodedBytes=charge-1});if(names>0)caps.Add(exact with{MaximumNameUpdates=names-1});if(tangible>0)caps.Add(exact with{MaximumTangibleUpdates=tangible-1});if(raws>0)caps.Add(exact with{MaximumRawUpdates=raws-1});if(rawString>0)caps.Add(exact with{MaximumStringBytes=rawString-1});if(nameString>0)caps.Add(exact with{MaximumNameStringBytes=nameString-1});if(unicode>0)caps.Add(exact with{MaximumUnicodeUnits=unicode-1});if(commands>0)caps.Add(exact with{MaximumComponentCommands=commands-1});if(keys>0)caps.Add(exact with{MaximumComponentKeys=keys-1});foreach(var cap in caps){Reject(()=>TangibleView3DeltaCodec.Decode(wire,cap));Reject(()=>TangibleView3DeltaCodec.Encode(plan,cap));}
                var env=ReplicationEnvelopeCodec.DecodeDelta(wire,DeltaLimits.MaximumPayloadBytes);Check(TangibleView3DeltaCodec.Encode(TangibleView3DeltaCodec.Decode(env,DeltaLimits),DeltaLimits).SequenceEqual(wire));Check(ReplicationEnvelopeCodec.EncodeDelta(TangibleView3DeltaCodec.EncodeEnvelope(plan,DeltaLimits),DeltaLimits.MaximumPayloadBytes).SequenceEqual(wire));Reject(()=>TangibleView3BaselineCodec.Decode(env,BaseLimits));
                foreach(ushort index in Enumerable.Range(11,8).Select(x=>(ushort)x).Append(ushort.MaxValue)){var tail=new byte[2];BinaryPrimitives.WriteUInt16LittleEndian(tail,index);Reject(()=>TangibleView3DeltaCodec.Decode(Append(wire,tail),DeltaLimits));}
                foreach(uint bits in new uint[]{0x7f800000,0xff800000,0x7fc00000,0x7f800001}){var tail=new byte[6];BinaryPrimitives.WriteUInt32LittleEndian(tail.AsSpan(2),bits);Reject(()=>TangibleView3DeltaCodec.Decode(Append(wire,tail),DeltaLimits));}
                var alias=(byte[])wire.Clone();var owned=TangibleView3DeltaCodec.Decode(alias,DeltaLimits);Array.Clear(alias);var output=TangibleView3DeltaCodec.Encode(owned,DeltaLimits);Array.Clear(output);var copy=plan.Updates.ToArray();var frozen=new TangibleView3DeltaPlan(plan.ObjectId,plan.DeclaredCount,copy,plan.MaximumComponentsElements);Array.Clear(copy);Check(TangibleView3DeltaCodec.Encode(owned,DeltaLimits).SequenceEqual(wire));Check(TangibleView3DeltaCodec.Encode(frozen,DeltaLimits).SequenceEqual(wire));deltas++;
            }
            Check(wire.SequenceEqual(encoded));
            Action<byte[]> decode=b=>{if(baseline)_=TangibleView3BaselineCodec.Decode(b,BaseLimits);else _=TangibleView3DeltaCodec.Decode(b,DeltaLimits);};
            var boundaries=new HashSet<int>();if(!baseline){int p=25;foreach(var row in expected.EnumerateArray()){p+=row.GetProperty("Hex").GetString()!.Length/2;boundaries.Add(p);}}
            IEnumerable<int> prefixes=wire.Length<2048?Enumerable.Range(0,wire.Length):Enumerable.Range(0,64).Concat(Enumerable.Range(wire.Length-16,16));
            // Every large fixture prefix is checked without allocating a sliced wire array.
            if(wire.Length>=2048)for(int n=0;n<wire.Length;n++)
            {
                negative++;
                try {if(baseline)_=TangibleView3BaselineCodec.Decode(wire.AsSpan(0,n),BaseLimits);else _=TangibleView3DeltaCodec.Decode(wire.AsSpan(0,n),DeltaLimits);}
                catch(InvalidDataException){continue;}
                throw new Exception("TANO large frame prefix accepted");
            }
            foreach(int n in prefixes){var prefix=wire[..n];Reject(()=>decode(prefix));if(n>=25){BinaryPrimitives.WriteUInt32LittleEndian(prefix.AsSpan(19),(uint)(n-23));if(boundaries.Contains(n)){decode(prefix);Check(true);}else Reject(()=>decode(prefix));}}
            foreach(int pos in new[]{0,2,14,18,19}){var bad=(byte[])wire.Clone();bad[pos]^=1;Reject(()=>decode(bad));}
            exports.Add(new{Hex=Convert.ToHexString(encoded),Decoded=charge});
        }
        Reject(()=>TangibleView3BaselineCodec.Decode(Array.Empty<byte>(),BaseLimits with{MaximumDecodedBytes=-1}));Reject(()=>TangibleView3DeltaCodec.Decode(Array.Empty<byte>(),DeltaLimits with{MaximumNameStringBytes=65537}));Reject(()=>TangibleView3DeltaCodec.Decode(Array.Empty<byte>(),DeltaLimits with{MaximumFields=4097}));
        byte[] Wrap(ReplicationEnvelopeKind kind,byte[] fields)=>kind==ReplicationEnvelopeKind.Baseline?ReplicationEnvelopeCodec.EncodeBaseline(new(kind,1,TangibleView3BaselineCodec.RawTag,3,0,fields,8388608),8388608):ReplicationEnvelopeCodec.EncodeDelta(new(kind,1,TangibleView3DeltaCodec.RawTag,3,0,fields,8388608),8388608);
        byte[] empty=new byte[47];
        foreach(int offset in new[]{12,22}){var bad=(byte[])empty.Clone();BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset),uint.MaxValue);Reject(()=>TangibleView3BaselineCodec.Decode(Wrap(ReplicationEnvelopeKind.Baseline,bad),BaseLimits));}
        foreach(int offset in new[]{4,10,20}){byte[] prefix=new byte[6];prefix[0]=255;prefix[1]=255;BinaryPrimitives.WriteUInt32LittleEndian(prefix.AsSpan(2),uint.MaxValue);var bad=empty[..offset].Concat(prefix).Concat(empty[(offset+2)..]).ToArray();Reject(()=>TangibleView3BaselineCodec.Decode(Wrap(ReplicationEnvelopeKind.Baseline,bad),BaseLimits));}
        foreach(var bad in new[]{new byte[]{1,0,255,255,255,255,255,255},new byte[]{1,0,0,0,0,0,0,0,255,255,255,255,255,255},new byte[]{2,0,255,255,255,255},new byte[]{5,0,255,255,255,255,0,0,0,0},Array.Empty<byte>()})Reject(()=>TangibleView3DeltaCodec.Decode(Wrap(ReplicationEnvelopeKind.Delta,bad),DeltaLimits));
        byte[] vector=new byte[6+65536*2];vector[0]=2;BinaryPrimitives.WriteUInt32LittleEndian(vector.AsSpan(2),65536);Reject(()=>TangibleView3DeltaCodec.Decode(Wrap(ReplicationEnvelopeKind.Delta,Enumerable.Range(0,9).SelectMany(_=>vector).ToArray()),DeltaLimits));
        Reject(()=>TangibleView3BaselineCodec.Decode(Array.Empty<byte>(),BaseLimits with{MaximumUnicodeUnits=int.MaxValue}));Reject(()=>TangibleView3BaselineCodec.Decode(Array.Empty<byte>(),BaseLimits with{MaximumContainerElements=int.MaxValue}));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(exports));Console.WriteLine($"TANO codecs: {baselines} baselines, {deltas} deltas, {negative} negatives, {checks} ownership/cap/sibling checks");
    }
}
