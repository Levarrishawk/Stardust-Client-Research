using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

public partial class ScenePumpedWorldPreview
{
    private bool heightFilteredTerrain;
    // Synthetic two-layer supplied execution; the original fractal generator is absent.
    private void LoadHeightFilteredTerrain(JsonElement root)
    {
        Check(root.GetProperty("Synthetic").GetBoolean()&&!root.GetProperty("OriginalTerrainParity").GetBoolean()
            &&root.GetProperty("HeightPolicy").GetString()=="supplied_current_height_fhgt_ahcn"
            &&root.GetProperty("FootOffsetBits").GetUInt32()==0,"Explicit height-filtered policy");
        Check(root.GetProperty("Profiles").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(new[]{24,24,24,24})
            &&root.GetProperty("QueryProfiles").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(new[]{53,64}),"Explicit height profiles");
        var input=root.GetProperty("Input");
        Check(input.GetProperty("CombinePrecision").GetInt32()==24&&input.GetProperty("ProductPrecision").GetInt32()==24,"Fixed display combination/product precision");
        byte[] Hex(string name)
        {
            string text=input.GetProperty(name).GetString()??throw new InvalidOperationException("Missing rule bytes");
            Check(text.Length is >0 and <=32768&&text.Length%2==0,"Bounded synthetic rule bytes");return Convert.FromHexString(text);
        }
        var circle=TerrainBoundaryCircleInput.Decode(Hex("CircleHex"));
        var rectangle=TerrainBoundaryRectangleInput.Decode(Hex("RectangleHex"));
        var cache=TerrainBoundaryRectangleInfluence.Cache(rectangle,TerrainHeightArithmeticPrecision.Binary53);
        var firstFilter=TerrainFilterHeightInput.Decode(Hex("FirstFilterHex"));
        var secondFilter=TerrainFilterHeightInput.Decode(Hex("SecondFilterHex"));
        TerrainHeightConstantInput[] Rules(string name)
        {
            var generator=TerrainGeneratorInput.Decode(Hex(name));Check(generator.Rules.Count==3,"Three ordered supplied AHCN rules");
            return generator.Rules.Select(v=>v.HeightConstant??throw new InvalidOperationException("Only supplied AHCN")).ToArray();
        }
        var firstRules=Rules("GeneratorHex");var secondRules=Rules("SecondGeneratorHex");
        string[] names={"walk","run","paused","corrected_paused","resumed","idle","unfocused","focus_resumed","reset"};
        var expectations=root.GetProperty("MotionExpectedBits");Check(expectations.EnumerateObject().Count()==9,"Nine motion expectations");
        var motion=new Dictionary<string,float>();
        foreach(string name in names){float value=BitConverter.UInt32BitsToSingle(expectations.GetProperty(name).GetUInt32());Check(float.IsFinite(value)&&Math.Abs(value)<=1000,"Finite inspection expectation");motion.Add(name,value);}
        Check(root.GetProperty("Chunks").GetArrayLength()==4,"Four complete filtered chunks");
        var grids=new TerrainSampleCoordinateGrid[4];var plans=new TerrainHeightFilteredPlan[400];
        int chunkIndex=0;
        foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
        {
            var r=chunk.GetProperty("Request");var request=new TerrainSampleCoordinateRequest(r.GetProperty("ChunkWidthBits").GetUInt32(),r.GetProperty("Tiles").GetInt32(),r.GetProperty("Margin").GetInt32(),r.GetProperty("First").GetInt32(),r.GetProperty("Second").GetInt32(),r.GetProperty("Scale").GetInt32());
            Check(request==new TerrainSampleCoordinateRequest(0x41000000,2,3,chunkIndex%2-1,chunkIndex/2-1,1)&&chunk.GetProperty("SeamMask").GetInt32()==0,"Fixed chunk order/identity/seams");
            Check(chunk.GetProperty("InitialHeightBits").GetArrayLength()==100,"Complete initial channel");
            var grid=TerrainSampleCoordinateGrid.Build(request,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);grids[chunkIndex]=grid;
            for(int row=0;row<10;row++)for(int column=0;column<10;column++)
            {
                int index=row*10+column;uint initial=chunk.GetProperty("InitialHeightBits")[index].GetUInt32();Check(float.IsFinite(BitConverter.UInt32BitsToSingle(initial)),"Finite initial display height");var xy=grid.Layer.GetPair(column,row);
                var cq=TerrainBoundaryCircleInfluence.Query(circle,xy.FirstBits,xy.SecondBits,TerrainHeightArithmeticPrecision.Binary53,TerrainHeightArithmeticPrecision.Binary64);
                var rq=TerrainBoundaryRectangleInfluence.Query(cache,xy.FirstBits,xy.SecondBits,TerrainHeightArithmeticPrecision.Binary64);
                TerrainLayerInfluenceEntry[] boundaries={new(true,3,TerrainLayerInfluenceQuery.FromCircle(cq)),new(true,2,TerrainLayerInfluenceQuery.FromRectangle(rq))};
                var first=TerrainHeightFilteredLayer.Create(boundaries,new[]{firstFilter},firstRules,input.GetProperty("IncomingBits").GetUInt32(),1,1,input.GetProperty("BoundaryInverted").GetBoolean(),input.GetProperty("FilterInverted").GetBoolean(),TerrainHeightArithmeticPrecision.Binary53,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
                var second=TerrainHeightFilteredLayer.Create(boundaries,new[]{secondFilter},secondRules,input.GetProperty("SecondIncomingBits").GetUInt32(),1,1,input.GetProperty("SecondBoundaryInverted").GetBoolean(),input.GetProperty("SecondFilterInverted").GetBoolean(),TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
                plans[chunkIndex*100+index]=TerrainHeightFilteredPlan.Create(initial,new[]{first,second},2);
            }
            chunkIndex++;
        }
        // Whole batch reservation and every height/geometry candidate precede GPU allocation.
        var batch=TerrainSuppliedHeightFilteredSequence.EvaluateBatch(plans,new(new(MaximumSamples:400,MaximumLayersPerSample:2)));
        var padded=new TerrainCoordinateHeightPatch[4];
        for(int c=0;c<4;c++)
        {
            var results=batch.Results.Skip(c*100).Take(100).ToArray();uint[] heights=results.Select(v=>v.FinalHeightBits).ToArray();
            Check(heights.All(v=>float.IsFinite(BitConverter.UInt32BitsToSingle(v))&&Math.Abs(BitConverter.UInt32BitsToSingle(v))<=1000),"Finite computed display heights");
            padded[c]=TerrainCoordinateHeightPatch.Build(grids[c],heights,Enumerable.Repeat(System.Numerics.Vector3.UnitY,100).ToArray(),Enumerable.Repeat(0xFF0040C0u,100).ToArray());
            GD.Print("height_filtered_chunk="+JsonSerializer.Serialize(new{Chunk=c,HeightBits=heights,FirstHeightBits=results.Select(v=>v.Stages[0].Receipt.FinalHeightBits),FirstQueryHeight=results.Select(v=>v.Stages[0].Bindings[0].Query?.CurrentHeightBits),SecondQueryHeight=results.Select(v=>v.Stages[1].Bindings[0].Query?.CurrentHeightBits),LocalBits=results.Select(v=>v.Stages[0].Receipt.Influence.LocalInfluenceBits),ProductBits=results.Select(v=>v.Stages[0].Receipt.Influence.ProductBits),SecondLocalBits=results.Select(v=>v.Stages[1].Receipt.Influence.LocalInfluenceBits),SecondProductBits=results.Select(v=>v.Stages[1].Receipt.Influence.ProductBits),HeightVisits=results.Select(v=>v.Stages[0].Receipt.Visits.Count),SecondHeightVisits=results.Select(v=>v.Stages[1].Receipt.Visits.Count)}));
        }
        terrainAllocationsBefore=TerrainPatchVisual.Allocations;terrainDisposalsBefore=TerrainPatchVisual.Disposals;suppliedTerrain=true;suppliedLayerMotion=motion;heightFilteredTerrain=true;
        try
        {
            for(int c=0;c<4;c++)foreach(var patch in padded[c].Patches)
            {
                var query=new TerrainPatchSurfaceQuery(patch.Geometry);var visual=new TerrainPatchVisual(patch.Geometry,this);suppliedTerrainVisuals.Add(visual);suppliedTerrainQueries.Add(query);visual.VerifyReadback();
                GD.Print("supplied_terrain_patch="+JsonSerializer.Serialize(new{Chunk=c,patch.Cell.Column,patch.Cell.Row,Positions=patch.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),Indices=patch.Geometry.CopyIndices(),query.TriangleCount}));
            }
            Check(suppliedTerrainVisuals.Count==16,"Complete filtered patches");
            GD.Print("supplied_terrain_ready="+JsonSerializer.Serialize(new{Synthetic=true,Chunks=4,FullSamples=400,Patches=16,QueryTriangles=suppliedTerrainQueries.Sum(q=>q.TriangleCount),OriginalTerrainParity=false}));
            GD.Print("height_filtered_ready="+JsonSerializer.Serialize(new{Layers=2,Filters=2,Rules=6,Samples=400,ActualExecutor=true,QueryProfiles=new[]{53,64},OriginalTerrainParity=false}));
        }
        catch{CloseSuppliedTerrain();throw;}
    }
}
