# Live color/style customization

## Owned revision worker

`SkeletalCustomizationRevisions` retains one immutable CPU source bundle. Each submission owns an integer snapshot and monotonically increasing revision number. One composer runs at a time; a single pending snapshot is replaced by newer edits. Submission cancels cooperative current work and clears a previously completed result. Even a noncooperative old worker's late success/failure cannot become the current result. Only the newest completion transfers, once, through `TryTake`; failure is explicit and does not replace the published appearance. Disposal clears pending/ready state, requests cancellation and forbids later submission/transfer. Worker completion remains awaitable without holding the owner lock or blocking the main thread.

The default composer is the [pure shared selection API](SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md); no historical implementation path (omitted) call occurs in this worker. Controlled portable gates prove one active composer, latest pending replacement, owned values, stale completed and noncooperative results, single transfer, explicit failure/recovery and disposal.

`DescribeControls` derives each key's valid index interval from owned palette cardinalities/style counts. Shared uses retain their independent source ranges; the UI's ordinary interval is their intersection. Explicit values outside the interval still follow each consumer's established fallback. This API does not introduce defaults or flatten namespace identities.

## Main-thread publication

Both standalone and interior shared-profile previews expose color/style spin controls and reset. Edits submit snapshots; frame polling takes the newest result and leaves the current appearance visible while composition runs or fails. Reset submits the loaded explicit profile. Current animation and actor state continue normally.

`SkeletalMeshPreview.ApplyCustomization` verifies unchanged geometry/skeleton/prepared-animation references. `SkeletalMaterialPreview.ApplyCustomization` verifies the same owned materials/effects/original textures/programs, albedo factor identities, generated bindings, output dimensions/color/format/alpha and exact RGBA extent. All new generated uploads prepare before published uniforms change. The commit updates retained material factor uniforms and MAIN bindings without a frame await. On upload/commit exception it restores old uniforms/bindings and releases new uploads. Successful commit releases previous generated textures. Original textures, meshes and material resource identities remain retained.

The owned texture cache supports explicit release and accounts decoded uploaded bytes; this is an ownership budget, not measured physical VRAM. Original human repeated updates retain11 textures/14,592,672 decoded uploaded bytes. Source data and selected CPU outputs remain owned independently of GPU lifetime. Exit disposes the revision owner before GPU preview resources, so CPU workers cannot publish into a closed scene.

## Verification

Portable suite verifies queue ordering/ownership/cancellation/failure/recovery and shared control cardinality. Prototype host standalone and interior controls exercise rapid three-edit selection, eight further updates, invalid snapshot failure and reset. Each publication compares current full uploaded pose/mesh/material fingerprints, bounds/placement and actor state before/after; normal animation may advance between worker requests. Across requests, topology/UV/material/mesh identities and texture cache accounting remain stable. Invalid albedo-identity and malformed-RGBA revisions reject before changing GPU state; the retained appearance still passes readback.

A first interior fixture incorrectly compared poses across asynchronous work while idle animation continued. It was corrected to check pose at each commit and topology/material identity across the interval. No pose/readback tolerance was raised. Final standalone/interior captures visually inspected; controls render cleanly in the standalone panel and reside in the interior scroll panel. Existing route, guarded-door, two-cycle animation, factor/face byte identity and filtered GPU sample checks pass. Shared-profile first-lookup cancellation proofs from the prior checkpoint remain applicable; revision-owner disposal is independently tested.

Reproduce with the same pinned shared customization commands, adding a fresh ignored `-CapturePath`. Existing native/independent original profile audits establish the74/139-source component outputs; live updates request no additional source. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIVE_CUSTOMIZATION.md).

## Limits and next probe

This is explicit preview snapshot selection, not the recovered full creation/network customization lifecycle. GPU commands are issued on the main thread; driver/device-loss transactional recovery is not proven. Legacy raster/display precision, lighting/specular, age normal-map texture selection and skeletal shape morphs remain separate. Next: recover/implement original MGN blend-target descriptions and one bounded shape-deformation slice before exposing shape controls.
