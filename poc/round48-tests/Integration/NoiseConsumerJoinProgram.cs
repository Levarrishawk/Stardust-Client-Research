using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using P=Stardust.Core.Geometry.TerrainHeightArithmeticPrecision;
if(args.Length!=2)throw new ArgumentException("fixture output");
using var input=JsonDocument.Parse(File.ReadAllBytes(args[0]));
var data=input.RootElement;
if(data.GetProperty("Cases").GetArrayLength()!=864||data.GetProperty("OriginalNoiseSamples").GetInt32()!=0)throw new InvalidDataException("Bounded conditional join");
byte[] raw=Convert.FromHexString(data.GetProperty("MfrcHex").GetString()!);
if(Convert.ToHexString(SHA256.HashData(raw))!="85A84EFBDF4EE507C0011C9E4D4ED130EBF4719735A521FD491ABD286EBD723E")throw new InvalidDataException("MFRC identity");
var decoded=TerrainFractalRawInput.Decode(raw);
string Hash(IReadOnlyList<uint> words){byte[] bytes=new byte[4*words.Count];for(int i=0;i<words.Count;i++)BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4*i,4),words[i]);return Convert.ToHexString(SHA256.HashData(bytes));}
var kernels=new Dictionary<int,TerrainSeededNoiseKernel>();var tables=new List<object>();var rows=new List<object>();
try
{
    foreach(int tp in new[]{24,53,64})
    {
        var kernel=TerrainSeededNoiseKernel.Create(decoded.Fields.SignedSeed,tp,TerrainSeededNoiseKernel.TableBytes,TerrainSeededNoiseKernel.BuildWork);
        kernels.Add(tp,kernel);var t=kernel.CopyTables(514*16);
        tables.Add(new{Precision=tp,PermutationSha=Hash(t.Permutation),Gradient1Sha=Hash(t.Scalar),Gradient2Sha=Hash(t.GradientPairs),RngDraws=t.Draws,RngSteps=t.Steps});
    }
    foreach(var c in data.GetProperty("Cases").EnumerateArray())
    {
        uint U(string k)=>c.GetProperty(k).GetUInt32();P Profile(string k)=>(P)c.GetProperty(k).GetInt32();
        var result=kernels[c.GetProperty("TablePrecision").GetInt32()].Query(new[]{new TerrainSeededNoiseKernel.Coordinate(U("X"),U("Z"))},c.GetProperty("NoisePrecision").GetInt32(),40,256,true)[0];
        var miss=TerrainSuppliedFractalSample.FromWideMagnitude(result.Magnitude,result.Negative,result.Exponent);
        if(result.InspectionBits!=miss.ProjectionBits)throw new InvalidDataException("Noise projection differs from supplied sample");
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
            var influence=TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(U("Weight"),boundary,filters,1,1,false,false,Profile("CombinePrecision"),Profile("ProductPrecision")));
            uint final=first.Consumer.ResultHeightBits;
            if(influence.LocalPositive)final=TerrainSuppliedHeightFractal.Evaluate(child,final,influence.ProductBits,sample,Profile("Precision")).Consumer.ResultHeightBits;
            return new{Name=c.GetProperty("Name").GetString(),CacheBits=miss.ProjectionBits,First=first.Consumer.ResultHeightBits,Scaled=query.ScaledSampleBits,Query=query.QueryProjectionBits,Local=influence.LocalInfluenceBits,Product=influence.ProductBits,Final=final};
        }
        rows.Add(new{Name=c.GetProperty("Name").GetString(),Sample=new{Magnitude=result.Magnitude,Exponent=result.Exponent,Negative=result.Negative},Miss=Run(miss),Hit=Run(hit)});
    }
    File.WriteAllText(args[1],JsonSerializer.Serialize(new{Words=decoded.CopyDataWordBits(),Tables=tables,Cases=rows}));
    Console.WriteLine($"Conditional computed noise consumer join cases={rows.Count}; original terrain parity unclaimed");
}
finally{foreach(var kernel in kernels.Values)kernel.Dispose();}
