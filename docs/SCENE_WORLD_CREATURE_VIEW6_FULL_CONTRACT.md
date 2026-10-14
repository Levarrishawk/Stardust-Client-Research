# Complete CREO6 world publication

Round38 joins the accepted [full kernel](CREATURE_VIEW6_FULL_CONTRACT.md) and
[prepared store](SCENE_CREATURE_VIEW6_FULL_STORE_CONTRACT.md) to the serialized
world. This optional partition owns all18-field models and metadata journals.
It does not resolve gameplay targets, attach wearables, execute original owner
callbacks or establish live interoperability.

## Policy and public boundary

[SceneWorldCoordinator](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldCoordinator.cs)
appends optional `SceneWorldCreatureView6FullPolicy` to both public constructors.
Baseline, full-delta, full-state and full-store limits are explicit copied
policies. Scalar, scalar/HAM and full CREO6 policies are mutually exclusive;
CREO3, TANO3 and TANO6 partitions can coexist. Default opaque and earlier typed
paths retain their contracts.

World snapshots expose optional `CreatureView6Full`; its owner/generation,
incarnation and exact underlying publication reference determine currentness.
`FindCreatureView6Full`, `ApplyCreatureView6FullDelta`,
`DrainCreatureView6FullEvents`, `AcknowledgeCreatureView6FullEvents`,
`IsCurrentCreatureView6FullEvent` and `RetainedCreatureView6FullBytes` operate
on the constructing serialized thread. Stale generation/expected receipts
reject before accessing a plan. A complete baseline is required before creature
end-baselines; deltas require a finalized current recipient.

## Atomic publication, failures and delivery

The world checks recipient, revision and change-journal capacity and allocates
the replacement world journal before store preparation. The genuine seam is
PrepareSeed/PrepareApply, CheckPrepared, assignment-only CommitPrepared, then
ReleasePrepared in finally. No callback, registry mutation or allocation occurs
between successful check and commit. Store/model and world-journal revisions
advance as one serialized publication. A failed candidate preserves earlier
valid publications, issued journals and resource ownership.

Retained full charge is current all-actor model bytes plus48 per pending event.
Frozen HAM/defender/wearable construction reservations remain24/32 inside the
kernel. Logical accounting is independent of CLR heap and other partitions.
Valid skipped/equal/no-op plans still advance the exact publication. Same
incarnation moves retain state; destroy/recreate/reset retire currentness.
Historical journals may acknowledge once while old recipients fail delivery.
World owner/generation and exact issued batch checks augment store identity;
immediate delivery additionally requires a current finalized incarnation.

[Full application router](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldCreatureView6FullApplicationRouter.cs)
owns a bounded ingress copy and borrows world/downstream. It parses exact grammar
before an optional decoder, captures world attempt/journal stamps, and rejects
callback mutation, drain/ack attempts, reentry, closure or generation changes
before preparation. Pure inspection is permitted. Failure closes router-owned
references while preserving borrowed world state. Other application messages
pass through the existing downstream chain. Exact active typed CREO6 failures
preserve prior state; wrong-family/generation-fatal behavior retains the earlier
world failure policy. Malformed legacy handling is not inferred from this modern
atomic policy.

## Original-resource host and verification

[Preview](../../SWG-Godot-Client/poc/Stardust.Godot/ScenePumpedWorldPreview.cs) accepts exclusive
`--creature6-full-peer`; [launcher](../../SWG-Godot-Client/poc/round16-tests/Integration/Launch.ps1)
adds `-CreatureView6FullPeer`. This is a bounded synthetic application peer over
original loaded human/terminal resources. The complete baseline precedes
end-baselines. Five plans cover every field, wrap, sparse growth, shrink,
assignment overshoot, equality and ignored commands. Six complete raw model
states and45 ordered descriptions agree with the independent accepted Python
oracle. Every batch is guarded and acknowledged exactly once; loaded visuals,
transforms, names, customization, motion and terminal clock remain retained.
No update archive reads occur. Headless and GPU builds/runs/readback/resource
cleanup pass; the private GPU capture was inspected.

[Public world scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldCreatureView6FullScenarios.cs)
use the actual separate Core assembly. [Independent world peer](../../SWG-Godot-Client/poc/round38-tests/Integration/world_peer.py)
compares97 full sequences and494 events, models/counters/work/peaks, with40
lifecycle/failure groups, including global other-actor/journal budgets and late
failure. [Host verifier](../../SWG-Godot-Client/poc/round38-tests/Integration/verify_host.py) derives
expectations from authored synthetic inputs, then invokes the accepted TANO6,
clock, names and customization gates. No native output generates expectations.

Observation: deterministic owned code/fixtures, exact oracle/native agreement
and actual replacement-host output. Interpretation: complete bounded full-view
publication and ingress are implemented. Confidence: Confirmed in these tested
domains; original gameplay callbacks and live behavior remain unverified.
The [round checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_FULL_CREATURE_PUBLICATION_AND_TERRAIN_INPUTS.md)
records inputs, corrected attempts, commands and acceptance. Next: bounded
owner-private view4/collection publication and supplied-terrain scene grounding.
