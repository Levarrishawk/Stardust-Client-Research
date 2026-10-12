# Client implementation acceptance record

## October 12 original Mos Taike outdoor district

The [Mos Taike entry](DX12_OFFLINE_MOS_TAIKE_RUNBOOK.md) combines original snapshot placement, a bounded 160 × 160 m height-rule traversal, five buildings and 89 other objects, animated human movement and conservative modern obstacle/camera handling. Independent raw-byte placement, displayed-triangle grounding, five original foundation targets and all22 default regressions pass. Actual DX12 captures show recognizable original facades, stalls and streets with a readable HUD. Nine omissions, modern material/numeric policies and closed interiors are explicit; this is not live interoperability or full-town/world parity. [Exported checks and evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-12_DX12_OFFLINE_MOS_TAIKE.md).

## October 12 exported x64 DirectX 12 terrain movement

The [separate terrain build](DX12_OFFLINE_TERRAIN_RUNBOOK.md) joins original human idle/walk/run with a 256 × 256 m original-resource base/detail terrain surface. Eight exported checks pass: original CPU loading/seams/controller, cancellation, headless refusal, invalid option/profile, actual OpenGL refusal, actual DX12 movement and independent triangle/distance checks. Visual review required lower lighting, filmic tone mapping and a readable HUD before acceptance. This is a partial Tatooine generator, not complete geographic parity or live gameplay. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-12_DX12_OFFLINE_TERRAIN.md).

## October 12 exported x64 DirectX 12 character

The [runnable build](DX12_OFFLINE_CLIENT_RUNBOOK.md) supplies an offline no-argument original-character entry over the existing loaders and animation host, with a self-contained Windows AMD64 export and runtime D3D12 admission. Eight exported positive/negative cases pass, including original material GPU checks and advancing animation; all 22 existing default core regressions pass on this workstation. Independent lead replay verifies character-region image changes and exact delivery hashes. [Evidence and limits](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-12_DX12_OFFLINE_CLIENT.md). Full terrain, generated facial texture, equipment and live interoperability are not established by this milestone; the Round93 session gates below remain separate.

## Current end-to-end gates (Round93)

Round93 adds an explicit controlled-transport entry over the reusable installation/application/Godot host and a bounded redacted attempt receipt written after actual local drain or startup rollback. Default startup still refuses contact. Offline native headless/GPU replay verifies the same strict admission/composition with mandatory synthetic spy ports, protected exchanges, one approved original human, bounded movement, deadlines and disconnect. Run, cleanup, rollback and diagnostic failures remain distinct; standalone quit follows ownership completion. The real UDP constructor branch is compiled and source-reviewed, never executed.

Next: obtain operator-approved deployment/account access, explicit advertised hostname-to-IPv4 mappings/ports and client version, one existing supported outdoor human with measured CREO3/4/6 limits and original resource identity, and a bounded minimum-readiness scenario. Keep movement disabled unless fresh stamp/ready state, matching validated position and passable floor are established. Then request specific authorization for one bounded controlled attempt. No original executable, real socket, DNS/server contact or live interoperability has been verified. General avatar/customization, nearby actors, full terrain and server-observed authority/release remain open.

[Launch profile](REPLACEMENT_LAUNCH_PROFILE_CONTRACT.md), [run contract](REPLACEMENT_CLIENT_RUN_CONTRACT.md), [server movement prerequisites](REPLACEMENT_SERVER_MOVEMENT_ADMISSION.md) and [consolidated reproduction](CLIENT_REGRESSION_RUNBOOK.md) define the accepted boundaries. Asynchronous I/O failures can fault drain; successful final send followed by failed disposal remains a separate cleanup failure. The joined synchronous send error and cancellable timeout report typed failure and successful local drain.

## Verified boundaries

| Boundary | Implemented behavior | Evidence / practical limit |
|---|---|---|
| Received-only CREO4 scalars | Immutable presence/provenance and ordered assignment events; unknown fields stay absent | [Contract](CREATURE_VIEW4_PARTIAL_STATE_CONTRACT.md);115 cases/528 messages/12516 events independently agree;43-check literal observer join passes. [Store](SCENE_CREATURE_VIEW4_PARTIAL_STORE_CONTRACT.md), [world/router](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_CONTRACT.md) and [received host](SCENE_WORLD_RECEIVED_MOVEMENT_PREVIEW_CONTRACT.md) now pass under the explicit partial policy. Original defaults/merging remain open. |
| Mouth GPU/interior preview | Separate vertex-lit shader with explicit diagnostic vertex lighting | [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_VERTEX_LIT_SPECULAR_PREVIEW.md);80/145-source unions,eight mouth/twelve normal GPU cases,retained controls/idle/route and headless/cancellation pass; exact runtime LIT unknown |
| Vertex-lit mouth pixel boundary | Owned nodot3 PS20 and supplied vertex-light evaluator | [Contract](SKELETAL_VERTEX_LIT_SPECULAR_CONTRACT.md);80-source/64-case independent compiled audit and native/failure gates pass; runtime vertex lighting/GPU pending |
| Specular GPU/interior preview | Recovered normal-material specular with explicit diagnostic lighting | [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SPECULAR_PREVIEW.md);12 filtered fixtures max0.002764, original standalone/interior retained resources/idle/color/shape/age/route/headless pass; mouth/full lighting pending |
| Selected material/specular equations | Immutable MATL colors/constants and supplied-light PS20 evaluator | [Contract](SKELETAL_SPECULAR_CONTRACT.md); four records/192 cases independently agree with original compiled instructions; GPU/full light selection pending |
| Interior normal/detail integration | Shared144-source closure, same live controls and shading | [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_NORMAL_DIFFUSE.md); independent source/normal/shape/floor/native route/GPU actor/door/idle/headless/cancellation gates pass |
| Normal diffuse/live age preview | Reflected tangent upload, diagnostic diffuse and four owned age choices | [Contract](SKELETAL_NORMAL_DIFFUSE_PREVIEW_CONTRACT.md);79-source independent union/GPU handedness/color interaction/idle/shape/reset/failure/headless/cancellation pass; original full lighting unimplemented |
| x64 engine-neutral core | .NET 8 library; no Godot references | Core smoke suite checks a 64-bit process |
| Session negotiation | Exact raw request/response; echoed ID, selectors, CRC width, wire size | Fixed fixtures; negotiated size bounded by request and 65,507-byte local ceiling |
| Established framing | Seeded CRC, selector-4, uncompressed send, bounded zlib receive | Independent Python vectors; compressed test streams use current .NET zlib |
| Corruption recovery | Reject framing without advancing sequence, emitting ACK, or failing login | Independent peer sends damaged token, then retransmits valid sequence 0 |
| Reliable channels | Four channels, cumulative/selective ACK, reorder, retransmission, wrap | Golden controls; existing 65,537-send wrap exercise; selective-hole span test |
| Queued fragmentation | One owned pending message per channel, maximum 1,500,000 bytes; pump through successive windows | Independent 1.5 MB reconstruction and 32 seeded impairment transfers |
| Multipayload | 1/3/7-byte lengths; expansion after fragment assembly | Independent 608-byte fragmented multipayload; 64-message limit |
| Outer `00 03` | One-byte block lengths, nested bounded decoding, all generated controls surfaced | Mixed ACK/ping/login transcript; malformed lengths rejected before dispatch |
| Ping/disconnect | Ignore protected ping; verify disconnect's echoed ID and close without replying | Synthetic independent frames; server construction chain documented in checkpoint |
| Status observations | CRC-validated `00 07` lengths 4/40 and `00 08` length 40; typed tick and unnamed wire words | Independent short-request frame; strict lengths, corruption, outer validation and close cleanup; no automatic reply |
| Extended status reply | Explicit 40-byte request reply, echoed peer counters, caller pre-send snapshot, normal protected unsequenced send | Direct construction/accounting evidence and independent Python wire fixture; short requests rejected by builder |
| Login lifecycle | Token/list/status/characters complete; fatal error, malformed application, deadline, disposal clear owned state | Independent reorder/loss transcript and direct allocation-zeroing checks |
| Configuration | Strict bounded UTF-8 JSON; unknown/duplicate fields and credential fields rejected | Positive schema, malformed/type/limit cases; no endpoint resolution |
| Diagnostics | Allowlisted kind/state/outcome/count JSON; sink exceptions counted and isolated | Synthetic private values absent from journal; no raw error text or payload logging |
| Original archive access | TREE 0005 stored/zlib blocks; filename CRC; priority and tombstones | Synthetic fixtures plus 1,477-entry installed archive index and one Python-matched extraction |
| Host accounting/clock | I/O attempts, failed sends, pinned receives, foreign drops, pre-send snapshots; guarded monotonic clock | Deterministic boundaries, equal/backwards time, low-32 wrap; generation is not sending |
| Async login host | One pending receive across ticks, pinned IPv4/port, cancellation, retries, owned result and failure cleanup | Injected I/O tests; UDP adapter compiles but has not been instantiated or executed |
| Persisted journal | Allowlisted bounded JSONL, whole-record limits, no-overwrite files, partial-I/O stop | Synthetic secrets absent; actual temporary file readback and failure fixtures |
| Static mesh geometry | Owned unpadded IFF; MESH/0004-0005, SPS/0001, primitive/0000-0001, VTXA/0003 declaration 1105 | One original mesh: 9 batches, 190 vertices, 159 triangles; independent Python geometry hash |
| Configured mounts/references | Explicit TREE/0005 mount owner; raw index keys and normalized requests; bounded SSHT/TXM external references | All48 configured archives open. Later accepted [DDS decoding](DDS_DECODER_CONTRACT.md), material and original-resource GPU proofs extend this early reference check; full original runtime lighting remains open |
| Godot hosting | Injected login and original-resource skeletal/interior/terrain presentation, input/motion and persistent offline scene ownership | Earlier native headless/GPU proofs include [Round82](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_82_2026-10-06.md); physical OS input and integrated live session remain unverified |

## Ownership and caller contract

- Protocol/session/coordinator state has one owning caller; these types are not
  a concurrent networking service. `TreArchive` serializes stream access;
  `TreOverlay` mount mutation remains caller-owned.
- Supply nonnegative monotonic milliseconds to scheduler/coordinator methods.
  Poll while waiting for responses; the configurable response deadline defaults
  to 30 seconds and does not reset after a partial reply. This is replacement
  policy, not a recovered original-client timing constant.
- Send **every** result in `ControlDatagrams`. `ControlDatagram` is a retained
  convenience for older single-packet consumers and returns only the first.
- `QueueReliableMessage` copies input. Call `PumpQueuedDatagrams` after ACK
  processing and on ticks, and request due retransmissions separately. A pending
  message prevents interleaving another message on that channel. The older
  `CreateReliableDatagrams` API remains an atomic, window-sized convenience.
- `Close` clears retained send/reorder/fragment/queued buffers. ACK release
  clears retained plaintext; login failure clears identity, enumeration and
  credential state while preserving the typed fatal error for the caller.
- Returned wire buffers, copied credentials, input strings, and caller-owned
  archive payloads belong to their recipient. Clearing internal byte buffers
  does not erase immutable caller strings or all runtime/OS copies.
- `TreArchive` owns its stream unless `leaveOpen` is true. Overlays borrow
  mounted archives; dispose their owners after use. Stored records use `usize`,
  including `csize=0`; only `usize=0` masks a resource. Logical paths are never
  translated into host filesystem paths by this reader.
- `ConfiguredTreOverlay` owns its mounted archives; it borrows the supplied
  configuration reader. It supports one explicit numbered TREE set only.
  Index CRCs validate raw names; normalized requests do not make noncanonical
  raw index names reachable. `Entries` retains those raw names.
- `LoginDatagramHost` owns one run and its datagram port. Await disposal to cancel
  and drain pending work; injected ports must honor cancellation/disposal. A
  successful `LoginConnectionResult` owns a credential clone; dispose the result
  after use. The host closes login transport even on success; it does not enter zone.
- `BoundedLoginJournal` defaults to 256 KiB / 4,096 records. Supply an empty
  writable stream or explicitly create a new file; seekable streams are checked
  for emptiness, while nonseekable emptiness is the caller's contract. Existing files are
  never replaced. A partial I/O failure stops further records and marks uncertain
  tail. Persistence is synchronous, so a slow sink can delay the host.

