using Stardust.Core.Protocol;
namespace Stardust.Core.Session;

public enum SceneObjectPhase { PendingBaselines, Finalized }
public readonly record struct SceneObjectHandle(Guid RegistryId, ulong SceneGeneration, ulong ObjectId, ulong Revision);
public readonly record struct SceneObjectIncarnation(Guid RegistryId, ulong SceneGeneration, ulong ObjectId, ulong CreationRevision);
public sealed record SceneObjectSnapshot(SceneObjectHandle Handle, SceneCreateObjectByCrcMessage Create, SceneObjectPhase Phase, ulong ContainerId, uint ArrangementBits)
{
    public SceneObjectIncarnation Incarnation { get; init; }
}

/// <summary>Single serialized owner. Modern bounded lifecycle policy; no engine objects or baseline parsing.</summary>
public sealed class SceneObjectRegistry
{
    private SortedDictionary<ulong, SceneObjectSnapshot> entries = new();
    private readonly int capacity;
    private readonly Guid registryId = Guid.NewGuid();
    private ulong revision;
    public SceneObjectRegistry(int capacity, ulong sceneGeneration, ulong revisionSeed = 0)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity; SceneGeneration = sceneGeneration; revision = revisionSeed;
    }
    public ulong SceneGeneration { get; private set; }
    public bool IsClosed { get; private set; }
    public int Count => entries.Count;
    public IReadOnlyList<SceneObjectSnapshot> Snapshots() => Array.AsReadOnly(entries.Values.ToArray());
    public SceneObjectSnapshot? Find(ulong id) => entries.GetValueOrDefault(id);
    public bool IsCurrent(SceneObjectHandle handle) => !IsClosed && handle.RegistryId == registryId && handle.SceneGeneration == SceneGeneration && entries.TryGetValue(handle.ObjectId, out var value) && value.Handle == handle;

    public bool IsCurrent(SceneObjectIncarnation incarnation) => !IsClosed && incarnation.RegistryId == registryId && incarnation.SceneGeneration == SceneGeneration && entries.TryGetValue(incarnation.ObjectId, out var value) && value.Incarnation == incarnation;

    public SceneObjectSnapshot? Create(ulong generation, SceneCreateObjectByCrcMessage value)
    {
        if (!Accept(generation)) return null;
        ValidateCreate(value);
        if (entries.TryGetValue(value.ObjectId, out var current))
        {
            if (!SameCreate(current.Create, value)) throw new InvalidDataException("Conflicting create requires explicit Replace.");
            return current;
        }
        if (entries.Count >= capacity) throw new InvalidDataException("Scene object capacity exhausted.");
        ulong candidateRevision = checked(revision + 1);
        var result = new SceneObjectSnapshot(new(registryId, SceneGeneration, value.ObjectId, candidateRevision), value, SceneObjectPhase.PendingBaselines, 0, 0)
        { Incarnation = new(registryId, SceneGeneration, value.ObjectId, candidateRevision) };
        var candidate = new SortedDictionary<ulong, SceneObjectSnapshot>(entries) { [value.ObjectId] = result };
        Commit(candidate, candidateRevision);
        return result;
    }
    public SceneObjectSnapshot? Replace(ulong generation, SceneCreateObjectByCrcMessage value)
    {
        if (!Accept(generation)) return null;
        ValidateCreate(value); Require(value.ObjectId);
        ulong candidateRevision = revision;
        var candidate = new SortedDictionary<ulong, SceneObjectSnapshot>(entries);
        DetachChildren(candidate, value.ObjectId, ref candidateRevision);
        ulong creationRevision = Next(ref candidateRevision);
        var result = new SceneObjectSnapshot(new(registryId, SceneGeneration, value.ObjectId, creationRevision), value, SceneObjectPhase.PendingBaselines, 0, 0)
        { Incarnation = new(registryId, SceneGeneration, value.ObjectId, creationRevision) };
        candidate[value.ObjectId] = result;
        Commit(candidate, candidateRevision);
        return result;
    }
    public bool Contain(ulong generation, UpdateContainmentMessage value)
    {
        if (!Accept(generation)) return false;
        ArgumentNullException.ThrowIfNull(value);
        var current = Require(value.ObjectId);
        if (value.ContainerId == value.ObjectId) throw new InvalidDataException("Self containment.");
        if (value.ContainerId != 0) Require(value.ContainerId);
        for (ulong parent = value.ContainerId; parent != 0; parent = entries[parent].ContainerId)
            if (parent == value.ObjectId) throw new InvalidDataException("Containment cycle.");
        if (current.ContainerId == value.ContainerId && current.ArrangementBits == value.ArrangementBits) return true;
        ulong candidateRevision = checked(revision + 1);
        var candidate = new SortedDictionary<ulong, SceneObjectSnapshot>(entries) {
            [value.ObjectId] = current with { Handle = new(registryId, SceneGeneration, value.ObjectId, candidateRevision), ContainerId = value.ContainerId, ArrangementBits = value.ArrangementBits }
        };
        Commit(candidate, candidateRevision); return true;
    }
    public bool EndBaselines(ulong generation, SceneEndBaselinesMessage value)
    {
        if (!Accept(generation)) return false;
        ArgumentNullException.ThrowIfNull(value);
        var current = Require(value.ObjectId);
        if (current.Phase == SceneObjectPhase.Finalized) return true;
        ulong candidateRevision = checked(revision + 1);
        var candidate = new SortedDictionary<ulong, SceneObjectSnapshot>(entries) {
            [value.ObjectId] = current with { Handle = new(registryId, SceneGeneration, value.ObjectId, candidateRevision), Phase = SceneObjectPhase.Finalized }
        };
        Commit(candidate, candidateRevision); return true;
    }
    public bool Destroy(ulong generation, SceneDestroyObjectMessage value)
    {
        if (!Accept(generation)) return false;
        ArgumentNullException.ThrowIfNull(value);
        if (!entries.ContainsKey(value.ObjectId)) return false;
        ulong candidateRevision = revision;
        var candidate = new SortedDictionary<ulong, SceneObjectSnapshot>(entries);
        DetachChildren(candidate, value.ObjectId, ref candidateRevision);
        candidate.Remove(value.ObjectId);
        Commit(candidate, candidateRevision); return true;
    }
    public void Reset()
    {
        if (IsClosed) throw new InvalidOperationException("Registry is closed.");
        Reset(checked(SceneGeneration + 1));
    }
    public void Reset(ulong generation)
    {
        if (IsClosed) throw new InvalidOperationException("Registry is closed.");
        if (generation <= SceneGeneration) throw new ArgumentOutOfRangeException(nameof(generation));
        var candidate = new SortedDictionary<ulong, SceneObjectSnapshot>();
        entries = candidate; SceneGeneration = generation; // Revision counter never rewinds.
    }
    public void Close()
    {
        if (IsClosed) return;
        entries = new(); IsClosed = true; // Closed flag invalidates handles even at maximum generation.
    }
    private bool Accept(ulong generation)
    {
        if (generation != SceneGeneration) return false;
        if (IsClosed) throw new InvalidOperationException("Registry is closed.");
        return true;
    }
    private SceneObjectSnapshot Require(ulong id) => entries.TryGetValue(id, out var value) ? value : throw new InvalidDataException("Missing scene object.");
    private static bool SameCreate(SceneCreateObjectByCrcMessage left, SceneCreateObjectByCrcMessage right) => SceneObjectLifecycleCodec.EncodeCreate(left).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(right));
    private static void ValidateCreate(SceneCreateObjectByCrcMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ObjectId == 0) throw new InvalidDataException("Registry reserves object ID zero for root containment.");
        _ = SceneObjectLifecycleCodec.EncodeCreate(value); // Enforce accepted DTO finite-field contract.
    }
    private static ulong Next(ref ulong value) => value = checked(value + 1);
    private void DetachChildren(SortedDictionary<ulong, SceneObjectSnapshot> candidate, ulong parentId, ref ulong candidateRevision)
    {
        foreach (var child in candidate.Values.Where(v => v.ContainerId == parentId).ToArray())
            candidate[child.Handle.ObjectId] = child with { Handle = new(registryId, SceneGeneration, child.Handle.ObjectId, Next(ref candidateRevision)), ContainerId = 0 };
    }
    private void Commit(SortedDictionary<ulong, SceneObjectSnapshot> candidate, ulong candidateRevision) { entries = candidate; revision = candidateRevision; }
}


