using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

using var document=JsonDocument.Parse(File.ReadAllText(args[0]));
var rows=new List<object>();int patches=0,queries=0,reflections=0;
foreach(var f in document.RootElement.GetProperty("Cases").EnumerateArray())
{
    uint[] Words(string key)=>f.GetProperty(key).EnumerateArray().Select(x=>x.GetUInt32()).ToArray();
    var circle=TerrainBoundaryCircleInput.Decode(Convert.FromHexString(f.GetProperty("CircleHex").GetString()!));
    var height=TerrainGeneratorInput.Decode(Convert.FromHexString(f.GetProperty("GeneratorHex").GetString()!)).Rules.Single().HeightConstant!;
    var value=TerrainCircleHeightPatch.Build(f.GetProperty("Cells").GetInt32(),Words("First"),Words("Second"),Words("Initial"),circle,height,
        (TerrainHeightArithmeticPrecision)f.GetProperty("CachePrecision").GetInt32(),(TerrainHeightArithmeticPrecision)f.GetProperty("QueryPrecision").GetInt32(),
        (TerrainHeightArithmeticPrecision)f.GetProperty("HeightPrecision").GetInt32(),f.GetProperty("Mask").GetInt32());
    var cells=new List<object>();int ordinal=0;
    foreach(var patch in value.Patches)
    {
        var query=new TerrainPatchSurfaceQuery(patch.Geometry);var hits=new List<TerrainPatchSurfaceHit?>();
        foreach(var p in f.GetProperty("Locations")[ordinal].EnumerateArray())
        {
            double x=p[0].GetDouble(),z=p[1].GetDouble();var hit=query.Query(x,z);hits.Add(hit);queries++;
            // Explicit accepted reflection only; no original/world-transform parity claim.
            double godotZ=-z;var reflected=query.Query(x,-godotZ);
            if(hit!=reflected)throw new InvalidOperationException("Native/Godot reflection round trip.");
            reflections++;
        }
        cells.Add(new {Pattern=patch.Geometry.PatternIndex,Hits=hits});ordinal++;patches++;
    }
    rows.Add(new {Name=f.GetProperty("Name").GetString(),Patches=cells});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new {Cases=rows,PatchCount=patches,QueryCount=queries,ReflectionCount=reflections}));
Console.WriteLine($"PASS: supplied height-patch/surface join patches={patches} queries={queries} reflected={reflections}");