## Configuration schema

The following example is synthetic and is not a live endpoint:

```json
{
  "host": "peer.invalid",
  "port": 44453,
  "clientVersion": "20260817-SD:01",
  "maximumDatagramSize": 496,
  "sessionRetryMilliseconds": 1000,
  "sessionTimeoutMilliseconds": 15000,
  "reliableRetransmitMilliseconds": 2000,
  "loginResponseTimeoutMilliseconds": 30000
}
```

`host`, `port`, and `clientVersion` are required; the remaining values default
as shown. Keys are case-sensitive. The input limit is 8,192 bytes. There is no
credential key, environment lookup, file discovery, DNS resolution, or socket
side effect. A future host supplies the selected stream explicitly.

## Open acceptance gates

1. **SOE status/liveness:** bounded raw observations are implemented; direct client
   instructions confirm the normal CRC gate for standalone status responses.
   Detailed field meanings, operational server zero-trailer mismatch,
   mapping/liveness controls, and adaptive retry policy are
   still outside the current transport. Outer multipackets accept the supported
   data/fragment/control/ping/disconnect/status leaves; unknown leaves fail explicitly.
2. **Operational networking:** the cancellable host, source filtering, clock,
   counters and persisted journal are implemented with injected-I/O tests. The
   IPv4 UDP adapter is compiled only; socket execution, endpoint resolution,
   live interoperability and zone handoff remain unverified. No socket or
   live-server run was performed.
3. **Compressed compatibility:** byte-exact historical zlib output and its
   malformed-stream behavior remain unverified. Inflation is currently capped
   by the negotiated datagram budget; larger decoded forms need evidence.
   Header/Adler validation is explicit; preset zlib dictionaries are unsupported.
4. **Archive breadth:** TREE 0004/0006, TOC, loose-file nodes, equal-priority
   ties, locale/non-ASCII folding, MD5 verification, and full payload-corpus
   validation remain open. All 48 current configured indices mount successfully.
   Defaults cap index blocks at 64 MiB, records at one
   million, and payloads at 256 MiB. These are explicit local limits.
5. **Presentation:** one original mesh section renders with verified DDS pixels,
   owned material graph and GPU coordinate/UV tests. Other mesh layouts, faithful
   lighting, parent appearance/LOD, animation and world rendering remain open.
   Default offline login retains its synthetic scene; original preview is explicit.

## Reproduction and resume

Run from the repository root:

```powershell
dotnet run --project .\poc\Stardust.Core.SmokeTests\Stardust.Core.SmokeTests.csproj
python .\tools\verify_transport_vectors.py
python .\tools\python\verify_login_zone_fixtures.py
python .\tools\python\validate_organized_workspace.py .
git diff --check
```

The optional archive probe and exact source identities are in the
[TRE checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_TRE_READER.md).
See the [transport checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_TRANSPORT_RECOVERY.md)
and [session resume point](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_HOST_AND_ASSET_HOUR.md).

Next transport action: select explicit short-status/missing-counter policy before
automatic liveness; real I/O remains separately authorized. Next asset action:
load the parent appearance and two verified LOD levels. Modern DDS next: BC7
with independent mode fixtures; BC6H requires an HDR contract.

### Textured preview gates (2026-09-29)

| Gate | Verified scope |
|---|---|
| DDS pixels | Original BC1/2/3, typed DX10 BC1–5 and uncompressed formats; malformed fixtures |
| Modern reference fixtures | 16 independent DX10/ATI fixtures match native bytes |
| Material graph | Owned candidate/pass/parameter structure and dependency failure cases |
| Original GPU upload | 9 batches, 190 vertices, 159 triangles, 9 textures and 80 matching mips |
| GPU coordinates | Asymmetric UV corners and reflected front face with culling |

See the [session](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_TEXTURED_PREVIEW_HOUR.md). BC7/BC6H remain unsupported and preview lighting is unlit.

### DTLA parser gate

Owned DTLA/0005–0007 grammar, ID mapping, visibility geometry and malformed/ownership fixtures pass. Four original L0–L3 geometry hashes independently match, including declaration 0x55405 auxiliary coordinates. Runtime selection and richer L0/L1 materials are not yet accepted. See the [DTLA checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_DTLA_READER.md).

### Authored LOD inspector gate

Distance-only selection, boundary ties and availability negatives pass. All four original children upload and switch in headless and GPU tests. L0/L1 richer bundles each match 134 reference mip levels. The [inspector checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_GODOT_LOD_INSPECTOR.md) explicitly excludes PIVT origin, lazy game instance behavior, full lighting and appearance composition.

### APT and exterior gate

Strict APT resolution, explicit empty/missing/masked default and broken-target negatives pass. The exterior APT resolves three original meshes with independently matched geometry; L0's 99 mip levels match reference pixels. Explicit MAIN texture-only GPU inspection passes; default authored-fallback mode rejects the richer material. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_APT_EXTERIOR_CHAIN.md).

### BC7 extension gate

DX10 BC7 UNORM/SRGB now passes standard synthetic modes 0–7, partitions/rotations, 147 valid reference mip comparisons and one normative reserved-output case. A real Godot nine-mip upload/readback passes. See the [BC7 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_BC7_DECODER.md). HDR, typeless formats and original-client BC7 acceptance remain outside this gate.

## Component geometry gate

All 165 deployed CMPA templates and 779 transforms independently match. Ownership, composition order, cycle diagnosis and fatal limits pass. Four vent-pillar LODs independently match and pass headless/GPU readback and switches; one unsupported particle is counted. See [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_COMPONENT_GEOMETRY.md). Nested garage meshes and full runtime presentation remain open.

## Sorted-index and nested assembly gate

Native versioned SIDX grammar/selection negatives and auxiliary-coordinate fixtures pass. All fourteen garage source identities, nine geometry hashes and 400 variant hashes independently match. Neutral headless/GPU readback and LOD switches pass with zero omissions. See [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SORTED_MESH_GARAGE.md). Material rendering and automatic camera distance remain separate gates.

## Shared material graph gate

Core sharing/owned geometry/failure/budget fixtures pass. Fifty garage source identities, eleven shaders/eight effects and 151 texture mips match independent reading. Nine Godot templates use seventeen shared texture uploads with 35 textured/ten neutral surfaces, passing readback and LOD checks. See [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_COMPONENT_MATERIALS.md). This is opt-in unlit inspection, not original visual equivalence.


## APPR/PIVT camera selection increment — 2026-09-30

The [camera LOD checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_PIVT_CAMERA_LOD.md) closes the primary extent distance origin: false/absent pivot uses the transformed authored sphere center, true uses world translation, and NULL uses zero local offset. All 6,198 original primary extents agree independently. Native malformed/transform fixtures and standalone/composed headless/GPU selection gates pass. The inspector supports opt-in camera distance and manual selection. Original DPVS, lazy residency and full lighting remain deferred. Five confirmed address labels bring the canonical registry to 2,590 distinct entries; research scores and packet counts are unchanged.

Canonical contract: [extent and distance](APPEARANCE_EXTENT_DISTANCE_CONTRACT.md).


## BC6H HDR extension — 2026-09-30

The [HDR checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md) adds a separate owned RGB half output for typed signed/unsigned BC6H. Forty standards-only fixtures/264 mips match independent Godot bcdec bit for bit and pass real GPU half upload/readback. Native endpoint, truncation, surface and budget gates pass. Static material integration and HDR presentation remain separate; this is not original executable acceptance evidence. Existing pinned dependencies, research metrics and registry counts are unchanged.


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

## 2026-09-30 portal transfer follow-up

The [bounded transfer contract](PORTAL_FLOOR_TRANSFER_CONTRACT.md) adds one owner transfer and remaining motion with current offline entry/barrier policy. Floor portal IDs index the owning cell reference vector; they are distinct from global geometry/PGRF keys. The prior tentative 0126AA80 owner-transfer interpretation is corrected: it preserves the same floor wrapper; 012670E0 / 00B2B3F0 perform cell continuation. Fourteen original directed links, 56 independent probes and 28 Godot adapter probes pass. Full actor collision, door lifecycle and exterior continuation remain open.

## 2026-09-30 permission barrier follow-up

The [barrier contract](PORTAL_BARRIER_COLLISION_CONTRACT.md) adds normal-controller primitives and bounded source-cell actor-box contact before transfer. Runtime fields are LeadingByte exclusion, SolidByte support and separate WindingByte; B43E80 inversion is specific to PRTO/0002. Current permission, mutable portal state and physical door-leaf clearance remain distinct. Independent 291-layout / 21,693-probe agreement and 12-link Godot actor contact gates pass; nonlevel requests stop explicitly. Physical leaf collision and global actor solver ordering remain next.


## 2026-09-30 single normal leaf follow-up

The [door contract](SINGLE_LEAF_DOOR_CONTRACT.md) supplies strict original style decoding, contact hysteresis, recovered displacement curve and conservative authored-box clearance. All 124 original rows and one 100-frame scenario match independent calculations; synthetic negative/cleanup gates and headless/GPU controls pass. This is standalone: original POB door instance wiring, narrow-phase shape fidelity, paired leaves and effect playback remain open.


## 2026-09-30 original interior door follow-up

The [movement contract](INTERIOR_DOOR_MOVEMENT_CONTRACT.md) connects a selected named original POB instance to physical actor clearance and one owner transfer. Twenty-four independent cases cover both references, two owner poses and six states. Native failure/cleanup gates, headless/GPU controls and existing YT-1300 regression checks pass. Fixed inspection crossing replays are complete; persistent locomotion, paired-controller fidelity and full original solver ordering remain open.


## 2026-09-30 persistent actor follow-up

The [actor contract](PERSISTENT_DOOR_ACTOR_CONTRACT.md) closes repeated owned actor steps across the original doorway. Independent 948-tick traces, synthetic lifetime/error gates and headless/GPU persistent controls pass. Continuous input timing, general world collision and legacy paired-controller fidelity remain open.

## Fixed-step doorway actor gate — 2026-09-30

The [timing gate](FIXED_STEP_ACTOR_CONTRACT.md) passes three render partitions, two owner poses, independent original trajectory, stall/suspension/failure gates and Godot physical-key/pause/focus adapter checks. Input snapshots require equal fixed-tick sequences; timestamped transitions and general locomotion remain open.

## Timestamped actor input gate — 2026-09-30

The [timeline gate](ACTOR_INPUT_TIMELINE_CONTRACT.md) passes between-render changes under three partitions, independent original trajectories, capacity/late-history rejection and suspension/drop behavior. Godot event routing and GPU checks pass; general planar locomotion remains next.

## Planar doorway movement gate — 2026-09-30

The [planar gate](PLANAR_DOORWAY_ACTOR_CONTRACT.md) passes original lateral/diagonal trajectories, transformed-owner round trips, normalized speed and synthetic floor stops. Headless/GPU W/A/S/D checks pass. General actor/world collision and moving-leaf response remain separate.

## Rigid detail-door asset gate — 2026-09-30

The [capability gate](DOOR_STYLE_CAPABILITY_MATRIX.md) passes 124-row / 291-layout reconciliation and 38 newly accepted detail leaves with 3,556 all-band envelope checks. Native/headless/GPU fixed-root/LOD controls pass. Style occurrence is distinct from usable paired-floor instance coverage.

## Detail-door instance gate — 2026-09-30

The [instance gate](DETAIL_DOOR_INSTANCE_READINESS.md) records bounded candidate screening and an independently verified upward Mustafar primary: 1,896 ticks / 88 transfers. Host gates pass; matching authored references remain distinct from usable floor instances. Six rounded floor contacts remain a concrete follow-up risk.

## Returned floor-locator gate — 2026-09-30

The [stability gate](FLOOR_CONTACT_STABILITY_CONTRACT.md) preserves 17,658 independent floor outcomes and verifies every returned corpus locator is reusable. Five thousand seeded correction/precision-hold/retreat/rejection cases pass; all six former actor InvalidStart cases disappear. Sloped door motion remains separate.

## Piecewise floor-path gate — 2026-09-30

The [trace gate](FLOOR_PATH_TRACE_CONTRACT.md) preserves 17,658 installed walk results and checks 22,063 segments against independent authored planes. Crease, reverse, stationary, invalid and traversal-prefix cases pass. Collision integration remains separate.


