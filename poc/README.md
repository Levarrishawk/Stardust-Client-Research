# Stardust Client Prototype

This prototype develops the engine-neutral Stardust client core and verifies
that Godot can host it. It does not contact a live server.

## Boundaries

- `Stardust.Core` contains no Godot references.
- `Stardust.Core.SmokeTests` validates exact recovered wire vectors, bounds,
  reliability behavior, and deterministic rejection of malformed data.
- `Stardust.Godot` hosts the same offline transport transcript in headless and
  graphical modes.
- Default tests use synthetic credentials/assets and no sockets or live endpoint.
  An explicitly selected, read-only original TRE probe is available separately.

## Offline SOE implementation in progress

`Stardust.Core.Protocol.SoeSessionCodec` implements the recovered raw 14-byte
SessionRequest and 17-byte SessionResponse layouts. It checks packet length,
opcode, version, echoed connection ID, CRC width, and nonzero datagram size.
`SoeSeededCrc`, `SoeSelector4`, and `SoeDatagramCodec` implement the confirmed
established-session framing, including bounded receive-side zlib inflation and
uncompressed outbound packets. `SoeReliableChannel` covers bounded
four-channel sequencing, reorder, cumulative ACK, selective out-of-order ACK,
retransmit timing, and 16-bit wrap expansion. `LoginProtocolCodec` serializes
the confirmed `LoginClientId` shape and parses the fixture-backed token and
error responses while retaining the credential blob byte-for-byte. The core
also handles multipayload length boundaries and bounded inbound/outbound
fragmentation, including channel-zero fragments received out of order. The offline hosted
transcript continues through cluster enumeration, separate zone/ping endpoints,
and character enumeration with explicit 64-bit object IDs. `LoginClientSession`
owns this phase's state and clears retained credential-bearing buffers during
replacement and shutdown. `LoginClientCoordinator` turns monotonic clock ticks
and inbound datagrams into session retries, login sends, ACKs, and reliable
retransmissions without owning a socket.

Outbound compression remains unimplemented. An owned IPv4 UDP adapter now
compiles, while default hosting uses injected offline I/O. No socket has been
instantiated or exercised; live testing remains a separately authorized milestone.

## Hardened offline boundaries (2026-09-29)

The core now discards corrupt framing without ending login, clears owned state
on fatal/malformed/deadline paths, and drains queued fragments through multiple
send windows. An independent peer validates a 1.5 MB message and 32 seeded
loss/corruption/reorder traces. Outer SOE multipackets return every generated
control; protected ping and matching disconnect are handled. JSON configuration
is bounded/strict, and diagnostics accept only typed identifiers and counts.

`Stardust.Core.Assets` reads original TREE 0005 archives directly, verifies
filename CRCs, lazily decodes stored/zlib payloads and preserves priority/mask
semantics. Stored `csize=0` records remain live when `usize>0`. One installed
1,477-entry archive and a Python-matched entry were verified read-only.
Shared zlib decoding explicitly checks header and Adler-32 after reproducing
a missing-trailer acceptance defect.

See the [acceptance record](../docs/CLIENT_IMPLEMENTATION_ACCEPTANCE.md) for
ownership, configuration schema and unimplemented transport/asset gates;
the [TRE checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_TRE_READER.md)
contains the optional original-entry command. Mesh/texture rendering is still open.

## Host and original asset increment (2026-09-29)

Godot now awaits an engine-neutral async login host with one pending receive,
source filtering, monotonic ticks, retries, cancellation and owned completion.
The offline proof exercises foreign senders, malformed handshake, corruption
and reordered replies; it records six sends, seven peer receives, one foreign
drop and 18 allowlisted journal events. Local JSONL persistence is bounded and
never overwrites existing files. Successful results own a credential clone and
must be disposed; the login transport is closed after completion.

An owned IFF tree and strict static-mesh decoder match an original 190-vertex,
159-triangle mesh against an independent Python geometry hash. Explicit mounts
open all 48 configured TREE/0005 archives and resolve its nine shaders/nine DDS
envelopes/one effect. Shader decoding currently extracts external references
and preserves opaque metadata. Texture pixels, material conversion and original
rendering remain open; the visible scene remains synthetic.

See the [host and asset session](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_HOST_AND_ASSET_HOUR.md)
for all five checkpoints, exact original identities and reproduction commands.

## Pass criteria

1. Both projects compile for x64-capable .NET 8.
2. The standalone core matches the documented SOE and login golden vectors and
   rejects malformed, corrupt, oversized, and out-of-window input.
3. Godot invokes the offline SOE session-to-login transcript without a render
   window or frame-dependent state.
4. Godot graphical mode presents resulting state without owning it.
5. Startup and shutdown finish without errors.

Run the engine-neutral fixture and state-machine suite from the repository root:

```powershell
dotnet run --project .\poc\Stardust.Core.SmokeTests\Stardust.Core.SmokeTests.csproj
```

Set `$godot` to the Godot 4.7.2 .NET x64 console executable, then run:

```powershell
& $godot --headless --path .\poc\Stardust.Godot -- --self-test
```

## Explicit original asset inspector

