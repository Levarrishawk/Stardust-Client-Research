using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainModernPatchSetRayQueryScenarios
{
    private static void Need(bool okay,string why){if(!okay)throw new InvalidOperationException(why);}
    private static uint[] A(JsonElement c,string k)=>c.GetProperty(k).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
    private static uint[][] Bits(IEnumerable<Vector3> v)=>v.Select(n=>new[]{BitConverter.SingleToUInt32Bits(n.X),BitConverter.SingleToUInt32Bits(n.Y),BitConverter.SingleToUInt32Bits(n.Z)}).ToArray();
    private static TerrainModernHeightPatch Make(JsonElement c)
    {
        var p=c.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
        var grid=TerrainSampleCoordinateGrid.Build(c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),p[0],p[1],p[2],p[3]);
        if(c.TryGetProperty("VerticalOriginBits",out var origin)&&origin.GetUInt32()!=0)
        {
            // Authored signed-zero metadata only, using actual frozen grid/source/normal builders.
            var constructor=typeof(TerrainSampleCoordinateGrid).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
            grid=(TerrainSampleCoordinateGrid)constructor.Invoke(new object[]{grid.Request,grid.Policies,grid.Parameters with{VerticalOriginBits=origin.GetUInt32()},grid.Layer,grid.Position});
        }
        var heights=A(c,"Heights");var source=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(Vector3.UnitY,heights.Length).ToArray(),A(c,"Colors"),c.GetProperty("Mask").GetInt32(),BitConverter.UInt32BitsToSingle(c.GetProperty("ScaleBits").GetUInt32()));
        return TerrainModernHeightPatch.Build(source,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
    }
    private static double D(JsonElement v)=>BitConverter.Int64BitsToDouble(unchecked((long)v.GetUInt64()));
    private static TerrainModernRayVector Vector(JsonElement a)=>new(D(a[0]),D(a[1]),D(a[2]));
    private static TerrainModernPatchSetRay[] Rays(JsonElement c)=>c.GetProperty("Rays").EnumerateArray().Select(v=>new TerrainModernPatchSetRay(Vector(v.GetProperty("O")),Vector(v.GetProperty("D")),D(v.GetProperty("Min")),D(v.GetProperty("Max")))).ToArray();
    private static TerrainModernPatchSetRayQueryLimits Limits(JsonElement c)
    {
        var l=c.TryGetProperty("Limits",out var a)?a.Deserialize<TerrainModernPatchSetRayQueryLimits>()!:new();
        if(c.TryGetProperty("ComponentBits",out var b))l=l with{MaximumComponent=D(b)};
        if(c.TryGetProperty("ParameterBits",out b))l=l with{MaximumParameter=D(b)};
        if(c.TryGetProperty("NormalBits",out b))l=l with{MinimumInterpolatedNormalLength=D(b)};
        return l;
    }
    private static object? Hit(TerrainModernPatchSetRayHit? h)=>h is null?null:new{Chunk=h.ChunkIndex,Patch=h.PatchIndex,h.PatternIndex,h.TriangleIndex,h.VertexA,h.VertexB,h.VertexC,h.T,h.WeightA,h.WeightB,h.WeightC,Position=new[]{h.Position.X,h.Position.Y,h.Position.Z},Normal=new[]{h.Normal.X,h.Normal.Y,h.Normal.Z}};
    private static object Charges(TerrainModernPatchSetRayQuery q)=>new{q.ChunkCount,q.PatchCount,q.TotalTriangles,q.OwnCopyBytes,q.OwnArrayBytes,q.OwnWork,q.BorrowedSurfaceArrayReservation,q.NormalBuilds,q.GeometryBuilds,q.SurfaceBuilds,q.SetAdmissions,q.OriginalCollisionParity};
    private static void Identity(TerrainModernPatchSetRayQuery q,TerrainModernPatchSetRayHit? h)
    {
        if(h is null)return;Need(ReferenceEquals(h.Source,q.Source)&&ReferenceEquals(h.Membership,q.Source.Children.Single(c=>c.ChunkIndex==h.ChunkIndex&&c.PatchIndex==h.PatchIndex))&&ReferenceEquals(h.Chunk,q.Source.Source.Patches[h.ChunkIndex])&&ReferenceEquals(h.Patch,h.Chunk.Patch.Patches[h.PatchIndex])&&ReferenceEquals(h.Geometry,h.Patch.Geometry)&&ReferenceEquals(h.Child,h.Membership.Query),"Actual identity chain.");
    }
    public static object Run(string fixture,string originalFixture)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixture));var root=doc.RootElement;
        var pool=root.GetProperty("Chunks").EnumerateArray().Select(Make).ToArray();var sets=root.GetProperty("Sets").EnumerateArray().Select(c=>TerrainModernHeightPatchSet.Build(c.GetProperty("Ids").EnumerateArray().Select(i=>pool[i.GetInt32()]).ToArray(),(TerrainModernPatchEdgePolicy)c.GetProperty("Edge").GetInt32(),(TerrainModernPatchNormalPolicy)c.GetProperty("Normal").GetInt32())).ToArray();
        var surfaces=sets.Select(s=>TerrainModernPatchSetSurfaceQuery.Build(s)).ToArray();var before=pool.Select(p=>p.CopyPositionHeightBits()).ToArray();var output=new List<object>();int checks=0;
        foreach(var c in root.GetProperty("Cases").EnumerateArray())
        {
            try
            {
                var surface=surfaces[c.GetProperty("Set").GetInt32()];var query=TerrainModernPatchSetRayQuery.Build(surface,Limits(c),new CancellationToken(c.TryGetProperty("CancelBuild",out var cb)&&cb.GetBoolean()));
                Need(ReferenceEquals(query.Source,surface)&&ReferenceEquals(query.Children,surface.Children),"Actual borrowed surface and membership.");checks++;
                var rays=Rays(c);var result=query.QueryBatch(rays,new CancellationToken(c.TryGetProperty("CancelQuery",out var cq)&&cq.GetBoolean()));var saved=result.ToArray();
                foreach(var pair in rays.Zip(result)){Need(query.Query(pair.First)==pair.Second,"Scalar batch equality.");Identity(query,pair.Second);checks++;}
                if(query.PatchCount>0){Array.Clear(query.CopyPositions(0));Array.Clear(query.CopyNormals(0));Array.Clear(query.CopyIndices(0));}
                Need(query.QueryBatch(rays).SequenceEqual(saved),"Detached geometry exports.");checks++;
                var old=(TerrainModernPatchSetRay[])rays.Clone();Array.Fill(rays,default);Need(query.QueryBatch(old).SequenceEqual(saved),"Detached ray input.");checks++;
                bool readOnly=false;try{((IList<TerrainModernPatchSetRayHit?>)result).Add(null);}catch(NotSupportedException){readOnly=true;}Need(readOnly,"Immutable results.");checks++;
                output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Charges=Charges(query),Reservation=query.GetQueryReservation(old.Length),Hits=saved.Select(Hit).ToArray()});
            }
            catch(Exception e)when(e is ArgumentException or InvalidDataException or OperationCanceledException or OverflowException){output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});checks++;}
            Need(pool.Select((p,i)=>p.CopyPositionHeightBits().SequenceEqual(before[i])).All(v=>v),"Failure preserves actual sources.");checks++;
        }
        var prior=TerrainModernPatchSetRayQuery.Build(surfaces[0]);var ray=new TerrainModernPatchSetRay(new(.5,100,.75),new(0,-2,0),0,100);var hit=prior.Query(ray);
        Parallel.For(0,8,i=>Need(prior.Query(ray)==hit,"Repeat concurrent immutable query."));checks+=8;
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();bool rejected=false;try{_=prior.Query(ray,cancelled.Token);}catch(OperationCanceledException){rejected=true;}Need(rejected&&prior.Query(ray)==hit,"Scalar precancel preserves prior.");checks++;
        rejected=false;try{_=prior.QueryBatch(new[]{ray},cancelled.Token);}catch(OperationCanceledException){rejected=true;}Need(rejected&&prior.Query(ray)==hit,"Batch precancel preserves prior.");checks++;
        var late=root.GetProperty("Cases").EnumerateArray().First(c=>c.TryGetProperty("LateFailure",out _));rejected=false;
        var lateQuery=TerrainModernPatchSetRayQuery.Build(surfaces[late.GetProperty("Set").GetInt32()],Limits(late));Need(lateQuery.Query(Rays(late)[0]) is not null,"First late-failure batch member succeeds.");checks++;
        try{_=lateQuery.QueryBatch(Rays(late));}catch(InvalidDataException){rejected=true;}Need(rejected&&prior.Query(ray)==hit,"Late result failure preserves prior.");checks++;
        var largest=surfaces.OrderByDescending(s=>s.TotalTriangles).ThenByDescending(s=>s.ChunkCount).First();var active=TerrainModernPatchSetRayQuery.Build(largest);var many=Enumerable.Repeat(ray,4096).ToArray();
        using(var during=new CancellationTokenSource())
        {
            var stop=new Thread(()=>{Thread.Sleep(10);during.Cancel();});stop.Start();rejected=false;
            try{_=active.QueryBatch(many,during.Token);}catch(OperationCanceledException){rejected=true;}finally{stop.Join();}
            Need(rejected&&prior.Query(ray)==hit,"Timed cancellation rejects bounded active batch and preserves prior.");checks++;
        }
        rejected=false;try{_=new TerrainPatchSurfaceQuery(new TerrainPatchGeometry(new Vector3[9],Enumerable.Repeat(Vector3.UnitY,9).ToArray(),new uint[9],1,0));}catch(ArgumentException){rejected=true;}Need(rejected,"Degenerate projected triangle rejected upstream before a retained surface can exist.");checks++;
        var original=TerrainModernPatchSetSurfaceQuery.Build(OriginalSet(originalFixture));var oq=TerrainModernPatchSetRayQuery.Build(original);var originalRays=Rays(root.GetProperty("Original"));var oh=oq.QueryBatch(originalRays);foreach(var h in oh)Identity(oq,h);
        return new{Cases=output,OwnershipChecks=checks,Original=new{Charges=Charges(oq),Reservation=oq.GetQueryReservation(originalRays.Length),Hits=oh.Select(Hit).ToArray(),HeightsNormalsCompared=400,OriginalCollisionParity=false,SyntheticWarmPriors=true}};
    }
    private static TerrainModernHeightPatchSet OriginalSet(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var root=doc.RootElement;
        Need(root.GetProperty("Version").GetInt32()==9&&!root.GetProperty("OriginalNumericParity").GetBoolean(),"Sealed approximate fixture.");
        var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));var binding=TerrainGeneratorFractalFamilies.Bind(generator);
        var slice=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);var candidates=new List<TerrainModernHeightPatch>();int index=0;
        foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
        {
            var grid=TerrainSampleCoordinateGrid.Build(chunk.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
            var boundary=TerrainHeightLayerBoundaryAdmission.Evaluate(slice,grid,new(TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
            var filtered=TerrainHeightLayerFilteredInfluence.Evaluate(boundary,slice,grid,A(chunk,"IncomingWeightBits"),new(53,64,64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
            var evaluation=TerrainHeightLayerSparseFirstCurveEvaluation.Evaluate(filtered,slice,grid,A(chunk,"PriorHeightBits"),new(53,64,64),TerrainHeightArithmeticPrecision.Binary64,TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);
            var height=evaluation.CopyHeightBits();var source=TerrainCoordinateHeightPatch.Build(grid,height,Enumerable.Repeat(Vector3.UnitY,height.Length).ToArray(),Enumerable.Repeat(0xff20a060u,height.Length).ToArray());
            var actual=TerrainModernHeightPatch.Build(source,TerrainModernNormalPolicy.ModernBinary64CentralDifference);var expected=root.GetProperty("Expected")[index++];
            Need(actual.CopyPositionHeightBits().SequenceEqual(A(expected,"HeightBits"))&&Bits(actual.CopyNormals()).SelectMany(a=>a).SequenceEqual(expected.GetProperty("NormalBits").EnumerateArray().SelectMany(v=>v.EnumerateArray().Select(b=>b.GetUInt32()))),"Sealed original-layer modern snapshot.");candidates.Add(actual);
        }
        var supplied=candidates.ToArray();var set=TerrainModernHeightPatchSet.Build(supplied,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals);
        Need(set.Count==4&&set.SharedEdgePairs==4&&set.SharedCornerPairs==2&&set.SharedSampleComparisons==22&&set.Patches.Zip(supplied).All(v=>ReferenceEquals(v.First,v.Second)),"Pinned strict edge/corner identities.");
        return set;
    }
}
