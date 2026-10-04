using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainFamilyHeightFractalScenarios
{
    private static void Check(bool condition){if(!condition)throw new InvalidOperationException("Route ownership invariant failed.");}
    private static int I(JsonElement c,string name,int fallback)=>c.TryGetProperty(name,out var v)?v.GetInt32():fallback;
    private static uint U(JsonElement c,string name)=>c.GetProperty(name).GetUInt32();
    private static TerrainFamilyHeightFractalReceipt Call(TerrainFractalFamilySet set,byte[] rule,JsonElement c)
    {
        var request=new TerrainFamilyHeightFractalRequest(I(c,"Width",2),I(c,"Rows",2),I(c,"Column",0),I(c,"Row",0),U(c,"X"),U(c,"Z"),U(c,"Height"),U(c,"Weight"));
        var controls=new TerrainFamilyHeightFractalControls(I(c,"Table",24),I(c,"Construction",24),I(c,"Query",24),(TerrainHeightArithmeticPrecision)I(c,"Consumer",24));
        var limits=new TerrainFamilyHeightFractalLimits(I(c,"MaximumRuleBytes",65536),I(c,"MaximumOwnedBytes",1048576),I(c,"MaximumWork",1048576));
        return TerrainFamilyHeightFractal.Evaluate(set,rule,request,controls,limits,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
    }
    private static string Fraction(TerrainSuppliedFractalSample sample)
    {
        if(sample.Magnitude==0)return sample.NegativeZero?"-0/1":"0/1";
        BigInteger numerator=sample.Magnitude,denominator=BigInteger.One;
        if(sample.IsNegative)numerator=-numerator;
        if(sample.Exponent>=0)numerator<<=sample.Exponent;else denominator<<=-sample.Exponent;
        var divisor=BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator),denominator);
        return $"{numerator/divisor}/{denominator/divisor}";
    }
    public static object Run(string fixture)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixture,Encoding.UTF8));var cases=new List<object>();
        foreach(var c in document.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var set=TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("GroupHex").GetString()!));
                var receipt=Call(set,Convert.FromHexString(c.GetProperty("RuleHex").GetString()!),c);
                Check(ReferenceEquals(receipt.Raw,receipt.Family.Raw)&&ReferenceEquals(receipt.Sample,receipt.Affector.Sample));
                cases.Add(new{Name=name,Reject="None",FamilyId=receipt.Family.Id,SourceSha256=receipt.FamilySetSha256,Origin=receipt.Origin.ToString(),Retained=Fraction(receipt.Sample),Projection=receipt.Sample.ProjectionBits,Result=receipt.Affector.Consumer.ResultHeightBits,receipt.AccountedBytes,receipt.AccountedWork});
            }
            catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){cases.Add(new{Name=name,Reject="Input"});}
        }
        var first=document.RootElement[0];var group=Convert.FromHexString(first.GetProperty("GroupHex").GetString()!);var familySet=TerrainFractalFamilySet.Decode(group);
        var ruleBytes=Convert.FromHexString(first.GetProperty("RuleHex").GetString()!);var originalRule=(byte[])ruleBytes.Clone();var a=Call(familySet,ruleBytes,first);int checks=0;
        Array.Fill(group,(byte)0);Check(familySet.SourceSha256==a.FamilySetSha256);checks++;
        Array.Fill(ruleBytes,(byte)0);Check(a.Rule.CopyEncodedBytes().SequenceEqual(originalRule));checks++;
        var exported=a.Rule.CopyEncodedBytes();Array.Fill(exported,(byte)0);Check(a.Rule.CopyEncodedBytes().SequenceEqual(originalRule));checks++;
        var names=a.Family.CopyNameBytes();Array.Fill(names,(byte)0xff);Check(!a.Family.CopyNameBytes().SequenceEqual(names));checks++;
        var again=Call(familySet,originalRule,first);Check(a.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&again.Origin==a.Origin);checks++;
        Check(!ReferenceEquals(a.Sample,again.Sample)&&Fraction(a.Sample)==Fraction(again.Sample)&&a.Affector.Consumer.ResultHeightBits==again.Affector.Consumer.ResultHeightBits);checks++;
        Check(ReferenceEquals(familySet.Find(a.Family.Id),a.Family)&&ReferenceEquals(a.Raw,a.Family.Raw));checks++;
        Exception? threadError=null;TerrainFamilyHeightFractalReceipt? threaded=null;
        var thread=new Thread(()=>{try{threaded=Call(familySet,originalRule,first);}catch(Exception ex){threadError=ex;}});thread.Start();thread.Join();
        Check(threadError is null&&threaded is not null&&threaded.Affector.Consumer.ResultHeightBits==a.Affector.Consumer.ResultHeightBits);checks++;
        Check(threaded!.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&!ReferenceEquals(threaded.Sample,a.Sample));checks++;
        Check(a.Rule.CopyEncodedBytes().SequenceEqual(originalRule)&&Fraction(a.Sample)==Fraction(again.Sample));checks++;
        return new{Cases=cases,OwnershipChecks=checks};
    }
}
