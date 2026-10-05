using System.Numerics;
using System.Text.Json;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class CreatureNominalTargetSpeedScenarios
{
    private static void Check(bool v) { if(!v)throw new InvalidOperationException("Nominal target invariant."); }
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var v)?v.GetInt32():d;
    private static string Fraction(CreatureNominalRetainedValue v)
    {
        if(v.Magnitude==0)return v.IsNegative?"-0/1":"0/1";
        BigInteger n=v.Magnitude,d=1;if(v.IsNegative)n=-n;if(v.Exponent>=0)n<<=v.Exponent;else d<<=-v.Exponent;
        var g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/g}/{d/g}";
    }
    private static CreatureNominalTargetSpeedResult Evaluate(JsonElement c)
    {
        var words=c.GetProperty("Direction").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
        if(words.Length!=3)throw new InvalidDataException("Three supplied direction words required.");
        var dir=new CreatureNominalTargetDirection(words[0],words[1],words[2]);
        var f=c.GetProperty("Factor");var factor=new CreatureNominalTargetFactor(f.GetProperty("Magnitude").GetUInt64(),f.GetProperty("Exponent").GetInt32(),f.GetProperty("Negative").GetBoolean());
        var result=CreatureNominalTargetSpeed.Evaluate(c.GetProperty("Run").GetUInt32(),c.GetProperty("Walk").GetUInt32(),checked((byte)c.GetProperty("Flag").GetInt32()),dir,factor,
            (CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32(),new(I(c,"MagnitudeBits",64),I(c,"ExponentCap",512),I(c,"WorkCap",65536)),
            new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
        words[0]^=0x7f800000;factor=factor with {Magnitude=0,IsNegative=!factor.IsNegative};dir=dir with {FirstBits=0};
        Check(result.Direction.FirstBits==c.GetProperty("Direction")[0].GetUInt32()&&result.Factor.Magnitude==f.GetProperty("Magnitude").GetUInt64()&&result.Factor.IsNegative==f.GetProperty("Negative").GetBoolean());
        return result;
    }
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));
        var saved=CreatureNominalTargetSpeed.Evaluate(0x40000000,0x3f800000,1,new(0x3f800000,0,0),new(1,0),CreatureNominalSpeedPrecision.NearestEven64);
        uint savedBits=saved.TargetBits;string savedValue=Fraction(saved.RetainedIntermediate);int ownership=0;var rows=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var r=Evaluate(c);ownership++;
                rows.Add(new {Name=name,Reject="None",Selection=r.Selection.ToString(),Selected=r.SelectedSpeedBits,Sum=Fraction(r.RetainedSquaredMagnitude),Magnitude=Fraction(r.RetainedMagnitude),Intermediate=Fraction(r.RetainedIntermediate),r.IntermediateBits,RetainedTarget=Fraction(r.RetainedTarget),r.TargetBits,Work=r.ReservedWork,Counts=new[]{r.SuppliedGetterWords,r.DirectionWords,r.SelectedSpeedUses,r.FactorValuesUsed,r.ArithmeticOperations,r.F32Stores},Provenance=r.Provenance.ToString()});
            }
            catch(OperationCanceledException) { rows.Add(new {Name=name,Reject="Canceled"}); }
            catch(Exception e) when(e is InvalidDataException or ArgumentOutOfRangeException or OverflowException) { rows.Add(new {Name=name,Reject="Input"}); }
            Check(saved.TargetBits==savedBits&&Fraction(saved.RetainedIntermediate)==savedValue);
        }
        for(int i=0;i<100;i++)Check(CreatureNominalTargetSpeed.Evaluate(0x40000000,0x3f800000,1,new(0x3f800000,0,0),new(1,0),CreatureNominalSpeedPrecision.NearestEven64).TargetBits==savedBits);
        Parallel.For(0,64,_=>Check(CreatureNominalTargetSpeed.Evaluate(0x40000000,0x3f800000,1,new(0x3f800000,0,0),new(1,0),CreatureNominalSpeedPrecision.NearestEven64).TargetBits==savedBits));
        Check(typeof(CreatureNominalTargetSpeedResult).GetProperties().All(p=>p.SetMethod is null));
        return new {Cases=rows,OwnershipChecks=ownership,Repeated=100,Threaded=64,PriorResultPreserved=true,NoMutableResultProperties=true};
    }
}
