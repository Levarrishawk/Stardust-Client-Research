using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using P=Stardust.Core.Geometry.TerrainHeightArithmeticPrecision;
if(args.Length!=2)throw new ArgumentException("fixture output");
using var input=JsonDocument.Parse(File.ReadAllBytes(args[0]));
var data=input.RootElement;
if(data.GetProperty("Cases").GetArrayLength()!=2592||data.GetProperty("OriginalNoiseSamples").GetInt32()!=0)throw new InvalidDataException("Bounded conditional join");
byte[] raw=Convert.FromHexString(data.GetProperty("OriginalGroupHex").GetString()!);
if(raw.Length!=5633||Convert.ToHexString(SHA256.HashData(raw))!=data.GetProperty("OriginalGroupSha256").GetString())throw new InvalidDataException("Whole MGRP identity");
var original=TerrainFractalFamilySet.Decode(raw);
if(original.Entries.Count!=43)throw new InvalidDataException("Bounded selected original family group");
var entries=original.Entries.Select(e=>{
    byte[] name=e.CopyNameBytes();if(name.Length<1||name[^1]!=0)throw new InvalidDataException("Owned raw terminator");
    return new{e.Id,NameSha256=Convert.ToHexString(SHA256.HashData(name.AsSpan(0,name.Length-1))),NameBytes=name.Length-1,MfrcSha256=Convert.ToHexString(SHA256.HashData(e.Raw.CopyEncodedBytes())),Words=e.Raw.CopyDataWordBits(),e.Offset,Bytes=e.EncodedBytes};
}).ToArray();
var rows=new List<object>();
foreach(var c in data.GetProperty("Cases").EnumerateArray())
{
        uint U(string k)=>c.GetProperty(k).GetUInt32();P Profile(string k)=>(P)c.GetProperty(k).GetInt32();
        var families=c.GetProperty("OriginalGroup").GetBoolean()?original:TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("MgrpHex").GetString()!));
        var selected=families.Find(1)??throw new InvalidDataException("Exact family1 required without fallback");
        if(selected.Raw.Fields.ModeBits!=U("Mode"))throw new InvalidDataException("Selected caller mode differs");
        using var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(selected.Raw,1,1,c.GetProperty("TablePrecision").GetInt32(),c.GetProperty("ConstructionPrecision").GetInt32(),1048576,1048576);
        var request=new[]{new TerrainConditionalFractalSampler.Request(0,0,U("X"),U("Z"))};
        var computed=sampler.Query(request,c.GetProperty("NoisePrecision").GetInt32(),2097152,16777216)[0];
        var cached=sampler.Query(request,c.GetProperty("NoisePrecision").GetInt32(),2097152,16777216)[0];
        if(computed.Origin!=TerrainConditionalFractalOrigin.ConditionalComputedMiss||cached.Origin!=TerrainConditionalFractalOrigin.CacheReloadF32||cached.Sample.Provenance!=TerrainSuppliedFractalProvenance.CacheReloadF32)throw new InvalidDataException("Sampler origin differs");
        var miss=computed.Sample;var hit=cached.Sample;
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
        rows.Add(new{Name=c.GetProperty("Name").GetString(),Normalization=sampler.NormalizationBits,Sample=new{Magnitude=miss.Magnitude,Exponent=miss.Exponent,Negative=miss.IsNegative},Miss=Run(miss),Hit=Run(hit)});
    }
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Entries=entries,Cases=rows}));
Console.WriteLine($"Conditional owned family/mode consumer join cases={rows.Count}; original terrain parity unclaimed");
