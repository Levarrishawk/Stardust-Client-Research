using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

/// <summary>Explicit palette-only selection over owned albedo scene resources. No face generation or asset lookup.</summary>
public static class SceneCreaturePaletteCustomization
{
    public static IReadOnlyList<SkeletalCustomizationControl> DescribeControls(SceneCreatureResources resources,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(resources); cancellation.ThrowIfCancellationRequested();
        var material = resources.Creature?.MaterialBundle;
        Need(material?.PaletteDefaults is { Count: > 0 and <= 64 } && material.AlbedoFactors is not null &&
            material.Materials.Count is > 0 and <= 64, "Owned palette/albedo material resources required.");
        Need(material!.AlbedoFactors!.Materials.Keys.ToHashSet().SetEquals(material.Materials.Keys), "Incomplete albedo material keys.");
        var requiredPalettes = material.Materials.Where(p => p.Value.Customization is not null).Select(p => p.Key).ToHashSet();
        Need(requiredPalettes.SetEquals(material.PaletteDefaults!.Keys), "Incomplete palette material keys.");
        var ranges = new Dictionary<ShaderCustomizationKey, int>(); int factors = 0;
        foreach (var palette in material.PaletteDefaults.Values)
        {
            cancellation.ThrowIfCancellationRequested();
            Need(palette.Shader.PaletteFactors.Count <= 4096 && palette.Palettes.Count <= 4096 && palette.MissingPalettePaths.Count <= 4096,
                "Palette description budget exceeded.");
            foreach (var factor in palette.Shader.PaletteFactors)
            {
                cancellation.ThrowIfCancellationRequested(); Need(++factors <= 4096, "Palette factor budget exceeded.");
                Need(factor.VariableName.Length is >= 1 and <= 256 && factor.VariableName.All(c => c is >= (char)33 and <= (char)126), "Invalid palette variable name.");
                Need(palette.Palettes.TryGetValue(factor.PalettePath, out var owned), "Owned palette absent.");
                int count = owned!.Entries.Count; Need(count is >= 1 and <= 1024, "Empty or oversized palette control range.");
                var key = new ShaderCustomizationKey(factor.VariableName, factor.RawPrivateFlag != 0);
                ranges[key] = Math.Min(ranges.GetValueOrDefault(key, int.MaxValue), count - 1);
            }
        }
        Need(ranges.Count is > 0 and <= 4096, "Palette controls required within budget.");
        cancellation.ThrowIfCancellationRequested();
        return Array.AsReadOnly(ranges.OrderBy(p => p.Key.VariableName, StringComparer.Ordinal).ThenBy(p => p.Key.Private)
            .Select(p => new SkeletalCustomizationControl(p.Key, p.Value)).ToArray());
    }

    /// <summary>Rejects missing, extra, duplicate and out-of-range inputs before publishing any result.</summary>
    public static SceneCreatureResources Apply(SceneCreatureResources resources,
        IEnumerable<KeyValuePair<ShaderCustomizationKey, int>> values, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(values); var controls = DescribeControls(resources, cancellation);
        var snapshot = new Dictionary<ShaderCustomizationKey, int>(); int count = 0;
        foreach (var pair in values)
        {
            cancellation.ThrowIfCancellationRequested(); Need(++count <= 4096 && snapshot.TryAdd(pair.Key, pair.Value), "Duplicate or oversized palette values.");
        }
        Need(controls.Select(c => c.Key).ToHashSet().SetEquals(snapshot.Keys), "Exactly the owned palette namespace keys are required.");
        foreach (var control in controls) Need(snapshot[control.Key] >= 0 && snapshot[control.Key] <= control.MaximumValue, "Palette value outside intersected range.");
        var creature = resources.Creature; var material = creature.MaterialBundle!;
        var palettes = new Dictionary<string, CustomizableShaderPaletteBundle>(StringComparer.Ordinal);
        foreach (var pair in material.PaletteDefaults!)
        {
            cancellation.ThrowIfCancellationRequested(); var original = pair.Value;
            PaletteDescription? Resolve(string path) => original.MissingPalettePaths.Contains(path) ? null :
                original.Palettes.TryGetValue(path, out var owned) ? owned : throw new InvalidDataException("Owned palette absent.");
            var selected = CustomizableShaderPaletteQuery.Select(original.Shader, Resolve, snapshot);
            Need(selected.All(f => !f.UsedAuthoredDefault && f.Color is not null && f.Color.SelectedIndex == f.RequestedIndex), "Implicit or fallback palette index rejected.");
            var diagnostics = original.Diagnostics.Where(d => !d.StartsWith("PaletteIndexFallback:", StringComparison.Ordinal) &&
                !d.StartsWith("EmptyPalette:", StringComparison.Ordinal)).ToArray();
            palettes.Add(pair.Key, original with { AuthoredFactors = selected, Diagnostics = Array.AsReadOnly(diagnostics) });
        }
        var factors = new Dictionary<string, SkeletalAlbedoFactors>(StringComparer.Ordinal);
        foreach (var pair in material.AlbedoFactors!.Materials)
        {
            cancellation.ThrowIfCancellationRequested(); palettes.TryGetValue(pair.Key, out var palette);
            factors.Add(pair.Key, SkeletalAlbedoFactorReader.WithPalette(material.Materials[pair.Key], palette, pair.Value));
        }
        var updated = material with { PaletteDefaults = new ReadOnlyDictionary<string, CustomizableShaderPaletteBundle>(palettes),
            AlbedoFactors = material.AlbedoFactors with { Materials = new ReadOnlyDictionary<string, SkeletalAlbedoFactors>(factors) } };
        cancellation.ThrowIfCancellationRequested();
        return resources with { Creature = creature with { MaterialBundle = updated,
            CustomizationValues = new ReadOnlyDictionary<ShaderCustomizationKey, int>(snapshot) } };
    }

    /// <summary>Reuse accepted strict version1 variableName/factoryFlag/value parsing; it does not require face assets.</summary>
    public static IReadOnlyDictionary<ShaderCustomizationKey, int> LoadProfile(Stream source)
        => SkeletalAppearanceCustomization.LoadProfile(source);

    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
