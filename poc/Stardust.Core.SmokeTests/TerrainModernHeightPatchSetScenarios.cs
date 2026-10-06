using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainModernHeightPatchSetScenarios
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
    private static TerrainModernHeightPatchSet Admit(TerrainModernHeightPatch[] input,JsonElement c)=>TerrainModernHeightPatchSet.Build(input,
        (TerrainModernPatchEdgePolicy)c.GetProperty("Edge").GetInt32(),(TerrainModernPatchNormalPolicy)c.GetProperty("Normal").GetInt32(),
        c.TryGetProperty("Limits",out var limits)?limits.Deserialize<TerrainModernHeightPatchSetLimits>():null,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
    private static object Snapshot(TerrainModernHeightPatchSet r)=>new{r.Count,r.TotalCells,r.TotalPatches,r.SelectedSamples,r.OwnCopyBytes,r.OwnArrayBytes,r.OwnWork,
        r.BorrowedCopyReservation,r.BorrowedArrayReservation,r.BorrowedWorkReservation,r.SharedEdgePairs,r.SharedCornerPairs,r.SharedSampleComparisons,r.NormalBuilds,r.GeometryBuilds,
        r.OriginalNormalParity,r.OriginalTerrainParity,r.SignedZeroHeightWordsAreDistinct,Edge=(int)r.EdgePolicy,Normal=(int)r.NormalPolicy};
    public static object Run(string fixtures,string originalFixture)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));var pool=doc.RootElement.GetProperty("Chunks").EnumerateArray().Select(Make).ToArray();
        var before=pool.Select(p=>p.CopyPositionHeightBits()).ToArray();var output=new List<object>();int checks=0;
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            var ids=c.GetProperty("Ids").EnumerateArray().Select(v=>v.GetInt32()).ToArray();var supplied=ids.Select(i=>pool[i]).ToArray();
            if(c.TryGetProperty("Foreign",out var foreign))
            {
                int at=foreign.GetInt32();var current=supplied[at];var constructor=typeof(TerrainModernHeightPatch).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
                supplied[at]=(TerrainModernHeightPatch)constructor.Invoke(new object[]{pool[(ids[at]+1)%pool.Length].Source,current.NormalField,current.Patch});
            }
            if(c.TryGetProperty("Null",out var nil))supplied[nil.GetInt32()]=null!;
            try
            {
                var saved=(TerrainModernHeightPatch[])supplied.Clone();var result=Admit(supplied,c);
                Need(result.Patches.Count==saved.Length&&result.Patches.Zip(saved).All(v=>ReferenceEquals(v.First,v.Second)),"Exact candidate identity/order.");checks++;
                if(supplied.Length>0){supplied[0]=pool[^1];var copy=result.CopyPatches();copy[0]=pool[^1];}
                Need(result.Patches.Zip(saved).All(v=>ReferenceEquals(v.First,v.Second)),"Detached caller and exported list.");checks++;
                bool readOnly=false;try{((IList<TerrainModernHeightPatch>)result.Patches).Add(pool[0]);}catch(NotSupportedException){readOnly=true;}Need(readOnly,"Read-only retained list.");checks++;
                output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Receipt=Snapshot(result)});
            }
            catch(Exception e)when(e is ArgumentException or InvalidDataException or OperationCanceledException or OverflowException)
            {output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});checks++;}
            Need(pool.Select((p,i)=>p.CopyPositionHeightBits().SequenceEqual(before[i])).All(v=>v),"All supplied receipts unchanged after admission/rejection.");checks++;
        }
        var first=doc.RootElement.GetProperty("Cases")[0];var initial=first.GetProperty("Ids").EnumerateArray().Select(v=>pool[v.GetInt32()]).ToArray();var prior=Admit(initial,first);
        for(int i=0;i<8;i++){Need(Admit(initial,first).Patches.Zip(prior.Patches).All(v=>ReferenceEquals(v.First,v.Second)),"Repeat retained identity.");checks++;}
        Parallel.For(0,8,i=>Need(Admit(initial,first).SharedSampleComparisons==prior.SharedSampleComparisons,"Concurrent admission."));checks+=8;
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();bool failed=false;
        try{_=TerrainModernHeightPatchSet.Build(initial,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.IgnoreSharedNormals,cancellation:cancelled.Token);}catch(OperationCanceledException){failed=true;}
        Need(failed&&prior.Patches.Zip(initial).All(v=>ReferenceEquals(v.First,v.Second)),"Precancellation preserves prior.");checks++;
        var late=doc.RootElement.GetProperty("Cases").EnumerateArray().First(c=>c.TryGetProperty("LateMismatch",out _));failed=false;
        try{_=Admit(late.GetProperty("Ids").EnumerateArray().Select(v=>pool[v.GetInt32()]).ToArray(),late);}catch(InvalidDataException){failed=true;}
        Need(failed&&prior.Patches.Zip(initial).All(v=>ReferenceEquals(v.First,v.Second)),"Late shared mismatch preserves prior.");checks++;
        var capture=CaptureChecks(initial,pool.First(p=>p.Width==32));
        return new{Cases=output,OwnershipChecks=checks,OwnedCapture=capture,Pool=pool.Select(p=>new{X=p.Source.Coordinates.Position.FirstAxisBits,Z=p.Source.Coordinates.Position.SecondAxisBits,Stored=p.CopyPositionHeightBits(),Normals=Bits(p.CopyNormals())}).ToArray(),Original=Original(originalFixture)};
    }
    private static object CaptureChecks(TerrainModernHeightPatch[] valid,TerrainModernHeightPatch tooLarge)
    {
        var mutable=(TerrainModernHeightPatch[])valid.Clone();int accepted=0,rejected=0;using var stopped=new CancellationTokenSource();
        var writer=Task.Run(()=>{while(!stopped.IsCancellationRequested){Volatile.Write(ref mutable[3],valid[0]);Volatile.Write(ref mutable[3],tooLarge);Volatile.Write(ref mutable[3],valid[3]);}});
        try
        {
            for(int iteration=0;iteration<256;iteration++)
            {
                try
                {
                    var set=TerrainModernHeightPatchSet.Build(mutable,TerrainModernPatchEdgePolicy.IndependentSuppliedPatches,TerrainModernPatchNormalPolicy.IgnoreSharedNormals,new(MaximumTotalCells:400,MaximumTotalPatches:16,MaximumSelectedSamples:144));
                    var captured=set.CopyPatches();Need(captured.Sum(p=>p.Count)<=400&&captured.Sum(p=>p.PatchCount)<=16&&captured.Select(p=>p.Source.Coordinates.Request).Distinct().Count()==captured.Length,"Captured caps/unique keys.");
                    Need(captured.All(p=>ReferenceEquals(p.NormalField.Source,p.Source)&&ReferenceEquals(p.Patch.Coordinates,p.Source.Coordinates)),"Captured actual child identities.");
                    for(int i=0;i<captured.Length;i++)for(int j=i+1;j<captured.Length;j++)
                    {
                        var a=captured[i].Source;var b=captured[j].Source;int ae=a.SampleBorder+2*a.CellsPerSide,be=b.SampleBorder+2*b.CellsPerSide;
                        var ax=a.Coordinates.Position;var bx=b.Coordinates.Position;
                        float F(uint bits)=>BitConverter.UInt32BitsToSingle(bits);
                        Need(!(Math.Max(F(ax.FirstAxisBits[a.SampleBorder]),F(bx.FirstAxisBits[b.SampleBorder]))<Math.Min(F(ax.FirstAxisBits[ae]),F(bx.FirstAxisBits[be]))
                            &&Math.Max(F(ax.SecondAxisBits[a.SampleBorder]),F(bx.SecondAxisBits[b.SampleBorder]))<Math.Min(F(ax.SecondAxisBits[ae]),F(bx.SecondAxisBits[be]))),"Captured interiors.");
                    }
                    accepted++;
                }
                catch(InvalidDataException){rejected++;}
            }
        }
        finally{stopped.Cancel();writer.GetAwaiter().GetResult();}
        Need(accepted+rejected==256,"Bounded captured-list churn verification.");return new{Checks=256,Accepted=accepted,Rejected=rejected,AllAcceptedCapturesValid=true,AtomicCallerCaptureClaimed=false};
    }
    private static object Original(string path)
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
        return new{Receipt=Snapshot(set),LayerIndex=slice.LayerIndex,binding.GeneratorSha256,SyntheticWarmPriors=true,OriginalNumericParity=false,OriginalNormalParity=false,HeightsCompared=400,NormalsCompared=400};
    }
}
