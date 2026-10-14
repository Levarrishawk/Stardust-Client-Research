# R14-G: guarded owned static visual and original terminal lifecycle

2026-10-02. Question: can accepted static resources become an attachable, current
Godot visual while retaining all original terminal LODs, keeping the explicit SWTS
inspection exception bounded, and retiring stale ownership deterministically?
Stop condition: scoped adapter plus standalone native/Godot/source/mip/pixel proof,
cleanup and lead handoff. Authorization is Round14 only.

## Inputs and orientation

Verified overlay:2,149 public files; HEAD
`65de8c88141071744dc75594be126b3bab3696a5`. Baseline manifest SHA-256
`43858afa91a89dbc33d33d3fd1e603e196ec25c56cfeefde8a1fd6ae5d5e9d6d`;
ZIP SHA-256
`9328987f72c4c575aa1f3bbc66b33eb00e3d44058fa5efcdc7911370053931e4`.
All archive member paths/containment, uniqueness, sizes and hashes were preflighted
before overwrite; every resulting public baseline hash matched. Ignored/unlisted
inputs and the intentionally dirty tree were preserved. No Git mutation.

Read AGENTS, START_HERE/latest Round13 session, static/SWTS resources and existing
preview checkpoints/contracts, current status/ledger/UNKNOWN, original auditor,
GeometryAppearancePreview and StaticSwitchTexturePreview before implementation.
The same-round world API was not consumed; the lead requested an injected lifetime
predicate overload as a future integration point, while registry tests stay baseline-only.

Original root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
Accepted terminal catalog:CRC2907007746; object
`object/tangible/terminal/shared_terminal_mission.iff`, SHA
`E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281`;
APT SHA `74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021`.
Explicit pinned SWTS frame0 as recorded in the durable contract. Existing .NET8,
Godot4.7.2 Mono, cached Python/Pillow and source-cleared offline NuGet were used.
No executable addresses were relied on; no executable execution, networking,
external mutation, downloads or publication.

## Observations and implementation consequences

[Adapter](../../../SWG-Godot-Client/poc/Stardust.Godot/SceneStaticVisual.cs) requires a current finalized
exact registry handle with byte-identical raw create, or caller-derived Transform3D
plus a borrowed serialized lifetime predicate. Registry placement uses accepted
SceneCreatePlacementBuilder Reject policy and SRS reflection. Predicate checks
precede allocation and follow construction/attachment before visibility. Wrong-thread
calls reject before metadata/GPU/node mutation. Stale calls retire; staged stale or
faulted transfer disposes its candidate. Disposal releases root/child tree, adapter,
inspection material, parsed-source and predicate references. Confidence:
**Confirmed** for executed positive/negative paths. Future world callers must provide
accepted publication provenance/currentness; arbitrary forged graphs are not authenticated.

CPU policy requires complete material/geometry descriptors with preallocation
mesh/texture/surface caps. The existing geometry adapter retains four LODs and seven
textures. Only the explicit terminal flag permits the exact two pinned SWTS frame0
MAIN/UV0 unshaded overrides. Default presentation rejects and rolls back those special
screens. Sixteen ordinal changes, visibility changes and placement refresh preserve
GPU IDs. Unsupported transforms and foreign creates leave placement unchanged.
Confidence: **Confirmed**; consequence: source completeness and shader fidelity are
separate, and this inspection exception must remain opt-in and labeled.

[Standalone preview](../../../SWG-Godot-Client/poc/Stardust.Godot/SceneStaticVisualPreview.cs) loads two
independent terminal closures off-owner in one read-only archive scope. Each has27
source reads/1,401,794 bytes, four inherited templates/four LODs, five shaders/four
effects, seven textures/all62 CPU mips. All source evidence remains unchanged after
visual lifecycle. Native clearing54 borrowed resolver buffers preserves complete
evidence. The [independent original oracle](../../../SWG-Godot-Client/poc/round14-tests/Rendering/Verify-Original.py)
reconstructs source union/priority/SHA, inheritance, geometry and all62 original
mip hashes; native/GPU/headless records agree. Confidence: **Confirmed**.

