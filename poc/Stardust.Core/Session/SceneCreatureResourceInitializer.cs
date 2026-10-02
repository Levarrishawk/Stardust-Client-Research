using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Owned modern load configuration. Injected loaders must separately snapshot their captured configuration.</summary>
public sealed class SceneCreatureResourceRequest
{
    public SceneObjectSnapshot Snapshot { get; }
    public ObjectTemplateCatalog Catalog { get; }
    public LogicalAnimationClipSelection Idle { get; }
    public IReadOnlyDictionary<string, LogicalAnimationClipSelection> Movement { get; }
    public int MaximumResources { get; }
    public long MaximumSourceBytes { get; }
    public SceneCreatureResourceRequest(SceneObjectSnapshot snapshot, ObjectTemplateCatalog catalog,
        LogicalAnimationClipSelection idle, IReadOnlyDictionary<string, LogicalAnimationClipSelection> movement,
        int maximumResources = 64, long maximumSourceBytes = 16 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(movement);
        if (maximumResources is < 1 or > 4096 || maximumSourceBytes is < 1 or > 134217728) throw new ArgumentOutOfRangeException(nameof(maximumResources));
        _ = SceneObjectLifecycleCodec.EncodeCreate(snapshot.Create); _ = catalog.Require(snapshot.Create.TemplateCrc);
        if (movement.Count != 2 || !movement.ContainsKey("walk") || !movement.ContainsKey("run")) throw new ArgumentException("Explicit walk/run bank required.");
        Snapshot = snapshot with { Create = snapshot.Create with { } }; Catalog = catalog;
        Idle = Copy(idle); Movement = new ReadOnlyDictionary<string, LogicalAnimationClipSelection>(new Dictionary<string, LogicalAnimationClipSelection>(StringComparer.Ordinal)
        { ["walk"] = Copy(movement["walk"]), ["run"] = Copy(movement["run"]) });
        MaximumResources = maximumResources; MaximumSourceBytes = maximumSourceBytes;
    }
    private static LogicalAnimationClipSelection Copy(LogicalAnimationClipSelection value)
    {
        ArgumentNullException.ThrowIfNull(value); ObjectTemplateCatalog.ValidatePin(value.ClipSha256);
        string path = LegacyAssetPath.Normalize(value.TablePath);
        if (!path.StartsWith("appearance/lat/", StringComparison.Ordinal) || !path.EndsWith(".lat", StringComparison.Ordinal) || path.Split('/').Any(p => p is "." or "..")) throw new ArgumentException("Logical table path required.");
        LogicalAnimationTableDecoder.ValidateName(value.LogicalName);
        if (value.ExplicitChildRoute is { Count: > 8 }) throw new ArgumentException("Route budget.");
        int[]? route = value.ExplicitChildRoute?.ToArray();
        if (route is not null && route.Any(i => i is < 0 or > 4095)) throw new ArgumentException("Invalid explicit route.");
        return value with { TablePath = path, ClipSha256 = value.ClipSha256.ToUpperInvariant(), ExplicitChildRoute = route is null ? null : Array.AsReadOnly(route) };
    }
}

public readonly record struct SceneCreatureResourceTicket(Guid InitializerId, SceneObjectHandle Handle, ulong Attempt);
public enum SceneCreatureResourceCompletionKind { Prepared, Faulted, Canceled, Stale }
public sealed record SceneCreatureResourceCompletion(SceneCreatureResourceTicket Ticket, SceneCreatureResourceCompletionKind Kind,
    SceneCreatureResources? Resources, Exception? Error);

