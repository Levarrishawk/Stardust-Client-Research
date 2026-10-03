using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Stardust.Core.Protocol;

namespace Stardust.Core.Assets;

public sealed record CustomizationVariableRange(int MinimumInclusive, int MaximumExclusive);

/// <summary>Owned explicit consumer ranges; does not recover ACST variables or linked/mounted directories.</summary>
public sealed class CustomizationVariableRangeContext
{
    public CustomizationVariableRangeContext(IEnumerable<KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        var copy = new Dictionary<ShaderCustomizationKey, CustomizationVariableRange>();
        foreach (var pair in ranges)
        {
            ValidateKey(pair.Key);
            if (copy.Count >= 4096 || pair.Value is null || pair.Value.MinimumInclusive >= pair.Value.MaximumExclusive || !copy.TryAdd(pair.Key, pair.Value))
                throw new InvalidDataException("Invalid, duplicate or excessive declared customization range.");
        }
        if (copy.Count == 0) throw new InvalidDataException("Explicit nonempty customization range context required.");
        Ranges = new ReadOnlyDictionary<ShaderCustomizationKey, CustomizationVariableRange>(copy);
    }
    public IReadOnlyDictionary<ShaderCustomizationKey, CustomizationVariableRange> Ranges { get; }
    internal static void ValidateKey(ShaderCustomizationKey key)
    {
        if (key.VariableName is null || key.VariableName.Length is < 1 or > 256 || key.VariableName.Any(c => c is < (char)33 or > (char)126))
            throw new InvalidDataException("Invalid declared customization key.");
    }
}

public enum CustomizationProjectionDecision { Applied, RetainedOutOfRange, SkippedUnsupportedNamespace, SkippedUndeclaredConsumer }
public sealed record CustomizationProjectionDiagnostic(int RecordIndex, byte Id, string FullName,
    ShaderCustomizationKey? Key, int AttemptedValue, int? PreviousValue, int? ResultValue, CustomizationProjectionDecision Decision);
public sealed class CustomizationVariableProjectionResult
{
    internal CustomizationVariableProjectionResult(Dictionary<ShaderCustomizationKey, int> values,
        List<CustomizationProjectionDiagnostic> diagnostics, bool isOuterEmpty)
    {
        Values = new ReadOnlyDictionary<ShaderCustomizationKey, int>(values);
        Diagnostics = diagnostics.AsReadOnly(); IsOuterEmpty = isOuterEmpty;
    }
    public IReadOnlyDictionary<ShaderCustomizationKey, int> Values { get; }
    public IReadOnlyList<CustomizationProjectionDiagnostic> Diagnostics { get; }
    public bool IsOuterEmpty { get; }
}

/// <summary>Pure ordered basic ranged/palette local writes with explicit full seed and atomic result publication.</summary>
public static class CustomizationVariableProjection
{
    public static CustomizationVariableProjectionResult Apply(CustomizationIdDictionary dictionary,
        IReadOnlyDictionary<ShaderCustomizationKey, CustomizationVariableRange> ranges,
        IReadOnlyDictionary<ShaderCustomizationKey, int> currentValues, PackedCustomizationSnapshot snapshot)
        => Apply(dictionary, new CustomizationVariableRangeContext(ranges), currentValues, snapshot);

    public static CustomizationVariableProjectionResult Apply(CustomizationIdDictionary dictionary,
        CustomizationVariableRangeContext ranges, IReadOnlyDictionary<ShaderCustomizationKey, int> currentValues,
        PackedCustomizationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(dictionary); ArgumentNullException.ThrowIfNull(ranges);
        ArgumentNullException.ThrowIfNull(currentValues); ArgumentNullException.ThrowIfNull(snapshot);
        if (currentValues.Count != ranges.Ranges.Count) throw new InvalidDataException("Exact full customization seed required.");
        var values = new Dictionary<ShaderCustomizationKey, int>();
        foreach (var pair in currentValues)
        {
            CustomizationVariableRangeContext.ValidateKey(pair.Key);
            if (values.Count >= 4096 || !ranges.Ranges.TryGetValue(pair.Key, out var range) ||
                pair.Value < range.MinimumInclusive || pair.Value >= range.MaximumExclusive || !values.TryAdd(pair.Key, pair.Value))
                throw new InvalidDataException("Customization seed key/value outside declared context.");
        }
        if (values.Count != ranges.Ranges.Count) throw new InvalidDataException("Incomplete customization seed.");
        // Preflight the entire installed-ID boundary. No result or callback exists before this succeeds.
        for (int i = 0; i < snapshot.Records.Count; i++)
            if (!dictionary.ById.ContainsKey(snapshot.Records[i].Id))
                throw new InvalidDataException($"Unknown installed customization ID {snapshot.Records[i].Id} at record {i}.");
        var diagnostics = new List<CustomizationProjectionDiagnostic>(snapshot.Records.Count);
        for (int i = 0; i < snapshot.Records.Count; i++)
        {
            var record = snapshot.Records[i]; var entry = dictionary.ById[record.Id];
            byte[] raw = record.CopyRawValue();
            int attempted = record.ValueWidth == 1 ? raw[0] : BinaryPrimitives.ReadInt16LittleEndian(raw);
            var projected = CustomizationIdDictionary.ProjectName(entry.FullName);
            ShaderCustomizationKey? key = projected.Key is { } p ? new(p.VariableName, p.FactoryFlag) : null;
            if (key is null)
                diagnostics.Add(new(i, record.Id, entry.FullName, null, attempted, null, null, CustomizationProjectionDecision.SkippedUnsupportedNamespace));
            else if (!ranges.Ranges.TryGetValue(key.Value, out var range))
                diagnostics.Add(new(i, record.Id, entry.FullName, key, attempted, null, null, CustomizationProjectionDecision.SkippedUndeclaredConsumer));
            else
            {
                int previous = values[key.Value]; bool accepted = attempted >= range.MinimumInclusive && attempted < range.MaximumExclusive;
                if (accepted) values[key.Value] = attempted;
                diagnostics.Add(new(i, record.Id, entry.FullName, key, attempted, previous, values[key.Value],
                    accepted ? CustomizationProjectionDecision.Applied : CustomizationProjectionDecision.RetainedOutOfRange));
            }
        }
        return new(values, diagnostics, snapshot.IsOuterEmpty);
    }
}
