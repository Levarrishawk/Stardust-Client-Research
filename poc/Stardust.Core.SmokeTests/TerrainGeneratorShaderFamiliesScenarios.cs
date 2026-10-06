using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
namespace Stardust.Core.SmokeTests;
public static class TerrainGeneratorShaderFamiliesScenarios
{
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var p)?p.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var p)?p.GetInt64():d;
    private static bool Flag(JsonElement c,string k)=>c.TryGetProperty(k,out var p)&&p.GetBoolean();
    private static TerrainGeneratorShaderFamiliesLimits Limits(JsonElement c)=>new(I(c,"MaximumEncodedBytes",4194304),I(c,"MaximumFamilies",4096),I(c,"MaximumEntriesPerFamily",1024),I(c,"MaximumTotalEntries",65536),I(c,"MaximumStringBytes",999),L(c,"MaximumTotalStringBytes",1048576),L(c,"MaximumCopyBytes",8388608),L(c,"MaximumArrayBytes",8388608),L(c,"MaximumWork",16777216));
    private static void Check(bool okay){if(!okay)throw new InvalidOperationException("Shader-family raw structure invariant.");}
    private static string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b));
    private static object Snapshot(TerrainGeneratorShaderFamilies r)=>new{r.Version,r.EncodedBytes,r.SourceSha256,r.TotalEntries,r.TotalStringBytes,r.LogicalCopyBytes,r.ArrayBytes,r.Work,r.ExportedGroupBytes,r.HasShaderSemantics,r.GeneratorOffset,r.GeneratorSha256,
        ActualReferences=r.Generator is null?r.Group is null:ReferenceEquals(r.Group,r.Generator.Groups[0]),
        Families=r.Families.Select(f=>new{f.Index,f.Offset,f.EncodedBytes,f.FamilyKeyBits,f.RawWord0Bits,f.RawWord1Bits,f.EntryCountBits,FirstHex=Convert.ToHexString(f.CopyFirstStringBytes()),SecondHex=Convert.ToHexString(f.CopySecondStringBytes()),TripleHex=Convert.ToHexString(f.CopyRawTripleBytes()),EncodedSha256=Hash(f.CopyEncodedBytes()),Entries=f.Entries.Select(e=>new{e.Index,e.Offset,e.EncodedBytes,e.RawWordBits,StringHex=Convert.ToHexString(e.CopyStringBytes()),EncodedSha256=Hash(e.CopyEncodedBytes())}).ToArray()}).ToArray()};
    private static TerrainGeneratorShaderFamilies Evaluate(JsonElement c){var token=new CancellationToken(Flag(c,"Cancel"));return Flag(c,"Bind")?TerrainGeneratorShaderFamilies.Bind(TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!)),Limits(c),token):TerrainGeneratorShaderFamilies.Decode(Convert.FromHexString(c.GetProperty("Hex").GetString()!),Limits(c),token);}
    public static object Run(string path){using var d=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();
        foreach(var c in d.RootElement.EnumerateArray()){
            try{var r=Evaluate(c);cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="None",Snapshot=Snapshot(r)});}
            catch(ArgumentException){cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Options"});}
            catch(InvalidDataException){cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Input"});}
            catch(OperationCanceledException){cases.Add(new{Name=c.GetProperty("Name").GetString(),Reject="Cancel"});}
        }
        var first=d.RootElement[0];var input=Convert.FromHexString(first.GetProperty("Hex").GetString()!);var original=(byte[])input.Clone();var r0=TerrainGeneratorShaderFamilies.Decode(input);string before=JsonSerializer.Serialize(Snapshot(r0));int checks=0;
        void Stable(){Check(before==JsonSerializer.Serialize(Snapshot(r0)));checks++;}
        Array.Fill(input,(byte)0);Stable();Array.Fill(r0.CopyEncodedBytes(),(byte)0);Stable();var f=r0.Families[0];Array.Fill(f.CopyEncodedBytes(),(byte)0);Stable();Array.Fill(f.CopyFirstStringBytes(),(byte)0);Stable();Array.Fill(f.CopySecondStringBytes(),(byte)0);Stable();Array.Fill(f.CopyRawTripleBytes(),(byte)0);Stable();Array.Fill(f.Entries[0].CopyStringBytes(),(byte)0);Stable();Array.Fill(f.Entries[0].CopyEncodedBytes(),(byte)0);Stable();
        bool ro=false;try{((IList<TerrainGeneratorShaderFamily>)r0.Families)[0]=f;}catch(NotSupportedException){ro=true;}Check(ro);Stable();ro=false;try{((IList<TerrainGeneratorShaderEntry>)f.Entries)[0]=f.Entries[0];}catch(NotSupportedException){ro=true;}Check(ro);Stable();
        Check(before==JsonSerializer.Serialize(Snapshot(TerrainGeneratorShaderFamilies.Decode(original))));Stable();Parallel.For(0,4,_=>Check(before==JsonSerializer.Serialize(Snapshot(TerrainGeneratorShaderFamilies.Decode(original)))));Stable();
        foreach(string name in new[]{"cancel","precopy-zero-cap","late-malformed-family"}){
            var c=d.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()==name);bool failed=false;try{Evaluate(c);}catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){failed=true;}Check(failed);Stable();
        }
        var bind=d.RootElement.EnumerateArray().First(x=>x.GetProperty("Name").GetString()=="bind-owned");var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(bind.GetProperty("GeneratorHex").GetString()!));var bound=TerrainGeneratorShaderFamilies.Bind(generator);string b0=JsonSerializer.Serialize(Snapshot(bound));
        void BoundStable(){Check(b0==JsonSerializer.Serialize(Snapshot(bound)));checks++;}
        Array.Fill(generator.Groups[0].Section.CopyEncodedBytes(),(byte)0);BoundStable();Array.Fill(bound.CopyEncodedBytes(),(byte)0);BoundStable();Check(ReferenceEquals(bound.Generator,generator)&&ReferenceEquals(bound.Group,generator.Groups[0]));BoundStable();
        Check(b0==JsonSerializer.Serialize(Snapshot(TerrainGeneratorShaderFamilies.Bind(generator))));BoundStable();Parallel.For(0,4,_=>Check(b0==JsonSerializer.Serialize(Snapshot(TerrainGeneratorShaderFamilies.Bind(generator)))));BoundStable();
        long start=GC.GetAllocatedBytesForCurrentThread();bool reject=false;try{TerrainGeneratorShaderFamilies.Decode(original,new(MaximumCopyBytes:0));}catch(InvalidDataException){reject=true;}Check(reject&&GC.GetAllocatedBytesForCurrentThread()-start<16384);Stable();
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
