# Explicit mixed creature/static world

R14-R,2026-10-02. SceneWorldCoordinator (external historical reference) now has an explicit generic resource constructor and separate publication API, using the accepted [router/options](SCENE_OBJECT_RESOURCE_ROUTER_CONTRACT.md) and new [initializer](SCENE_OBJECT_RESOURCE_INITIALIZER_CONTRACT.md). One coordinator owns registry, ready barrier, replication, lifecycle, resource queues and acknowledged changes for both recipes. No duplicate world/state implementation, parent transform graph, engine consumer or original readiness policy is added.

## Old and generic entry points

The original `(catalog,idle,movement,creatureLoader,zeroPolicy,...replicationPolicy,changePolicy)` constructor, PollResources(), SceneWorldResourcePoll and ScenePreparedCreaturePublication remain behavior-compatible and creature-only. Their early typed decoding and existing strict creature validator remain intact. Unknown/static create recipes still reject through that entry point. The generic constructor requires caller-owned options for every recipe present in its explicit catalog:

```csharp
public SceneWorldCoordinator(ObjectTemplateCatalog catalog,
    IReadOnlyDictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> options,
    Func<SceneObjectResourceRequest, CancellationToken, Task<SceneObjectResources>> loader,
    SceneCreateZeroQuaternionPolicy zeroPolicy, int maximumObjects = 64,
    int maximumOutstandingJobs = 8, int maximumStagedMessages = 256,
    int maximumStagedBytes = 1048576, int maximumEnvelopes = 256,
    int maximumEnvelopeBytes = 1048576, int maximumPayloadBytes = 65536,
    SceneWorldReplicationPolicy? replicationPolicy = null,
    SceneWorldChangePolicy? changePolicy = null);
public SceneWorldObjectResourcePoll PollObjectResources();
public bool IsCurrent(ScenePreparedObjectPublication publication);
public sealed record SceneWorldObjectResourcePoll(
    IReadOnlyList<ScenePreparedObjectPublication> Prepared,
    IReadOnlyList<SceneObjectResourceCompletion> Retired);
```

ScenePreparedObjectPublication is sealed with private ownership construction and getter-only Generation, Ticket, Placement and Resources. Ticket is the new exact handle/attempt ticket; Resources is the baseline sealed discriminator carrying exactly one Creature or Static payload. Placement preserves the exact create and existing explicitly chosen quaternion policy. Each options key must match its sealed option's recipe; the map and all option values are reconstructed/copy-owned. Missing options, unknown CRC or wrong recipe has no fallback or implicit default. Supplying extra unused supported options does not expand catalog membership.

Call PollResources only for a legacy constructor and PollObjectResources only for a generic constructor; the wrong poll surface throws InvalidOperationException without failing/resetting the scene. Existing world properties, RestartResource, lifecycle, replication/change queries and Close/DrainCompletion work through the selected initializer. IsCurrent on the other publication type returns false. Generic resource freshness requires world owner/generation, active state, exact handle/attempt, finalized object and no queued restart. State currentness remains incarnation/local revision based and independent.

## Barrier and replication by recipe

Before the ready barrier, creates may exist only as staged bytes. Generic Decode therefore validates/copy-owns the full outer replication envelope and exact declared byte size, then stages the original bytes. During ordered replay (or active Apply), it resolves the current registry recipe **before** payload/state publication. This intentionally differs from legacy early typed decoding, which is preserved. Typed payload failure after earlier replay creates still clears the whole generation and returns no ready result/drainable prefix.

The generic entry currently accepts CREO family for CreatureDetail0 and TANO family for StaticAppearance as an explicit bounded compatibility policy. Creature typed modes use the exact accepted [R13 atomic codecs/store](SCENE_WORLD_REPLICATION_CONTRACT.md), including composite5+11..17, existing per-group/total limits, checked revision and logical accounting. Generic opaque mode keeps creature CREO opaque; static TANO remains bounded opaque in every mode, with existing append-only envelope count/byte budgets. Static IDs have no default typed replication state. Other family/recipe combinations, including static CREO and creature TANO, reject before Seed/Apply/history publication; unknown CRC fails before registry/resource work. This two-family recipe policy is not a claim about all original static/client families.

Accepted replay emits one acknowledged latest snapshot per affected ID with existing coalescing/order/capacity rules. Typed creature state-only updates and opaque static messages do not reload resources. End-baselines queues finalized loads; caller ready barrier remains explicit and loads begin on later polling. The shared queue checks exact handles, caps pending objects via the bounded registry and honors running/completed-unpolled/cancelled job backpressure. No resource load is silently chosen from replication contents.