## Piecewise actor-box gate — 2026-09-30

The [box sweep gate](FLOOR_PATH_BOX_SWEEP_CONTRACT.md) passes 6,000 independently reconstructed synthetic queries and crease/chord disagreement plus negative/resource tests. Contact landing and portal slope integration remain separate.


## Single-cell box-contact locator gate — 2026-09-30

The [landing gate](SINGLE_CELL_FLOOR_BOX_CONTRACT.md) passes 6,000 independent synthetic contact/locator results plus reuse, retreat, tie ordering, callback failure and explicit precision holds. Portal integration remains separate.


## Original sloped-floor box and level-guard gates — 2026-09-30

The [floor-box gate](SINGLE_CELL_FLOOR_BOX_CONTRACT.md) passes 600 original-plane fixtures with synthetic boxes and 12,000 zero-drift repeated queries. All locators are reusable; 530 retreat and 70 genuine overlaps remain blocked. The [portal guard](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LEVEL_PATH_GUARD.md) rejects equal-endpoint source/target ridges; original actor/host and candidate-count regressions pass.

## Precise portal floor-box gate — 2026-09-30 evening

The [query gate](PORTAL_FLOOR_BOX_CONTRACT.md) passes 2,400 independent synthetic requests plus ownership, asymmetric-frame, snapshot, failure and precision rollback tests. Door integration remains separate; the existing level-only adapter is still default.

## Precise sloped persistent door gate

[Bounded native acceptance](PRECISE_DOOR_ACTOR_CONTRACT.md): snapshot-before-callback and directional permission gates pass; original YT-2400 passes 1,896 independent ticks / 88 transfers / 53 blocked ticks. The installed screen improves from 131 to 495 exact short round trips on the same 782 candidates. Default host path remains level-only pending explicit Godot verification.

## Precise sloped Godot gate

[Host milestone](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PRECISE_DOOR_GODOT.md) passes pinned YT-2400 forward headless and reverse GPU controls, including scheduling/input/reflection. Build has zero warnings/errors; private capture inspected. Explicit -PreciseDoorWalk opts in; default stays level-only.

## Thin portal target acceptance

[Target bridge gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LANDING_NORMAL.md): 1,000 synthetic slanted landings and pinned Yavin 948-tick independent reference pass. Existing source/world and local correction budgets preserved. Same precise screen now has 496 round trips and no LandingFailed.

## Riser proposal gate

[Bounded proposal](FLOOR_STEP_PROPOSAL_CONTRACT.md): synthetic rise/drop and rejection gates pass; nine original proposals independently verified among 19 audited vertical transitions. Ten explicit holds. Actor clearance/adoption is not yet accepted.

## One-step box gate

[Same-space clearance contract](FLOOR_STEP_BOX_CONTRACT.md) passes low-ceiling rollback, zero-drift holds, retreat, target contact and policy/resource guards. Nine original floors pass 36 movement/blocker cases and 360 repeated transitions. Portal/door integration remains next.

## World step/portal gate

[Contract](PORTAL_FLOOR_STEP_CONTRACT.md): native rollback/snapshot/permission/transfer-limit guards pass. Original small-house both directions/two poses match 1,896 ticks / 88 transfers / 88 steps / 58 blocked. Step-enabled screen passes 504 short round trips; explicit host exposure remains next.

## Step doorway Godot gate

[Host checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_STEP_DOOR_GODOT.md): pinned small-house headless/GPU controls, scheduling/input and placement pass. 20 repeated manual transfers / 23 step events; private capture inspected; zero-warning build.

## Local step coverage gate

[Explicit local option](FLOOR_STEP_RAY_CONTRACT.md): side-wall/hole negatives, 16 independently checked original proposals, 64 movement/blocker cases and 640 repeat step transitions pass. Portal actor screening and host exposure remain next; whole-strip default stays unchanged.

## Local-step original doorway gate

[Black-sun actor checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_ACTOR.md) passes both directions/two poses, independent per-tick status/owner/step/transfer checks. Explicit local screen yields 509 round trips; host local-option exposure remains next.

## 2026-09-30 local-step host gate

[Black-sun host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_GODOT.md) passes forward headless/reverse GPU controls and placement with explicit local coverage; 20 manual transfers per direction, 23/25 manual step events. Native actor oracle counts remain separate.

## 2026-09-30 local-step host gate

[Black-sun host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_GODOT.md) passes forward headless/reverse GPU controls and placement with explicit local coverage; 20 manual transfers per direction, 23/25 manual step events. Native actor oracle counts remain separate.

## 2026-09-30 connected approach

Selected-edge approach gate passes stacked-sheet and negative routes, 1,056 independent original endpoints and lower black-sun repeated trace. Connected screen 524/528 is separate from vertical inspection 509/533. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_APPROACH.md).

## 2026-09-30 connected host gate

[Lower-floor host](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_GODOT.md) passes forward headless/reverse GPU controls, scheduling/input and reflected placement; 20 manual transfers each, 48/50 manual step events. Explicit `-ConnectedDoorApproach` requires local, step and precise movement.

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

## 2026-09-30 selected scene rooms

[Selected appearance and host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md) passes native selection negatives, independent geometry identity and original headless/GPU room-owner placement. View switches preserve state; room collision remains unimplemented.

## 2026-09-30 selected room textures

[Selected material gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_TEXTURES.md) passes later inline implementation asymmetric/truncation tests, selected shared loading, independent DDS/effect comparisons and original headless/GPU host controls/upload readback. MAIN rendering remains an inspection policy.

## 2026-09-30 zero-pass surfaces

[Decoded no-pass gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ZERO_PASS_SURFACES.md) passes native classification and original mapped upload plus synthetic all-surface omission in headless/GPU hosts. Rendering omission leaves decoded collision geometry intact.

## 2026-09-30 portal visibility

[Visibility query/host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_VISIBILITY.md) passes native clipping/negative/fail-open checks, independent representative ray samples and original node visibility in both owner poses. Legacy DPVS and exterior traversal remain separate.

## 2026-09-30 actor camera heading

[View/input gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ACTOR_VIEW_INPUT.md) passes horizontal heading and timestamp semantics plus original rendered/headless motion events, camera placement, portal selection and focus/pause release in both poses. Pitch remains presentation-only.

## 2026-09-30 combined scene content

[Content gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_CONTENT.md) independently reconciles exact source union and preserves original trace; source budget and wrong-layout substitution negatives plus host regressions pass.

## 2026-09-30 shared scene visuals

[Sharing gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_SCENE_VISUALS.md) passes independent original leaf geometry/mips/source union, decoded sharing/budgets, headless/GPU identical mesh resources with independent leaf nodes, and unchanged original movement trace.

## 2026-09-30 background scene load

[Worker/host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BACKGROUND_SCENE_LOAD.md) passes cooperative cancellation, stale-owner rejection and original success/cancelled publication boundaries. Archives close on the worker; GPU and scene creation stay on the main thread.

## 2026-09-30 continuous triangle-box query

[Contact gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_BOX_SWEEP.md) passes native analytic/negative cases and512 independent synthetic LP contact-time comparisons. Movement integration and original obstacle selection remain outside this gate.

## 2026-09-30 triangle contact along precise paths

[Path gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_PATH.md) passes double-coordinate/sloped/vertical lift cases and512 independent LP segment/global-time comparisons. Contact query does not adopt an actor locator.

## 2026-09-30 supported triangle floor contact

[Adoption gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_WALK.md) passes flat/ramp/ceiling supported locators,100 repeat holds, retreat, precision rollback and invalid/prefix recovery. Original scene collision is not integrated.

## 2026-09-30 triangle step clearance

[Step gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_STEP.md) passes rise/drop, ceiling source hold,100 repeats/retreat, source/target wall contact and height limits. Original host obstacle integration remains separate.

## 2026-09-30 original obstacle-source constraint

[Helper geometry audit](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_HELPER_GEOMETRY.md) verifies existing source identities and aggregate geometry; its route-disjoint result prevents claiming full scene collision from hidden surfaces alone.


## October 1 original-profile sphere milestone

The [profile preview](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_PROFILE_PREVIEW.md) adds bounded original object-template dimensions to the owned scene/worker pipeline:70 original sources/9,622,375 bytes, one external lookup each;2,880 native ticks/24 transfers/182 contacts; original headless/GPU route and occupied closing gates. Source/frame audit is independent; dynamic controllers remain modern. Character skeletal rendering, sphere steps/sliding and original mesh participation are not claimed. Research/function/protocol measures are unchanged.


## 2026-10-01 combined scene and skeletal source stages

[Combined modern sphere guard](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMBINED_SPHERE_SCENE.md) passes original source/geometry/frame audit and headless host; local complete tick mean0.702 ms. Explicit original mesh participation remains open.

[Object-root skeletal geometry](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_MGN_GEOMETRY.md) resolves twelve unchanged sources once, retaining1,790 shader vertices/2,810 triangles plus generated-face binding. SAT/MLOD/SKTM/SLOD and176 original bind matrices also agree independently. Full native syntax/ownership/negative gates pass; earlier bind-stage Godot build passes. No rendered/animated character claim.


[Neutral character inspection](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_NEUTRAL_CHARACTER_PREVIEW.md) now passes headless/cancellation/GPU upload and inspected capture through the original object-root chain. Five surfaces/2,810 triangles use neutral materials; character animation and texture/customization remain pending.


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


## 2026-10-01 shared skeletal deformation batch

[Shared position batching](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_DEFORMATION_BATCH.md) passes native, independent original geometry/animation and480-update GPU gates. Positions evaluate once per mesh; original shader direction semantics remain unchanged. Research metrics unchanged.


## 2026-10-01 original skeletal material defaults

[Original character texture defaults](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_MATERIAL_DEFAULTS.md) pass32-source/4,668,368-byte independent audit, four CSHD materials/ten textures/80 mip agreement, native rejection gates, headless/cancel/GPU animation and inspected capture. Explicit MAIN texture-only rendering leaves palette shading/generated face open. Metrics unchanged.


## 2026-10-01 textured animated interior character

[Interior character materials](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_MATERIALS.md) pass97-source/14,282,453-byte union audit, original material/80-mip and unchanged2,880-tick route checks, headless/cancel/GPU gates and inspected scene capture. Explicit texture-only character mode preserves feet/door authority; palette/generated-face remain open. Metrics unchanged.


## 2026-10-01 fractional animation clock boundaries

[Preview clock precision](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ANIMATION_CLOCK_PRECISION.md) fixes a reproduced fractional loop/one-shot undercount with compensated accumulation and explicit near-boundary numerical equality.18 duration/scale combinations, native suite and original textured GPU/interior host regressions pass. Research metrics unchanged.


## 2026-10-01 retained skeletal mesh bindings

[Retained mesh joint mappings](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_RETAINED_SKELETAL_BINDINGS.md) remove per-pose name resolution. Standalone/interior original textured GPU gates and new material-instance identity checks pass. Core math/source closures/metrics unchanged.


## 2026-10-01 owned character palette defaults

[Authored palette ownership](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CHARACTER_PALETTE_DEFAULTS.md) passes three-original-palette/five-factor independent audit,35-source standalone/100-source interior unions, fallback/malformed/option gates, GPU and cancellation. Palette shading remains explicit pending work. Metrics unchanged.


## 2026-10-01 owned original blueprint face description

[Original face command description](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_BLUEPRINT_FACE_DESCRIPTION.md) passes13-source/332,507-byte original audit, all destination/table/camera/render/prepare operands and vertex hashes, original/synthetic negative gates and native build. Corrects older fixture counts to32 textures/four destination IDs. Generated-face execution remains next; metrics unchanged.


## 2026-10-01 inline original face effects

[Inline face shader compatibility](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INLINE_FACE_EFFECTS.md) adds explicit owned inline effects and validated IMPL0003/PASS0007/PTXM0000. Both pinned original shader candidates and independent25-field/raw-metadata audits pass; malformed/truncated/ownership gates and Godot build pass. Cached blend/write state correlation is recorded; target-alpha mode and combiner remain next. Metrics unchanged.


## 2026-10-01 original face resource closure

[Owned face dependency closure](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_RESOURCE_CLOSURE.md) validates50-source/2,283,397-byte original chain, two shaders,32 textures/251 declared mips and three palettes. Seven bounded legacy DDS terminal blocks are retained via explicit opt-in; declared mip output unchanged. Independent pixel/palette/union, missing/masked, budget and ownership gates pass; Godot build passes. Generated face remains pending; metrics unchanged.