A and B use independently authored high-bit IDs and nonunit quaternion
(.2,-0,.3,1.7), with wire positions(-2,7,4)/(8,7,4). A's core origin is(-2,4,7),
Godot origin(-2,4,-7), norm1.7378147687453016. The
[independent visual oracle](../../../SWG-Godot-Client/poc/round14-tests/Rendering/Verify-Visual.py)
uses quaternion-vector cross products and SRS to verify all basis columns rather
than copying the implementation's matrix equations. Raw signed zero remains in
47-byte create evidence. A initial handle2/incarnation1 becomes handle6/incarnation5
on recreation. B stays current with identical GPU IDs. Recreated A reuses immutable
prepared CPU resources under a new exact handle, creates new GPU IDs, and performs
no archive reload. This is modern cache/publication policy, not recovered lifecycle.

Whole terminal initial/recreated PNGs were visually inspected and are byte-identical
below UI row100:30,326 nonbackground pixels, bbox(536,105,743,420),4,374 warm screen
pixels. The fixed camera leaves the whole object visible. Confidence: **Confirmed**
for replacement pixels. No original rendering, SWTS automatic timing/initial frame,
parent transforms, component/gameplay/damage meaning or live compatibility claim.
GPU verification checks original uploaded geometry and all bound MAIN mip bytes;
all62 CPU mips are independently audited, including auxiliary textures.

## Validation and corrections

Godot build passes with zero warnings/errors. Native68 negatives include all47
create truncations, descriptor/policy/placement/capacity and foreign/lifecycle/reset/
close checks. Godot22 negatives plus16 selections cover four wrong-thread actions,
budget rejection before allocation, detached parent, stale/foreign/disposed state,
false injected guard, injected retirement, stale/faulted transfer rollback and
strict-default rejection. Positive cleanup allocates/disposes7 roots; cancel
publishes0, closes the archive and observes the worker. Five prelaunch negatives
leave log counts unchanged and captures absent. Original/visual oracles pass.

The first headless run caught reversed positional registry constructor arguments;
corrected to named capacity2/generation1 before passing reruns. Preview close shares
one shutdown task, waits for the worker, uses finally for fault cleanup and observes
external tree-removal cleanup. Cancellation and ordinary teardown execute this
shutdown path; manual OS-close/tree-removal behavior was inspected, not separately
UI-exercised. The existing root-certificate-store startup diagnostic is nonfatal;
no other positive-run error or leak warning was observed.

Exact commands and schemas are in [the durable contract](../../docs/SCENE_STATIC_VISUAL_CONTRACT.md).
Private receipt/commands, logs, source-derived details, baseline audit/diff and
images remain under ignored `local-private/round14/`. Workspace validator, focused
new-file whitespace/link checks and Git diff check complete the handoff. Shared
research metrics/registries/ledgers are unchanged and lead-owned.

## Exact scope and handoff

Twelve new files, no baseline file modification:

- `poc/Stardust.Godot/SceneStaticVisual.cs`
- `poc/Stardust.Godot/SceneStaticVisualPreview.cs`
- `tools/powershell/Invoke-StardustSceneStaticVisualPreview.ps1`
- `docs/SCENE_STATIC_VISUAL_CONTRACT.md`
- this checkpoint
- `poc/round14-tests/Rendering/Generate-Fixture.py`
- `poc/round14-tests/Rendering/Rendering.csproj`
- `poc/round14-tests/Rendering/NuGet.Config`
- `poc/round14-tests/Rendering/Program.cs`
- `poc/round14-tests/Rendering/Verify-Original.py`
- `poc/round14-tests/Rendering/Verify-Visual.py`
- `poc/round14-tests/Rendering/Verify-Launch.ps1`

Main registration remains lead-owned. Proposed exact branch before generic asset-root
handling: `if(args.Contains("--scene-static-visual-preview")){AddChild(new SceneStaticVisualPreview());return;}`.
The private-scene launcher independently verifies without this registration. Lead
can attach the explicit-transform/predicate overload to its completed generic world
publication without a duplicate registry, then perform a fresh heterogeneous scene
acceptance and navigation updates. That integration is the exact next action; no
unfinished worker API was assumed here. Further original SWTS timer/factory research
would require a separately bounded task and executable hash check. Worker stops idle
after Round14; no Round15 work is authorized.
