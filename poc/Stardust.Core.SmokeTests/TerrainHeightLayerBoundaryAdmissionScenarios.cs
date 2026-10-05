using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainHeightLayerBoundaryAdmissionScenarios
{
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var p)?p.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var p)?p.GetInt64():d;
    private static void Check(bool b){if(!b)throw new InvalidOperationException("Boundary admission invariant.");}
    private static TerrainSampleCoordinateGrid Grid(JsonElement c)
    {
        var r=c.GetProperty("Request");var p=c.GetProperty("Profiles").EnumerateArray().Select(x=>(TerrainHeightArithmeticPrecision)x.GetInt32()).ToArray();
        return TerrainSampleCoordinateGrid.Build(new(r.GetProperty("ChunkWidthBits").GetUInt32(),I(r,"Tiles",2),I(r,"Margin",0),I(r,"First",0),I(r,"Second",0),I(r,"Scale",1)),p[0],p[1],p[2],p[3],new(1024,1048576));
    }
    private static TerrainHeightLayerBoundaryAdmission Evaluate(TerrainGeneratorHeightLayerSlice s,TerrainSampleCoordinateGrid g,JsonElement c)
        =>TerrainHeightLayerBoundaryAdmission.Evaluate(s,g,new((TerrainHeightArithmeticPrecision)I(c,"Cache",53),(TerrainHeightArithmeticPrecision)I(c,"Query",53),(TerrainHeightArithmeticPrecision)I(c,"Combine",53)),
            new(I(c,"MaximumAxisCount",256),I(c,"MaximumCells",65536),I(c,"MaximumQueries",65536),L(c,"MaximumArrayBytes",16777216),L(c,"MaximumCopyBytes",1572864),L(c,"MaximumWork",4194304)),new CancellationToken(c.TryGetProperty("Cancel",out var x)&&x.GetBoolean()));
    private static object Snapshot(TerrainHeightLayerBoundaryAdmission r)=>new{
        r.Slice.GeneratorSha256,r.Slice.LayerIndex,r.Slice.LayerSha256,r.CircleSource,r.Grid.Request,r.Grid.Policies,r.Grid.Parameters,r.Controls,
        r.AxisCount,r.QueryCount,r.CarrierCount,r.ReceiptCount,r.ArrayBytes,r.CopiedEntryBytes,r.CopiedInputBytes,r.Work,
        Cells=r.Cells.Select(x=>new{x.Coordinates,x.BoundaryBits,x.Admitted,
            Circle=new{x.CircleQuery.FirstCoordinateBits,x.CircleQuery.SecondCoordinateBits,x.CircleQuery.RuntimeFeatherAmountBits,x.CircleQuery.RadiusSquaredBits,x.CircleQuery.DistanceSquaredBits,x.CircleQuery.QueryProjectionBits,Branch=(int)x.CircleQuery.Branch,CachePrecision=(int)x.CircleQuery.CachePrecision,Precision=(int)x.CircleQuery.Precision},
            Carrier=new{Source=(int)x.Carrier.Source,Precision=(int)x.Carrier.Precision,x.Carrier.RawBits,x.Carrier.ProjectionBits},
            Probe=new{x.BoundaryProbe.LocalInfluenceBits,x.BoundaryProbe.ProductBits,x.BoundaryProbe.BoundaryFallback,x.BoundaryProbe.BoundaryEarlyExit,x.BoundaryProbe.FilterEarlyExit,x.BoundaryProbe.BoundaryPositive,x.BoundaryProbe.LocalPositive,x.BoundaryProbe.AffectorEligible,x.BoundaryProbe.ProductPositive,x.BoundaryProbe.BoundaryVisits,x.BoundaryProbe.FilterVisits},
            Identity=ReferenceEquals(x.CircleQuery.Fields,r.Slice.Circle.Fields)&&ReferenceEquals(x.Carrier.Circle,x.CircleQuery)&&ReferenceEquals(x.BoundaryProbe.Sample.Boundaries[0].Query,x.Carrier)}).ToArray()};
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray()){
            try{var g=TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!));var b=TerrainGeneratorFractalFamilies.Bind(g);
                var s=TerrainGeneratorHeightLayerSlice.Select(g,b,I(c,"Index",0),(TerrainGeneratorHeightLayerSlicePolicy)I(c,"Policy",0));var grid=Grid(c);var a=Evaluate(s,grid,c);
                Check(ReferenceEquals(a.Slice,s)&&ReferenceEquals(a.Grid,grid));Check(a.Cells.All(x=>x.BoundaryProbe.Sample.Filters.Count==0&&x.BoundaryProbe.Sample.BoundaryGate==1&&x.BoundaryProbe.Sample.FilterGate==0&&!x.BoundaryProbe.Sample.InvertFilter&&x.BoundaryProbe.Sample.InvertBoundary==s.BoundaryInverted&&x.BoundaryProbe.Sample.IncomingWeightBits==0x3f800000));
                cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Snapshot=Snapshot(a)});
            }catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});}
        }
        var first=doc.RootElement[0];var raw=Convert.FromHexString(first.GetProperty("GeneratorHex").GetString()!);var source=TerrainGeneratorInput.Decode(raw);
        var slice=TerrainGeneratorHeightLayerSlice.Select(source,TerrainGeneratorFractalFamilies.Bind(source),0,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);var grid0=Grid(first);var result=Evaluate(slice,grid0,first);
        string before=JsonSerializer.Serialize(Snapshot(result));int checks=0;
        void Stable(){Check(before==JsonSerializer.Serialize(Snapshot(result)));checks++;}
        Array.Fill(raw,(byte)0);Stable();Array.Fill(slice.CopyAdtaWordBits(),0u);Stable();Array.Fill(slice.Circle.CopyDataWordBits(),0u);Stable();
        Array.Fill(result.CopyBoundaryBits(),0u);Stable();Array.Fill(result.CopyAdmission(),false);Stable();Array.Fill(grid0.Layer.CopyFirstAxisBits(),0u);Stable();Array.Fill(grid0.Layer.CopySecondAxisBits(),0u);Stable();
        bool ro=false;try{((IList<TerrainHeightLayerBoundaryAdmissionCell>)result.Cells)[0]=result.Cells[0];}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        var repeated=Evaluate(slice,grid0,first);Check(JsonSerializer.Serialize(Snapshot(repeated))==before&&!ReferenceEquals(result.Cells[0].CircleQuery,repeated.Cells[0].CircleQuery));Stable();
        Parallel.For(0,8,_=>Check(JsonSerializer.Serialize(Snapshot(Evaluate(slice,grid0,first)))==before));Stable();
        foreach(string name in new[]{"cancel","one-less-MaximumQueries","late-overflow"}){
            var c=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()==name);bool rejected=false;
            try{Evaluate(slice,Grid(c),c);}catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){rejected=true;}Check(rejected);Stable();
        }
        var large=doc.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()=="preallocation-large-grid");var big=Grid(large);long start=GC.GetAllocatedBytesForCurrentThread();bool pre=false;
        try{Evaluate(slice,big,large);}catch(InvalidDataException){pre=true;}Check(pre&&GC.GetAllocatedBytesForCurrentThread()-start<16384);Stable();
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
