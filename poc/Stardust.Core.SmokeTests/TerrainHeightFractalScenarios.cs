using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainHeightFractalRun(List<object> RawCases,List<object> Queries,int OwnershipChecks,int ArgumentNegatives,object Original);
public static class TerrainHeightFractalScenarios
{
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static object Snapshot(TerrainHeightFractalInput input)=>new
    {
        input.Version,input.ClientVersionRecognized,input.HasDecodedFields,input.DecodedCopyBytes,
        Words=input.CopyDataWordBits(),Family=input.Fields?.FamilyKeyBits,Operation=input.Fields?.OperationBits,Scale=input.Fields?.ScaleBits,
        HeaderVersion=input.Header?.Version,Enabled=input.Header?.EnabledBits,
        NameSha=input.Header is null?null:Hash(input.Header.CopyNameBytes()),LegacySha=input.Header is null?null:Hash(input.Header.CopyLegacyBytes()),
        EncodedBytes=input.CopyEncodedBytes().Length,Sha256=Hash(input.CopyEncodedBytes())
    };
    public static TerrainHeightFractalRun Run(string fixtures,string root,string config)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var cases=new List<object>();TerrainHeightFractalInput? valid=null;
        foreach(var fixture in document.RootElement.GetProperty("RawCases").EnumerateArray())
        {
            string name=fixture.GetProperty("Name").GetString()!;
            try
            {
                var input=TerrainHeightFractalInput.Decode(Convert.FromHexString(fixture.GetProperty("Hex").GetString()!),JsonSerializer.Deserialize<TerrainHeightFractalInputLimits>(fixture.GetProperty("Limits")));
                if(input.HasDecodedFields&&input.Header is not null)valid??=input;
                cases.Add(new{Name=name,Reject="None",Snapshot=Snapshot(input)});
            }
            catch(ArgumentOutOfRangeException) { cases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { cases.Add(new{Name=name,Reject="Input"}); }
        }
        int checks=0,nulls=0;void Check(bool condition) { checks++;if(!condition)throw new InvalidOperationException("AHFR ownership/budget check failed."); }
        void Bad<T>(Action action) where T:Exception { try { action();throw new InvalidOperationException("Expected AHFR rejection."); }catch(T) { checks++; } }
        var prior=JsonSerializer.Serialize(Snapshot(valid!));byte[] bytes=valid!.CopyEncodedBytes();var separate=TerrainHeightFractalInput.Decode(bytes);
        Array.Clear(bytes);Check(JsonSerializer.Serialize(Snapshot(separate))==prior);
        Array.Clear(valid.CopyDataWordBits());Array.Clear(valid.Header!.CopyNameBytes());Array.Clear(valid.Header.CopyLegacyBytes());
        Check(JsonSerializer.Serialize(Snapshot(valid))==prior);
        var fields=valid.Fields! with {FamilyKeyBits=0x7fc12345};Check(fields.FamilyKeyBits!=valid.Fields!.FamilyKeyBits);
        Check(typeof(TerrainHeightFractalInput).GetProperties().All(p=>p.SetMethod is null));
        Bad<InvalidDataException>(()=>TerrainHeightFractalInput.Decode(valid.CopyEncodedBytes().AsSpan(0,valid.CopyEncodedBytes().Length-1)));
        Check(JsonSerializer.Serialize(Snapshot(valid))==prior);
        var exact=new TerrainHeightFractalInputLimits(MaximumInputBytes:valid.CopyEncodedBytes().Length,MaximumStringBytes:valid.Header.CopyNameBytes().Length,MaximumDecodedCopyBytes:valid.DecodedCopyBytes);
        Check(TerrainHeightFractalInput.Decode(valid.CopyEncodedBytes(),exact).DecodedCopyBytes==valid.DecodedCopyBytes);
        Bad<InvalidDataException>(()=>TerrainHeightFractalInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumInputBytes=exact.MaximumInputBytes-1}));
        Bad<InvalidDataException>(()=>TerrainHeightFractalInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumStringBytes=exact.MaximumStringBytes-1}));
        Bad<InvalidDataException>(()=>TerrainHeightFractalInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumDecodedCopyBytes=exact.MaximumDecodedCopyBytes-1}));
        foreach(var l in new[]{new TerrainHeightFractalInputLimits(MaximumInputBytes:23),new(MaximumNodes:0),new(MaximumDepth:0),new(MaximumStringBytes:-1),new(MaximumDecodedCopyBytes:-1)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainHeightFractalInput.Decode(valid.CopyEncodedBytes(),l));
        try { TerrainHeightFractalInput.Decode((TerrainGeneratorSection)null!);throw new InvalidOperationException("Expected null section rejection."); }catch(ArgumentNullException) { nulls++; }
        var queries=new List<object>();
        foreach(var q in document.RootElement.GetProperty("Queries").EnumerateArray())
        {
            string name=q.GetProperty("Name").GetString()!;
            try
            {
                var rule=TerrainHeightFractalInput.Decode(Convert.FromHexString(q.GetProperty("RuleHex").GetString()!));
                var result=TerrainSuppliedHeightFractal.Evaluate(rule,q.GetProperty("Height").GetUInt32(),q.GetProperty("Influence").GetUInt32(),q.GetProperty("Fractal").GetUInt32(),(TerrainHeightArithmeticPrecision)q.GetProperty("Precision").GetInt32());
                queries.Add(new{Name=name,Reject="None",Output=result.ResultHeightBits,Outcome=(int)result.Outcome});
            }
            catch(ArgumentOutOfRangeException) { queries.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { queries.Add(new{Name=name,Reject="Input"}); }
        }
        uint[] h={0x3f800000,0x40000000},w={0x3f000000,0x3f000000},f={0x40400000,0x40800000};
        var exactBatch=new TerrainHeightFractalLimits(2,12,24);
        var batch=TerrainSuppliedHeightFractal.EvaluateBatch(valid,h,w,f,TerrainHeightArithmeticPrecision.Binary53,exactBatch);
        var priorBatch=JsonSerializer.Serialize(batch);Check(batch.Work==12&&batch.Results.Count==2);
        Array.Clear(h);Array.Clear(w);Array.Clear(f);Array.Clear(batch.CopyHeightBits());Check(JsonSerializer.Serialize(batch)==priorBatch);
        uint[] same={0x3f800000,0x40000000};var aliases=TerrainSuppliedHeightFractal.EvaluateBatch(valid,same,same,same,TerrainHeightArithmeticPrecision.Binary64);Array.Clear(same);Check(aliases.Results[0].InputHeightBits==0x3f800000);
        h=new uint[]{0,0};w=new uint[]{0x3f800000,0x3f800000};f=new uint[]{0,0};
        foreach(var l in new[]{exactBatch with {MaximumSamples=1},exactBatch with {MaximumWork=11},exactBatch with {MaximumSnapshotBytes=23}})
            Bad<InvalidDataException>(()=>TerrainSuppliedHeightFractal.EvaluateBatch(valid,h,w,f,TerrainHeightArithmeticPrecision.Binary24,l));
        foreach(var l in new[]{new TerrainHeightFractalLimits(-1),new(MaximumWork:-1),new(MaximumSnapshotBytes:-1),new(1048577),new(MaximumWork:6291457),new(MaximumSnapshotBytes:12582913)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedHeightFractal.EvaluateBatch(valid,h,w,f,TerrainHeightArithmeticPrecision.Binary24,l));
        Bad<InvalidDataException>(()=>TerrainSuppliedHeightFractal.EvaluateBatch(valid,h,w,new uint[1],TerrainHeightArithmeticPrecision.Binary24));
        f[1]=0x7fc12345;Bad<InvalidDataException>(()=>TerrainSuppliedHeightFractal.EvaluateBatch(valid,h,w,f,TerrainHeightArithmeticPrecision.Binary24));Check(JsonSerializer.Serialize(batch)==priorBatch);
        Check(TerrainSuppliedHeightFractal.EvaluateBatch(valid,Array.Empty<uint>(),Array.Empty<uint>(),Array.Empty<uint>(),TerrainHeightArithmeticPrecision.Binary24,new(0,0,0)).Results.Count==0);
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedHeightFractal.Evaluate(valid,0,0,0,(TerrainHeightArithmeticPrecision)25));
        Bad<ArgumentNullException>(()=>TerrainSuppliedHeightFractal.Evaluate(null!,0,0,0,TerrainHeightArithmeticPrecision.Binary24));
        var firstHeight=TerrainSuppliedHeightFractal.Evaluate(valid,0x3f800000,0x3f800000,0x40400000,TerrainHeightArithmeticPrecision.Binary53);
        var fresh=TerrainSuppliedHeightFractal.Evaluate(valid,firstHeight.ResultHeightBits,0x3f000000,0x40a00000,TerrainHeightArithmeticPrecision.Binary53);
        var stale=TerrainSuppliedHeightFractal.Evaluate(valid,0x3f800000,0x3f000000,0x40a00000,TerrainHeightArithmeticPrecision.Binary53);
        Check(firstHeight.ResultHeightBits==0x40400000&&fresh.ResultHeightBits==0x40800000&&stale.ResultHeightBits==0x40400000);
        var opaqueFixture=document.RootElement.GetProperty("RawCases").EnumerateArray().First(x=>x.GetProperty("Expected").TryGetProperty("Snapshot",out var sn)&&sn.GetProperty("Version").GetString()=="0000");
        var opaque=TerrainHeightFractalInput.Decode(Convert.FromHexString(opaqueFixture.GetProperty("Hex").GetString()!));
        Bad<InvalidDataException>(()=>TerrainSuppliedHeightFractal.Evaluate(opaque,0,0,0,TerrainHeightArithmeticPrecision.Binary24));
        Bad<InvalidDataException>(()=>TerrainSuppliedHeightFractal.EvaluateBatch(opaque,Array.Empty<uint>(),Array.Empty<uint>(),Array.Empty<uint>(),TerrainHeightArithmeticPrecision.Binary24));
        string configHash=Hash(File.ReadAllBytes(config));Check(configHash=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524");
        byte[] terrain;int? priority;using(var cfg=File.OpenText(config))using(var overlay=ConfiguredTreOverlay.Open(root,cfg))
        { var found=overlay.Lookup("terrain/tatooine.trn");Check(found.Kind==TreLookupKind.Present&&found.Payload is not null);terrain=found.Payload!;priority=found.Priority; }
        string sourceHash=Hash(terrain);Check(priority==42&&terrain.Length==1188446&&sourceHash=="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();string generatorHash=Hash(generator);
        Check(generator.Length==139608&&generatorHash=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32");
        var rules=TerrainGeneratorInput.Decode(generator).Rules.Where(r=>r.Section.FormType=="AHFR").ToArray();object? first=null;
        if(rules.Length>0) { var rule=rules[0];first=new {rule.LayerIndex,rule.ChildOrdinal,rule.Section.Offset,Snapshot=Snapshot(TerrainHeightFractalInput.Decode(rule.Section))}; }
        object original=new {ConfigSha256=configHash,SourceSha256=sourceHash,GeneratorSha256=generatorHash,Priority=priority,Count=rules.Length,First=first};
        return new(cases,queries,checks,nulls,original);
    }
}
