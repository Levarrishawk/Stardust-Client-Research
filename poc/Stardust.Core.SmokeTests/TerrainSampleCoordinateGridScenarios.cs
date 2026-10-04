using System.Text.Json;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public static class TerrainSampleCoordinateGridScenarios
{
    public static object Run(string fixtures)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));var cases=new List<object>();
        foreach(var f in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            var request=f.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>();
            var p=f.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
            var limits=f.GetProperty("Limits").Deserialize<TerrainSampleCoordinateLimits>();
            try
            {
                var grid=TerrainSampleCoordinateGrid.Build(request,p[0],p[1],p[2],p[3],limits);
                object Axes(TerrainSampleCoordinateAxes axes)=>new{First=axes.CopyFirstAxisBits(),Second=axes.CopySecondAxisBits(),Pairs=axes.CopyPairs(),
                    axes.FirstStrictlyIncreasing,axes.SecondStrictlyIncreasing,axes.HasStrictlyIncreasingDisplayAxes};
                cases.Add(new{Name=name,Reject="None",grid.Request,grid.Policies,grid.Parameters,Layer=Axes(grid.Layer),Position=Axes(grid.Position)});
            }
            catch(ArgumentOutOfRangeException){cases.Add(new{Name=name,Reject="Options"});}
            catch(InvalidDataException){cases.Add(new{Name=name,Reject="Input"});}
        }
        int checks=0;void Check(bool condition){checks++;if(!condition)throw new InvalidOperationException("Coordinate ownership check.");}
        void Bad<T>(Action action) where T:Exception {try{action();}catch(T){checks++;return;}throw new InvalidOperationException("Expected coordinate rejection.");}
        var mode=TerrainHeightArithmeticPrecision.Binary53;var request0=new TerrainSampleCoordinateRequest(0x41000000,2,1,-2,3,1);
        TerrainSampleCoordinateGrid Build(TerrainSampleCoordinateRequest r,TerrainSampleCoordinateLimits? limits=null)=>TerrainSampleCoordinateGrid.Build(r,mode,mode,mode,mode,limits);
        TerrainSampleCoordinateBatch Batch(TerrainSampleCoordinateRequest[] requests,TerrainSampleCoordinateBatchLimits? limits=null)=>TerrainSampleCoordinateGrid.BuildBatch(requests,mode,mode,mode,mode,batchLimits:limits);
        var old=Build(request0,new(6,36));uint[] first=old.Layer.CopyFirstAxisBits(),second=old.Position.CopySecondAxisBits();
        Check(old.Parameters.Count==6&&old.Parameters.DerivedSpacingBits==0x40800000&&old.Parameters.StepBits==0x40000000&&old.Parameters.FirstOriginBits==0xc1900000&&old.Parameters.SecondOriginBits==0x41b00000);
        var copy=old.Layer.CopyFirstAxisBits();copy[0]=0xdeadbeef;Check(old.Layer.CopyFirstAxisBits().SequenceEqual(first));
        copy=old.Position.CopySecondAxisBits();copy[0]=0xdeadbeef;Check(old.Position.CopySecondAxisBits().SequenceEqual(second));
        var pairs=old.Layer.CopyPairs();pairs[0]=default;Check(old.Layer.GetPair(0,0).FirstBits==first[0]);
        Check(old.Layer.CopyPairs().Select(p=>(p.Column,p.Row)).SequenceEqual(Enumerable.Range(0,36).Select(i=>(i%6,i/6))));
        Bad<NotSupportedException>(()=>((IList<uint>)old.Layer.FirstAxisBits)[0]=0);
        Bad<NotSupportedException>(()=>((IList<uint>)old.Position.SecondAxisBits)[0]=0);
        foreach(var pair in new[]{(-1,0),(0,-1),(6,0),(0,6)})Bad<ArgumentOutOfRangeException>(()=>old.Layer.GetPair(pair.Item1,pair.Item2));
        Bad<InvalidDataException>(()=>Build(request0,new(5,36)));Bad<InvalidDataException>(()=>Build(request0,new(6,35)));
        foreach(var cap in new[]{new TerrainSampleCoordinateLimits(1,36),new(1025,36),new(6,3),new(6,1048577)})Bad<ArgumentOutOfRangeException>(()=>Build(request0,cap));
        TerrainSampleCoordinateRequest[] requests={request0,request0 with{First=1}};var snapshot=(TerrainSampleCoordinateRequest[])requests.Clone();
        var batch=Batch(requests,new(2,72));Check(requests.SequenceEqual(snapshot)&&batch.TotalPairs==72&&batch.Grids.Count==2);
        requests[0]=default;Check(batch.Grids[0].Request==request0);
        Bad<NotSupportedException>(()=>((IList<TerrainSampleCoordinateGrid>)batch.Grids)[0]=old);
        Bad<InvalidDataException>(()=>Batch(snapshot,new(1,72)));Bad<InvalidDataException>(()=>Batch(snapshot,new(2,71)));
        Check(Batch([],new(0,0)).Grids.Count==0);
        foreach(var cap in new[]{new TerrainSampleCoordinateBatchLimits(-1,72),new(4097,72),new(2,-1),new(2,16777217)})Bad<ArgumentOutOfRangeException>(()=>Batch([],cap));
        var late=request0 with{ChunkWidthBits=0x7f000000,Tiles=1,Margin=0,First=3,Second=0,Scale=1};
        Bad<InvalidDataException>(()=>Batch(new[]{request0,late}));Check(old.Layer.CopyFirstAxisBits().SequenceEqual(first)&&batch.Grids[0].Layer.CopyFirstAxisBits().SequenceEqual(first));
        var underflow=Build(request0 with{ChunkWidthBits=1,Tiles=2,Margin=0,First=0,Second=0});
        Check(underflow.Parameters.DerivedSpacingBits==0&&underflow.Parameters.StepBits==0&&!underflow.Layer.HasStrictlyIncreasingDisplayAxes);
        var duplicate=Build(request0 with{ChunkWidthBits=0x3f800000,Tiles=2,Margin=0,First=int.MaxValue,Second=int.MinValue});
        Check(!duplicate.Layer.FirstStrictlyIncreasing&&!duplicate.Position.SecondStrictlyIncreasing);
        var full=Build(request0 with{Tiles=512,Margin=0,First=0,Second=0},new(1024,1048576));
        Check(full.Layer.Count==1024&&full.Position.GetPair(1023,1023).Row==1023);
        Check(full.Layer.CopyPairs().Length==1048576);
        Bad<InvalidDataException>(()=>Build(request0 with{Tiles=513,Margin=0},new(1024,1048576)));
        var negativeMargin=Build(request0 with{Tiles=3,Margin=-2});Check(negativeMargin.Parameters.Count==2);
        Bad<InvalidDataException>(()=>Build(request0 with{Tiles=int.MaxValue,Margin=1}));
        Bad<InvalidDataException>(()=>Build(request0 with{Tiles=1,Margin=int.MaxValue-1}));
        Bad<InvalidDataException>(()=>Build(request0 with{ChunkWidthBits=0x7f000000,Tiles=1,Margin=0,First=1,Second=0,Scale=3}));
        Check(old.Position.CopySecondAxisBits().SequenceEqual(second));
        var largest=Enumerable.Repeat(request0 with{Tiles=1,Margin=0},4096).ToArray();
        Check(Batch(largest,new(4096,16384)).TotalPairs==16384);
        Check(Build(request0 with{Tiles=128,Margin=0}).Parameters.Count==256);
        Bad<InvalidDataException>(()=>Build(request0 with{Tiles=129,Margin=0}));
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
