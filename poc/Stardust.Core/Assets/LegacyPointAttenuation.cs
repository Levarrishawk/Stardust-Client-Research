using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Recovered positive-distance source equation; singular source behavior is deliberately outside this boundary.</summary>
public static class LegacyPointAttenuation
{
    public static float Evaluate(Vector3 coefficients,float distanceSquared)
    {
        if(!Finite(coefficients)||coefficients.X<0||coefficients.Y<0||coefficients.Z<0||!float.IsFinite(distanceSquared)||distanceSquared<=0)
            throw new InvalidDataException("Finite nonnegative point coefficients and positive distance required.");
        float distance=MathF.Sqrt(distanceSquared),denominator=coefficients.X+coefficients.Y*distance+coefficients.Z*distanceSquared;
        float value=1/denominator;
        if(!float.IsFinite(denominator)||denominator<=0||!float.IsFinite(value))throw new InvalidDataException("Singular/nonfinite point attenuation.");
        return value;
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
}
