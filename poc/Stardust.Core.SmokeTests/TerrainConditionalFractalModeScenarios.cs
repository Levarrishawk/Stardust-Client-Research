using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainConditionalFractalModeScenarios
{
    private static void Need(bool value){if(!value)throw new Exception("Conditional sampler assertion.");}
    private static string Fraction(TerrainSuppliedFractalSample s)
    {BigInteger n=s.Magnitude,d=BigInteger.One;if(s.IsNegative)n=-n;if(s.Exponent>=0)n<<=s.Exponent;else d<<=-s.Exponent;BigInteger g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/g}/{d/g}";}
    private static byte[] Synthetic()
    {
        byte[] raw=new byte[84];"FORM"u8.CopyTo(raw);BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(4),76);"MFRCFORM"u8.CopyTo(raw.AsSpan(8));BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(16),64);"0001DATA"u8.CopyTo(raw.AsSpan(20));BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(28),52);
        uint[] words=[1,0,0x7fc12345,0,0x7f800000,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0];for(int i=0;i<13;i++)BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(32+4*i),words[i]);return raw;
    }
    public static object Run(string fixturePath)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixturePath,System.Text.Encoding.UTF8));var cases=new List<object>();
        foreach(var c in document.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;TerrainConditionalFractalSampler? sampler=null;
            int Int(JsonElement e,string key,int fallback)=>e.TryGetProperty(key,out var v)?v.GetInt32():fallback;
            bool Flag(JsonElement e,string key)=>e.TryGetProperty(key,out var v)&&v.GetBoolean();
            try
            {
                var raw=TerrainFractalRawInput.Decode(Convert.FromHexString(c.GetProperty("Hex").GetString()!));using var factoryCancellation=new CancellationTokenSource();if(Flag(c,"Cancel"))factoryCancellation.Cancel();
                sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(raw,Int(c,"Width",2),Int(c,"Rows",2),Int(c,"TablePrecision",53),Int(c,"ConstructionPrecision",53),Int(c,"ByteBudget",1048576),Int(c,"WorkBudget",1048576),Flag(c,"Sine"),factoryCancellation.Token);
                var actions=new List<object>();
                foreach(var a in c.GetProperty("Actions").EnumerateArray())
                {
                    using var token=new CancellationTokenSource();if(Flag(a,"Cancel"))token.Cancel();var results=new List<object>();string reject="None";
                    try
                    {
                        if(a.GetProperty("Kind").GetString()=="Clear")sampler.Clear(token.Token);
                        else
                        {
                            var requests=a.GetProperty("Requests").EnumerateArray().Select(r=>new TerrainConditionalFractalSampler.Request(r.GetProperty("Column").GetInt32(),r.GetProperty("Row").GetInt32(),r.GetProperty("XBits").GetUInt32(),r.GetProperty("ZBits").GetUInt32())).ToArray();
                            foreach(var r in sampler.Query(requests,Int(a,"Precision",53),Int(a,"ByteBudget",2097152),Int(a,"WorkBudget",16777216),token.Token))
                                results.Add(new{Origin=r.Origin.ToString(),Retained=Fraction(r.Sample),NegativeZero=r.Sample.NegativeZero,Projection=r.Sample.ProjectionBits});
                        }
                    }
                    catch(Exception e)when(e is ArgumentException or InvalidDataException or OperationCanceledException){reject="Input";results.Clear();}
                    actions.Add(new{Reject=reject,Results=results,Cache=sampler.CopyCache(16384)});
                }
                cases.Add(new{Name=name,Reject="None",Normalization=sampler.NormalizationBits,Actions=actions});
            }
            catch(Exception e)when(e is ArgumentException or InvalidDataException or OperationCanceledException){cases.Add(new{Name=name,Reject="Input"});}
            finally{sampler?.Dispose();}
        }
        int negatives=0;
        void Reject(Action a)
        {try{a();throw new Exception("Expected explicit rejection.");}catch(Exception e)when(e is ArgumentException or InvalidDataException or InvalidOperationException or OperationCanceledException or NotSupportedException){negatives++;}}
        byte[] encoded=Synthetic();var input=TerrainFractalRawInput.Decode(encoded);var original=input.CopyEncodedBytes();Array.Clear(encoded);
        for(uint mode=1;mode<=5;mode++)
        {
            byte[] rejected=original.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(rejected.AsSpan(80),mode);
            var nonzero=TerrainFractalRawInput.Decode(rejected);
            Reject(()=>TerrainConditionalFractalSampler.Create(nonzero,2,2,53,64,1048576,1048576));
        }
        Need(!input.Fields.HasFiniteParameters);using var owned=TerrainConditionalFractalSampler.CreateForSupportedModes(input,2,2,53,64,1048576,1048576);
        var p=new TerrainConditionalFractalSampler.Request(0,0,0x3f000001,0x3e800001);
        var miss=owned.Query([p],64,2097152,16777216)[0];var snapshot=owned.CopyCache(16384);var hit=owned.Query([p],64,2097152,16777216)[0];
        Need(miss.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&miss.Sample.Provenance==TerrainSuppliedFractalProvenance.SuppliedWideMiss);
        Need(hit.Origin==TerrainConditionalFractalOrigin.CacheReloadF32&&hit.Sample.Provenance==TerrainSuppliedFractalProvenance.CacheReloadF32);
        Reject(()=>owned.Query([p],23,2097152,16777216));Reject(()=>owned.Query([p],53,0,16777216));Reject(()=>owned.Query([p],53,2097152,0));
        Reject(()=>owned.Query([p],53,int.MaxValue,16777216));Reject(()=>owned.Query([p],53,2097152,int.MaxValue));
        Reject(()=>owned.Query(new TerrainConditionalFractalSampler.Request[1025],53,2097152,16777216));
        Reject(()=>owned.Query([new(0,0,0x3f800000,0),new(1,0,0x7fc12345,0)],53,2097152,16777216));
        Need(snapshot.SequenceEqual(owned.CopyCache(16384)));Need(owned.Query([p],64,2097152,16777216)[0].Origin==TerrainConditionalFractalOrigin.CacheReloadF32);
        Reject(()=>owned.Query([new(-1,0,0,0)],53,2097152,16777216));Reject(()=>owned.Query([new(0,2,0,0)],53,2097152,16777216));
        using var cancel=new CancellationTokenSource();cancel.Cancel();Reject(()=>owned.Query([p],53,2097152,16777216,cancel.Token));Reject(()=>owned.Clear(cancel.Token));Reject(()=>owned.CopyCache(16384,cancel.Token));
        Reject(()=>owned.CopyCache(0));Reject(()=>((IList<TerrainConditionalFractalSampler.CacheCell>)snapshot)[0]=default);
        var thread=new Thread(()=>{Reject(()=>owned.Query([p],53,2097152,16777216));Reject(()=>owned.Clear());Reject(()=>owned.CopyCache(16384));Reject(()=>owned.Dispose());});thread.Start();thread.Join();
        Need(snapshot.SequenceEqual(owned.CopyCache(16384)));owned.Clear();Need(owned.CopyCache(16384).All(c=>!c.Occupied&&c.XBits==0&&c.ZBits==0&&c.ValueBits==0));
        Need(owned.Query([p],64,2097152,16777216)[0].Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss);
        Need(input.CopyEncodedBytes().SequenceEqual(original));owned.Dispose();owned.Dispose();Reject(()=>owned.Clear());Reject(()=>owned.Query([p],53,2097152,16777216));Reject(()=>owned.CopyCache(16384));Need(snapshot.Count==4&&snapshot[0].Occupied);
        return new{Cases=cases,NegativeChecks=negatives,AtomicLateFailure=true,SnapshotSurvival=true,RawInputUnchanged=true,Reentry="Structurally excluded: bounded spans and immutable raw values; no callbacks or external enumerators in operation."};
    }
}