## 2026-10-01 explicit original face preparation

[Original face preparation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_EXPLICIT_FACE_PREPARATION.md) confirms SST1 factory flagtrue and distinct texture endpoint clamp/STFP invalid-to-last palette rules. Six explicit keys prepare five separate slots/nine operations. Independent0/INT_MIN/INT_MAX original audits, omitted-key and portable ownership/failure gates pass; Godot build passes. Original programmable RGB×factor/copied-alpha behavior recorded. Rendering remains pending; metrics unchanged.


2026-10-01: optional owned face pixel-program semantics pass original51-source closure and independent instruction/source-union audit; synthetic truncation/altered-token/ownership and original missing/masked gates pass. Camera/sampler/scratch boundary is traced; CPU raster and GPU MAIN override remain next. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_EXECUTION_BOUNDARY.md).


2026-10-01: [CPU face composition](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CPU_FACE_COMPOSITION.md) passes original15-layer/final-image independent agreement and deterministic hand-calculated/failure/ownership/cancellation fixtures under explicit modern policy. GPU TRTS MAIN binding remains next.


2026-10-01: [Generated face preview](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_GENERATED_FACE_PREVIEW.md) passes original72-source union/one raw TRTS binding, strict profile/per-mesh ownership/failure fixtures, exact GPU RGBA/parameter identity, retained animation materials and cancellation cleanup. Original face displayed; body/eye palette shading remains next.


2026-10-01: [Skeletal albedo factors](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_ALBEDO_FACTORS.md) passes original74-source/four-material/two-program instruction/factor/color audit and GPU post-sampling/display/parameter/animation/cancellation checks. Explicit unlit RGB slice; full lighting and joint face/body customization remain separate.


## Interior generated character —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_GENERATED_CHARACTER.md):139 sources/15,541,123 bytes independently pinned and read once; generated face pixels/TRTS binding, four albedo descriptions/two programs, character/guard geometry and2,880 floor tick records agree. Offline Godot guarded-door/control/animation and GPU filtered color checks pass. One authored face profile is explicit; body/eye palette defaults remain separate.


## Shared color/style snapshot —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SHARED_CUSTOMIZATION.md): exact eight-key union and zero/varied/INT_MAX original palette/face/source agreement; retained74/139-source standalone/interior geometry/route/animation and GPU/cancellation gates. Asset pixels remain original; authored snapshot is explicitly selected. Interactive revisions remain open.


## Live character color/style revisions —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIVE_CUSTOMIZATION.md): one-composer/latest-pending ownership and stale/failure/disposal fixtures pass; standalone/interior rapid edits, eight repeated updates and reset preserve current pose/actor state and mesh/material identities.11-texture/14,592,672-byte owned cache remains bounded. Invalid GPU revisions preserve readback.


## Original skeletal blend descriptions —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_BLEND_DESCRIPTIONS.md):30 targets/27 variables and all ordered sparse-record hashes independently agree over unchanged twelve-source closure. Native negative/ownership gates and Godot build pass. Shape application/HPTS remain open; research metrics unchanged.


## Explicit skeletal shape application —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_BLEND_APPLICATION.md):22,070 original vectors across five selections independently agree (max2.13518e-7); threshold/global normalization/handedness and pre-skin noncommuting fixtures pass. Explicit0..255/float32 policy; original factory defaults and HPTS execution remain open. GPU integration next; metrics unchanged.


## Live original skeletal shapes —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIVE_SKELETAL_SHAPES.md):27 controls, all-max/nonaccumulation/exact reset and GPU failure preservation pass over unchanged74-source character union. Core ownership, GPU/headless/cancellation/build pass. Interior shape integration next; research metrics unchanged.


## Original interior skeletal shapes —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_SKELETAL_SHAPES.md):139-source union unchanged;26,484 vectors/six selections and existing route/floor/material audits agree. Original idle/live shape/failed edit/reset GPU and headless/cancellation pass. Actor state/root placement preserved. Age/normal-map boundary next; metrics unchanged.


## Owned skeletal texture choices —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_TEXTURE_CHOICES.md):four original wrinkle choices,35-source closure and six explicit selection/metadata/mip audits agree. Ownership/clamping/namespace/nonaccumulation/failure/native/build gates pass. Normal-map rendering/age controls next; metrics unchanged.


## Original compressed normals —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_COMPRESSED_NORMALS.md):compiled alpha/green and absolute-radicand behavior confirmed;852 original vectors/six maps agree, including78 outside-disk texels.42-source closure, failure/native/build gates pass. Tangent GPU/normal diffuse preview next; metrics unchanged.

## Original locomotion suffix ownership —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPRESSED_LOCOMOTION.md): original walk/run keys/messages and222 independent position samples pass; full native/build gates pass. Graph selection, actor playback and event scheduling are not claimed.

## Original animation bank —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_ANIMATION_BANK.md): original IFF/SAT/LATT/explicit SSAT/SPAT/PXAT/CKAT17-source closure and222 prepared poses pass; full native/build and failure gates pass. GPU clip switching is next.

## Movement-linked interior —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_MOVEMENT_SKELETAL_PREVIEW.md):147-source original closure,480 retained movement clip updates, original route/live controls/GPU/headless/cancellation pass. Collision owns feet; modern gait/phase policy is explicit. Pose blending remains next.

## Skeletal pose transitions —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_POSE_TRANSITIONS.md):795 independent blend matrices,100 retained GPU updates, interrupted continuity and original route/controls/headless pass. Transition arithmetic/timing is explicit replacement policy.

## Original camera profile —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FREE_CHASE_CAMERA_PROFILE.md):212 original bytes/15 zoom settings/135 height results independently agree; full native/build and malformed/ownership gates pass. Camera-controller integration is next.

## Original interior chase camera and facing correction —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHASE_CAMERA.md):148-source closure/360 independently checked camera queries and64 GPU views pass, preserving original route/resources. Original source/normal audit exposed an extra winding reversal; corrected static/skeletal uploads preserve indices and front/back GPU culling passes. Headless/cancellation/visual checks pass. Third-person camera-room ownership remains next; research metrics/names/packet counts unchanged.

## Camera-cell tracking —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CAMERA_CELL_TRACKING.md):28 original doorway samples independently agree;26 located/13 changed/two ambiguous. Camera/actor cells differ correctly in two owner poses;64 rendered views/visibility toggle/original route/resource/native/build/headless/GPU gates pass. Unknown ownership keeps all rooms visible. Explicit recovery smoothing next; research metrics unchanged.

## Camera-distance recovery —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CAMERA_DISTANCE_RECOVERY.md):256 independently checked timing cases/20 original release steps and paused door retraction pass. Displayed distance remains within current clearance; camera-cell/actor/resource/live-control/native/build/GPU/headless proofs pass. Explicit modern0.2-second policy; original cell-light records next. Metrics unchanged.

## Original cell-light records —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_CELL_LIGHTS.md):19 original records independently agree, including nine directional/ten point lights, HDR colors, local-pi transform correction and five secondary specular instances. Loader/factory/owner/backend evidence establishes the creation contract; strict owned parser/negative/native/build checks pass. Two canonical names bring the registry to2,601; research percentages and345 packets unchanged. Opt-in retained character lighting is next; original light selection remains open.

## Explicit cell-lighting math —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CELL_LIGHTING_MATH.md):288 original-data cases independently agree at max3.15e-7; hand-calculated point/specular, ownership and malformed gates pass. Explicit modern selection/ambient/near-light policy; retained GPU integration next. Metrics unchanged.

## Retained original-cell character lighting —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CELL_LIGHTING_PREVIEW.md):19 owned records/288 independent math cases,32 GPU/all-slot cases and12 active normal-material cases pass; original three-owner route publishes50 retained updates. Camera-cell independence/headless/cancellation pass. Original light selection remains open; room material/packed-color boundary next. Metrics unchanged.

## Original static COLOR0 —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_VERTEX_COLORS.md):backend D3DCOLOR/COLOR0 mapping,72,495 normalized colors and five independent hashes agree. Exact GPU readback/three rendered cases pass. Original vertex sources distinguish pure baked from baked-plus-dynamic room families. Family pixel boundary next; metrics unchanged.

## Original room pixel/material closure —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_ROOM_LIGHTING.md):154-source closure/18 material bindings/three programs and192 independently executed cases agree at max7.28e-8. Unselected factor state is explicit; source modules distinguish baked/dynamic/emissive terms. Retained GPU rooms next; metrics unchanged.

## Legacy pixel COLOR range —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_PIXEL_COLORS.md):explicit vertex-color saturation fixes the PS1.x/PS2.0 boundary; eight normal/four mouth HDR/negative GPU cases and prior original route/resource gates pass. Pixel specular remains HDR. Room GPU integration next; metrics unchanged.

## Retained original room materials —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_ROOM_LIGHTING_PREVIEW.md):42 retained room overrides share existing28 textures;48 GPU equations agree at max0.002881. Original route/12 owner publications/camera independence/headless pass. Baked and dynamic inputs remain distinct; static door normal/tangent boundary next. Metrics unchanged.

## Original static door normal/material closure —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_DOOR_NORMALS.md):156-source closure/110 tangent records and64 independent compiled PS20 cases agree. Four original zero tangents are preserved; hardware behavior remains Unknown. Owned parser/negative/original route gates pass. Retained door GPU adapter next; metrics unchanged.

## Retained original door normals/specular —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_DOOR_NORMAL_PREVIEW.md):four surfaces/two retained materials share existing meshes/28 textures;110 converted tangents independently agree and12 GPU equations pass. Source zeros use explicit modern fallback. Original route/camera/owner/headless/cancellation gates pass. Original backend light-slot selection next; metrics unchanged.

## Original bounded light selection —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_LIGHT_SELECTION.md):25 hash-gated instruction anchors close baked/unbaked masks,1+2 directional/1+4 point slots, absolute-RGB ranking and channel roles. Twelve original/64 synthetic independent selection cases pass. Live visibility/order remains Unknown; baked room over-lighting correction next. Metrics unchanged.

## Recovered cell slots and baked lighting correction —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_CELL_LIGHTING_PREVIEW.md):576 original grouped-light cases independently agree;32 all-slot GPU cases pass. Baked room primary diffuse over-lighting is fixed; opt-in recovered selection for character/doors preserves source/route/owner/resources/headless/cancel gates. Original live visibility/LIT/precision remain open; metrics unchanged.

## Original tangent-light varyings —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_TANGENT_LIGHTING_VARYINGS.md):157-source union and660 independent original vertex/view/light cases close raw basis and vertex/pixel normalization staging. Explicit zero/interpolation policies are bounded; GPU door varying integration next. Compiled vertex precision remains Unknown; metrics unchanged.

## Door tangent varying GPU stage —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_DOOR_TANGENT_VARYING_PREVIEW.md):48 perspective/owner/handedness GPU cases pass at max0.003136 and distinguish old fragment recomputation. Original157-source route/resources/headless/cancel remain valid. Character vertex-family evidence next; metrics unchanged.

## Original character tangent vertex closure —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_TANGENT_VERTEX.md):independent original body/head/face bindings share the closed door VSH/functions. Owned dependencies and malformed/source gates pass; full157-source union remains shared/read-once. Character GPU integration next; compiled vertex precision remains Unknown. Metrics unchanged.

## Shared character tangent varying GPU stage —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_TANGENT_VARYING_PREVIEW.md):48 combined hue/mask/perspective/tangent GPU cases pass at max0.002878; retained original route, headless/cancel and material identities pass. Shader-cache mask alias corrected. Source vertex lighting/constant packing next; metrics unchanged.

## Original positive point attenuation —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_POINT_ATTENUATION.md):20 baseline/source/packing anchors and60 original/48 synthetic independent reciprocal cases pass; coefficient order/fourth-zero/source cap absence closed. Singular/device/LIT behavior remains Unknown. Metrics unchanged.

## Recovered point attenuation preview —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_POINT_ATTENUATION_PREVIEW.md):64 recovered GPU cases pass at max0.003189 and distinguish old cap by RGB-vector0.148170. Original157-source route/resources/live controls/headless/cancel pass. Metrics unchanged.

