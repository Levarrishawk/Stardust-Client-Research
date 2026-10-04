using System.Numerics;
using System.Text.Json;
using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

internal static class CircleSquareRootChecks
{
    public static List<object> Run(string fixtures)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixtures));
        var roots=new List<object>();
        foreach(var f in doc.RootElement.GetProperty("SquareRoots").EnumerateArray())
        {
            Number n=f.GetProperty("RawBits").ValueKind==JsonValueKind.Null
                ?new Number(BigInteger.Parse(f.GetProperty("Mantissa").GetString()!),f.GetProperty("Exponent").GetInt32())
                :Number.FromBits(f.GetProperty("RawBits").GetUInt32());
            Number r=Number.SquareRoot(n,f.GetProperty("Precision").GetInt32());
            BigInteger numerator=r.Mantissa,denominator=BigInteger.One;
            if(r.Exponent>=0)numerator<<=r.Exponent;else denominator<<=-r.Exponent;
            BigInteger gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator),denominator);numerator/=gcd;denominator/=gcd;
            roots.Add(new {Numerator=numerator.ToString(),Denominator=denominator.ToString(),r.NegativeZero,Bits=r.ToFloatBits()});
        }
        return roots;
    }
}
