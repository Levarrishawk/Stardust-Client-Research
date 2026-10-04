using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

internal static class TerrainHeightConstantOperationScenarios
{
    private static string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    private static void Need(bool value,string reason) { if(!value)throw new Exception(reason); }
    private static byte[] Chunk(string tag,byte[] bytes) { byte[] result=new byte[bytes.Length+8];Encoding.ASCII.GetBytes(tag).CopyTo(result,0);BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4),(uint)bytes.Length);bytes.CopyTo(result,8);return result; }
    private static byte[] Form(string tag,params byte[][] children)=>Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(b=>b)).ToArray());
    private static TerrainHeightConstantInput Rule(string hex)
    {
        var header=Form("IHDR",Form("0001",Chunk("DATA",new byte[]{1,0,0,0,0})));
        var layer=Form("LAYR",Form("0001",header,Chunk("ADTA",new byte[8]),Convert.FromHexString(hex)));
        string[] tags={"SGRP","FGRP","RGRP","EGRP","MGRP"};string[] versions={"0006","0008","0003","0002","0000"};
        var parts=tags.Select((t,i)=>Form(t,Form(versions[i]))).Append(Form("LYRS",layer)).ToArray();
        return TerrainGeneratorInput.Decode(Form("TGEN",Form("0000",parts))).Rules.Single().HeightConstant!;
    }
    internal static void Run(string[] args)
    {
        Need(args.Length==3,"Explicit root/config/fixtures required.");const string config="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524",source="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427";
        Need(Hash(File.ReadAllBytes(args[1]))==config,"Configuration pin.");using var fixtures=JsonDocument.Parse(File.ReadAllBytes(args[2]));var cases=new List<object>();TerrainHeightConstantInput? retained=null;
        foreach(var f in fixtures.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!,expected=f.GetProperty("Reject").GetString()!;var rule=Rule(f.GetProperty("RuleHex").GetString()!);string actual="None";TerrainHeightConstantResult? result=null;
            uint input=f.GetProperty("InputHeightBits").GetUInt32(),influence=f.GetProperty("InfluenceBits").GetUInt32();var precision=(TerrainHeightArithmeticPrecision)f.GetProperty("Precision").GetInt32();
            try { result=TerrainHeightConstantOperation.Evaluate(rule,input,influence,precision); }catch(InvalidDataException) { actual="Input"; }catch(ArgumentOutOfRangeException) { actual="Options"; }
            Need(actual==expected,"Fixture "+name+": expected "+expected+", got "+actual);
            if(result is {} r)Need(r.InputHeightBits==input&&r.InfluenceBits==influence&&r.ConstantHeightBits==rule.HeightBits&&r.OperationBits==rule.OperationBits&&r.EnabledBits==rule.Header.EnabledBits,"Raw result provenance.");
            cases.Add(new {Name=name,Reject=actual,Result=result});if(rule.OperationBits==1&&rule.HeightBits==0x3f800000&&rule.Header.IsEnabled)retained=rule;
        }
        Need(retained is not null,"Known additive rule.");var scalarRule=retained!;var p=TerrainHeightArithmeticPrecision.Binary24;uint[] inputBits={0x3f800000,0x80000000},influences={0x3f000000,0};uint[] initial=(uint[])inputBits.Clone();
        var batch=TerrainHeightConstantOperation.EvaluateBatch(scalarRule,inputBits,influences,p,2);uint[] expectedBits=batch.CopyHeightBits();int checks=0;
        Need(inputBits.SequenceEqual(initial)&&expectedBits.SequenceEqual(new uint[]{0x3fc00000,0x80000000}),"Literal batch and no source mutation.");checks++;
        Array.Clear(inputBits);Array.Clear(influences);Need(batch.CopyHeightBits().SequenceEqual(expectedBits),"Input snapshots.");checks++;
        var copy=batch.CopyHeightBits();Array.Clear(copy);Need(batch.CopyHeightBits().SequenceEqual(expectedBits),"Output clone.");checks++;
        uint[] alias={0x3f000000,0x3f800000};var joined=TerrainHeightConstantOperation.EvaluateBatch(scalarRule,alias,alias,p);Need(joined.CopyHeightBits().SequenceEqual(new uint[]{0x3f800000,0x40000000})&&alias[0]==0x3f000000,"Aliased input spans.");checks++;
        Need(TerrainHeightConstantOperation.EvaluateBatch(scalarRule,Array.Empty<uint>(),Array.Empty<uint>(),p,0).Results.Count==0,"Empty exact cap.");checks++;
        void Reject<T>(Action action) where T:Exception { try {action();}catch(T){checks++;return;}throw new Exception("Unexpected batch acceptance."); }
        Reject<InvalidDataException>(()=>TerrainHeightConstantOperation.EvaluateBatch(scalarRule,alias,alias,p,1));
        Reject<InvalidDataException>(()=>TerrainHeightConstantOperation.EvaluateBatch(scalarRule,alias,new uint[1],p));
        Reject<ArgumentOutOfRangeException>(()=>TerrainHeightConstantOperation.EvaluateBatch(scalarRule,alias,alias,p,-1));
        Reject<ArgumentOutOfRangeException>(()=>TerrainHeightConstantOperation.EvaluateBatch(scalarRule,alias,alias,p,1048577));
        Reject<InvalidDataException>(()=>TerrainHeightConstantOperation.EvaluateBatch(scalarRule,new uint[]{0x3f800000,0x7fc12345},new uint[]{0x3f000000,0x3f000000},p));
        Need(batch.CopyHeightBits().SequenceEqual(expectedBits)&&batch.Results is not TerrainHeightConstantResult[],"Earlier successful batch retained after late failure; readonly outputs.");
        byte[] terrain;int? priority;using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg)) { var lookup=overlay.Lookup("terrain/tatooine.trn");Need(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null,"Original present.");terrain=lookup.Payload!;priority=lookup.Priority; }
        Need(priority==42&&terrain.Length==1188446&&Hash(terrain)==source,"Original full pin.");var generator=ProceduralTerrainInput.Decode(terrain).CopyGeneratorForm();string genHash=Hash(generator);var tgen=TerrainGeneratorInput.Decode(generator);
        var heights=tgen.Rules.Where(r=>r.HeightConstant is not null).Select(r=>r.HeightConstant!).ToArray();Need(heights.Length==87,"Same original family count.");
        var operations=heights.GroupBy(r=>r.OperationBits).OrderBy(g=>g.Key).Select(g=>new {OperationBits=g.Key,Count=g.Count(),EnabledCount=g.Count(r=>r.Header.IsEnabled),FiniteHeightCount=g.Count(r=>r.HasFiniteHeight)}).ToArray();Array.Clear(terrain);Array.Clear(generator);
        var original=new {Root=Path.GetFullPath(args[0]).Replace('\\','/'),Configuration=Path.GetFullPath(args[1]).Replace('\\','/'),ConfigSha256=config,Path="terrain/tatooine.trn",Priority=priority,SourceBytes=1188446,SourceSha256=source,GeneratorSha256=genHash,HeightConstants=87,Operations=operations};
        Console.WriteLine("ahcn_operation="+JsonSerializer.Serialize(new {Cases=cases,BatchChecks=checks,Original=original}));Console.WriteLine("PASS: explicit precision AHCN operation order, raw bits, skip/error gates, finite policy, bounded owned batches and original distribution.");
    }
}