## Original light-scaling selector —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIGHT_SCALING_SELECTOR.md):23 configuration/constructor/copy/raster anchors close enableLightScaling default true and engine shader snapshot ownership. Owned both-mode/original source/math/route/headless gates pass. One canonical getter brings current count to2602;345 packets/research scores unchanged.

## Complete selected tangent vertex-source ownership —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_TANGENT_VERTEX_SOURCE_CLOSURE.md):Independent source graph confirms4 exact sources/3 include edges/20,146 source bytes; full union159 sources/16,642,134 bytes, each read once. All four missing/masked/truncated/changed/oversized gates, original route and GPU/headless pass. Metrics unchanged.

## Recovered scene-lighting default —2026-10-01

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_RECOVERED_LIGHTING_DEFAULT.md):SceneCellLighting now uses the recovered slot/scaling/positive attenuation path by default. The former SceneLegacyLightSelection flag is retained as a compatible alias. Full scene without that flag passes source/route/48 character/48 door/64 selected-light GPU and retained-resource gates. Metrics unchanged.

## Combined point-light vertex color acceptance —2026-10-01

Accepted bounded owned fixture:48 corrected combined point-color/tangent/hue/mask cases pass at max0.003225 within2/255. Original compiled vertex/hardware precision remains open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_POINT_COLOR_GPU_PROBE.md).

## Parallel development Round 1 — 2026-10-01

| Boundary | Accepted behavior | Evidence / practical limit |
|---|---|---|
| Explicit skeletal detail1 | Original object IFF → SAT → MLOD → selected MGN → skeleton/geometry/material closure; read-once and rejection gates | [Contract](SKELETAL_LOD_SELECTION_CONTRACT.md); 29 sources, 968 positions, 1,904 triangles; independent byte audit. Skeleton level0 binding is explicit; automatic distance selection remains open. |
| Mouth vertex sources | Selected EFCT/PVSH and four exact source identities; owned validation and CPU color staging | [Contract](SKELETAL_VERTEX_LIT_SOURCE_CONTRACT.md); 64 independent modern literal-power cases, maximum error 3.4835412372480334e-6 at 1e-5. Original compiled LIT behavior remains unknown; no GPU equation change. |
| Animation event playback | Engine-neutral bounded cursor, loop/endpoint ordering, pause/reset, authored duplicate preservation, atomic rejection | [Contract](ANIMATION_EVENT_CURSOR_CONTRACT.md); independent 160-target oracle and original walk/run MSGS. Playback interval policy is modern; effect/audio delivery remains open. |
| Zone scene message bytes | SelectCharacter encoder/decoder and CmdStartScene decoder; owned DTOs and strict malformed-input policy | [Contract](ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md); hand-authored vectors and independent Python oracle. No connection, scene application or CmdSceneReady behavior. |

Lead integration retains the full original interior preview: 160 owned sources / 16,643,480 bytes, each read once; independent source/math/route audit, native suite, fresh GPU capture and headless first-lookup cancellation pass. [Round report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_1_2026-10-01.md). Research scores and packet/function registries are unchanged.

## Parallel development Round 2 — 2026-10-01

| Boundary | Accepted behavior | Evidence / limit |
|---|---|---|
| Lower skeletal details | Details0..3 closure and sampled walk/run deformation at explicit level0 skeletons | [Contract](SKELETAL_LOWER_DETAIL_CONTRACT.md); 1,272 matrix rows / 11,640 weighted positions agree independently; automatic LOD and skeleton coupling remain open |
| Mouth multi-light GPU channels | Existing vertex shader tested for separate diffuse/specular and combined pixels | [Contract](SKELETAL_MOUTH_LIGHTING_GPU_CONTRACT.md);216 cases max0.003438 at2/255; wrong staging discriminators pass; literal power remains modern |
| Event playback time bridge | Compensated elapsed seconds/FPS, focus/play gates and owned generation-tagged batches | [Contract](ANIMATION_EVENT_PLAYBACK_CONTRACT.md);1,120 independent partition cases and both pinned clips; no original scheduler or effect/audio delivery |
| Deferred scene readiness | Exact ready header, bounded ordered staging and one-shot current-generation publication | [Contract](ZONE_SCENE_TRANSITION_CONTRACT.md);lead native/independent wire checks pass; no terrain construction, replication parsing or connection routing |

Lead combined native suite, original lower-detail byte/deformation audit, Godot build, fresh full-interior GPU and workspace checks pass. Worker headless/cancellation checks pass. Source union remains160 /16,643,480 bytes, read once. [Round report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_2_2026-10-01.md).

## Parallel development Round 3 — 2026-10-01

| Boundary | Accepted behavior | Evidence / limit |
|---|---|---|
| Lower-detail shading | Shared single-color MAIN/SPEC ownership, raw effect/palette/material audit and compiled pixel agreement | [Contract](SKELETAL_LOWER_DETAIL_SHADING_CONTRACT.md);22 sources per detail,64 original texel cases max8.87e-8; hardware/compiled vertex selection remains open |
| Explicit detail GPU preview | Selected MLOD1–3; no-normal vertex-only family for2/3; default0 preserved | [Contract](SKELETAL_DETAIL_PREVIEW_CONTRACT.md);1904/756/190 triangles; exact48/25/25 native/Godot sources and13 option negatives; automatic LOD is open |
| Diagnostic event delivery | Fixed ring, current-owner/generation/frontier checks and original walk/run controls | [Contract](ANIMATION_EVENT_DIAGNOSTIC_PREVIEW_CONTRACT.md); headless165 poses and fresh GPU captures with authored MAIN; no original scheduler/audio/effect/root motion claim |
| Transport-to-scene proof | Reorder/fragment/CRC/retry/ACK feeds deferred one-shot ready publication | [Contract](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md); independent peer fixture and export oracle; create/baseline opaque and synthetic, no actual replication or live connection |

Lead combined native suite/build, original shading and wire oracles, fresh details2/3/event/default0 interior GPU, headless/cancellation and scope checks pass. Worker detail1 GPU and all-three headless/cancel evidence retained. [Round report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_3_2026-10-01.md). Existing Godot startup certificate-store error persists in passing offline logs.

## Parallel development Round4 —2026-10-02

Modern projected-detail hysteresis and accepted-displacement event traversal now have deterministic engine-neutral APIs. Original detail2/3 meshes stay retained through132 switches with one27-resource source union and shared sampled idle phase. Real create/destroy/containment/end-baselines codecs preserve exact wire values. [Round report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_4_2026-10-02.md) records independent and combined native checks, inspected GPU captures, budgets/ownership and limits. No automatic adapter, object registry, original scheduler or live connection claim.


## Accepted parallel development Round5 —2026-10-02

Accepted explicit create-CRC to original creature resources, all-four retained automatic detail preview, opt-in original-interior accepted-movement event diagnostics, and immutable-snapshot object lifecycle registry. Lead native/independent audits, combined suite, clean Godot build and fresh retained/interior GPU checks pass. Original resource proof37 sources/4,683,848 bytes; retained union58/6,111,276; full customized interior160/16,643,480. Catalog, selection and recovery rules are modern explicit policy; original scheduler/global catalog/field replication/async world/live work remains open. [Round report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_5_2026-10-02.md) separates worker and lead gates. Research89%/94%,2,602 names and345 packet rows unchanged. Rounds6–8 remain authorized.


## Accepted parallel development Round6 —2026-10-02

Accepted robust raw-create placement, owned revision-guarded Godot creature visuals, bounded asynchronous resource initialization with current-attempt checks, and strict baseline/delta application envelopes. Lead native/independent/original-source audits, combined suite, clean build and fresh GPU/headless/cancel/cleanup pass. [Round record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_6_2026-10-02.md) separates modern policy, synthetic bytes and original assets. Application envelope23 bytes versus server-framed27 is now explicit in protocol documents. Full schema/world/live and original readiness/repair remain open. Research89%/94%,2,602 names/345 rows unchanged; Rounds7–8 remain authorized.


## Accepted parallel development Round7 —2026-10-02

Accepted original static-object resource preparation with explicit SWTS material gaps, placed asynchronous creature visual, offline SOE/world lifecycle coordination, and six ordered typed CREO3 scalar delta fields. Lead independent original-source/wire/placement audits, combined native suite, clean Godot build and fresh GPU/headless/cancellation/cleanup pass. [Round record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_7_2026-10-02.md) records ownership, strict failure policy, scope and corrections. Full baseline/state binding, static animated textures, terrain/live and original readiness remain open. Research89%/94%,2,602 names/345 rows unchanged; Round8 remains authorized.


## Accepted parallel development Round8 —2026-10-02

Accepted explicit original SWTS frame materials, world-owned offline Godot creature publication, exact-revision scalar state and complete eighteen-field CREO3 baselines. Lead native/independent/original-source audits, baseline-to-store integration, clean Godot build, fresh world GPU/headless/cancel and full customized160-source interior regression pass. [Round8 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_8_2026-10-02.md) records limits and reproduction. World fields remain opaque until a stable-incarnation/state-binding milestone; no full terrain/live or SWTS timing claim. Research89%/94%,2,602 names/345 rows unchanged. Workers idle; authorization ends after Round8.

## Accepted parallel development Round9 —2026-10-02

[Round9](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_9_2026-10-02.md) accepts strict catalog profiles, retained terminal frame/detail inspection, stable-incarnation baseline/scalar state and wounds vector decode/application. Lead combined freshness, native/oracles and fresh GPU/headless/cancel pass. Terminal uses labeled built-in MAIN inspection; vector counter/index policy remains explicitly modern. No original timer, typed-world integration or live claim.

## Accepted parallel development Round10 —2026-10-02

[Round10](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_10_2026-10-02.md) accepts39-source optional albedo, state-driven original actor, typed-world scalar binding and instruction-backed legacy vector kernel. Lead combined SOE/world/original-resource failure/ownership proof, native/oracles and fresh GPU/headless/cancel pass. Current typed-world indices11–16 and modern Height policy remain bounded; mixed wounds/world presentation follows. Research metrics unchanged.

## Accepted parallel development Round11 —2026-10-02

Accepted palette-only owned scene colors, world-owned original albedo presentation, bounded acknowledged world changes and ordered atomic mixed scalar/wounds plans. Lead shared/native/independent source/wire/state/pixel oracles, clean Godot build and fresh GPU/headless/cancellation/cleanup pass. Modern observer/Height/strict customization policies are explicit; wire customization/full lighting/other views/live remain open. Research89%/94%,2,602 names and345 packet rows unchanged. [Round11 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_11_2026-10-02.md). Round12 remains authorized.

## Accepted parallel development Round12 —2026-10-02

Accepted opaque DDS colors, acknowledged observer/palette rendering, atomic mixed scalar/wounds world/store mode and standalone recovered components-set codec. Lead independent native/source/wire/state/pixel audits, shared suite, clean Godot build and fresh GPU/headless/cancel gates pass. Combined original39-source palette resources and mixed state reach current presentation without resource reload; late failure retires the whole generation. Components-world, other views/fields, original callback/Height semantics and full rendering/live remain open. Research89%/94%,2,602 names and345 packets unchanged. All workers idle; authorization ends after12. [Round12 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_12_2026-10-02.md).


## Round13 - components transaction, resource route and two actors

[Round13 acceptance](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md): explicit owned creature/static router; ordered components/scalars/wounds plan and incarnation-bound Mode3 world transaction; acknowledged two-creature original-resource presentation. Lead fresh router65 negatives/66 borrowed buffers and original39/27-source audits, composite18 cases, runtime69 recovered set cases/1000 cycles, original framed combined rollback/ownership proof, shared/old Mode2 regressions, warning-free Godot build, native50 negatives/41 truncations, GPU/headless/cancel/primary-Main and5 prelaunch gates pass. GPU A changes13,435 pixels; B0 and exact resources retained across A recreation. Modern Height/observer/routing/accounting choices are explicit; static world publication, original callbacks/visual semantics and live interoperability remain open. All workers idle after13; no staging or publication.

## Round14 — mixed original human and terminal world

