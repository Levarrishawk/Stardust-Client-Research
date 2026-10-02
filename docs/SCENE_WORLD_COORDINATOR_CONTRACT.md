# Scene world coordinator

This document describes the legacy creature constructor and its shared lifecycle rules. Round14 adds an explicit [mixed-resource constructor and separate publication API](SCENE_MIXED_WORLD_CONTRACT.md); use that contract for creature/static routing and deferred generic envelope replay.

Round 7, 2026-10-02. This is a modern engine-neutral coordination policy implemented in [SceneWorldCoordinator](../poc/Stardust.Core/Session/SceneWorldCoordinator.cs). It composes the accepted [scene transition](ZONE_SCENE_TRANSITION_CONTRACT.md), [object registry](SCENE_OBJECT_REGISTRY_CONTRACT.md), [resource initializer](SCENE_CREATURE_RESOURCE_INITIALIZER_CONTRACT.md), and [replication envelopes](REPLICATION_ENVELOPE_CODEC_CONTRACT.md). The checkpoint records validation and input identity.

## Boundary and confidence

Confirmed by native tests: the caller supplies a generation and complete APPLICATION bytes, without SOE framing. The coordinator decodes the existing start/create/end-baselines/contain/destroy/ready and baseline/delta codecs. Unknown opcodes fail the current generation; stale generations are ignored before parsing. Static shape, catalog membership, supported recipe, and placement are checked before staging. Registry relationships and envelope budgets are checked during ordered replay. These checks are implementation behavior, not evidence of the original client's dispatcher policy.

BeginSelection returns owned SelectCharacter bytes and advances the external scene generation. Registry reset epochs are separate internal handle identities. Before the explicit caller minimum-scene barrier opens, lifecycle messages are bounded owned staged bytes; there is no registered world or resource load. A false or stale barrier returns no ready message. A true barrier replays every staged message in order and returns the exact CmdSceneReady bytes only after successful replay. Semantic or budget failure clears the entire generation and returns no ready publication. The serialized owner-thread API prevents observation of an intermediate replay world.

The caller decides what minimum scene readiness means. Terrain, original readiness, gameplay activation, GPU preparation, and parent transform composition remain unknown or outside this component. CmdSceneReady does not assert all creature resources are prepared. A later current resource failure clears the world; the caller must recover its transport/scene because already-sent ready bytes cannot be recalled.

## World and resource rules

Create preserves the raw descriptor and uses the accepted placement builder with an explicit zero-quaternion policy. Quaternion normalization is a modern boundary conversion. Containment stores the accepted graph relationship and arrangement bits; it does not reinterpret create placement as a parent-relative pose. Identical duplicate creates are idempotent; conflicting creates fail rather than silently replace objects. End-baselines finalizes the object. Containment revisions and detached children receive new registry handles. Destroy removes placement and envelope history and cancels obsolete loads.

The catalog is caller-supplied configuration. The legacy constructor requires CreatureDetail0 before registry mutation or loading, even if the catalog accepts other recipes. Its mixed-catalog regression puts static first: the supported creature publishes once, then an unsupported static create fails and clears the world. Enum255 remains the invalid catalog fixture. The generic constructor instead requires explicit sealed options for both catalog recipes, as documented separately. Unknown CRCs have no fallback in either path.

Loads begin only after the barrier and only for the latest finalized revision. The pending queue has at most one entry per object, bounded by the registry object budget. New revisions replace the pending entry and move it to the end. Duplicate finalized notifications do not restart an existing current attempt. RestartResource explicitly queues a new attempt and immediately suppresses an older publication, even before capacity permits starting the new attempt.

One persistent initializer survives selection changes and failures. Cancelled workers that ignore cancellation still occupy the job budget until observed completion. Cancellation never frees capacity early. PollResources drains completed work, then starts queued current revisions while capacity permits. A fault or invalid result for any current job fails the entire world; no partial prepared batch escapes. Stale or destroyed jobs retire with observed errors and without resources, preserving the new scene. Close cancels and clears synchronously; DrainCompletion waits for unfinished retired jobs and observes their faults.

Publications contain external generation, initializer ticket (including attempt), placement, and the owned managed resource result. Consumers must call coordinator.IsCurrent immediately before use: it checks coordinator identity, generation, active scene, queued restart suppression, initializer attempt, registry handle, and finalized phase. Snapshot arrays and envelope byte copies are owned. Idle/movement configuration is snapshotted; an injected loader must also snapshot its own captured configuration and must never mutate the registry from worker threads.

## Opaque replication bookkeeping

Baseline/delta APPLICATION header size is 23 bytes, minimum 25 including the declared count. Payload size owns the two count bytes and opaque bytes. The coordinator retains arrival order, object id, raw tag, view, declared count, and bytes without applying inner fields or validating schema counts. Budgets limit payload bytes, total retained envelope count, and total full APPLICATION bytes. Destroy releases an object's retained envelope budget; selection/failure/close clears all history. No baseline/delta schema coverage is added.

## Proof and reproduction

[OfflineSceneWorldScenario](../poc/Stardust.Core/Proof/OfflineSceneWorldScenario.cs) drives actual application messages through existing SoeClientTransport using independently generated offline peer datagrams. It covers reordered create/start delivery, duplicate suppression, corrupt CRC rejection, false/stale barriers, ordered replay, exact ready transmission/retry/ack, one pinned resource publication, server ready, and destroy. Synthetic opaque CREO:3 fields deliberately declare count 99 with bytes 7F00FF; they are not schema-valid gameplay fixtures. No sockets, executable execution, server contact, captures, or GPU work occur.

Run from the repository with Python on PATH and dotnet 8. Use a writable temporary APPDATA. Restore the isolated [Runtime project](../poc/round7-tests/Runtime/Runtime.csproj) with its sources-cleared NuGet.Config, the existing offline package cache, NuGetAudit=false, and UseSharedCompilation=false. Then:

```powershell
python poc/round7-tests/Runtime/Generate-Peer.py local-private/round7/peer.json
dotnet build poc/round7-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round7-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round7/peer.json
dotnet poc/round7-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round7/peer.json C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json local-private/round7/transcript.json
python poc/round7-tests/Runtime/Verify-Transcript.py local-private/round7/peer.json local-private/round7/transcript.json
```

Capture the original proof's stdout to an ignored file and pass it to Verify-Original.py with the root/configuration arguments to independently audit the effective overlay source identities. Missing original inputs prevent that optional proof, not the deterministic synthetic scenarios. Native fixtures use task completion barriers without sleeps. They cover cancellation across generations, budgets, malformed/unknown messages, missing parents, conflicting creates, explicit restart attempts, destroyed late faults, whole-world mixed completion failure, close/drain, and owner-thread enforcement.

The original proof loads one pinned human creature closure, 37 distinct reads and 4,683,848 source bytes, including material, geometry, skeleton, idle/walk/run clips. Configuration SHA-256: 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Original inputs stay private; the proof creates no curated proprietary corpus. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R7_SCENE_WORLD_COORDINATOR.md) for exact validation and handoff.

## Current optional typed mode —Round10

The default opaque contract above remains. An explicit [typed CREO3 baseline/scalar policy](SCENE_WORLD_REPLICATION_CONTRACT.md) adds bounded current state and separate world-owned freshness, replacing supported history with immutable snapshots. Resource identity/barrier semantics are unchanged; no terrain/other-view readiness claim.
