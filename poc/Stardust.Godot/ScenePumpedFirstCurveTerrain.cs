using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

public partial class ScenePumpedWorldPreview
{
    private bool firstCurveTerrain;
    private float firstCurveCameraHeight;
    private void ConfigureFirstCurveTerrainCamera(Camera3D camera)
    {
        if(!firstCurveTerrain)return;
        camera.Projection=Camera3D.ProjectionType.Orthogonal;camera.Size=22;
        camera.Position=new Vector3(14,firstCurveCameraHeight+18,-20);
        camera.LookAt(new Vector3(0,firstCurveCameraHeight+4,0));
    }
    private void LoadFirstCurveTerrain(JsonElement root)
    {
        const string policy="owned_layer_first_curve_modern_approximation";
        Check(root.GetProperty("Version").GetInt32()==8&&root.GetProperty("HeightPolicy").GetString()==policy
            &&root.GetProperty("Synthetic").GetBoolean()&&root.GetProperty("OriginalLayerUsed").GetBoolean()
            &&!root.GetProperty("OriginalNumericParity").GetBoolean()&&!root.GetProperty("OriginalTerrainParity").GetBoolean()
            &&root.GetProperty("FootOffsetBits").GetUInt32()==0,"Explicit original-layer modern approximation");
        Check(root.GetProperty("Profiles").EnumerateArray().Select(x=>x.GetInt32()).SequenceEqual(new[]{24,24,24,24})
            &&root.GetProperty("Controls").EnumerateArray().Select(x=>x.GetInt32()).SequenceEqual(new[]{53,64,64,64})
            &&root.GetProperty("FilterControls").EnumerateArray().Select(x=>x.GetInt32()).SequenceEqual(new[]{53,64,64,64,64,64})
            &&root.GetProperty("BoundaryControls").EnumerateArray().Select(x=>x.GetInt32()).SequenceEqual(new[]{64,64,64}),"Explicit profiles");
        string hex=root.GetProperty("GeneratorHex").GetString()??throw new InvalidOperationException("Missing generator");
        Check(hex.Length==279216,"Pinned bounded original generator bytes");
        var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(hex));
        var binding=TerrainGeneratorFractalFamilies.Bind(generator);
        Check(binding.GeneratorSha256=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32"
            &&binding.GroupSha256=="A221167452D8D67719BD72A3DB30CC10B7B652881B935D3571B9D55328B444A1","Pinned generator/group");
        var slice=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);
        Check(slice.LayerSha256=="E0F7CC6190A9A25BCBE51D6FF4ED304D357667B37521DBF7DF5771EBD9EBD067"
            &&slice.Height.RuleSha256=="B85D90EC684322F07798476A55A665DCDE050D5B355D86CB3AD5ACB41883F674"
            &&slice.Height.FamilyId==4&&!slice.HasSupportedInputs,"Exact evidence slice; strict operational rejection retained");
        string[] names={"walk","run","paused","corrected_paused","resumed","idle","unfocused","focus_resumed","reset"};
        var motion=new Dictionary<string,float>();var values=root.GetProperty("MotionExpectedBits");
        Check(values.EnumerateObject().Count()==9,"Nine independent grounding observations");
        foreach(string name in names){float value=BitConverter.UInt32BitsToSingle(values.GetProperty(name).GetUInt32());Check(float.IsFinite(value)&&Math.Abs(value)<=1000000,"Finite bounded observation");motion.Add(name,value);}
        Check(root.GetProperty("Chunks").GetArrayLength()==4,"Four bounded chunks");
        var candidates=new TerrainCoordinateHeightPatch[4];int c=0;
        int allocations=TerrainPatchVisual.Allocations,disposals=TerrainPatchVisual.Disposals;
        try {
            foreach(var chunk in root.GetProperty("Chunks").EnumerateArray()){
                var r=chunk.GetProperty("Request");var request=new TerrainSampleCoordinateRequest(r.GetProperty("ChunkWidthBits").GetUInt32(),r.GetProperty("Tiles").GetInt32(),r.GetProperty("Margin").GetInt32(),r.GetProperty("First").GetInt32(),r.GetProperty("Second").GetInt32(),r.GetProperty("Scale").GetInt32());
                Check(request==new TerrainSampleCoordinateRequest(0x41000000,2,3,c%2-1,c/2-1,1)&&chunk.GetProperty("SeamMask").GetInt32()==0,"Controlled chunk identity");
                var grid=TerrainSampleCoordinateGrid.Build(request,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
                uint[] prior=chunk.GetProperty("PriorHeightBits").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();
                uint[] incoming=chunk.GetProperty("IncomingWeightBits").EnumerateArray().Select(x=>x.GetUInt32()).ToArray();
                Check(prior.Length==100&&incoming.Length==100,"Complete bounded channels");
                var boundary=TerrainHeightLayerBoundaryAdmission.Evaluate(slice,grid,new(TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
                var filtered=TerrainHeightLayerFilteredInfluence.Evaluate(boundary,slice,grid,incoming,new(53,64,64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
                var requests=Enumerable.Range(0,100).Select(i=>{var p=grid.Layer.GetPair(i%10,i/10);return new TerrainConditionalFractalSampler.Request(p.Column,p.Row,p.FirstBits,p.SecondBits);}).ToArray();
                var batch=TerrainFirstCurveFractalBatch.Build(slice.Height.Family.Raw.CopyEncodedBytes(),10,10,requests,new(53,64,64),TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);
                var field=TerrainHeightLayerFirstCurveConsumer.Evaluate(filtered,slice,grid,batch,prior,TerrainHeightArithmeticPrecision.Binary64,TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);
                uint[] heights=field.CopyHeightBits();
                Check(heights.SequenceEqual(root.GetProperty("Expected")[c].GetProperty("HeightBits").EnumerateArray().Select(x=>x.GetUInt32())),"Independently verified native height observations");
                candidates[c]=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(System.Numerics.Vector3.UnitY,100).ToArray(),Enumerable.Repeat(0xFF20A060u,100).ToArray(),0,1);
                GD.Print("first_curve_height_chunk="+JsonSerializer.Serialize(new{Chunk=c,HeightBits=heights,ProductBits=filtered.CopyProductBits(),FilterCounts=filtered.Counts,HeightCounts=field.Counts,
                    batch.ComputedMisses,batch.KernelOctaves,batch.SamplerFactories,batch.KernelBatches,batch.SamplerDisposals,OriginalNumericParity=false}));
                c++;
            }
        } catch {
            GD.Print("first_curve_height_preflight_failure="+JsonSerializer.Serialize(new{CompletedChunks=c,AllocatedTerrain=TerrainPatchVisual.Allocations-allocations,DisposedTerrain=TerrainPatchVisual.Disposals-disposals,OriginalNumericParity=false}));
            throw;
        }
        // All four native fields and mesh candidates pass before any terrain GPU allocation.
        terrainAllocationsBefore=TerrainPatchVisual.Allocations;terrainDisposalsBefore=TerrainPatchVisual.Disposals;
        suppliedTerrain=true;suppliedLayerMotion=motion;firstCurveTerrain=true;firstCurveCameraHeight=motion["reset"];
        try {
            for(c=0;c<4;c++)foreach(var patch in candidates[c].Patches){
                var query=new TerrainPatchSurfaceQuery(patch.Geometry);var visual=new TerrainPatchVisual(patch.Geometry,this);
                suppliedTerrainVisuals.Add(visual);suppliedTerrainQueries.Add(query);visual.VerifyReadback();
                GD.Print("supplied_terrain_patch="+JsonSerializer.Serialize(new{Chunk=c,patch.Cell.Column,patch.Cell.Row,
                    Positions=patch.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),Indices=patch.Geometry.CopyIndices(),query.TriangleCount}));
            }
            Check(suppliedTerrainVisuals.Count==16,"Complete approximate original-layer display");
            GD.Print("first_curve_height_ready="+JsonSerializer.Serialize(new{Cells=400,slice.LayerIndex,slice.LayerSha256,HeightRuleSha256=slice.Height.RuleSha256,FilterRuleSha256=slice.Filter.RuleSha256,Policy=policy,OriginalNumericParity=false,OriginalTerrainParity=false}));
            GD.Print("supplied_terrain_ready="+JsonSerializer.Serialize(new{Synthetic=true,Chunks=4,FullSamples=400,Patches=16,QueryTriangles=128,OriginalTerrainParity=false}));
        } catch {CloseSuppliedTerrain();throw;}
    }
}