Build Debug and run `tools/powershell/Invoke-StardustAssetPreview.ps1` with explicit `-GodotPath`, `-ArchiveRoot`, `-Configuration`, `-Member` and `-ExpectedSha256`. The [preview checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_ORIGINAL_GODOT_PREVIEW.md) supplies the verified logical member and identities. Default launcher mode is a hidden bounded GPU self-test; `-Headless` checks resources only. `-Interactive` opens the inspector: drag to orbit, scroll to zoom, R to reset. `-CapturePath` requires a fresh private PNG.

Default startup still runs offline login. The [DDS contract](../docs/DDS_DECODER_CONTRACT.md) lists legacy and modern formats and metadata policies. BC7 is now supported; BC6H still requires HDR output. Original lighting and full runtime appearance behavior remain future work. No original executable or socket is run.

### Authored detail inspector

Add `-Detail` to the launcher for a verified DTLA member. It eagerly verifies all authored child resources and exposes a selection-distance slider with orbit controls. See the [LOD checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_GODOT_LOD_INSPECTOR.md). The slider is an offline inspection control; actual camera/PIVT distance semantics and original lighting remain open.

### APT exterior entry point

The detail inspector accepts an APT redirect to DTLA. Add `-MainTextureOnly` explicitly for opaque MAIN texture inspection when the material lacks the default preview fallback. The inspector labels this unlit mode; it does not execute original lighting programs. The [exterior checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_APT_EXTERIOR_CHAIN.md) records exact source identities and verification.

### BC7 and pinned codec dependency

Typed DX10 BC7 UNORM/SRGB is supported across all eight modes. See [DDS contract](../docs/DDS_DECODER_CONTRACT.md), [BC7 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_BC7_DECODER.md) and [dependency notices](THIRD_PARTY_NOTICES.md). Lock files pin the managed codec graph. Restore with `--locked-mode` and an explicit package cache, then use `--no-restore` for builds/tests. Godot's `--dds-self-test FILE --dds-sha256 HASH` checks owned decoded mip uploads; BC6H still requires HDR output.

## Component geometry inspector

Add `-Component` to inspect static appearance graphs with neutral colors and explicit offline LOD distance. Omitted parts are counted; original particles/materials/lighting are not translated. See [contract](../docs/COMPONENT_APPEARANCE_CONTRACT.md) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_COMPONENT_GEOMETRY.md). Native `--geometry-graph` reports hashes/omissions under explicit root identity.

## Sorted-index mesh support

The core preserves versioned SIDX variants and selects the minimum local-camera dot product; the neutral component inspector still uses ordinary indices. See [sorted-index contract](../docs/STATIC_MESH_SORTED_INDEX_CONTRACT.md). Add `--geometry` to the independent component verifier for complete supported static-graph source/geometry/sorted hashes.

## Component textures

Add `-Component -ComponentTextures -MainTextureOnly` for mixed unlit MAIN inspection. Unsupported shader batches receive explicit neutral material diagnostics; original geometry remains. CPU descriptions and GPU textures are shared per load/preview. See [component contract](../docs/COMPONENT_APPEARANCE_CONTRACT.md). Independent `--textured` verification checks source/geometry/sorted/pixel identities.


## APPR/PIVT camera selection increment — 2026-09-30

The [camera LOD checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_PIVT_CAMERA_LOD.md) closes the primary extent distance origin: false/absent pivot uses the transformed authored sphere center, true uses world translation, and NULL uses zero local offset. All 6,198 original primary extents agree independently. Native malformed/transform fixtures and standalone/composed headless/GPU selection gates pass. The inspector supports opt-in camera distance and manual selection. Original DPVS, lazy residency and full lighting remain deferred. Five confirmed address labels bring the canonical registry to 2,590 distinct entries; research scores and packet counts are unchanged.

Use `-CameraLod` with `-Detail` or `-Component` to enable camera distance initially. The checkbox restores manual selection. Component nodes each measure their own world-space origin.


## BC6H HDR extension — 2026-09-30

The [HDR checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md) adds a separate owned RGB half output for typed signed/unsigned BC6H. Forty standards-only fixtures/264 mips match independent Godot bcdec bit for bit and pass real GPU half upload/readback. Native endpoint, truncation, surface and budget gates pass. Static material integration and HDR presentation remain separate; this is not original executable acceptance evidence. Existing pinned dependencies, research metrics and registry counts are unchanged.

Build the Godot project, then run `tools/powershell/Invoke-StardustHdrVerification.ps1 -GodotPath ... -PythonPath ... -OutputDirectory ...` with a fresh private directory. Default runs a hidden GPU gate; `-Headless` runs independent CPU agreement. `DdsHdrDecoder` exposes RGB half mips separately from `DdsDecoder` RGBA8.


## APPR hardpoints — 2026-09-30

The [hardpoint checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_HARDPOINTS.md) adds owned exact names/CRC/transforms and explicit duplicate queries to mesh/detail/component templates. All 31,151 effective APPR bases/13,253 hardpoints agree independently. The old serialized count/name-prefix claim is corrected. Ninety-one duplicate-name groups contain different transforms; ambiguous attachment choice remains unverified. Native and focused host gates pass; metrics/registry counts are unchanged.


## Owned portal layout — 2026-09-30

