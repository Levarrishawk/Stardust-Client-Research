using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Baseline minimum-dot selection. Caller supplies camera displacement already in primitive-local coordinates.</summary>
public static class SortedMeshIndexSelector
{
    public static IReadOnlyList<ushort> Select(MeshPrimitive primitive,Vector3 cameraDisplacementLocal)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        if(!float.IsFinite(cameraDisplacementLocal.X) || !float.IsFinite(cameraDisplacementLocal.Y) || !float.IsFinite(cameraDisplacementLocal.Z))
            throw new ArgumentOutOfRangeException(nameof(cameraDisplacementLocal));
        if(primitive.SortedIndices is not {Count:>0} variants)return primitive.Indices;
        double minimum=double.PositiveInfinity;int selected=0;
        for(int i=0;i<variants.Count;i++)
        {
            Vector3 direction=variants[i].Direction;
            double dot=(double)direction.X*cameraDisplacementLocal.X+(double)direction.Y*cameraDisplacementLocal.Y+(double)direction.Z*cameraDisplacementLocal.Z;
            if(!double.IsFinite(dot))throw new InvalidDataException("Nonfinite sorted-index direction.");
            if(dot<minimum){minimum=dot;selected=i;}
        }
        return variants[selected].Indices;
    }
}
