using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>PS1.x/PS2.0 COLOR register range boundary, applied to interpolated vertex colors before pixel arithmetic.</summary>
public static class LegacyPixelColorInput
{
    public static Vector3 Saturate(Vector3 color)
    {
        if(!float.IsFinite(color.X)||!float.IsFinite(color.Y)||!float.IsFinite(color.Z))throw new InvalidDataException("Finite legacy pixel color input required.");
        return Vector3.Clamp(color,Vector3.Zero,Vector3.One);
    }
}
