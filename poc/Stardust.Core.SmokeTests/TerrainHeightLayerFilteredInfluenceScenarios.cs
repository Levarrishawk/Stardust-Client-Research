using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainHeightLayerFilteredInfluenceScenarios
{
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var p)?p.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var p)?p.GetInt64():d;
    private static bool Flag(JsonElement c,string k)=>c.TryGetProperty(k,out var p)&&p.GetBoolean();
    private static void Check(bool b){if(!b)throw new InvalidOperationException("Filtered influence invariant.");}
    private static TerrainGeneratorHeightLayerSlice Slice(JsonElement c){var g=TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!));return TerrainGeneratorHeightLayerSlice.Select(g,TerrainGeneratorFractalFamilies.Bind(g),I(c,"Index",0),TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);}
    private static TerrainSampleCoordinateGrid Grid(JsonElement c){var r=c.GetProperty("Request");var p=c.GetProperty("Profiles").EnumerateArray().Select(x=>(TerrainHeightArithmeticPrecision)x.GetInt32()).ToArray();return TerrainSampleCoordinateGrid.Build(new(r.GetProperty("ChunkWidthBits").GetUInt32(),I(r,"Tiles",2),I(r,"Margin",0),I(r,"First",0),I(r,"Second",0),I(r,"Scale",1)),p[0],p[1],p[2],p[3],new(1024,1048576));}
    private static TerrainHeightLayerBoundaryAdmission Boundary(TerrainGeneratorHeightLayerSlice s,TerrainSampleCoordinateGrid g,JsonElement c)=>TerrainHeightLayerBoundaryAdmission.Evaluate(s,g,new((TerrainHeightArithmeticPrecision)I(c,"Cache",53),(TerrainHeightArithmeticPrecision)I(c,"CircleQuery",53),(TerrainHeightArithmeticPrecision)I(c,"BoundaryCombine",I(c,"Combine",53))),new(1024,1048576,1048576,268435456,25165824,67108864));
    private static TerrainHeightLayerFilteredInfluence Evaluate(TerrainHeightLayerBoundaryAdmission b,TerrainGeneratorHeightLayerSlice s,TerrainSampleCoordinateGrid g,uint[] weights,JsonElement c,CancellationToken token=default)=>TerrainHeightLayerFilteredInfluence.Evaluate(b,s,g,weights,new(I(c,"Table",53),I(c,"Construction",53),I(c,"SamplerQuery",53),(TerrainHeightArithmeticPrecision)I(c,"FilterQuery",53),(TerrainHeightArithmeticPrecision)I(c,"Combine",53),(TerrainHeightArithmeticPrecision)I(c,"Product",53)),
        new(I(c,"MaximumAxisCount",32),I(c,"MaximumCells",1024),I(c,"MaximumFilterQueries",1024),L(c,"MaximumCopyBytes",131072),L(c,"MaximumArrayBytes",1048576),L(c,"MaximumRouteBytes",8388608),L(c,"MaximumWork",16777216),I(c,"MaximumSamplerBytes",1048576),I(c,"MaximumSamplerWork",1048576),I(c,"MaximumQueryBytes",2097152),I(c,"MaximumQueryWork",16777216)),Flag(c,"Cancel")?new CancellationToken(true):token);
    private static string Fraction(TerrainSuppliedFractalSample s){if(s.Magnitude==0)return s.NegativeZero?"-0/1":"0/1";BigInteger n=s.Magnitude,d=1;if(s.IsNegative)n=-n;if(s.Exponent>=0)n<<=s.Exponent;else d<<=-s.Exponent;var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";}
    private static object Snapshot(TerrainHeightLayerFilteredInfluence r)=>new{
        r.Slice.GeneratorSha256,r.Slice.LayerIndex,r.Slice.LayerSha256,r.FilterSource,r.Filter.FamilyId,r.Filter.FamilyGeneratorOffset,r.Filter.GroupSha256,r.Controls,
        Incoming=r.IncomingWeightBits,r.Counts,r.ArrayBytes,r.CopiedArrayBytes,r.CopiedInputBytes,r.CopiedEncodedInputBytes,r.ReservedCopyBytes,r.ReservedRouteBytes,r.AccountedRouteBytes,r.ReservedWork,r.AccountedWork,r.ReservedSamplerBytes,r.ReservedSamplerWork,r.ReservedQueryBytes,r.ReservedQueryWork,
        Cells=r.Cells.Select(x=>new{x.Index,x.Boundary.Coordinates,BoundaryBits=x.Boundary.BoundaryBits,x.Boundary.Admitted,x.IncomingWeightBits,
            Computed=x.Computed is null?null:new{Origin=(int)x.Computed.Origin,Retained=Fraction(x.Computed.Sample),Projection=x.Computed.Sample.ProjectionBits,Provenance=(int)x.Computed.Sample.Provenance},
            Filter=x.FilterQuery is null?null:new{x.FilterQuery.SuppliedSampleBits,x.FilterQuery.ScaledSampleBits,x.FilterQuery.RuntimeAmountBits,x.FilterQuery.QueryProjectionBits,x.FilterQuery.FeatherSpanProjectionBits,Branch=(int)x.FilterQuery.Branch,Precision=(int)x.FilterQuery.Precision},
            Carrier=x.FilterCarrier is null?null:new{Source=(int)x.FilterCarrier.Source,Precision=(int)x.FilterCarrier.Precision,x.FilterCarrier.ProjectionBits},
            Influence=new{x.Influence.LocalInfluenceBits,x.Influence.ProductBits,x.Influence.BoundaryFallback,x.Influence.BoundaryEarlyExit,x.Influence.FilterEarlyExit,x.Influence.BoundaryPositive,x.Influence.LocalPositive,x.Influence.AffectorEligible,x.Influence.ProductPositive,x.Influence.BoundaryVisits,x.Influence.FilterVisits},
            Identity=ReferenceEquals(x.Boundary,r.Boundary.Cells[x.Index])&&ReferenceEquals(x.Influence.Sample.Boundaries[0].Query,x.Boundary.Carrier)&&(!x.Boundary.Admitted
                ?x.Computed is null&&x.FilterQuery is null&&x.FilterCarrier is null&&x.Influence.Sample.Filters.Count==0
                :ReferenceEquals(x.Computed!.Sample,x.FilterQuery!.RetainedSample)&&ReferenceEquals(x.FilterQuery.Input,r.Filter.Input)&&ReferenceEquals(x.FilterCarrier!.FractalFilter,x.FilterQuery)&&ReferenceEquals(x.Influence.Sample.Filters[0].Query,x.FilterCarrier))}).ToArray()};
    public static object Run(string path){
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray()){
            try{var s=Slice(c);var g=Grid(c);var b=Boundary(s,g,c);var weights=c.GetProperty("Incoming").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();
                using var active=new CancellationTokenSource();if(Flag(c,"ActiveCancel")){active.CancelAfter(10);Check(!active.IsCancellationRequested);}
                var r=Evaluate(b,Flag(c,"MismatchSlice")?Slice(c):s,Flag(c,"MismatchGrid")?Grid(c):g,weights,c,active.Token);Check(!Flag(c,"ActiveCancel"));Check(ReferenceEquals(r.Boundary,b)&&ReferenceEquals(r.Slice,s)&&ReferenceEquals(r.Grid,g));
                Check(r.Cells.All(x=>x.Influence.Sample.InvertBoundary==s.BoundaryInverted&&x.Influence.Sample.InvertFilter==s.FilterInverted&&x.Influence.Sample.IncomingWeightBits==x.IncomingWeightBits));
                cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Snapshot=Snapshot(r)});
            }catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){Check(!Flag(c,"ActiveCancel")||ex is OperationCanceledException);cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});}
        }
        var first=doc.RootElement[0];var slice=Slice(first);var grid=Grid(first);var boundary=Boundary(slice,grid,first);var incoming=first.GetProperty("Incoming").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();var result=Evaluate(boundary,slice,grid,incoming,first);string before=JsonSerializer.Serialize(Snapshot(result));int checks=0;
        void Stable(){Check(before==JsonSerializer.Serialize(Snapshot(result)));checks++;}
        Array.Fill(incoming,0u);Stable();Array.Fill(result.CopyIncomingWeightBits(),0u);Stable();Array.Fill(result.CopyLocalInfluenceBits(),0u);Stable();Array.Fill(result.CopyProductBits(),0u);Stable();
        Array.Fill(slice.Filter.CopyEncodedBytes(),(byte)0);Stable();Array.Fill(slice.CopyAdtaWordBits(),0u);Stable();Array.Fill(grid.Layer.CopyFirstAxisBits(),0u);Stable();
        bool ro=false;try{((IList<TerrainHeightLayerFilteredInfluenceCell>)result.Cells)[0]=result.Cells[0];}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        ro=false;try{((IList<uint>)result.IncomingWeightBits)[0]=0;}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        var repeatWeights=first.GetProperty("Incoming").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();var again=Evaluate(boundary,slice,grid,repeatWeights,first);Check(JsonSerializer.Serialize(Snapshot(again))==before);Stable();
        Parallel.For(0,4,_=>Check(JsonSerializer.Serialize(Snapshot(Evaluate(boundary,slice,grid,repeatWeights,first)))==before));Stable();
        foreach(var name in new[]{"cancel","one-less-MaximumCopyBytes","late-filter-overflow","active-cancel"}){
            var c=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()==name);bool rejected=false;
            try{var s=Slice(c);var g=Grid(c);var b=Boundary(s,g,c);var w=c.GetProperty("Incoming").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();using var active=new CancellationTokenSource();if(Flag(c,"ActiveCancel")){active.CancelAfter(10);Check(!active.IsCancellationRequested);}Evaluate(b,s,g,w,c,active.Token);}catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){Check(!Flag(c,"ActiveCancel")||ex is OperationCanceledException);rejected=true;}Check(rejected);Stable();
        }
        var large=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()=="precopy-large-grid");var lg=Grid(large);var lb=Boundary(slice,lg,large);var lw=new uint[lg.Layer.Count*lg.Layer.Count];long start=GC.GetAllocatedBytesForCurrentThread();bool pre=false;
        try{Evaluate(lb,slice,lg,lw,large);}catch(InvalidDataException){pre=true;}Check(pre&&GC.GetAllocatedBytesForCurrentThread()-start<16384);Stable();
        var retained=result.Cells.First(x=>x.Computed is not null);var q=TerrainFilterFractalInfluence.Query(slice.Filter.Input,retained.Computed!.Sample,result.Controls.FilterQueryPrecision);Check(q.ScaledSampleBits==retained.FilterQuery!.ScaledSampleBits);Stable();
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