The [portal reader checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LAYOUT_READER.md) closes native deployed PRTO/CELL/PRTL parsing against all 291 layouts, 3,470 cells and 7,642 portal references. Owned geometry, names/door fields, optional bounds and paired zero-based adjacency pass independent agreement and negative fixtures. Scene loading, floor behavior and runtime visibility remain separate. The older 00B43E80 neighbor-helper attribution is corrected to a byte toggle. Metrics and registry counts are unchanged.


## Portal cell scene increment — 2026-09-30

The [cell scene checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_CELL_SCENE.md) closes shared bounded cell appearance loading and named-cell offline inspection. The pinned original YT-1300 matches independently across eight cells, 14 sources, 11 meshes, 66,617 vertices, 55,916 triangles and 310 sorted variants. VTXA `0x110D` preserves its packed word and correct UV offset. Native ownership/budget/identity tests and Godot headless/GPU gates pass. Manual visibility is distinct from unimplemented portal culling, floors, doors and lighting. Research scores, packet rows and address counts are unchanged.


## Owned floor reader — 2026-09-30

The [FLOR checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_MESH_READER.md) verifies all 4,730 deployed floors: 414,301 vertices, 604,429 triangles and 189,927 boundary records. Owned adjacency, authored normals/raw flags and optional payload hashes agree independently. Negative and ownership fixtures pass. Floor navigation/movement remains separate; metrics and registry counts are unchanged.


## Floor surface inspection — 2026-09-30

The [query checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_SURFACE_INSPECTION.md) closes a modern owned deterministic BVH surface inspector: 32,918 exhaustive independent probes across 4,730 floors agree. The original scene now includes seven floors/149 triangles under 21 shared sources; headless/GPU geometry and reflected picking pass. Native analytic/negative/ownership cases pass. Original adjacency path walking, permission and portal transitions remain unimplemented. Metrics and registry counts are unchanged.


## Owned path graph — 2026-09-30

The [PGRF checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PATH_GRAPH_READER.md) closes deployed graph field decoding against all 3,914 graphs, 39,647 nodes and 86,964 directed edges. Raw positions/keys/types, width floats and ECNT/ESTR ranges agree independently. Empty zero-count/start-minus-one entries are preserved. Native malformed/ownership gates pass. Original navigation cost, cross-cell mapping and dynamic permission remain separate; metrics and registry counts are unchanged.


## Directed route inspection — 2026-09-30

The [route checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DIRECTED_ROUTE_INSPECTOR.md) closes modern owned Euclidean/Dijkstra inspection against 25,847 independent Bellman–Ford probes, with exact edge-chain validation. Eight original floor/layout graphs upload and route-display in Godot; hidden GPU capture passes. Original client cost, cross-cell association and dynamic permission remain distinct. Metrics and registry counts are unchanged.


## Portal/floor association audit — 2026-09-30

The [association checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_PATH_ASSOCIATIONS.md) rejects floor-key-as-layout-ordinal and verifies conservative geometry-key bindings for all 291 layouts: 14,783 valid bindings, 3,581 paired candidates, 977 multi-node groups and 14 incomplete groups. Thirty-one bunker contradictions and 13 missing police-station floors remain explicit. Candidates do not establish legal movement. Shared cache failure accounting/pin guards pass; headless MultiMesh readback limits are reported accurately. Metrics and registry counts are unchanged.


## Textured interior inspection — 2026-09-30

The [textured portal checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TEXTURED_PORTAL_SCENE.md) closes bounded shared interior materials and baseline IMPL0004/PASS0008 decoding. The original eight-cell scene independently agrees on 77 sources, seven complete material meshes, 23 textures/221 mips and 18 effect records. Four customizable exterior bundles remain explicit omissions. GPU unlit MAIN inspection uploads all texture mips and displays 78 textured/ten neutral surfaces; strict headless and native negative/budget fixtures pass. Original lighting/customization remain separate; metrics and registry counts are unchanged.


## Owned customizable shader reader — 2026-09-30

The [CSHD checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CUSTOMIZABLE_SHADER_READER.md) verifies all 5,173 deployed version 0001 shaders,5,446 choices,856 selections and 7,908 palette factors. Native owned records agree independently on exact names, signed ranges/defaults, flags and inline shader identity. Static calls confirm private/shared-owner namespaces and clamped texture indexing. Inline values and authored defaults remain separate; runtime customization/palette translation is open. Metrics and registry counts are unchanged.


## Authored customization texture inspection — 2026-09-30

The [authored texture checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_AUTHORED_TEXTURE_INSPECTION.md) verifies856 selections under 2,568 default/extreme cases and completes all 11 original scene material bundles under explicit preview policy. Independent agreement covers87 sources,33 textures/321 mips and 39 effect records. GPU inspection shows85 textured/three neutral surfaces; unused choices are not loaded. Palette colors, original lighting and live customization remain open. Metrics and registry counts are unchanged.


## BC6H material integration — 2026-09-30

The [HDR material checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_MATERIAL_BUNDLES.md) preserves typed BC6H RGB-half data through shared core material caches and actual Godot MAIN samplers. Independent40-fixture/264-mip agreement and 80 template bindings pass on GPU and separately headless. Mixed RGBA/HDR aggregate budgets pass exact/fatal gates. Original87-source/321-mip scene agreement remains intact. HDR tone mapping/full lighting remain separate; metrics and registry counts are unchanged.


