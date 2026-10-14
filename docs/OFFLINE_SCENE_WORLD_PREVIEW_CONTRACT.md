# Offline scene world creature preview

R8-RENDER-01, October 2, 2026. This bounded presentation fixture connects the accepted [world coordinator](SCENE_WORLD_COORDINATOR_CONTRACT.md) to the [owned creature visual](SCENE_CREATURE_VISUAL_CONTRACT.md), using accepted [resource preparation](SCENE_CREATURE_RESOURCE_CONTRACT.md), [initializer](SCENE_CREATURE_RESOURCE_INITIALIZER_CONTRACT.md), and [placement](SCENE_CREATE_PLACEMENT_CONTRACT.md). It is an offline replacement implementation observation, not original runtime or live-world evidence.

## Application flow and distinct states

The independently authored Python peer produces actual application layouts and SOE datagrams. `OfflineWorldPeerDriver` feeds them through existing `SoeClientTransport`: select/ACK, CRC rejection, reordered create/start, duplicate start suppression, baseline, delta, end-baselines, containment, ready/retry/ACK, and server-ready echo. No socket or external system participates. The coordinator owns its private registry, staged bytes, envelope bookkeeping, resource queue, initializer, and generations. The preview creates no second registry and exposes none.

Before the explicit caller minimum-scene barrier, five lifecycle/envelope messages are staged, with no registered object or new resource job. False and stale barriers publish nothing. The satisfied barrier replays owned messages, finalizes the object, and produces one client-ready application; transmission and retry occur before creature preparation. A later poll starts the current finalized revision's load. Prepared resources can publish while the coordinator remains AwaitingServerReady. The server-ready echo advances the coordinator to Ready. UI and exports show **Finalized**, **Prepared**, and **Ready** separately; none implies terrain, full baseline, or gameplay readiness.

The two retained CREO:3 envelopes are deliberately synthetic bookkeeping: application header 23 bytes, payload size 5 (count plus three opaque bytes), total 28 bytes, declared count 99, opaque fields `7F00FF`. They are not complete or valid CREO:3 schemas. No typed height, posture, gameplay, or rendering meaning is assigned to those bytes.

## World-owned visual identity and placement

`SceneCreatureVisual.TryPublishWorld(world, publication, engineTransform, parent, ...)` accepts only an authentic world-owned publication. `MatchesWorld` requires `world.IsCurrent(publication)` and exact encoded equality of resource-create and placement raw-create fields. The coordinator checks owner identity, external generation, active state, pending restart suppression, initializer attempt, registry handle, and finalized phase.

A private identity-check delegate unifies the narrow overload with earlier registry-based APIs. The R6 exact registry/create guard and optional R7 attempt guard retain their behavior. All paths check before allocation, after staged allocation, after attachment while hidden, and on each guarded update. Stale preflight allocates no visual; staged or attached invalidation frees the candidate synchronously. Queued restart invalidates the old publication immediately, even before capacity permits another task. Callers process freshness before rendering; there is no automatic coordinator event subscription.

The preview passes the publication's accepted Core placement through unchanged historical prototype identifier (omitted), applying `S*R*S` and reflected translation once. Explicit zero-quaternion policy is Reject. The synthetic create has quaternion `(-0, .25, -.5, 2)` and wire position X/Z/Y `(123.5, -45.25, binary32(1e-40))`. Its nonunit quaternion is normalized by modern policy; raw bytes remain unchanged. Source Cartesian translation is `(123.5, binary32(1e-40), -45.25)` and engine origin is `(123.5, binary32(1e-40), 45.25)`. The fixture chooses an identity scene parent. Containment remains bookkeeping; no container-relative composition or root-motion rule is inferred.

## Ownership and bounds

