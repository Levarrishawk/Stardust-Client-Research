using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public static class TerrainFirstCurveFractalSparseBatchScenarios
{
    private static void Need(bool okay,string why){if(!okay)throw new InvalidDataException(why);}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    private static long L(JsonElement c,string key,long fallback)=>c.TryGetProperty(key,out var v)?v.GetInt64():fallback;
    private static bool B(JsonElement c,string key)=>c.TryGetProperty(key,out var v)&&v.GetBoolean();
    private static TerrainConditionalFractalSampler.Request[] Requests(JsonElement c)=>c.GetProperty("Requests").EnumerateArray()
        .Select(r=>new TerrainConditionalFractalSampler.Request(r.GetProperty("Column").GetInt32(),r.GetProperty("Row").GetInt32(),r.GetProperty("XBits").GetUInt32(),r.GetProperty("ZBits").GetUInt32())).ToArray();
    private static string Fraction(TerrainSuppliedFractalSample s)
    {BigInteger v=new(s.Magnitude);if(s.IsNegative)v=-v;return s.Exponent>=0?$"{v<<s.Exponent}/1":$"{v}/{BigInteger.One<<-s.Exponent}";}
    private static TerrainFirstCurveFractalSparseBatch Build(JsonElement c,byte[] raw,TerrainConditionalFractalSampler.Request[] requests)
    {
        using var cancel=new CancellationTokenSource();if(B(c,"Cancel"))cancel.Cancel();
        return TerrainFirstCurveFractalSparseBatch.Build(raw,I(c,"Width",3),I(c,"Rows",3),requests,
            B(c,"NullControls")?null!:new(I(c,"Table",53),I(c,"Construction",53),I(c,"Query",53)),(TerrainFirstCurveNumericPolicy)I(c,"Policy",0),
            new(I(c,"MaximumCells",1024),I(c,"MaximumRequests",256),I(c,"MaximumInputBytes",84),L(c,"MaximumCopyBytes",1048576),
                L(c,"MaximumRouteBytes",4194304),L(c,"MaximumWork",16777216),I(c,"MaximumQueryBytes",2097152),I(c,"MaximumQueryWork",16777216)),cancel.Token);
    }
    private static int FullEquivalent(TerrainFirstCurveFractalSparseBatch b)
    {
        if(b.Cells.Count==0)return 0;
        var fullRequests=Enumerable.Range(0,b.ShapeCells).Select(i=>new TerrainConditionalFractalSampler.Request(i%b.Width,i/b.Width,0,0)).ToArray();
        foreach(var c in b.Cells)fullRequests[c.Index]=c.Request;
        var full=TerrainFirstCurveFractalBatch.Build(b.CopyOriginalBytes(),b.Width,b.Rows,fullRequests,b.Controls,b.Policy,
            new(MaximumSamples:1024));
        foreach(var c in b.Cells)
        {
            var f=full.Cells[c.Index];Need(c.Request==f.Request&&Fraction(c.KernelResult.Sample)==Fraction(f.KernelResult.Sample)
                &&c.KernelResult.Sample.IsNegative==f.KernelResult.Sample.IsNegative&&c.Approximation.BaseF32Bits==f.Approximation.BaseF32Bits
                &&c.Approximation.ExponentBinary64Bits==f.Approximation.ExponentBinary64Bits
                &&c.Approximation.OutputBinary64Bits==f.Approximation.OutputBinary64Bits,"Exact frozen full-batch equivalence at supplied coordinates.");
        }
        return b.Cells.Count;
    }
    public static object Run(string path)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(path));var output=new List<object>();int ownership=0,equivalent=0,empty=0;
        foreach(var c in document.RootElement.GetProperty("Cases").EnumerateArray())
        {
            byte[] raw=Convert.FromHexString(c.GetProperty("Hex").GetString()!);var requests=Requests(c);
            try
            {
                var b=Build(c,raw,requests);
                Need(!b.OriginalNumericParity&&b.Policy==TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation,"Mandatory modern provenance.");
                for(int i=0;i<b.Cells.Count;i++)Need(b.Cells[i].RequestIndex==i&&b.Cells[i].Request==requests[i]
                    &&b.Cells[i].Index==requests[i].Row*b.Width+requests[i].Column,"Preserved subset order/index/coordinates.");
                equivalent+=FullEquivalent(b);
                if(b.Cells.Count==0)
                {
                    Need(b.SamplerFactories==0&&b.KernelBatches==0&&b.SamplerDisposals==0&&b.ComputedMisses==0&&b.LogCalls==0
                        &&b.PowerCalls==0&&b.ReservedQueryBytes==0&&b.ReservedQueryWork==0,"Empty subset performs no kernel/curve work.");empty++;
                }
                var src=b.CopyOriginalBytes();var derived=b.CopyDerivedKernelBytes();string saved=b.OriginalSha256;
                Array.Fill(raw,(byte)0);Array.Clear(requests);src[0]^=1;derived[0]^=1;
                Need(b.CopyOriginalBytes()[0]==(byte)'F'&&b.CopyDerivedKernelBytes()[0]==(byte)'F'&&b.OriginalSha256==saved,"Owned raw/request/export snapshots.");ownership++;
                bool readOnly=false;try{((IList<TerrainFirstCurveFractalSparseBatch.Cell>)b.Cells).Clear();}catch(NotSupportedException){readOnly=true;}
                Need(readOnly,"Immutable cells.");ownership++;
                var cells=b.Cells.Select(cell=>
                {
                    var a=cell.Approximation;var supplied=a.ToSuppliedSample();
                    Need(cell.KernelResult.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&a.BaseF32Bits==cell.KernelResult.Sample.ProjectionBits,"Actual retained miss and mandatory F32 base.");
                    return new {cell.RequestIndex,cell.Index,cell.Request.Column,cell.Request.Row,cell.Request.XBits,cell.Request.ZBits,
                        Kernel=Fraction(cell.KernelResult.Sample),KernelNegative=cell.KernelResult.Sample.IsNegative,Base=a.BaseF32Bits,
                        ExponentBits=a.ExponentBinary64Bits,OutputBits=a.OutputBinary64Bits,a.Magnitude,a.Exponent,Converted=Fraction(supplied),
                        Projection=supplied.ProjectionBits,Provenance=supplied.Provenance.ToString(),Policy=a.Policy.ToString(),a.OriginalNumericParity};
                }).ToArray();
                output.Add(new {Name=c.GetProperty("Name").GetString(),Reject="None",b.Width,b.Rows,b.ShapeCells,b.SuppliedRequests,
                    b.OriginalSha256,b.DerivedSha256,OriginalHex=Convert.ToHexString(b.CopyOriginalBytes()),DerivedHex=Convert.ToHexString(b.CopyDerivedKernelBytes()),Cells=cells,
                    Counts=new[]{b.SamplerFactories,b.KernelBatches,b.SamplerDisposals,b.ComputedMisses,b.KernelOctaves,b.CurveEvaluations,b.LogCalls,b.PowerCalls,b.EndpointEvaluations},
                    b.LogicalCopyBytes,b.ReservedRouteBytes,b.ReservedWork,b.ReservedQueryBytes,b.ReservedQueryWork,b.AccountedRouteBytes,b.AccountedWork});
            }
            catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException or OperationCanceledException)
            {output.Add(new {Name=c.GetProperty("Name").GetString(),Reject="Input"});}
        }
        var first=document.RootElement.GetProperty("Cases")[0];var original=Convert.FromHexString(first.GetProperty("Hex").GetString()!);var qs=Requests(first);
        var prior=Build(first,original,qs);var bits=prior.Cells.Select(c=>c.Approximation.OutputBinary64Bits).ToArray();
        bool fullRejected=false;try{_=TerrainFirstCurveFractalBatch.Build(original,2,2,qs.AsSpan(0,2),new(24,24,24),TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);}catch(InvalidDataException){fullRejected=true;}
        Need(fullRejected,"Frozen full batch still rejects a partial rectangle.");ownership++;
        for(int k=0;k<8;k++){var next=Build(first,original,qs);Need(next.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits)
            &&!ReferenceEquals(next.Cells[0].KernelResult.Sample,prior.Cells[0].KernelResult.Sample),"Fresh repeat ownership.");ownership++;}
        Parallel.For(0,8,k=>{var next=Build(first,original,qs);Need(next.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits),"Independent parallel factory.");});ownership+=8;
        var bad=(TerrainConditionalFractalSampler.Request[])qs.Clone();bad[^1]=bad[^1] with {XBits=0x7f7fffff};bool rejected=false;
        try{_=Build(first,original,bad);}catch(InvalidDataException){rejected=true;}
        Need(rejected&&prior.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits),"Late kernel failure preserves prior receipt.");ownership++;
        foreach(var candidate in document.RootElement.GetProperty("Cases").EnumerateArray().Where(c=>B(c,"LateCurve")))
        {
            rejected=false;try{_=Build(candidate,Convert.FromHexString(candidate.GetProperty("Hex").GetString()!),Requests(candidate));}catch(InvalidDataException){rejected=true;}
            Need(rejected&&prior.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits),"Late curve rejection preserves prior receipt.");ownership++;
        }
        return new {Cases=output,OwnershipChecks=ownership,FullEquivalentOutputs=equivalent,EmptyReceipts=empty};
    }
}
