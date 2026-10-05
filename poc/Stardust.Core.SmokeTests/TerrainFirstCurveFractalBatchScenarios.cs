using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public static class TerrainFirstCurveFractalBatchScenarios
{
    private static void Need(bool okay,string reason){if(!okay)throw new InvalidDataException(reason);}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    private static long L(JsonElement c,string key,long fallback)=>c.TryGetProperty(key,out var v)?v.GetInt64():fallback;
    private static string Fraction(TerrainSuppliedFractalSample s)
    {
        BigInteger v=new(s.Magnitude);if(s.IsNegative)v=-v;
        return s.Exponent>=0?$"{v<<s.Exponent}/1":$"{v}/{BigInteger.One<<-s.Exponent}";
    }
    private static TerrainFirstCurveFractalBatch Build(JsonElement c,byte[] raw,TerrainConditionalFractalSampler.Request[] requests)
    {
        using var cancel=new CancellationTokenSource();if(c.TryGetProperty("Cancel",out var x)&&x.GetBoolean())cancel.Cancel();
        return TerrainFirstCurveFractalBatch.Build(raw,I(c,"Width",2),I(c,"Rows",2),requests,
            new(I(c,"Table",53),I(c,"Construction",53),I(c,"Query",53)),(TerrainFirstCurveNumericPolicy)I(c,"Policy",0),
            new(I(c,"MaximumSamples",256),I(c,"MaximumInputBytes",84),L(c,"MaximumCopyBytes",1048576),
                L(c,"MaximumRouteBytes",4194304),L(c,"MaximumWork",16777216),I(c,"MaximumQueryBytes",2097152),I(c,"MaximumQueryWork",16777216)),cancel.Token);
    }
    public static object Run(string path)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(path));var output=new List<object>();int ownership=0;
        foreach(var c in document.RootElement.GetProperty("Cases").EnumerateArray())
        {
            var raw=Convert.FromHexString(c.GetProperty("Hex").GetString()!);
            var requests=c.GetProperty("Requests").EnumerateArray().Select(r=>new TerrainConditionalFractalSampler.Request(
                r.GetProperty("Column").GetInt32(),r.GetProperty("Row").GetInt32(),r.GetProperty("XBits").GetUInt32(),r.GetProperty("ZBits").GetUInt32())).ToArray();
            try
            {
                var b=Build(c,raw,requests);
                Need(!b.OriginalNumericParity&&b.Policy==TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation,"Explicit modern provenance.");
                var source=b.CopyOriginalBytes();var derived=b.CopyDerivedKernelBytes();var saved=b.OriginalSha256;
                Array.Fill(raw,(byte)0);Array.Clear(requests);source[0]^=1;derived[0]^=1;
                Need(b.CopyOriginalBytes()[0]==(byte)'F'&&b.CopyDerivedKernelBytes()[0]==(byte)'F'&&b.OriginalSha256==saved,"Owned immutable snapshots.");ownership++;
                bool readonlyCells=false;try{((IList<TerrainFirstCurveFractalBatch.Cell>)b.Cells)[0]=b.Cells[0];}catch(NotSupportedException){readonlyCells=true;}
                Need(readonlyCells,"Immutable cell collection.");ownership++;
                var cells=b.Cells.Select(cell=>
                {
                    var a=cell.Approximation;var supplied=a.ToSuppliedSample();
                    Need(cell.KernelResult.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss,"Actual fresh retained miss.");
                    Need(a.BaseF32Bits==cell.KernelResult.Sample.ProjectionBits&&!a.OriginalNumericParity,"Mandatory actual kernel F32 snapshot.");
                    Need(supplied.Provenance==TerrainSuppliedFractalProvenance.SuppliedWideMiss,"Conversion is caller supplied.");
                    return new {cell.Index,cell.Request.Column,cell.Request.Row,cell.Request.XBits,cell.Request.ZBits,
                        Kernel=Fraction(cell.KernelResult.Sample),KernelNegative=cell.KernelResult.Sample.IsNegative,
                        Base=a.BaseF32Bits,ExponentBits=a.ExponentBinary64Bits,OutputBits=a.OutputBinary64Bits,
                        a.Magnitude,a.Exponent,Converted=Fraction(supplied),Projection=supplied.ProjectionBits,
                        Provenance=supplied.Provenance.ToString(),Policy=a.Policy.ToString(),a.OriginalNumericParity};
                }).ToArray();
                output.Add(new {Name=c.GetProperty("Name").GetString(),Reject="None",b.OriginalSha256,b.DerivedSha256,
                    OriginalHex=Convert.ToHexString(b.CopyOriginalBytes()),DerivedHex=Convert.ToHexString(b.CopyDerivedKernelBytes()),Cells=cells,
                    Counts=new[]{b.SamplerFactories,b.KernelBatches,b.SamplerDisposals,b.ComputedMisses,b.KernelOctaves,b.CurveEvaluations,b.LogCalls,b.PowerCalls,b.EndpointEvaluations},
                    b.LogicalCopyBytes,b.ReservedRouteBytes,b.ReservedWork,b.ReservedQueryBytes,b.ReservedQueryWork,b.AccountedRouteBytes,b.AccountedWork});
            }
            catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException or OperationCanceledException)
            {output.Add(new {Name=c.GetProperty("Name").GetString(),Reject="Input"});}
        }
        // Fresh stateless repeats, cross-thread independent factories and a rejected late candidate preserve a prior receipt.
        var first=document.RootElement.GetProperty("Cases")[0];byte[] r0=Convert.FromHexString(first.GetProperty("Hex").GetString()!);
        var q0=first.GetProperty("Requests").EnumerateArray().Select(r=>new TerrainConditionalFractalSampler.Request(r.GetProperty("Column").GetInt32(),r.GetProperty("Row").GetInt32(),r.GetProperty("XBits").GetUInt32(),r.GetProperty("ZBits").GetUInt32())).ToArray();
        var prior=Build(first,r0,q0);var bits=prior.Cells.Select(c=>c.Approximation.OutputBinary64Bits).ToArray();
        bool frozenReject=false;try{using var forbidden=TerrainConditionalFractalSampler.CreateForSupportedModes(TerrainFractalRawInput.Decode(r0),2,2,53,53,1048576,1048576);}catch(InvalidDataException){frozenReject=true;}
        Need(frozenReject,"Frozen sampler still rejects original enabled first curve.");ownership++;
        for(int k=0;k<8;k++){var next=Build(first,r0,q0);Need(next.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits)&&!ReferenceEquals(next.Cells[0].KernelResult.Sample,prior.Cells[0].KernelResult.Sample),"Fresh repeat ownership.");ownership++;}
        Parallel.For(0,8,k=>{var next=Build(first,r0,q0);Need(next.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits),"Independent cross-thread factory.");});ownership+=8;
        var bad=(TerrainConditionalFractalSampler.Request[])q0.Clone();bad[^1]=bad[^1] with {XBits=0x7f7fffff};
        bool rejected=false;try{_=Build(first,r0,bad);}catch(InvalidDataException){rejected=true;}
        Need(rejected&&prior.Cells.Select(c=>c.Approximation.OutputBinary64Bits).SequenceEqual(bits),"Late kernel failure leaves prior output unchanged.");ownership++;
        return new {Cases=output,OwnershipChecks=ownership};
    }
}
