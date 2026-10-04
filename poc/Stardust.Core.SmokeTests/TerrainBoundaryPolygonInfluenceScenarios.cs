using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public static class TerrainBoundaryPolygonInfluenceScenarios
{
    public static (List<object> Cases,int OwnershipChecks,int ArgumentNegatives,object Original) Run(string fixtures,string root,string config)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var cases=new List<object>();
        TerrainBoundaryPolygonInput? valid=null,disabled=null,empty=null;
        foreach(var f in document.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            var input=TerrainBoundaryPolygonInput.Decode(Convert.FromHexString(f.GetProperty("RuleHex").GetString()!),
                new(MaximumInputBytes:8388608,MaximumPoints:65536,MaximumDecodedCopyBytes:8388608));
            if(input.Header?.IsEnabled==true&&input.Points.Count==4&&input.Metadata?.FeatheringAmountBits==0)valid??=input;
            if(input.Header?.IsEnabled==false)disabled??=input;
            if(input.Header?.IsEnabled==true&&input.Points.Count==0&&input.Metadata?.FeatheringAmountBits==0)empty??=input;
            uint x=f.GetProperty("Coordinates")[0].GetUInt32(),y=f.GetProperty("Coordinates")[1].GetUInt32();
            var cp=(TerrainHeightArithmeticPrecision)f.GetProperty("CachePrecision").GetInt32();
            var p=(TerrainHeightArithmeticPrecision)f.GetProperty("Precision").GetInt32();
            var limits=JsonSerializer.Deserialize<TerrainPolygonInfluenceLimits>(f.GetProperty("Limits"))!;
            try
            {
                TerrainPolygonQuery? query;uint? influence;long charge;
                if(f.GetProperty("Action").GetString()=="query")
                { query=TerrainBoundaryPolygonInfluence.Query(input,x,y,cp,p,limits);influence=TerrainBoundaryPolygonInfluence.Shape(query,f.GetProperty("Selector").GetUInt32());charge=query.Cache.PointVisitCharge+query.PointVisitCharge; }
                else
                { var result=TerrainBoundaryPolygonInfluence.Evaluate(input,x,y,cp,p,limits);query=result.Query;influence=result.InfluenceBits;charge=result.PointVisitCharge; }
                var bounds=query?.Cache.Bounds;
                cases.Add(new {Name=name,Reject="None",Skipped=query is null,Influence=influence,Query=(uint?)query?.QueryProjectionBits,
                    Branch=query is null?(int?)null:(int)query.Branch,
                    Bounds=bounds is not TerrainPolygonBounds b?null:new[]{b.FirstMinimumBits,b.SecondMinimumBits,b.FirstMaximumBits,b.SecondMaximumBits},
                    Amount=(uint?)query?.Cache.RuntimeFeatherAmountBits,Crossings=query?.Crossings,Square=query?.SquaredAmountBits,
                    Minimum=query?.MinimumDistanceSquaredBits,Projections=query?.SegmentProjectionsConsidered,Charge=charge});
            }
            catch(ArgumentOutOfRangeException) { cases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { cases.Add(new{Name=name,Reject="Input"}); }
        }
        int checks=0,negatives=0;
        void Check(bool value) { checks++;if(!value)throw new InvalidOperationException("Polygon ownership/budget/identity check failed."); }
        void Bad<T>(Action action) where T:Exception
        { try { action();throw new InvalidOperationException("Expected polygon rejection."); }catch(T) { checks++; } }
        var mode=TerrainHeightArithmeticPrecision.Binary64;var exact=new TerrainPolygonInfluenceLimits(4,2,28);
        var cache=TerrainBoundaryPolygonInfluence.Cache(valid!,mode,new(4,0,4));var saved=cache.CopyPoints();
        Check(cache.PointCount==4&&cache.PointVisitCharge==4);
        var copy=cache.CopyPoints();Array.Clear(copy);Check(cache.Points.SequenceEqual(saved));
        Check(cache.Points is not TerrainBoundaryPolygonPoint[]);
        Bad<NotSupportedException>(()=>((IList<TerrainBoundaryPolygonPoint>)cache.Points)[0]=default);
        var changed=cache.Metadata with {FeatheringAmountBits=0x3f800000};Check(changed.FeatheringAmountBits!=cache.Metadata.FeatheringAmountBits);
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Cache(valid!,mode,new(3,0,4)));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Cache(valid!,mode,new(4,0,3)));
        var retained=TerrainBoundaryPolygonInfluence.Query(cache,0,0,mode,new(4,1,12));uint old=retained.QueryProjectionBits;
        Check(retained.PointVisitCharge==12&&retained.Cache==cache);
        Check(TerrainBoundaryPolygonInfluence.Query(valid!,0,0,mode,mode,new(4,1,16)).QueryProjectionBits==old);
        Check(typeof(TerrainPolygonCache).GetProperties().All(p=>p.SetMethod is null)&&typeof(TerrainPolygonQuery).GetProperties().All(p=>p.SetMethod is null));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Query(cache,0,0,mode,new(3,1,12)));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Query(cache,0,0,mode,new(4,1,11)));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Query(valid!,0,0,mode,mode,new(4,1,15)));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Query(cache,0,0,mode,new(4,0,12)));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryPolygonInfluence.Cache(valid!,(TerrainHeightArithmeticPrecision)23));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryPolygonInfluence.Query(cache,0,0,(TerrainHeightArithmeticPrecision)65));
        uint[] first=[0,0x3f000000],second=[0,0x3f000000];var batch=TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,first,second,mode,mode,exact);
        Check(batch.Results.Count==2&&batch.PointVisitCharge==28&&batch.Cache is not null&&batch.Results.All(r=>r.Query!.Cache==batch.Cache));
        var values=batch.CopyInfluenceBits();Array.Clear(first);Array.Clear(second);Check(batch.CopyInfluenceBits().SequenceEqual(values));
        var inspection=batch.CopyInfluenceBits();inspection[0]=0xdeadbeef;Check(batch.CopyInfluenceBits().SequenceEqual(values));
        Check(batch.Results is not TerrainPolygonInfluenceResult[]);
        Bad<NotSupportedException>(()=>((IList<TerrainPolygonInfluenceResult>)batch.Results)[0]=batch.Results[1]);
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,new uint[]{0,0},new uint[]{0,0},mode,mode,exact with {MaximumPointVisits=27}));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,new uint[]{0,0},new uint[]{0,0},mode,mode,exact with {MaximumPoints=3}));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,new uint[]{0,0},new uint[]{0,0},mode,mode,exact with {MaximumSamples=1}));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,new uint[]{0},Array.Empty<uint>(),mode,mode));
        foreach(var bad in new[]{new TerrainPolygonInfluenceLimits(-1),new(MaximumPoints:65537),new(MaximumSamples:-1),new(MaximumSamples:1048577),new(MaximumPointVisits:-1),new(MaximumPointVisits:67108865)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,bad));
        uint[] alias=[0,0x3f000000];var aliased=TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,alias,alias,mode,mode,exact);
        Check(alias.SequenceEqual(new uint[]{0,0x3f000000})&&aliased.Results.Count==2&&aliased.PointVisitCharge==28);
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,new uint[]{0,0x7fc12345},new uint[]{0,0},mode,mode,exact));
        Check(retained.QueryProjectionBits==old&&batch.CopyInfluenceBits().SequenceEqual(values));
        var rawPoints=valid!.CopyPoints();Array.Clear(rawPoints);Array.Clear(valid.CopyMetadataWordBits());
        Check(cache.Points.SequenceEqual(saved)&&TerrainBoundaryPolygonInfluence.Shape(retained,0)==old);
        Check(TerrainBoundaryPolygonInfluence.Shape(retained,0xffffffff)==0);
        var zero=TerrainBoundaryPolygonInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,new(4,0,0));
        Check(zero.Results.Count==0&&zero.Cache is null&&zero.PointVisitCharge==0);
        var skipped=TerrainBoundaryPolygonInfluence.EvaluateBatch(disabled!,new uint[]{0x7fc12345},new uint[]{0x7f800000},mode,mode,new(4,1,0));
        Check(skipped.Cache is null&&skipped.PointVisitCharge==0&&skipped.Results[0].SkippedDisabled&&skipped.Results[0].FirstCoordinateBits==0x7fc12345);
        var emptyCache=TerrainBoundaryPolygonInfluence.Cache(empty!,mode,new(0,0,0));var emptyQuery=TerrainBoundaryPolygonInfluence.Query(emptyCache,0,0,mode,new(0,1,0));
        Check(emptyCache.PointCount==0&&emptyQuery.Branch==TerrainPolygonQueryBranch.ParityOutside&&emptyQuery.QueryProjectionBits==0);
        byte[] opaqueBytes=valid.CopyEncodedBytes();opaqueBytes[23]=(byte)'9';var opaque=TerrainBoundaryPolygonInput.Decode(opaqueBytes);
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Cache(opaque,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Query(opaque,0,0,mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.Evaluate(opaque,0,0,mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(opaque,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode));
        void Null(Action action)
        { try { action();throw new InvalidOperationException("Expected null rejection."); }catch(ArgumentNullException) { negatives++; } }
        Null(()=>TerrainBoundaryPolygonInfluence.Cache(null!,mode));Null(()=>TerrainBoundaryPolygonInfluence.Query((TerrainBoundaryPolygonInput)null!,0,0,mode,mode));
        Null(()=>TerrainBoundaryPolygonInfluence.Query((TerrainPolygonCache)null!,0,0,mode));Null(()=>TerrainBoundaryPolygonInfluence.Shape(null!,0));
        Null(()=>TerrainBoundaryPolygonInfluence.Evaluate(null!,0,0,mode,mode));Null(()=>TerrainBoundaryPolygonInfluence.EvaluateBatch(null!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode));
        static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));string configHash=Hash(File.ReadAllBytes(config));
        Check(configHash=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524");
        byte[] terrain;int? priority;
        using(var cfg=File.OpenText(config))using(var overlay=ConfiguredTreOverlay.Open(root,cfg))
        { var lookup=overlay.Lookup("terrain/tatooine.trn");Check(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null);terrain=lookup.Payload!;priority=lookup.Priority; }
        string sourceHash=Hash(terrain);Check(priority==42&&terrain.Length==1188446&&sourceHash=="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();
        string generatorHash=Hash(generator);Check(generator.Length==139608&&generatorHash=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32");
        var rules=TerrainGeneratorInput.Decode(generator).Rules.Where(r=>r.Section.FormType=="BPOL").ToArray();Check(rules.Length==57);
        var rule=rules[0];var polygon=TerrainBoundaryPolygonInput.Decode(rule.Section);
        object original=new {ConfigSha256=configHash,SourceSha256=sourceHash,GeneratorSha256=generatorHash,Priority=priority,Count=rules.Length,
            First=new {rule.LayerIndex,rule.ChildOrdinal,rule.Section.Offset,polygon.Version,HeaderVersion=polygon.Header!.Version,polygon.Header.EnabledBits,
                PointBits=polygon.CopyPoints().Select(p=>new[]{p.FirstBits,p.SecondBits}).ToArray(),MetadataWordBits=polygon.CopyMetadataWordBits(),EncodedBytes=polygon.CopyEncodedBytes().Length,Sha256=Hash(polygon.CopyEncodedBytes())}};
        return (cases,checks,negatives,original);
    }
}
