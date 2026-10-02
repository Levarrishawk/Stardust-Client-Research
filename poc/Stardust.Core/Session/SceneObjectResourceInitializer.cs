using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed class SceneObjectResourceRequest
{
    public SceneObjectSnapshot Snapshot { get; }
    public ObjectTemplateCatalog Catalog { get; }
    public SceneObjectResourceOptions Options { get; }
    public SceneObjectResourceRequest(SceneObjectSnapshot snapshot, ObjectTemplateCatalog catalog, SceneObjectResourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(options);
        _ = SceneObjectLifecycleCodec.EncodeCreate(snapshot.Create);
        if (catalog.Require(snapshot.Create.TemplateCrc).Recipe != options.Recipe) throw new InvalidDataException("Explicit options/recipe mismatch.");
        Snapshot = snapshot with { Create = snapshot.Create with { } }; Catalog = catalog; Options = CopyOptions(options);
    }
    internal static SceneObjectResourceOptions CopyOptions(SceneObjectResourceOptions options) => options switch
    {
        SceneCreatureResourceOptions c => new SceneCreatureResourceOptions(c.Idle, c.Movement, c.LoadAlbedoFactors, c.MaximumResources, c.MaximumSourceBytes),
        SceneStaticResourceOptions s => new SceneStaticResourceOptions(s.MaterialPolicy, s.SwitchTextureFrame, s.Limits, s.MaximumDecodedBytes),
        _ => throw new InvalidDataException("Unsupported explicit options.")
    };
}

public readonly record struct SceneObjectResourceTicket(Guid InitializerId, SceneObjectHandle Handle, ulong Attempt);
public enum SceneObjectResourceCompletionKind { Prepared, Faulted, Canceled, Stale }
public sealed record SceneObjectResourceCompletion(SceneObjectResourceTicket Ticket, SceneObjectResourceCompletionKind Kind,
    SceneObjectResources? Resources, Exception? Error);

