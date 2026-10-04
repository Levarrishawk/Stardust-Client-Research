using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public static class TerrainBoundaryCircleInfluenceScenarios
{
    public static (List<object> Cases, int BatchChecks, int ArgumentNegatives) Run(string fixtures)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));
        var cases=new List<object>(); int batchChecks=0, negatives=0;
        TerrainBoundaryCircleInput? valid=null, disabled=null;
        foreach(var f in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            var input=TerrainBoundaryCircleInput.Decode(Convert.FromHexString(f.GetProperty("RuleHex").GetString()!));
            if(input.Header!.IsEnabled && input.Fields!.HasFiniteGeometry && input.Fields.RadiusBits==0x3f800000)valid??=input;
            if(!input.Header.IsEnabled)disabled??=input;
            uint first=f.GetProperty("Coordinates")[0].GetUInt32(),second=f.GetProperty("Coordinates")[1].GetUInt32();
            var cachePrecision=(TerrainHeightArithmeticPrecision)f.GetProperty("CachePrecision").GetInt32();
            var precision=(TerrainHeightArithmeticPrecision)f.GetProperty("Precision").GetInt32();
            try
            {
                TerrainCircleQuery? q;uint? result;
                if(f.GetProperty("Action").GetString()=="query") { q=TerrainBoundaryCircleInfluence.Query(input,first,second,cachePrecision,precision);result=TerrainBoundaryCircleInfluence.Shape(q,f.GetProperty("Selector").GetUInt32()); }
                else { var receipt=TerrainBoundaryCircleInfluence.Evaluate(input,first,second,cachePrecision,precision);q=receipt.Query;result=receipt.InfluenceBits; }
                cases.Add(new { Name=name, Reject="None", Skipped=q is null, Influence=result,
                    Query=(uint?)q?.QueryProjectionBits,RadiusSquared=(uint?)q?.RadiusSquaredBits,DistanceSquared=(uint?)q?.DistanceSquaredBits,
                    Amount=(uint?)q?.RuntimeFeatherAmountBits,Branch=q is null?(int?)null:(int)q.Branch });
            }
            catch(ArgumentOutOfRangeException) { cases.Add(new {Name=name,Reject="Options"}); }
            catch(InvalidDataException) { cases.Add(new {Name=name,Reject="Input"}); }
        }
        void Check(bool condition) { batchChecks++;if(!condition)throw new InvalidOperationException("Circle ownership/budget check failed."); }
        void Bad<T>(Action action) where T:Exception { try { action();throw new InvalidOperationException("Expected rejection."); } catch(T) { batchChecks++; } }
        var mode=TerrainHeightArithmeticPrecision.Binary64;
        uint[] x=[0,0x3f000000],y=[0,0];
        var batch=TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,x,y,mode,mode,2);
        Check(batch.Results.Count==2);
        var saved=batch.CopyInfluenceBits(); x[0]=0x7fc12345;y[0]=0x7fc12345;
        Check(batch.Results[0].InfluenceBits==saved[0]);
        var copy=batch.CopyInfluenceBits();copy[0]=0xdeadbeef;Check(batch.Results[0].InfluenceBits==saved[0]);
        var retained=batch.Results[0].Query!;uint old=retained.QueryProjectionBits;
        Bad<InvalidDataException>(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,new uint[]{0,0x7fc12345},new uint[]{0,0},mode,mode));
        Check(batch.Results[0].Query!.QueryProjectionBits==old && batch.Results[0].InfluenceBits==saved[0]);
        Bad<InvalidDataException>(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,new uint[]{0},Array.Empty<uint>(),mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,new uint[]{0},new uint[]{0},mode,mode,0));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,-1));
        Bad<ArgumentOutOfRangeException>(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,1048577));
        Check(TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode,0).Results.Count==0);
        uint[] alias=[0,0x3f000000];var aliased=TerrainBoundaryCircleInfluence.EvaluateBatch(valid!,alias,alias,mode,mode,2);
        Check(alias.SequenceEqual(new uint[]{0,0x3f000000}) && aliased.Results.Count==2);
        var skipped=TerrainBoundaryCircleInfluence.EvaluateBatch(disabled!,new uint[]{0x7fc12345},new uint[]{0x7f800000},mode,mode);
        Check(skipped.Results[0].SkippedDisabled && skipped.Results[0].InfluenceBits is null && skipped.Results[0].FirstCoordinateBits==0x7fc12345);
        Check(TerrainBoundaryCircleInfluence.Shape(retained,0xffffffff)==0);
        Check(TerrainBoundaryCircleInfluence.Shape(retained,0)==retained.QueryProjectionBits);
        Check(batch.Results is not TerrainCircleInfluenceResult[]);
        Bad<NotSupportedException>(()=>((IList<TerrainCircleInfluenceResult>)batch.Results)[0]=batch.Results[1]);
        byte[] opaqueBytes=valid!.CopyEncodedBytes();opaqueBytes[23]=(byte)'9';
        var opaque=TerrainBoundaryCircleInput.Decode(opaqueBytes);
        Bad<InvalidDataException>(()=>TerrainBoundaryCircleInfluence.Query(opaque,0,0,mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryCircleInfluence.Evaluate(opaque,0,0,mode,mode));
        Bad<InvalidDataException>(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(opaque,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode));
        void Null(Action action) { try { action();throw new InvalidOperationException("Expected null rejection."); } catch(ArgumentNullException) { negatives++; } }
        Null(()=>TerrainBoundaryCircleInfluence.Query(null!,0,0,mode,mode));Null(()=>TerrainBoundaryCircleInfluence.Shape(null!,0));
        Null(()=>TerrainBoundaryCircleInfluence.Evaluate(null!,0,0,mode,mode));Null(()=>TerrainBoundaryCircleInfluence.EvaluateBatch(null!,Array.Empty<uint>(),Array.Empty<uint>(),mode,mode));
        return (cases,batchChecks,negatives);
    }
}