/// <summary>One serialized owner; bounded jobs include cancellation-ignoring workers. Poll alone transfers results.</summary>
public sealed class SceneCreatureResourceInitializer : IDisposable
{
    private sealed class Job(SceneCreatureResourceTicket ticket, SceneCreatureResourceRequest request)
    {
        public readonly SceneCreatureResourceTicket Ticket = ticket;
        public readonly SceneCreatureResourceRequest Request = request;
        public readonly CancellationTokenSource Cancellation = new();
        public Task<SceneCreatureResources>? Task;
        public bool Canceled;
        public Exception? CancellationError;
        public void Cancel()
        {
            if (Canceled) return; Canceled = true;
            try { Cancellation.Cancel(); } catch (Exception error) { CancellationError = error; }
        }
    }
    private readonly SceneObjectRegistry registry;
    private readonly Func<SceneCreatureResourceRequest, CancellationToken, Task<SceneCreatureResources>> loader;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly Guid identity = Guid.NewGuid();
    private readonly int maximumObjects, maximumOutstandingJobs;
    private readonly Dictionary<ulong, SceneCreatureResourceTicket> current = new();
    private readonly List<Job> jobs = new();
    private ulong attempt;
    private int outstanding;
    private bool busy, disposed;
    public int OutstandingJobs => Volatile.Read(ref outstanding);
    public bool IsDisposed => disposed;
    public Task DrainCompletion { get; private set; } = Task.CompletedTask;
    public int TrackedObjects { get { Enter(); try { return current.Count; } finally { busy = false; } } }
    public SceneCreatureResourceInitializer(SceneObjectRegistry registry,
        Func<SceneCreatureResourceRequest, CancellationToken, Task<SceneCreatureResources>> loader,
        int maximumOutstandingJobs = 8, int maximumObjects = 64)
    {
        ArgumentNullException.ThrowIfNull(registry); ArgumentNullException.ThrowIfNull(loader);
        if (maximumOutstandingJobs is < 1 or > 4096 || maximumObjects is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(maximumOutstandingJobs));
        this.registry = registry; this.loader = loader; this.maximumOutstandingJobs = maximumOutstandingJobs; this.maximumObjects = maximumObjects;
    }
    public SceneCreatureResourceTicket? Find(ulong objectId)
    { Enter(); try { return current.TryGetValue(objectId, out var value) ? value : null; } finally { busy = false; } }
    public bool IsCurrent(SceneCreatureResourceTicket ticket)
    { Enter(); try { return current.TryGetValue(ticket.Handle.ObjectId, out var value) && value == ticket && registry.IsCurrent(ticket.Handle); } finally { busy = false; } }
    public SceneCreatureResourceTicket Start(SceneCreatureResourceRequest request, bool restart = false)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(request); Require(Current(request), "Stale/foreign/altered create snapshot.");
            ulong id = request.Snapshot.Handle.ObjectId;
            if (current.TryGetValue(id, out var previous) && previous.Handle == request.Snapshot.Handle && !restart) return previous;
            Require(jobs.Count < maximumOutstandingJobs && (current.ContainsKey(id) || current.Count < maximumObjects), "Initializer capacity exhausted; poll completed/stale work first.");
            ulong next = checked(attempt + 1); var ticket = new SceneCreatureResourceTicket(identity, request.Snapshot.Handle, next);
            var job = new Job(ticket, request); // Allocate before canceling the previous attempt.
            jobs.EnsureCapacity(jobs.Count + 1); current.EnsureCapacity(current.Count + 1);
            foreach (var old in jobs.Where(j => j.Ticket.Handle.ObjectId == id)) old.Cancel();
            jobs.Add(job); current[id] = ticket; attempt = next; Interlocked.Increment(ref outstanding);
            try { job.Task = loader(request, job.Cancellation.Token) ?? Task.FromException<SceneCreatureResources>(new InvalidDataException("Loader returned no task.")); }
            catch (Exception error) { job.Task = Task.FromException<SceneCreatureResources>(error); }
            return ticket;
        } finally { busy = false; }
    }
    public bool Cancel(SceneCreatureResourceTicket ticket)
    {
        Enter(); try
        {
            if (!current.TryGetValue(ticket.Handle.ObjectId, out var value) || value != ticket) return false;
            var job = jobs.SingleOrDefault(j => j.Ticket == ticket); if (job is null) return false;
            job.Cancel(); return true;
        } finally { busy = false; }
    }
    public IReadOnlyList<SceneCreatureResourceCompletion> Poll()
    {
        Enter(); try
        {
            foreach (var pair in current.ToArray())
                if (!registry.IsCurrent(pair.Value.Handle)) current.Remove(pair.Key);
            foreach (var job in jobs)
                if (!Current(job.Request) || !current.TryGetValue(job.Ticket.Handle.ObjectId, out var value) || value != job.Ticket) job.Cancel();
            var completed = jobs.Where(j => j.Task!.IsCompleted).ToArray();
            var result = new List<SceneCreatureResourceCompletion>(completed.Length);
            foreach (var job in completed)
            {
                var task = job.Task!; SceneCreatureResources? resources = null; Exception? error = job.CancellationError;
                bool stale = !Current(job.Request) || !current.TryGetValue(job.Ticket.Handle.ObjectId, out var value) || value != job.Ticket;
                SceneCreatureResourceCompletionKind kind;
                if (task.IsFaulted) { error = task.Exception!; kind = SceneCreatureResourceCompletionKind.Faulted; }
                else if (task.IsCanceled || job.Canceled) kind = SceneCreatureResourceCompletionKind.Canceled;
                else
                {
                    resources = task.GetAwaiter().GetResult();
                    try
                    {
                        Require(resources is not null && SameCreate(resources.Create, job.Request.Snapshot.Create) && resources.Recipe == job.Request.Catalog.Require(resources.Create.TemplateCrc), "Loader result create/recipe identity mismatch.");
                        ValidatePrepared(resources!, job.Request);
                        kind = SceneCreatureResourceCompletionKind.Prepared;
                    }
                    catch (Exception failure) { resources = null; error = failure; kind = SceneCreatureResourceCompletionKind.Faulted; }
                }
                if (stale) { kind = SceneCreatureResourceCompletionKind.Stale; resources = null; }
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
    private static void ValidatePrepared(SceneCreatureResources result, SceneCreatureResourceRequest request)
    {
        var creature = result.Creature; var recipe = request.Catalog.Require(request.Snapshot.Create.TemplateCrc);
        Require(creature is not null && creature.Profile is not null && creature.Appearance is not null && creature.Sources is not null, "Incomplete creature result.");
        var sources = creature!.Sources;
        Require(sources.Count > 0 && sources.Count <= request.MaximumResources && result.ExternalReads == sources.Count &&
            sources.All(p => p.Value is not null && p.Key == p.Value.Path && p.Value.Bytes > 0) &&
            result.SourceBytes == sources.Values.Sum(p => (long)p.Bytes) && result.SourceBytes <= request.MaximumSourceBytes, "Creature source accounting mismatch.");
        bool Source(string path, string? pin = null) => sources.TryGetValue(path, out var identity) && (pin is null || identity.Sha256 == pin);
        Require(creature.Profile.RootPath == recipe.ObjectPath && Source(recipe.ObjectPath, recipe.ObjectSha256) &&
            Source(creature.Profile.AppearancePath, recipe.AppearanceSha256), "Creature root/appearance identity mismatch.");
        Require(creature.Skeletons is { Count: > 0 and <= 64 } && creature.Geometries is { Count: > 0 and <= 64 } &&
            creature.SkinSources is { Count: > 0 and <= 64 } && creature.LodMeshes is { Count: > 0 and <= 64 } && creature.LogicalTables is { Count: > 0 and <= 64 } &&
            creature.MaterialBundle is not null && creature.SelectedAnimation is not null && creature.PreparedAnimation is not null &&
            creature.AdditionalAnimations is { Count: 2 }, "Missing prepared creature closure.");
        Require(creature.Appearance.Skeletons.Count > 0 && creature.Appearance.MeshGenerators.Count > 0 &&
            creature.Appearance.Skeletons.All(p => Source(p.Path) && creature.Skeletons!.TryGetValue(p.Path, out var value) && value is not null && value.ExposedLevels().FirstOrDefault() is { Joints.Count: > 0 }) &&
            creature.Appearance.MeshGenerators.All(p => Source(p) && creature.LodMeshes!.TryGetValue(p, out var value) && value is not null && value.SerializedPaths.Count > 0 && creature.Geometries!.ContainsKey(value.SerializedPaths[0])), "Missing skeleton/detail0 geometry references.");
        var materials = creature.MaterialBundle!;
        Require(materials.Materials is { Count: > 0 and <= 64 } && materials.Textures is { Count: > 0 and <= 64 } && materials.Effects is { Count: > 0 and <= 64 } && materials.PaletteDefaults is not null,
            "Missing authored material closure.");
        Require(creature.Geometries!.All(p => Source(p.Key) && p.Value is not null && p.Value.Skin is not null && creature.SkinSources!.TryGetValue(p.Key, out var skin) && skin is not null &&
            p.Value.Shaders.Count > 0 && p.Value.Shaders.All(shader => materials.Materials.ContainsKey(shader.ShaderPath))) &&
            materials.Materials.All(p => Source(p.Key) && p.Value?.References is { } references && p.Value.Parameters is not null &&
                Source(references.EffectPath) && materials.Effects.TryGetValue(references.EffectPath, out var effect) && effect is not null &&
                references.Textures.All(t => t.AssetPath is string path && Source(path) && materials.Textures.ContainsKey(path))) &&
            materials.Textures.All(p => Source(p.Key) && p.Value is { Mips.Count: > 0 }), "Incomplete geometry/material dependencies.");
        void Clip(PreparedSkeletalAnimation clip, LogicalAnimationClipSelection selection)
        {
            Require(clip is not null && clip.ClipSha256 == selection.ClipSha256 && Source(clip.ClipPath, selection.ClipSha256) &&
                creature.Skeletons!.ContainsKey(clip.SkeletonPath) && creature.LogicalTables!.TryGetValue(selection.TablePath, out var table) && table is not null && Source(selection.TablePath) && clip.EndFrame > 0 && float.IsFinite(clip.FrameRate) && clip.FrameRate > 0,
                "Prepared clip identity/dependency mismatch.");
        }
        var selected = creature.SelectedAnimation!;
        Require(selected.Selection is not null && selected.Proxy is not null && selected.Clip is not null && selected.Selection.TablePath == request.Idle.TablePath &&
            selected.Selection.LogicalName == request.Idle.LogicalName && selected.Selection.ClipSha256 == request.Idle.ClipSha256 &&
            (selected.Selection.ExplicitChildRoute ?? Array.Empty<int>()).SequenceEqual(request.Idle.ExplicitChildRoute ?? Array.Empty<int>()) &&
            selected.Proxy.ClipPath == creature.PreparedAnimation!.ClipPath, "Selected idle identity mismatch.");
        Clip(creature.PreparedAnimation!, request.Idle);
        foreach (string key in new[] { "walk", "run" })
        {
            Require(creature.AdditionalAnimations!.TryGetValue(key, out var clip) && clip is not null && clip.Locomotion is not null && clip.Locomotion.AuthoredSpeed > 0, "Missing prepared movement clip.");
            Clip(clip!, request.Movement[key]);
        }
    }
    private bool Current(SceneCreatureResourceRequest request) => registry.IsCurrent(request.Snapshot.Handle) &&
        registry.Find(request.Snapshot.Handle.ObjectId) is { } snapshot && snapshot.Phase == request.Snapshot.Phase && snapshot.ContainerId == request.Snapshot.ContainerId && snapshot.ArrangementBits == request.Snapshot.ArrangementBits && SameCreate(snapshot.Create, request.Snapshot.Create);
    private static bool SameCreate(SceneCreateObjectByCrcMessage a, SceneCreateObjectByCrcMessage b) => SceneObjectLifecycleCodec.EncodeCreate(a).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(b));
    private void CheckOwner() { if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Initializer serialized owner thread required."); }
    private void Enter() { CheckOwner(); ObjectDisposedException.ThrowIf(disposed, this); if (busy) throw new InvalidOperationException("Initializer owner is reentrant."); busy = true; }
    private static void Require([DoesNotReturnIf(false)] bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
