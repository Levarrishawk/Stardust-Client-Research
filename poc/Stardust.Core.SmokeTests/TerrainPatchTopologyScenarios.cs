using System.Text.Json;
using Stardust.Core.Geometry;

internal static class TerrainPatchTopologyScenarios
{
    private sealed record Fixture(int Pattern, ushort[] Indices, Rebase[] Rebases);
    private sealed record Rebase(int VertexBase, int DestinationVertexCount, int[] Indices);
    private static int checks;

    internal static void Run(string fixturePath, string outputPath)
    {
        checks = 0;
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(fixturePath))!;
        Check(fixtures.Length == 9, "All explicit patterns");
        Check(TerrainPatchTopology.PatternCount == 9 && TerrainPatchTopology.VerticesPerCell == 9
            && TerrainPatchTopology.MaximumIndexCount == 24, "Fixed bounded API");
        var rows = new List<object>();
        int triangleChecks = 0, negativeChecks = 0, rebaseCases = 0;
        // Declared modern X/Z fixture. It is not recovered PTAT vertex generation.
        (int X, int Z)[] points = [(0,0),(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1),(0,-1),(1,-1)];
        foreach (var f in fixtures)
        {
            ushort[] original = TerrainPatchTopology.CopyIndices(f.Pattern);
            Check(original.SequenceEqual(f.Indices), "Independent literal indices");
            Check(TerrainPatchTopology.GetIndexCount(f.Pattern) == f.Indices.Length, "Count");
            Check(original.Length % 3 == 0 && original.All(i => i < 9), "Triangles and local bounds");
            for (int i = 0; i < original.Length; i += 3)
            {
                var a = points[original[i]]; var b = points[original[i+1]]; var c = points[original[i+2]];
                Check((b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X) > 0, "Synthetic signed X/Z area");
                triangleChecks++;
            }
            ushort[] other = TerrainPatchTopology.CopyIndices(f.Pattern);
            Check(!ReferenceEquals(original, other), "Distinct inspection ownership");
            original[0] = ushort.MaxValue;
            Check(other.SequenceEqual(f.Indices) && TerrainPatchTopology.CopyIndices(f.Pattern).SequenceEqual(f.Indices), "No inspection alias");
            var rebased = new List<object>();
            foreach (var r in f.Rebases)
            {
                int[] result = TerrainPatchTopology.RebaseIndices(f.Pattern, r.VertexBase, r.DestinationVertexCount);
                Check(result.SequenceEqual(r.Indices), "Independent rebasing oracle");
                Check(result.All(i => i >= r.VertexBase && i < r.DestinationVertexCount), "Destination bounds");
                int[] second = TerrainPatchTopology.RebaseIndices(f.Pattern, r.VertexBase, r.DestinationVertexCount);
                Check(!ReferenceEquals(result, second), "Distinct rebased ownership");
                rebased.Add(new { r.VertexBase, r.DestinationVertexCount, Indices = second });
                result[0] = -1;
                Check(second.SequenceEqual(r.Indices) && TerrainPatchTopology.CopyIndices(f.Pattern).SequenceEqual(f.Indices), "No output alias");
                rebaseCases++;
            }
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.RebaseIndices(f.Pattern, -1, 9)); negativeChecks++;
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.RebaseIndices(f.Pattern, 0, -1)); negativeChecks++;
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.RebaseIndices(f.Pattern, 0, 8)); negativeChecks++;
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.RebaseIndices(f.Pattern, 1, 9)); negativeChecks++;
            Expect<OverflowException>(() => TerrainPatchTopology.RebaseIndices(f.Pattern, int.MaxValue-8, int.MaxValue)); negativeChecks++;
            Expect<OverflowException>(() => TerrainPatchTopology.RebaseIndices(f.Pattern, int.MaxValue, int.MaxValue)); negativeChecks++;
            Check(TerrainPatchTopology.CopyIndices(f.Pattern).SequenceEqual(f.Indices), "Invalid input never mutates shared topology");
            rows.Add(new { f.Pattern, Indices = other, Rebases = rebased });
        }
        foreach (int invalid in new[] { int.MinValue, -1, 9, int.MaxValue })
        {
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.GetIndexCount(invalid)); negativeChecks++;
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.CopyIndices(invalid)); negativeChecks++;
            Expect<ArgumentOutOfRangeException>(() => TerrainPatchTopology.RebaseIndices(invalid, 0, 9)); negativeChecks++;
        }
        File.WriteAllText(outputPath, JsonSerializer.Serialize(new { Checks = checks, TriangleChecks = triangleChecks,
            NegativeChecks = negativeChecks, RebaseCases = rebaseCases, OriginalCoordinates = false, Results = rows },
            new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"R31_TOPOLOGY_PASS checks={checks} triangles={triangleChecks} negatives={negativeChecks} rebases={rebaseCases}");
    }

    private static void Check(bool value, string reason)
    {
        checks++;
        if (!value) throw new Exception(reason);
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        checks++;
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}
