using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Precision=Stardust.Core.Geometry.TerrainHeightArithmeticPrecision;

// Synthetic composition over accepted APIs; no original sampler, traversal or cache.
using var input=JsonDocument.Parse(File.ReadAllBytes(args[0]));
var rows=new List<object>();
foreach(var c in input.RootElement.GetProperty("Cases").EnumerateArray())
{
    uint Word(string key)=>c.GetProperty(key).GetUInt32();
    Precision P(string key)=>(Precision)c.GetProperty(key).GetInt32();
    byte[] Bytes(string key)=>Convert.FromHexString(c.GetProperty(key).GetString()!);
    var root=TerrainHeightFractalInput.Decode(Bytes("RootHex"));
    var ff=TerrainFilterFractalInput.Decode(Bytes("FractalFilterHex"));
    var hf=TerrainFilterHeightInput.Decode(Bytes("HeightFilterHex"));
    var child=TerrainHeightFractalInput.Decode(Bytes("ChildHex"));
    var first=TerrainSuppliedHeightFractal.Evaluate(root,Word("Initial"),0x3f800000,Word("RootSample"),P("Precision"));
    uint boundary=Word("Boundary");
    var boundaries=new[]{new TerrainLayerInfluenceEntry(true,0,TerrainLayerInfluenceQuery.FromRawBits(boundary,P("Combine")))};
    var filters=new List<TerrainLayerInfluenceEntry>();
    TerrainFractalFilterQuery? fq=null;TerrainHeightFilterQuery? hq=null;
    TerrainLayerInfluenceReceipt Combine()=>TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(Word("Incoming"),boundaries,filters.ToArray(),1,1,false,false,P("Combine"),P("Product")));
    var receipt=Combine();
    if(receipt.BoundaryPositive)
    {
        fq=TerrainFilterFractalInfluence.Query(ff,Word("FilterSample"),P("Query"));
        filters.Add(new(true,ff.Fields!.FeatheringTypeBits,TerrainLayerInfluenceQuery.FromFractalFilter(fq)));
        receipt=Combine();
        if(receipt.LocalPositive)
        {
            hq=TerrainFilterHeightInfluence.Query(hf,first.ResultHeightBits,P("HeightQuery"));
            filters.Add(new(true,hf.Fields!.FeatheringTypeBits!.Value,TerrainLayerInfluenceQuery.FromHeightFilter(hq)));
            receipt=Combine();
        }
    }
    TerrainHeightFractalResult? last=null;
    if(receipt.LocalPositive)
        last=TerrainSuppliedHeightFractal.Evaluate(child,first.ResultHeightBits,receipt.ProductBits,Word("ChildSample"),P("Precision"));
    rows.Add(new{Name=c.GetProperty("Name").GetString(),First=first.ResultHeightBits,FirstOutcome=(int)first.Outcome,
        FractalQuery=fq?.QueryProjectionBits,Scaled=fq?.ScaledSampleBits,HeightQuery=hq?.QueryProjectionBits,QueryHeight=hq?.CurrentHeightBits,
        Local=receipt.LocalInfluenceBits,Product=receipt.ProductBits,FilterVisits=receipt.FilterVisits.Count,
        Final=last?.ResultHeightBits??first.ResultHeightBits,ChildOutcome=last is {} r?(int?)r.Outcome:null});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(rows));
Console.WriteLine($"fractal composition cases={rows.Count}");
