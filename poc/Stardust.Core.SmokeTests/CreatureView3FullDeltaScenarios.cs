using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3FullDeltaScenarios
{
    private static readonly CreatureView3FullDeltaLimits Limits=new(1048576,4096,4096,4096,65536,1048576,65536,65536,65536,65536,65536,65536);
    public static void Run(string peerPath,string exportPath)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(peerPath));var exports=new List<object>();int exact=0,negative=0,checks=0;
        void Check(bool v){checks++;if(!v)throw new Exception("Full check failed");}
        void Reject(Action a){negative++;try{a();}catch(InvalidDataException){return;}catch(ArgumentException){return;}throw new Exception("Full negative accepted");}
        byte[] Append(byte[] bytes,byte[] tail){var b=bytes.Concat(tail).ToArray();BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(19),(uint)(b.Length-23));return b;}
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            var limits=c.TryGetProperty("MaximumElements",out var elements)?Limits with{MaximumWoundsElements=elements.GetInt32(),MaximumWoundsValues=3}:Limits;
            byte[] wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);bool legacy=c.GetProperty("Legacy").GetBoolean();var policy=legacy?CreatureWoundsApplicationPolicy.LegacyBounded:CreatureWoundsApplicationPolicy.StrictInterval;
            var plan=CreatureView3FullDeltaCodec.Decode(wire,limits,policy);byte[] encoded=CreatureView3FullDeltaCodec.Encode(plan,limits);
            Check(wire.SequenceEqual(encoded));Check(plan.WoundsPolicy==policy);Check(plan.DeclaredCount==BinaryPrimitives.ReadUInt16LittleEndian(wire.AsSpan(23)));
            var rows=c.GetProperty("Rows").EnumerateArray().ToArray();Check(rows.Length==plan.Updates.Count);
            for(int i=0;i<rows.Length;i++)
            {
                var u=plan.Updates[i];Check(u.Index==rows[i].GetProperty("Index").GetUInt16());
                byte[] single=Append(wire[..25],Convert.FromHexString(rows[i].GetProperty("Hex").GetString()!));
                var decoded=CreatureView3FullDeltaCodec.Decode(single,limits,policy);Check(decoded.Updates.Count==1);
                Check(CreatureView3FullDeltaCodec.Encode(decoded,limits).SequenceEqual(single));
                var env=ReplicationEnvelopeCodec.DecodeDelta(single,limits.MaximumPayloadBytes);
                if(u.Components is {} component){var sibling=CreatureView3ComponentsDeltaCodec.Decode(env,new(1048576,1,65536,65536,65536));Check(component.Counter==sibling.Updates[0].Counter);Check(component.Commands.SequenceEqual(sibling.Updates[0].Commands));}
                if(u.Wounds is {} wounds){var sibling=CreatureView3WoundsDeltaCodec.Decode(env,new(1048576,1,65536,65536,65536),policy);Check(wounds.CommandCount==sibling.Updates[0].CommandCount);Check(wounds.Counter==sibling.Updates[0].Counter);Check(CreatureView3WoundsDeltaCodec.Encode(sibling,new(1048576,1,65536,65536,65536)).SequenceEqual(single));}
                if(u.Customization is {} raw){var sibling=CreatureView3CustomizationDeltaCodec.Decode(env,new(1048576,1,65536,1048576));Check(raw.CopyBytes().SequenceEqual(sibling.Updates[0].CopyBytes()));var copy=raw.CopyBytes();Array.Clear(copy);Check(CreatureView3FullDeltaCodec.Encode(plan,limits).SequenceEqual(wire));}
            }
            var boundaries=new HashSet<int>();int end=25;foreach(var row in rows){end+=row.GetProperty("Hex").GetString()!.Length/2;boundaries.Add(end);}
            for(int n=25;n<wire.Length;n++){var trimmed=wire[..n];BinaryPrimitives.WriteUInt32LittleEndian(trimmed.AsSpan(19),(uint)(n-23));if(boundaries.Contains(n)){Check(CreatureView3FullDeltaCodec.Encode(CreatureView3FullDeltaCodec.Decode(trimmed,limits,policy),limits).SequenceEqual(trimmed));}else Reject(()=>CreatureView3FullDeltaCodec.Decode(trimmed,limits,policy));}
            Reject(()=>CreatureView3FullDeltaCodec.Decode(Append(wire,new byte[]{5,0,1,0,0,0,0,0,0,0,3}),limits,policy));
            Reject(()=>CreatureView3FullDeltaCodec.Decode(Append(wire,new byte[]{17,0,1,0,0,0,0,0,0,0,5}),limits,policy));
            for(int n=0;n<wire.Length;n++){var truncated=wire[..n];Reject(()=>CreatureView3FullDeltaCodec.Decode(truncated,limits,policy));}
            Reject(()=>CreatureView3FullDeltaCodec.Decode(Append(wire,new byte[]{99,0}),limits,policy));
            foreach(uint bits in new uint[]{0x7f800000,0xff800000,0x7fc00000,0x7f800001}){byte[] tail=new byte[6];tail[0]=14;BinaryPrimitives.WriteUInt32LittleEndian(tail.AsSpan(2),bits);Reject(()=>CreatureView3FullDeltaCodec.Decode(Append(wire,tail),limits,policy));}
            foreach(int pos in new[]{0,2,14,18,19}){var bad=(byte[])wire.Clone();bad[pos]^=1;Reject(()=>CreatureView3FullDeltaCodec.Decode(bad,limits,policy));}
            if(plan.Updates.Any(u=>u.Scalar!=null||u.Customization?.Length>0||u.Components?.Commands.Any(cmd=>cmd.Kind!=CreatureComponentsCommandKind.Clear)==true||u.Wounds?.Commands.Any(cmd=>cmd.Values.Count>0)==true))
            {Reject(()=>CreatureView3FullDeltaCodec.Decode(wire,limits with{MaximumDecodedBytes=0},policy));Reject(()=>CreatureView3FullDeltaCodec.Encode(plan,limits with{MaximumDecodedBytes=0}));}
            if(legacy&&plan.Updates.Any(u=>u.Wounds is {} w&&w.CommandCount>w.Commands.Count))
            {var wrong=new CreatureView3FullDeltaPlan(plan.ObjectId,plan.DeclaredCount,plan.Updates,CreatureWoundsApplicationPolicy.StrictInterval,65536,65536);Reject(()=>CreatureView3FullDeltaCodec.Encode(wrong,limits));}
            var caps=new List<CreatureView3FullDeltaLimits>{limits with{MaximumFields=0},limits with{MaximumPayloadBytes=2}};
            if(plan.Updates.Any(u=>u.Scalar!=null))caps.Add(limits with{MaximumScalarUpdates=0});
            if(plan.Updates.Any(u=>u.Customization!=null))caps.Add(limits with{MaximumRawUpdates=0});
            if(plan.Updates.Any(u=>u.Customization?.Length>0))caps.Add(limits with{MaximumStringBytes=0});
            if(plan.Updates.Any(u=>u.Components?.Commands.Count>0)){caps.Add(limits with{MaximumComponentCommands=0});caps.Add(limits with{MaximumComponentKeys=0});}
            if(plan.Updates.Any(u=>u.Wounds?.CommandCount>0))caps.Add(limits with{MaximumWoundsCommands=0});
            if(plan.Updates.Any(u=>u.Wounds?.Commands.Any(cmd=>cmd.Values.Count>0)==true)){caps.Add(limits with{MaximumWoundsValues=0});}
            foreach(var cap in caps){Reject(()=>CreatureView3FullDeltaCodec.Decode(wire,cap,policy));Reject(()=>CreatureView3FullDeltaCodec.Encode(plan,cap));}
            var alias=(byte[])wire.Clone();var owned=CreatureView3FullDeltaCodec.Decode(alias,limits,policy);Array.Clear(alias);Check(CreatureView3FullDeltaCodec.Encode(owned,limits).SequenceEqual(wire));
            if(c.TryGetProperty("MaximumElements",out _))
            {
                var row=rows.Single(r=>r.GetProperty("Index").GetInt32()==17);byte[] single=Append(wire[..25],Convert.FromHexString(row.GetProperty("Hex").GetString()!));
                var sibling=CreatureView3WoundsDeltaCodec.Decode(single,new(1048576,1,4,3,1),policy);
                var initial=new CreatureView3WoundsState(plan.ObjectId,5,Array.Empty<int>(),1);
                if(sibling.Updates[0].Counter==5){var result=sibling.Apply(initial);Check(result.Counter==5&&result.Values.Count==0);}else Reject(()=>sibling.Apply(initial));
                Check(plan.Updates.Select(u=>u.Index).SequenceEqual(new ushort[]{4,17,4}));
            }
            exports.Add(new{Hex=Convert.ToHexString(encoded),Indices=plan.Updates.Select(u=>u.Index).ToArray()});exact++;
        }
        Reject(()=>CreatureView3FullDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3FullDeltaCodec.RawTag,3,1,Array.Empty<byte>(),2),Limits));
        foreach(var invalid in new[]{Limits with{MaximumDecodedBytes=-1},Limits with{MaximumFields=4097},Limits with{MaximumComponentCommands=65537},Limits with{MaximumComponentKeys=-1},Limits with{MaximumComponentElements=65537},Limits with{MaximumWoundsCommands=-1},Limits with{MaximumWoundsValues=65537},Limits with{MaximumWoundsElements=-1},Limits with{MaximumStringBytes=-1},Limits with{MaximumRawUpdates=-1},Limits with{MaximumScalarUpdates=-1}})Reject(()=>CreatureView3FullDeltaCodec.Decode(Array.Empty<byte>(),invalid));
        Reject(()=>CreatureView3FullDeltaCodec.Decode(Array.Empty<byte>(),Limits,(CreatureWoundsApplicationPolicy)99));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(exports));Console.WriteLine($"Full codec: {exact} exact, {negative} negatives, {checks} ownership/cap/sibling checks");
    }
}
