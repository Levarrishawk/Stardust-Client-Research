using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public static class TerrainModernHeightNormalsScenarios
{
    private static void Need(bool okay,string why){if(!okay)throw new InvalidOperationException(why);}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    private static long L(JsonElement c,string key,long fallback)=>c.TryGetProperty(key,out var v)?v.GetInt64():fallback;
    private static uint[] Words(JsonElement c,string key)=>c.GetProperty(key).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
    private static uint[][] Bits(IEnumerable<Vector3> normals)=>normals.Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}).ToArray();
    private static TerrainCoordinateHeightPatch Source(JsonElement c)
    {
        var request=c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>();
        var p=c.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
        var grid=TerrainSampleCoordinateGrid.Build(request,p[0],p[1],p[2],p[3]);var h=Words(c,"Heights");
        return TerrainCoordinateHeightPatch.Build(grid,h,Enumerable.Repeat(Vector3.UnitY,h.Length).ToArray(),Enumerable.Repeat(0xff4080c0u,h.Length).ToArray(),I(c,"Mask",0),
            limits:new(MaximumGridSamples:1048576,MaximumPatches:1024,MaximumSelectedSamples:9216));
    }
    private static TerrainModernHeightNormals Build(TerrainCoordinateHeightPatch source,JsonElement c)
    {
        using var cancel=new CancellationTokenSource();if(c.TryGetProperty("Cancel",out var v)&&v.GetBoolean())cancel.Cancel();
        return TerrainModernHeightNormals.Build(source,(TerrainModernNormalPolicy)I(c,"Policy",0),
            new(I(c,"MaximumAxisSamples",32),I(c,"MaximumCells",1024),L(c,"MaximumCopyBytes",65536),L(c,"MaximumArrayBytes",131072),L(c,"MaximumWork",262144)),cancel.Token);
    }
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var outputs=new List<object>();int ownership=0,seam=0;
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            try
            {
                var source=Source(c);var field=Build(source,c);
                Need(ReferenceEquals(source,field.Source)&&field.Policy==TerrainModernNormalPolicy.ModernBinary64CentralDifference&&!field.OriginalNormalParity,"Actual source retained and modern policy.");
                Need(field.CopyPositionHeightBits().SequenceEqual(source.CopyPositionHeightBits())&&field.CopyPositionXBits().SequenceEqual(source.Coordinates.Position.FirstAxisBits),"Actual stored height/position axes.");
                var before=Bits(field.Normals);var n=field.CopyNormals();n[0]=default;var h=field.CopyPositionHeightBits();h[0]=0x7fc12345;
                var x=field.CopyPositionXBits();x[0]=0x7fc12345;
                Need(Bits(field.Normals).SelectMany(a=>a).SequenceEqual(before.SelectMany(a=>a))&&field.CopyPositionHeightBits().SequenceEqual(source.CopyPositionHeightBits()),"Owned exports.");ownership++;
                bool ro=false;try{((IList<Vector3>)field.Normals)[0]=default;}catch(NotSupportedException){ro=true;}Need(ro,"Read-only normal collection.");ownership++;
                foreach(var patch in source.Patches)
                {
                    var ids=patch.CopySourceSampleIndices();
                    foreach(int index in ids)Need(field.Normals[index]==field.CopyNormals()[index],"Active padded source-index mapping.");
                    seam+=ids.Length;
                }
                outputs.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",field.Width,field.Count,Policy=field.Policy.ToString(),field.OriginalNormalParity,
                    X=field.CopyPositionXBits(),Z=field.CopyPositionZBits(),StoredHeights=field.CopyPositionHeightBits(),Normals=before,
                    field.LogicalCopyBytes,field.LogicalArrayBytes,field.LogicalWork,SourceIndices=source.Patches.Select(p=>p.CopySourceSampleIndices()).ToArray()});
            }
            catch(Exception e) when(e is InvalidDataException or ArgumentException or OperationCanceledException or OverflowException)
            {outputs.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});}
        }
        var first=doc.RootElement.GetProperty("Cases")[0];var priorSource=Source(first);var prior=Build(priorSource,first);var saved=Bits(prior.Normals).SelectMany(v=>v).ToArray();
        for(int i=0;i<8;i++){Need(Bits(Build(priorSource,first).Normals).SelectMany(v=>v).SequenceEqual(saved),"Repeat deterministic normals.");ownership++;}
        Parallel.For(0,8,i=>Need(Bits(Build(priorSource,first).Normals).SelectMany(v=>v).SequenceEqual(saved),"Independent cross-thread normals."));ownership+=8;
        using(var cancelled=new CancellationTokenSource())
        {
            cancelled.Cancel();bool rejected=false;try{_=TerrainModernHeightNormals.Build(priorSource,TerrainModernNormalPolicy.ModernBinary64CentralDifference,cancellation:cancelled.Token);}catch(OperationCanceledException){rejected=true;}
            Need(rejected&&Bits(prior.Normals).SelectMany(v=>v).SequenceEqual(saved),"Cancellation preserves prior.");ownership++;
        }
        var bad=Words(first,"Heights");bad[^1]=0x7fc12345;bool failed=false;
        try{_=TerrainCoordinateHeightPatch.Build(priorSource.Coordinates,bad,Enumerable.Repeat(Vector3.UnitY,bad.Length).ToArray(),new uint[bad.Length]);}catch(InvalidDataException){failed=true;}
        Need(failed&&Bits(prior.Normals).SelectMany(v=>v).SequenceEqual(saved),"Late source-height rejection preserves prior.");ownership++;
        var adjacent=doc.RootElement.GetProperty("Cases").EnumerateArray().Where(c=>c.TryGetProperty("SeamPair",out _)).ToArray();
        Need(adjacent.Length==2,"Adjacent padded seam pair.");var left=Build(Source(adjacent[0]),adjacent[0]);var right=Build(Source(adjacent[1]),adjacent[1]);
        int last=left.Source.SampleBorder+2*left.Source.CellsPerSide,begin=right.Source.SampleBorder;
        for(int row=begin;row<=last;row++)
        {
            Need(left.CopyPositionXBits()[last]==right.CopyPositionXBits()[begin],"Shared active seam coordinate.");
            Need(Vector3.Distance(left.Normals[row*left.Width+last],right.Normals[row*right.Width+begin])<3e-6f,"Padded stencil seam normal.");seam++;
        }
        return new{Cases=outputs,OwnershipChecks=ownership,ActivePaddedSeamChecks=seam};
    }
}
