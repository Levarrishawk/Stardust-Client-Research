using System.Numerics;
using System.Text.Json;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class CreatureNominalSpeedTransitionScenarios
{
    private static void Check(bool value) { if(!value)throw new InvalidOperationException("Nominal speed scenario invariant."); }
    private static string Fraction(CreatureNominalRetainedValue value)
    {
        if(value.Magnitude==0)return value.IsNegative?"-0/1":"0/1";
        BigInteger n=value.Magnitude,d=1;if(value.IsNegative)n=-n;
        if(value.Exponent>=0)n<<=value.Exponent;else d<<=-value.Exponent;
        var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";
    }
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var p)?p.GetInt32():fallback;
    private static CreatureNominalSpeedResult Evaluate(JsonElement c)
    {
        CreatureNominalRate? rate=null;
        if(c.TryGetProperty("Rate",out var r)&&r.ValueKind!=JsonValueKind.Null)
            rate=new(r.GetProperty("Magnitude").GetUInt64(),r.GetProperty("Exponent").GetInt32(),r.GetProperty("Negative").GetBoolean());
        var limits=new CreatureNominalSpeedLimits(I(c,"MagnitudeBits",64),I(c,"ExponentCap",512),I(c,"WorkCap",8192));
        return CreatureNominalSpeedTransition.Evaluate(c.GetProperty("Current").GetUInt32(),c.GetProperty("Target").GetUInt32(),c.GetProperty("Seconds").GetUInt32(),
            (CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32(),rate,limits,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
    }
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));
        var saved=CreatureNominalSpeedTransition.Evaluate(0,0x3f800000,0x3f000000,CreatureNominalSpeedPrecision.NearestEven64,new(1,0));
        uint priorBits=saved.StoredBits;string priorRetained=Fraction(saved.RetainedCandidate);var output=new List<object>();int rejected=0;
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var result=Evaluate(c);
                output.Add(new {Name=name,Reject="None",result.StoredBits,result.CandidateBits,Retained=Fraction(result.RetainedCandidate),Step=result.RetainedStep.HasValue?Fraction(result.RetainedStep.Value):null,
                    Branch=result.Branch.ToString(),result.Clamped,Work=result.ReservedWork,result.RatesUsed,Provenance=result.RateProvenance.ToString()});
                if(result.Branch==CreatureNominalSpeedBranch.Equal)Check(result.RatesUsed==0&&result.RetainedStep is null&&result.SuppliedRate is null&&result.StoredBits==c.GetProperty("Current").GetUInt32());
                else Check(result.RatesUsed==1&&result.SuppliedRate.HasValue);
            }
            catch(OperationCanceledException) { output.Add(new {Name=name,Reject="Canceled"});rejected++; }
            catch(Exception e) when(e is InvalidDataException or ArgumentOutOfRangeException or OverflowException)
            { output.Add(new {Name=name,Reject="Input"});rejected++; }
            Check(saved.StoredBits==priorBits&&Fraction(saved.RetainedCandidate)==priorRetained);
        }
        for(int i=0;i<100;i++)Check(CreatureNominalSpeedTransition.Evaluate(0,0x3f800000,0x3f000000,CreatureNominalSpeedPrecision.NearestEven64,new(1,0)).StoredBits==priorBits);
        Parallel.For(0,64,_=>Check(CreatureNominalSpeedTransition.Evaluate(0,0x3f800000,0x3f000000,CreatureNominalSpeedPrecision.NearestEven64,new(1,0)).StoredBits==priorBits));
        Check(typeof(CreatureNominalSpeedResult).GetProperties().All(p=>p.SetMethod is null));
        Check(rejected>0&&saved.StoredBits==priorBits&&Fraction(saved.RetainedCandidate)==priorRetained);
        return new {Cases=output,Repeated=100,Threaded=64,PriorResultPreserved=true,NoMutableResultProperties=true};
    }
}