## Owned palette reader — 2026-09-30

The [palette checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_READER.md) verifies264 palettes/23,410 entries and 24,730 index queries. Raw flags, opaque ARGB, three ignored tails and low-version warning policy pass. The prior missing-fallback color label is corrected to cyan 0xFF00FFFF. CSHD references expose one missing palette and 16 out-of-range authored defaults; these remain explicit. Metrics and registry counts are unchanged.


## Palette-factor bundles — 2026-09-30

The [factor checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_BUNDLES.md) closes bounded pinned palette dependencies and explicit supplied/authored factor queries. All 7,908 factors agree under 23,724 independent cases. Exact namespaces, owned results, missing/masked cyan, empty/malformed distinctions and aggregate budgets pass native gates. One missing factor and sixteen invalid defaults remain explicit. Original lighting/live customization are separate; research scores and registry counts are unchanged.


## Palette-factor inspector — 2026-09-30

The [swatch checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_INSPECTOR.md) adds editable authored/supplied color inspection with exact variable namespaces and shared-key controls. Original YT-1300 and missing-palette swamp-trooper shaders pass two-swatch GPU readback; headless and synthetic shared-control gates pass. Captures remain private. Scene material lighting/live owner state remain separate; metrics and registry counts are unchanged.


## Hardpoint instance inspection — 2026-09-30

The [placement checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_HARDPOINT_INSTANCE_INSPECTION.md) verifies owned per-instance hardpoint composition against two original graphs: 26 placements/312 fields. Exact name ambiguity, occurrence paths, cycles and limits pass native gates. Godot reflected marker transforms and cell/LOD visibility pass headless/GPU inspection with readable selected labels. Original runtime attachment lookup remains separate; metrics and registry counts are unchanged.


## Same-cell surface route selection — 2026-09-30

The [connection checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SURFACE_ROUTE_SELECTION.md) verifies owned floor-hit-to-nearest-node selection against all 149 original scene probes. Missing/empty graph and cell isolation policies pass native gates. Seven reflected floor samples drive named-cell/route-start controls in headless/GPU hosts. Original walking, costs and portal transitions remain separate; metrics and registry counts are unchanged.


## Single-cell floor movement — 2026-09-30

The [movement checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_CELL_FLOOR_MOVEMENT.md) closes an owned horizontal single-cell walker and two-pick Godot inspector. Static edge/caller evidence, 500 seeded grid walks, 17,658 independent probes across 4,441 eligible installed floors, and seven reflected cell adapters pass. Portals override raw flags; missing-neighbor transitions, special collision edges and nearly vertical surfaces stop explicitly. Cross-cell permission/owner continuation remains next. Research scores, packet rows and stable-name counts are unchanged.

## Portal floor transfer inspection

Floor inspection now provides Portal transfer, Entry allowed and Barrier open controls for a two-pick, one-portal continuation. These are offline policy inputs. The engine-neutral query retains explicit owner/contact outcomes and rejects unresolved geometry or permissions. See [contract](../docs/PORTAL_FLOOR_TRANSFER_CONTRACT.md) and [reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_FLOOR_TRANSFER.md).

## Portal barrier actor bounds

In floor inspection, enable Walk between picks, Portal transfer and Actor bounds to inspect a 0.5×1.8×0.5 actor box against permission barriers before owner transfer. Entry allowed and Barrier open are offline controls. Nonlevel sweeps stop explicitly. Physical door-leaf clearance remains separate. The core also exposes frame-pulsed normal-door controller primitives. See [contract](../docs/PORTAL_BARRIER_COLLISION_CONTRACT.md) and [reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_BARRIER_COLLISION.md).


## Original single-leaf door inspector

The [door contract](../docs/SINGLE_LEAF_DOOR_CONTRACT.md) adds a standalone original normal-leaf inspector. Use the existing asset launcher with `-DoorStyle cantina_door` and `-DoorTableSha256`, plus pinned leaf identity. Contact, entry permission, step and reset controls drive the engine-neutral runtime. Neutral geometry and conservative authored-box clearance are explicit; interior instance wiring, paired leaves and effects playback remain next. See [exact reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_LEAF_DOOR.md).


## Original interior doorway inspector

Use the same launcher with a pinned POB member plus `-DoorStyle cantina_door`, `-DoorTableSha256`, `-DoorCell 3`, `-DoorPortal 10` and `-DoorLeafSha256`. It displays the original leaf and both floors with actor-box crossing, permission, contact, step/reset and owner placement controls. See [exact command](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md). Reverse inspection uses cell 12 / portal 0. Crossing replays fixed source/target points; persistent actor movement is next.


## Persistent doorway stepping

Add `-DoorWalk` to the pinned interior door launcher command. Forward/backward/wait controls move from the current actor locator and update actual cell ownership; current proximity drives one shared leaf. Permission, owner placement and reset controls remain explicit. See [contract](../docs/PERSISTENT_DOOR_ACTOR_CONTRACT.md) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PERSISTENT_DOOR_ACTOR.md).

## Continuous doorway movement

