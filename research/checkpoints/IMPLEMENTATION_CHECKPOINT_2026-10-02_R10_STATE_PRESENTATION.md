# R10-RENDER-01: guarded creature state presentation

Date: 2026-10-02. Question: can current incarnation state drive bounded Height and
Visibility presentation while preserving GPU resources and rejecting stale state
without retiring a still-valid resource visual? Stop condition: independent native
bytes/placement oracle, original 37-source audit, fresh fixed-camera GPU pair,
headless/cancellation/negative fixtures and deterministic cleanup all pass.

The canonical policy and reproduction interface are in the
[state presentation contract](../../docs/CREATURE_STATE_PRESENTATION_CONTRACT.md).
This checkpoint adds no executable-address claim or research coverage score.

## Inputs and scope

Worker checkout: isolated ed09 Stardust-Client. HEAD:
`65de8c88141071744dc75594be126b3bab3696a5`. Accepted Round 10 baseline contains
2,000 files. ZIP SHA-256:
`b22195465dd313d509571ca0d732a5fffb058658d41ee2a41750b82448fb4e2a`.
Manifest SHA-256:
`58c635c920215d7fbb47de5d8d7b9c636f96291b8b841e61593e195529072f82`.
Every baseline file was checked before and after synchronization. Scope is measured
against that manifest because the accepted baseline intentionally differs from HEAD.

Original input root: `C:/Stardust-DEV`, configuration `swgemu_live.cfg`, with the
configuration pin stated in the contract. Existing catalog pins constrain
`object/creature/player/shared_human_male.iff`, its SAT, logical idle selection,
and the human locomotion profile. The independently audited effective source set
contains 37 entries and 4,683,848 bytes. No original executable was run or needed.

Tools: .NET SDK 8.0.300, Godot 4.7.2 .NET x64, OpenGL GPU renderer on RTX 4070 Super,
bundled Python and Pillow, existing offline NuGet cache. Existing visual, Core,
previews, projects, shared runners, ledgers and registries remain unchanged.
The only accepted-baseline modification is one opt-in Main branch; all other worker
files are additions within the assigned allowlist/test directory.

## Evidence and interpretation

Observation: the adapter checks exact resource handle/create bits and current
store-owned state before allocation and after staging/attachment. Tests advance
state during staging and ChildEnteredTree, obtaining null publication and one
disposal; attached root IDs no longer resolve. Confidence: **Confirmed**.
Consequence: invalidated staged state cannot publish a visible actor.

Observation: state revision advancement invalidates the old snapshot, while the
same actor's guard and resource fingerprint remain valid. The updated current
snapshot applies; the old snapshot returns false. The publication closure drops
the staged snapshot after success and retains only its incarnation token.
Confidence: **Confirmed**. Consequence: obsolete baselines are not retained by
visual lifetime, and state freshness is separate from resource identity.

Observation: five raw finite Height deltas (positive zero, negative zero, negative,
below bound and above bound) remain intact in the store but presentation rejects
each without changing the actor transform/fingerprint. Inclusive endpoints pass.
Native checks additionally reject NaN and both infinities. Visibility bytes 0 and
255 map to hidden/visible after baseline reseed. Other scalars are displayed only.
Confidence: **Confirmed for the modern implementation**. Original presentation
policy: **Unknown**. Consequence: raw codecs preserve their accepted domain;
presentation uses an explicitly bounded positive uniform multiplier.

Observation: native and GPU/headless logs agree with the independently packed
47-byte create (including negative-zero quaternion Y), complete baseline and
Height delta. A cross-product quaternion oracle verifies engine basis/position
and one SRS reflection. Height 1.5 multiplies every basis column while preserving
origin `[-2,4,-7]`. Confidence: **Confirmed**. Consequence: state presentation does
not accidentally transpose placement or reorder wire X/Z/Y coordinates.

Observation: fresh baseline/delta PNGs have 13,044 changed pixels below UI row 180;
actor widths are 62/91 pixels and heights 200/296 pixels. Camera transform, idle
frame and resource allocation fingerprint remain fixed. GPU mip readback passes.
Confidence: **Confirmed for the bounded original-input replacement preview**.
Consequence: Height changes actual actor presentation without rebuilding resources.
Authored MAIN remains pale; full customized albedo is outside this round's input.

Observation: native runner passes 12 negative checks and two bound endpoints;
GPU/headless fixtures cover foreign store, application recheck, stale preflight,
staging/attachment invalidation and destroy/same-ID recreation. Cancellation closes
archives and publishes zero geometry. Eight invalid launcher combinations create
no new preview logs/captures. Teardown frees owned roots/resources/loaders.
Confidence: **Confirmed**. No live-server, transport or original-client execution
behavior is established.

## Validation and resume point

The build succeeds with zero warnings/errors. Native source/byte/placement checks,
independent archive audit, GPU/headless/cancellation fixtures and prelaunch checks
pass. The initial native restore encountered host AppData access denial; setting
APPDATA to the existing private build directory and using the source-cleared
config/offline cache resolved it. Godot's known startup certificate-store diagnostic
does not prevent the fixture or capture acceptance markers.

Private evidence is under the checkout's `local-private/round10/`: generated
`peer.json`, `native.log`, final capture pair, randomized state-presentation logs,
scope/check receipt and validation outputs. These are local audit paths, not
redistributable assets or curated document dependencies. Shared living documents
and metrics require lead integration and were intentionally left unchanged.

Unresolved: original Height units/semantics, posture animation mapping, complete
customized material presentation, and integration with a newly accepted world
publication owner. Exact next action: lead reviews this bounded adapter/receipt and
integrates the Round 10 handoff before assigning Round 11. The rendering worker
stops here; no further round is started automatically.