[Round14 integration](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_14_2026-10-02.md) adds pure completion validation, bounded generic world resource delivery with preserved legacy creature APIs, independent heterogeneous lifecycle/SOE peer and guarded static actor. Original39/27-source async delivery and one static reload preserve human typed state, incarnation and GPU IDs. Static TANO remains opaque; recipe/family mismatch fails the whole world with no returned prefix. Assets89 negatives/seven positives, Networking12 records/1,006 malformed probes/10 late failures, Runtime66-source/142-mip original/model/late-task checks, Rendering68 native/22 Godot/five launcher negatives and source/pixel oracles pass. Shared suite, clean Godot build and fresh mixed/standalone GPU/headless/cancel/primary-Main pass. Front human Height changes14,333 pixels, terminal0 and recreated crops identical. The terminal screen uses an explicit unshaded inspection override; full effects/face/customization/live and reusable heterogeneous consumer remain open.

## Round15: reusable heterogeneous presentation

The [consumer](SCENE_WORLD_OBJECT_PRESENTATION_CONTRACT.md) and
[recipe visual factory](SCENE_WORLD_OBJECT_VISUAL_CONTRACT.md) now join one
accepted generic world through an engine-neutral interface. Lead standalone/shared
160 assertions, independent40-event peer, alternate-ID original-resource native
join, warning-free Godot build, combined GPU/headless/cancel and legacy headless
regression pass. Resource/state updates, stale acknowledgment retry, isolated
recreation and zero-outstanding cleanup are verified; sequential adapter apply
does not promise whole-batch visual rollback. Scalar creature mode and opaque
static/frame0 policy remain bounded.
[Round15 acceptance](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_15_2026-10-02.md).
No live session, continuous scene pump, broad schema or full render fidelity claim.

## Continuous scene host accepted - October2,2026

[Round16](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_16_2026-10-02.md)
joins the bounded [pump](SCENE_WORLD_PRESENTATION_PUMP_CONTRACT.md),
[Godot host](SCENE_WORLD_PRESENTATION_HOST_CONTRACT.md) and logical
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
[Godot motion](SCENE_WORLD_OBJECT_MOTION_VISUAL_CONTRACT.md).
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

## Round20 integration

[Accepted integration evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R20_CUSTOMIZATION_INTEGRATION.md) records owned CIDM/raw packed decoding, supplied current-creature appearance revisions and bounded presentation/world drain. Original-resource, native/headless/GPU, latest/failure/retirement and real pending-close gates pass. This is explicit supplied inspection state; rawfield4 state translation is next. Four Low workers stopped; overnight authorization permits the next round after acceptance. Known certificate/shader initialization diagnostics persist and are counted, while actual upload/readback/pixels pass.

## Round21 integrated boundary

Round21 joined native/headless/GPU wire appearance gates pass: earlier valid value survives later invalid attempt, empty forms retain display, extended-small spelling survives, and malformed/unknown-ID actual ingress drains actors/archive jobs. Exact original CIDM and supplied consumer bounds are distinguished from live defaults. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_WIRE_CUSTOMIZATION_INTEGRATION.md).

## Round22 integrated boundary

Round22 original ACST/independent byte/state/headless/GPU gates pass. Mixed4+11..16 atomic state and actual Height updates retain resources/clock; held appearance commit preserves newer Height/scalars. Components/wounds/palette context remain next. [Evidence/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_INTEGRATION.md).

## Round23 integrated boundary

The supported CREO:3 union4/5/11–17 now stages raw customization, scalars, components, wounds and pending world state together. Original selected ACST/CIDM context supplies exact ranges and authored defaults through five already owned palettes. Main independent native, headless/GPU, motion and held-container gates pass. Broader fields/views, live mounts/player seeds and graphical attachment/wound behavior remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_FULL_CREATURE_INTEGRATION.md).

## Round24 integrated boundary

Supported CREO3 fixed common fields0/3/6–10 join the existing union. Current common state survives older ingress; visibility uses that overlay while hidden appearance work commits. Actual delta headless/GPU/motion and separate held-baseline visibility proofs pass. Names1/2, TANO, gameplay meanings and live interoperability remain open. STF variant1 exact owned lookup passes two original tables; locale/cache/fallback is deferred. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_CREATURE_INTEGRATION.md).

## Round25 named creature increment

The supported ordered CREO3 union now covers indices0–17, including raw StringId/custom names. Current name overlays and variable charge survive older ingress. Bounded original STF1 resolution drives an optional caption; actual delta headless/GPU and separate held-baseline name proofs pass. Explicit modern name precedence/directory/display policies do not establish original UI/locale behavior. Other families/views and live interoperability remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_NAMED_CREATURE_INTEGRATION.md).

## Round26 tangible stages

Complete owned TANO3 baseline and ordered0–10 delta codecs, standalone current tangible store and bounded original STF preload pass independent native oracles. Static supplied-projection caption/visibility passes original terminal GPU and lifecycle checks. Lead codec-store-preload join passes; atomic typed SceneWorld publication is next. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R26_TANGIBLE_STAGE_INTEGRATION.md).

## Round27 typed tangible publication

Optional complete TANO3 world state commits with its journal after private preparation. Static typed token gates actual original terminal caption/raw visibility beside supported CREO ingress; five headless/GPU states, retained resources and teardown pass independent oracles. Default opaque behavior remains. Authored template names and local SWTS selector semantics are separately documented; outer clock and live interoperability remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TANGIBLE_WORLD_INTEGRATION.md).

## Round29 accepted behavior

Original mission terminal all-frame loading and mixed-world playback pass headless/GPU:34 unique sources,15 retained images/126mips/2490372decoded bytes,3969782pending weight;8 cached GPU frame uploads,13 independently checked states and deterministic retirement. Initializer policy-copy omission was caught in the main-host gate and fixed before acceptance. Typed TANO3 captions/visibility and prior CREO3 regressions pass. Standalone object clock passes390checks/36cases/157independentoperations; direct TANO6 state audit109anchors/twoRTTI/16models. Modern numeric/time policy and absent live interoperability remain explicit. [Combined acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R29_SWITCH_FRAME_INTEGRATION.md).

## Round30 clock and defender increment

Owned TANO6 state passes21 sequences/23 transitions,700 negatives,315 budget and114 ownership checks with an independent wire/state/event/work oracle. Clock-to-cursor147 checks/14 cases/79 reference operations and resource-option20 projections/seven completions pass. Actual main headless/GPU fourteen-state clock delivery and prior supplied-time mode retain original terminal geometry/captions and drain caches/clock without update I/O. First-visible-frame uncertainty and absent live interoperability remain explicit. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R30_CLOCK_AND_DEFENDER_STAGES.md).

## Round31 outdoor and view6 prerequisites

Owned direct-version terrain masks and all nine exact terrain patch patterns pass independent original-byte/table and native oracles. Current-incarnation TANO6 publication and historical-event ownership pass143 checks/23 transitions/38 events. CREO6 prefix/extension structure passes109 anchors/18 registrations/16RTTI; complete codec/world routing remain next. Actual headless/GPU consumed-time failure retires the clock and cached presentation without retry. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R31_TERRAIN_AND_REPLICATION_STAGES.md).

## Round32 accepted world and terrain inputs

Optional typed TANO6 world publication uses prebuilt store/world candidates and guarded historical event ownership. Main headless and GPU joins pass six literal full-width/wrap/assignment states, eight current events, exact acknowledgment, retained accounting and original human/terminal visuals; prior fourteen-state animation/name/customization gates also pass. Complete18-field CREO6 baselines and the explicit scalar/string/pair delta subset pass independent byte/model/budget oracles. Owned PTAT0013/0014 DATA/TGEN inputs and supplied terrain geometry/UV/color adapters pass original-byte/static/native/headless/GPU checks. Complete TGEN generation, CREO6 vector deltas/state, terrain seam/sample mapping, gameplay and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows are unchanged. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R32_WORLD_AND_TERRAIN_INPUTS.md).

## Round33 terrain structure and creature state

Owned TGEN inputs expose 257 layers and 87 AHCN records in the same pinned Tatooine file while retaining unsupported rules. Exact nine-sample terrain assembly and seam branch priority pass independent static/native checks. Complete current CREO6 scalar/string/pair models now have bounded incarnation-owned publication and a prepared commit seam. Separately, the exact HAM/max-HAM vector codec and mutation kernel pass independent wire/state/counter/work oracles. These remain separate partitions, not a composed full CREO6 world update. Original mixed-world headless and combined Core regressions pass. Height evaluation, group semantics, vector composition, original owner side effects and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain unchanged. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R33_TERRAIN_AND_CREATURE_STATE.md).

## Round34 creature world and terrain arithmetic

Optional supported CREO6 publication now joins the original mixed host: six exact full models, ordered events/acknowledgment and retained visuals pass headless/GPU. Separately accepted scalar/HAM composition is not yet world-routed. Terrain constant-height arithmetic and color conversion have independent native/static oracles; original worker precision and complete generation remain open. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R34_CREATURE_AND_TERRAIN_KERNELS.md).

## Round35 terrain and composite publication prerequisites

The current-incarnation composite CREO6 store passes an independent full-model/event/work oracle, and field15 wearable mutation semantics are separately closed. BCIR serialized inputs match all256 original records. Terrain startup/configuration has bounded static evidence; original worker precision remains unresolved. No new world route or generated terrain is claimed. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R35_TERRAIN_AND_COMPOSITE_PREREQUISITES.md).

## Round36 composite world and terrain boundaries

Optional scalar/HAM CREO6 world delivery passes actual original mixed-host headless/GPU full-model/event/acknowledgment/retained-resource gates. All four vector specializations now have independent narrow components, but defender/wearable composition into a full ordered model remains next. Owned BREC and supplied-coordinate BCIR arithmetic pass independent checks with separate loading/query precision. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R36_COMPOSITE_WORLD_AND_TERRAIN_BOUNDARIES.md).

## Round37 complete creature kernel and supplied terrain

[Complete CREO6 kernel](CREATURE_VIEW6_FULL_CONTRACT.md) accepts all18 ordered delta indices with full-model and48-byte event accounting. Current world delivery still uses the frozen scalar/HAM partition; full prepared storage/world join remains next. Supplied circle/AHCN geometry, GPU readback/cleanup and surface queries pass independently and together; original generated terrain is not claimed. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_COMPLETE_CREATURE_AND_SUPPLIED_TERRAIN.md).

## Round38 full creature publication and terrain inputs

[Complete CREO6 world](SCENE_WORLD_CREATURE_VIEW6_FULL_CONTRACT.md) publishes all18-field models and48-byte journals through genuine prepared commits.97 sequences/494 events and original-resource headless/GPU sixmodels/45events pass. CREO4 complete snapshots/scalars, BPLN raw inputs and explicit four-profile coordinates also pass. Owner gameplay effects, wearable resource attachment and original terrain generation remain separate. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_FULL_CREATURE_PUBLICATION_AND_TERRAIN_INPUTS.md).

## Round39 supplied terrain grounding and CREO4 prerequisites

Original-resource headless/GPU terrain joins now pass16 synthetic supplied patches/128 triangles, nine grounded motion states and exact resource drain, alongside complete CREO6/TANO6/animation/name/customization gates. Fresh rectangle influence, padded complete channel assembly and CREO4 field2 deltas pass independent main replay. Original generation and gameplay remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_TERRAIN_GROUNDING_AND_PRIVATE_VIEW.md).
## Round40 layer and CREO4 composition

Fresh BPOL influence, supplied layer combination, restricted CREO4 scalar/encumbrance composition and the separate byte-key skill-map kernel pass independent main replays. The supplied layer/AHCN/coordinate/patch/query join passes nine profiles/3600 samples/144 patches/576 queries. ADTA version-to-runtime inversion mapping is verified separately; unknown flag/cache roles remain explicit. Original-resource headless regressions and combined/default builds pass. Full CREO4 publication, original terrain generation, owner gameplay and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R40_LAYER_AND_CREATURE4_COMPOSITION.md).
## Round41 supplied height execution and CREO4 fields

Open-polyline influence, ordered supplied height execution, scalar/encumbrance/skill-map CREO4 composition and the separate mission-set kernel pass independent main replay. The native terrain join passes3600 samples/144 patches/576 queries across nine profiles. Actual computed-height original-actor headless/GPU joins pass400 samples/16 patches/nine grounded states and exact disposal; late malformed fourth-chunk input drains12 partial visuals. Static layer preparation closes enabled counts/local bounds/cache stride conditionally. Full14-field CREO4 composition/publication, real height filters, original complete terrain, gameplay and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R41_SUPPLIED_HEIGHT_AND_CREO4_FIELDS.md).
## Round42 complete private state and supplied terrain sequence

