using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed record SceneWorldRawCustomizationUpdate(ulong Revision, ReplicationByteString Value);

/// <summary>Immutable current raw field and ordered unacknowledged updates for one incarnation.</summary>
public sealed class SceneWorldRawCustomizationBatch
{
    public SceneObjectIncarnation Incarnation { get; }
    public ulong ThroughRevision { get; }
    public ReplicationByteString Current { get; }
    public IReadOnlyList<SceneWorldRawCustomizationUpdate> Updates { get; }
    public SceneWorldRawCustomizationBatch(SceneObjectIncarnation incarnation, ulong throughRevision,
        ReplicationByteString current, IReadOnlyList<SceneWorldRawCustomizationUpdate> updates)
    {
        ArgumentNullException.ThrowIfNull(current); ArgumentNullException.ThrowIfNull(updates);
        if (throughRevision == 0 || updates.Count > 4096) throw new ArgumentOutOfRangeException(nameof(throughRevision));
        var copy = updates.ToArray(); ulong previous = 0;
        foreach (var update in copy)
        {
            if (update is null || update.Value is null || update.Revision <= previous || update.Revision > throughRevision)
                throw new InvalidDataException("Raw customization update order.");
            previous = update.Revision;
        }
        Incarnation = incarnation; ThroughRevision = throughRevision; Current = current;
        Updates = Array.AsReadOnly(copy);
    }
}

/// <summary>Borrowed single-consumer journal. Acknowledge only the exact current batch after complete application.</summary>
public interface ISceneWorldRawCustomizationSource
{
    SceneWorldRawCustomizationBatch? Find(SceneWorldObjectSnapshot snapshot);
    bool Acknowledge(SceneWorldRawCustomizationBatch batch);
}
