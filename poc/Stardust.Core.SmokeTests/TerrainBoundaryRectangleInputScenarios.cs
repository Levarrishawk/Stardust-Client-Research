using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TerrainBoundaryRectangleInputScenarios
{
    private const string Config="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";
    private const string Source="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427";
    private const string Generator="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32";
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static void Need(bool value,string reason) { if(!value)throw new Exception(reason); }
    private static object Metadata(TerrainBoundaryRectangleInput input)=>new {
        input.Version,input.ClientVersionRecognized,input.HasDecodedFields,input.HasSerializedExtension,input.DecodedStringBytes,
        Header=input.Header is null?null:new {input.Header.Version,input.Header.EnabledBits,input.Header.IsEnabled,NameSha256=Hash(input.Header.CopyNameBytes()),LegacySha256=Hash(input.Header.CopyLegacyBytes())},
        Fields=input.Fields is null?null:new {input.Fields.CornerFirstABits,input.Fields.CornerSecondABits,input.Fields.CornerFirstBBits,input.Fields.CornerSecondBBits,input.Fields.LegacyPrefixBits,input.Fields.FeatheringTypeBits,input.Fields.FeatheringAmountBits,input.Fields.ExtensionFlagFirstBits,input.Fields.ExtensionFlagSecondBits,input.Fields.ExtensionScalarFirstBits,input.Fields.ExtensionScalarSecondBits,input.Fields.HasFiniteGeometry,input.Fields.HasFiniteSerializedFeatheringAmount,input.Fields.IsExtensionFlagFirstSet,input.Fields.IsExtensionFlagSecondSet},
        ExtensionNameSha256=Hash(input.CopyExtensionNameBytes()),
        DataWordBits=input.CopyDataWordBits(),EncodedBytes=input.CopyEncodedBytes().Length,Sha256=Hash(input.CopyEncodedBytes())};
    internal static void Run(string[] args)
    {
        Need(args.Length==3,"Explicit readonly root/config/fixture paths required.");Need(Hash(File.ReadAllBytes(args[1]))==Config,"Config identity.");
        using var fixtures=JsonDocument.Parse(File.ReadAllBytes(args[2]));var cases=new List<object>();
        foreach(var f in fixtures.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!,expected=f.GetProperty("Reject").GetString()!;
            byte[] raw=Convert.FromHexString(f.GetProperty("Hex").GetString()!);var limits=JsonSerializer.Deserialize<TerrainBoundaryRectangleInputLimits>(f.GetProperty("Limits"))!;
            string actual="None";object? metadata=null;
            try { metadata=Metadata(TerrainBoundaryRectangleInput.Decode(raw,limits)); }
            catch(InvalidDataException) { actual="Input"; }catch(ArgumentOutOfRangeException) { actual="Options"; }
            Need(actual==expected,$"Fixture {name}: expected {expected}, got {actual}");cases.Add(new {Name=name,Reject=actual,Metadata=metadata});
        }
        byte[] bytes=Convert.FromHexString(fixtures.RootElement[0].GetProperty("Hex").GetString()!);
        var owned=TerrainBoundaryRectangleInput.Decode(bytes);string before=JsonSerializer.Serialize(Metadata(owned));int ownership=0;
        Array.Clear(bytes);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Source snapshot isolation.");ownership++;
        foreach(var copy in new[]{owned.CopyEncodedBytes(),owned.Header!.CopyNameBytes(),owned.Header.CopyLegacyBytes(),owned.CopyExtensionNameBytes()})
        { Array.Clear(copy);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Byte accessor isolation.");ownership++; }
        var words=owned.CopyDataWordBits();Array.Clear(words);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Word accessor isolation.");ownership++;
        var changed=owned.Fields! with {CornerFirstABits=0};Need(changed.CornerFirstABits!=owned.Fields!.CornerFirstABits&&before==JsonSerializer.Serialize(Metadata(owned)),"Record copy isolation.");ownership++;
        Need(typeof(TerrainBoundaryRectangleInput).GetProperties().All(p=>p.SetMethod is null)&&typeof(TerrainRuleHeader).GetProperties().All(p=>p.SetMethod is null),"Immutable published model/header surface.");ownership++;
        try { TerrainBoundaryRectangleInput.Decode(new byte[3]);throw new Exception("Expected later rejection."); }catch(InvalidDataException) { }
        Need(before==JsonSerializer.Serialize(Metadata(owned)),"Retain earlier model after later failure.");ownership++;
        byte[] wrapped=Convert.FromHexString(fixtures.RootElement[0].GetProperty("SectionGeneratorHex").GetString()!);
        var sectionOwner=TerrainGeneratorInput.Decode(wrapped);var section=sectionOwner.Rules.Single().Section;
        var fromSection=TerrainBoundaryRectangleInput.Decode(section);Array.Clear(wrapped);
        Need(before==JsonSerializer.Serialize(Metadata(fromSection)),"Section decoder agrees and owns bytes.");ownership++;
        try { TerrainBoundaryRectangleInput.Decode(section,new(MaximumInputBytes:section.EncodedBytes-1));throw new Exception("Expected section byte cap."); }catch(InvalidDataException) { }ownership++;
        try { TerrainBoundaryRectangleInput.Decode(section,new(MaximumInputBytes:23));throw new Exception("Expected section invalid option."); }catch(ArgumentOutOfRangeException) { }ownership++;
        try { TerrainBoundaryRectangleInput.Decode(sectionOwner.Groups[0].Section);throw new Exception("Expected wrong section family."); }catch(InvalidDataException) { }ownership++;
        Array.Clear(section.CopyEncodedBytes());Need(before==JsonSerializer.Serialize(Metadata(fromSection)),"Source section inspection copy isolation.");ownership++;
        try { TerrainBoundaryRectangleInput.Decode((TerrainGeneratorSection)null!);throw new Exception("Expected null section."); }catch(ArgumentNullException) { }ownership++;
        var opaqueFixture=fixtures.RootElement.EnumerateArray().Single(f=>f.GetProperty("Name").GetString()=="opaque-0004");
        byte[] opaqueSource=Convert.FromHexString(opaqueFixture.GetProperty("Hex").GetString()!);var opaque=TerrainBoundaryRectangleInput.Decode(opaqueSource);
        string opaqueBefore=JsonSerializer.Serialize(Metadata(opaque));Array.Clear(opaqueSource);
        Need(!opaque.ClientVersionRecognized&&!opaque.HasDecodedFields&&opaqueBefore==JsonSerializer.Serialize(Metadata(opaque)),"Owned unsupported0004 bytes.");ownership++;
        Array.Clear(opaque.CopyEncodedBytes());Need(opaqueBefore==JsonSerializer.Serialize(Metadata(opaque)),"Opaque inspection isolation.");ownership++;
        byte[] terrain;int? priority;
        using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg))
        { var lookup=overlay.Lookup("terrain/tatooine.trn");Need(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null,"Pinned terrain present.");terrain=lookup.Payload!;priority=lookup.Priority; }
        Need(priority==42&&terrain.Length==1188446&&Hash(terrain)==Source,"Pinned original identity.");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();
        Need(generator.Length==139608&&Hash(generator)==Generator,"TGEN identity.");var tgen=TerrainGeneratorInput.Decode(generator);
        var rectangles=tgen.Rules.Where(r=>r.Section.FormType=="BREC").Select(r=>new {r.LayerIndex,r.ChildOrdinal,r.Section.Offset,Metadata=Metadata(TerrainBoundaryRectangleInput.Decode(r.Section))}).ToArray();
        Need(rectangles.Length==29,"Same pinned original29 BREC.");
        var original=new {ConfigSha256=Config,SourceSha256=Source,GeneratorSha256=Generator,Priority=priority,SourceBytes=terrain.Length,GeneratorBytes=generator.Length,Rectangles=rectangles};
        Console.WriteLine("brec_input="+JsonSerializer.Serialize(new {Cases=cases,OwnershipChecks=ownership,Original=original}));
        Console.WriteLine("PASS: owned BREC serialized fields, opaque unsupported versions, bounded failures and pinned29-record oracle.");
    }
}
