using System.Collections.ObjectModel;
using Stardust.Core.Assets;

namespace Stardust.Core.Session;

/// <summary>Immutable supplied appearance values, separate from raw replication bytes.</summary>
public sealed class SceneWorldCreatureCustomizationSnapshot
{
    public SceneObjectIncarnation Incarnation { get; }
    public ulong Revision { get; }
    public IReadOnlyDictionary<ShaderCustomizationKey, int> Values { get; }

    public SceneWorldCreatureCustomizationSnapshot(SceneObjectIncarnation incarnation, ulong revision,
        IReadOnlyDictionary<ShaderCustomizationKey, int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (revision == 0) throw new ArgumentOutOfRangeException(nameof(revision));
        if (values.Count is < 1 or > 4096) throw new InvalidDataException("Customization value count.");
        var copy = new Dictionary<ShaderCustomizationKey, int>();
        foreach (var pair in values)
        {
            var name = pair.Key.VariableName;
            if (string.IsNullOrEmpty(name) || name.Length > 256 || name.Any(c => c < 0x21 || c > 0x7e))
                throw new InvalidDataException("Customization variable name.");
            copy.Add(pair.Key, pair.Value);
        }
        Incarnation = incarnation; Revision = revision;
        Values = new ReadOnlyDictionary<ShaderCustomizationKey, int>(copy);
    }
}

/// <summary>Borrowed source; missing values retain the current appearance. Revisions never rewind within an incarnation.</summary>
public interface ISceneWorldCreatureCustomizationSource
{
    SceneWorldCreatureCustomizationSnapshot? Find(SceneWorldObjectSnapshot snapshot);
}
