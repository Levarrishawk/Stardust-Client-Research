namespace Stardust.Core.Protocol;

/// <summary>Owned ordered CREO3 union0..17; support for other views/families is separate.</summary>
public sealed class CreatureView3NamedDeltaUpdate
{
    private CreatureView3NamedDeltaUpdate(CreatureView3ExtendedDeltaUpdate? extended, CreatureView3NameUpdate? name)
    { Extended = extended; Name = name; }
    public CreatureView3ExtendedDeltaUpdate? Extended { get; }
    public CreatureView3NameUpdate? Name { get; }
    public ushort Index => Extended?.Index ?? Name!.Index;
    public static CreatureView3NamedDeltaUpdate FromExtended(CreatureView3ExtendedDeltaUpdate value)
    { ArgumentNullException.ThrowIfNull(value); return new(value, null); }
    public static CreatureView3NamedDeltaUpdate FromName(CreatureView3NameUpdate value)
    { ArgumentNullException.ThrowIfNull(value); return new(null, value); }
}

public sealed class CreatureView3NamedDeltaPlan
{
    public CreatureView3NamedDeltaPlan(ulong objectId, ushort declaredCount,
        IReadOnlyList<CreatureView3NamedDeltaUpdate> updates, CreatureWoundsApplicationPolicy woundsPolicy,
        int maximumWoundsElements, int maximumComponentsElements)
    {
        ArgumentNullException.ThrowIfNull(updates);
        if (updates.Count is < 1 or > 4096 ||
            woundsPolicy is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded) ||
            maximumWoundsElements is < 0 or > 65536 || maximumComponentsElements is < 0 or > 65536)
            throw new InvalidDataException("Bounded named creature plan required.");
        var owned = new List<CreatureView3NamedDeltaUpdate>(); var extended = new List<CreatureView3ExtendedDeltaUpdate>();
        long names = 0;
        foreach (var update in updates)
        {
            if (update is null || owned.Count >= 4096) throw new InvalidDataException("Bounded nonnull named updates required.");
            owned.Add(update); if (update.Extended is { } value) extended.Add(value);
            names += update.Name?.DecodedBytes ?? 0;
            if (names > 1048576) throw new InvalidDataException("Named aggregate variable payload budget.");
        }
        if (owned.Count != updates.Count) throw new InvalidDataException("Named update collection changed.");
        if (extended.Count > 0) _ = new CreatureView3ExtendedDeltaPlan(objectId, declaredCount, extended, woundsPolicy,
            maximumWoundsElements, maximumComponentsElements);
        ObjectId = objectId; DeclaredCount = declaredCount; Updates = owned.AsReadOnly(); WoundsPolicy = woundsPolicy;
        MaximumWoundsElements = maximumWoundsElements; MaximumComponentsElements = maximumComponentsElements;
    }
    public ulong ObjectId { get; }
    public ushort DeclaredCount { get; }
    public IReadOnlyList<CreatureView3NamedDeltaUpdate> Updates { get; }
    public CreatureWoundsApplicationPolicy WoundsPolicy { get; }
    public int MaximumWoundsElements { get; }
    public int MaximumComponentsElements { get; }
}
