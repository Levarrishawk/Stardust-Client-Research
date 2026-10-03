namespace Stardust.Core.Protocol;

/// <summary>Ordered supported view3 union including fixed-width common fields; indices1/2 remain unsupported.</summary>
public sealed class CreatureView3ExtendedDeltaUpdate
{
    private CreatureView3ExtendedDeltaUpdate(CreatureView3FullDeltaUpdate? full, CreatureView3TangibleScalarUpdate? tangible)
    { Full = full; Tangible = tangible; }
    public CreatureView3FullDeltaUpdate? Full { get; }
    public CreatureView3TangibleScalarUpdate? Tangible { get; }
    public ushort Index => Full?.Index ?? Tangible!.Index;
    public static CreatureView3ExtendedDeltaUpdate FromFull(CreatureView3FullDeltaUpdate update)
    { ArgumentNullException.ThrowIfNull(update); return new(update, null); }
    public static CreatureView3ExtendedDeltaUpdate FromTangible(CreatureView3TangibleScalarUpdate update)
    { ArgumentNullException.ThrowIfNull(update); return new(null, update); }
}

public sealed class CreatureView3ExtendedDeltaPlan
{
    public CreatureView3ExtendedDeltaPlan(ulong objectId, ushort declaredCount,
        IReadOnlyList<CreatureView3ExtendedDeltaUpdate> updates, CreatureWoundsApplicationPolicy woundsPolicy,
        int maximumWoundsElements, int maximumComponentsElements)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if (updates.Count is < 1 or > 4096 ||
            woundsPolicy is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded) ||
            maximumWoundsElements is < 0 or > 65536 || maximumComponentsElements is < 0 or > 65536)
            throw new InvalidDataException("Bounded extended creature plan required.");
        var owned = new List<CreatureView3ExtendedDeltaUpdate>();
        var full = new List<CreatureView3FullDeltaUpdate>();
        foreach (var update in updates)
        {
            if (update is null || owned.Count >= 4096) throw new InvalidDataException("Bounded nonnull extended updates required.");
            owned.Add(update); if (update.Full is { } value) full.Add(value);
        }
        if (owned.Count != updates.Count) throw new InvalidDataException("Extended update collection changed.");
        if (full.Count > 0) _ = new CreatureView3FullDeltaPlan(objectId, declaredCount, full, woundsPolicy,
            maximumWoundsElements, maximumComponentsElements);
        ObjectId = objectId; DeclaredCount = declaredCount; Updates = owned.AsReadOnly(); WoundsPolicy = woundsPolicy;
        MaximumWoundsElements = maximumWoundsElements; MaximumComponentsElements = maximumComponentsElements;
    }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureView3ExtendedDeltaUpdate> Updates { get; }
    public CreatureWoundsApplicationPolicy WoundsPolicy { get; }
    public int MaximumWoundsElements { get; }
    public int MaximumComponentsElements { get; }
}
