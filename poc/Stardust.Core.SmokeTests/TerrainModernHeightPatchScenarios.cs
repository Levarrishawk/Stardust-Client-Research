using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public static class TerrainModernHeightPatchScenarios
{
    private static void Need(bool okay,string why){if(!okay)throw new InvalidOperationException(why);}
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var v)?v.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var v)?v.GetInt64():d;
    private static uint[] Words(JsonElement c,string k)=>c.GetProperty(k).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
    private static uint[][] Bits(IEnumerable<Vector3> v)=>v.Select(n=>new[]{BitConverter.SingleToUInt32Bits(n.X),BitConverter.SingleToUInt32Bits(n.Y),BitConverter.SingleToUInt32Bits(n.Z)}).ToArray();
    private static TerrainCoordinateHeightPatch Source(JsonElement c)
    {
        var p=c.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
        var grid=TerrainSampleCoordinateGrid.Build(c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),p[0],p[1],p[2],p[3]);
        if(c.TryGetProperty("VerticalOriginBits",out var origin)&&origin.GetUInt32()!=0)
        {
            // Test-only supplied metadata fixture: public producer emits zero vertical origin.
            // Reuse actual generated axes/policies and actual frozen source builder, not injected normals.
            var ctor=typeof(TerrainSampleCoordinateGrid).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single();
            grid=(TerrainSampleCoordinateGrid)ctor.Invoke(new object[]{grid.Request,grid.Policies,grid.Parameters with{VerticalOriginBits=origin.GetUInt32()},grid.Layer,grid.Position});
        }
        var h=Words(c,"Heights");return TerrainCoordinateHeightPatch.Build(grid,h,Enumerable.Repeat(Vector3.UnitY,h.Length).ToArray(),Words(c,"Colors"),I(c,"Mask",0),
            BitConverter.UInt32BitsToSingle(c.GetProperty("ScaleBits").GetUInt32()),new(1048576,1024,9216));
    }
    private static TerrainModernHeightPatch Build(TerrainCoordinateHeightPatch source,JsonElement c)
    {
        using var token=new CancellationTokenSource();if(c.TryGetProperty("Cancel",out var v)&&v.GetBoolean())token.Cancel();
        var normal=c.TryGetProperty("NormalLimits",out var nl)?nl.Deserialize<TerrainModernHeightNormalLimits>():null;
        var final=c.TryGetProperty("PatchLimits",out var pl)?pl.Deserialize<TerrainCoordinateHeightPatchLimits>():null;
        return TerrainModernHeightPatch.Build(source,(TerrainModernNormalPolicy)I(c,"Policy",0),new(I(c,"MaximumAxisSamples",32),I(c,"MaximumCells",1024),
            I(c,"MaximumPatches",64),I(c,"MaximumSelectedSamples",576),L(c,"MaximumCopyBytes",1048576),L(c,"MaximumArrayBytes",2097152),L(c,"MaximumWork",4194304),normal,final),token.Token);
    }
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var output=new List<object>();int ownership=0,originWitnesses=0,seam=0;
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            try
            {
                var source=Source(c);var before=source.CopyPositionHeightBits();var receipt=Build(source,c);var final=receipt.Patch;var normals=receipt.NormalField.CopyNormals();
                Need(ReferenceEquals(receipt.Source,source)&&ReferenceEquals(receipt.NormalField.Source,source)&&ReferenceEquals(final.Coordinates,source.Coordinates),"Actual three-stage identities.");
                Need(final.CopyInputHeightBits().SequenceEqual(source.CopyInputHeightBits())&&final.CopyPositionHeightBits().SequenceEqual(before)
                    &&final.CopyArgbColors().SequenceEqual(source.CopyArgbColors()),"Raw/stored/full color preservation.");ownership++;
                var nc=receipt.CopyNormals();nc[0]=default;var hc=receipt.CopyInputHeightBits();hc[0]=0x7fc12345;var cc=receipt.CopyArgbColors();cc[0]^=1;var sc=receipt.CopyPositionHeightBits();sc[0]^=1;
                Need(final.CopyInputHeightBits().SequenceEqual(source.CopyInputHeightBits())&&source.CopyPositionHeightBits().SequenceEqual(before)
                    &&final.CopyPositionHeightBits().SequenceEqual(before)&&final.CopyArgbColors().SequenceEqual(source.CopyArgbColors())
                    &&Bits(receipt.CopyNormals()).SelectMany(a=>a).SequenceEqual(Bits(normals).SelectMany(a=>a)),"Detached exports/source remains unchanged.");ownership++;
                var patches=final.Patches.Select((p,i)=>
                {
                    var original=source.Patches[i];var indices=p.CopySourceSampleIndices();var geometry=p.Geometry;
                    Need(indices.SequenceEqual(original.CopySourceSampleIndices())&&geometry.CopyPositions().SequenceEqual(original.Geometry.CopyPositions())
                        &&geometry.CopyIndices().SequenceEqual(original.Geometry.CopyIndices())&&geometry.CopyBaseUvs().SequenceEqual(original.Geometry.CopyBaseUvs()),"Exact geometry/index/UV/seam preservation.");
                    Need(Bits(geometry.CopyNormals()).SelectMany(a=>a).SequenceEqual(Bits(indices.Select(id=>normals[id])).SelectMany(a=>a)),"Actual selected derived normals.");
                    return new{Source=indices,Pattern=geometry.PatternIndex,Positions=Bits(geometry.CopyPositions()),Normals=Bits(geometry.CopyNormals()),
                        Colors=geometry.CopyArgbColors(),Rgba=geometry.CopyRgbaColors().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z),BitConverter.SingleToUInt32Bits(v.W)}).ToArray(),
                        Uvs=geometry.CopyBaseUvs().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y)}).ToArray(),Indices=geometry.CopyIndices()};
                }).ToArray();
                if(BitConverter.UInt32BitsToSingle(source.Coordinates.Parameters.VerticalOriginBits)!=0)
                {
                    var doubled=TerrainCoordinateHeightPatch.Build(source.Coordinates,source.CopyPositionHeightBits(),normals,source.CopyArgbColors(),source.SeamMask,source.UvScale,new(1048576,1024,9216));
                    Need(!doubled.CopyPositionHeightBits().SequenceEqual(before),"Strong stored-as-input double-origin witness.");originWitnesses++;
                }
                output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",receipt.Width,receipt.Count,receipt.PatchCount,receipt.SelectedSamples,
                    Raw=receipt.CopyInputHeightBits(),Stored=receipt.CopyPositionHeightBits(),Colors=receipt.CopyArgbColors(),Normals=Bits(normals),Patches=patches,
                    receipt.OwnCopyBytes,receipt.OwnArrayBytes,receipt.OwnWork,receipt.NormalCopyBytes,receipt.NormalArrayBytes,receipt.NormalWork,
                    receipt.FinalReservedCopyBytes,receipt.FinalReservedArrayBytes,receipt.FinalReservedWork,receipt.AggregateCopyBytes,receipt.AggregateArrayBytes,receipt.AggregateWork});
            }
            catch(Exception e) when(e is InvalidDataException or ArgumentException or OperationCanceledException or OverflowException)
            {output.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});}
        }
        var first=doc.RootElement.GetProperty("Cases")[0];var source0=Source(first);var prior=Build(source0,first);var saved=Bits(prior.CopyNormals()).SelectMany(a=>a).ToArray();
        for(int i=0;i<8;i++){Need(Bits(Build(source0,first).CopyNormals()).SelectMany(a=>a).SequenceEqual(saved),"Repeat composition.");ownership++;}
        Parallel.For(0,8,i=>Need(Bits(Build(source0,first).CopyNormals()).SelectMany(a=>a).SequenceEqual(saved),"Independent threads."));ownership+=8;
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();bool reject=false;
        try{_=TerrainModernHeightPatch.Build(source0,TerrainModernNormalPolicy.ModernBinary64CentralDifference,cancellation:cancelled.Token);}catch(OperationCanceledException){reject=true;}
        Need(reject&&Bits(prior.CopyNormals()).SelectMany(a=>a).SequenceEqual(saved),"Cancelled candidate preserves prior.");ownership++;
        var bad=source0.CopyInputHeightBits();bad[^1]=0x7fc12345;reject=false;
        try{_=TerrainCoordinateHeightPatch.Build(source0.Coordinates,bad,Enumerable.Repeat(Vector3.UnitY,bad.Length).ToArray(),source0.CopyArgbColors());}catch(InvalidDataException){reject=true;}
        Need(reject&&Bits(prior.CopyNormals()).SelectMany(a=>a).SequenceEqual(saved),"Late source rejection preserves prior.");ownership++;
        var adjacent=doc.RootElement.GetProperty("Cases").EnumerateArray().Where(c=>c.TryGetProperty("SeamPair",out _)).ToArray();
        Need(adjacent.Length==2,"Adjacent padded seam pair.");var left=Build(Source(adjacent[0]),adjacent[0]);var right=Build(Source(adjacent[1]),adjacent[1]);
        int last=left.Source.SampleBorder+2*left.Source.CellsPerSide,begin=right.Source.SampleBorder;
        var ln=left.CopyNormals();var rn=right.CopyNormals();
        for(int row=begin;row<=last;row++)
        {
            Need(left.Source.Coordinates.Position.FirstAxisBits[last]==right.Source.Coordinates.Position.FirstAxisBits[begin],"Shared active seam coordinate.");
            Need(Vector3.Distance(ln[row*left.Width+last],rn[row*right.Width+begin])<3e-6f,"Padded stencil seam normal.");seam++;
        }
        return new{Cases=output,OwnershipChecks=ownership,DoubleOriginWitnesses=originWitnesses,ActivePaddedSeamChecks=seam};
    }
}
