using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record SkeletalDeformedShader(IReadOnlyList<Vector3> Positions,IReadOnlyList<Vector3>? Normals,
    IReadOnlyList<Vector4>? Tangents);

/// <summary>Complete PIDX-owned influences; NIDX/DOT3 select authored directions independently.</summary>
public static class SkeletalShaderDeformation
{
    public static SkeletalDeformedShader Create(SkeletalMeshGeometryDescription geometry,int shaderIndex,
        IReadOnlyList<Matrix4x4> palette)
    {
        ArgumentNullException.ThrowIfNull(geometry);ArgumentNullException.ThrowIfNull(palette);
        if(shaderIndex<0||shaderIndex>=geometry.Shaders.Count)throw new ArgumentOutOfRangeException(nameof(shaderIndex));
        ValidateShape(geometry.Shaders[shaderIndex]);
        return MapShader(geometry,geometry.Shaders[shaderIndex],palette,SkeletalJointBinding.DeformPositions(geometry.Skin,palette));
    }

    /// <summary>Validate the complete batch, then deform global positions exactly once for this mesh.</summary>
    public static IReadOnlyList<SkeletalDeformedShader> CreateAll(SkeletalMeshGeometryDescription geometry,IReadOnlyList<Matrix4x4> palette)
    {
        ArgumentNullException.ThrowIfNull(geometry);ArgumentNullException.ThrowIfNull(palette);
        if(geometry.Shaders.Count>2048)throw new InvalidDataException("Deformation shader batch budget.");
        long total=0;
        foreach(var shader in geometry.Shaders){ValidateShape(shader);total+=shader.PositionIndices.Count;if(total>1048576)throw new InvalidDataException("Deformation vertex batch budget.");}
        var global=SkeletalJointBinding.DeformPositions(geometry.Skin,palette);var result=new SkeletalDeformedShader[geometry.Shaders.Count];
        for(int i=0;i<result.Length;i++)result[i]=MapShader(geometry,geometry.Shaders[i],palette,global);
        return Array.AsReadOnly(result);
    }

    private static void ValidateShape(SkeletalShaderGeometry shader)
    {
        int count=shader.PositionIndices.Count;
        if(count>262144||shader.NormalIndices is not null&&shader.NormalIndices.Count!=count||shader.TangentIndices is not null&&shader.TangentIndices.Count!=count)
            throw new InvalidDataException("Deformed shader map count/budget mismatch.");
    }
    private static SkeletalDeformedShader MapShader(SkeletalMeshGeometryDescription geometry,SkeletalShaderGeometry shader,
        IReadOnlyList<Matrix4x4> palette,IReadOnlyList<Vector3> global)
    {
        var skin=geometry.Skin;int count=shader.PositionIndices.Count;var positions=new Vector3[count];
        Vector3[]? normals=shader.NormalIndices is null?null:new Vector3[count];Vector4[]? tangents=shader.TangentIndices is null?null:new Vector4[count];
        for(int vertex=0;vertex<count;vertex++)
        {
            uint position=shader.PositionIndices[vertex];if(position>=global.Count)throw new InvalidDataException("Deformed position map out of range.");
            positions[vertex]=global[(int)position];var range=skin.WeightRanges[(int)position];
            Vector3 Direction(Vector3 direction)
            {
                if(!float.IsFinite(direction.X)||!float.IsFinite(direction.Y)||!float.IsFinite(direction.Z)||Math.Abs(direction.X)>1e6||Math.Abs(direction.Y)>1e6||Math.Abs(direction.Z)>1e6)throw new InvalidDataException("Invalid deformation direction.");
                Vector3 result=Vector3.Zero;
                for(int i=0;i<range.Count;i++){var weight=skin.Weights[range.Start+i];result+=Vector3.TransformNormal(direction,palette[(int)weight.TransformIndex])*weight.Weight;}
                if(!float.IsFinite(result.X)||!float.IsFinite(result.Y)||!float.IsFinite(result.Z))throw new InvalidDataException("Invalid deformed direction result.");return result;
            }
            if(normals is not null)
            {
                uint index=shader.NormalIndices![vertex];if(index>=skin.Normals.Count)throw new InvalidDataException("Deformed normal map out of range.");normals[vertex]=Direction(skin.Normals[(int)index]);
            }
            if(tangents is not null)
            {
                uint index=shader.TangentIndices![vertex];if(index>=geometry.Tangents.Count)throw new InvalidDataException("Deformed tangent map out of range.");
                Vector4 raw=geometry.Tangents[(int)index];if(!float.IsFinite(raw.W))throw new InvalidDataException("Invalid tangent fourth component.");
                tangents[vertex]=new(Direction(new(raw.X,raw.Y,raw.Z)),raw.W);
            }
        }
        return new(Array.AsReadOnly(positions),normals is null?null:Array.AsReadOnly(normals),tangents is null?null:Array.AsReadOnly(tangents));
    }
}
