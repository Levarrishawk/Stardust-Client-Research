using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record SkeletalPixelLighting(Vector3 TangentLightDirection,Vector3 TangentHalfAngle,
    Vector3 Dot3DiffuseColor,Vector3 Dot3SpecularColor,Vector3 VertexDiffuse,Vector3 VertexSpecular,
    bool BloomEnabled=false,bool AlphaFadeEnabled=false,float AlphaFadeOpacity=1);

/// <summary>Recovered selected compressed-normal PS20 pixel equation with explicit supplied lighting.</summary>
public static class SkeletalSpecularEvaluator
{
    public static Vector4 Evaluate(Vector4 diffuseSample,Vector4 compressedNormalSample,float specularMask,
        SkeletalAlbedoFactors factors,StaticShaderMaterial material,SkeletalPixelLighting lighting)
    {
        ArgumentNullException.ThrowIfNull(material);ArgumentNullException.ThrowIfNull(lighting);
        var albedo=SkeletalAlbedoEvaluator.Evaluate(diffuseSample,factors);
        var normal=SkeletalCompressedNormalEvaluator.Decode(compressedNormalSample);
        Vector3[] vectors={lighting.TangentLightDirection,lighting.TangentHalfAngle,lighting.Dot3DiffuseColor,lighting.Dot3SpecularColor,lighting.VertexDiffuse,lighting.VertexSpecular};
        if(vectors.Any(v=>!Finite(v))||!Finite(new(material.SpecularRgba.X,material.SpecularRgba.Y,material.SpecularRgba.Z))||
            !float.IsFinite(specularMask)||specularMask<0||specularMask>1||!float.IsFinite(material.SpecularPower)||material.SpecularPower<=0||material.SpecularPower>4096||
            !float.IsFinite(lighting.AlphaFadeOpacity)||lighting.AlphaFadeOpacity<0||lighting.AlphaFadeOpacity>1||lighting.TangentHalfAngle.LengthSquared()<1e-12f)
            throw new InvalidDataException("Unsupported specular evaluation input.");
        float diffuseIntensity=Math.Clamp(Vector3.Dot(lighting.TangentLightDirection,normal),0,1);
        var diffuse=Clamp(lighting.Dot3DiffuseColor*diffuseIntensity+lighting.VertexDiffuse);
        var half=Vector3.Normalize(lighting.TangentHalfAngle);
        // Compiled program has no N.L gate on the specular term.
        float intensity=MathF.Pow(Math.Clamp(Vector3.Dot(half,normal),0,1),material.SpecularPower);
        var specular=(lighting.Dot3SpecularColor*new Vector3(material.SpecularRgba.X,material.SpecularRgba.Y,material.SpecularRgba.Z)*intensity+lighting.VertexSpecular)*specularMask;
        var rgb=albedo*diffuse+specular*(lighting.BloomEnabled?.5f:1);
        float alpha=Vector3.Dot(specular,new(.3f,.59f,.11f))*(lighting.BloomEnabled?.65f:1);
        if(lighting.AlphaFadeEnabled)alpha=lighting.AlphaFadeOpacity;
        if(!Finite(rgb)||!float.IsFinite(alpha))throw new InvalidDataException("Specular evaluation overflow.");
        return new(rgb,alpha);
    }
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z);
    private static Vector3 Clamp(Vector3 value)=>Vector3.Clamp(value,Vector3.Zero,Vector3.One);
}
