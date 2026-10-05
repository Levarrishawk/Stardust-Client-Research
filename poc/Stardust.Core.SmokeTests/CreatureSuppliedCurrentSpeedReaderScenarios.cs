using System.Numerics;
using System.Text.Json;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class CreatureSuppliedCurrentSpeedReaderScenarios
{
    private static void Check(bool v) { if(!v)throw new InvalidOperationException("Supplied reader invariant."); }
    private static string Fraction(CreatureNominalRetainedValue v)
    {
        if(v.Magnitude==0)return v.IsNegative?"-0/1":"0/1";
        BigInteger n=v.Magnitude,d=1;if(v.IsNegative)n=-n;if(v.Exponent>=0)n<<=v.Exponent;else d<<=-v.Exponent;
        var g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/g}/{d/g}";
    }
    private static int Get(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    public static object Run(string path)
    {
        var policy=CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly;var precision=CreatureNominalSpeedPrecision.NearestEven64;
        var prior=CreatureSuppliedCurrentSpeedReader.Evaluate(0x40000000,0x3f800000,0x3f800000,policy,precision);
        var priorProduct=prior.RetainedModifierProduct;var priorSpeed=prior.RetainedSpeed;
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var output=new List<object>();int owned=0;
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                uint b=c.GetProperty("Base").GetUInt32(),first=c.GetProperty("First").GetUInt32(),second=c.GetProperty("Second").GetUInt32();
                var p=(CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32();var selected=(CreatureSuppliedCurrentSpeedPolicy)Get(c,"Policy",1);
                var limits=new CreatureSuppliedCurrentSpeedLimits(Get(c,"MaximumCopyBytes",128),Get(c,"MaximumWork",8192));
                var result=CreatureSuppliedCurrentSpeedReader.Evaluate(b,first,second,selected,p,limits,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
                var reservation=CreatureSuppliedCurrentSpeedReader.GetRequiredReservations(b,first,second,selected,p,limits);
                Check(result.ReservedWork==reservation.Work&&result.ReservedCopyBytes==reservation.CopyBytes);
                b=first=second=0;Check(result.BaseBits==c.GetProperty("Base").GetUInt32()&&result.FirstModifierSnapshotBits==c.GetProperty("First").GetUInt32()&&result.SecondModifierBits==c.GetProperty("Second").GetUInt32());owned++;
                output.Add(new {Name=name,Reject="None",Base=result.BaseBits,First=result.FirstModifierSnapshotBits,Second=result.SecondModifierBits,Precision=(int)result.Precision,Modifiers=Fraction(result.RetainedModifierProduct),Speed=Fraction(result.RetainedSpeed),result.ReturnBits,Work=result.ReservedWork,Copy=result.ReservedCopyBytes,Counts=new[]{result.SuppliedWords,result.Multiplications,result.CallerF32Stores},Policy=result.Policy.ToString(),Provenance=result.Provenance.ToString()});
            }
            catch(OperationCanceledException){output.Add(new {Name=name,Reject="Canceled"});}
            catch(Exception e) when(e is InvalidDataException or ArgumentOutOfRangeException or OverflowException){output.Add(new {Name=name,Reject="Input"});}
            Check(prior.ReturnBits==0x40000000&&prior.RetainedModifierProduct==priorProduct&&prior.RetainedSpeed==priorSpeed);
        }
        for(int i=0;i<100;i++)Check(CreatureSuppliedCurrentSpeedReader.Evaluate(0x40000000,0x3f800000,0x3f800000,policy,precision).ReturnBits==prior.ReturnBits);
        Parallel.For(0,64,_=>Check(CreatureSuppliedCurrentSpeedReader.Evaluate(0x40000000,0x3f800000,0x3f800000,policy,precision).ReturnBits==prior.ReturnBits));
        var run=CreatureSuppliedCurrentSpeedReader.Evaluate(0x40000000,0x3f800000,0x3f800000,policy,precision);
        var walk=CreatureSuppliedCurrentSpeedReader.Evaluate(0x3f800000,0x3f800000,0x3f800000,policy,precision);
        Check(!ReferenceEquals(run,walk)&&run.Multiplications+walk.Multiplications==4&&run.CallerF32Stores+walk.CallerF32Stores==2);
        bool immutable=typeof(CreatureSuppliedCurrentSpeedResult).GetProperties().All(v=>v.SetMethod is null);Check(immutable);
        return new {Cases=output,OwnershipChecks=owned,Repeated=100,Threaded=64,PriorResultPreserved=true,NoMutableResultProperties=immutable,IndependentReaderMultiplications=4,IndependentReaderStores=2};
    }
}
