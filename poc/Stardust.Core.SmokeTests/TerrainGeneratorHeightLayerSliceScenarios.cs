using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
namespace Stardust.Core.SmokeTests;
public static class TerrainGeneratorHeightLayerSliceScenarios
{
    private static string Sha(byte[] b)=>Convert.ToHexString(SHA256.HashData(b));
    private static void Check(bool b){if(!b)throw new InvalidOperationException("Slice ownership invariant.");}
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var p)?p.GetInt32():d;
    private static object Header(TerrainRuleHeader h)=>new{h.Version,h.EnabledBits,NameSha=Sha(h.CopyNameBytes()),LegacySha=Sha(h.CopyLegacyBytes())};
    private static object Rule(TerrainGeneratorFractalFilterRule r)=>new{r.GeneratorSha256,r.GroupSha256,r.RuleIndex,r.LayerIndex,r.ChildOrdinal,r.RuleOffset,r.RuleBytes,r.RuleSha256,r.FamilyId,r.FamilyGeneratorOffset,FamilyBytes=r.Family.EncodedBytes,FamilyRawSha=Sha(r.Family.Raw.CopyEncodedBytes()),r.IsEnabled,r.HasOperationalInputs,Header=Header(r.Header),Words=r.Input.CopyDataWordBits()};
    private static object Rule(TerrainGeneratorHeightFractalRule r)=>new{r.GeneratorSha256,r.GroupSha256,r.RuleIndex,r.LayerIndex,r.ChildOrdinal,r.RuleOffset,r.RuleBytes,r.RuleSha256,r.FamilyId,r.FamilyGeneratorOffset,FamilyBytes=r.Family.EncodedBytes,FamilyRawSha=Sha(r.Family.Raw.CopyEncodedBytes()),r.IsEnabled,r.HasOperationalInputs,Header=Header(r.Header),Words=r.Input.CopyDataWordBits()};
    private static object Snapshot(TerrainGeneratorHeightLayerSlice r)=>new{
        r.GeneratorSha256,r.GeneratorBytes,r.GroupSha256,r.GroupOffset,r.GroupBytes,r.LayerIndex,r.ParentIndex,r.Depth,r.LayerOffset,r.LayerBytes,r.LayerSha256,
        r.IsLocallyEnabled,r.BoundaryInverted,r.FilterInverted,r.AdtaWord2Bits,r.HasSupportedInputs,Policy=(int)r.Policy,
        Adta=r.CopyAdtaWordBits(),DescriptionSha=Sha(r.CopyDescriptionBytes()),Header=Header(r.Header),r.Children,
        Circle=new{r.Circle.Version,Header=Header(r.Circle.Header!),Words=r.Circle.CopyDataWordBits(),Sha256=Sha(r.Circle.CopyEncodedBytes())},
        Filter=Rule(r.Filter),Height=Rule(r.Height),r.DecodedCopyBytes,r.ReservedDecodedCopyBytes,r.SnapshotBytes,r.ReservedSnapshotBytes,r.Work,r.ReservedWork};
    private static TerrainGeneratorHeightLayerSlice Select(TerrainGeneratorInput g,TerrainGeneratorFractalFamilies b,JsonElement c)
        =>TerrainGeneratorHeightLayerSlice.Select(g,b,I(c,"Index",0),(TerrainGeneratorHeightLayerSlicePolicy)I(c,"Policy",1),
            new(I(c,"MaximumLayerBytes",1048576),I(c,"MaximumChildBytes",65536),I(c,"MaximumNameBytes",65536),
                I(c,"MaximumDecodedCopyBytes",1048576),I(c,"MaximumSnapshotBytes",8388608),I(c,"MaximumWork",8388608)),
            new CancellationToken(c.TryGetProperty("Cancel",out var p)&&p.GetBoolean()));
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            var name=c.GetProperty("Name").GetString()!;
            try{
                var g=TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!));
                var bg=c.TryGetProperty("BindingHex",out var other)?TerrainGeneratorInput.Decode(Convert.FromHexString(other.GetString()!)):g;
                var b=TerrainGeneratorFractalFamilies.Bind(bg);var slice=Select(g,b,c);
                Check(ReferenceEquals(slice.Filter.Family,b.Families.Find(slice.Filter.FamilyId))&&ReferenceEquals(slice.Height.Family,b.Families.Find(slice.Height.FamilyId)));
                cases.Add(new{Name=name,Reject="None",Snapshot=Snapshot(slice)});
            }catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){cases.Add(new{Name=name,Reject="Input"});}
        }
        var first=doc.RootElement[0];var bytes=Convert.FromHexString(first.GetProperty("GeneratorHex").GetString()!);
        var generator=TerrainGeneratorInput.Decode(bytes);var binding=TerrainGeneratorFractalFamilies.Bind(generator);var a=Select(generator,binding,first);
        string before=JsonSerializer.Serialize(Snapshot(a));int checks=0;
        Array.Fill(bytes,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var encoded=a.CopyEncodedBytes();Array.Fill(encoded,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var adta=a.CopyAdtaWordBits();Array.Fill(adta,0u);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var description=a.CopyDescriptionBytes();Array.Fill(description,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var nameBytes=a.Header.CopyNameBytes();Array.Fill(nameBytes,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var circle=a.Circle.CopyEncodedBytes();Array.Fill(circle,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var circleWords=a.Circle.CopyDataWordBits();Array.Fill(circleWords,0u);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var filter=a.Filter.CopyEncodedBytes();Array.Fill(filter,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var height=a.Height.CopyEncodedBytes();Array.Fill(height,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var family=a.Filter.Family.Raw.CopyEncodedBytes();Array.Fill(family,(byte)0);Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var list=(IList<TerrainGeneratorHeightLayerChild>)a.Children;bool readOnly=false;
        try{list[0]=list[0] with{ChildOrdinal=99};}catch(NotSupportedException){readOnly=true;}Check(readOnly);checks++;
        try{TerrainGeneratorHeightLayerSlice.Select(generator,binding,-1,TerrainGeneratorHeightLayerSlicePolicy.EnabledSupportedSlice);throw new InvalidOperationException("Expected rejection.");}
        catch(ArgumentOutOfRangeException){Check(before==JsonSerializer.Serialize(Snapshot(a)));checks++;}
        var same=TerrainGeneratorInput.Decode(generator.CopyGeneratorForm());Check(JsonSerializer.Serialize(Snapshot(Select(same,binding,first)))==before);checks++;
        var large=doc.RootElement.EnumerateArray().First(c=>c.GetProperty("Name").GetString()=="precopy-large-name");
        var lg=TerrainGeneratorInput.Decode(Convert.FromHexString(large.GetProperty("GeneratorHex").GetString()!));var lb=TerrainGeneratorFractalFamilies.Bind(lg);
        long start=GC.GetAllocatedBytesForCurrentThread();bool rejected=false;try{Select(lg,lb,large);}catch(InvalidDataException){rejected=true;}
        Check(rejected&&GC.GetAllocatedBytesForCurrentThread()-start<16384&&before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        var late=doc.RootElement.EnumerateArray().First(c=>c.GetProperty("Name").GetString()=="malformed-height");
        var lateGenerator=TerrainGeneratorInput.Decode(Convert.FromHexString(late.GetProperty("GeneratorHex").GetString()!));
        var lateBinding=TerrainGeneratorFractalFamilies.Bind(lateGenerator);bool lateRejected=false;
        try{Select(lateGenerator,lateBinding,late);}catch(InvalidDataException){lateRejected=true;}
        Check(lateRejected&&before==JsonSerializer.Serialize(Snapshot(a)));checks++;
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
