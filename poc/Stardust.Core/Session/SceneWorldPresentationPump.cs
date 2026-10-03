using Stardust.Core.Assets;

namespace Stardust.Core.Session;

/// <summary>Modern bounded owner-thread scheduling over the existing world and consumer.</summary>
public sealed class SceneWorldPresentationPump : ISceneWorldPresentationPump
{
    private sealed record Message(long Generation, byte[] Bytes);
    private sealed class Pending(ScenePreparedObjectPublication publication, long weight)
    {
        public ScenePreparedObjectPublication Publication { get; } = publication;
        public long Weight { get; } = weight;
        public int Attempts { get; set; }
    }
    private readonly SceneWorldCoordinator world;
    private readonly SceneWorldObjectPresentationConsumer consumer;
    private readonly SceneWorldPresentationPumpPolicy policy;
    private Func<SceneObjectResources, long>? weight;
    private Func<long, ReadOnlyMemory<byte>, bool>? applicationReceiver;
    private readonly Queue<Message> messages = new();
    private readonly List<Pending> publications = new();
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private long generation, queuedBytes, resourceWeight;
    private bool busy, closed, callbackReentered;
    private Task drain = Task.CompletedTask;
    public SceneWorldPresentationPump(SceneWorldCoordinator world,
        Func<ScenePreparedObjectPublication, SceneWorldObjectSnapshot, ISceneWorldObjectPresentation?> factory,
        SceneWorldPresentationPumpPolicy? policy = null, Func<SceneObjectResources, long>? pendingResourceWeight = null,
        Func<long, ReadOnlyMemory<byte>, bool>? applicationReceiver = null)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(factory);
        var limits = policy ?? new(); Validate(limits);
        _ = world.Snapshots(); // Verify coordinator owner affinity before acquiring close ownership.
        this.world = world; this.policy = limits with { }; weight = pendingResourceWeight ?? (resource => resource.SourceBytes);
        this.applicationReceiver = applicationReceiver ?? ((generation, message) => world.Accept(generation, message.Span));
        consumer = new(world, factory, limits.MaximumActors); generation = world.Generation;
    }
    private static void Validate(SceneWorldPresentationPumpPolicy p)
    {
        if (p.MaximumQueuedMessages is < 1 or > 65536 || p.MaximumQueuedBytes < 1 ||
            p.MaximumMessageBytes < 1 || p.MaximumMessageBytes > p.MaximumQueuedBytes ||
            p.MaximumMessagesPerStep < 1 || p.MaximumMessagesPerStep > p.MaximumQueuedMessages ||
            p.MaximumPendingPublications is < 1 or > 4096 || p.MaximumPublicationsPerStep < 1 ||
            p.MaximumPublicationsPerStep > p.MaximumPendingPublications || p.MaximumPublicationAttempts is < 1 or > 1024 ||
            p.MaximumChangeAttemptsPerStep is < 1 or > 16 || p.MaximumActors is < 1 or > 4096 ||
            p.MaximumPendingResourceWeight < 1) throw new ArgumentOutOfRangeException(nameof(p), "Invalid presentation pump policy.");
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Presentation pump owner thread required.");
        if (busy) { callbackReentered = true; throw new InvalidOperationException("Presentation pump callback reentrancy."); }
        busy = true;
    }
    private T Read<T>(Func<T> read) { Enter(); try { return read(); } finally { busy = false; } }
    public SceneWorldObjectPresentationConsumer Consumer => Read(() => consumer);
    public int QueuedMessageCount => Read(() => messages.Count);
    public long QueuedMessageBytes => Read(() => queuedBytes);
    public int PendingPublicationCount => Read(() => publications.Count);
    public long PendingResourceWeight => Read(() => resourceWeight);
    public Task DrainCompletion => Read(() => closed ? drain : consumer.DrainCompletion);
    public void QueueApplication(long generation, ReadOnlySpan<byte> message)
    {
        Enter(); try
        {
            if (closed) throw new ObjectDisposedException(nameof(SceneWorldPresentationPump));
            if (message.Length < 1 || message.Length > policy.MaximumMessageBytes) throw new ArgumentOutOfRangeException(nameof(message));
            if (messages.Count >= policy.MaximumQueuedMessages || message.Length > policy.MaximumQueuedBytes - queuedBytes)
                throw new InvalidDataException("Presentation message queue capacity.");
            long next = checked(queuedBytes + message.Length);
            var owned = message.ToArray(); messages.Enqueue(new(generation, owned)); queuedBytes = next;
        } catch (OverflowException) { CloseCore(); throw; } finally { busy = false; }
    }
    private void Healthy()
    {
        if (callbackReentered) throw new InvalidOperationException("Presentation pump callback attempted reentrancy.");
        if (world.State is ZoneSceneTransitionState.Failed or ZoneSceneTransitionState.Closed)
            throw new InvalidDataException("Presentation world is failed or closed.");
    }
    private int Synchronize()
    {
        int retired = 0;
        if (generation != world.Generation)
        {
            generation = world.Generation;
            consumer.RetireAll(); Healthy();
        }
        for (int i = publications.Count - 1; i >= 0; i--)
            if (!world.IsCurrent(publications[i].Publication)) { Remove(publications[i]); retired++; }
        return retired;
    }
    private void Remove(Pending item)
    { if (publications.Remove(item)) resourceWeight = checked(resourceWeight - item.Weight); }
    public SceneWorldPresentationPumpStep Step(double seconds)
    {
        Enter(); try
        {
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (closed) return new(0, 0, 0, 0, 0, false, 0, 0, false);
            if (world.State is ZoneSceneTransitionState.Failed or ZoneSceneTransitionState.Closed)
            { CloseCore(); return new(0, 0, 0, 0, 0, false, 0, 0, false); }
            int accepted = 0, dropped = 0, prepared = 0, published = 0, retired = 0;
            void Sync() { retired = checked(retired + Synchronize()); }
            Sync();
            for (int i = 0; i < policy.MaximumMessagesPerStep && messages.Count != 0; i++)
            {
                var item = messages.Dequeue(); queuedBytes -= item.Bytes.Length;
                try
                {
                    if (item.Generation != world.Generation) dropped++;
                    else if (applicationReceiver!(item.Generation, item.Bytes)) accepted++;
                    else dropped++;
                } finally { Array.Clear(item.Bytes); }
                Healthy(); Sync();
            }
            var poll = world.PollObjectResources(); Healthy(); Sync(); retired = checked(retired + poll.Retired.Count);
            var burst = new List<Pending>();
            foreach (var publication in poll.Prepared)
            {
                if (!world.IsCurrent(publication)) { retired++; continue; }
                long logical = weight!(publication.Resources); Healthy();
                if (logical < 0) throw new InvalidDataException("Negative pending resource weight.");
                if (!world.IsCurrent(publication)) { retired++; continue; }
                burst.Add(new(publication, logical));
            }
            Sync();
            retired = checked(retired + burst.RemoveAll(item => !world.IsCurrent(item.Publication)));
            long nextWeight = resourceWeight;
            foreach (var item in burst) nextWeight = checked(nextWeight + item.Weight);
            if (burst.Count > policy.MaximumPendingPublications - publications.Count || nextWeight > policy.MaximumPendingResourceWeight)
                throw new InvalidDataException("Pending resource burst capacity.");
            publications.AddRange(burst); resourceWeight = nextWeight; prepared = burst.Count;
            Sync();
            // Snapshot prevents a false return from being retried again in this same step.
            foreach (var item in publications.Take(policy.MaximumPublicationsPerStep).ToArray())
            {
                if (!publications.Contains(item)) continue;
                if (!world.IsCurrent(item.Publication)) { Remove(item); retired++; continue; }
                bool result = consumer.AcceptResource(item.Publication); Healthy();
                if (result) { Remove(item); published++; }
                else if (!world.IsCurrent(item.Publication)) { Remove(item); retired++; }
                else
                {
                    item.Attempts = checked(item.Attempts + 1);
                    if (item.Attempts >= policy.MaximumPublicationAttempts) throw new InvalidDataException("Publication acceptance retries exhausted.");
                    publications.Remove(item); publications.Add(item);
                }
                Sync();
            }
            bool acknowledged = false;
            for (int i = 0; i < policy.MaximumChangeAttemptsPerStep; i++)
            {
                var batch = world.DrainChanges(world.Generation);
                if (batch is null || batch.Entries.Count == 0) break;
                bool applied = consumer.ApplyBatch(batch); Healthy(); Sync();
                if (applied && consumer.AcknowledgeAppliedBatch(batch)) { acknowledged = true; break; }
                Healthy();
                if (batch.Generation != world.Generation) break; // External reset returns to host-controlled initialization.
                // A frame work budget leaves unapplied/current changes pending for the next step.
            }
            bool active = consumer.Tick(seconds); Healthy(); Sync();
            return new(accepted, dropped, prepared, published, retired, acknowledged, messages.Count, publications.Count, active);
        }
        catch (Exception error)
        {
            try { CloseCore(); } catch (Exception cleanup) { throw new AggregateException("Pump failure and cleanup failure.", error, cleanup); }
            throw;
        }
        finally { busy = false; }
    }
    private void CloseCore()
    {
        if (closed) return;
        closed = true;
        bool reentryBeforeCleanup = callbackReentered;
        foreach (var item in messages) Array.Clear(item.Bytes);
        messages.Clear(); queuedBytes = 0; publications.Clear(); resourceWeight = 0; weight = null; applicationReceiver = null;
        var errors = new List<Exception>();
        try { consumer.Dispose(); } catch (Exception error) { errors.Add(error); }
        try { world.Close(); } catch (Exception error) { errors.Add(error); }
        Task presentationDrain, worldDrain;
        try { presentationDrain = consumer.DrainCompletion; } catch (Exception error) { errors.Add(error); presentationDrain = Task.FromException(error); }
        try { worldDrain = world.DrainCompletion; } catch (Exception error) { errors.Add(error); worldDrain = Task.FromException(error); }
        drain = Task.WhenAll(presentationDrain, worldDrain);
        _ = drain.ContinueWith(static completed => { _ = completed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        if (!reentryBeforeCleanup && callbackReentered) errors.Add(new InvalidOperationException("Presentation cleanup callback attempted pump reentrancy."));
        if (errors.Count != 0) throw new AggregateException("Presentation pump cleanup failed.", errors);
    }
    public void Close() { Enter(); try { CloseCore(); } finally { busy = false; } }
    public void Dispose() => Close();
}
