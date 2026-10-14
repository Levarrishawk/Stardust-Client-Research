# World object presentation consumer

R15-R, October 2, 2026. Consumer (external historical reference) owns bounded engine-neutral adapter bookkeeping over the accepted [mixed coordinator](SCENE_MIXED_WORLD_CONTRACT.md). The frozen interface (external historical reference), coordinator and old creature consumer are unchanged. This is modern replacement policy, not recovered original scheduling or rendering behavior.

## Exact API

```csharp
public SceneWorldObjectPresentationConsumer(SceneWorldCoordinator world,
    Func<ScenePreparedObjectPublication, SceneWorldObjectSnapshot,
        ISceneWorldObjectPresentation?> factory, int maximumActors = 64);
public int Count { get; }
public ISceneWorldObjectPresentation? Find(ulong objectId);
public bool AcceptResource(ScenePreparedObjectPublication publication);
public bool ApplyBatch(SceneWorldChangeBatch batch);
public bool AcknowledgeAppliedBatch(SceneWorldChangeBatch batch);
public bool ConsumePending(int maximumAttempts = 4);
public bool Tick(double seconds);
public void RetireAll();
public void Dispose();
```

Actor capacity is 1..4096; retry attempts are 1..16; Tick accepts finite nonnegative seconds. All public operations including getters enforce construction-thread affinity and reject callback reentrancy. One dictionary owns adapters and their exact resource publication/creation incarnation. There is no extra registry, state store, scheduler or resource loader. Find returns a borrowed adapter; callers must not dispose consumer-owned adapters or retain them as substitute ownership.

## Acceptance and resource lifetime

AcceptResource validates world.IsCurrent(publication), obtains the latest world snapshot with the exact resource handle and finalized phase, and validates typed replication independently with world.IsCurrent(snapshot.Replication). The creation incarnation is bound at acceptance; state revisions may advance while the resource attempt remains valid. Opaque envelope history identity is checked against the latest snapshot before/after callback, without decoding its semantics. Static opaque snapshots carry Replication null and can be passed to Apply without invented field semantics. Recipe adapters choose their own rendering policy.

Exact-publication acceptance is idempotent and returns true without calling the factory or reapplying state. Subsequent state advancement goes through ApplyBatch/ConsumePending. Missing actors can be acknowledged: later resource acceptance reads latest world state even when the earlier observer batch has been acknowledged.

Capacity exhaustion throws InvalidDataException before factory invocation. Replacement defers retirement of the same object's previous owned adapter while constructing the candidate. The factory must return a fresh adapter exposing the exact publication reference. Candidate Apply receives the acceptance snapshot, and resource/state checks run before and after it. Only then is the old adapter retired and the candidate installed, with another freshness check after disposal. Candidate null/false/staleness/errors roll back the candidate; stale old roots are subsequently retired. A factory returning an already owned adapter is rejected without disposing it as a new candidate. The factory is responsible for any allocation it does not return, including roots created before it throws.

Resource restart, containment/recreation, generation changes, world failure and Close invalidate ownership through the coordinator. Normal operations prune stale adapters; replacement acceptance temporarily defers only its target's retirement. Tick checks lifetime before and after each adapter callback; false retires that adapter. Tick returns active-world status, not a promise that every adapter remains present. Count/Find also synchronize world boundaries. Consumers retain no old snapshot after Apply; adapters control their own snapshot retention.

## Batches and acknowledgment

ApplyBatch accepts only a current world-owned batch and validates each typed snapshot separately. Missing actors do not block acknowledgment. Destroyed or resource-stale actors retire; current static adapters receive the opaque snapshot. Callback false rejects application; arbitrary adapter exceptions propagate and the faulting installed adapter retires. No special Height exception is swallowed and no RejectedHeightBits property is supplied: the frozen heterogeneous interface has no typed diagnostic failure contract.

Application is serialized and retryable, **not a visual rollback transaction**. An earlier adapter may already have applied when a later adapter fails or changes the world. In that case no acknowledgment is permitted; pending observations remain for retry. Successful application records the exact batch object reference, including its world owner identity through IsCurrent. A different drain with equal generation/revision is not the applied batch. AcknowledgeAppliedBatch checks this reference and currentness again before calling world.AcknowledgeChanges. Stale acknowledgment cannot discard newer changes. Any later ApplyBatch replaces the previous application receipt.

ConsumePending drains/reapplies/acknowledges within the explicit attempt bound; no changes or exhausted attempts returns false. Adapter false can therefore be retried; callers with expensive failure paths should choose one attempt. Callbacks may update the world, but may not reenter the consumer. Resource and state freshness remain separate throughout.

## Cleanup and evidence

RetireAll detaches the entire dictionary/application receipt before disposing every adapter. Disposal failures are collected into AggregateException after all roots have been attempted. Dispose marks the consumer disposed and releases world/factory references even when retirement fails; repeated Dispose is harmless. Individual adapter errors are not converted into successful consumption. A secondary disposal error can replace an earlier callback error through normal exception unwinding. After disposal Count is zero, Find null and active operations return false (argument validation still applies). Consumer disposal does not close the externally owned world: caller closes it and awaits DrainCompletion separately.

Native scenarios (external historical reference) use genuine coordinator-issued publications, accepted synthetic wire layouts and original sealed router completions. They verify both recipes, delayed state, exact/foreign/stale publication/batch identity, restart/recreation, candidate rollback, capacity-before-factory, thread/reentrancy, later-adapter failure/stale retry, arbitrary errors, cleanup aggregation, global failure and late close drain. Original assets stay read-only; no publication forging/reflection, sockets, GPU or original executable execution. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R15_OBJECT_PRESENTATION.md) gives input identity, reproduction and remaining consumer integration.
