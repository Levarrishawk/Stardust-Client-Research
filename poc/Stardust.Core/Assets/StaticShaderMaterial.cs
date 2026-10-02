using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Serialized MATL order is ambient, diffuse, emissive, specular ARGB then power.</summary>
public sealed record StaticShaderMaterial(Vector4 AmbientRgba,Vector4 DiffuseRgba,Vector4 EmissiveRgba,Vector4 SpecularRgba,float SpecularPower)
{
    public IReadOnlyList<Vector4> VertexConstants()=>Array.AsReadOnly(new[]{DiffuseRgba,AmbientRgba,SpecularRgba,EmissiveRgba,new Vector4(SpecularPower,0,0,0)});
    public Vector4 PixelSpecularConstant=>SpecularRgba;
    public Vector4 PixelPowerConstant=>new(SpecularPower,0,0,0);
}

public static class StaticShaderMaterialDecoder
{
    public static StaticShaderMaterial Decode(IReadOnlyList<float> serialized)
    {
        ArgumentNullException.ThrowIfNull(serialized);
        if(serialized.Count!=17||serialized.Any(v=>!float.IsFinite(v)))throw new InvalidDataException("Invalid MATL scalar record.");
        Vector4 Color(int offset)=>new(serialized[offset+1],serialized[offset+2],serialized[offset+3],serialized[offset]);
        return new(Color(0),Color(4),Color(8),Color(12),serialized[16]);
    }
    /// <summary>Backend fallback for a nonzero selected material tag missing from the shader map.</summary>
    public static StaticShaderMaterial MissingSelectedTagFallback=>new(Vector4.One,Vector4.One,new(0,0,0,1),new(0,0,0,1),0);
}
