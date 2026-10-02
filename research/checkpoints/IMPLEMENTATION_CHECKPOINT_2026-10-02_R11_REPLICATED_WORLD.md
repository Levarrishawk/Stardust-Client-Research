# R11-RENDER-01: world-owned state and original albedo creature

Date: 2026-10-02. Bounded question: can accepted typed world state drive an original
albedo creature through guarded presentation without duplicating ownership or
coupling state revision to resource lifetime? Stop condition: independent wire,
state/placement, source/factor/mip/sample and actor pixel/extent proofs, fresh
GPU/headless/cancel/negative fixtures, old-preview regression and scope validation.
Canonical implementation policy:
[offline replicated world contract](../../docs/OFFLINE_REPLICATED_WORLD_PREVIEW_CONTRACT.md).

## Input identity and tools

Isolated ed09 checkout; HEAD `65de8c88141071744dc75594be126b3bab3696a5`.
Accepted Round 11 baseline: 2,039 files, ZIP SHA-256
`287f1bdef5bcb0972ab09517c22b3c4bae1397bcbbd6dde9227ad93cde5d4da7`,
manifest SHA-256 `48fb34d65ed9161e8472bc4d5adc9675f5997747a7bb4202bd00d62b49114468`.
Safe member paths, exact membership and every input/output hash were checked;
ignored/unlisted material was preserved. Working-tree scope is measured against
this accepted manifest rather than HEAD's earlier research tree.

Original archive root `C:/Stardust-DEV`, pinned `swgemu_live.cfg`; portable catalog
`poc/preview-profiles/scene-object-catalog.json`, human locomotion profile and pinned
idle selection. The optional reader retains 39 sources / 4,686,371 bytes.
Configuration and object/appearance identity pins are in the canonical contract
and accepted profiles. No executable VA claim or original-client execution.

.NET SDK 8.0.300, existing offline NuGet cache, Godot 4.7.2 .NET x64/OpenGL on RTX
4070 Super, bundled Python/Pillow and accepted independently authored archive,
IFF/albedo/DDS audit tools. Shared Core, profiles, prior previews, registries,
status/ledgers and runners are untouched. Only the optional world guard in
SceneCreatureVisual and one Main dispatch branch change existing baseline files.

## Evidence, confidence and consequence

Observation: current resource publication and current world state pass separate
guards; old state fails after a scalar revision while the same actor/resource IDs
remain valid. Restart attempts retire old visuals; same-ID recreation rejects old
incarnations. Foreign world and stale generation fail. Staged and attached roots
stay hidden and are freed after state changes. The staged snapshot reference is
cleared on success. Confidence: **Confirmed**. Consequence: state changes can reuse
resources without retaining an obsolete baseline or exposing a duplicate store.

Observation: failure advances the registry epoch separately; reselection creates
an unequal world-generation/registry-epoch pair that still matches and publishes.
Confidence: **Confirmed**. Consequence: these counters must be compared only within
their respective identity domains.

Observation: independently framed 496-byte peer messages preserve select/ACK,
CRC rejection, reliable reorder/duplicates, complete baseline/scalar staging,
explicit barrier, ready/retry and application order. Finalized and Prepared precede
the distinct server Ready observation. Current Height/visibility changes enter
through world.Accept. Confidence: **Confirmed modern offline orchestration**;
original arrival/readiness behavior remains **Unknown**.

Observation: native and GPU/headless logs match an independent 47-byte asymmetric
create including negative-zero quaternion Y, full baseline bytes, Height 1.5
delta and unsupported raw scalar bits. The quaternion cross-product oracle confirms
the SRS reflection and engine origin `[-2,4,-7]`; Height multiplies basis only.
Five finite unsupported heights remain in current world state and reject before
visual mutation; inclusive bounds pass; native nonfinite probes reject.
Confidence: **Confirmed modern presentation policy**. Original Height units and
posture mapping remain **Unknown**; codecs are unchanged.

Observation: independent original archive reading verifies all 39 priorities,
lengths and hashes, five inherited templates, selected geometry, two programs,
four factors, ten textures/80 mip hashes and four sampled pixels. Three sampled
factor colors differ from MAIN-only. GPU parameters on every surface agree with
those factor records. Caller-buffer clearing leaves owned pixels/programs/factors
unchanged. Confidence: **Confirmed**. Consequence: accepted authored albedo can be
used by this world-owned actor without depending on unfinished palette APIs.

Observation: visually inspected fixed-camera captures show the whole actor in
three-quarter view with authored body color. Below UI row 180, 28,113 pixels change,
width 132→204 and height 237→367; warm pixels 10,569/25,209. Animation frame and
camera/resource IDs stay fixed. Confidence: **Confirmed for the fixture**, not
full original lighting, face composition, material customization or visual fidelity.

Observation: GPU/headless probes cover old snapshots, foreign worlds, restart,
destroy/recreate, application/staging/attachment changes, malformed generation,
capacity-two cancellation-ignoring work, late success/fault and awaited close drain.
Cancellation at first lookup closes archives, publishes no geometry and drains.
Native twelve negatives and eight prelaunch combinations pass. Old opaque-world
headless preview passes with its accepted 37-source inputs. Confidence: **Confirmed**.

## Verification, artifacts and resume

Godot build: zero warnings/errors. Independent world and original audits pass;
fresh GPU/headless/cancel acceptance markers and old-preview regression pass.
An initial test assumed four surfaces from four material descriptions; source
geometry showed repeated bindings, so validation now checks all source surfaces
and four distinct shader paths. An initial audit guessed a SAT directory; it now
selects the unique source matching the accepted catalog appearance pin. Neither
correction changes assets, shaders or presentation behavior. The known Godot
certificate-store startup diagnostic remains nonfatal.

Local audit directory: `local-private/round11/`. It contains generated peer JSON,
native log, final capture pair, randomized replicated-world logs, independent
oracle outputs and baseline-relative receipt/path hashes. Images, raw inputs,
build outputs and derived evidence remain ignored. Curated documents link only
to public specifications/scripts, not private artifacts.

Unresolved: original Height/posture/lighting behavior, full customized face and
palette semantics, other replication views, live world/gameplay and unsupported
mixed fields. No research/function/protocol score changes. Lead-owned shared
status/ledger integration remains outside worker scope. Exact next action: lead
review/integrates this milestone and receipt, then explicitly dispatches Round 12.
Rendering stops after this one Round 11 milestone.