/// <summary>One serialized owner; bounded jobs include cancellation-ignoring workers. Poll alone transfers results.</summary>
public sealed class SceneObjectResourceInitializer : IDisposable
{
    private sealed class Job(SceneObjectResourceTicket ticket, SceneObjectResourceRequest request)
    {
        public readonly SceneObjectResourceTicket Ticket = ticket;
        public readonly SceneObjectResourceRequest Request = request;
        public readonly CancellationTokenSource Cancellation = new();
        public Task<SceneObjectResources>? Task;
        public bool Canceled;
        public Exception? CancellationError;
        public void Cancel()
        {
            if (Canceled) return; Canceled = true;
            try { Cancellation.Cancel(); } catch (Exception error) { CancellationError = error; }
        }
    }
    private readonly SceneObjectRegistry registry;
    private readonly Func<SceneObjectResourceRequest, CancellationToken, Task<SceneObjectResources>> loader;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly Guid identity = Guid.NewGuid();
    private readonly int maximumObjects, maximumOutstandingJobs;
    private readonly Dictionary<ulong, SceneObjectResourceTicket> current = new();
    private readonly List<Job> jobs = new();
    private ulong attempt;
    private int outstanding;
    private bool busy, disposed;
    public int OutstandingJobs => Volatile.Read(ref outstanding);
    public bool IsDisposed => disposed;
    public Task DrainCompletion { get; private set; } = Task.CompletedTask;
    public int TrackedObjects { get { Enter(); try { return current.Count; } finally { busy = false; } } }
    public SceneObjectResourceInitializer(SceneObjectRegistry registry,
        Func<SceneObjectResourceRequest, CancellationToken, Task<SceneObjectResources>> loader,
        int maximumOutstandingJobs = 8, int maximumObjects = 64, ulong attemptSeed = 0)
    {
        ArgumentNullException.ThrowIfNull(registry); ArgumentNullException.ThrowIfNull(loader);
        if (maximumOutstandingJobs is < 1 or > 4096 || maximumObjects is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(maximumOutstandingJobs));
        attempt = attemptSeed; this.registry = registry; this.loader = loader; this.maximumOutstandingJobs = maximumOutstandingJobs; this.maximumObjects = maximumObjects;
    }
    public SceneObjectResourceTicket? Find(ulong objectId)
    { Enter(); try { return current.TryGetValue(objectId, out var value) ? value : null; } finally { busy = false; } }
    public bool IsCurrent(SceneObjectResourceTicket ticket)
    { Enter(); try { return current.TryGetValue(ticket.Handle.ObjectId, out var value) && value == ticket && registry.IsCurrent(ticket.Handle); } finally { busy = false; } }
    public SceneObjectResourceTicket Start(SceneObjectResourceRequest request, bool restart = false)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(request); Require(Current(request), "Stale/foreign/altered create snapshot.");
            ulong id = request.Snapshot.Handle.ObjectId;
            if (current.TryGetValue(id, out var previous) && previous.Handle == request.Snapshot.Handle && !restart) return previous;
            Require(jobs.Count < maximumOutstandingJobs && (current.ContainsKey(id) || current.Count < maximumObjects), "Initializer capacity exhausted; poll completed/stale work first.");
            ulong next = checked(attempt + 1); var ticket = new SceneObjectResourceTicket(identity, request.Snapshot.Handle, next);
            var job = new Job(ticket, request); // Allocate before canceling the previous attempt.
            jobs.EnsureCapacity(jobs.Count + 1); current.EnsureCapacity(current.Count + 1);
            foreach (var old in jobs.Where(j => j.Ticket.Handle.ObjectId == id)) old.Cancel();
            jobs.Add(job); current[id] = ticket; attempt = next; Interlocked.Increment(ref outstanding);
            try { job.Task = loader(request, job.Cancellation.Token) ?? Task.FromException<SceneObjectResources>(new InvalidDataException("Loader returned no task.")); }
            catch (Exception error) { job.Task = Task.FromException<SceneObjectResources>(error); }
            return ticket;
        } finally { busy = false; }
    }
    public bool Cancel(SceneObjectResourceTicket ticket)
    {
        Enter(); try
        {
            if (!current.TryGetValue(ticket.Handle.ObjectId, out var value) || value != ticket) return false;
            var job = jobs.SingleOrDefault(j => j.Ticket == ticket); if (job is null) return false;
            job.Cancel(); return true;
        } finally { busy = false; }
    }
    public IReadOnlyList<SceneObjectResourceCompletion> Poll()
    {
        Enter(); try
        {
            foreach (var pair in current.ToArray())
                if (!registry.IsCurrent(pair.Value.Handle)) current.Remove(pair.Key);
            foreach (var job in jobs)
                if (!Current(job.Request) || !current.TryGetValue(job.Ticket.Handle.ObjectId, out var value) || value != job.Ticket) job.Cancel();
            var completed = jobs.Where(j => j.Task!.IsCompleted).ToArray();
            var result = new List<SceneObjectResourceCompletion>(completed.Length);
            foreach (var job in completed)
            {
                var task = job.Task!; SceneObjectResources? resources = null; Exception? error = job.CancellationError;
                bool stale = !Current(job.Request) || !current.TryGetValue(job.Ticket.Handle.ObjectId, out var value) || value != job.Ticket;
                SceneObjectResourceCompletionKind kind;
                if (task.IsFaulted) { error = task.Exception!; kind = SceneObjectResourceCompletionKind.Faulted; }
                else if (task.IsCanceled || job.Canceled) kind = SceneObjectResourceCompletionKind.Canceled;
                else
                {
                    resources = task.GetAwaiter().GetResult();
                    try
                    {
                        Require(resources is not null && SameCreate(resources.Create, job.Request.Snapshot.Create) && resources.Recipe == job.Request.Catalog.Require(resources.Create.TemplateCrc), "Loader result create/recipe identity mismatch.");
                        ValidatePrepared(resources!, job.Request);
                        kind = SceneObjectResourceCompletionKind.Prepared;
                    }
                    catch (Exception failure) { resources = null; error = failure; kind = SceneObjectResourceCompletionKind.Faulted; }
                }
                if (stale) { kind = SceneObjectResourceCompletionKind.Stale; resources = null; }
                if (job.Canceled) resources = null;
                result.Add(new(job.Ticket, kind, resources, error));
                job.Task = null; job.Cancellation.Dispose(); jobs.Remove(job); Interlocked.Decrement(ref outstanding);
            }
            return Array.AsReadOnly(result.ToArray());
        } finally { busy = false; }
    }
    public void Dispose()
    {
        CheckOwner(); if (disposed) return; if (busy) throw new InvalidOperationException("Initializer owner is reentrant.");
        busy = true; try
        {
            disposed = true; current.Clear();
            foreach (var job in jobs) job.Cancel();
            DrainCompletion = Task.WhenAll(jobs.Select(job => job.Task!.ContinueWith(task =>
            {
                _ = task.Exception; // Observe late faults even after the owner has stopped polling.
                job.Task = null; job.Cancellation.Dispose(); Interlocked.Decrement(ref outstanding);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)).ToArray());
            jobs.Clear();
        } finally { busy = false; }
    }
    private static void ValidatePrepared(SceneObjectResources result, SceneObjectResourceRequest request)
        => SceneObjectResourceValidation.Validate(result, request.Snapshot.Create, request.Catalog, request.Options);
    private bool Current(SceneObjectResourceRequest request) => registry.IsCurrent(request.Snapshot.Handle) &&
        registry.Find(request.Snapshot.Handle.ObjectId) is { } snapshot && snapshot.Phase == request.Snapshot.Phase && snapshot.ContainerId == request.Snapshot.ContainerId && snapshot.ArrangementBits == request.Snapshot.ArrangementBits && SameCreate(snapshot.Create, request.Snapshot.Create);
    private static bool SameCreate(SceneCreateObjectByCrcMessage a, SceneCreateObjectByCrcMessage b) => SceneObjectLifecycleCodec.EncodeCreate(a).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(b));
    private void CheckOwner() { if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Initializer serialized owner thread required."); }
    private void Enter() { CheckOwner(); ObjectDisposedException.ThrowIf(disposed, this); if (busy) throw new InvalidOperationException("Initializer owner is reentrant."); busy = true; }
    private static void Require([DoesNotReturnIf(false)] bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
