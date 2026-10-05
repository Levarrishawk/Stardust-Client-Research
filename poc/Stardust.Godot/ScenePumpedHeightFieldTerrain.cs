using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

public partial class ScenePumpedWorldPreview
{
    // Version6 composes two selected original rules under explicit synthetic field controls.
    private void LoadHeightFieldTerrain(JsonElement root)
    {
        bool originalRule=true;
        Check(root.GetProperty("Version").GetInt32()==6,"Explicit field fixture version");
        Check(root.GetProperty("Synthetic").GetBoolean()&&!root.GetProperty("OriginalTerrainParity").GetBoolean()
            &&root.GetProperty("HeightPolicy").GetString()==(originalRule?"selected_filter_per_cell_synthetic_composition":"original_family_synthetic_ahfr_fresh_patch")
            &&root.GetProperty("FootOffsetBits").GetUInt32()==0,"Explicit family-height policy");
        Check(root.GetProperty("Profiles").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(new[]{24,24,24,24})
            &&root.GetProperty("Controls").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(new[]{53,64,64,64}),"Explicit coordinate/noise profiles");
        byte[] Hex(string name,int maximum)
        {
            string value=root.GetProperty(name).GetString()??throw new InvalidOperationException("Missing encoded input");
            Check(value.Length is >0&&value.Length<=2*maximum&&value.Length%2==0,"Bounded encoded input");return Convert.FromHexString(value);
        }
        var generator=TerrainGeneratorInput.Decode(Hex("GeneratorHex",262144));
        var binding=TerrainGeneratorFractalFamilies.Bind(generator);
        Check(binding.GeneratorBytes==139608&&binding.GroupBytes==5633&&binding.GroupOffset==6602&&binding.Families.Entries.Count==43
            &&binding.GeneratorSha256=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32"
            &&binding.GroupSha256=="A221167452D8D67719BD72A3DB30CC10B7B652881B935D3571B9D55328B444A1","Pinned original generator/group");
        Check(binding.GeneratorSha256==root.GetProperty("GeneratorSha256").GetString()
            &&binding.GroupSha256==root.GetProperty("GroupSha256").GetString(),"Fixture provenance agrees");
        byte[] rule=Hex("RuleHex",1024);int familyId=1;
        TerrainGeneratorHeightFractalRule? selected=null;
        if(originalRule)
        {
            Check(root.GetProperty("OriginalRuleUsed").GetBoolean(),"Explicit original rule provenance");
            var expected=root.GetProperty("SelectedRule");
            selected=TerrainGeneratorHeightFractalRule.Select(generator,binding,expected.GetProperty("RuleIndex").GetInt32(),TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);
            Check(selected.RuleIndex==0&&selected.RuleOffset==12346&&selected.RuleBytes==114
                &&selected.RuleSha256=="51735AECA9C172F0BD1E8194ACBCECD8CEF03041075CF307822E275D5E936B1E"
                &&selected.RuleOffset==expected.GetProperty("Offset").GetInt32()
                &&selected.RuleBytes==expected.GetProperty("EncodedBytes").GetInt32()
                &&selected.RuleSha256==expected.GetProperty("Sha256").GetString()
                &&selected.LayerIndex==expected.GetProperty("LayerIndex").GetInt32()
                &&selected.ChildOrdinal==expected.GetProperty("ChildOrdinal").GetInt32()
                &&selected.FamilyId==expected.GetProperty("FamilyId").GetInt32()
                &&selected.CopyEncodedBytes().AsSpan().SequenceEqual(rule),"Exact selected original rule/family");
            familyId=selected.FamilyId;
        }
        var filterExpected=root.GetProperty("SelectedFilterRule");
        var filter=TerrainGeneratorFractalFilterRule.Select(generator,binding,filterExpected.GetProperty("RuleIndex").GetInt32(),TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset);
        Check(filter.RuleIndex==8&&filter.RuleOffset==13554&&filter.RuleSha256=="038FA063AD5C2B8A10562A0040748B9C0CB247645418F38B5ACAE682409A1136"
            &&filter.RuleOffset==filterExpected.GetProperty("Offset").GetInt32()&&filter.RuleBytes==filterExpected.GetProperty("EncodedBytes").GetInt32()
            &&filter.RuleSha256==filterExpected.GetProperty("Sha256").GetString()&&filter.LayerIndex==filterExpected.GetProperty("LayerIndex").GetInt32()
            &&filter.ChildOrdinal==filterExpected.GetProperty("ChildOrdinal").GetInt32()&&filter.FamilyId==filterExpected.GetProperty("FamilyId").GetInt32()
            &&filter.CopyEncodedBytes().AsSpan().SequenceEqual(Hex("FilterRuleHex",1024)),"Selected original filter provenance");
        var controls=new TerrainFamilyHeightFractalControls(53,64,64,TerrainHeightArithmeticPrecision.Binary64);
        string[] names={"walk","run","paused","corrected_paused","resumed","idle","unfocused","focus_resumed","reset"};
        var expectations=root.GetProperty("MotionExpectedBits");Check(expectations.EnumerateObject().Count()==9,"Nine inspection expectations");
        var motion=new Dictionary<string,float>();
        foreach(string name in names){float value=BitConverter.UInt32BitsToSingle(expectations.GetProperty(name).GetUInt32());Check(float.IsFinite(value)&&Math.Abs(value)<=1000,"Finite inspection expectation");motion.Add(name,value);}
        Check(root.GetProperty("Chunks").GetArrayLength()==4,"Four controlled chunks");
        var candidates=new TerrainCoordinateHeightPatch[4];int c=0;
        int preflightAllocations=TerrainPatchVisual.Allocations,preflightDisposals=TerrainPatchVisual.Disposals;
        foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
        {
            var r=chunk.GetProperty("Request");var request=new TerrainSampleCoordinateRequest(r.GetProperty("ChunkWidthBits").GetUInt32(),r.GetProperty("Tiles").GetInt32(),r.GetProperty("Margin").GetInt32(),r.GetProperty("First").GetInt32(),r.GetProperty("Second").GetInt32(),r.GetProperty("Scale").GetInt32());
            Check(request==new TerrainSampleCoordinateRequest(0x41000000,2,3,c%2-1,c/2-1,1)&&chunk.GetProperty("SeamMask").GetInt32()==0,"Controlled chunk order");
            var grid=TerrainSampleCoordinateGrid.Build(request,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
            TerrainFamilyHeightField field;
            try
            {
                var prior=TerrainFamilyHeightPatch.BuildSingleSampler(binding.Families,rule,new(10,10,grid.Parameters.FirstOriginBits,grid.Parameters.SecondOriginBits,grid.Parameters.StepBits,grid.Parameters.StepBits,chunk.GetProperty("InitialHeightBits").GetUInt32(),chunk.GetProperty("InfluenceBits").GetUInt32()),controls);
                var requests=Enumerable.Range(0,100).Select(i=>new TerrainConditionalFractalSampler.Request(i%10,i/10,prior.XAxisBits[i%10],prior.ZAxisBits[i/10])).ToArray();
                uint[] weights=new uint[100];object[] samples=new object[100];
                using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(filter.Family.Raw,10,10,53,64,1048576,1048576))
                {
                    var computed=sampler.Query(requests,64,2097152,16777216);
                    for(int i=0;i<100;i++)
                    {
                        var sample=computed[i].Sample;
                        var query=TerrainFilterFractalInfluence.Query(filter.Input,sample,TerrainHeightArithmeticPrecision.Binary64);
                        var carrier=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(query);
                        weights[i]=TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(chunk.GetProperty("InfluenceBits").GetUInt32(),[],[new(true,filter.Fields.FeatheringTypeBits,carrier)],1,1,false,false,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64)).ProductBits;
                        samples[i]=new{Magnitude=sample.Magnitude,Exponent=sample.Exponent,Negative=sample.IsNegative};
                    }
                }
                uint[] suppliedPrior=chunk.GetProperty("PriorHeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
                uint[] suppliedWeights=chunk.GetProperty("CellInfluenceBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
                field=TerrainFamilyHeightField.Build(binding.Families,rule,new(10,10,grid.Parameters.FirstOriginBits,grid.Parameters.SecondOriginBits,grid.Parameters.StepBits,grid.Parameters.StepBits),suppliedPrior,suppliedWeights,controls);
                Check(field.PriorHeightBits.SequenceEqual(prior.HeightBits)&&field.InfluenceBits.SequenceEqual(weights),"Independent field operands agree");
                Check(field.XAxisBits.SequenceEqual(grid.Layer.FirstAxisBits)&&field.ZAxisBits.SequenceEqual(grid.Layer.SecondAxisBits),"Exact field axes");
                GD.Print("field_height_sampling="+JsonSerializer.Serialize(new{Chunk=c,field.SamplingCounts,field.AccountedWork,field.AccountedRouteBytes,FilterSamples=samples}));
            }
            catch{GD.Print("field_height_preflight_failure="+JsonSerializer.Serialize(new{CompletedChunks=c,Allocated=TerrainPatchVisual.Allocations-preflightAllocations,Disposed=TerrainPatchVisual.Disposals-preflightDisposals}));throw;}
            uint[] heights=field.CopyHeightBits();Check(heights.All(v=>float.IsFinite(BitConverter.UInt32BitsToSingle(v))&&Math.Abs(BitConverter.UInt32BitsToSingle(v))<=1000),"Finite display height cap");
            candidates[c]=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(System.Numerics.Vector3.UnitY,100).ToArray(),Enumerable.Repeat(0xFF0040C0u,100).ToArray());
            GD.Print("field_height_chunk="+JsonSerializer.Serialize(new{Chunk=c,HeightBits=heights,XBits=field.XAxisBits,ZBits=field.ZAxisBits,PriorHeightBits=field.PriorHeightBits,InfluenceBits=field.InfluenceBits,PositiveCells=field.SamplingCounts.ComputedCells,SkippedCells=100-field.SamplingCounts.ComputedCells}));c++;
        }
        // All native height/geometry candidates precede the first GPU allocation.
        terrainAllocationsBefore=TerrainPatchVisual.Allocations;terrainDisposalsBefore=TerrainPatchVisual.Disposals;suppliedTerrain=true;suppliedLayerMotion=motion;
        try
        {
            for(c=0;c<4;c++)foreach(var patch in candidates[c].Patches)
            {
                var query=new TerrainPatchSurfaceQuery(patch.Geometry);var visual=new TerrainPatchVisual(patch.Geometry,this);suppliedTerrainVisuals.Add(visual);suppliedTerrainQueries.Add(query);visual.VerifyReadback();
                GD.Print("supplied_terrain_patch="+JsonSerializer.Serialize(new{Chunk=c,patch.Cell.Column,patch.Cell.Row,Positions=patch.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),Indices=patch.Geometry.CopyIndices(),query.TriangleCount}));
            }
            Check(suppliedTerrainVisuals.Count==16,"Complete family-height terrain");
            GD.Print("field_height_ready="+JsonSerializer.Serialize(new{SelectedHeightRule=selected!.RuleSha256,SelectedFilterRule=filter.RuleSha256,FilterRuleIndex=filter.RuleIndex,OriginalRuleUsed=true,SyntheticComposition=true,OriginalTerrainParity=false,Cells=400}));
            GD.Print("supplied_terrain_ready="+JsonSerializer.Serialize(new{Synthetic=true,Chunks=4,FullSamples=400,Patches=16,QueryTriangles=128,OriginalTerrainParity=false}));
            GD.Print("family_height_ready="+JsonSerializer.Serialize(new{Samples=400,Chunks=4,Patches=16,FamilyId=familyId,binding.GeneratorSha256,binding.GroupSha256,Entries=43,FreshComputedMisses=400,OriginalTerrainParity=false}));
        }
        catch{CloseSuppliedTerrain();throw;}
    }
}
