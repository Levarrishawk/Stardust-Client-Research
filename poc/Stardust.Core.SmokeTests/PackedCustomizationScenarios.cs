using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;

public static class PackedCustomizationScenarios
{
    public sealed record Row(byte Id,string Value);
    public sealed record Fixture(string Name,bool OuterEmpty,Row[] Rows,string Wire,string Canonical);
    public static void Run(string peerPath,string exportPath)
    {
        var fixtures=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(peerPath))!;
        var results=new List<object>();int negatives=0,ownership=0;
        void Check(bool v,string why){if(!v)throw new Exception(why);ownership++;}
        void Reject<T>(Action a)where T:Exception{try{a();}catch(T){negatives++;return;}throw new Exception("Expected "+typeof(T).Name);}
        foreach(var f in fixtures)
        {
            byte[] wire=Convert.FromHexString(f.Wire);var decoded=PackedCustomizationCodec.Decode(wire);Array.Clear(wire);
            Check(decoded.IsOuterEmpty==f.OuterEmpty&&decoded.Version==(f.OuterEmpty?null:(byte?)1),"Empty versus version1");
            Check(decoded.Records.Count==f.Rows.Length,"Record count");
            var constructed=new List<PackedCustomizationRecord>();
            for(int i=0;i<f.Rows.Length;i++)
            {
                var r=decoded.Records[i];var expected=f.Rows[i];var raw=r.CopyRawValue();
                Check(r.Id==expected.Id&&Convert.ToHexString(raw)==expected.Value,"Exact ordered raw record");Array.Clear(raw);
                Check(Convert.ToHexString(r.CopyRawValue())==expected.Value,"Raw copy ownership");
                var input=Convert.FromHexString(expected.Value);constructed.Add(new(expected.Id,input));Array.Clear(input);
            }
            var snapshot=new PackedCustomizationSnapshot(f.OuterEmpty,constructed);constructed.Clear();
            byte[] encoded=PackedCustomizationCodec.Encode(decoded);Check(Convert.ToHexString(encoded)==f.Wire,"Decoded wire identity");Array.Clear(encoded);
            Check(Convert.ToHexString(PackedCustomizationCodec.Encode(decoded))==f.Wire,"Encode ownership");
            Check(Convert.ToHexString(PackedCustomizationCodec.Encode(snapshot))==f.Canonical,"Constructed canonical encode");
            Check(snapshot.Records.Count==f.Rows.Length,"Snapshot list ownership");
            var full=Convert.FromHexString(f.Wire);
            if(full.Length>0)
            {
                // Prefix zero is the legal outer-empty alternative, not a truncated v1 failure.
                for(int n=1;n<full.Length;n++){int length=n;Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(full.AsSpan(0,length)));}
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(full.Concat(new byte[]{0}).ToArray()));
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(full,new(MaximumEncodedBytes:full.Length-1)));
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Encode(decoded,new(MaximumEncodedBytes:full.Length-1)));
            }
            if(decoded.DecodedByteCount>0)
            {
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(full,new(MaximumDecodedBytes:decoded.DecodedByteCount-1)));
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Encode(decoded,new(MaximumDecodedBytes:decoded.DecodedByteCount-1)));
            }
            if(f.Rows.Length>0)
            {
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(full,new(MaximumRecords:f.Rows.Length-1)));
                Reject<InvalidDataException>(()=>PackedCustomizationCodec.Encode(snapshot,new(MaximumRecords:f.Rows.Length-1)));
            }
            results.Add(new{f.Name,OuterEmpty=decoded.IsOuterEmpty,Rows=decoded.Records.Select(r=>new Row(r.Id,Convert.ToHexString(r.CopyRawValue()))).ToArray(),Wire=Convert.ToHexString(PackedCustomizationCodec.Encode(decoded)),Canonical=Convert.ToHexString(PackedCustomizationCodec.Encode(snapshot)),DecodedBytes=decoded.DecodedByteCount});
        }
        foreach(var hex in new[]{"01","01FF","0101FF03","01010001FF03","0101800102FF03","0200FF03","01010101","01010101FF02","01010101FF0300","010101FF03FF03","010101FF00FF03","010101FF04FF03"})
            Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(Convert.FromHexString(hex)));
        for(int code=0;code<256;code++)if(code is not 1 and not 2)
        {byte c=(byte)code;Reject<InvalidDataException>(()=>PackedCustomizationCodec.Decode(new byte[]{1,1,1,255,c,255,3}));}
        Reject<ArgumentOutOfRangeException>(()=>new PackedCustomizationRecord(0,new byte[]{1}));
        Reject<ArgumentOutOfRangeException>(()=>new PackedCustomizationRecord(128,new byte[]{1}));
        Reject<ArgumentOutOfRangeException>(()=>new PackedCustomizationRecord(1,Array.Empty<byte>()));
        Reject<ArgumentOutOfRangeException>(()=>new PackedCustomizationRecord(1,new byte[3]));
        Reject<ArgumentOutOfRangeException>(()=>new PackedCustomizationSnapshot(true,new[]{new PackedCustomizationRecord(1,new byte[]{1})}));
        Reject<ArgumentOutOfRangeException>(()=>new PackedCustomizationSnapshot(false,Enumerable.Repeat(new PackedCustomizationRecord(1,new byte[]{1}),256).ToArray()));
        Reject<ArgumentException>(()=>new PackedCustomizationSnapshot(false,new PackedCustomizationRecord[]{null!}));
        Reject<ArgumentNullException>(()=>new PackedCustomizationSnapshot(false,null!));
        Reject<ArgumentNullException>(()=>PackedCustomizationCodec.Encode(null!));
        foreach(var bad in new[]{new PackedCustomizationLimits(-1),new(1048577),new(MaximumRecords:-1),new(MaximumRecords:256),new(MaximumDecodedBytes:-1),new(MaximumDecodedBytes:768)})
        {Reject<ArgumentOutOfRangeException>(()=>PackedCustomizationCodec.Decode([],bad));Reject<ArgumentOutOfRangeException>(()=>PackedCustomizationCodec.Encode(new(true,Array.Empty<PackedCustomizationRecord>()),bad));}
        Check(PackedCustomizationCodec.Decode([],new(0,0,0)).IsOuterEmpty,"Zero budgets allow outer empty");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Cases=results,Negatives=negatives,OwnershipChecks=ownership},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Packed customization: {results.Count} exact cases, {negatives} negatives, {ownership} ownership checks passed");
    }
}