In `-DoorWalk`, enable continuous movement and hold physical W/S to move along the selected doorway axis at 1.6 m/s. Pause/focus loss suspends a bounded 50 Hz engine-neutral host; reset starts a new epoch. Manual step buttons are disabled while continuous mode owns input. See [timing contract](../docs/FIXED_STEP_ACTOR_CONTRACT.md).

## Timestamped movement input

Continuous doorway controls now queue physical key/permission transitions at monotonic timestamps and apply them at fixed tick starts. Focus/pause and render reconciliation preserve no-backdating behavior. See [timeline contract](../docs/ACTOR_INPUT_TIMELINE_CONTRACT.md).

## Planar doorway controls

Continuous `-DoorWalk` now uses W/A/S/D: W/S toward target/source, A/D doorway-relative left/right, with normalized 1.6 m/s diagonal speed. Floor navigation remains a feet query and actor bounds apply to door/barrier clearance. See [planar contract](../docs/PLANAR_DOORWAY_ACTOR_CONTRACT.md).

## Rigid detail door assets

Single normal door primaries now accept complete static-mesh DTLA bands whose geometry fits the authored root EXBX. The collider stays fixed across visual LOD changes. Installed acceptance: 91 rows; secondary/force/flip and declaration failures remain separate. See [capability matrix](../docs/DOOR_STYLE_CAPABILITY_MATRIX.md).

## Second original detail-door instance

The selected Mustafar mining 5/1 ↔ 6/0 pair verifies upward primary motion with fixed DTLA root bounds, persistent actor/timed planar input and headless/GPU controls. The generic persistent verifier accepts explicit member/layout/cell/portal/leaf pins. See [readiness record](../docs/DETAIL_DOOR_INSTANCE_READINESS.md).

## Stable returned floor contacts

Returned floor-edge positions are canonicalized inward within 0.1 mm when float rounding would make the next start invalid. Unrepresentable contacts hold the validated source explicitly. See [stability contract](../docs/FLOOR_CONTACT_STABILITY_CONTRACT.md).

## Piecewise floor paths

The core exposes double-precision triangle-plane movement segments alongside the unchanged reusable float locator. See [trace contract](../docs/FLOOR_PATH_TRACE_CONTRACT.md).


## Piecewise actor-box sweep

The core sweeps fixed actor bounds along actual floor heights against one immutable rigid box. Earliest contacts preserve request parameters; locator adoption remains separate. See [sweep contract](../docs/FLOOR_PATH_BOX_SWEEP_CONTRACT.md).


## Single-cell floor and box movement

The core now returns validated reusable floor locators at piecewise box contacts, with explicit holds when float precision cannot meet the contact budget. See [single-cell contract](../docs/SINGLE_CELL_FLOOR_BOX_CONTRACT.md).


## Original floor-box contact and level guard

Original sloped-plane/synthetic-obstacle fixtures verify reusable stops, bounded rounding correction and zero repeated-contact drift. Portal barrier movement remains level-only and now checks source/target intermediate planes. See [floor-box contract](../docs/SINGLE_CELL_FLOOR_BOX_CONTRACT.md) and [guard checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LEVEL_PATH_GUARD.md).

## Precise portal floor-box query

The core can sweep captured world-space boxes along source and target floor planes, including target geometry reached by actor bounds before feet cross. Door actor integration remains separate. See [query contract](../docs/PORTAL_FLOOR_BOX_CONTRACT.md).

## Precise persistent doorway option

PortalDoorActorSimulation accepts usePreciseMovement: true for connected sloped triangle-plane paths and conservative captured box collision. [Contract and bounded original gates](../docs/PRECISE_DOOR_ACTOR_CONTRACT.md). The existing level movement default is preserved.

The Godot launcher accepts -DoorWalk -PreciseDoorWalk for this explicit sloped actor mode; pinned YT-2400 headless/GPU gates pass.

## Bounded riser proposals

[Floor step proposal](../docs/FLOOR_STEP_PROPOSAL_CONTRACT.md) identifies a supported landing through reciprocal solid vertical strips under explicit modern limits. It does not move an actor or establish collider clearance.

## One-step collision query

[One-step floor box walker](../docs/FLOOR_STEP_BOX_CONTRACT.md) checks vertical actor clearance before adopting a supported landing and continuing horizontally. It is a separate modern same-space query; door movement integration remains next.

## Portal actor modern steps

[World step/portal contract](../docs/PORTAL_FLOOR_STEP_CONTRACT.md): usePreciseMovement: true plus an explicit FloorStepPolicy enables bounded steps through original thresholds. One step/one cell transfer per request, upright owners, captured world boxes and supported-floor rollback. Host exposure remains next.

The Godot launcher now accepts -DoorWalk -PreciseDoorWalk -StepDoorWalk for the explicit bounded step policy. Pinned small-house headless/GPU controls and reflected placement pass.

## Local vertical coverage option

[Explicit local step policy](../docs/FLOOR_STEP_RAY_CONTRACT.md) uses UseLocalRayCoverage: true to require a covered vertical contact line, excluding distant attached walls and rejecting gaps. Whole-strip behavior remains default.

## Explicit local step preview

