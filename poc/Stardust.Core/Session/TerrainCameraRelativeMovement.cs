using System.Numerics;

namespace Stardust.Core.Session;

/// <summary>Pure supplied native-camera projection; adapters reflect engine Z before calling.</summary>
public static class TerrainCameraRelativeMovement
{
    public static Vector2 Project(Vector2 rawKeys, Vector3 nativeCameraForward)
    {
        if (!float.IsFinite(rawKeys.X) || !float.IsFinite(rawKeys.Y) || Math.Abs(rawKeys.X) > 1 || Math.Abs(rawKeys.Y) > 1)
            throw new ArgumentOutOfRangeException(nameof(rawKeys));
        if (!float.IsFinite(nativeCameraForward.X) || !float.IsFinite(nativeCameraForward.Y) || !float.IsFinite(nativeCameraForward.Z) ||
            Math.Abs(nativeCameraForward.X) > 1_000_000 || Math.Abs(nativeCameraForward.Y) > 1_000_000 || Math.Abs(nativeCameraForward.Z) > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(nativeCameraForward));
        double fx = nativeCameraForward.X, fz = nativeCameraForward.Z;
        double horizontal = Math.Sqrt(fx * fx + fz * fz);
        if (!double.IsFinite(horizontal) || horizontal < 1e-6)
            throw new ArgumentOutOfRangeException(nameof(nativeCameraForward));
        fx /= horizontal; fz /= horizontal;
        double x = rawKeys.X, y = rawKeys.Y, length = Math.Sqrt(x * x + y * y);
        if (length > 1) { x /= length; y /= length; }
        var result = new Vector2((float)(fz * x - fx * y), (float)(-fx * x - fz * y));
        static float TowardZero(float v) => v > 0 ? MathF.BitDecrement(v) : v < 0 ? MathF.BitIncrement(v) : v;
        static bool Outside(Vector2 v) => !float.IsFinite(v.X) || !float.IsFinite(v.Y) ||
            (double)v.X * v.X + (double)v.Y * v.Y > 1 || v.Length() > 1;
        for (int attempt = 0; attempt < 4 && Outside(result); attempt++)
            result = new(TowardZero(result.X), TowardZero(result.Y));
        if (Outside(result)) throw new InvalidDataException("Camera-relative binary32 unit-disk admission.");
        return result;
    }
}