## Failure and lifecycle

Both poll paths collect candidates locally. A single current job fault/invalid result fails the entire world: no partial Prepared list is returned, registry/typed/opaque/placement/pending/change state clears and current jobs cancel. Superseded/stale/prior-generation faults remain observed Retired completions and do not fail a later scene. Cancelled-but-unfinished tasks stay charged until polled or closed/drained. DrainCompletion retains its existing close-drain meaning.

Containment/end-baselines revisions retain creature state incarnation but invalidate exact resources when the registry handle changes; static resources follow the same exact rule. Destroy/recreate/reset/restart invalidate current recipe publications and cannot publish a prior success into a replacement. Parent detachment remains existing registry bookkeeping only; no child world transform or render hierarchy is implied. Close cancels/disposes the selected initializer and awaits its late-fault observation via DrainCompletion. Owner-thread/reentrancy guards cover new poll/currentness methods and actual loader invocation.

## Confirmed tests and limits

Native deterministic tasks cover two recipes, completed/unpolled capacity, idempotent start/restart, exact raw signed-zero identity, stale containment/recreation/reset, foreign tickets/worlds/threads, actual loader reentrancy, metadata failures, attempt overflow, cancellation-ignoring success/fault and dispose/close drain. Prebarrier create+CREO composite+TANO envelopes replay successfully; static CREO, creature TANO, unknown CRC, outer truncation and an intact-envelope nonfinite typed tail clear globally with no resources/batch prefix. One current job fault discards an otherwise valid simultaneous completion. Static opaque history exhaustion remains bounded. Legacy scalar/mixed/composite/observer/opaque regressions pass.

Fresh dedicated original jobs load human39sources/4,686,371bytes and terminal27sources/1,401,794bytes with explicit albedo and strict frame0. All archives close before transfer;66 borrowed raw buffers are cleared while owned payload fingerprints remain stable. An independent496-byte peer/model oracle verifies exact handshake/reliable order/select/ready, state and mutation outputs, all66 effective original source priority/size/SHA identities and142 independently decoded mip hashes. No original executable execution, live connection, asset publication, GPU or complete-client claim.

Lead registration: parse peer dictionary and call `SceneMixedWorldScenarios.Run(new[]{root,configuration,catalog,bank},peer,exportPath)`. Subsequent mixed rendering must choose this generic constructor and PollObjectResources, branch on Resources.Kind, preserve separate state/resource currentness, and acknowledge only a current world batch. Existing creature previews keep their old entry point. No unfinished worker API is required. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R14_MIXED_WORLD.md) records exact commands and scope.

## Lead acceptance and joined completion gate

Lead replaced the temporary generic validation wrapper with the independently accepted [pure gate](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md). Fresh deterministic tasks, original66-source/142-mip oracle, shared native suite and a separately handpacked original human/terminal integration still pass. Conservative gate limits, static HDR exclusion and unrequested transformed-result exclusions are explicit; legacy delivery/readers are unchanged. [Mixed prototype host proof](MIXED_WORLD_PREVIEW_CONTRACT.md) checks exact publications, stale acknowledgment, static-only recreation, unaffected human state/GPU resources, recipe-family failure and drain. It applies two actors explicitly; reusable heterogeneous presentation is next.

## Round15 presentation consumer join

Generic publications/current snapshots now feed a reusable
[engine-neutral consumer](SCENE_WORLD_OBJECT_PRESENTATION_CONTRACT.md), with
[recipe adapters](SCENE_WORLD_OBJECT_VISUAL_CONTRACT.md) owning prototype host roots.
Resource freshness, creation incarnation, typed state and opaque history identity
are checked independently. Acknowledgment requires the exact current successfully
applied batch. Stale attempts cannot replace current actors; missing actors may
attach using latest state after earlier batch acknowledgment. World policies and
legacy APIs are unchanged. Combined original/native/GPU/headless/cancel acceptance
is recorded in [Round15](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_15_2026-10-02.md).

## Continuous scene host accepted - October2,2026

[Round16](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_16_2026-10-02.md)
joins the bounded [pump](SCENE_WORLD_PRESENTATION_PUMP_CONTRACT.md),
[prototype host host](SCENE_WORLD_PRESENTATION_HOST_CONTRACT.md) and logical
[resource accounting](SCENE_OBJECT_RESOURCE_ACCOUNTING_CONTRACT.md).
Independent peer and original headless/GPU/cancellation checks pass; the human
retains its resource identity during state updates and terminal-only recreation.
These are synthetic offline messages with explicit original asset recipes and
modern queue/timing policy. Movement/interior integration and live contact remain
separate. Research scores and protocol/name denominators are unchanged.