Add `-LocalStepDoorWalk` with `-StepDoorWalk -PreciseDoorWalk -DoorWalk` to select local vertical ray coverage. The pinned black-sun 1/1 and 2/1 host gates pass. This is modern bounded policy; original solver tolerances remain unknown.

## 2026-09-30 connected approach

Optional `ConnectedApproach` anchors inspection to the selected paired floor sheet; it requires a separate actor clearance gate. Core original screen and repeated lower-floor trace pass. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_APPROACH.md).

Use `-ConnectedDoorApproach` with `-LocalStepDoorWalk -StepDoorWalk -PreciseDoorWalk -DoorWalk` for the explicitly paired connected-floor fixture. Black-sun lower-floor 2/0 and 3/0 host gates pass.

## 2026-09-30 multi-door scene

[Bounded owned scene](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_SCENE.md) passes synthetic three-cell/two-leaf lifecycle and 80 repeated native transfers. Independent reference matches 1,440 ticks / 24 transfers / 128 blocked ticks. Original scene gate is next; no coverage score changes.

## 2026-09-30 original multi-door route

[Existing Mustafar scene](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md) independently passes 2,880 ticks / 24 transfers / 172 blocked ticks / zero steps across three cells, two physical leaves and two owner poses. Directional denial/unknown/retreat/recovery and controller lifecycle checked. No broad new POB survey or score change.

## 2026-09-30 shared typed scheduling

[Shared scheduler/timeline](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TYPED_SCENE_SCHEDULING.md) preserves single-door API and complete multi-door snapshots. Three render partitions and direct 420-tick scene reference match; stall/pause/partial fault/reset/dispose/reentrancy and existing single-door native/original headless gates pass.

## 2026-09-30 pinned scene bundle

[Reusable bundle](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_BUNDLE.md) pins bounded explicit selections and shares normalized asset lookup. Malformed/duplicate/bad identity gates and unchanged 2,880-tick original scene reference pass. No score changes.

## 2026-09-30 original scene host

[Godot multi-door scene](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_GODOT.md) passes headless/GPU: 16 manual transfers / two poses, second denial/retreat/reset, physical timestamped keys and diagonal round trips, typed four-transfer route, stall/pause/focus and both-leaf reflected placement. Private capture inspected.

## 2026-09-30 explicit closing safety

[Optional closing sweep](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DOOR_CLOSING_SAFETY.md) holds an obstructed close without granting entry. Native lifecycle/event/failure/scene-retreat gates and independent 240 ticks / 52 holds pass; original unguarded 2,880-tick scene regression unchanged. Original obstruction policy remains unknown.

## 2026-09-30 original occupied close

[Original closing/host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_DOOR_CLOSING_SAFETY.md) independently matches 692 guarded/default ticks over two poses: 156 holds in guarded mode, 314 physical stops in unguarded replacement mode. Explicit `-HoldClosingDoors` headless/GPU denied-entry/retreat/release and existing timing/input/placement gates pass. Original game obstruction behavior remains unknown.

## Selected rooms in the persistent scene

Add `-SceneGeometry` to the pinned scene preview to render the route rooms. Interior view and floor overlay controls are independent; room geometry currently uses neutral materials and supplies no wall collision. See the [host contract](../docs/INTERIOR_SCENE_HOST_CONTRACT.md).

Add `-SceneTextures` with `-SceneGeometry` to use explicit MAIN-only inspection in the pinned scene. Specular/emissive effects and lighting remain untranslated; fallbacks are reported.

Add `-PortalVisibility` with `-SceneGeometry` for bounded aperture selection in interior view. Overview shows all selected rooms. See the [visibility contract](../docs/PORTAL_VISIBILITY_CONTRACT.md).

Continuous interior view now provides mouse look: Tab toggles, Escape releases; pause/focus loss/manual/overview release. W/A/S/D follow camera heading while pitch remains visual. See the [input contract](../docs/ACTOR_VIEW_INPUT_CONTRACT.md).

The pinned scene now shares one visual graph/material/GPU texture owner across rooms and both door instances. Door MAIN textures are included; leaf transforms/controllers and camera LOD remain independent.

Scene CPU loading now runs on an owned cancellable worker with a responsive loading panel. Godot resources remain on the main thread. `-CancelSceneLoad -Headless` runs the bounded pending-lookup cancellation test; see the [worker contract](../docs/BACKGROUND_ASSET_LOAD_CONTRACT.md).

## Continuous triangle-box collision foundation

The engine-neutral stationary-triangle query supports continuous world-axis box translation and owned geometry. Native tests plus an independent512-case synthetic LP oracle pass; see the [contract](../docs/TRIANGLE_BOX_SWEEP_CONTRACT.md). It is not yet connected to actor movement or original obstacle sources.

The triangle query also consumes precise piecewise feet paths, including slopes and vertical lift segments;512 independent two-segment LP cases pass. Actor response and original collision-source selection remain future work.

The same-cell triangle floor walker adopts validated supported contact and conservatively holds the source when a float landing cannot be represented. Flat/ramp/ceiling, repeat and retreat tests pass; step/portal integration remains open.

The bounded same-cell step walker now guards lift/landing and continuation against triangles. Ceiling blocking retains supported source feet; rise/drop, repeated hold/retreat and source/target walls pass synthetic tests.


