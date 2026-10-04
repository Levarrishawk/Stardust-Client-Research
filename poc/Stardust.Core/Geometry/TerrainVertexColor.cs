using System.Numerics;

namespace Stardust.Core.Geometry;

/// <summary>Three source bytes in recovered R, G, B order.</summary>
public readonly record struct TerrainRgb(byte R, byte G, byte B);

/// <summary>Owned scalar result; Rgba uses modern R,G,B,A vector order.</summary>
public readonly record struct TerrainVertexColorResult(TerrainRgb ModifiedRgb, Vector4 Rgba, uint Argb);

/// <summary>Recovered terrain RGB conversion, with caller-resolved modifier configuration.</summary>
public static class TerrainVertexColor
{
    private const float Reciprocal255 = 0.003921568859368563f; // bits 3B808081
    private const float ModifierFraction = 0.6000000238418579f; // bits 3F19999A

    public static TerrainVertexColorResult Convert(TerrainRgb source, byte bakedMask,
        bool modifierEnabled, TerrainRgb zeroEndpoint, TerrainRgb nonzeroEndpoint)
    {
        TerrainRgb rgb = source;
        if (modifierEnabled)
        {
            TerrainRgb endpoint = bakedMask == 0 ? zeroEndpoint : nonzeroEndpoint;
            rgb = new(Modify(source.R, endpoint.R), Modify(source.G, endpoint.G), Modify(source.B, endpoint.B));
        }
        Vector4 rgba = new(Normalize(rgb.R), Normalize(rgb.G), Normalize(rgb.B), 1f);
        uint argb = 0xFF000000u | (Pack(rgba.X) << 16) | (Pack(rgba.Y) << 8) | Pack(rgba.Z);
        return new(rgb, rgba, argb);
    }

    // Truncate the signed scaled difference BEFORE adding the source byte.
    // Double preserves the exact product of bounded integer and binary32 constant.
    private static byte Modify(byte source, byte endpoint) =>
        checked((byte)(source + (int)((endpoint - source) * (double)ModifierFraction)));

    private static float Normalize(byte channel) => channel * Reciprocal255;
    private static uint Pack(float channel) => (uint)(channel * 255d);
}
