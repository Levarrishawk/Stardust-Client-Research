using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

public partial class ScenePumpedWorldPreview
{
    private Dictionary<string,float>? suppliedLayerMotion;

    // Explicit supplied queries/rules and channels; no original generator/filter claim.
    private void LoadCalculatedLayerTerrain(JsonElement root)
    {
        Check(root.GetProperty("Synthetic").GetBoolean()
            && root.GetProperty("HeightPolicy").GetString()=="supplied_layer_ahcn"
            && root.GetProperty("FootOffsetBits").GetUInt32()==0,"Supplied layer fixture policy");
        Check(root.GetProperty("Profiles").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(new[]{24,24,24,24}),"Explicit coordinate profiles");
        var input=root.GetProperty("Input");
        byte[] Hex(string key,int maximum)
        {
            string value=input.GetProperty(key).GetString() ?? throw new InvalidOperationException("Hex input");
            Check(value.Length is >0 && value.Length<=2*maximum && value.Length%2==0,"Bounded synthetic rule bytes");
            return Convert.FromHexString(value);
        }
        var circle=TerrainBoundaryCircleInput.Decode(Hex("CircleHex",1024));
        var rectangle=TerrainBoundaryRectangleInput.Decode(Hex("RectangleHex",1024));
        var cache=TerrainBoundaryRectangleInfluence.Cache(rectangle,TerrainHeightArithmeticPrecision.Binary53);
        var generator=TerrainGeneratorInput.Decode(Hex("GeneratorHex",16384));
        Check(generator.Rules.Count==3,"Three supplied typed affectors");
        var rules=generator.Rules.Select(v=>v.HeightConstant ?? throw new InvalidOperationException("Only AHCN")).ToArray();
        Check(input.GetProperty("CombinePrecision").GetInt32()==24 && input.GetProperty("ProductPrecision").GetInt32()==24
            && !input.GetProperty("BoundaryInverted").GetBoolean() && !input.GetProperty("FilterInverted").GetBoolean(),"Fixed supplied layer phase policy");
        uint incoming=input.GetProperty("IncomingBits").GetUInt32(),filter=input.GetProperty("FilterBits").GetUInt32();
        var expectations=root.GetProperty("MotionExpectedBits");
        string[] names={"walk","run","paused","corrected_paused","resumed","idle","unfocused","focus_resumed","reset"};
        Check(expectations.EnumerateObject().Count()==names.Length,"Nine bounded test pose expectations");
        var motion=new Dictionary<string,float>();
        foreach(string name in names)
        {
            float value=BitConverter.UInt32BitsToSingle(expectations.GetProperty(name).GetUInt32());
            Check(float.IsFinite(value) && Math.Abs(value)<=1000,"Finite inspection expectation");motion.Add(name,value);
        }
        Check(root.GetProperty("Chunks").GetArrayLength()==4,"Four layer chunks");
        terrainAllocationsBefore=TerrainPatchVisual.Allocations;terrainDisposalsBefore=TerrainPatchVisual.Disposals;
        suppliedTerrain=true;suppliedLayerMotion=motion;
        try
        {
            int chunkIndex=0;
            foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
            {
                var r=chunk.GetProperty("Request");
                var request=new TerrainSampleCoordinateRequest(r.GetProperty("ChunkWidthBits").GetUInt32(),r.GetProperty("Tiles").GetInt32(),
                    r.GetProperty("Margin").GetInt32(),r.GetProperty("First").GetInt32(),r.GetProperty("Second").GetInt32(),r.GetProperty("Scale").GetInt32());
                Check(request==new TerrainSampleCoordinateRequest(0x41000000,2,3,chunkIndex%2-1,chunkIndex/2-1,1),"Layer chunk identity/order");
                var grid=TerrainSampleCoordinateGrid.Build(request,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,
                    TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
                Check(chunk.GetProperty("InitialHeightBits").GetArrayLength()==100,"Complete initial height channel");
                uint[] initial=chunk.GetProperty("InitialHeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
                var heights=new uint[100];var locals=new uint[100];var products=new uint[100];var counts=new int[100];
                for(int row=0;row<10;row++)for(int column=0;column<10;column++)
                {
                    int index=row*10+column;var xy=grid.Layer.GetPair(column,row);
                    var cq=TerrainBoundaryCircleInfluence.Query(circle,xy.FirstBits,xy.SecondBits,TerrainHeightArithmeticPrecision.Binary53,TerrainHeightArithmeticPrecision.Binary64);
                    var rq=TerrainBoundaryRectangleInfluence.Query(cache,xy.FirstBits,xy.SecondBits,TerrainHeightArithmeticPrecision.Binary64);
                    TerrainLayerInfluenceEntry[] boundaries={new(true,3,TerrainLayerInfluenceQuery.FromCircle(cq)),new(true,2,TerrainLayerInfluenceQuery.FromRectangle(rq))};
                    TerrainLayerInfluenceEntry[] filters={new(true,1,TerrainLayerInfluenceQuery.FromRawBits(filter,TerrainHeightArithmeticPrecision.Binary64))};
                    var layer=TerrainLayerInfluenceSample.Create(incoming,boundaries,filters,1,1,false,false,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
                    var result=TerrainSuppliedLayerHeight.Evaluate(TerrainSuppliedLayerHeightSample.Create(layer,initial[index],rules,TerrainHeightArithmeticPrecision.Binary24));
                    Check(result.FinalHeightIsFinite,"Display requires finite height");heights[index]=result.FinalHeightBits;
                    locals[index]=result.Influence.LocalInfluenceBits;products[index]=result.Influence.ProductBits;counts[index]=result.Visits.Count;
                }
                GD.Print("calculated_layer_chunk="+JsonSerializer.Serialize(new{Chunk=chunkIndex,HeightBits=heights,LocalBits=locals,ProductBits=products,HeightVisits=counts}));
                var padded=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(System.Numerics.Vector3.UnitY,100).ToArray(),Enumerable.Repeat(0xFF0040C0u,100).ToArray());
                foreach(var patch in padded.Patches)
                {
                    var query=new TerrainPatchSurfaceQuery(patch.Geometry);var visual=new TerrainPatchVisual(patch.Geometry,this);
                    suppliedTerrainVisuals.Add(visual);suppliedTerrainQueries.Add(query);visual.VerifyReadback();
                    GD.Print("supplied_terrain_patch="+JsonSerializer.Serialize(new{Chunk=chunkIndex,patch.Cell.Column,patch.Cell.Row,
                        Positions=patch.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),
                        Indices=patch.Geometry.CopyIndices(),query.TriangleCount}));
                }
                chunkIndex++;
            }
            Check(suppliedTerrainVisuals.Count==16,"Complete calculated patches");
            GD.Print("supplied_terrain_ready="+JsonSerializer.Serialize(new{Synthetic=true,Chunks=4,FullSamples=400,Patches=16,QueryTriangles=suppliedTerrainQueries.Sum(q=>q.TriangleCount),OriginalTerrainParity=false}));
            GD.Print("calculated_layer_ready="+JsonSerializer.Serialize(new{Rules=3,Samples=400,ActualExecutor=true,SyntheticQueries=true,OriginalTerrainParity=false}));
        }
        catch{CloseSuppliedTerrain();throw;}
    }
}
