namespace Stardust.Core.Protocol;

/// <summary>Owned ordered field-4 replacements. Declared count remains raw envelope metadata.</summary>
public sealed class CreatureView3CustomizationDeltaPlan
{
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<ReplicationByteString> Updates { get; }
    public CreatureView3CustomizationDeltaPlan(ulong objectId, ushort declaredCount,
        IReadOnlyList<ReplicationByteString> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if (updates.Count is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(updates));
        var copy = updates.ToArray();
        if (copy.Any(v => v is null)) throw new ArgumentException("Null customization field.", nameof(updates));
        ObjectId = objectId; DeclaredCount = declaredCount; Updates = Array.AsReadOnly(copy);
    }
}
