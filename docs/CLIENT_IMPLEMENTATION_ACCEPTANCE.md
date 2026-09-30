# Client implementation acceptance record

Updated: 2026-09-29. This record describes executable implementation checks,
separately from research coverage, address naming, and the server packet inventory.
No overall client completion percentage is assigned.

## Verified boundaries

| Boundary | Implemented behavior | Evidence / practical limit |
|---|---|---|
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
