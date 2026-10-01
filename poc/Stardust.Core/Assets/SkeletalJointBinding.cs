using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record BoundSkeletonJoint(string SkeletonPath,int SourceJoint,int Parent,string Name,
    Matrix4x4 LocalBind,Matrix4x4 WorldBind,Matrix4x4 InverseBind);
public sealed record SkeletalJointBinding(IReadOnlyList<BoundSkeletonJoint> Joints)
{
    /// <summary>Compose explicit local poses in the already validated attached hierarchy.</summary>
    public IReadOnlyList<Matrix4x4> CreateWorldPose(IReadOnlyList<Matrix4x4> localPose)
    {
        ArgumentNullException.ThrowIfNull(localPose);
        if(localPose.Count!=Joints.Count||Joints.Count>8192)throw new InvalidDataException("Local pose count/budget mismatch.");
        var result=new Matrix4x4[Joints.Count];
        for(int i=0;i<result.Length;i++)
        {
            int parent=Joints[i].Parent;if(parent< -1||parent>=i)throw new InvalidDataException("Invalid bound pose topology.");
            SkeletonBindPoseBuilder.RigidInverse(localPose[i]);result[i]=localPose[i]*(parent<0?Matrix4x4.Identity:result[parent]);SkeletonBindPoseBuilder.RigidInverse(result[i]);
        }
        return Array.AsReadOnly(result);
    }
    /// <summary>Resolve only within the mesh's declared skeleton paths; ambiguous folded names reject.</summary>
    public IReadOnlyList<int> Resolve(SkeletalMeshSkinSourceDescription mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if(mesh.SkeletonPaths.Count>64||mesh.TransformNames.Count>2048||mesh.SkeletonPaths.Distinct(StringComparer.Ordinal).Count()!=mesh.SkeletonPaths.Count)
            throw new InvalidDataException("Invalid mesh binding resource set.");
        foreach(string path in mesh.SkeletonPaths)if(!Joints.Any(j=>j.SkeletonPath==path))throw new InvalidDataException("Mesh skeleton absent from binding.");
        var allowed=new HashSet<string>(mesh.SkeletonPaths,StringComparer.Ordinal);var result=new int[mesh.TransformNames.Count];
        for(int i=0;i<result.Length;i++)
        {
            string name=mesh.TransformNames[i];if(name.Length==0||name.Length>1024||name.Any(c=>c<32||c>126))throw new InvalidDataException("Invalid mesh binding name.");
            int found=-1;
            for(int joint=0;joint<Joints.Count;joint++)if(allowed.Contains(Joints[joint].SkeletonPath)&&string.Equals(Joints[joint].Name,name,StringComparison.OrdinalIgnoreCase))
            {if(found>=0)throw new InvalidDataException("Ambiguous mesh binding name.");found=joint;}
            if(found<0)throw new InvalidDataException("Mesh transform absent from binding.");result[i]=found;
        }
        return Array.AsReadOnly(result);
    }

    /// <summary>Modern rigid linear-blend palette; world poses are explicit inputs, not original animation output.</summary>
    public IReadOnlyList<Matrix4x4> CreatePalette(IReadOnlyList<Matrix4x4> worldPose,IReadOnlyList<int> transformJoints)
    {
        ArgumentNullException.ThrowIfNull(worldPose);ArgumentNullException.ThrowIfNull(transformJoints);
        if(worldPose.Count!=Joints.Count||transformJoints.Count>2048)throw new InvalidDataException("Pose/binding size mismatch.");
        foreach(var pose in worldPose)SkeletonBindPoseBuilder.RigidInverse(pose);
        var result=new Matrix4x4[transformJoints.Count];
        for(int i=0;i<result.Length;i++)
        {
            int joint=transformJoints[i];if(joint<0||joint>=Joints.Count)throw new InvalidDataException("Pose binding index out of range.");
            result[i]=Joints[joint].InverseBind*worldPose[joint];SkeletonBindPoseBuilder.RigidInverse(result[i]);
        }
        return Array.AsReadOnly(result);
    }

