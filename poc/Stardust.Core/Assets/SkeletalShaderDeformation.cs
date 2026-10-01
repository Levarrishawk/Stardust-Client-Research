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
        var skin=geometry.Skin;var shader=geometry.Shaders[shaderIndex];int count=shader.PositionIndices.Count;
        if(count>262144||shader.NormalIndices is not null&&shader.NormalIndices.Count!=count||shader.TangentIndices is not null&&shader.TangentIndices.Count!=count)
            throw new InvalidDataException("Deformed shader map count/budget mismatch.");
        var global=SkeletalJointBinding.DeformPositions(skin,palette);var positions=new Vector3[count];
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
