# Client implementation acceptance record

Updated: 2026-09-29. This record describes executable implementation checks,
separately from research coverage, address naming, and the server packet inventory.
No overall client completion percentage is assigned.

## Verified boundaries

| Boundary | Implemented behavior | Evidence / practical limit |
|---|---|---|
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
| Configured mounts/references | Explicit TREE/0005 mount owner; raw index keys and normalized requests; bounded SSHT/TXM external references | All 48 configured archives opened; one mesh's 9 shaders, 9 DDS envelopes and 1 EFCT resolve; no pixel/material decoding |
| Godot hosting | Injected async login host transcript runs headlessly | Build and exit 0; root-certificate-store diagnostic remains in sandbox |

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
