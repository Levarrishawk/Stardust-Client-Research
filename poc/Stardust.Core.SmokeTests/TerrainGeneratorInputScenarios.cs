using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TerrainGeneratorInputScenarios
{
    private const string Config="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";
    private const string Source="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427";
    private static string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    private static void Need(bool value,string reason) { if(!value)throw new Exception(reason); }
    private static object Section(TerrainGeneratorSection s)=>new {s.Tag,s.FormType,s.Version,s.Offset,s.EncodedBytes,Sha256=Hash(s.CopyEncodedBytes())};
    private static object? Header(TerrainRuleHeader? h)=>h is null?null:new {h.Version,h.EnabledBits,NameSha256=Hash(h.CopyNameBytes()),LegacySha256=Hash(h.CopyLegacyBytes())};
    private static object Metadata(TerrainGeneratorInput input)=>new {
        GeneratorBytes=input.CopyGeneratorForm().Length,GeneratorSha256=Hash(input.CopyGeneratorForm()),
        Groups=input.Groups.Select(g=>new {Role=g.Role.ToString(),Section=Section(g.Section),g.ClientVersionRecognized,g.RecordCount}).ToArray(),
        Layers=input.Layers.Select(l=>new {l.Index,l.ParentIndex,l.Depth,Section=Section(l.Section),Header=Header(l.Header),AdtaWordBits=l.CopyAdtaWordBits(),DescriptionSha256=Hash(l.CopyDescriptionBytes()),Children=l.Children.Select(Section).ToArray()}).ToArray(),
        Rules=input.Rules.Select(r=>new {r.LayerIndex,r.ChildOrdinal,Section=Section(r.Section),HeightConstant=r.HeightConstant is null?null:new {Header=Header(r.HeightConstant.Header),r.HeightConstant.OperationBits,r.HeightConstant.HeightBits}}).ToArray(),
        input.GroupRecordCount,input.DecodedStringBytes,input.HeightConstantCount};
    internal static void Run(string[] args)
    {
        Need(args.Length==3,"Explicit root/config/synthetic fixtures required.");Need(Hash(File.ReadAllBytes(args[1]))==Config,"Configuration pin.");
        using var fixtures=JsonDocument.Parse(File.ReadAllBytes(args[2]));var cases=new List<object>();
        foreach(var f in fixtures.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!,expected=f.GetProperty("Reject").GetString()!;byte[] raw=Convert.FromHexString(f.GetProperty("Hex").GetString()!);
            var limits=JsonSerializer.Deserialize<TerrainGeneratorInputLimits>(f.GetProperty("Limits"))!;string actual="None";object? metadata=null;
            try { metadata=Metadata(TerrainGeneratorInput.Decode(raw,limits)); }catch(InvalidDataException) { actual="Input"; }catch(ArgumentOutOfRangeException) { actual="Options"; }
            Need(actual==expected,"Fixture "+name+": expected "+expected+", got "+actual);cases.Add(new {Name=name,Reject=actual,Metadata=metadata});
        }
        byte[] bytes=Convert.FromHexString(fixtures.RootElement[0].GetProperty("Hex").GetString()!);var owned=TerrainGeneratorInput.Decode(bytes);string before=JsonSerializer.Serialize(Metadata(owned));int ownership=0;
        Need(owned.Layers.Count==2&&owned.Layers[1].ParentIndex==0&&owned.HeightConstantCount==2,"Literal containment and typed family counts.");
        var h=owned.Rules[0].HeightConstant!;Need(h.Operation==-2&&h.HeightBits==0x80000000&&h.Header.IsEnabled&&h.Header.EnabledBits==uint.MaxValue,"Signed operation/full height/flag bits preserved.");
        Array.Clear(bytes);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Owned source snapshot.");ownership++;
        foreach(var copy in new[]{owned.CopyGeneratorForm(),owned.Groups[0].Section.CopyEncodedBytes(),owned.Layers[0].Section.CopyEncodedBytes(),owned.Layers[0].CopyDescriptionBytes(),h.Header.CopyNameBytes(),h.Header.CopyLegacyBytes(),owned.Rules[1].Section.CopyEncodedBytes()})
        { Array.Clear(copy);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Inspection bytes isolated.");ownership++; }
        var flags=owned.Layers[0].CopyAdtaWordBits();Array.Clear(flags);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Flag copy isolated.");ownership++;
        Need(owned.Groups is not TerrainGeneratorGroup[]&&owned.Layers is not TerrainGeneratorLayer[]&&owned.Rules is not TerrainGeneratorRule[]&&owned.Layers[0].Children is not TerrainGeneratorSection[]&&typeof(TerrainGeneratorInput).GetProperties().All(p=>p.SetMethod is null),"Readonly collection and scalar surface.");ownership++;
        byte[] terrain;int? priority;
        using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg)) { var lookup=overlay.Lookup("terrain/tatooine.trn");Need(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null,"Original input present.");terrain=lookup.Payload!;priority=lookup.Priority; }
        Need(priority==42&&terrain.Length==1188446&&Hash(terrain)==Source,"Original identity pin.");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();Need(generator.Length==139608&&Hash(generator)=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32","Generator pin.");
        var input=TerrainGeneratorInput.Decode(generator);Array.Clear(generator);Array.Clear(terrain);Need(input.Layers.Count==257&&input.HeightConstantCount==87,"Same pinned original AHCN occurrence.");
        var original=new {Root=Path.GetFullPath(args[0]).Replace('\\','/'),Configuration=Path.GetFullPath(args[1]).Replace('\\','/'),ConfigSha256=Config,Path="terrain/tatooine.trn",Priority=priority,SourceBytes=1188446,SourceSha256=Source,GeneratorOffset=210,Metadata=Metadata(input)};
        Console.WriteLine("tgen_input="+JsonSerializer.Serialize(new {Cases=cases,OwnershipChecks=ownership,Original=original}));
        Console.WriteLine("PASS: bounded owned TGEN groups/layer containment, exact AHCN and inherited headers, opaque unsupported records, one pinned original oracle.");
    }
}
