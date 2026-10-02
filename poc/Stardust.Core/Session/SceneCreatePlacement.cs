using System.Numerics;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum SceneCreateZeroQuaternionPolicy { Reject, Identity }

/// <summary>Modern derived presentation placement. Raw create fields retain their original scalar bits.</summary>
public sealed record SceneCreatePlacement(SceneCreateObjectByCrcMessage RawCreate, AppearanceTransform Transform,
    Quaternion UnitQuaternion, double InputQuaternionNorm, bool AppliedNormalization, bool UsedZeroIdentity,
    SceneCreateZeroQuaternionPolicy ZeroPolicy);

public static class SceneCreatePlacementBuilder
{
    /// <summary>Standard XYZW quaternion rotation for column vectors; no engine reflection or parent/root-motion inference.</summary>
    public static SceneCreatePlacement Create(SceneCreateObjectByCrcMessage raw, SceneCreateZeroQuaternionPolicy zeroPolicy)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (zeroPolicy is not (SceneCreateZeroQuaternionPolicy.Reject or SceneCreateZeroQuaternionPolicy.Identity))
            throw new ArgumentOutOfRangeException(nameof(zeroPolicy));
        if (new[] { raw.QuaternionX, raw.QuaternionY, raw.QuaternionZ, raw.QuaternionW,
                    raw.PositionX, raw.PositionZ, raw.PositionY }.Any(f => !float.IsFinite(f)))
            throw new InvalidDataException("Finite create quaternion and position required.");
        double x = raw.QuaternionX, y = raw.QuaternionY, z = raw.QuaternionZ, w = raw.QuaternionW;
        double scale = Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Max(Math.Abs(z), Math.Abs(w)));
        bool fallback = scale == 0;
        double norm;
        if (fallback)
        {
            if (zeroPolicy == SceneCreateZeroQuaternionPolicy.Reject) throw new InvalidDataException("Zero create quaternion rejected by explicit modern policy.");
            x = y = z = 0; w = 1; norm = 0;
        }
        else
        {
            // Scaling avoids float overflow/underflow, including the smallest nonzero binary32 input.
            x /= scale; y /= scale; z /= scale; w /= scale;
            double scaledNorm = Math.Sqrt(x * x + y * y + z * z + w * w);
            norm = scale * scaledNorm;
            x /= scaledNorm; y /= scaledNorm; z /= scaledNorm; w /= scaledNorm;
        }
        var rotation = new AppearanceTransform(
            new((float)(1 - 2 * (y * y + z * z)), (float)(2 * (x * y - z * w)), (float)(2 * (x * z + y * w))),
            new((float)(2 * (x * y + z * w)), (float)(1 - 2 * (x * x + z * z)), (float)(2 * (y * z - x * w))),
            new((float)(2 * (x * z - y * w)), (float)(2 * (y * z + x * w)), (float)(1 - 2 * (x * x + y * y))),
            new(raw.PositionX, raw.PositionY, raw.PositionZ));
        return new(raw with { }, rotation, new((float)x, (float)y, (float)z, (float)w), norm,
            !fallback && norm != 1, fallback, zeroPolicy);
    }
}
