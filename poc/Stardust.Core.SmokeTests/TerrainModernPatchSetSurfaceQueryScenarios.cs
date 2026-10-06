using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainModernPatchSetSurfaceQueryScenarios
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
    private static double D(ulong bits)=>BitConverter.Int64BitsToDouble(unchecked((long)bits));
    private static TerrainPatchSurfaceLocation[] Locations(JsonElement c)=>c.GetProperty("Locations").EnumerateArray().Select(v=>new TerrainPatchSurfaceLocation(D(v[0].GetUInt64()),D(v[1].GetUInt64()))).ToArray();
    private static TerrainModernPatchSetSurfaceQueryLimits Limits(JsonElement c)
    {
        var l=c.TryGetProperty("Limits",out var value)?value.Deserialize<TerrainModernPatchSetSurfaceQueryLimits>()!:new();var child=l.ChildLimits??new();
        if(c.TryGetProperty("ChildCapBits",out var bits))child=child with{MaximumCoordinateComponent=D(bits.GetUInt64())};
        return l with{ChildLimits=child};
    }
    private static object? Snapshot(TerrainModernPatchSetSurfaceHit? r)=>r is null?null:new{Chunk=r.ChunkIndex,Patch=r.PatchIndex,r.Hit.PatternIndex,r.Hit.TriangleIndex,r.Hit.VertexA,r.Hit.VertexB,r.Hit.VertexC,r.Hit.WeightA,r.Hit.WeightB,r.Hit.WeightC,r.Hit.Height,Normal=new[]{r.Hit.Normal.X,r.Hit.Normal.Y,r.Hit.Normal.Z}};
    private static object Charges(TerrainModernPatchSetSurfaceQuery q)=>new{q.ChunkCount,q.PatchCount,q.TotalTriangles,q.ChildBuilds,q.NormalBuilds,q.GeometryBuilds,q.SetAdmissions,q.OriginalCollisionParity,q.OwnCopyBytes,q.OwnArrayBytes,q.OwnWork,q.ChildPositionCopyBytes,q.ChildNormalCopyBytes,q.ChildIndexCopyBytes,q.ChildCopyBytes,q.ChildAreaArrayBytes,q.ChildArrayBytes,q.ChildWork,q.ChildLogicalOwnedBytes,q.AggregateCopyBytes,q.AggregateArrayBytes,q.AggregateWork,q.BorrowedSetArrayReservation};
    private static void Identity(TerrainModernPatchSetSurfaceQuery q,TerrainModernPatchSetSurfaceHit? hit)
    {
        if(hit is null)return;var chunk=q.Source.Patches[hit.ChunkIndex];var patch=chunk.Patch.Patches[hit.PatchIndex];
        Need(ReferenceEquals(hit.Chunk,chunk)&&ReferenceEquals(hit.Patch,patch)&&ReferenceEquals(hit.Geometry,patch.Geometry),"Actual hit source identities.");
        var membership=q.Children.Single(c=>c.ChunkIndex==hit.ChunkIndex&&c.PatchIndex==hit.PatchIndex);Need(ReferenceEquals(hit.Child,membership.Query),"Actual child membership.");
    }
    public static object Run(string fixtures,string originalFixture)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));var root=doc.RootElement;var pool=root.GetProperty("Chunks").EnumerateArray().Select(Make).ToArray();
        var sets=root.GetProperty("Sets").EnumerateArray().Select(c=>TerrainModernHeightPatchSet.Build(c.GetProperty("Ids").EnumerateArray().Select(i=>pool[i.GetInt32()]).ToArray(),(TerrainModernPatchEdgePolicy)c.GetProperty("Edge").GetInt32(),(TerrainModernPatchNormalPolicy)c.GetProperty("Normal").GetInt32())).ToArray();
        var before=pool.Select(p=>p.CopyPositionHeightBits()).ToArray();var output=new List<object>();int checks=0,hits=0,misses=0;
        foreach(var c in root.GetProperty("Cases").EnumerateArray())
        {
            try
            {
                var set=sets[c.GetProperty("Set").GetInt32()];var query=TerrainModernPatchSetSurfaceQuery.Build(set,Limits(c),new CancellationToken(c.TryGetProperty("CancelBuild",out var cb)&&cb.GetBoolean()));
                Need(ReferenceEquals(query.Source,set)&&query.Children.Count==set.TotalPatches&&query.ChildBuilds==set.TotalPatches,"Actual set and once-built child count.");checks++;
                foreach(var child in query.Children){Need(ReferenceEquals(child.Chunk,set.Patches[child.ChunkIndex])&&ReferenceEquals(child.Patch,child.Chunk.Patch.Patches[child.PatchIndex])&&ReferenceEquals(child.Geometry,child.Patch.Geometry),"Actual child identities.");checks++;}
                var points=Locations(c);var result=query.QueryBatch(points,new CancellationToken(c.TryGetProperty("CancelQuery",out var cq)&&cq.GetBoolean()));var reservation=query.GetQueryReservation(points.Length);
                for(int i=0;i<points.Length;i++){Need(result[i]==query.Query(points[i].X,points[i].Z),"Scalar/batch identical ordered result.");Identity(query,result[i]);if(result[i] is not null)Need(result[i]!.Hit==result[i]!.Child.Query(points[i].X,points[i].Z),"Actual child hit value.");if(result[i] is null)misses++;else hits++;checks++;}
                var originalPointsCopy=(TerrainPatchSurfaceLocation[])points.Clone();var saved=result.ToArray();Array.Fill(points,new TerrainPatchSurfaceLocation(double.NaN,0));Need(result.SequenceEqual(saved),"Detached captured locations.");checks++;
                var copied=query.CopyChildren();if(copied.Length>0){copied[0]=null!;Array.Clear(query.Children[0].Query.CopyPositions());Array.Clear(query.Children[0].Query.CopyNormals());Array.Clear(query.Children[0].Query.CopyIndices());}
                Need(query.QueryBatch(originalPointsCopy).SequenceEqual(saved),"Detached child copies preserve query results.");checks++;
                bool ro=false;try{((IList<TerrainModernPatchSetSurfaceChild>)query.Children).Add(null!);}catch(NotSupportedException){ro=true;}Need(ro,"Read-only child membership.");checks++;
                ro=false;try{((IList<TerrainModernPatchSetSurfaceHit?>)result).Add(null);}catch(NotSupportedException){ro=true;}Need(ro,"Read-only batch result.");checks++;
                if(c.TryGetProperty("TruncateSource",out _))
                {
                    var source=set.Patches[0].Source;var raw=source.CopyInputHeightBits();_=TerrainCoordinateHeightPatch.Build(source.Coordinates,raw.AsSpan(0,raw.Length-1),source.CopyNormals(),source.CopyArgbColors());throw new InvalidOperationException("Truncated source must reject.");
                }
                output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Charges=Charges(query),Reservation=reservation,Hits=saved.Select(Snapshot).ToArray()});
            }
            catch(Exception e)when(e is ArgumentException or InvalidDataException or OperationCanceledException or OverflowException){output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});checks++;}
            Need(pool.Select((p,i)=>p.CopyPositionHeightBits().SequenceEqual(before[i])).All(v=>v),"Sources unchanged after success/failure.");checks++;
        }
        var prior=TerrainModernPatchSetSurfaceQuery.Build(sets[0]);var old=prior.Query(.5,.75);for(int i=0;i<8;i++){Need(prior.Query(.5,.75)==old,"Repeat query.");checks++;}
        Parallel.For(0,8,i=>Need(prior.Query(.5,.75)==old,"Concurrent query."));checks+=8;
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();bool failed=false;try{_=prior.QueryBatch(new[]{new TerrainPatchSurfaceLocation(.5,.75)},cancelled.Token);}catch(OperationCanceledException){failed=true;}
        Need(failed&&prior.Query(.5,.75)==old,"Cancelled request preserves prior.");checks++;
        failed=false;try{_=prior.Query(.5,.75,cancelled.Token);}catch(OperationCanceledException){failed=true;}
        Need(failed&&prior.Query(.5,.75)==old,"Cancelled scalar preserves prior.");checks++;
        var late=root.GetProperty("Cases").EnumerateArray().First(c=>c.TryGetProperty("LateFailure",out _));failed=false;
        try{var q=TerrainModernPatchSetSurfaceQuery.Build(sets[late.GetProperty("Set").GetInt32()],Limits(late));_=q.QueryBatch(Locations(late));}catch(InvalidDataException){failed=true;}
        Need(failed&&prior.Query(.5,.75)==old,"Late child failure preserves prior.");checks++;
        var original=OriginalSet(originalFixture);var actual=TerrainModernPatchSetSurfaceQuery.Build(original);var originalPoints=root.GetProperty("OriginalLocations").EnumerateArray().Select(v=>new TerrainPatchSurfaceLocation(D(v[0].GetUInt64()),D(v[1].GetUInt64()))).ToArray();var originalHits=actual.QueryBatch(originalPoints);
        foreach(var hit in originalHits)Identity(actual,hit);Need(actual.PatchCount==16&&actual.Source.Patches.Zip(original.Patches).All(v=>ReferenceEquals(v.First,v.Second)),"Actual sealed set membership.");
        return new{Cases=output,OwnershipChecks=checks,HitChecks=hits,MissChecks=misses,Original=new{Charges=Charges(actual),Hits=originalHits.Select(Snapshot).ToArray(),OriginalCollisionParity=false,HeightsNormalsCompared=400,SyntheticWarmPriors=true}};
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