### Original object-root skeletal source stages

Native optional probes now support `--skeletal-descriptors`, `--skeletal-skeletons`, `--skeletal-bind-pose`, `--skeletal-skin-sources` and `--skeletal-geometry`, with explicit object/SAT/configuration inputs. The [geometry contract](../docs/SKELETAL_MESH_GEOMETRY_CONTRACT.md) links independent original auditors and preceding descriptor/bind specifications. Current original human L0 union is twelve sources/329,296 bytes; character rendering/animation remains next.


Standalone `-Creature -CreatureAppearanceSha256 SAT_PIN` now draws the original object-root human bind geometry under [neutral inspection policy](../docs/NEUTRAL_CHARACTER_PREVIEW_CONTRACT.md). Headless/cancellation/GPU capture gates pass; animation, generated face textures and customization are pending.


[Interior character visual](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_VISUAL.md) passes77-source independent audit,2,880 original ticks, both-pose placement, cancellation and inspected GPU capture. Five neutral surfaces now follow persistent actor feet in the original interior. Binding/skinning/animation remain pending; metrics unchanged.


[Skeletal joint binding](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_JOINT_BINDING.md) resolves53 original attached joints/52 mesh names and independently verifies4,362 modern position diagnostics. Bind preservation error below4e-7; original consumer/normal/tangent semantics remain open. Metrics unchanged.


[Skeletal shader deformation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SHADER_DEFORMATION.md) passes unequal-map/raw-direction/negative fixtures and independently agrees on5,370 original shader vertex diagnostics. Complete PIDX weights deform NIDX normals and DOT3 tangents; generated animation remains open. Metrics unchanged.


[Weighted character pose preview](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_WEIGHTED_CHARACTER_POSE_PREVIEW.md) shows a visibly bent original forearm under explicit synthetic pose. Eight headless/GPU switches/reset, complete upload and cancellation gates pass; original animation playback remains open. Metrics unchanged.


[Logical animation tables](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LOGICAL_ANIMATION_TABLES.md) independently matches14 original sources/710,463 bytes and1,046 serialized entries. Four duplicated body names retained; authored/synthetic default and opaque typed subtrees validated. Selected clip/proxy execution remains next; metrics unchanged.


[Selected animation proxy](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SELECTED_ANIMATION_PROXY.md) independently matches15 original sources/726,134 bytes and one original ambient idle CKAT envelope. Owned proxy/scale and masked/wrong-pin gates pass; channel decoding/playback remain next. Metrics unchanged.


## 2026-10-01 compressed keyframe structure

[Compressed clip structure](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPRESSED_KEYFRAME_STRUCTURE.md) independently matches all selected original channel arrays and 2,284 keys through fifteen unchanged sources. Thirty-seven clip names match the body skeleton; `rifle` remains unresolved. Packed quaternion evaluation/playback remain next. Native truncation/negative/ownership gates pass; metrics unchanged.


## 2026-10-01 packed quaternion numerical decoding

[Packed quaternion decoder](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PACKED_QUATERNION_DECODE.md) independently agrees with executable-table decoding of all 2,225 selected original rotations (maximum error9.14e-8, zero selected repairs). All deployed-selector native gates and explicit overshoot diagnostic/repair pass. Interpolation, unmatched transform handling and playback remain next; metrics unchanged.


## 2026-10-01 compressed animation sampling

[Owned key sampler](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPRESSED_ANIMATION_SAMPLING.md) independently verifies380 original transform samples, including fractional endpoints/interiors and reverse access, maximum error1.16e-7. Native ownership/seconds/negative gates pass. No pose application/playback or metric change.


## 2026-10-01 sampled skeleton animation poses

[Instruction-backed local poses](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETON_ANIMATION_POSE.md) independently verifies265 attached joint poses and8,950 original shader vertices, maximum error1.34e-6. Unused `rifle` and missing `hold_l` are explicit; bind fallback is opt-in. GPU pose upload/timed playback remain next; metrics unchanged.


## 2026-10-01 rendered original animation pose

[Selected original idle frame](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_ANIMATION_POSE_PREVIEW.md) passes background ownership, prepared/native independent audit, headless/cancellation, full GPU pose readback and inspected frame30 capture.15 original sources remain unchanged. Frozen sampled pose works; timed playback and materials/customization remain next. Metrics unchanged.


## 2026-10-01 bounded original idle playback

[Original idle preview playback](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_ANIMATION_PLAYBACK.md) passes480 pose uploads/two loops, headless/GPU readbacks, pause/focus/seek and actual play/scrub control signals. Updated capture inspected;15-source closure unchanged. Full animation graphs/root motion/materials remain separate; metrics unchanged.


## 2026-10-01 animated original interior character

[Interior idle animation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_ANIMATION.md) passes80-source/10,340,219-byte independent audit, unchanged original route/geometry and two-owner animated host/GPU/cancellation gates. Inspected capture shows original idle pose inside original interior; actor feet/door state remain authoritative. Character materials/full animation graphs remain pending; metrics unchanged.


Original skeletal previews accept `-CreatureMaterialDefaults` with `-Creature` for authored CSHD MAIN texture-only inspection, including selected idle playback. See the [material contract](../docs/SKELETAL_MATERIAL_DEFAULTS_CONTRACT.md). Palette shading and generated face remain pending.

