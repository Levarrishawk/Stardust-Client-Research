using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed class SceneCreatureScalarSnapshot
{
    internal SceneCreatureScalarSnapshot(object owner, SceneObjectHandle handle, ulong revision, CreatureView3ScalarState state)
    { Owner = owner; Handle = handle; Revision = revision; State = state; }
    internal object Owner { get; }
    public SceneObjectHandle Handle { get; }
    public ulong Revision { get; }
    public CreatureView3ScalarState State { get; }
}

/// <summary>Modern caller-seeded six-scalar state, conservatively bound to an exact registry revision.</summary>
public sealed class SceneCreatureScalarStore : IDisposable
{
    private readonly SceneObjectRegistry registry;
    private readonly int capacity;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly object identity = new();
    private readonly SortedDictionary<ulong, SceneCreatureScalarSnapshot> entries = new();
    private ulong revision;
    private bool busy, closed;

    public SceneCreatureScalarStore(SceneObjectRegistry registry, int capacity = 64, ulong revisionSeed = 0)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (capacity is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.registry = registry; this.capacity = capacity; revision = revisionSeed;
    }

    public SceneCreatureScalarSnapshot? Seed(SceneObjectHandle handle, CreatureView3ScalarState initialState)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale();
            if (!registry.IsCurrent(handle)) return null;
            ArgumentNullException.ThrowIfNull(initialState);
            if (!float.IsFinite(initialState.Height)) throw new InvalidDataException("Finite caller-seeded height required.");
            if (entries.ContainsKey(handle.ObjectId)) throw new InvalidOperationException("Current handle already seeded.");
            if (entries.Count >= capacity) throw new InvalidDataException("Scalar store capacity exhausted.");
            ulong next = checked(revision + 1);
            var candidate = new SceneCreatureScalarSnapshot(identity, handle, next, initialState with { });
            entries.Add(handle.ObjectId, candidate); revision = next; return candidate;
        } finally { busy = false; }
    }

    public SceneCreatureScalarSnapshot? Apply(SceneObjectHandle handle, CreatureView3ScalarDeltaPlan plan)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale();
            if (!registry.IsCurrent(handle)) return null;
            ArgumentNullException.ThrowIfNull(plan);
            if (plan.ObjectId != handle.ObjectId) throw new InvalidDataException("Plan object does not match caller handle.");
            if (!entries.TryGetValue(handle.ObjectId, out var current) || current.Handle != handle)
                throw new InvalidOperationException("Explicit seed required for this exact registry revision.");
            var state = plan.Apply(current.State);
            ulong next = checked(revision + 1);
            var candidate = new SceneCreatureScalarSnapshot(identity, handle, next, state);
            entries[handle.ObjectId] = candidate; revision = next; return candidate;
        } finally { busy = false; }
    }

    public SceneCreatureScalarSnapshot? Find(SceneObjectHandle handle)
    {
        Enter(); try { RetireStale(); return !closed && registry.IsCurrent(handle) && entries.TryGetValue(handle.ObjectId, out var value) && value.Handle == handle ? value : null; }
        finally { busy = false; }
    }

    public bool IsCurrent(SceneCreatureScalarSnapshot snapshot)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(snapshot); RetireStale();
            return !closed && ReferenceEquals(snapshot.Owner, identity) && registry.IsCurrent(snapshot.Handle) &&
                entries.TryGetValue(snapshot.Handle.ObjectId, out var value) && value.Revision == snapshot.Revision && value.Handle == snapshot.Handle;
        } finally { busy = false; }
    }

    public IReadOnlyList<SceneCreatureScalarSnapshot> Snapshots()
    {
        Enter(); try { RetireStale(); return Array.AsReadOnly(entries.Values.ToArray()); }
        finally { busy = false; }
    }

    public void Close()
    {
        Enter(); try { closed = true; entries.Clear(); }
        finally { busy = false; }
    }
    public void Dispose() => Close();
    private void RetireStale()
    { foreach (ulong id in entries.Where(row => !registry.IsCurrent(row.Value.Handle)).Select(row => row.Key).ToArray()) entries.Remove(id); }
    private void EnsureOpen() => ObjectDisposedException.ThrowIf(closed, this);
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Scalar store serialized owner thread required.");
        if (busy) throw new InvalidOperationException("Scalar store owner is reentrant.");
        busy = true;
    }
}
