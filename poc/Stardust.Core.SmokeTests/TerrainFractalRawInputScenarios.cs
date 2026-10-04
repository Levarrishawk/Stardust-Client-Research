using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainFractalRawRun(List<object> Raw,List<object> Wide,int OwnershipChecks,int BudgetChecks);
public static class TerrainFractalRawInputScenarios
{
    private static object Snapshot(TerrainFractalRawInput input)=>new
    { Words=input.CopyDataWordBits(),Seed=input.Fields.SignedSeed,Octaves=input.Fields.SignedOctaveCount,
        Curve1=input.Fields.FirstCurveEnabled,Curve2=input.Fields.SecondCurveEnabled,Finite=input.Fields.HasFiniteParameters,
        Sha=Convert.ToHexString(SHA256.HashData(input.CopyEncodedBytes())) };
    public static TerrainFractalRawRun Run(string fixture)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var raw=new List<object>();var wide=new List<object>();TerrainFractalRawInput? valid=null;
        foreach(var row in doc.RootElement.GetProperty("Raw").EnumerateArray())
        {
            string name=row.GetProperty("Name").GetString()!;
            try
            { var input=TerrainFractalRawInput.Decode(Convert.FromHexString(row.GetProperty("Hex").GetString()!),JsonSerializer.Deserialize<TerrainFractalRawInputLimits>(row.GetProperty("Limits")));valid??=input;raw.Add(new{Name=name,Reject="None",Snapshot=Snapshot(input)}); }
            catch(ArgumentOutOfRangeException) { raw.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { raw.Add(new{Name=name,Reject="Input"}); }
        }
        foreach(var row in doc.RootElement.GetProperty("Wide").EnumerateArray())
        {
            string name=row.GetProperty("Name").GetString()!;
            try
            {
                var sample=TerrainSuppliedFractalSample.FromWideMagnitude(row.GetProperty("Magnitude").GetUInt64(),row.GetProperty("Negative").GetBoolean(),row.GetProperty("Exponent").GetInt32());
                var rule=TerrainHeightFractalInput.Decode(Convert.FromHexString(row.GetProperty("Hex").GetString()!));
                var r=TerrainSuppliedHeightFractal.Evaluate(rule,row.GetProperty("Height").GetUInt32(),0x3f800000,sample,(TerrainHeightArithmeticPrecision)row.GetProperty("Precision").GetInt32());
                // Full object serialization must neither wrap signed inspection nor throw from a getter.
                using var snapshot=JsonDocument.Parse(JsonSerializer.Serialize(sample));
                var cache=sample.ToCacheReload();
                wide.Add(new{Name=name,Reject="None",sample.Magnitude,sample.IsNegative,sample.Significand,sample.NegativeZero,sample.ProjectionBits,
                    Provenance=(int)sample.Provenance,Output=r.Consumer.ResultHeightBits,Cache=cache.ProjectionBits,CacheProvenance=(int)cache.Provenance});
            }
            catch(ArgumentOutOfRangeException) { wide.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { wide.Add(new{Name=name,Reject="Input"}); }
        }
        int ownership=0,budgets=0;
        void Check(bool okay) { if(!okay)throw new InvalidOperationException("MFRC ownership mismatch.");ownership++; }
        void Bad(Action action) { try { action();throw new InvalidOperationException("Expected raw rejection."); }catch(InvalidDataException) { budgets++; } }
        byte[] source=valid!.CopyEncodedBytes();var separate=TerrainFractalRawInput.Decode(source);string before=JsonSerializer.Serialize(Snapshot(separate));
        Array.Clear(source);Array.Clear(separate.CopyDataWordBits());Array.Clear(separate.CopyEncodedBytes());
        Check(JsonSerializer.Serialize(Snapshot(separate))==before);Check(typeof(TerrainFractalRawInput).GetProperties().All(p=>p.SetMethod is null));
        var data=valid.CopyEncodedBytes();ReadOnlyMemory<byte>[] inputs={data,data};
        var batch=TerrainFractalRawInput.DecodeBatch(inputs,new(84,2,168,272));inputs[0]=ReadOnlyMemory<byte>.Empty;Array.Clear(data);
        Check(batch.InputBytes==168&&batch.OwnedCopyBytes==272&&JsonSerializer.Serialize(Snapshot(batch.Inputs[0]))==before);
        var good=new ReadOnlyMemory<byte>[]{valid.CopyEncodedBytes(),valid.CopyEncodedBytes()};
        foreach(var cap in new[]{new TerrainFractalRawInputLimits(84,1,168,272),new(84,2,167,272),new(84,2,168,271),new(83,2,168,272)})
            Bad(()=>TerrainFractalRawInput.DecodeBatch(good,cap));
        Bad(()=>TerrainFractalRawInput.DecodeBatch(new ReadOnlyMemory<byte>[]{valid.CopyEncodedBytes(),new byte[83]}));
        Check(JsonSerializer.Serialize(Snapshot(batch.Inputs[0]))==before&&good[0].Length==84);
        var empty=TerrainFractalRawInput.DecodeBatch(Array.Empty<ReadOnlyMemory<byte>>(),new(0,0,0,0));Check(empty.Inputs.Count==0&&empty.OwnedCopyBytes==0);
        Check(TerrainSuppliedFractalSample.FromWide(long.MinValue,-63).Significand==long.MinValue);
        Check(TerrainSuppliedFractalSample.FromWideMagnitude(0x8000000000000000UL,true,-63).Significand==long.MinValue);
        Check(TerrainSuppliedFractalSample.FromWideMagnitude(0x8000000000000000UL,false,-63).Significand is null);
        Check(TerrainSuppliedFractalSample.FromWideMagnitude(ulong.MaxValue,true,-64).Significand is null);
        Check(TerrainSuppliedFractalSample.AccountedBytes==32);
        return new(raw,wide,ownership,budgets);
    }
}
