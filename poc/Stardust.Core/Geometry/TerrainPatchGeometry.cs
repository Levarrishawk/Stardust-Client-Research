using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

public sealed record TerrainPatchGeometryLimits(float MaximumPositionComponent = 1_000_000f,
    float MaximumNormalComponent = 4096f, float MaximumUvScale = 65536f);

/// <summary>Owned supplied CPU cell; no terrain evaluation, coordinate generation, or seam selection.</summary>
public sealed class TerrainPatchGeometry
{
    private static readonly Vector2[] Base = [
        new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),
        new(1,.5f),new(1,1),new(.5f,1),new(0,1)];
    private static readonly Vector2[][] Auxiliary = [
        [new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1)],
        [new(.5f,.5f),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1),new(0,.5f),new(0,0)],
        [new(.5f,.5f),new(1,.5f),new(1,1),new(.5f,1),new(0,1),new(0,.5f),new(0,0),new(.5f,0),new(1,0)],
        [new(.5f,.5f),new(.5f,1),new(0,1),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1)]
    ];
    private readonly Vector3[] positions, normals;
    private readonly uint[] colors;
    private readonly Vector2[] baseUvs;
    private readonly Vector2[][] auxiliaryUvs;
    private readonly int[] selectors;
    private readonly ushort[] indices;
    public int PatternIndex { get; }
    public float UvScale { get; }
    public int AuxiliarySetCount => selectors.Length;
    public int TriangleCount => indices.Length / 3;
    public Vector3 Minimum { get; }
    public Vector3 Maximum { get; }

    public TerrainPatchGeometry(ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals,
        ReadOnlySpan<uint> argbColors, float uvScale, int patternIndex,
        ReadOnlySpan<int> auxiliarySelectors = default, TerrainPatchGeometryLimits? limits = null)
    {
        limits ??= new();
        ValidateCap(limits.MaximumPositionComponent, nameof(limits.MaximumPositionComponent));
        ValidateCap(limits.MaximumNormalComponent, nameof(limits.MaximumNormalComponent));
        ValidateCap(limits.MaximumUvScale, nameof(limits.MaximumUvScale));
        if (positions.Length != 9 || normals.Length != 9 || argbColors.Length != 9)
            throw new ArgumentException("Exactly nine supplied positions, normals and ARGB words are required.");
        if (!float.IsFinite(uvScale) || uvScale <= 0 || uvScale > limits.MaximumUvScale)
            throw new ArgumentOutOfRangeException(nameof(uvScale));
        if (auxiliarySelectors.Length > 3) throw new ArgumentOutOfRangeException(nameof(auxiliarySelectors));
        this.positions = positions.ToArray(); this.normals = normals.ToArray(); colors = argbColors.ToArray();
        selectors = auxiliarySelectors.ToArray();
        foreach (int selector in selectors)
            if ((uint)selector >= 4) throw new ArgumentOutOfRangeException(nameof(auxiliarySelectors));
        indices = TerrainPatchTopology.CopyIndices(patternIndex);
        for (int i = 0; i < 9; i++)
        {
            ValidateVector(this.positions[i], limits.MaximumPositionComponent, "position");
            ValidateVector(this.normals[i], limits.MaximumNormalComponent, "normal");
            var n = this.normals[i];
            double squared = (double)n.X*n.X + (double)n.Y*n.Y + (double)n.Z*n.Z;
            if (squared < 1e-12) throw new ArgumentException("A supplied normal must have nonzero eligible length.");
        }
        PatternIndex = patternIndex; UvScale = uvScale;
        baseUvs = Base.Select(v => new Vector2(v.X*uvScale, v.Y*uvScale)).ToArray();
        if (baseUvs.Any(v => !float.IsFinite(v.X) || !float.IsFinite(v.Y))) throw new OverflowException("UV multiplication.");
        auxiliaryUvs = selectors.Select(s => (Vector2[])Auxiliary[s].Clone()).ToArray();
        Vector3 minimum = this.positions[0], maximum = minimum;
        foreach (var p in this.positions) { minimum = Vector3.Min(minimum,p); maximum = Vector3.Max(maximum,p); }
        Minimum = minimum; Maximum = maximum;
    }

    public Vector3[] CopyPositions() => (Vector3[])positions.Clone();
    public Vector3[] CopyNormals() => (Vector3[])normals.Clone();
    public uint[] CopyArgbColors() => (uint[])colors.Clone();
    public Vector4[] CopyRgbaColors() => colors.Select(PackedVertexColor.ToRgba).ToArray();
    public Vector2[] CopyBaseUvs() => (Vector2[])baseUvs.Clone();
    public int[] CopyAuxiliarySelectors() => (int[])selectors.Clone();
    public ushort[] CopyIndices() => (ushort[])indices.Clone();
    public Vector2[] CopyAuxiliaryUvs(int setIndex)
    {
        if ((uint)setIndex >= auxiliaryUvs.Length) throw new ArgumentOutOfRangeException(nameof(setIndex));
        return (Vector2[])auxiliaryUvs[setIndex].Clone();
    }
    private static void ValidateCap(float cap, string name)
    {
        if (!float.IsFinite(cap) || cap <= 0 || cap > 1e9f) throw new ArgumentOutOfRangeException(name);
    }
    private static void ValidateVector(Vector3 value, float cap, string kind)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z)
            || Math.Abs(value.X)>cap || Math.Abs(value.Y)>cap || Math.Abs(value.Z)>cap)
            throw new ArgumentException("Nonfinite or over-cap "+kind+".");
    }
}
