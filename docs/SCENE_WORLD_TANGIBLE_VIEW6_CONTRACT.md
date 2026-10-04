# Optional TANO6 world publication

Round32, 2026-10-03. This modern serialized adapter integrates the accepted [TANO6 store](SCENE_TANGIBLE_VIEW6_STORE_CONTRACT.md), [state contract](TANGIBLE_VIEW6_STATE_CONTRACT.md) and [codec](TANGIBLE_VIEW6_CODEC_CONTRACT.md). No legacy control-flow or live interoperability claim is added.

Implementation: [coordinator](../poc/Stardust.Core/Session/SceneWorldCoordinator.cs), [policy, receipts and router](../poc/Stardust.Core/Session/SceneWorldTangibleView6ApplicationRouter.cs). Verification: [native scenarios](../poc/Stardust.Core.SmokeTests/SceneWorldTangibleView6Scenarios.cs), [isolated runner](../poc/round32-tests/Runtime/Program.cs), [synthetic fixtures](../poc/round32-tests/Runtime/ordered-fixtures.json) and [independent oracle](../poc/round32-tests/Runtime/verify.py).

## Frozen public boundary

Both existing coordinator constructors append `SceneWorldTangibleView6Policy? tangibleView6Policy = null`. The generic object constructor accepts the optional policy; the creature-only constructor rejects it. Existing default modes and TANO3 policy remain unchanged. The immutable policy contains `TangibleView6Limits CodecLimits`, `TangibleView6StateLimits StateLimits`, `SceneTangibleView6StoreLimits StoreLimits`, and optional `ulong RevisionSeed = 0`. Limits are validated and copied. There are no world-policy decoder callbacks for TANO6.

`TangibleView6Enabled`, `RetainedTangibleView6Bytes`, `FindTangibleView6(generation, id)` and `IsCurrent(SceneWorldTangibleView6Snapshot)` expose state. `SceneWorldObjectSnapshot.TangibleView6` propagates into ordinary snapshots, prepared existing customization views and drained world change batches. A world token exposes Generation, Incarnation, global store Revision and immutable accepted `TangibleView6State`.

`ApplyTangibleView6Delta(long generation, SceneWorldTangibleView6Snapshot expected, TangibleView6Delta plan)` requires this world's exact underlying current stored state receipt, current generation and incarnation. Stale generation/receipt returns false before plan access. Current receipt and wrong payload ID throw. Manually supplied plans are already-decoded owned codec models, not wire bytes; codec limits apply at wire ingress and state limits independently apply to every plan.

`DrainTangibleView6Events(generation)` returns an owned world wrapper around the exact issued store batch. `AcknowledgeTangibleView6Events(batch)` delegates exact underlying-batch ownership acknowledgment after world owner/generation validation. Repeated drains may return distinct immutable wrappers for the same issued underlying batch; successful acknowledgment through one invalidates them all. Stale/foreign/repeated/closed acknowledgment fails.

`IsCurrentTangibleView6Event(batch, index)` validates world owner/generation, current underlying batch, index, registry incarnation and finalized phase. The caller must use this guard before eventual delivery and remain serialized through delivery. This API performs no callback or recipient lookup outside the registry. Acknowledgment only retires journal ownership.

## Recipient and lifecycle policy

Only configured static-appearance objects accept typed TANO6. Exact TANO6 baselines seed pending or finalized matching-family recipients; finalized deltas require an existing complete seed. Enabling the policy requires a complete TANO6 seed before the static end-baselines barrier. When optional TANO3 is also enabled, both policies' explicit barriers apply. Typed packets do not add opaque history. Other views retain the preceding bounded opaque policy.

World currentness follows exact stored state identity and current incarnation, rather than the historical store handle revision. End-baselines or containment can therefore leave a world token current while its store publication retains the former exact handle. A later update uses the newest registry handle and the exact stored snapshot to refresh publication. Destruction/recreation, generation replacement and close invalidate former world tokens. Recreated baseline state starts revision one; the store publication revision continues.