Complete14-field CREO4 codec/state and full prepared storage pass122 independent full-model sequences/2439 events. Separate supported storage passes122 sequences/924 events and40 prepared-seam checks. Ordered supplied layer sequences and a two-layer geometry/query join pass15456 plans and3600 joined samples/144 patches/576 queries. Direct FHGT evidence establishes current-height reads before this layer's affectors; the raw/query implementation passes375 raw cases/13959 queries. World routing, query-time filter composition, original recursive terrain, gameplay and live interoperability remain next. Research89%/readiness94%,2626 names and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R42_PRIVATE_CREATURE_AND_TERRAIN_SEQUENCE.md).

## Round43 private world and current-height filters

Optional complete CREO4 world publication and original-resource headless/GPU joins pass independent full-model, ordered-event, variable-journal and cleanup checks. Query-time FHGT supplied execution and its two-layer geometry/query join pass independently. FSLP raw structure is implemented; initializer timing/x87 sine preprocessing remains unresolved. Server observer CREO4 deltas can arrive before end-baselines without a full view4 baseline; original default/template initialization and modern admission for that path remain the next compatibility milestone. The current seeded finalized private route does not yet cover it. Original recursive terrain, gameplay and live interoperability remain open. Research89%/readiness94%,2626 names/zero duplicates and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R43_PRIVATE_WORLD_AND_HEIGHT_FILTERS.md).

## Round45 accepted compatibility increment

The [Round45 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_45_2026-10-04.md) records independently reviewed/replayed supplied
AHFR/FFRA consumers, an incarnation-bound received-only partial CREO4 store,
one human movement-template input chain and the actual current-height FHGT/AHCN
Godot preview. The store accepts pending/finalized objects without default seeds
but has no world/router admission yet. Original fractal generation/cache lifetime,
floating controls, complete terrain and later creature writers remain unknown.
Research scores and canonical name/packet denominators remain unchanged.

## Round46 accepted compatibility increment

The [Round46 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_46_2026-10-04.md) records received-only CREO4 world/router admission and its original-resource headless/GPU preview, an owned authored movement profile, and static family-map/sampler prerequisites. Partial/full4 policies are mutually exclusive; no default seed is fabricated. Fractal cache misses can return retained wide values while hits reload F32, so supplied-F32 consumer acceptance is not original sampler parity. Research scores,2626 canonical names and345 packet-source rows are unchanged.

## Round47 compatibility increment

[Round47](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_47_2026-10-04.md): retained samples7762 cases/13 ownership/21 budget/18 factory negatives; received reader128 cases1408 actions10240 slots16947 checks; authored consumer20 fixtures659 independent checks; seed/noise289 anchors1004 conditional cases520 recurrence checks. Lead786 retained joins,72 separate-source joins and six-model/60-slot original-resource headless/GPU host pass. These are bounded implementation/research gates, not an overall client completion counter.

## Round48 compatibility increment

[Round48](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_48_2026-10-04.md): strict MFRC330 cases/full-U642592 cases; conditional kernel1155 cases24 API negatives; explicit movement840 cases9154 checks2988 independent comparisons; original speed109 anchors40 byte checks18 negatives. Lead864 computed consumer and144 actual movement compositions pass. These bounded gates do not measure whole-client completion or original terrain/gameplay parity.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.

## Round50 accepted boundary

The [owned family/sampler join](TERRAIN_FRACTAL_FAMILY_SAMPLER_JOIN_CONTRACT.md), [current resource movement host](SCENE_WORLD_CURRENT_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md), and [controller input chain](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md) extend the prior accepted slice. Exact conditional compositions and modern movement/source ownership are independently checked; original terrain/controls/full movement parity remain Unknown. See [Round50](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_50_2026-10-04.md) for assembled acceptance and next probes.

## Round51 bounded boundary

[Generator-to-family height routing](TERRAIN_GENERATOR_FAMILY_HEIGHT_JOIN_CONTRACT.md) passes486 actual joins under explicit fresh-call controls. [Run-default preference](PLAYER_CONTROLLER_RUN_FLAG_CONTRACT.md) and [acceleration helper](CREATURE_ACCELERATION_HELPER_CONTRACT.md) have bounded static evidence; message timing, getter sampling and units remain open. The shared original-resource host remains the separately accepted Round50 slice. [Round51 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_51_2026-10-04.md).

## Round52 accepted boundary

[Computed family-height terrain](SCENE_WORLD_FAMILY_HEIGHT_TERRAIN_PREVIEW_CONTRACT.md) reaches headless/GPU surface and grounding checks under explicit synthetic rules/controls. [Acceleration getter](CREATURE_ACCELERATION_TEMPLATE_GETTER_CONTRACT.md) and [run-input producer](PLAYER_CONTROLLER_RUN_INPUT_PRODUCER_CONTRACT.md) have bounded original evidence; update units, active queue binding and full terrain/controller parity remain open. [Round52](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_52_2026-10-04.md) records independent acceptance gates.

## Round53 accepted functional gates

[Selected original-rule preview](SCENE_WORLD_FAMILY_HEIGHT_TERRAIN_PREVIEW_CONTRACT.md#round53-selected-original-rule-with-one-sampler-per-patch)
passes400 retained samples/heights with4 batch versus400 reference samplers,
headless/GPU surfaces and grounding, late rejection with zero terrain visuals,
prior Version4/3 previews and final Core/Godot builds. The [round record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_53_2026-10-04.md)
keeps full original terrain and the two remaining controller/input carrier edges
separate. These gates do not measure whole-client completion.


## Round54 accepted increment

[Round54](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_54_2026-10-04.md) passes all four independent worker replays,400 selected-filter native/reference/Fraction/headless/GPU cells,16 patches/128 triangles/nine grounded states, late invalid-cell zero-allocation cleanup, prior Version5 host and final Core/Godot builds. Full original terrain traversal and player-update units remain Unknown.


## Round55 owned slice and filter pipeline

[Accepted bounded integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R55_LAYER_SLICE_AND_FILTERED_FIELD.md) adds explicit original-layer evidence/rejection and immutable retained filter-to-field composition. Version7 headless/GPU/native/Fraction matches400 cells/16 patches/128 triangles/nine grounded states; late fourth-chunk invalid input allocates zero terrain visuals and drains cleanly. Prior Version6 remains verified. Original layer3's height curves are unsupported; full terrain traversal and original numeric parity remain unclaimed. Controller seconds have conditional carrier evidence; queue lifetime and physical acceleration remain open.


## Round56 boundary and nominal motion

[Round56 integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R56_BOUNDARY_ADMISSION_AND_NOMINAL_SPEED.md) adds retained boundary-only first-gate evaluation and a60-step supplied nominal-speed trace through the real native motion driver/original-resource presentation. Native/Fraction/headless/GPU, late invalid-step preflight/no-new-publication drain and prior terrain gates pass. Original layer3 curve support remains unsupported; curve operand/store research closes with exact power/control parity still Unknown. Queue storage writer/cleanup is evidenced, with internals/lifecycle still open.


## Round57 filters, approximate curves and supplied targets

[Round57 integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R57_BOUNDARY_FILTER_AND_TARGET_SPEED.md) adds boundary-admitted retained FFRA products, a separately labeled modern first-curve batch and supplied target construction through the original-resource motion host. Worker/lead numeric and malformed-input/cleanup gates pass; exact original curve output and full original locomotion remain unclaimed. Strict curve admission is unchanged.


## Round58 height display and atomic planning

[Lead gates](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R58_APPROXIMATE_LAYER_HEIGHT_AND_SPEED_PLAN.md) pass400 native/host height cells,16 patches/128 triangles/nine grounded states and60 atomic-plan target/transition/motion comparisons. Late fourth-chunk NaN allocates zero terrain visuals; final-direction NaN publishes zero plan steps/motion. Both drain archives and leave zero actors. Prior Version7 terrain and Version1 nominal traces pass. Final assembled scope/link review precedes acceptance.


## Round59 sparse terrain and received local speed

[Round59 lead checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R59_SPARSE_HEIGHT_NORMALS_AND_CURRENT_SPEED.md) adds positive-cell sparse heights, modern stored-height normals/lit policy and actual received local fields through paired supplied readers/atomic speed planning. Independent worker/main/native/headless/GPU/malformed-input/cleanup and prior-mode gates pass. Height-query count200 versus400 with explicit supplied warm priors; no original cache/normal/control/locomotion parity is inferred. Queue backing release ordering closes while concrete payload ownership remains Unknown. Research89/readiness94,2626 names and345 packet rows stay unchanged.

## Round60 owned compositions and requested stop

[Round60 lead checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R60_OWNED_TERRAIN_AND_CURRENT_SPEED_COMPOSITION.md) records independently replayed owned sparse evaluation, modern height/normal patch assembly, paired current-speed inputs and the exact queue-entry producer boundary. Native/headless/GPU comparisons preserve400 terrain heights/normals and60 movement steps; rejected inputs prevent publication and drain resources. Cleanup now attempts independent closures and releases source/buffers through finally. Original numerical/material/movement parity and payload ownership remain Unknown. Research89/readiness94,2626 names and345 packet rows are unchanged. Levarris requested completion of60 then a stop; all workers idle, continuation heartbeat paused, no61 dispatched.

## Round61 input and retained composition acceptance

[Round61 lead checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-05_R61_SHADER_PATCH_SET_AND_CURRENT_PLAN.md) adds raw SGRP0006 families, strict retained patch sets, current-source atomic planning and one exact queue-payload caller. Independent replay/native/headless/GPU and four rejection-cleanup gates pass, preserving400 terrain samples and60 movement steps. Original shader meanings/numerical/material/movement parity and payload ownership remain Unknown. One round only; workers idle and continuation paused. Timing is recorded separately from research/readiness/function/protocol measures, which are unchanged.

## Round62 retained queries and playback

[Round62 acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-05_R62_SET_QUERY_AND_PLAN_PLAYBACK.md) adds actual retained-set surface queries and current-source playback with pre-effect freshness/driver checks. Independent native/headless/GPU, rejection and prior-mode gates pass; shader first-string pointer flow and message+38 forwarding are verified separately. Original resolver/material/movement parity and payload ownership remain Unknown. One round complete; workers idle and continuation paused. Research/readiness/function/protocol measures remain separate and unchanged.


## Round63 rays and owned terrain playback

[Round63 integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-05_R63_RAY_QUERY_AND_TERRAIN_PLAYBACK.md) joins actual retained geometry ray queries and proven world/store/current-plan terrain playback. Independent worker/native/headless/GPU, malformed-input cleanup and prior-mode gates pass.104 rays and one actual camera ray accompany60 grounded current-plan steps. Conditional shader comparison and parser payload writer evidence remain separate from unknown asset/material/ownership meanings. Original parity and research/readiness/function/protocol scores are unchanged. Overnight continuation remains authorized until October6 at07:00 Eastern.

## Round64 material, replacement and directional transforms

[Lead acceptance evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R64_MATERIAL_REPLACEMENT_AND_TRANSFORMS.md) records all four independent replays, actual headless/GPU consumption and three rejection gates. Both hosts pass60 grounded steps with one halfway replacement, eight independently verified mip hashes shared by16 patches,240 full transform frames,104 rays and one camera ray. Prior direct terrain and planar playback pass; final builds/default smoke pass. Material association, original sender timing, full-transform world application and original movement/collision parity are outside this accepted boundary.

## Round65 camera target and full correction

[Acceptance evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R65_CAMERA_TARGET_AND_FULL_CORRECTION.md): complete second-string caller175 static guards; actual camera picker36 independent projection/reflection/Fraction probes and103 native checks; grounded point controller111 updates/eight cases/721 checks (136 negative/lifecycle); full-root router eight pose/six serial/190 checks. Main joined headless/GPU89-step traces cross two patches/two chunks, apply non-collinear direct correction, reject duplicate, arrive/stop and drain. Two admission failures and prior Version5 slot mode pass. Original resources and all earlier terrain/material gates remain retained. Thirty final worker hashes match; no original movement/picking/interpolation/material-association parity or overall completion percentage follows.

## Round66 point input and queued correction

[Acceptance evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R66_POINT_INPUT_PAUSE_AND_QUEUED_CORRECTION.md): Assets54 anchors/142 guards/27 independent checks/35 original record witnesses; Rendering69 headless/70 GPU checks with exact independent target casts; Runtime71 pause updates/362 checks/37 negatives plus unchanged111-update65 oracle; Networking eight routes/134 checks/six failures. Joined102 updates per mode include two picks, three paused ticks, retarget and queued off-axis correction/duplicate before steering, two patch/chunk crossings and zero-speed stop. Malformed queue cleanup and unchanged89-step prior host pass. Thirty-one final worker hashes match. Original assets retain accepted resource chains; modern terrain/input/movement policies do not establish original parity or overall completion.

## Round67 viewport input, clear and offline outbound motion

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R67_VIEWPORT_INPUT_CLEAR_AND_OUTBOUND_ROOT.md) composes the actual [unhandled-input Node](TERRAIN_POINT_TARGET_INPUT_HOST_CONTRACT.md), [clear intent](SCENE_WORLD_TERRAIN_POINT_TARGET_CLEAR_CONTRACT.md) and [current-local root builder](SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md). Headless/GPU102 updates include three actual viewport selections, three clear-idle ticks, three paused ticks and queued correction/duplicate before steering. Five offline packets are checked against independent geometry, binary32 trajectory, orientation basis and wire layout. Invalid-clock teardown and unchanged102-update Version2 pass. No original sender units/cadence/sign, live send, original movement/terrain fidelity or overall completion claim follows.

