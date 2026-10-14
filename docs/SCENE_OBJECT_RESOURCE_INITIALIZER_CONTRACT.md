# Generic scene object resource initializer

R14-R,2026-10-02. [SceneObjectResourceInitializer](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneObjectResourceInitializer.cs) schedules the accepted [sealed resource router](SCENE_OBJECT_RESOURCE_ROUTER_CONTRACT.md) results for both catalog recipes. This is modern engine-neutral asynchronous ownership policy. It adds no source discovery, parser, executor thread or implicit recipe options.

## Exact API

```csharp
public sealed class SceneObjectResourceRequest
{
    public SceneObjectSnapshot Snapshot { get; }
    public ObjectTemplateCatalog Catalog { get; }
    public SceneObjectResourceOptions Options { get; }
    public SceneObjectResourceRequest(SceneObjectSnapshot snapshot,
        ObjectTemplateCatalog catalog, SceneObjectResourceOptions options);
}
public readonly record struct SceneObjectResourceTicket(
    Guid InitializerId, SceneObjectHandle Handle, ulong Attempt);
public enum SceneObjectResourceCompletionKind { Prepared, Faulted, Canceled, Stale }
public sealed record SceneObjectResourceCompletion(SceneObjectResourceTicket Ticket,
    SceneObjectResourceCompletionKind Kind, SceneObjectResources? Resources, Exception? Error);
public SceneObjectResourceInitializer(SceneObjectRegistry registry,
    Func<SceneObjectResourceRequest, CancellationToken, Task<SceneObjectResources>> loader,
    int maximumOutstandingJobs = 8, int maximumObjects = 64, ulong attemptSeed = 0);
public SceneObjectResourceTicket Start(SceneObjectResourceRequest request, bool restart = false);
public SceneObjectResourceTicket? Find(ulong objectId);
public bool IsCurrent(SceneObjectResourceTicket ticket);
public bool Cancel(SceneObjectResourceTicket ticket);
public IReadOnlyList<SceneObjectResourceCompletion> Poll();
public int OutstandingJobs { get; }
public int TrackedObjects { get; }
public bool IsDisposed { get; }
public Task DrainCompletion { get; }
public void Dispose();
```

Request copies the snapshot/create and reconstructs its explicit sealed creature/static option, including readonly motion routes/bank or static frame/graph limits. Catalog membership and exact recipe/option agreement are mandatory. Unknown CRC or absent options fail without loader invocation. Immutable options are caller-created; the injected loader must separately snapshot any mutable captured configuration and own its resolver/archive lifetime.

## Identity, capacity and transfer

One serialized construction thread owns operations. Reentrant and foreign-thread calls reject; no locks are added to registry operations. Currentness requires exact registry handle, phase/container/arrangement and byte-identical create, including signed-zero bits. Incarnation-only freshness is insufficient for resources. Ticket GUID rejects foreign initializers; checked Attempt increases per actual new start and never rewinds. Explicit attemptSeed supports bounded overflow testing.

Capacity1..4096 counts all started running, completed/unpolled and cancelled-but-unfinished jobs. Tracked current objects are separately1..4096 bounded. World pending work is outside this initializer and bounded by the registry. Completed tasks do not free a job slot until Poll. A duplicate exact-handle Start without restart returns the prior ticket and ignores replacement configuration; explicit restart requires a new slot, allocates/checks next attempt before cancelling prior attempts, then publishes a new ticket. Overflow/capacity failure starts no loader or prefix cancellation.

Poll observes task completion/faults, cancels stale exact handles/attempts, validates current results and transfers an owned readonly completion list once. Stale results contain no resources even after cancellation-ignoring success; stale fault Error remains observed. Explicit cancellation suppresses a current result. Null task/result, synchronous throw, task fault/cancellation or metadata failure becomes Faulted/Canceled completion rather than escaping through Start. Superseded current tickets remain separately distinguishable from foreign tickets. Find/current ticket metadata may remain after transfer until stale polling or disposal; no completed payload history is retained.

Lead integration calls the independently accepted [pure completion validator](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md) directly for each result, using the request's exact raw create, catalog and owned options. No transient initializer/job/ticket is constructed for validation. Its source, geometry/material/clip/frame and explicit option consistency checks run without source I/O, then existing handle/attempt/generation checks govern transfer. Current result rejection still faults the entire world without returning a prepared prefix.

The completion gate accepts the explicitly configured router result, with its documented conservative work limits. Static HDR maps and unrequested creature face/shape/custom-color/other transformed extensions currently reject; their readers and legacy creature delivery are unchanged. Palette-only transformations can be prepared/applied through their separately accepted presentation boundary rather than supplied as an unspecified generic completion.

The generic initializer does not convert static assets into creature placeholders. Loader results must originate from the accepted sealed SceneObjectResources/router surface. This metadata gate is not a hostile-object-graph validator or deep immutability guarantee; read-only owned descriptor/pixel behavior remains the router contract.

## Cancellation and disposal

Cancellation is cooperative. Cancel invokes the token and captures callback errors; it does not release a cancellation-ignoring job's slot. Poll later observes/discards its result. Registry containment/destroy/recreate/reset and later attempts retire exact resource ownership. Dispose cancels all jobs, clears ticket ownership immediately and arranges continuations to observe late faults and dispose token sources. OutstandingJobs remains charged until each continuation finishes; await DrainCompletion before assuming work has drained. It is safe to call Dispose repeatedly on the owner thread. Other operations after disposal throw ObjectDisposedException.

Fresh original human/terminal jobs and deterministic controlled-task tests establish these implementation facts with Confirmed confidence. They do not recover original thread/lifecycle behavior. [Mixed-world contract](SCENE_MIXED_WORLD_CONTRACT.md) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R14_MIXED_WORLD.md) contain reproduction, errors, limits and lead hookup.
