# 2026-09-30 owned background scene load

## Question and inputs

Can CPU asset loading leave the main thread while maintaining one result transfer, cancellation cleanup and no stale scene publication? Stop at controlled worker/lifetime/fault tests and original success/cancelled host paths. Reuse [current scene pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_SCENE_VISUALS.md). No original executable or server contact.

## Confirmed observations

[Owned worker contract](../../docs/BACKGROUND_ASSET_LOAD_CONTRACT.md) passes native different-thread dispatch, one result request, cancellation/finally cleanup, ignored-token stale completion after owner disposal, repeated disposal/cancel, null rejection and active-owner InvalidDataException propagation. The affinity test uses a controlled start event so thread-pool continuation reuse cannot produce a false failure.

The prototype host host opens and closes the configured overlay within the worker, checks cancellation at source boundaries and transfers decoded content once. Original headless/GPU runs verify a different worker thread, main-thread delivery and completed archive scope before scene/GPU creation. Loading remains responsive: inspected headless run observed197 loading frames and GPU run223; these are observations, not performance promises. Existing shared visuals, owner poses, mouse/input/visibility, scheduling and guarded-close gates pass. Source union remains65 assets / 9,614,085 bytes.

Headless `-CancelSceneLoad` dispatches the normal pinned request, waits at its first asset lookup, invokes the loading button's cancellation path and confirms worker scope cleanup, no actor scene and no GPU visual owner. It exits normally. Successful GPU capture inspected. Native smoke/build and bounded success/cancellation runs pass.

Confidence: Confirmed cooperative replacement worker and main-thread publication boundary. Cancellation latency inside an individual decoder remains bounded by that decoder rather than immediately interruptible. Original asynchronous streamer fidelity, physical OS mouse grabbing and full obstacle collision remain separate. Metrics unchanged.

## Next action

Build a bounded engine-neutral continuous actor-box/triangle contact query with independently authored synthetic geometry and a separate oracle. Keep collision-source selection and scene integration unclaimed until authoritative geometry/policy is established.
