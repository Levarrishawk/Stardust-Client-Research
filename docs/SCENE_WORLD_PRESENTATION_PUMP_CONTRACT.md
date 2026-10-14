# Bounded world presentation pump

R16-R, October 2, 2026. SceneWorldPresentationPump (external historical reference) implements the lead-frozen the separate implementation record over the accepted [mixed world](SCENE_MIXED_WORLD_CONTRACT.md) and [object consumer](SCENE_WORLD_OBJECT_PRESENTATION_CONTRACT.md). It is modern scheduling policy, not recovered original frame timing, readiness or asynchronous scheduling.

## Construction and ownership

```csharp
public SceneWorldPresentationPump(SceneWorldCoordinator world,
    Func<ScenePreparedObjectPublication, SceneWorldObjectSnapshot,
        ISceneWorldObjectPresentation?> factory,
    SceneWorldPresentationPumpPolicy? policy = null,
    Func<SceneObjectResources, long>? pendingResourceWeight = null);
```

The pump constructs and owns its own SceneWorldObjectPresentationConsumer with the policy actor bound; it accepts no external/reused consumer. Consumer is a borrowed inspection surface. The host may use its Find/count, but must not dispose it, compete for its batches/publications, or dispose borrowed adapters. The world is transferred for Close ownership while the host retains borrowed selection/minimum-barrier access. Construct and operate on the world owner thread. All pump operations/properties enforce owner-thread affinity and reject reentrancy. A caught attempt to reenter the pump from a callback is latched and causes the outer Step to fail/close. Foreign-thread calls reject before mutation and do not perform cross-thread cleanup.

Policy is copied. Queue count is 1..65536; queued bytes positive Int32; maximum message bytes 1..queued bytes; messages per step 1..queue count. Pending count/actors are 1..4096; publications per step 1..pending count; total attempts per publication 1..1024; change attempts per step 1..16; pending logical weight 1..Int64.MaxValue. Invalid construction does not acquire/close the world. Defaults are exactly those in the frozen interface.

Default logical weight is Resources.SourceBytes. The injected callback returns a nonnegative logical weight and runs once per current completion before retention; zero is allowed. Negative values, exceptions and checked-sum overflow fail/close. Weight remains charged while a publication is retained, including false acceptance. It does not measure CLR overhead, decoded/GPU allocation or total process memory. This milestone depends only on baseline assets; no unfinished same-round weight estimator is required.

## Queue and Step ordering

QueueApplication owns a byte copy tagged with its supplied generation. Empty/oversized input and queue count/byte budget fail before copy or queue/world mutation; these ordinary admission errors leave existing work open. No parsing/world mutation occurs at admission. Closed admission throws ObjectDisposedException. Dequeued/closed owned bytes are cleared; caller buffers are never modified. FIFO follows accepted enqueue order; future/stale generations are not applied.

Step requires finite nonnegative seconds; invalid time is a pump failure and closes it. Each successful open step performs these operations:

1. Observe external generation changes and retire old consumer roots and stale retained publications. Queued entries retain FIFO order; stale generations are dropped only by the normal bounded dequeue loop.
2. Dequeue at most MaximumMessagesPerStep, applying only the current generation in FIFO order. Normal stale dequeues count toward this budget. Individual message bytes are cleared even if Accept throws. Per-step payload bytes are bounded by the count times MaximumMessageBytes.
3. Poll the existing generic initializer once. Current completions form a local burst. Compute/check the entire burst count/weight against retained work before adding or invoking any adapter factory for that burst. Overflow rejects without a new publication prefix; already existing actors are then retired by failure cleanup. Poll can start existing world jobs; their bounds and cancellation remain coordinator-owned.
4. Attempt at most MaximumPublicationsPerStep retained publications. Each is attempted at most once in this step, even if it returns false. Successful acceptance releases retention; stale attempts retire without charging retries. Current false acceptance increments its total attempt count and rotates to the FIFO tail. The configured maximum includes the final failing attempt, which closes the pump. A new attempt/ticket starts with a new count.
5. Drain/apply/acknowledge at most MaximumChangeAttemptsPerStep batches, using exact consumer receipts. No changes needs no acknowledgment. False/stale application retries within this frame work bound; reaching it preserves pending observations and retries in a later step. External generation reset during a callback returns to host-controlled selection rather than becoming retry exhaustion. Resource acceptance reads latest state, so missing actors can acknowledge earlier batches safely.
6. Tick consumer actors and recheck world/callback health. Pre-ready idle/selecting/initializing Tick false is normal. No implicit minimum barrier or ready message is supplied. The host explicitly calls CompleteMinimumSceneBarrier and owns transmission of any returned ready bytes.

PreparedResources counts current completions admitted into retention in that step; PublishedResources counts successful acceptance calls; RetiredResources includes stale retained/completed resources and retired initializer completions. AcceptedMessages/DroppedMessages count actual processed/retired queue entries; AcceptedMessages plus DroppedMessages never exceeds MaximumMessagesPerStep, including reset frames. Pending counts report final retained state; Active is consumer/world activity, not an all-actors-present or server-ready guarantee. Closed Step returns a zero inactive record.

## Failure, close and limits

