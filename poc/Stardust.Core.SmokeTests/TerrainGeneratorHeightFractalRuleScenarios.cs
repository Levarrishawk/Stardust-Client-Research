using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainGeneratorHeightFractalRuleScenarios
{
    private static string Sha(byte[] value)=>Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool value){if(!value)throw new InvalidOperationException("Selection ownership invariant.");}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var p)?p.GetInt32():fallback;
    private static TerrainGeneratorHeightFractalRule Select(TerrainGeneratorInput g,TerrainGeneratorFractalFamilies b,JsonElement c)
        =>TerrainGeneratorHeightFractalRule.Select(g,b,I(c,"Index",0),(TerrainGeneratorHeightFractalRulePolicy)I(c,"Policy",1),
            new(I(c,"MaximumRuleBytes",65536),I(c,"MaximumDecodedCopyBytes",65536),I(c,"MaximumNameBytes",4096),
                I(c,"MaximumSnapshotBytes",1048576),I(c,"MaximumWork",1048576)),new CancellationToken(c.TryGetProperty("Cancel",out var p)&&p.GetBoolean()));
    private static string Fraction(TerrainSuppliedFractalSample sample)
    {
        if(sample.Magnitude==0)return sample.NegativeZero?"-0/1":"0/1";
        BigInteger n=sample.Magnitude,d=BigInteger.One;if(sample.IsNegative)n=-n;
        if(sample.Exponent>=0)n<<=sample.Exponent;else d<<=-sample.Exponent;
        var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";
    }
    private static object Snapshot(TerrainGeneratorHeightFractalRule r)=>new{
        r.GeneratorSha256,r.GeneratorBytes,r.GroupSha256,r.GroupOffset,r.GroupBytes,r.RuleIndex,r.LayerIndex,r.ChildOrdinal,
        r.RuleSha256,r.RuleOffset,r.RuleBytes,r.FamilyId,r.FamilyGeneratorOffset,FamilyBytes=r.Family.EncodedBytes,
        RawSha256=Sha(r.Family.Raw.CopyEncodedBytes()),r.IsEnabled,r.HasOperationalInputs,Policy=(int)r.Policy,
        r.SnapshotBytes,r.ReservedSnapshotBytes,r.Work,r.ReservedWork,DecodedCopyBytes=r.Input.DecodedCopyBytes,
        r.Input.Version,HeaderVersion=r.Header.Version,r.Header.EnabledBits,
        NameSha256=Sha(r.Header.CopyNameBytes()),LegacySha256=Sha(r.Header.CopyLegacyBytes()),
        r.Fields.FamilyKeyBits,r.Fields.OperationBits,r.Fields.ScaleBits,EncodedSha256=Sha(r.CopyEncodedBytes())};
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();object? route=null;
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var g=TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!));
                var bg=c.TryGetProperty("BindingHex",out var alternate)?TerrainGeneratorInput.Decode(Convert.FromHexString(alternate.GetString()!)):g;
                var binding=TerrainGeneratorFractalFamilies.Bind(bg);var selected=Select(g,binding,c);
                Check(ReferenceEquals(selected.Family,binding.Families.Find(selected.FamilyId)));
                cases.Add(new{Name=name,Reject="None",Snapshot=Snapshot(selected)});
                if(c.TryGetProperty("Original",out var original)&&original.GetBoolean())
                {
                    // Caller controls and coordinates are synthetic; the copied rule and family are original evidence.
                    var receipt=TerrainFamilyHeightFractal.Evaluate(binding.Families,selected.CopyEncodedBytes(),
                        new(2,2,0,0,0x3e000000,0x3ec00000,0xbe800000,0x3f400000),
                        new(53,53,53,(TerrainHeightArithmeticPrecision)53));
                    Check(receipt.Rule.CopyEncodedBytes().SequenceEqual(selected.CopyEncodedBytes()));
                    route=new{FamilyId=receipt.Family.Id,SourceSha256=receipt.FamilySetSha256,Origin=receipt.Origin.ToString(),
                        Retained=Fraction(receipt.Sample),Projection=receipt.Sample.ProjectionBits,Result=receipt.Affector.Consumer.ResultHeightBits,
                        receipt.AccountedBytes,receipt.AccountedWork};
                }
            }
            catch(Exception ex)when(ex is InvalidDataException or ArgumentException or OperationCanceledException)
            {cases.Add(new{Name=name,Reject="Input"});}
        }
        var first=doc.RootElement[0];var source=Convert.FromHexString(first.GetProperty("GeneratorHex").GetString()!);
        var generator=TerrainGeneratorInput.Decode(source);var families=TerrainGeneratorFractalFamilies.Bind(generator);
        var a=Select(generator,families,first);var expected=a.CopyEncodedBytes();string before=JsonSerializer.Serialize(Snapshot(a));int checks=0;
        Array.Fill(source,(byte)0);Check(a.CopyEncodedBytes().SequenceEqual(expected));checks++;
        var exported=a.CopyEncodedBytes();Array.Fill(exported,(byte)0);Check(a.CopyEncodedBytes().SequenceEqual(expected));checks++;
        var words=a.Input.CopyDataWordBits();Array.Fill(words,0u);Check(a.Fields.FamilyKeyBits!=words[0]);checks++;
        var nameBytes=a.Header.CopyNameBytes();Array.Fill(nameBytes,(byte)0);Check(!a.Header.CopyNameBytes().SequenceEqual(nameBytes));checks++;
        var familyName=a.Family.CopyNameBytes();Array.Fill(familyName,(byte)0xff);Check(!a.Family.CopyNameBytes().SequenceEqual(familyName));checks++;
        var raw=a.Family.Raw.CopyEncodedBytes();Array.Fill(raw,(byte)0);Check(Sha(a.Family.Raw.CopyEncodedBytes())!=Sha(raw));checks++;
        try{TerrainGeneratorHeightFractalRule.Select(generator,families,-1,TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);throw new InvalidOperationException("Expected rejection.");}
        catch(ArgumentOutOfRangeException){Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;}
        var equivalent=TerrainGeneratorInput.Decode(generator.CopyGeneratorForm());var again=Select(equivalent,families,first);
        Check(Sha(again.CopyEncodedBytes())==a.RuleSha256&&before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var large=doc.RootElement.EnumerateArray().First(c=>c.GetProperty("Name").GetString()=="precopy-large-budget");
        var largeGenerator=TerrainGeneratorInput.Decode(Convert.FromHexString(large.GetProperty("GeneratorHex").GetString()!));
        var largeFamilies=TerrainGeneratorFractalFamilies.Bind(largeGenerator);
        long allocated=GC.GetAllocatedBytesForCurrentThread();bool rejected=false;
        try{Select(largeGenerator,largeFamilies,large);}catch(InvalidDataException){rejected=true;}
        long delta=GC.GetAllocatedBytesForCurrentThread()-allocated;
        Check(rejected&&delta<16384&&before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        return new{Cases=cases,Route=route,OwnershipChecks=checks};
    }
}
