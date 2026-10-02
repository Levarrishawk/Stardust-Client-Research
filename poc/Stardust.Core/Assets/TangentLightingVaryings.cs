using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record TangentLightingVaryings(Vector3 Light,Vector3 HalfAngle);

/// <summary>Recovered selected vertex-source tangent projection; raster weights supplied explicitly.</summary>
public static class TangentLightingVertexEvaluator
{
    public static TangentLightingVaryings Evaluate(Vector3 position,Vector3 normal,Vector4 tangent,Vector3 towardLight,Vector3 camera)
    {
        Validate(position);Validate(normal);Validate(new(tangent.X,tangent.Y,tangent.Z));Validate(towardLight);Validate(camera);
        if(!float.IsFinite(tangent.W)||Math.Abs(tangent.W)!=1||normal.LengthSquared()<1e-12f)throw new InvalidDataException("Invalid source tangent lighting basis.");
        var t=new Vector3(tangent.X,tangent.Y,tangent.Z);var b=Vector3.Cross(normal,t)*tangent.W;
        var view=Unit(camera-position);var half=Unit(view+towardLight);
        Vector3 Project(Vector3 v)=>new(Vector3.Dot(v,t),Vector3.Dot(v,b),Vector3.Dot(v,normal));
        var result=new TangentLightingVaryings(Project(towardLight),Project(half));Validate(result.Light);Validate(result.HalfAngle);return result;
    }
    public static TangentLightingVaryings Interpolate(IReadOnlyList<TangentLightingVaryings> vertices,Vector3 screenWeights,Vector3 clipW)
    {
        ArgumentNullException.ThrowIfNull(vertices);Validate(screenWeights);Validate(clipW);
        if(vertices.Count!=3||screenWeights.X<0||screenWeights.Y<0||screenWeights.Z<0||Math.Abs(screenWeights.X+screenWeights.Y+screenWeights.Z-1)>1e-5f||clipW.X<=1e-6f||clipW.Y<=1e-6f||clipW.Z<=1e-6f)throw new InvalidDataException("Unsupported perspective interpolation inputs.");
        var weights=screenWeights/clipW;weights/=weights.X+weights.Y+weights.Z;Vector3 light=Vector3.Zero,half=Vector3.Zero;
        for(int i=0;i<3;i++){var v=vertices[i]??throw new InvalidDataException("Null tangent varying.");Validate(v.Light);Validate(v.HalfAngle);float weight=i==0?weights.X:i==1?weights.Y:weights.Z;light+=v.Light*weight;half+=v.HalfAngle*weight;}
        Validate(light);Validate(half);return new(light,half);
    }
    // Explicit modern finite-zero policy; original normalize(0) device behavior is not inferred.
    private static Vector3 Unit(Vector3 v)=>v/Math.Max(v.Length(),1e-6f);
    private static void Validate(Vector3 value){if(!float.IsFinite(value.X)||!float.IsFinite(value.Y)||!float.IsFinite(value.Z)||Math.Abs(value.X)>1e6||Math.Abs(value.Y)>1e6||Math.Abs(value.Z)>1e6)throw new InvalidDataException("Bounded finite tangent lighting vector required.");}
}
