using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

/// <summary>Explicit supplied face inspection values; never recovered player defaults.</summary>
public sealed class SceneCreatureFaceInspectionPolicy
{
    public IReadOnlyDictionary<BlueprintCustomizationKey,int> Values { get; }
    public long MaximumDecodedBytes { get; }
    public SceneCreatureFaceInspectionPolicy(IReadOnlyDictionary<BlueprintCustomizationKey,int> values,
        long maximumDecodedBytes=67108864)
    {
        ArgumentNullException.ThrowIfNull(values);
        if(values.Count is <1 or >4096 || maximumDecodedBytes is <4 or >134217728)
            throw new ArgumentOutOfRangeException(nameof(values));
        var copy=new Dictionary<BlueprintCustomizationKey,int>();
        foreach(var pair in values)
        {
            string name=pair.Key.VariableName;
            if(name is null || name.Length is <1 or >256 || name.Any(c=>c<33 || c>126) || !copy.TryAdd(pair.Key,pair.Value))
                throw new ArgumentException("Invalid explicit face inspection key.",nameof(values));
        }
        Values=new ReadOnlyDictionary<BlueprintCustomizationKey,int>(copy);
        MaximumDecodedBytes=maximumDecodedBytes;
    }
}