Each `Task.Runhistorical prototype identifier (omitted)22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; accepted reader/catalog/profile retain original root, SAT, and clip pins.

Preview bounds: one registry object, two outstanding jobs including cancellation-ignoring workers, eight staged messages / 1 MiB, two retained envelopes / 56 application bytes, and 64 payload bytes per envelope. Source and decoded-image budgets remain accepted reader limits. The initializer owns observation of worker faults and close/drain. The preview tracks only unfinished tasks, pruning completed entries during scheduling, transfer, and polling; it asserts no more than two tracked tasks and zero completed-result tasks after initial transfer. Old results are not accumulated across interactive restarts. One current publication/visual and movement bank remain retained.

Independent peer configuration is bounded to a 64 KiB file, 32 entries, and 4,096 characters per value. Diagnostic export retains at most 64 controls and 16 delivered applications; selection clears its history. These are modern inspection limits, not protocol requirements. Native repeated-ready duplicates verify bounded trace retention and one-shot application delivery.

Controls provide select/stage, explicit open barrier, server-ready echo, restart load, destroy, selection reset, idle/walk/run, pause, and orbit/zoom/reset camera. Restart and selection retire old visuals; destroy/containment revisions cancel stale work through the world. Walk/run are presentation-only clips. Normal window close disables automatic quit and awaits world `DrainCompletion`; fixture teardown does the same. Forced tree exit starts shutdown and observes errors but cannot await a notification. Forced process termination does not guarantee drain.

## Evidence, confidence, and limits

Confirmed replacement execution observations: native/headless/GPU exports agree on all 37 source path/priority/length/hash tuples and exact select, ready, retry, ordered application, and control bytes. An independent effective-overlay audit reopens original archives through the accepted Python index, verifies all 37 priorities, byte counts, and SHA-256 values, and rejects a tampered hash export. Native mutation of borrowed source buffers preserves owned geometry, original mips, and sampled idle/walk/run poses; clearing borrowed peer configuration and source-create buffers preserves owned behavior.

Native production `MatchesWorld` checks reject 15 foreign/stale cases covering restart, selection, destroy, contain, and close. They call managed production adapter math without native prototype host allocations or a GPU claim. The independent packet/cross-product oracle checks all raw 47 create bytes including signed zero/subnormal bits, normalization, source rows, reflected engine columns, and five basis/asymmetric points. Binary32 JSON values are rounded back to binary32; basis tolerance is 2e-7 and point tolerance is 3e-6 times max(1, expected magnitude). Existing independent wire decoders validate actual exports and opaque boundaries.

GPU/headless fixtures retain resource identities, topology, and authored mip bytes through 96 idle and 64 movement updates, visibility, focus and pause. Controlled tasks prove queued-restart suppression, stale no-allocation preflight, preparation/attachment-time cleanup, two-job backpressure with one pending object, generation/destroy/containment cancellation, stale success/fault retirement, and close awaiting cancellation-ignoring success/fault tasks. Those controlled completions reuse an accepted parsed original closure and represent synthetic scheduling policy, separate from the actual original archive worker.

Real first-lookup cancellation resets selection, closes archives, and publishes zero visuals. Seven invalid launcher combinations reject before engine launch. R6 and R7 headless fixtures pass after the private identity-check refactor. The fresh idle45 GPU capture was inspected: complete actor at the derived tilted rotation, ten retained textures, original mip readback. Authored MAIN remains intentionally limited; complete customization, generated face, normal/specular shading, terrain, real container transforms, Vulkan, arbitrary species and live replication remain outside this slice. Known certificate-store startup diagnostics remain in passing logs. No research coverage score, function label, or protocol coverage change follows.

## Reproduction

Prerequisites: accepted .NET 8 / prototype host 4.7.2 .NET toolchain, already-restored default prototype host SDK cache, accepted offline Core packages, original configured corpus, and a sources-cleared private NuGet configuration. From the checkout, using the configured Python executable if `python` is absent:

> Historical prototype-specific command or code example omitted from the research-only documentation.


Build prototype host first. The native console references its actual Debug adapter DLL and managed prototype hostSharp; historical prototype identifier (omitted) can override that managed DLL path. No network restore or download is implied. The prototype host SDK resolver uses its existing default cache, separately from console packages.

Use the launcher (external historical reference) with explicit historical prototype identifier (omitted), `-ArchiveRoot`, `-Configuration`, and `-PeerFixture`. Default/`-Headless` runs a bounded fixture, `-CancelLoad` cancels the real first lookup, `-CapturePath` selects a separate GPU run to a fresh private PNG, and `-Interactive` opens controls. Automated runs are hidden with a 45-second deadline. The oracle (external historical reference) takes peer JSON, native, GPU, headless, and cancellation logs in order. The options verifier (external historical reference) takes the four launcher inputs plus `-ExistingCapture`.

See the [R8 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R8_WORLD_PREVIEW.md). Lead acceptance is next; no Round 9 is authorized.
