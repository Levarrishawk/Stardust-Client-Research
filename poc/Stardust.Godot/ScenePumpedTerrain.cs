using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Geometry;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private readonly List<TerrainPatchVisual> suppliedTerrainVisuals = new();
    private readonly List<TerrainPatchSurfaceQuery> suppliedTerrainQueries = new();
    private TerrainModernPatchSetSurfaceQuery? suppliedTerrainSetQuery;
    private bool suppliedTerrain;
    private int terrainAllocationsBefore, terrainDisposalsBefore;

    // Explicit synthetic supplied channels; this is not original terrain generation.
    private void LoadSuppliedTerrain(string path)
    {
        long fixtureBytes=new FileInfo(path).Length;
        Check(!suppliedTerrain && fixtureBytes is > 0 and <= 524288, "Bounded terrain fixture");
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 16 });
        var root = fixture.RootElement;
        Check(suppliedTerrainMaterial is null||root.GetProperty("Version").GetInt32()==9,"Supplied MAIN material requires retained sparse terrain mode");
        if(root.GetProperty("Version").GetInt32()==9){LoadSparseFirstCurveTerrain(root);return;}
        if(root.GetProperty("Version").GetInt32()==8){LoadFirstCurveTerrain(root);return;}
        if(root.GetProperty("Version").GetInt32()==7){LoadFilteredHeightTerrain(root);return;}
        if(root.GetProperty("Version").GetInt32()==6){LoadHeightFieldTerrain(root);return;}
        if(root.GetProperty("Version").GetInt32() is 4 or 5){LoadFamilyHeightTerrain(root);return;}
        Check(fixtureBytes<=262144,"Existing terrain fixture cap");
        if(root.GetProperty("Version").GetInt32()==3){LoadHeightFilteredTerrain(root);return;}
        if(root.GetProperty("Version").GetInt32()==2){LoadCalculatedLayerTerrain(root);return;}
        Check(root.GetProperty("Version").GetInt32() == 1 && root.GetProperty("Synthetic").GetBoolean()
            && root.GetProperty("HeightPolicy").GetString() == "supplied_native_z_divided_by_four"
            && root.GetProperty("FootOffsetBits").GetUInt32() == 0, "Explicit supplied terrain fixture policy");
        var profiles = root.GetProperty("Profiles").EnumerateArray().Select(p => p.GetInt32()).ToArray();
        Check(profiles.SequenceEqual(new[] { 24, 24, 24, 24 }), "Explicit terrain precision profiles");
        var chunks = root.GetProperty("Chunks");
        Check(chunks.GetArrayLength() == 4, "Four supplied chunks");
        terrainAllocationsBefore = TerrainPatchVisual.Allocations;
        terrainDisposalsBefore = TerrainPatchVisual.Disposals;
        suppliedTerrain = true;
        try
        {
            int chunkIndex = 0;
            foreach (var chunk in chunks.EnumerateArray())
            {
                var r = chunk.GetProperty("Request");
                var request = new TerrainSampleCoordinateRequest(r.GetProperty("ChunkWidthBits").GetUInt32(),
                    r.GetProperty("Tiles").GetInt32(), r.GetProperty("Margin").GetInt32(),
                    r.GetProperty("First").GetInt32(), r.GetProperty("Second").GetInt32(), r.GetProperty("Scale").GetInt32());
                Check(request == new TerrainSampleCoordinateRequest(0x41000000, 2, 3,
                    chunkIndex % 2 - 1, chunkIndex / 2 - 1, 1), "Synthetic chunk identity and order");
                Check(chunk.GetProperty("HeightBits").GetArrayLength() == 100
                    && chunk.GetProperty("NormalBits").GetArrayLength() == 100
                    && chunk.GetProperty("Argb").GetArrayLength() == 100, "Complete bounded supplied channels");
                uint[] heights = chunk.GetProperty("HeightBits").EnumerateArray().Select(v => v.GetUInt32()).ToArray();
                var normals = chunk.GetProperty("NormalBits").EnumerateArray().Select(v =>
                {
                    Check(v.GetArrayLength() == 3, "Normal width");
                    return new System.Numerics.Vector3(BitConverter.UInt32BitsToSingle(v[0].GetUInt32()),
                        BitConverter.UInt32BitsToSingle(v[1].GetUInt32()), BitConverter.UInt32BitsToSingle(v[2].GetUInt32()));
                }).ToArray();
                uint[] colors = chunk.GetProperty("Argb").EnumerateArray().Select(v => v.GetUInt32()).ToArray();
                Check(chunk.GetProperty("SeamMask").GetInt32() == 0 && chunk.GetProperty("UvScale").GetSingle() == 1,
                    "Explicit supplied seam and UV");
                var coordinates = TerrainSampleCoordinateGrid.Build(request, TerrainHeightArithmeticPrecision.Binary24,
                    TerrainHeightArithmeticPrecision.Binary24, TerrainHeightArithmeticPrecision.Binary24, TerrainHeightArithmeticPrecision.Binary24);
                var result = TerrainCoordinateHeightPatch.Build(coordinates, heights, normals, colors, 0, 1);
                foreach (var patch in result.Patches)
                {
                    var query = new TerrainPatchSurfaceQuery(patch.Geometry);
                    var visual = new TerrainPatchVisual(patch.Geometry, this);
                    suppliedTerrainVisuals.Add(visual);
                    suppliedTerrainQueries.Add(query);
                    visual.VerifyReadback();
                    GD.Print("supplied_terrain_patch=" + JsonSerializer.Serialize(new
                    {
                        Chunk = chunkIndex, patch.Cell.Column, patch.Cell.Row,
                        Positions = patch.Geometry.CopyPositions().Select(v => new[] { BitConverter.SingleToUInt32Bits(v.X), BitConverter.SingleToUInt32Bits(v.Y), BitConverter.SingleToUInt32Bits(v.Z) }),
                        Indices = patch.Geometry.CopyIndices(), query.TriangleCount
                    }));
                }
                chunkIndex++;
            }
            Check(suppliedTerrainVisuals.Count == 16 && suppliedTerrainQueries.Count == 16, "Sixteen terrain patches");
            GD.Print("supplied_terrain_ready=" + JsonSerializer.Serialize(new { Synthetic = true, Chunks = 4,
                FullSamples = 400, Patches = 16, QueryTriangles = suppliedTerrainQueries.Sum(q => q.TriangleCount),
                OriginalTerrainParity = false }));
        }
        catch { CloseSuppliedTerrain(); throw; }
    }

    private ISceneWorldLocalMotionDriver CreateLocalDriver() => suppliedTerrainSetQuery is not null
        ? new SceneWorldTerrainMotionDriver(world!,motions!,humanId,suppliedTerrainSetQuery)
        : suppliedTerrain
        ? new SceneWorldTerrainMotionDriver(world!, motions!, humanId, suppliedTerrainQueries)
        : new SceneWorldLocalMotionDriver(world!, motions!, humanId);

    private System.Numerics.Vector3 TerrainExpected(string name, System.Numerics.Vector3 expected)
    {
        // Corrections remain the authoritative raw pose until a local tick occurs.
        if(suppliedLayerMotion is not null)expected.Y=suppliedLayerMotion[name];
        else if (suppliedTerrain && name != "corrected_paused") expected.Y = (float)((double)expected.Z / 4);
        return expected;
    }

    private void CloseSuppliedTerrain()
    {
        CloseSparseTerrainLight();
        foreach (var visual in suppliedTerrainVisuals) visual.Dispose();
        CloseTerrainMaterial();
        suppliedTerrainVisuals.Clear(); suppliedTerrainQueries.Clear();suppliedTerrainRayQuery=null;suppliedTerrainSetQuery=null;
        suppliedLayerMotion?.Clear();suppliedLayerMotion=null;
        if (suppliedTerrain)
        {
            int allocated = TerrainPatchVisual.Allocations - terrainAllocationsBefore;
            int disposed = TerrainPatchVisual.Disposals - terrainDisposalsBefore;
            Check(allocated == disposed, "Supplied terrain resources drain");
            GD.Print("supplied_terrain_cleanup=" + JsonSerializer.Serialize(new { Allocated = allocated, Disposed = disposed, Drained = true }));
            suppliedTerrain = false;
            heightFilteredTerrain = false;
        }
    }
}
