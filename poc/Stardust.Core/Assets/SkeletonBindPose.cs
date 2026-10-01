using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Matrices use System.Numerics row vectors: they are transposes of the recovered legacy 3x4 matrices.</summary>
public sealed record SkeletonBindPose(IReadOnlyList<Matrix4x4> LocalBind,IReadOnlyList<Matrix4x4> InverseBind);

public static class SkeletonBindPoseBuilder
{
    public static SkeletonBindPose Create(BasicSkeletonDescription skeleton,Matrix4x4? externalInverseBind=null)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        int count=skeleton.Joints.Count;if(count>2048)throw new InvalidDataException("Skeleton pose joint budget.");
        Matrix4x4 external=externalInverseBind??Matrix4x4.Identity;ValidateAffine(external);
        var local=new Matrix4x4[count];var inverse=new Matrix4x4[count];int roots=0;
        for(int i=0;i<count;i++)
        {
            SkeletonJoint joint=skeleton.Joints[i];
            if(joint.Parent< -1||joint.Parent>=i)throw new InvalidDataException("Invalid pose parent topology.");
            if(joint.Parent==-1)roots++;
            Quaternion Q(ScalarFirstQuaternion value)
            {
                double norm=(double)value.W*value.W+(double)value.X*value.X+(double)value.Y*value.Y+(double)value.Z*value.Z;
                if(!double.IsFinite(norm)||Math.Abs(norm-1)>.0001)throw new InvalidDataException("Invalid pose quaternion.");
                return new(value.X,value.Y,value.Z,value.W);
            }
            Quaternion q=Q(joint.Rpst)*Q(joint.Bpro)*Q(joint.Rpre);
            // Recovered conversion's near-positive-identity shortcut; no normalization or sign canonicalization.
            local[i]=(double)q.W+1.0000000272452012e-27>=1?Matrix4x4.Identity:Matrix4x4.CreateFromQuaternion(q);
            local[i].Translation=joint.Bptr;ValidateAffine(local[i]);
            Matrix4x4 inverted=RigidInverse(local[i]);
            // Transposing legacy localInverse * parent/external reverses the System.Numerics product.
            inverse[i]=(joint.Parent<0?external:inverse[joint.Parent])*inverted;ValidateAffine(inverse[i]);
        }
        if(count>0&&roots!=1)throw new InvalidDataException("Nonempty pose requires one root.");
        return new(Array.AsReadOnly(local),Array.AsReadOnly(inverse));
    }
    public static Matrix4x4 RigidInverse(Matrix4x4 matrix)
    {
        ValidateAffine(matrix);
        Matrix4x4 result=Matrix4x4.Transpose(matrix);result.M14=0;result.M24=0;result.M34=0;result.M44=1;
        result.Translation=Vector3.TransformNormal(-matrix.Translation,result);ValidateAffine(result);return result;
    }
    private static void ValidateAffine(Matrix4x4 matrix)
    {
        float[] values={matrix.M11,matrix.M12,matrix.M13,matrix.M14,matrix.M21,matrix.M22,matrix.M23,matrix.M24,matrix.M31,matrix.M32,matrix.M33,matrix.M34,matrix.M41,matrix.M42,matrix.M43,matrix.M44};
        if(values.Any(v=>!float.IsFinite(v))||matrix.M14!=0||matrix.M24!=0||matrix.M34!=0||matrix.M44!=1||Math.Abs(matrix.M41)>1e6||Math.Abs(matrix.M42)>1e6||Math.Abs(matrix.M43)>1e6)
            throw new InvalidDataException("Invalid pose affine matrix.");
        Vector3 x=new(matrix.M11,matrix.M12,matrix.M13),y=new(matrix.M21,matrix.M22,matrix.M23),z=new(matrix.M31,matrix.M32,matrix.M33);
        if(Math.Abs(x.LengthSquared()-1)>.002||Math.Abs(y.LengthSquared()-1)>.002||Math.Abs(z.LengthSquared()-1)>.002||Math.Abs(Vector3.Dot(x,y))>.002||Math.Abs(Vector3.Dot(x,z))>.002||Math.Abs(Vector3.Dot(y,z))>.002||Vector3.Dot(Vector3.Cross(x,y),z)<.998)
            throw new InvalidDataException("Pose matrix is not bounded right-handed rigid data.");
    }
}
