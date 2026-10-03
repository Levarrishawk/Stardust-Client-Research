namespace Stardust.Core.Session;

/// <summary>Serialized, bounded ownership of heterogeneous world presentation adapters.</summary>
public sealed class SceneWorldObjectPresentationConsumer : IDisposable
{
    private sealed record Actor(ScenePreparedObjectPublication Resource, SceneObjectIncarnation Incarnation, ISceneWorldObjectPresentation Adapter);
    private SceneWorldCoordinator? world;
    private Func<ScenePreparedObjectPublication, SceneWorldObjectSnapshot, ISceneWorldObjectPresentation?>? factory;
    private readonly Dictionary<ulong, Actor> actors = new();
    private readonly List<Task> draining = new();
    private readonly int maximumDrainingPresentations;
    private sealed class DrainGroup(Exception? initialFailure)
    {
        private readonly object gate = new();
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int pending;
        private bool sealedGroup;
        private Exception? failure = initialFailure;
        public Task Completion => completion.Task;
        public void Fail(Exception error) { lock(gate) failure ??= error; }
        public bool TryAdd(Task task)
        {
            lock(gate) { if(sealedGroup)return false; pending++; }
            _ = task.ContinueWith(static (finished,state) => ((DrainGroup)state!).Finish(finished),this,
                CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            return true;
        }
        private void Finish(Task task)
        {
            lock(gate)
            {
                if(task.IsFaulted)failure ??= task.Exception!; else if(task.IsCanceled)failure ??= new TaskCanceledException(task);
                if(--pending!=0)return;
                sealedGroup=true;
                if(failure is null)completion.SetResult(); else completion.SetException(failure);
            }
        }
    }
    private DrainGroup? drainGroup;
    private Task? drainFailure;
    private readonly int maximumActors, ownerThread = Environment.CurrentManagedThreadId;
    private long generation;
    private SceneWorldChangeBatch? applied;
    private bool busy, disposed;
    public SceneWorldObjectPresentationConsumer(SceneWorldCoordinator world,
        Func<ScenePreparedObjectPublication, SceneWorldObjectSnapshot, ISceneWorldObjectPresentation?> factory, int maximumActors = 64, int? maximumDrainingPresentations = null)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(factory);
        if (maximumActors is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(maximumActors));
        int drainingLimit = maximumDrainingPresentations ?? checked(maximumActors * 2);
        if (drainingLimit < maximumActors || drainingLimit > 8192) throw new ArgumentOutOfRangeException(nameof(maximumDrainingPresentations));
        this.maximumDrainingPresentations = drainingLimit;
        this.world = world; this.factory = factory; this.maximumActors = maximumActors; generation = world.Generation;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Presentation owner thread required.");
        if (busy) throw new InvalidOperationException("Presentation callback reentrancy.");
        busy = true;
    }
    public int Count { get { Enter(); try { Boundary(); return actors.Count; } finally { busy = false; } } }
    public ISceneWorldObjectPresentation? Find(ulong id)
    { Enter(); try { Boundary(); return actors.GetValueOrDefault(id)?.Adapter; } finally { busy = false; } }
    private bool Active => !disposed && world!.State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
    private bool Boundary(ulong? deferredRetirement = null, bool allowDrainFailure = false)
    {
        ReapDrains();
        if (disposed) return false;
        if (!allowDrainFailure && drainFailure is not null) throw new InvalidDataException("Retired presentation drain failed; dispose the consumer.", drainFailure.Exception);
        if (generation != world!.Generation || !Active) { Clear(); generation = world.Generation; }
        foreach (var pair in actors.ToArray()) if (pair.Key != deferredRetirement && !Current(pair.Value)) Retire(pair.Key);
        return Active;
    }
    private SceneWorldObjectSnapshot? Latest(ScenePreparedObjectPublication resource)
    {
        if (!world!.IsCurrent(resource)) return null;
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle == resource.Ticket.Handle);
        return snapshot is { Object.Phase: SceneObjectPhase.Finalized } &&
            (snapshot.Replication is null || world.IsCurrent(snapshot.Replication)) ? snapshot : null;
    }
    private bool Current(Actor actor) => Latest(actor.Resource) is { } latest && latest.Object.Incarnation == actor.Incarnation;
    private bool Current(Actor actor, SceneWorldObjectSnapshot snapshot) => Latest(actor.Resource) is { } latest &&
        latest.Object.Incarnation == actor.Incarnation && snapshot.Object.Handle == actor.Resource.Ticket.Handle &&
        snapshot.Object.Incarnation == actor.Incarnation && snapshot.Object.Phase == SceneObjectPhase.Finalized &&
        snapshot.Envelopes.SequenceEqual(latest.Envelopes) &&
        (snapshot.Replication is null ? latest.Replication is null : world!.IsCurrent(snapshot.Replication));
    // No callbacks mutate owner state from worker threads. Fault observation continuations retain only their task.
    private static void ObserveFault(Task task) => _ = task.ContinueWith(static completed => { _ = completed.Exception; },
        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    private void RecordDrainFailure(Exception error)
    {
        if (drainFailure is not null) return; // One bounded sticky diagnostic; every later task fault is still observed.
        drainFailure = Task.FromException(error); ObserveFault(drainFailure); drainGroup?.Fail(error);
    }
    private void ReapDrains()
    {
        for (int i = draining.Count - 1; i >= 0; i--)
        {
            var task = draining[i]; if (!task.IsCompleted) continue;
            if (task.IsFaulted) RecordDrainFailure(task.Exception!);
            else if (task.IsCanceled) RecordDrainFailure(new TaskCanceledException(task));
            draining.RemoveAt(i);
        }
    }
    public int DrainingPresentationCount { get { Enter(); try { ReapDrains(); return draining.Count; } finally { busy = false; } } }
    public Task DrainCompletion
    {
        get
        {
            Enter(); try
            {
                ReapDrains();
                // One task per overlapping drain group: repeated reads do not create joins behind a long-lived worker.
                return draining.Count==0 ? drainFailure ?? Task.CompletedTask : drainGroup!.Completion;
            } finally { busy = false; }
        }
    }
    private void DisposeAdapter(ISceneWorldObjectPresentation adapter)
    {
        Exception? disposal = null;
        try { adapter.Dispose(); } catch (Exception error) { disposal = error; }
        try
        {
            if (adapter is ISceneWorldObjectPresentationDrain owned)
            {
                Task task;
                try { task = owned.DrainCompletion ?? throw new InvalidDataException("Null presentation drain task."); }
                catch (Exception error) { RecordDrainFailure(error); throw; }
                ReapDrains(); // Reclaim completed tasks before retaining the just-retired task, even when a previous one failed.
                ObserveFault(task);
                if (task.IsCompleted)
                {
                    if (task.IsFaulted) RecordDrainFailure(task.Exception!);
                    else if (task.IsCanceled) RecordDrainFailure(new TaskCanceledException(task));
                }
                else if (!draining.Any(previous => ReferenceEquals(previous, task)))
                {
                    // Admission reserves a slot for every actor and the possible rollback candidate.
                    if (draining.Count >= maximumDrainingPresentations) throw new InvalidOperationException("Presentation drain reservation invariant.");
                    draining.Add(task);
                    if(drainGroup is null || !drainGroup.TryAdd(task))
                    {
                        drainGroup = new(drainFailure?.Exception);
                        ObserveFault(drainGroup.Completion);
                        _ = drainGroup.TryAdd(task);
                    }
                }
            }
        }
        catch (Exception error)
        {
            if (disposal is not null) throw new AggregateException("Presentation dispose and drain capture failed.", disposal, error);
            throw;
        }
        if (disposal is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disposal).Throw();
    }
    private void Retire(ulong id) { if (actors.Remove(id, out var actor)) DisposeAdapter(actor.Adapter); }
    private void Clear()
    {
        var owned = actors.Values.ToArray(); actors.Clear(); applied = null;
        List<Exception>? errors = null;
        foreach (var actor in owned) try { DisposeAdapter(actor.Adapter); } catch (Exception error) { (errors ??= new()).Add(error); }
        if (errors is not null) throw new AggregateException("Presentation retirement failed.", errors);
    }
    public bool AcceptResource(ScenePreparedObjectPublication publication)
    {
        Enter(); try { ArgumentNullException.ThrowIfNull(publication); return Accept(publication); } finally { busy = false; }
    }
    private bool Accept(ScenePreparedObjectPublication publication)
    {
        ulong id = publication.Ticket.Handle.ObjectId;
        if (!Boundary(id) || Latest(publication) is not { } snapshot) { Boundary(); return false; }
        if (actors.TryGetValue(id, out var existing) && ReferenceEquals(existing.Resource, publication)) return true;
        if (!actors.ContainsKey(id) && actors.Count >= maximumActors) throw new InvalidDataException("Presentation actor capacity.");
        ReapDrains();
        if (draining.Count + actors.Count >= maximumDrainingPresentations)
            throw new InvalidDataException("Presentation drain capacity; complete retired work before retrying admission.");
        ISceneWorldObjectPresentation? candidate = null;
        try
        {
            candidate = factory!(publication, snapshot);
            if (candidate is null) { Boundary(); return false; }
            if (actors.Values.Any(a => ReferenceEquals(a.Adapter, candidate)))
            {
                candidate = null; // Existing ownership cannot be transferred or rolled back as a candidate.
                throw new InvalidDataException("Factory returned an already owned adapter.");
            }
            if (!ReferenceEquals(candidate.Publication, publication)) throw new InvalidDataException("Adapter publication identity differs.");
            var owned = new Actor(publication, snapshot.Object.Incarnation, candidate);
            if (!Boundary(id) || !Current(owned, snapshot)) return false;
            if (!candidate.Apply(snapshot) || !Boundary(id) || !Current(owned, snapshot)) return false;
            Retire(id); // Candidate is prepared before the previous owned adapter is retired.
            if (!Boundary(id) || !Current(owned, snapshot)) return false;
            actors.Add(id, owned); candidate = null; return true;
        }
        finally { try { if (candidate is not null) DisposeAdapter(candidate); } finally { Boundary(allowDrainFailure: true); } }
    }
    public bool ApplyBatch(SceneWorldChangeBatch batch)
    { Enter(); try { ArgumentNullException.ThrowIfNull(batch); return Apply(batch); } finally { busy = false; } }
    private bool Apply(SceneWorldChangeBatch batch)
    {
        applied = null;
        if (!Boundary() || !world!.IsCurrent(batch)) return false;
        foreach (var entry in batch.Entries)
        {
            if (!world.IsCurrent(batch)) { Boundary(); return false; }
            if (entry.Current is null) { Retire(entry.ObjectId); if (!Boundary() || !world.IsCurrent(batch)) return false; continue; }
            var snapshot = entry.Current;
            if (snapshot.Object.Handle.ObjectId != entry.ObjectId) throw new InvalidDataException("Batch object identity differs.");
            if (snapshot.Replication is not null && !world.IsCurrent(snapshot.Replication)) return false;
            if (!actors.TryGetValue(entry.ObjectId, out var actor)) continue; // Delayed acceptance reads latest state.
            if (!Current(actor, snapshot)) { Retire(entry.ObjectId); continue; }
            bool success;
            try { success = actor.Adapter.Apply(snapshot); }
            catch { Retire(entry.ObjectId); Boundary(); throw; }
            if (!Boundary() || !world.IsCurrent(batch) || !Current(actor, snapshot)) return false;
            if (!success) return false;
        }
        if (!Boundary() || !world.IsCurrent(batch)) return false;
        applied = batch; return true;
    }
    public bool AcknowledgeAppliedBatch(SceneWorldChangeBatch batch)
    { Enter(); try { ArgumentNullException.ThrowIfNull(batch); return Acknowledge(batch); } finally { busy = false; } }
    private bool Acknowledge(SceneWorldChangeBatch batch)
    {
        if (!Boundary() || !ReferenceEquals(applied, batch) || !world!.IsCurrent(batch)) return false;
        bool result = world.AcknowledgeChanges(batch); applied = null; return result;
    }
    public bool ConsumePending(int maximumAttempts = 4)
    {
        Enter(); try
        {
            if (maximumAttempts is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
            if (!Boundary()) return false;
            for (int i = 0; i < maximumAttempts; i++)
            {
                var batch = world!.DrainChanges(generation);
                if (batch is null || batch.Entries.Count == 0) return false;
                if (Apply(batch) && Acknowledge(batch)) return true;
            }
            return false;
        } finally { busy = false; }
    }
    public bool Tick(double seconds)
    {
        Enter(); try
        {
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!Boundary()) return false;
            foreach (var pair in actors.ToArray())
            {
                if (!Current(pair.Value)) { Retire(pair.Key); continue; }
                bool success;
                try { success = pair.Value.Adapter.Tick(seconds); }
                catch { Retire(pair.Key); Boundary(); throw; }
                if (!success) Retire(pair.Key);
                if (!Boundary()) return false;
            }
            return true;
        } finally { busy = false; }
    }
    public void RetireAll() { Enter(); try { Clear(); } finally { busy = false; } }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return;
            disposed = true;
            try { Clear(); } finally { world = null; factory = null; }
        } finally { busy = false; }
    }
}
