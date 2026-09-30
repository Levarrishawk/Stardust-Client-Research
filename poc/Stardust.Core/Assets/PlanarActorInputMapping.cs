using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Owned horizontal doorway-relative axes; diagonals are normalized and never exceed declared speed.</summary>
public static class PlanarActorInputMapping
{
    public static PortalActorInput Create(Vector3 worldForward, float forwardAxis, float rightAxis, float speed, bool? entryAllowed)
    {
        if (!float.IsFinite(worldForward.X) || !float.IsFinite(worldForward.Y) || !float.IsFinite(worldForward.Z) || Math.Abs(worldForward.Y) > 1e-6f ||
            !float.IsFinite(forwardAxis) || !float.IsFinite(rightAxis) || Math.Abs(forwardAxis) > 1 || Math.Abs(rightAxis) > 1 ||
            !float.IsFinite(speed) || speed is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(worldForward), "Finite planar axes and bounded speed required.");
        double length = Math.Sqrt((double)worldForward.X * worldForward.X + (double)worldForward.Z * worldForward.Z);
        if (length < 1e-8) throw new ArgumentOutOfRangeException(nameof(worldForward), "Nonzero horizontal forward required.");
        double axisLength = Math.Sqrt((double)forwardAxis * forwardAxis + (double)rightAxis * rightAxis);
        double scale = speed / Math.Max(1, axisLength);
        // Right = up cross forward in the native coordinate convention. Engine adapters handle reflection.
        double x = ((double)worldForward.X * forwardAxis + (double)worldForward.Z * rightAxis) / length * scale;
        double z = ((double)worldForward.Z * forwardAxis - (double)worldForward.X * rightAxis) / length * scale;
        return new(new((float)x, 0, (float)z), entryAllowed);
    }
}
