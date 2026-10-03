using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;

public static class CreatureView3CustomizationScalarDeltaScenarios
{
    public sealed record Row(ushort Index,string Hex,bool Extended);
    public sealed record Fixture(string Name,Row[] Rows,ushort DeclaredCount,string Wire);
    public sealed record Bad(string Name,string Wire);
    public sealed record Peer(Fixture[] Cases,Bad[] Negatives);
    private static CreatureView3CustomizationScalarUpdate Make(Row row)
    {
        var bytes=Convert.FromHexString(row.Hex);
        if(row.Index==4){var owned=ReplicationByteString.Create(bytes,row.Extended);Array.Clear(bytes);return CreatureView3CustomizationScalarUpdate.FromCustomization(owned);}
        CreatureView3ScalarUpdate scalar=row.Index switch
        {
            11=>new CreaturePostureUpdate(bytes[0]),12=>new CreatureRankUpdate(bytes[0]),13=>new CreatureLinkUpdate(BinaryPrimitives.ReadUInt64LittleEndian(bytes)),
            14=>new CreatureHeightUpdate(BinaryPrimitives.ReadSingleLittleEndian(bytes)),15=>new CreatureShockUpdate(BinaryPrimitives.ReadUInt32LittleEndian(bytes)),
            16=>new CreatureStatesUpdate(BinaryPrimitives.ReadUInt64LittleEndian(bytes)),_=>throw new Exception("Fixture index")
        };
        return CreatureView3CustomizationScalarUpdate.FromScalar(scalar);
    }
    private static Row Observe(CreatureView3CustomizationScalarUpdate update)
    {
        if(update.Customization is{} raw)return new(4,Convert.ToHexString(raw.CopyBytes()),raw.UsesExtendedLength);
        byte[] bytes=new byte[update.Index is 11 or 12?1:update.Index is 13 or 16?8:4];
        switch(update.Scalar)
        {
            case CreaturePostureUpdate v:bytes[0]=v.Value;break;case CreatureRankUpdate v:bytes[0]=v.Value;break;
            case CreatureLinkUpdate v:BinaryPrimitives.WriteUInt64LittleEndian(bytes,v.Value);break;
            case CreatureHeightUpdate v:BinaryPrimitives.WriteSingleLittleEndian(bytes,v.Value);break;
            case CreatureShockUpdate v:BinaryPrimitives.WriteUInt32LittleEndian(bytes,v.Value);break;
            case CreatureStatesUpdate v:BinaryPrimitives.WriteUInt64LittleEndian(bytes,v.Value);break;
        }
        return new(update.Index,Convert.ToHexString(bytes),false);
    }
    public static void Run(string peerPath,string exportPath)
    {
        var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        var limits=new CreatureView3CustomizationScalarDeltaLimits(262144,4096,4096,4096,131072,262144);
        var results=new List<object>();int negatives=0,checks=0;
        void Check(bool v,string why){if(!v)throw new Exception(why);checks++;}
        void Reject<T>(Action a)where T:Exception{try{a();}catch(T){negatives++;return;}throw new Exception("Expected "+typeof(T).Name);}
        foreach(var f in peer.Cases)
        {
            byte[] input=Convert.FromHexString(f.Wire);var decoded=CreatureView3CustomizationScalarDeltaCodec.Decode(input,limits);Array.Clear(input);
            Check(decoded.DeclaredCount==f.DeclaredCount&&decoded.Updates.Count==f.Rows.Length,"Raw count/update order");
            for(int i=0;i<f.Rows.Length;i++)
            {
                Check(Observe(decoded.Updates[i])==f.Rows[i],"Exact union row");
                if(decoded.Updates[i].Customization is{} raw){var b=raw.CopyBytes();Array.Clear(b);Check(Observe(decoded.Updates[i])==f.Rows[i],"Raw byte copy ownership");}
            }
            var list=f.Rows.Select(Make).ToList();var plan=new CreatureView3CustomizationScalarDeltaPlan(decoded.ObjectId,f.DeclaredCount,list);list.Clear();
            Check(Convert.ToHexString(CreatureView3CustomizationScalarDeltaCodec.Encode(plan,limits))==f.Wire,"Constructed ordering/list/raw ownership");
            var encoded=CreatureView3CustomizationScalarDeltaCodec.Encode(decoded,limits);Check(Convert.ToHexString(encoded)==f.Wire,"Exact application");Array.Clear(encoded);
            var env=CreatureView3CustomizationScalarDeltaCodec.EncodeEnvelope(decoded,limits);var copy=env.CopyOpaqueFields();Array.Clear(copy);
            Check(Convert.ToHexString(CreatureView3CustomizationScalarDeltaCodec.Encode(CreatureView3CustomizationScalarDeltaCodec.Decode(env,limits),limits))==f.Wire,"Envelope/output ownership");
            int rawCount=f.Rows.Count(r=>r.Index==4),scalarCount=f.Rows.Length-rawCount,maxString=f.Rows.Where(r=>r.Index==4).Select(r=>r.Hex.Length/2).DefaultIfEmpty().Max();
            long decodedBytes=f.Rows.Sum(r=>(long)r.Hex.Length/2);
            var exact=limits with{MaximumPayloadBytes=(int)env.PayloadSize,MaximumFields=f.Rows.Length,MaximumRawUpdates=rawCount,MaximumScalarUpdates=scalarCount,MaximumStringBytes=maxString,MaximumDecodedBytes=decodedBytes};
            Check(Convert.ToHexString(CreatureView3CustomizationScalarDeltaCodec.Encode(CreatureView3CustomizationScalarDeltaCodec.Decode(Convert.FromHexString(f.Wire),exact),exact))==f.Wire,"Independent exact inclusive caps");
            var restrictive=new List<CreatureView3CustomizationScalarDeltaLimits>{limits with{MaximumPayloadBytes=(int)env.PayloadSize-1},limits with{MaximumFields=f.Rows.Length-1},limits with{MaximumDecodedBytes=decodedBytes-1}};
            if(rawCount>0)restrictive.Add(limits with{MaximumRawUpdates=rawCount-1});
            if(scalarCount>0)restrictive.Add(limits with{MaximumScalarUpdates=scalarCount-1});
            if(maxString>0)restrictive.Add(limits with{MaximumStringBytes=maxString-1});
            foreach(var cap in restrictive)
            {
                Reject<InvalidDataException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode(env,cap));
                Reject<InvalidDataException>(()=>CreatureView3CustomizationScalarDeltaCodec.EncodeEnvelope(decoded,cap));
            }
            var full=Convert.FromHexString(f.Wire);var prefixes=full.Length<2048?Enumerable.Range(0,full.Length):Enumerable.Range(0,40).Concat(Enumerable.Range(full.Length-16,16));
            foreach(int n in prefixes)Reject<InvalidDataException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode(full.AsSpan(0,n),limits));
            Reject<InvalidDataException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode(full.Concat(new byte[]{0}).ToArray(),limits));
            results.Add(new{f.Name,decoded.ObjectId,decoded.DeclaredCount,Rows=decoded.Updates.Select(Observe).ToArray(),Wire=Convert.ToHexString(CreatureView3CustomizationScalarDeltaCodec.Encode(decoded,limits))});
        }
        foreach(var f in peer.Negatives)Reject<InvalidDataException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode(Convert.FromHexString(f.Wire),limits));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode((ReplicationEnvelope)null!,limits));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode([],null!));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationScalarDeltaCodec.Encode(null!,limits));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationScalarDeltaCodec.EncodeEnvelope(null!,limits));
        foreach(var invalid in new[]{limits with{MaximumPayloadBytes=1},limits with{MaximumPayloadBytes=int.MaxValue},limits with{MaximumFields=-1},limits with{MaximumFields=4097},limits with{MaximumScalarUpdates=-1},limits with{MaximumScalarUpdates=4097},limits with{MaximumRawUpdates=-1},limits with{MaximumRawUpdates=4097},limits with{MaximumStringBytes=-1},limits with{MaximumDecodedBytes=-1}})
        {Reject<ArgumentOutOfRangeException>(()=>CreatureView3CustomizationScalarDeltaCodec.Decode([],invalid));Reject<ArgumentOutOfRangeException>(()=>CreatureView3CustomizationScalarDeltaCodec.Encode(new(1,0,new[]{Make(new(11,"00",false))}),invalid));}
        // Existing sibling encoders are byte-identical for each isolated accepted scalar/raw update.
        foreach(var row in peer.Cases.SelectMany(f=>f.Rows).Distinct().Where(r=>r.Hex.Length<1024))
        {
            var one=new CreatureView3CustomizationScalarDeltaPlan(0xF122334455667788UL,65535,new[]{Make(row)});
            string wire=Convert.ToHexString(CreatureView3CustomizationScalarDeltaCodec.Encode(one,limits));
            var plan=CreatureView3CustomizationScalarDeltaCodec.Decode(Convert.FromHexString(wire),limits);
            byte[] sibling=plan.Updates[0].Customization is{} raw?
                CreatureView3CustomizationDeltaCodec.Encode(new(plan.ObjectId,plan.DeclaredCount,new[]{raw}),new(262144,4096,131072,262144)):
                CreatureView3ScalarDeltaCodec.Encode(CreatureView3ScalarDeltaCodec.Decode(Convert.FromHexString(wire),262144,4096),262144,4096);
            Check(Convert.ToHexString(sibling)==wire,"Accepted sibling encoding agrees");
        }
        foreach(float invalidHeight in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            Reject<InvalidDataException>(()=>CreatureView3CustomizationScalarUpdate.FromScalar(new CreatureHeightUpdate(invalidHeight)));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Cases=results,Negatives=negatives,OwnershipChecks=checks},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Customization/scalar: {results.Count} exact cases, {negatives} negatives, {checks} ownership/cap checks passed");
    }
}