The original interior preview also accepts `-SceneCharacterMaterialDefaults` with `-SceneCharacter`, independently of selected idle animation. [Verified scene milestone](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_MATERIALS.md).

Explicit `-CreaturePaletteDefaults`/`-SceneCharacterPaletteDefaults` retain authored palette selections with their respective material-default flags. This is color-data inspection; preview shading remains texture-only.

The native `--skeletal-face-blueprints` probe follows original head TRTS into an owned BTRT live description. [Contract](../docs/BLUEPRINT_FACE_DESCRIPTION_CONTRACT.md); generated-face rendering is pending.

### Explicit generated human face preview

Use `-CreatureFaceProfile poc/preview-profiles/human-face-zero.json` with the existing pinned standalone creature launcher and `-CreatureMaterialDefaults`. This strict JSON profile supplies six exact name/Boolean values; it is an authored inspection profile, not recovered character-creation defaults. CPU preparation/composition remains engine-neutral and worker-owned. Generated MAIN binds through the original head MGN TRTS entry; GPU bytes and actual material texture identity are checked. Existing original idle/pose controls remain available. Combined palette/idle/face proof owns72 original sources; no original assets are shipped. See [composition contract](../docs/BLUEPRINT_FACE_COMPOSITION_CONTRACT.md). Body/eye palette shading, normal/specular effects and legacy driver bit parity remain pending.

`-CreatureAlbedoFactors` additionally requires `-CreaturePaletteDefaults` and applies validated authored skin/eye MAIN/HUEB factors after sampling. The generated-face/idle combination owns74 original sources. Full lighting is still unlit inspection. [Albedo factor contract](../docs/SKELETAL_ALBEDO_FACTOR_CONTRACT.md) records exact scope and display policy.


### Interior generated face and albedo

The pinned animated interior proof accepts `-SceneCharacterFaceProfile` with material defaults and `-SceneCharacterAlbedoFactors` with palette defaults. CPU composition stays in the shared owned worker/cache; generated textures/materials retain identity through animation and room transfer. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_GENERATED_CHARACTER.md).


### Shared character color/style selection

Use `-CreatureCustomizationProfile` or `-SceneCharacterCustomizationProfile` with albedo/palette/material options to select one exact shared color/style snapshot. [Contract and reproduction](../docs/SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md). Profiles are authored preview selections; texture-choice/shape updates and interactive revisions remain separate.


### Live color/style edits

Shared-profile previews expose color/style inputs and reset. CPU revisions coalesce and cancel outdated work; GPU publication retains current meshes/materials/animation and releases previous generated face textures. [Contract and evidence](../docs/LIVE_SKELETAL_CUSTOMIZATION_CONTRACT.md).


Optional `-CreatureShapeProfile poc/preview-profiles/human-shapes-zero.json` adds explicit shared-owner shape controls to the pinned standalone preview. The authored zero selection is not an original default claim. See [live shape boundary](../docs/LIVE_SKELETAL_SHAPE_CONTRACT.md).


`-SceneCharacterShapeProfile poc/preview-profiles/human-shapes-varied.json` applies an explicit authored shape selection to the pinned interior character and exposes live shape controls. Authoritative actor collision/feet remain unchanged. [Interior shape boundary](../docs/LIVE_SKELETAL_SHAPE_CONTRACT.md#interior-integration).

### Original-profile third-person interior camera

The pinned full-character scene accepts `-SceneCameraProfileSha256` with `-SceneTriangleGuard`. The profile joins the original owned source closure; authored zoom controls and an explicit sphere boom retract before room/door geometry. Camera-cell tracking drives portal visibility independently of actor ownership, with all-rooms fallback when unresolved. [Camera contract](../docs/INTERIOR_CHASE_CAMERA_CONTRACT.md) and [cell tracking](../docs/CAMERA_CELL_TRACKING_CONTRACT.md). Source triangle indices now remain unchanged after Z reflection; [facing correction](../docs/GODOT_GEOMETRY_FACING_CONTRACT.md).

### Selected original cell/material lighting

With the pinned full-character scene, `-SceneCellLighting` uses the recovered cell slot partition, original light-scaling default and positive point attenuation. `-SceneRoomLighting` applies baked-room material families; `-SceneDoorLighting` applies the selected normal/specular door family and shared vertex stage. The older `-SceneLegacyLightSelection` switch remains a compatible alias. These are offline bounded previews with explicit ambient, singular-light and basis policies. See [light selection](../docs/LEGACY_LIGHT_SELECTION_CONTRACT.md), [point attenuation](../docs/LEGACY_POINT_ATTENUATION_CONTRACT.md) and [shared vertex stage](../docs/TANGENT_LIGHTING_VARYING_CONTRACT.md).

### Point-color GPU diagnostic

`-PointColorGpuProbe` adds an opt-in combined point-light/normal/tangent/hue/mask fixture to a noninteractive GPU preview with `-CreatureSpecular` or `-SceneCharacterSpecular`. It is rejected with headless or interactive mode. Ordinary previews omit this additional diagnostic; its48 corrected cases pass. [Evidence and next probe](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_POINT_COLOR_GPU_PROBE.md).
