namespace Stardust.Core.Geometry;

/// <summary>Explicit legacy topology vectors; seam selection and vertex generation are separate.</summary>
public static class TerrainPatchTopology
{
    public const int PatternCount = 9;
    public const int VerticesPerCell = 9;
    public const int MaximumIndexCount = 24;

    // Active ushort vectors only. The original table's unused zero padding is not topology.
    private static readonly ushort[][] Patterns =
    [
        [0,1,2, 0,2,3, 0,3,4, 0,4,5, 0,5,6, 0,6,7, 0,7,8, 0,8,1],
        [0,1,2, 0,2,4, 0,4,5, 0,5,6, 0,6,7, 0,7,8, 0,8,1],
        [0,2,4, 0,4,5, 0,5,6, 0,6,7, 0,7,8, 0,8,2],
        [0,2,3, 0,3,4, 0,4,5, 0,5,6, 0,6,7, 0,7,8, 0,8,2],
        [0,2,3, 0,3,4, 0,4,5, 0,5,6, 0,6,8, 0,8,2],
        [0,1,2, 0,2,3, 0,3,4, 0,4,5, 0,5,6, 0,6,8, 0,8,1],
        [0,1,2, 0,2,3, 0,3,4, 0,4,6, 0,6,8, 0,8,1],
        [0,1,2, 0,2,3, 0,3,4, 0,4,6, 0,6,7, 0,7,8, 0,8,1],
        [0,1,2, 0,2,4, 0,4,6, 0,6,7, 0,7,8, 0,8,1]
    ];

    public static int GetIndexCount(int patternIndex) => Select(patternIndex).Length;

    /// <summary>Returns an owned copy; modifying it cannot change the shared definition.</summary>
    public static ushort[] CopyIndices(int patternIndex) => (ushort[])Select(patternIndex).Clone();

    /// <summary>
    /// Returns owned 32-bit indices for a complete nine-vertex cell within the declared
    /// destination range. Work and allocation are bounded by MaximumIndexCount.
    /// </summary>
    public static int[] RebaseIndices(int patternIndex, int vertexBase, int destinationVertexCount)
    {
        ushort[] pattern = Select(patternIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(vertexBase);
        ArgumentOutOfRangeException.ThrowIfLessThan(destinationVertexCount, VerticesPerCell);
        int endExclusive = checked(vertexBase + VerticesPerCell);
        if (endExclusive > destinationVertexCount)
            throw new ArgumentOutOfRangeException(nameof(vertexBase), "The complete cell must fit the destination vertex range.");

        int[] result = new int[pattern.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = checked(vertexBase + pattern[i]);
        return result;
    }

    private static ushort[] Select(int patternIndex)
    {
        if ((uint)patternIndex >= PatternCount)
            throw new ArgumentOutOfRangeException(nameof(patternIndex));
        return Patterns[patternIndex];
    }
}
