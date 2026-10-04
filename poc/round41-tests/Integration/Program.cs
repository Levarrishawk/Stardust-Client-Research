using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

try
{
    if(args.Length!=2 || new FileInfo(args[0]).Length is <=0 or >8388608) throw new ArgumentException("bounded fixture-json native-json");
    using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[0]),new JsonDocumentOptions{MaxDepth=24});
    var root=fixture.RootElement;
    if(!root.GetProperty("Synthetic").GetBoolean() || root.GetProperty("OriginalTerrainParity").GetBoolean() || root.GetProperty("Cases").GetArrayLength()!=9)
        throw new InvalidDataException("Explicit synthetic nine-profile join");
    uint U(JsonElement value,string key)=>value.GetProperty(key).GetUInt32();
    int I(JsonElement value,string key)=>value.GetProperty(key).GetInt32();
    var cases=new List<object>();
    foreach(var value in root.GetProperty("Cases").EnumerateArray())
    {
        var circle=TerrainBoundaryCircleInput.Decode(Convert.FromHexString(value.GetProperty("CircleHex").GetString()!));
        var rectangle=TerrainBoundaryRectangleInput.Decode(Convert.FromHexString(value.GetProperty("RectangleHex").GetString()!));
        var rectangleCache=TerrainBoundaryRectangleInfluence.Cache(rectangle,TerrainHeightArithmeticPrecision.Binary53);
        var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(value.GetProperty("GeneratorHex").GetString()!));
        var rules=generator.Rules.Select(rule=>rule.HeightConstant ?? throw new InvalidDataException("Typed synthetic AHCN")).ToArray();
        if(rules.Length!=3)throw new InvalidDataException("Three ordered rules");
        var precision=(TerrainHeightArithmeticPrecision)I(value,"CombinePrecision");
        var productPrecision=(TerrainHeightArithmeticPrecision)I(value,"ProductPrecision");
        var chunks=new List<object>();
        if(value.GetProperty("Chunks").GetArrayLength()!=4)throw new InvalidDataException("Four complete chunks");
        foreach(var chunk in value.GetProperty("Chunks").EnumerateArray())
        {
            var r=chunk.GetProperty("Request");
            var grid=TerrainSampleCoordinateGrid.Build(new(U(r,"ChunkWidthBits"),I(r,"Tiles"),I(r,"Margin"),I(r,"First"),I(r,"Second"),I(r,"Scale")),
                TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,
                TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
            if(grid.Parameters.Count!=10 || chunk.GetProperty("InitialHeightBits").GetArrayLength()!=100)throw new InvalidDataException("Synthetic complete extent");
            uint[] initial=chunk.GetProperty("InitialHeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
            var heightVisits=new int[100];var local=new uint[100];var product=new uint[100];var heights=new uint[100];var visits=new int[100][];
            for(int row=0;row<10;row++)for(int column=0;column<10;column++)
            {
                int index=row*10+column;var xy=grid.Layer.GetPair(column,row);
                var cq=TerrainBoundaryCircleInfluence.Query(circle,xy.FirstBits,xy.SecondBits,
                    TerrainHeightArithmeticPrecision.Binary53,TerrainHeightArithmeticPrecision.Binary64);
                var rq=TerrainBoundaryRectangleInfluence.Query(rectangleCache,xy.FirstBits,xy.SecondBits,TerrainHeightArithmeticPrecision.Binary64);
                TerrainLayerInfluenceEntry[] boundaries=[new(true,3,TerrainLayerInfluenceQuery.FromCircle(cq)),new(true,2,TerrainLayerInfluenceQuery.FromRectangle(rq))];
                TerrainLayerInfluenceEntry[] filters=[new(true,1,TerrainLayerInfluenceQuery.FromRawBits(U(value,"FilterBits"),TerrainHeightArithmeticPrecision.Binary64))];
                var sample=TerrainLayerInfluenceSample.Create(U(value,"IncomingBits"),boundaries,filters,1,1,
                    value.GetProperty("BoundaryInverted").GetBoolean(),value.GetProperty("FilterInverted").GetBoolean(),precision,productPrecision);
                var execution=TerrainSuppliedLayerHeight.Evaluate(TerrainSuppliedLayerHeightSample.Create(sample,initial[index],rules,precision));
                var result=execution.Influence;
                local[index]=result.LocalInfluenceBits;product[index]=result.ProductBits;
                visits[index]=[result.BoundaryVisits.Count,result.FilterVisits.Count];
                heights[index]=execution.FinalHeightBits;
                heightVisits[index]=execution.Visits.Count;
            }
            var padded=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(Vector3.UnitY,100).ToArray(),
                Enumerable.Repeat(0xFF0040C0u,100).ToArray(),I(chunk,"SeamMask"));
            if(padded.Patches.Count!=4 || chunk.GetProperty("Locations").GetArrayLength()!=4)throw new InvalidDataException("Four row-major patches");
            var patches=new List<object>();
            for(int index=0;index<padded.Patches.Count;index++)
            {
                var patch=padded.Patches[index];var surface=new TerrainPatchSurfaceQuery(patch.Geometry);
                var locations=chunk.GetProperty("Locations")[index];
                if(locations.GetArrayLength()!=4)throw new InvalidDataException("Four queries");
                patches.Add(new{patch.Cell.Column,patch.Cell.Row,Pattern=patch.Geometry.PatternIndex,
                    Positions=patch.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),
                    Indices=patch.Geometry.CopyIndices(),Hits=locations.EnumerateArray().Select(q=>surface.Query(q[0].GetDouble(),q[1].GetDouble())).ToArray()});
            }
            chunks.Add(new{LocalBits=local,ProductBits=product,Visits=visits,HeightBits=heights,HeightVisits=heightVisits,Patches=patches});
        }
        cases.Add(new{Name=value.GetProperty("Name").GetString(),Chunks=chunks});
    }
    File.WriteAllText(args[1],JsonSerializer.Serialize(new{Cases=cases,Synthetic=true,OriginalTerrainParity=false}));
    Console.WriteLine("PASS: supplied ordered-layer/AHCN/coordinates/padded geometry/query public API join");
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