World failure/closure, callback errors, latched pump reentry, current resource failure, burst/weight overflow, and publication retry exhaustion close deterministically. Step propagates the triggering exception; if cleanup also throws, it preserves both in AggregateException. An externally failed/closed world observed at Step entry closes and returns inactive. Closure marks the pump closed, clears queued bytes/retained publications/weight callback, then attempts both consumer disposal and world.Close even when one fails. Cleanup errors aggregate. DrainCompletion is captured **after** world close in finally, so callers can await cancellation-ignoring jobs even after cleanup failure. Repeated Close/Dispose is harmless; getters remain usable to inspect zero retention and obtain the drain task. Before close DrainCompletion is Task.CompletedTask; it is a close-drain receipt, not a promise that active jobs have completed.

Callbacks can change world state; freshness and generation are rechecked before further transfer. The underlying consumer retains its documented serialized/retryable application policy, without visual rollback. The pump cannot undo already applied engine side effects, recover a factory's unreturned allocations, or force blocking resolver cancellation. Caller must obey borrowed ownership. Hostile callbacks that violate the borrowed consumer/world API contracts are not isolated. World cleanup/drain inherits the accepted initializer contract; no new cancellation-handler exception guarantee is invented.

Native scenarios (external historical reference) cover copied queue/FIFO/budgets, explicit barriers, burst rejection without factory prefix, exact retry fairness/exhaustion, original sealed human/terminal results, stale/out-of-order/current completions, state/resource independence, callback resets/errors/reentrancy, owner-thread rejection and close/drain including disposal failure. No reflection-forged publication, original executable execution, live contact, new static field semantics or GPU proof. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R16_PRESENTATION_PUMP.md) records reproduction and lead integration.

Lead integration additionally checks coordinator owner affinity before constructor ownership transfer, using the existing guarded snapshot read. A mismatched owner rejects without closing the caller's world.

## Round18 application receiver

Constructor's optional fifth argument is
Func<long,ReadOnlyMemory<byte>,bool> applicationReceiver. Default delegates to
world.Accept(generation,message.Span). Each bounded current queue entry invokes
it synchronously; false counts as dropped, exceptions close/drain, caught pump
reentry is latched. Receiver borrows only this call's owned bytes and must not
retain memory; pump clears dequeued arrays and releases the callback on close.
[Motion router](SCENE_WORLD_MOTION_INGRESS_CONTRACT.md) is one explicit consumer.
Queue/step budgets and default171-check behavior remain unchanged.

## Round20 integration

[Accepted integration evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R20_CUSTOMIZATION_INTEGRATION.md) records owned CIDM/raw packed decoding, supplied current-creature appearance revisions and bounded presentation/world drain. Original-resource, native/headless/GPU, latest/failure/retirement and real pending-close gates pass. This is explicit supplied inspection state; rawfield4 state translation is next. Four Low workers stopped; overnight authorization permits the next round after acceptance. Known certificate/shader initialization diagnostics persist and are counted, while actual upload/readback/pixels pass.

## Round21 integrated boundary

Round21 optional customization router precedes motion/world ingress. Two ordered packets can arrive in one bounded pump step; the source preserves the earlier valid update when the later typed attempt is invalid. Malformed inner/unknown ID stop the host and clear actors/drain archive jobs. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_WIRE_CUSTOMIZATION_INTEGRATION.md).

## Round22 integrated boundary

Optional CustomizationScalars routing accepts ordered4/11..16 before motion/world ingress. Empty customization retains display while scalar Height changes; budgets and shutdown remain bounded. Held composition/body race and real pending-close regressions pass. [Evidence/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_INTEGRATION.md).

## Round23 integrated boundary

The supported CREO:3 union4/5/11–17 now stages raw customization, scalars, components, wounds and pending world state together. Original selected ACST/CIDM context supplies exact ranges and authored defaults through five already owned palettes. Main independent native, headless/GPU, motion and held-container gates pass. Broader fields/views, live mounts/player seeds and graphical attachment/wound behavior remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_FULL_CREATURE_INTEGRATION.md).

## Round24 integrated boundary

Supported CREO3 fixed common fields0/3/6–10 join the existing union. Current common state survives older ingress; visibility uses that overlay while hidden appearance work commits. Actual delta headless/GPU/motion and separate held-baseline visibility proofs pass. Names1/2, TANO, gameplay meanings and live interoperability remain open. STF variant1 exact owned lookup passes two original tables; locale/cache/fallback is deferred. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_CREATURE_INTEGRATION.md).

## Round25 named creature increment

The supported ordered CREO3 union now covers indices0–17, including raw StringId/custom names. Current name overlays and variable charge survive older ingress. Bounded original STF1 resolution drives an optional caption; actual delta headless/GPU and separate held-baseline name proofs pass. Explicit modern name precedence/directory/display policies do not establish original UI/locale behavior. Other families/views and live interoperability remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_NAMED_CREATURE_INTEGRATION.md).

## Round27 typed tangible publication

Optional complete TANO3 world state commits with its journal after private preparation. Static typed token gates actual original terminal caption/raw visibility beside supported CREO ingress; five headless/GPU states, retained resources and teardown pass independent oracles. Default opaque behavior remains. Authored template names and local SWTS selector semantics are separately documented; outer clock and live interoperability remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TANGIBLE_WORLD_INTEGRATION.md).