Historical journal records survive object removal/recreation and contain no historical state graphs. The delivery guard rejects former incarnations even when a newly drained historical batch can legitimately be acknowledged. A generation replacement clears retained state and acknowledges/discards that world's historical journal. Close clears owned store/registry state and existing resource work; router disposal never closes its borrowed world or downstream.

## Preparation, failure and budgets

The store's new internal prepare/check/commit/release seam retains its owner gate through coordinator preparation. A candidate owns a prebuilt map, queue, snapshot, checked total state charge and checked revision/epoch. The exact active preparation identity, store owner, current registry handle/incarnation and unconsumed status are checked before commit. No prepare operation publishes state.

The coordinator checks finalized family, exact receipt, checked world mutation revision and world journal capacity, and allocates the complete replacement journal before preparing the store. Store preparation checks global mapped state, existing events, intermediate kernel output/work and newly generated event capacities. After all preparation and recipient checks succeed, commit assigns store fields and then the prebuilt world journal/revision. No parsing, decoder, callback, allocation or checked arithmetic occurs after the first store assignment. Finally releases the owner gate. Failure never consumes a store publication and then claims rollback.

`RetainedTangibleView6Bytes` is the sum of this store's mapped state `DecodedBytes` plus 24 logical bytes per pending event. Store policy limits bound this TANO6 partition globally across actors. Existing CREO, TANO3, opaque envelope and resource-job budgets remain separate; this is not a combined total-world CLR heap cap. `MaximumWork` limits accepted state-kernel work; object-map/event copying has separate bounded collection overhead. Historical caller snapshots and drained wrappers are caller-retained storage.

Rejected direct plans and active exact TANO6 `Accept` preserve current state, logical charge, world phase and valid earlier journal work. Missing seeds during staged end-barrier replay and wrong-family/noncandidate envelopes retain the existing generation-fatal failure/drain policy. There is no rollback of external callback effects. Revision/epoch overflow rejects before publication. Current registry is private to the serialized coordinator; no concurrent mutation guarantee is implied.

## Ingress callback ownership

`SceneWorldTangibleView6ApplicationRouter(world, decoder = null, downstream = null, maximumPayloadBytes = 65536)` borrows the world/downstream, owns callback references and a message copy, and routes exact TANO6 delta envelopes only. Default downstream calls world.Accept. Baselines/lifecycle/control pass downstream. Disposal or failed ingress drops owned callback references without closing borrowers.

Exact codec grammar is validated before an optional custom delta decoder. It runs before store preparation, against the existing whole-world stamp extended with TANO6 state revisions. The stamp includes generation/phase, mutation revision, callback-attempt epoch, registry identities, existing typed state revisions and world journal membership. Event drain/ack attempts advance the callback-attempt epoch. Mutations, journal acknowledgment, generation replacement, caught router reentry or caught disposal poison the returned plan. Matching object ID/declared count is checked. Read-only custom decoder inspection is allowed. External captured state and downstream effects remain caller-owned.

## Verified evidence and next probe

Confirmed modern implementation evidence: 496 native checks, 21 accepted ordered sequences and 27 world case groups. Independent accepted30 Python wire/state replay matches 23 transitions and 38 ordered events including counter/skip/wrap cases. Accepted31 store suite remains 143 checks, nine groups, with the same independent transition/event oracle. New cases cover kernel/state/output/event/queue caps, global second actor charge, world journal/revision rejection, history and delivery guards, handle movement, recreation/generation, foreign ownership, owner/closed access, callback mutation/ack/reentry/disposal, malformed/budget ingress, pending recipients, default opaque behavior, missing seeds and held cancellation/drain.

No original executable/assets, live server, packet capture, downloads or dynamic legacy execution were used. Synthetic catalog paths/pins and late resource faults are clearly modern test inputs. There is no rendering or automatic event delivery integration. Next: register this router above the current TANO3/CREO chain in the lead-owned scene host, explicitly choose event delivery/ack policy, and validate complete TANO6 baseline availability in that host's static end barrier.