    /// <summary>Modern diagnostic position deformation. Weights retain authored order/value; no truncation or normalization.</summary>
    public static IReadOnlyList<Vector3> DeformPositions(SkeletalMeshSkinSourceDescription mesh,IReadOnlyList<Matrix4x4> palette)
    {
        ArgumentNullException.ThrowIfNull(mesh);ArgumentNullException.ThrowIfNull(palette);
        if(palette.Count!=mesh.TransformNames.Count||mesh.Positions.Count>262144||mesh.Weights.Count>1048576||mesh.WeightRanges.Count!=mesh.Positions.Count)throw new InvalidDataException("Deformation array budget/size mismatch.");
        foreach(var matrix in palette)SkeletonBindPoseBuilder.RigidInverse(matrix);
        var result=new Vector3[mesh.Positions.Count];int cursor=0;
        for(int i=0;i<result.Length;i++)
        {
            var point=mesh.Positions[i];if(!float.IsFinite(point.X)||!float.IsFinite(point.Y)||!float.IsFinite(point.Z)||Math.Abs(point.X)>1e6||Math.Abs(point.Y)>1e6||Math.Abs(point.Z)>1e6)throw new InvalidDataException("Invalid deformation position.");
            var range=mesh.WeightRanges[i];if(range.Start!=cursor||range.Count is <1 or >64||range.Count>mesh.Weights.Count-cursor)throw new InvalidDataException("Invalid deformation weight range.");
            double sum=0;Vector3 transformed=Vector3.Zero;
            for(int j=0;j<range.Count;j++)
            {
                var weight=mesh.Weights[cursor++];if(weight.TransformIndex>=palette.Count||!float.IsFinite(weight.Weight)||weight.Weight<0||weight.Weight>1)throw new InvalidDataException("Invalid deformation weight.");
                sum+=weight.Weight;transformed+=Vector3.Transform(point,palette[(int)weight.TransformIndex])*weight.Weight;
            }
            if(Math.Abs(sum-1)>.0001||!float.IsFinite(transformed.X)||!float.IsFinite(transformed.Y)||!float.IsFinite(transformed.Z))throw new InvalidDataException("Invalid deformation weight sum/result.");
            result[i]=transformed;
        }
        if(cursor!=mesh.Weights.Count)throw new InvalidDataException("Unused deformation weights.");
        return Array.AsReadOnly(result);
    }
}

public static class SkeletalJointBindingBuilder
{
    /// <summary>Explicit per-path level selection; no automatic attached-skeleton LOD fallback.</summary>
    public static SkeletalJointBinding Create(SkeletalAppearanceDescription appearance,
        IReadOnlyDictionary<string,SkeletonTemplateDescription> skeletons,IReadOnlyDictionary<string,int> levels)
    {
        ArgumentNullException.ThrowIfNull(appearance);ArgumentNullException.ThrowIfNull(skeletons);ArgumentNullException.ThrowIfNull(levels);
        if(appearance.Skeletons.Count is <1 or >64||levels.Count!=appearance.Skeletons.Count)throw new InvalidDataException("Invalid appearance skeleton selection.");
        var joints=new List<BoundSkeletonJoint>();var paths=new HashSet<string>(StringComparer.Ordinal);int roots=0;
        foreach(var reference in appearance.Skeletons)
        {
            if(!paths.Add(reference.Path)||!levels.TryGetValue(reference.Path,out int level)||level<0||level>3||!skeletons.TryGetValue(reference.Path,out var source))throw new InvalidDataException("Missing/duplicate skeleton selection.");
            var exposed=source.ExposedLevels();if(level>=exposed.Count)throw new InvalidDataException("Selected skeleton level unavailable.");
            var basic=exposed[level];if(basic.Joints.Count==0||joints.Count+basic.Joints.Count>8192)throw new InvalidDataException("Binding joint budget/empty skeleton.");
            int attachment=-1;
            if(reference.AttachmentTransform.Length==0){if(++roots!=1)throw new InvalidDataException("Binding requires one unattached skeleton.");}
            else
            {
                for(int i=0;i<joints.Count;i++)if(string.Equals(joints[i].Name,reference.AttachmentTransform,StringComparison.OrdinalIgnoreCase))
                {if(attachment>=0)throw new InvalidDataException("Ambiguous skeleton attachment.");attachment=i;}
                if(attachment<0)throw new InvalidDataException("Skeleton attachment must precede child.");
            }
            int start=joints.Count;
            var pose=SkeletonBindPoseBuilder.Create(basic,attachment<0?null:joints[attachment].InverseBind);
            for(int i=0;i<basic.Joints.Count;i++)
            {
                var joint=basic.Joints[i];int parent=joint.Parent<0?attachment:start+joint.Parent;
                Matrix4x4 world=pose.LocalBind[i]*(parent<0?Matrix4x4.Identity:joints[parent].WorldBind);
                SkeletonBindPoseBuilder.RigidInverse(world);
                joints.Add(new(reference.Path,i,parent,joint.Name,pose.LocalBind[i],world,pose.InverseBind[i]));
            }
        }
        if(roots!=1)throw new InvalidDataException("Missing binding root.");
        return new(Array.AsReadOnly(joints.ToArray()));
    }
}
