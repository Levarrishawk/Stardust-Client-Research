using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3ExtendedDeltaScenarios
{
    private static readonly CreatureView3FullDeltaLimits Full=new(1048576,4096,4096,4096,65536,1048576,65536,65536,65536,65536,65536,65536);
    public static void Run(string peerPath,string exportPath)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(peerPath));var exports=new List<object>();int exact=0,negative=0,checks=0;
        void Check(bool v){checks++;if(!v)throw new Exception("Extended check failed");}
        void Reject(Action a){negative++;try{a();}catch(InvalidDataException){return;}catch(ArgumentException){return;}throw new Exception("Extended negative accepted");}
        byte[] Append(byte[] bytes,byte[] tail){var b=bytes.Concat(tail).ToArray();BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(19),(uint)(b.Length-23));return b;}
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            var full=c.TryGetProperty("MaximumElements",out var elements)?Full with{MaximumWoundsElements=elements.GetInt32(),MaximumWoundsValues=3}:Full;
            var limits=new CreatureView3ExtendedDeltaLimits(full,4096);
            byte[] wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);var policy=c.GetProperty("Legacy").GetBoolean()?CreatureWoundsApplicationPolicy.LegacyBounded:CreatureWoundsApplicationPolicy.StrictInterval;
            var plan=CreatureView3ExtendedDeltaCodec.Decode(wire,limits,policy);var encoded=CreatureView3ExtendedDeltaCodec.Encode(plan,limits);
            Check(wire.SequenceEqual(encoded));
            var envelope=ReplicationEnvelopeCodec.DecodeDelta(wire,full.MaximumPayloadBytes);
            Check(CreatureView3ExtendedDeltaCodec.Encode(CreatureView3ExtendedDeltaCodec.Decode(envelope,limits,policy),limits).SequenceEqual(wire));
            Check(ReplicationEnvelopeCodec.EncodeDelta(CreatureView3ExtendedDeltaCodec.EncodeEnvelope(plan,limits),full.MaximumPayloadBytes).SequenceEqual(wire));
            if(policy==CreatureWoundsApplicationPolicy.StrictInterval)Check(CreatureView3ExtendedDeltaCodec.Encode(CreatureView3ExtendedDeltaCodec.Decode(wire,limits),limits).SequenceEqual(wire));
            Check(plan.DeclaredCount==BinaryPrimitives.ReadUInt16LittleEndian(wire.AsSpan(23)));Check(plan.WoundsPolicy==policy);
            var rows=c.GetProperty("Rows").EnumerateArray().ToArray();Check(rows.Length==plan.Updates.Count);
            long decoded=0;int raw=0,scalar=0,tangible=0,cc=0,ck=0,wc=0,wv=0;
            for(int i=0;i<rows.Length;i++)
            {
                var update=plan.Updates[i];ushort index=rows[i].GetProperty("Index").GetUInt16();Check(update.Index==index);
                byte[] bytes=Convert.FromHexString(rows[i].GetProperty("Hex").GetString()!);
                if(update.Tangible is {} t)
                {
                    tangible++;int width=index==10?1:4;decoded+=width;
                    if(index==0)Check(BitConverter.SingleToInt32Bits(t.Complexity)==BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(2)));
                    else if(index==10)Check(t.Visibility==bytes[2]);else Check(t.Value==BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(2)));
                }
                else
                {
                    byte[] single=Append(wire[..25],bytes);var sibling=CreatureView3FullDeltaCodec.Decode(single,full,policy);
                    Check(CreatureView3FullDeltaCodec.Encode(sibling,full).SequenceEqual(single));
                    var u=update.Full!;
                    if(u.Customization is {} value){raw++;decoded+=value.Length;Check(value.CopyBytes().SequenceEqual(sibling.Updates[0].Customization!.CopyBytes()));var copy=value.CopyBytes();Array.Clear(copy);}
                    if(u.Scalar is {} v){scalar++;decoded+=v.Index is 11 or 12?1:v.Index is 13 or 16?8:4;}
                    if(u.Components is {} component){cc+=component.Commands.Count;ck+=component.Commands.Count(cmd=>cmd.Kind!=CreatureComponentsCommandKind.Clear);Check(component.Counter==sibling.Updates[0].Components!.Counter);}
                    if(u.Wounds is {} wounds){wc+=checked((int)wounds.CommandCount);wv+=wounds.Commands.Sum(cmd=>cmd.Values.Count);Check(wounds.CommandCount==sibling.Updates[0].Wounds!.CommandCount);Check(wounds.Counter==sibling.Updates[0].Wounds!.Counter);}
                }
            }
            decoded+=4L*(ck+wv);
            var exactLimits=limits with{MaximumTangibleUpdates=tangible,Full=full with{MaximumPayloadBytes=wire.Length-23,MaximumFields=plan.Updates.Count,MaximumDecodedBytes=decoded,MaximumRawUpdates=raw,MaximumScalarUpdates=scalar,MaximumComponentCommands=cc,MaximumComponentKeys=ck,MaximumWoundsCommands=wc,MaximumWoundsValues=wv}};
            Check(CreatureView3ExtendedDeltaCodec.Encode(plan,exactLimits).SequenceEqual(wire));Check(CreatureView3ExtendedDeltaCodec.Decode(wire,exactLimits,policy).Updates.Count==rows.Length);
            var caps=new List<CreatureView3ExtendedDeltaLimits>{limits with{Full=full with{MaximumFields=plan.Updates.Count-1}},limits with{Full=full with{MaximumPayloadBytes=wire.Length-24}}};
            if(decoded>0)caps.Add(limits with{Full=full with{MaximumDecodedBytes=decoded-1}});
            if(tangible>0)caps.Add(limits with{MaximumTangibleUpdates=tangible-1});
            if(raw>0)caps.Add(limits with{Full=full with{MaximumRawUpdates=raw-1}});
            if(scalar>0)caps.Add(limits with{Full=full with{MaximumScalarUpdates=scalar-1}});
            if(cc>0)caps.Add(limits with{Full=full with{MaximumComponentCommands=cc-1}});
            if(ck>0)caps.Add(limits with{Full=full with{MaximumComponentKeys=ck-1}});
            if(wc>0)caps.Add(limits with{Full=full with{MaximumWoundsCommands=wc-1}});
            if(wv>0)caps.Add(limits with{Full=full with{MaximumWoundsValues=wv-1}});
            if(plan.Updates.Any(u=>u.Full?.Customization?.Length>0))caps.Add(limits with{Full=full with{MaximumStringBytes=0}});
            foreach(var cap in caps){Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(wire,cap,policy));Reject(()=>CreatureView3ExtendedDeltaCodec.Encode(plan,cap));}
            IEnumerable<int> prefixes=wire.Length<2048?Enumerable.Range(0,wire.Length):Enumerable.Range(0,64).Concat(Enumerable.Range(wire.Length-16,16));
            var boundaries=new HashSet<int>();int end=25;foreach(var row in rows){end+=row.GetProperty("Hex").GetString()!.Length/2;boundaries.Add(end);}
            foreach(int n in prefixes)
            {
                var prefix=wire[..n];Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(prefix,limits,policy));
                if(n>=25){BinaryPrimitives.WriteUInt32LittleEndian(prefix.AsSpan(19),(uint)(n-23));if(boundaries.Contains(n))Check(CreatureView3ExtendedDeltaCodec.Encode(CreatureView3ExtendedDeltaCodec.Decode(prefix,limits,policy),limits).SequenceEqual(prefix));else Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(prefix,limits,policy));}
            }
            Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Append(wire,new byte[]{5,0,1,0,0,0,0,0,0,0,3}),limits,policy));
            Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Append(wire,new byte[]{17,0,1,0,0,0,0,0,0,0,5}),limits,policy));
            Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Append(wire,new byte[]{17,0,1,0,0,0,0,0,0,0,3,255,255}),limits,policy));
            if(plan.Updates.Count==4096)Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Append(wire,new byte[]{10,0,1}),limits,policy));
            foreach(ushort badIndex in new ushort[]{1,2,18,65535}){byte[] tail=new byte[2];BinaryPrimitives.WriteUInt16LittleEndian(tail,badIndex);Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Append(wire,tail),limits,policy));}
            foreach(ushort floatIndex in new ushort[]{0,14})foreach(uint bits in new uint[]{0x7f800000,0xff800000,0x7fc00000,0x7f800001}){byte[] tail=new byte[6];BinaryPrimitives.WriteUInt16LittleEndian(tail,floatIndex);BinaryPrimitives.WriteUInt32LittleEndian(tail.AsSpan(2),bits);Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Append(wire,tail),limits,policy));}
            foreach(int pos in new[]{0,2,14,18,19}){var bad=(byte[])wire.Clone();bad[pos]^=1;Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(bad,limits,policy));}
            var alias=(byte[])wire.Clone();var owned=CreatureView3ExtendedDeltaCodec.Decode(alias,limits,policy);Array.Clear(alias);var output=CreatureView3ExtendedDeltaCodec.Encode(owned,limits);Array.Clear(output);Check(CreatureView3ExtendedDeltaCodec.Encode(owned,limits).SequenceEqual(wire));
            var copyUpdates=plan.Updates.ToArray();var copied=new CreatureView3ExtendedDeltaPlan(plan.ObjectId,plan.DeclaredCount,copyUpdates,policy,plan.MaximumWoundsElements,plan.MaximumComponentsElements);Array.Clear(copyUpdates);Check(CreatureView3ExtendedDeltaCodec.Encode(copied,limits).SequenceEqual(wire));
            if(c.TryGetProperty("MaximumElements",out _))
            {
                var row=rows.Single(r=>r.GetProperty("Index").GetInt32()==17);byte[] single=Append(wire[..25],Convert.FromHexString(row.GetProperty("Hex").GetString()!));var sibling=CreatureView3WoundsDeltaCodec.Decode(single,new(1048576,1,4,3,1),policy);var initial=new CreatureView3WoundsState(plan.ObjectId,5,Array.Empty<int>(),1);
                if(sibling.Updates[0].Counter==5){var result=sibling.Apply(initial);Check(result.Counter==5&&result.Values.Count==0);}else Reject(()=>sibling.Apply(initial));
                Check(plan.Updates.Select(u=>u.Index).SequenceEqual(new ushort[]{0,4,17,4,10}));
            }
            exports.Add(new{Hex=Convert.ToHexString(encoded),Indices=plan.Updates.Select(u=>u.Index).ToArray()});exact++;
        }
        var normal=new CreatureView3ExtendedDeltaLimits(Full,4096);
        byte[] excessiveComponents=new byte[25+10+4097];BinaryPrimitives.WriteUInt16LittleEndian(excessiveComponents,5);BinaryPrimitives.WriteUInt32LittleEndian(excessiveComponents.AsSpan(2),ReplicationEnvelopeCodec.DeltaOpcode);BinaryPrimitives.WriteUInt32LittleEndian(excessiveComponents.AsSpan(14),CreatureView3ExtendedDeltaCodec.RawTag);excessiveComponents[18]=3;BinaryPrimitives.WriteUInt32LittleEndian(excessiveComponents.AsSpan(19),(uint)(excessiveComponents.Length-23));excessiveComponents[25]=5;BinaryPrimitives.WriteUInt32LittleEndian(excessiveComponents.AsSpan(27),4097);excessiveComponents.AsSpan(35).Fill(2);
        Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(excessiveComponents,normal));
        Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,1,CreatureView3ExtendedDeltaCodec.RawTag,3,1,new byte[]{10,0,1},5),normal));
        foreach(var invalid in new[]{normal with{MaximumTangibleUpdates=-1},normal with{MaximumTangibleUpdates=4097},normal with{Full=Full with{MaximumDecodedBytes=-1}},normal with{Full=Full with{MaximumFields=4097}},normal with{Full=Full with{MaximumWoundsElements=-1}},normal with{Full=Full with{MaximumComponentCommands=65537}}})Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Array.Empty<byte>(),invalid));
        Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(Array.Empty<byte>(),normal,(CreatureWoundsApplicationPolicy)99));
        Reject(()=>CreatureView3ExtendedDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3ExtendedDeltaCodec.RawTag,3,1,Array.Empty<byte>(),2),normal));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(exports));Console.WriteLine($"Extended codec: {exact} exact, {negative} negatives, {checks} ownership/cap/sibling checks");
    }
}