## Round68 manual movement, GUI ownership and offline FIFO

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R68_MANUAL_INPUT_AND_OWNED_OUTBOUND.md) composes actual [WASD input](TERRAIN_MANUAL_INPUT_HOST_CONTRACT.md), [manual direction](SCENE_WORLD_TERRAIN_MANUAL_INPUT_CONTRACT.md) and [owned root queue](SCENE_WORLD_CLIENT_ROOT_MOTION_QUEUE_CONTRACT.md). Headless/GPU110 updates include eight manual intents, two actual GUI-consumed keys, four point picks and nine historical FIFO frames independently checked against geometry, binary32 motion, basis and wire metadata. Changed clock rejection preserves25 completed updates and drains one pending frame; unchanged Version3 passes102 updates. Original resource/material publication remains retained. No original movement/terrain/sender fidelity, live send or overall completion denominator follows.

## Round69 supplied run intent and offline transport join

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R69_RUN_SPEED_AND_OFFLINE_TRANSPORT.md) passes110 headless/GPU updates with four supplied speed changes, three actual Shift events, nine reliable SOE datagrams and nine independent ACK releases. [Speed](SCENE_WORLD_TERRAIN_MOVEMENT_SPEED_CONTRACT.md) changes only owned intent until the next tick; [Shift host](TERRAIN_RUN_INPUT_HOST_CONTRACT.md) tracks independent locations, GUI/focus and callback prefixes; [bridge](SCENE_WORLD_ROOT_MOTION_TRANSPORT_BRIDGE_CONTRACT.md) transfers FIFO bytes only when the whole transport has no pending messages. A27-update clock rejection preserves earlier motion and drains two pending frames. Previous Version4 and both changed core branches pass. This is offline ownership/framing evidence, with no socket/send or original movement/terrain/sender parity claim.

## Round70 camera-relative movement and SOE ingress

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R70_CAMERA_RELATIVE_AND_SOE_INGRESS.md) passes109 headless/GPU updates with three actual orbit inputs, eight camera-relative manual intents, one held-key remap and two inbound SOE applications/two unsent controls. [Projection](TERRAIN_CAMERA_RELATIVE_MOVEMENT_CONTRACT.md) stays engine-neutral; [orbit](TERRAIN_CAMERA_ORBIT_HOST_CONTRACT.md) borrows camera; [ingress](SCENE_WORLD_SOE_APPLICATION_INGRESS_CONTRACT.md) copies to actual pump and transfers controls to caller. A valid SOE/truncated application preserves20 updates before actual pump closes world and all owners drain. Previous Version5 passes110 updates. No original camera/control/terrain parity or live send claim.

## Round71 accepted displacement and disconnect

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R71_ACCEPTED_LOCOMOTION_AND_DISCONNECT.md) passes109 headless/GPU updates with actual grounded displacement driving retained original idle/walk/run frames and two footstep occurrences. [Observer](SCENE_WORLD_TERRAIN_LOCOMOTION_OBSERVER_CONTRACT.md) rebases authoritative correction without teleport distance; [presentation](SCENE_WORLD_ACCEPTED_LOCOMOTION_VISUAL_CONTRACT.md) adds no elapsed clock. Matching offline peer disconnect closes transport before caller scene teardown; wrong-ID failure preserves109 updates and all owners drain. Prior Version6 passes109. Modern selection/scheduling remains explicit, original graph/control/terrain/live parity unclaimed.

## Round72 following-camera join

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R72_FOLLOWING_CAMERA_AND_DISCONNECT_PREFIX.md) passes109 headless/GPU updates using actual accepted player position, native offset and explicit correction snap. [Follow clock](TERRAIN_CAMERA_PIVOT_FOLLOW_CONTRACT.md) and [pivot adapter](TERRAIN_CAMERA_PIVOT_HOST_CONTRACT.md) preserve orbit/input ownership; new follow-only held-direction deadband squared1e-12 avoids float translation jitter. Original supplied poses/two footstep events remain retained. Final outer disconnect queues one reliable application/ACK prefix; explicit caller policy drops copied prefix on scene close, suffix never delivers. Wrong-ID after109 updates drains all owners; prior Version7 passes109. Modern camera/selection policy and original terrain/root/live limitations remain explicit.

## Round73 longer offline session

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R73_LONG_OFFLINE_SESSION.md) passes1000 headless/GPU updates with persistent original supplied poses/17 footstep occurrences, modern following camera, six later viewport target changes, owned controls/status HUD and90 scheduled offline root frames/ACKs. Explicit budget preserves earlier defaults; wrong-ID disconnect drains all owners, prior Version8 passes109. Scripted10 simulated seconds does not establish real-time interactive play or original camera/movement/cadence/live parity. [Budget](TERRAIN_OFFLINE_SESSION_BUDGET_CONTRACT.md), [HUD](TERRAIN_OFFLINE_PLAYGROUND_HUD_CONTRACT.md) and [cadence](SCENE_WORLD_ROOT_MOTION_CADENCE_OUTBOX_CONTRACT.md) remain separate boundaries.

## Round74 elapsed offline scene

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R74_ELAPSED_OFFLINE_HOST.md) passes1000 motion updates with actual separate fixed presentation steps, original supplied poses/17 events/HUD/camera and90 offline protected roots/ACKs. [Planner](TERRAIN_OFFLINE_FRAME_CLOCK_CONTRACT.md) separates reserved slots and explicit time drops from completed motion; [host](SCENE_WORLD_FIXED_STEP_PRESENTATION_HOST_CONTRACT.md) preserves its exact old signature/default proof. [Reusable drain](SCENE_WORLD_OFFLINE_ROOT_MOTION_DRAIN_CONTRACT.md) validates ACK before FIFO take and owns one bridge. Measured GPU593 plans/10,013,260us/no time drop; supplied headless379 plans/exact drop accounting. Rejected first stop ACK preserves one queued frame until caller cleanup; prior Version9 passes1000. Modern offline policy, original actor/camera/cadence/live limits remain explicit.

## Round75 offline session controls

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R75_OFFLINE_SESSION_CONTROLS.md). Actual P pause/resume and Escape finish join passes601 headless/measured GPU updates, original supplied animation/events/camera/HUD and53 scheduled protected roots/ACKs. Final ordinary tick is zero-speed Idle without target or displacement; caller closes owners without injected peer disconnect. Rejected P sink preserves five completed updates and clean failure teardown; prior Version10 passes1000/17/90. Scripted control replay and unrestricted terrain roaming remain separate.

## Round76 idle-start playground

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R76_IDLE_START_PLAYGROUND.md). Separate Version12 idle-start offline playground passes401 headless/measured GPU updates,25 edge holds/reverse recovery,seven original footstep events/39 protected root frames,focus/P/Escape ownership. Unscripted default reaches6000 updates/zero input/60 stationary frames and normal zero stop. Rejected focus preserves40-row/four-frame prefix and clean teardown; unchanged Version11 passes601/9/53. Original/live parity remains unverified.

## Round77 configured playground

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R77_CONFIGURED_OFFLINE_PLAYGROUND.md). Version13 configured idle-start playground passes401 headless/measured GPU updates,32 issued targets and a nonfaulting33rd-click decline,25 edge holds/reverse recovery,seven original footstep occurrences/39 protected offline roots. Unscripted three-update/128-click configuration reaches its exact final zero stop; unchanged Version12 passes401/7/39. Original/live parity remains unverified.

## Round78 configured owners

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R78_CONFIGURED_OWNERS.md). Configured Version13 now owns actual point/controller, limits display, and offline transport/root session through dedicated lifetimes.401 headless/measured GPU updates preserve32 clicks/33rd decline/25 holds/seven original events/39 protected roots. Unscripted3/128 exact stop, pre-world one-bit settings contradiction rejection and unchanged12 replay pass. Original/live parity remains unverified.

## Round79 offline outcome and failure prefix

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R79_OFFLINE_OUTCOME.md). Configured preview now captures primitive actual completion/root-buffer reports and an owned outcome panel.401 headless/measured GPU updates retain32 clicks/33rd decline/25 holds/seven original events/39 protected roots. Separate128-click/129th-decline native join passes. Budget3 reports BudgetCompleted; strict terrain failure preserves225 completed/226 attempted updates/five events/23 roots, reports Faulted rather than successful stop, and drains all owners/actors. Unchanged12 passes. Original/live parity remains unverified. [Runbook](../docs/OFFLINE_PLAYGROUND_RUNBOOK.md).

## Round80 persistent offline restart slice

[Session](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_80_2026-10-06.md) and [joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R80_RESTART_COMPOSITION.md). Four Low milestones add bounded configured-session/root restart owners, a persistent outcome/restart button and the13-byte conditional getter. Lead replays129 runtime checks/37 negatives,92 networking checks,94 headless/95 GPU display checks and19 static guards/eight getter fixtures. Joined headless/GPU each pass60 checks across two completed runs, a real completed1/attempted2 terrain fault and healthy recovery, four handshakes/three exact protected roots, rejected/retried factory and actual GUI replacement. Cleanup failure is terminal; damaged passive subtrees cannot dispatch. Version13 playground remains one-session; full persistent playground wiring is next. Getter returns this+C or01908090 without calls/writes; dynamic type, fallback contents, setter/lifetime remain Unknown. Research89/readiness94/function uncalibrated/2626 names/345 packet rows remain separate and unchanged. Four workers idle after this one round; heartbeat paused.

## Round81 persistent full configured preview

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R81_FULL_PLAYGROUND_RESTART.md). Actual original-resource terrain/actor/input/camera/world children restart only after full drain. Headless/GPU each pass401,401,225/226 strict terrain fault and401 recovery;1428 successful updates/140 protected roots/26 original events. Actual GUI factory rejection/retry and three replacements preserve outcomes until successor readiness; changed heldinput/orbit and clocks/latches/root state reset. Controlled active close through the interactive launcher retains31-update Incomplete prefix/three roots and exits after drain without a successor. Unchanged single-run normal/fault fixtures pass. Failed cleanup is terminal and retains its prefix. Physical OS pointer/close routing and live multiplayer remain unverified.

## Round82 scenario entry and inspection

[Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R82_SCENARIO_PLAYGROUND.md). Strict authored manifest validates bounded UTF8, exact duplicate-free fields, local input existence and explicit options before factory creation; returns defensive run arguments. Independent producer reseeds inherited protected profiles and records private identities. Actual Core/producer join preserves accepted context; original-resource headless/GPU each pass two three-update idle previews/fresh owners/drain/visible disabled cap. Synthetic owned Window.CloseRequested through actual manifest launcher retains31-update Incomplete prefix/three protected roots and drains without successor. Four malformed native launch cases reject before preview admission. New inspection checks52 headless/25 GPU; existing94/95 HUD and25 shell regression separate. Physical OS events, actual terrain fault through new manifest entry, missing-context preparation and live gameplay remain open. Private input existence is not an immutable content pin.
