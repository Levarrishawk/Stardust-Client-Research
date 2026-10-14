# Original multi-door scene host

The prototype host preview consumes the [pinned scene bundle](INTERIOR_DOOR_SCENE_BUNDLE_CONTRACT.md) and [shared typed scheduler/timeline](TYPED_ACTOR_SCHEDULING_CONTRACT.md). The current bounded inspector requires two selected leaves forming a three-cell route. Core scene/bundle APIs retain their broader documented resource limits.

PowerShell options: `-SceneSpecification`, `-SceneSpecificationSha256`, `-DoorTableSha256`, plus existing archive/configuration/member/layout identity and headless/capture/interactive options. The specification selects its doors and precise local-step policy; single-door inspector flags cannot be combined. The layout pin supplied to the launcher must match the bundle's independently pinned POB.

The preview draws all loaded floors, both independently posed primary leaves and the persisted actor. Controls provide forward/reverse/wait/reset, second-door entry permission, transformed owner, pause and continuous physical W/A/S/D movement. Each render frame advances the typed integer clock; key transitions submit timestamped normalized planar input. Focus and mode changes consume suspended input without replaying motion. The scheduler borrows the scene; exiting disposes the owned controllers, geometry previews and resources.

[Host acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_PROTOTYPE.md) passes headless/GPU gates on the same Mustafar 5→6→7 route: 16 manual transfers over two poses, second denial/retreat/reset, actual physical key event/release route and normalized diagonals, two-pose diagonal owner round trips, timestamped four-transfer scene route, stall/pause/focus and both leaf/actor/floor placement. Private capture inspected. This remains an offline floor/door scene, with full building obstacle meshes and closing-leaf overlap response unresolved.

`-HoldClosingDoors` explicitly enables [modern closing safety](DOOR_CLOSING_SAFETY_CONTRACT.md). Its original headless/GPU gate passes occupied hold, denied entry, retreat/release and existing controls. Full obstacle meshes, opening sweeps and original collision order remain unresolved.

`-SceneGeometry` selects appearance geometry for the loaded floor cells. Room and floor roots share the building owner pose; the interior camera hides the actor box and view switching preserves simulation state. The optional floor overlay remains available. [Selected-room gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md) passes original headless/GPU placement and controls. Room meshes currently use neutral inspection materials and do not participate in collision.

`-SceneTextures` requires `-SceneGeometry` and attaches the existing explicit MAIN-only material inspection. [Selected texture gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_TEXTURES.md) independently matches 26 textures / 251 mips and 25 effect records. Headless/GPU host draws 42 textured surfaces with two diagnosed neutral collision-only fallbacks. Lighting/specular/emissive rendering remains untranslated.

[Zero-pass rendering](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ZERO_PASS_SURFACES.md) now omits primitives when every decoded effect candidate has zero passes. Selected room counts are 42 textured / two omitted / zero neutral surfaces, with no material diagnostics. Upload verification retains original primitive indices; hiding geometry does not add collision behavior.

`-PortalVisibility` requires `-SceneGeometry` and enables the [bounded modern visibility query](PORTAL_VISIBILITY_CONTRACT.md). Interior view selects rooms from the actor cell/camera; overview renders all selected rooms. Original two-pose node selection and state-preserving controls pass. No legacy DPVS equivalence is claimed.

[Camera heading input](ACTOR_VIEW_INPUT_CONTRACT.md) now supports continuous mouse look, camera-relative horizontal keys and pitched portal visibility. Tab activates/releases; Escape, focus/pause/manual/overview/reset/exit release. Native timing and original headless/GPU dispatched-event gates pass; interactive OS capture remains manually unverified.

The host now uses the [owned combined content reader](INTERIOR_SCENE_CONTENT_CONTRACT.md), sharing normalized source reads and global source budgets across actor and selected visual stages. [Original acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_CONTENT.md) verifies 61 unique sources / 9,262,168 bytes with one external lookup each.

The current visual adapter owns one composite graph for rooms and both doors. Independent leaf nodes share both immutable LOD meshes; camera LOD and world transforms remain per instance. One GPU texture cache serves the graph. [Shared scene gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_SCENE_VISUALS.md) passes headless/GPU resource identity and placement: five textured templates / 44 unique textured surfaces / two omitted / zero neutral / 28 GPU textures. Door MAIN textures are now included.

CPU content loading now uses the [owned cancellable worker](BACKGROUND_ASSET_LOAD_CONTRACT.md). Configuration/TRE streams belong to the worker and close before transfer; prototype host resources are created on the main thread. A responsive loading panel supplies Cancel. [Success/cancellation gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BACKGROUND_SCENE_LOAD.md) passes original headless/GPU success and headless pending-lookup cancellation with no scene publication.


## Explicit original-profile sphere

Launcher `-ActorTemplate` and `-ActorTemplateSha256` require a pinned scene specification and select the deterministic object-root profile. Background loading delivers owned profile values and the70-source content union before GPU creation. The displayed collision sphere uses recovered center(0,1,0)/radius0.5 for the original human sample; camera height remains the separate preview policy. Character skeletal appearance is not loaded by this option.

Sphere mode preserves the existing controls but supplies no step/slide policy. Room triangles remain visual. Closing safety uses the declared enclosing cube. Original headless/GPU gates pass existing route/input/placement checks plus occupied closing hold, denial, retreat and release under two poses. Pending lookup cancellation yields zero scene publication. Private GPU capture was inspected. Default box host regression passes. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_PROFILE_PREVIEW.md).


## Explicit modern room-triangle sphere guard

Optional `InteriorSceneTriangleGuard` owns direct static-mesh room triangles selected by explicit collision selector0/1. Source/retained capacity and cell limits are enforced before publication. Content creation requires actor profile, appearances and materials; the original host selects selector1/capacity65,536 using `-SceneTriangleGuard`. All42,986 original triangles and the70-source union agree independently.

The sphere scene preflights static work before controller mutation, compares static and captured door/barrier progress, and validates moved landings against both obstacle sets. Door contact wins exact ties; unsupported combined landing retains initial owner/position. No stepping/sliding or original actor selection claim follows. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMBINED_SPHERE_SCENE.md) records native positive contacts, original2,880-tick audit, resource atomicity, local timing and headless host acceptance.