## Native motion join - October2,2026

[Round17](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_17_2026-10-02.md)
joins [compact codecs](COMPACT_SCENE_TRANSFORM_CONTRACT.md),
[native poses](SCENE_WORLD_OBJECT_MOTION_CONTRACT.md),
[body envelopes](SCENE_CREATURE_BODY_ENVELOPE_CONTRACT.md) and
[prototype host motion](SCENE_WORLD_OBJECT_MOTION_VISUAL_CONTRACT.md).
Continuous original headless/GPU/cancel and independent/native/shared gates pass.
Height applies once; immutable create/resource identity survives wrap/stale and
clear/local resume. Diagnostic zero yaw/immediate pose is modern policy, not
recovered interpolation. Parent resolution, physical speed interpretation, unified
world ingress, locomotion and interior traversal remain open.

## Round18 local input and offline motion ingress

[Round18](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_18_2026-10-02.md)
joins [local driver](SCENE_WORLD_LOCAL_MOTION_CONTRACT.md),
[input and locomotion](SCENE_WORLD_LOCAL_INPUT_CONTRACT.md) and
[motion ingress](SCENE_WORLD_MOTION_INGRESS_CONTRACT.md) through the continuous
bounded pump. Original headless/GPU/cancel and independent nine-stage time/pose/
clip/resource proofs pass. Modern correction/pause/free planar movement is
explicit; original yaw, interpolation, prediction and parent-world conversion
remain unclaimed. [Head-material diagnosis](SCENE_HUMAN_HEAD_MATERIAL_GAP.md)
requires an explicit generated-face route next. Research/function/protocol
denominators are unchanged. Workers remain Low; successive rounds are authorized
under the overnight instruction.

## Round19 explicit generated face route

[Round19](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_19_2026-10-02.md)
joins [owned face resources](SCENE_CREATURE_FACE_RESOURCE_CONTRACT.md),
[completion/accounting](SCENE_CREATURE_FACE_COMPLETION_CONTRACT.md) and
[actual GPU MAIN inspection](SCENE_CREATURE_FACE_INSPECTION_CONTRACT.md).
Opt-in supplied values survive async option copying;76-source original closure,
five-layer output, scoped face pixels, movement retention and malformed/canceled/
lost-incarnation cleanup pass. Default route and metrics retain their meanings.
[Wire/packed ingress](CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) identifies the
next typed setter/field4 revision boundary; actual player values remain unknown.

## Round21 integrated boundary

Round21 optional wire customization uses one raw journal consumer over the borrowed world. Exact-current batch acknowledgment precedes source cache commit; source failures retain prior values and the joined host fails closed. Original resources and world pose/Height are retained. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_WIRE_CUSTOMIZATION_INTEGRATION.md).

## Round22 integrated boundary

Round22 raw/scalar/world-journal state changes commit from prepared candidates. Raw-only leaves replication unchanged and scalar-only leaves raw issued receipts unchanged. Original appearance resources survive mixed Height updates. [Evidence/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_INTEGRATION.md).

## Round23 integrated boundary

The supported CREO:3 union4/5/11–17 now stages raw customization, scalars, components, wounds and pending world state together. Original selected ACST/CIDM context supplies exact ranges and authored defaults through five already owned palettes. Main independent native, headless/GPU, motion and held-container gates pass. Broader fields/views, live mounts/player seeds and graphical attachment/wound behavior remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_FULL_CREATURE_INTEGRATION.md).

## Round24 integrated boundary

Supported CREO3 fixed common fields0/3/6–10 join the existing union. Current common state survives older ingress; visibility uses that overlay while hidden appearance work commits. Actual delta headless/GPU/motion and separate held-baseline visibility proofs pass. Names1/2, TANO, gameplay meanings and live interoperability remain open. STF variant1 exact owned lookup passes two original tables; locale/cache/fallback is deferred. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_CREATURE_INTEGRATION.md).

## Round27 typed tangible publication

Optional complete TANO3 world state commits with its journal after private preparation. Static typed token gates actual original terminal caption/raw visibility beside supported CREO ingress; five headless/GPU states, retained resources and teardown pass independent oracles. Default opaque behavior remains. Authored template names and local SWTS selector semantics are separately documented; outer clock and live interoperability remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TANGIBLE_WORLD_INTEGRATION.md).
