using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using P=Stardust.Core.Geometry.TerrainHeightArithmeticPrecision;
if(args.Length!=2)throw new ArgumentException("fixture output");
using var input=JsonDocument.Parse(File.ReadAllBytes(args[0]));
if(input.RootElement.GetProperty("Cases").GetArrayLength()!=786)throw new InvalidDataException("Bounded synthetic join");
var rows=new List<object>();
foreach(var c in input.RootElement.GetProperty("Cases").EnumerateArray())
{
    uint U(string k)=>c.GetProperty(k).GetUInt32();P Profile(string k)=>(P)c.GetProperty(k).GetInt32();
    var raw=c.GetProperty("Sample");long magnitude=raw.GetProperty("Magnitude").GetInt64();bool negative=raw.GetProperty("Negative").GetBoolean();
    var miss=TerrainSuppliedFractalSample.FromWide(negative?-magnitude:magnitude,raw.GetProperty("Exponent").GetInt32(),magnitude==0&&negative);
    var hit=miss.ToCacheReload();
    var root=TerrainHeightFractalInput.Decode(Convert.FromHexString(c.GetProperty("RootHex").GetString()!));
    var filter=TerrainFilterFractalInput.Decode(Convert.FromHexString(c.GetProperty("FilterHex").GetString()!));
    var child=TerrainHeightFractalInput.Decode(Convert.FromHexString(c.GetProperty("ChildHex").GetString()!));
    object Run(TerrainSuppliedFractalSample sample)
    {
        var first=TerrainSuppliedHeightFractal.Evaluate(root,U("Initial"),0x3f800000,sample,Profile("Precision"));
        var query=TerrainFilterFractalInfluence.Query(filter,sample,Profile("QueryPrecision"));
        var boundary=new[]{new TerrainLayerInfluenceEntry(true,0,TerrainLayerInfluenceQuery.FromRawBits(0x3f800000,Profile("CombinePrecision")))};
        var retained=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(query);
        if(!ReferenceEquals(retained.RetainedFractalSample,sample))throw new InvalidDataException("Owned sample provenance lost");
        var filters=new[]{new TerrainLayerInfluenceEntry(true,U("Selector"),retained)};
        var receipt=TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(U("Weight"),boundary,filters,1,1,false,false,Profile("CombinePrecision"),Profile("ProductPrecision")));
        uint final=first.Consumer.ResultHeightBits;
        if(receipt.LocalPositive)final=TerrainSuppliedHeightFractal.Evaluate(child,final,receipt.ProductBits,sample,Profile("Precision")).Consumer.ResultHeightBits;
        return new{Name=c.GetProperty("Name").GetString(),CacheBits=miss.ProjectionBits,First=first.Consumer.ResultHeightBits,Scaled=query.ScaledSampleBits,Query=query.QueryProjectionBits,Local=receipt.LocalInfluenceBits,Product=receipt.ProductBits,Final=final};
    }
    rows.Add(new{Miss=Run(miss),Hit=Run(hit)});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(rows));Console.WriteLine($"Retained supplied consumer composition cases={rows.Count}; original noise/cache not executed");
