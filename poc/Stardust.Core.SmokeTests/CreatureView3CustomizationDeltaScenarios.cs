using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;

public static class CreatureView3CustomizationDeltaScenarios
{
    public sealed record Update(string Hex,bool Extended);
    public sealed record Fixture(string Name,ulong ObjectId,ushort DeclaredCount,Update[] Updates,string Wire);
    public sealed record Bad(string Name,string Wire);
    public sealed record Peer(Fixture[] Cases,Bad[] Negatives);
    public static void Run(string peerPath,string exportPath)
    {
        var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        var limits=new CreatureView3CustomizationDeltaLimits(262144,4096,131072,262144);
        var results=new List<object>();int negatives=0,ownership=0;
        void Check(bool v,string why){if(!v)throw new Exception(why);ownership++;}
        void Reject<T>(Action a)where T:Exception{try{a();}catch(T){negatives++;return;}throw new Exception("Expected "+typeof(T).Name);}
        foreach(var f in peer.Cases)
        {
            byte[] source=Convert.FromHexString(f.Wire);
            var decoded=CreatureView3CustomizationDeltaCodec.Decode(source,limits);Array.Clear(source);
            Check(decoded.ObjectId==f.ObjectId&&decoded.DeclaredCount==f.DeclaredCount&&decoded.Updates.Count==f.Updates.Length,"Raw metadata/count");
            var values=new List<ReplicationByteString>();
            for(int i=0;i<f.Updates.Length;i++)
            {
                var update=decoded.Updates[i];var expected=f.Updates[i];var copy=update.CopyBytes();
                Check(Convert.ToHexString(copy)==expected.Hex&&update.UsesExtendedLength==expected.Extended,"Exact ordered raw update");
                Array.Clear(copy);Check(Convert.ToHexString(update.CopyBytes())==expected.Hex,"Output clone ownership");
                var raw=Convert.FromHexString(expected.Hex);values.Add(ReplicationByteString.Create(raw,expected.Extended));Array.Clear(raw);
            }
            var constructed=new CreatureView3CustomizationDeltaPlan(f.ObjectId,f.DeclaredCount,values);values.Clear();
            Check(Convert.ToHexString(CreatureView3CustomizationDeltaCodec.Encode(constructed,limits))==f.Wire,"Constructed/Create/list ownership");
            byte[] encoded=CreatureView3CustomizationDeltaCodec.Encode(decoded,limits);Check(Convert.ToHexString(encoded)==f.Wire,"Exact span encoding");Array.Clear(encoded);
            Check(Convert.ToHexString(CreatureView3CustomizationDeltaCodec.Encode(decoded,limits))==f.Wire,"Encode copy ownership");
            var envelope=CreatureView3CustomizationDeltaCodec.EncodeEnvelope(decoded,limits);var fields=envelope.CopyOpaqueFields();Array.Clear(fields);
            var again=CreatureView3CustomizationDeltaCodec.Decode(envelope,limits);
            Check(Convert.ToHexString(CreatureView3CustomizationDeltaCodec.Encode(again,limits))==f.Wire,"Owned envelope APIs");
            byte[] full=Convert.FromHexString(f.Wire);
            // Exhaustive small prefixes; large extent cases probe boundary-adjacent and final prefixes.
            var lengths=full.Length<2048?Enumerable.Range(0,full.Length):Enumerable.Range(0,40).Concat(Enumerable.Range(full.Length-16,16));
            foreach(int n in lengths)Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(full.AsSpan(0,n),limits));
            Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(full.Concat(new byte[]{0}).ToArray(),limits));
            Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(envelope,limits with{MaximumPayloadBytes=(int)envelope.PayloadSize-1}));
            Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Encode(decoded,limits with{MaximumPayloadBytes=(int)envelope.PayloadSize-1}));
            Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(full,limits with{MaximumFields=f.Updates.Length-1}));
            Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.EncodeEnvelope(decoded,limits with{MaximumFields=f.Updates.Length-1}));
            int max=f.Updates.Max(u=>u.Hex.Length/2);long sum=f.Updates.Sum(u=>(long)u.Hex.Length/2);
            if(max>0)
            {
                Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(full,limits with{MaximumStringBytes=max-1}));
                Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Encode(decoded,limits with{MaximumStringBytes=max-1}));
                Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(full,limits with{MaximumDecodedBytes=sum-1}));
                Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.EncodeEnvelope(decoded,limits with{MaximumDecodedBytes=sum-1}));
            }
            var exact=limits with{MaximumPayloadBytes=(int)envelope.PayloadSize,MaximumFields=f.Updates.Length,MaximumStringBytes=max,MaximumDecodedBytes=sum};
            Check(Convert.ToHexString(CreatureView3CustomizationDeltaCodec.Encode(CreatureView3CustomizationDeltaCodec.Decode(full,exact),exact))==f.Wire,"Exact inclusive caps");
            results.Add(new{f.Name,decoded.ObjectId,decoded.DeclaredCount,Updates=decoded.Updates.Select(u=>new Update(Convert.ToHexString(u.CopyBytes()),u.UsesExtendedLength)).ToArray(),Wire=Convert.ToHexString(CreatureView3CustomizationDeltaCodec.Encode(decoded,limits))});
        }
        foreach(var bad in peer.Negatives)
        {
            var wire=Convert.FromHexString(bad.Wire);Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(wire,limits));
            try { var envelope=ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes);Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(envelope,limits)); }
            catch(InvalidDataException){ /* Outer malformed paths cannot form an owned envelope. */ }
        }
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationDeltaCodec.Decode((ReplicationEnvelope)null!,limits));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationDeltaCodec.Decode([],null!));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationDeltaCodec.Encode(null!,limits));
        Reject<ArgumentNullException>(()=>CreatureView3CustomizationDeltaCodec.EncodeEnvelope(null!,limits));
        foreach(var bad in new[]{limits with{MaximumPayloadBytes=1},limits with{MaximumPayloadBytes=int.MaxValue},limits with{MaximumFields=-1},limits with{MaximumFields=4097},limits with{MaximumStringBytes=-1},limits with{MaximumDecodedBytes=-1}})
        {
            Reject<ArgumentOutOfRangeException>(()=>CreatureView3CustomizationDeltaCodec.Decode([],bad));
            var plan=new CreatureView3CustomizationDeltaPlan(1,0,new[]{ReplicationByteString.Create([])});
            Reject<ArgumentOutOfRangeException>(()=>CreatureView3CustomizationDeltaCodec.Encode(plan,bad));
        }
        var baseline=new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,1,0x4352454F,3,1,new byte[]{4,0,0,0},4096);
        Reject<InvalidDataException>(()=>CreatureView3CustomizationDeltaCodec.Decode(baseline,limits));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Cases=results,Negatives=negatives,OwnershipChecks=ownership},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Customization delta: {results.Count} exact cases, {negatives} negatives, {ownership} ownership checks passed");
    }
}
