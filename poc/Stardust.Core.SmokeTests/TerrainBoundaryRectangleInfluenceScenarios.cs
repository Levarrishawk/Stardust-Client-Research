using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public static class TerrainBoundaryRectangleInfluenceScenarios
{
    public static (List<object> Cases, int BatchChecks, int ArgumentNegatives, object Original) Run(string fixtures,string root,string config)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));
        var cases=new List<object>(); int batchChecks=0, negatives=0;
        TerrainBoundaryRectangleInput? valid=null, disabled=null;
        foreach(var f in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            var input=TerrainBoundaryRectangleInput.Decode(Convert.FromHexString(f.GetProperty("RuleHex").GetString()!));
            if(input.Header?.IsEnabled==true && input.Fields is not null && input.Fields.HasFiniteGeometry && input.Fields.CornerFirstBBits==0x40000000)valid??=input;
            if(input.Header?.IsEnabled==false)disabled??=input;
            uint first=f.GetProperty("Coordinates")[0].GetUInt32(),second=f.GetProperty("Coordinates")[1].GetUInt32();
            var cachePrecision=(TerrainHeightArithmeticPrecision)f.GetProperty("CachePrecision").GetInt32();
            var precision=(TerrainHeightArithmeticPrecision)f.GetProperty("Precision").GetInt32();
            try
            {
                TerrainRectangleQuery? q;uint? result;
                if(f.GetProperty("Action").GetString()=="query") { q=TerrainBoundaryRectangleInfluence.Query(input,first,second,cachePrecision,precision);result=TerrainBoundaryRectangleInfluence.Shape(q,f.GetProperty("Selector").GetUInt32()); }
                else { var receipt=TerrainBoundaryRectangleInfluence.Evaluate(input,first,second,cachePrecision,precision);q=receipt.Query;result=receipt.InfluenceBits; }
                static uint[] Bounds(TerrainRectangleBounds b)=>new[]{b.FirstABits,b.SecondABits,b.FirstBBits,b.SecondBBits};
                cases.Add(new { Name=name, Reject="None", Skipped=q is null, Influence=result,
                    Query=(uint?)q?.QueryProjectionBits,Amount=(uint?)q?.Cache.RuntimeFeatherAmountBits,Branch=q is null?(int?)null:(int)q.Branch,
                    CacheOuter=q is null?null:Bounds(q.Cache.Outer),CacheInner=q is null?null:Bounds(q.Cache.Inner),
                    CacheExtents=q is null?null:new[]{q.Cache.FirstExtentBits,q.Cache.SecondExtentBits,q.Cache.MinimumExtentBits},
                    QueryEdges=q?.EdgeDistances is not TerrainRectangleEdgeDistances edges?null:new[]{edges.FirstABits,edges.FirstBBits,edges.SecondABits,edges.SecondBBits},
                    QueryExtents=q?.FirstExtentBits is null?null:new[]{q.FirstExtentBits.Value,q.SecondExtentBits!.Value,q.MinimumExtentBits!.Value} });
            }
            catch(ArgumentOutOfRangeException) { cases.Add(new {Name=name,Reject="Options"}); }
            catch(InvalidDataException) { cases.Add(new {Name=name,Reject="Input"}); }
        }
        void Check(bool condition) { batchChecks++;if(!condition)throw new InvalidOperationException("Rectangle ownership/budget check failed."); }
        void Bad<T>(Action action) where T:Exception { try { action();throw new InvalidOperationException("Expected rejection."); } catch(T) { batchChecks++; } }
        var mode=TerrainHeightArithmeticPrecision.Binary64;
        uint[] x=[0,0x3f000000],y=[0,0];
        var batch=TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,x,y,mode,mode,2);
        Check(batch.Results.Count==2);
        var saved=batch.CopyInfluenceBits(); x[0]=0x7fc12345;y[0]=0x7fc12345;
        Check(batch.Results[0].InfluenceBits==saved[0]);
        var copy=batch.CopyInfluenceBits();copy[0]=0xdeadbeef;Check(batch.Results[0].InfluenceBits==saved[0]);
        var retained=batch.Results[0].Query!;uint old=retained.QueryProjectionBits;
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,new uint[]{0,0x7fc12345},new uint[]{0,0},mode,mode));
        Check(batch.Results[0].Query!.QueryProjectionBits==old && batch.Results[0].InfluenceBits==saved[0]);
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,new uint[]{0},Array.Empty<uint>(),mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,new uint[]{0},new uint[]{0},mode,mode,0));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,-1));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,1048577));
        Check(TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,0).Results.Count==0);
        uint[] alias=[0,0x3f000000];var aliased=TerrainBoundaryRectangleInfluence.EvaluateBatch(valid!,alias,alias,mode,mode,2);
        Check(alias.SequenceEqual(new uint[]{0,0x3f000000}) && aliased.Results.Count==2);
        var skipped=TerrainBoundaryRectangleInfluence.EvaluateBatch(disabled!,new uint[]{0x7fc12345},new uint[]{0x7f800000},mode,mode);
        Check(skipped.Results[0].SkippedDisabled && skipped.Results[0].InfluenceBits is null && skipped.Results[0].FirstCoordinateBits==0x7fc12345);
        Check(TerrainBoundaryRectangleInfluence.Shape(retained,0xffffffff)==0);
        Check(TerrainBoundaryRectangleInfluence.Shape(retained,0)==retained.QueryProjectionBits);
        Check(batch.Results is not TerrainRectangleInfluenceResult[]);
        Bad<NotSupportedException>(()=>((IList<TerrainRectangleInfluenceResult>)batch.Results)[0]=batch.Results[1]);
        byte[] opaqueBytes=valid!.CopyEncodedBytes();opaqueBytes[23]=(byte)'9';
        var opaque=TerrainBoundaryRectangleInput.Decode(opaqueBytes);
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.Query(opaque,0,0,mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.Evaluate(opaque,0,0,mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(opaque,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode));
        void Null(Action action) { try { action();throw new InvalidOperationException("Expected null rejection."); } catch(ArgumentNullException) { negatives++; } }
        Null(()=>TerrainBoundaryRectangleInfluence.Query(null!,0,0,mode,mode));Null(()=>TerrainBoundaryRectangleInfluence.Shape(null!,0));
        Null(()=>TerrainBoundaryRectangleInfluence.Evaluate(null!,0,0,mode,mode));Null(()=>TerrainBoundaryRectangleInfluence.EvaluateBatch(null!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode));
        Null(()=>TerrainBoundaryRectangleInfluence.Cache(null!,mode));
        Null(()=>TerrainBoundaryRectangleInfluence.Query((TerrainRectangleCache)null!,0,0,mode));
        var cache=TerrainBoundaryRectangleInfluence.Cache(valid!,mode);
        var cachedQuery=TerrainBoundaryRectangleInfluence.Query(cache,0,0,mode);
        Check(cachedQuery.QueryProjectionBits==retained.QueryProjectionBits&&cachedQuery.Cache==cache);
        Check(typeof(TerrainRectangleQuery).GetProperties().All(p=>p.SetMethod is null)&&typeof(TerrainRectangleCache).GetProperties().All(p=>p.SetMethod is null));
        var replacement=cache.Fields with {CornerFirstABits=0x7fc12345};
        Check(replacement.CornerFirstABits!=cache.Fields.CornerFirstABits&&cachedQuery.QueryProjectionBits==old);
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryRectangleInfluence.Cache(valid!,(TerrainHeightArithmeticPrecision)23));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryRectangleInfluence.Query(cache,0,0,(TerrainHeightArithmeticPrecision)65));
        Bad<InvalidDataException>(()=>TerrainBoundaryRectangleInfluence.Query(cache,0x7fc12345,0,mode));
        Check(TerrainBoundaryRectangleInfluence.Shape(cachedQuery,0)==cachedQuery.QueryProjectionBits);
        var data=valid!.CopyDataWordBits();Array.Clear(data);
        Check(TerrainBoundaryRectangleInfluence.Query(cache,0,0,mode).QueryProjectionBits==old);
        static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        string configHash=Hash(File.ReadAllBytes(config));
        Check(configHash=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524");
        byte[] terrain;int? priority;
        using(var cfg=File.OpenText(config))using(var overlay=ConfiguredTreOverlay.Open(root,cfg))
        { var lookup=overlay.Lookup("terrain/tatooine.trn");Check(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null);terrain=lookup.Payload!;priority=lookup.Priority; }
        string sourceHash=Hash(terrain);Check(priority==42&&terrain.Length==1188446&&sourceHash=="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();
        string generatorHash=Hash(generator);Check(generator.Length==139608&&generatorHash=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32");
        var rules=TerrainGeneratorInput.Decode(generator).Rules.Where(r=>r.Section.FormType=="BREC").ToArray();Check(rules.Length==29);
        var rule=rules[0];var rectangle=TerrainBoundaryRectangleInput.Decode(rule.Section);
        object original=new {ConfigSha256=configHash,SourceSha256=sourceHash,GeneratorSha256=generatorHash,Priority=priority,Count=rules.Length,
            First=new {rule.LayerIndex,rule.ChildOrdinal,rule.Section.Offset,rectangle.Version,HeaderVersion=rectangle.Header!.Version,rectangle.Header.EnabledBits,
                DataWordBits=rectangle.CopyDataWordBits(),EncodedBytes=rectangle.CopyEncodedBytes().Length,Sha256=Hash(rectangle.CopyEncodedBytes())}};
        return (cases,batchChecks,negatives,original);
    }
}
