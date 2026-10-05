using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainHeightLayerSparseFirstCurveEvaluationScenarios
{
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var p)?p.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var p)?p.GetInt64():d;
    private static bool Flag(JsonElement c,string k)=>c.TryGetProperty(k,out var p)&&p.GetBoolean();
    private static void Check(bool b){if(!b)throw new InvalidOperationException("First-curve consumer invariant.");}
    private static uint[] A(JsonElement c,string k)=>c.GetProperty(k).EnumerateArray().Select(x=>x.GetUInt32()).ToArray();
    private static TerrainGeneratorHeightLayerSlice Slice(JsonElement c){var g=TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!));return TerrainGeneratorHeightLayerSlice.Select(g,TerrainGeneratorFractalFamilies.Bind(g),I(c,"Index",0),TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);}
    private static TerrainSampleCoordinateGrid Grid(JsonElement c){var r=c.GetProperty("Request");var p=c.GetProperty("Profiles").EnumerateArray().Select(x=>(TerrainHeightArithmeticPrecision)x.GetInt32()).ToArray();return TerrainSampleCoordinateGrid.Build(new(r.GetProperty("ChunkWidthBits").GetUInt32(),I(r,"Tiles",2),I(r,"Margin",0),I(r,"First",0),I(r,"Second",0),I(r,"Scale",1)),p[0],p[1],p[2],p[3],new(1024,1048576));}
    private static TerrainHeightLayerFilteredInfluence Filter(TerrainGeneratorHeightLayerSlice s,TerrainSampleCoordinateGrid g,JsonElement c){var b=TerrainHeightLayerBoundaryAdmission.Evaluate(s,g,new((TerrainHeightArithmeticPrecision)I(c,"Cache",53),(TerrainHeightArithmeticPrecision)I(c,"CircleQuery",53),(TerrainHeightArithmeticPrecision)I(c,"Combine",53)),new(1024,1048576,1048576,268435456,25165824,67108864));return TerrainHeightLayerFilteredInfluence.Evaluate(b,s,g,A(c,"Incoming"),new(I(c,"Table",53),I(c,"Construction",53),I(c,"SamplerQuery",53),(TerrainHeightArithmeticPrecision)I(c,"FilterQuery",53),(TerrainHeightArithmeticPrecision)I(c,"Combine",53),(TerrainHeightArithmeticPrecision)I(c,"Product",53)));}
    private static TerrainFirstCurveFractalSparseBatch Curve(TerrainGeneratorHeightLayerSlice s,TerrainSampleCoordinateGrid g,TerrainHeightLayerFilteredInfluence f,JsonElement c){
        int axis=g.Layer.Count,width=Flag(c,"WrongShape")?axis*2:axis,rows=Flag(c,"WrongShape")?axis/2:axis;
        var indices=c.TryGetProperty("RequestIndices",out var input)?input.EnumerateArray().Select(x=>x.GetInt32()).ToArray():f.Cells.Where(x=>(x.ProductBits&0x80000000)==0&&(x.ProductBits&0x7fffffff)!=0).Select(x=>x.Index).ToArray();
        var requests=indices.Select(i=>{var p=g.Layer.GetPair(i%axis,i/axis);return new TerrainConditionalFractalSampler.Request(i%width,i/width,p.FirstBits,p.SecondBits);}).ToArray();
        if(Flag(c,"WrongCoordinates")&&requests.Length>0)requests[^1]=requests[^1] with{XBits=0x3e000000};
        var raw=c.TryGetProperty("CurveHex",out var h)?Convert.FromHexString(h.GetString()!):s.Height.Family.Raw.CopyEncodedBytes();
        return TerrainFirstCurveFractalSparseBatch.Build(raw,width,rows,requests,new(I(c,"CurveTable",53),I(c,"CurveConstruction",53),I(c,"CurveQuery",53)),TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation,new(MaximumRequests:1024));
    }
    private static TerrainHeightLayerSparseFirstCurveEvaluation Evaluate(TerrainHeightLayerFilteredInfluence f,TerrainGeneratorHeightLayerSlice s,TerrainSampleCoordinateGrid g,uint[] priors,JsonElement c){
        TerrainFirstCurveFractalControls? controls=Flag(c,"NullControls")?null:new(I(c,"CurveTable",53),I(c,"CurveConstruction",53),I(c,"CurveQuery",53));
        var result=TerrainHeightLayerSparseFirstCurveEvaluation.Evaluate(f,s,g,priors,controls!,(TerrainHeightArithmeticPrecision)I(c,"HeightPrecision",53),(TerrainFirstCurveNumericPolicy)I(c,"Policy",0),
            new(I(c,"MaximumAxisCount",32),I(c,"MaximumCells",1024),I(c,"MaximumRequests",1024),L(c,"MaximumAggregateCopyBytes",4194304),L(c,"MaximumOwnedArrayBytes",1048576),L(c,"MaximumAggregateRouteBytes",16777216),L(c,"MaximumAggregateWork",67108864),c.TryGetProperty("MaximumAbsoluteHeightBits",out var b)?b.GetUInt32():0x49742400),new CancellationToken(Flag(c,"Cancel")));
        Check(ReferenceEquals(result.CurveControls,controls)&&ReferenceEquals(result.BorrowedFilterCounts,f.Counts));return result;
    }
    private static string Fraction(TerrainSuppliedFractalSample s){if(s.Magnitude==0)return s.NegativeZero?"-0/1":"0/1";BigInteger n=s.Magnitude,d=1;if(s.IsNegative)n=-n;if(s.Exponent>=0)n<<=s.Exponent;else d<<=-s.Exponent;var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";}
    private static object ConsumerSnapshot(TerrainHeightLayerFirstCurveSparseConsumer r)=>new{
        r.Slice.GeneratorSha256,r.Slice.LayerIndex,r.Slice.LayerSha256,r.HeightSource,r.Height.FamilyId,r.Height.FamilyGeneratorOffset,r.Height.GroupSha256,r.FamilyRawSha256,BatchOriginalSha256=r.Batch.OriginalSha256,
        Policy=(int)r.Policy,r.OriginalNumericParity,Precision=(int)r.Precision,Priors=r.PriorHeightBits,r.Counts,r.CopiedPriorBytes,r.CopiedEncodedInputBytes,r.ArrayBytes,r.MappingBytes,r.Work,r.MaximumAbsoluteHeightBits,
        BorrowedSamplerFactories=r.Batch.SamplerFactories,BorrowedKernelBatches=r.Batch.KernelBatches,BorrowedSamplerDisposals=r.Batch.SamplerDisposals,BorrowedLogCalls=r.Batch.LogCalls,BorrowedPowerCalls=r.Batch.PowerCalls,
        Cells=r.Cells.Select(x=>new{x.Index,x.PriorHeightBits,Product=x.Filtered.ProductBits,x.HeightBits,x.Applied,Request=x.Curve?.Request,
            Curve=x.Curve is null?null:new{Kernel=Fraction(x.Curve.KernelResult.Sample),KernelProjection=x.Curve.KernelResult.Sample.ProjectionBits,x.Curve.Approximation.BaseF32Bits,x.Curve.Approximation.ExponentBinary64Bits,x.Curve.Approximation.OutputBinary64Bits,x.Curve.Approximation.Magnitude,x.Curve.Approximation.Exponent,Policy=(int)x.Curve.Approximation.Policy,x.Curve.Approximation.OriginalNumericParity},
            Supplied=x.Supplied is null?null:new{Fraction=Fraction(x.Supplied),x.Supplied.ProjectionBits,Provenance=(int)x.Supplied.Provenance},Consumer=x.Consumer.Consumer,
            Identity=ReferenceEquals(x.Filtered,r.Filtered.Cells[x.Index])&&(x.Curve is null?!x.Applied:ReferenceEquals(x.Curve,r.Batch.Cells[x.Curve.RequestIndex])&&ReferenceEquals(x.Curve.Approximation,r.Batch.Cells[x.Curve.RequestIndex].Approximation))&&ReferenceEquals(x.Consumer.Sample,x.Supplied)}).ToArray()};
    private static object Snapshot(TerrainHeightLayerSparseFirstCurveEvaluation r)=>new{Consumer=ConsumerSnapshot(r.Consumer),r.Reservation,r.Requests,r.PriorHeightBits,r.OwnedSamplerFactories,r.OwnedSamplerQueryBatches,r.OwnedSamplerDisposals,r.ConsumerCounts,r.BorrowedFilterCounts,r.AccountedAggregateRouteBytes,r.AccountedAggregateWork,Policy=(int)r.Policy,r.OriginalNumericParity,CurveControls=r.CurveControls,HeightPrecision=(int)r.HeightPrecision,
        ActualReferences=ReferenceEquals(r.Filtered,r.Consumer.Filtered)&&ReferenceEquals(r.Batch,r.Consumer.Batch)&&ReferenceEquals(r.Slice,r.Consumer.Slice)&&ReferenceEquals(r.Grid,r.Consumer.Grid)&&ReferenceEquals(r.Cells,r.Consumer.Cells)};
    public static object Run(string path){using var doc=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();int equivalent=0,curveEquivalent=0,empty=0;
        foreach(var c in doc.RootElement.EnumerateArray()){
            string stage="Preparation";
            try{var s=Slice(c);var g=Grid(c);var f=Filter(s,g,c);stage="Evaluation";var result=Evaluate(f,Flag(c,"MismatchSlice")?Slice(c):s,Flag(c,"MismatchGrid")?Grid(c):g,A(c,"Priors"),c);
                Check(ReferenceEquals(result.Filtered,f)&&ReferenceEquals(result.Slice,s)&&ReferenceEquals(result.Grid,g)&&ReferenceEquals(result.Consumer.Batch,result.Batch)&&!result.OriginalNumericParity);
                var referenceBatch=Curve(s,g,f,c);var reference=TerrainHeightLayerFirstCurveSparseConsumer.Evaluate(f,s,g,referenceBatch,A(c,"Priors"),(TerrainHeightArithmeticPrecision)I(c,"HeightPrecision",53),TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation,new(MaximumAbsoluteHeightBits:c.TryGetProperty("MaximumAbsoluteHeightBits",out var bound)?bound.GetUInt32():0x49742400));
                Check(result.CopyHeightBits().SequenceEqual(reference.CopyHeightBits()));
                Check(result.Requests.SequenceEqual(referenceBatch.Cells.Select(x=>x.Request)));
                for(int j=0;j<result.Batch.Cells.Count;j++)Check(result.Batch.Cells[j].Approximation.OutputBinary64Bits==referenceBatch.Cells[j].Approximation.OutputBinary64Bits);
                equivalent+=result.Cells.Count;curveEquivalent+=result.Batch.Cells.Count;
                if(result.Batch.Cells.Count==0){empty++;Check(result.OwnedSamplerFactories==0&&result.OwnedSamplerQueryBatches==0&&result.OwnedSamplerDisposals==0&&result.Batch.LogCalls==0&&result.Batch.PowerCalls==0);}
                cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",ExplicitRouteEquivalent=true,Snapshot=Snapshot(result)});
            }catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input",Stage=stage});}
        }
        var first=doc.RootElement[0];var slice=Slice(first);var grid=Grid(first);var filtered=Filter(slice,grid,first);var priors=A(first,"Priors");var result0=Evaluate(filtered,slice,grid,priors,first);string before=JsonSerializer.Serialize(Snapshot(result0));int checks=0;
        void Stable(){Check(before==JsonSerializer.Serialize(Snapshot(result0)));checks++;}
        Array.Fill(priors,0u);Stable();Array.Fill(result0.CopyPriorHeightBits(),0u);Stable();Array.Fill(result0.CopyHeightBits(),0u);Stable();Array.Fill(result0.Batch.CopyOriginalBytes(),(byte)0);Stable();Array.Fill(result0.Batch.CopyDerivedKernelBytes(),(byte)0);Stable();Array.Fill(slice.Height.CopyEncodedBytes(),(byte)0);Stable();Array.Fill(slice.Height.Family.Raw.CopyEncodedBytes(),(byte)0);Stable();Array.Fill(filtered.CopyProductBits(),0u);Stable();
        bool ro=false;try{((IList<TerrainHeightLayerFirstCurveSparseConsumerCell>)result0.Cells)[0]=result0.Cells[0];}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        ro=false;try{((IList<uint>)result0.PriorHeightBits)[0]=0;}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        Array.Fill(result0.CopyEncodedHeightFamilyBytes(),(byte)0);Stable();
        ro=false;try{((IList<TerrainConditionalFractalSampler.Request>)result0.Requests)[0]=result0.Requests[0];}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        ro=false;try{((IList<uint>)result0.Consumer.PriorHeightBits)[0]=0;}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        var again=Evaluate(filtered,slice,grid,A(first,"Priors"),first);Check(JsonSerializer.Serialize(Snapshot(again))==before);Stable();Parallel.For(0,4,_=>Check(JsonSerializer.Serialize(Snapshot(Evaluate(filtered,slice,grid,A(first,"Priors"),first)))==before));Stable();
        foreach(string name in new[]{"cancel","one-less-MaximumAggregateCopyBytes","late-height-limit","late-height-overflow","late-curve-export-failure"}){
            var c=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()==name);bool rejected=false;
            try{var s=Slice(c);var g=Grid(c);var f=Filter(s,g,c);Evaluate(f,s,g,A(c,"Priors"),c);}catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){rejected=true;}Check(rejected);Stable();
        }
        var small=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()=="precopy-zero-cap");long start=GC.GetAllocatedBytesForCurrentThread();bool pre=false;try{Evaluate(filtered,slice,grid,A(first,"Priors"),small);}catch(InvalidDataException){pre=true;}Check(pre&&GC.GetAllocatedBytesForCurrentThread()-start<16384);Stable();
        var nonfinite=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()=="nonfinite-prior");var invalidPrior=A(nonfinite,"Priors");start=GC.GetAllocatedBytesForCurrentThread();pre=false;
        try{Evaluate(filtered,slice,grid,invalidPrior,nonfinite);}catch(InvalidDataException){pre=true;}Check(pre&&GC.GetAllocatedBytesForCurrentThread()-start<16384);Stable();
        return new{Cases=cases,OwnershipChecks=checks,ExplicitRouteEquivalentHeightCells=equivalent,ExplicitRouteEquivalentCurveOutputs=curveEquivalent,EmptyReceipts=empty};
    }
}
