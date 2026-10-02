# 64-bit Replacement Plan

## Host and asset increment — 2026-09-29

A cancellable engine-neutral login host now binds guarded monotonic time,
I/O accounting and bounded safe JSONL persistence. Godot runs injected offline
I/O; the IPv4 UDP adapter is compiled but unexecuted. One original CPU mesh
matches Python and its dependencies resolve through the configured archive set.
Next: independently verified DDS pixels/mips and one material/axis adapter.
See the [session resume point](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_HOST_AND_ASSET_HOUR.md).

## Feasibility answers

1. **Architecture reconstruction:** the principal platform, transport, session, replication, scene, renderer, asset, UI, input and audio boundaries are address-backed in the verified executable. The normalized registry has 2,198 stable provisional function addresses, but this is not exhaustive function-by-function recovery. Specialized presentation policy and failure/lifetime tails remain open.
2. **Protocol recovery:** the current Stardust server-source matrix classifies 345 packet definitions with zero reachable inventory gaps; this does not mean every optional client handler is fully characterized. SOE transport and key client dispatch paths have been reconstructed, but server-generated/live-server golden vectors and deployed-version checks still remain.
3. **Asset dependence:** world, appearance, animation, terrain, shaders, UI, effects, and audio are deeply asset-dependent. The networking POC is not.
4. **Conceptual reuse:** session sequencing, wire messages, template CRCs, replication views, containment/cell/scene concepts, and virtual paths. Redesign platform, allocation, jobs, renderer, UI internals, and resource ownership.
5. **Major blockers:** remaining long-tail asset/presentation semantics, unresolved-resource failure behavior, exhaustive ownership and optional workflow auditing, original-asset provenance/redistribution review, and validation against the actually deployed server build/configuration. The packet inventory is closed, while operational wire tests and fidelity tests still remain.
6. **Renderer/engine:** the user selected Godot 4.7.2 .NET x64 as the host after
   its bounded feasibility proof passed with an engine-neutral .NET 8 core and
   Direct3D 12 Forward+. Protocol, state, simulation, and original-asset logic
   remain outside Godot behind adapter boundaries. See `RENDERING_ARCHITECTURE.md`.
7. **Smallest POC:** a headless-capable x64 Windows executable that initializes logs/config, establishes the transport session, sends the minimum login message to a controlled Stardust test server, parses one known reply, records a deterministic structured log, disconnects, and exits cleanly.

## 32-to-64-bit hazards

- Never serialize native `size_t`, pointer, enum layout, `long`, or compiler-packed runtime structs.
- Define every wire field with fixed-width types and explicit little-endian readers/writers.
- Preserve 64-bit SWG object/network IDs exactly; do not widen 32-bit account IDs, opcodes, CRCs, lengths, or sequence values on wire.
- `Stardust.exe` stores `EnumerateCharacterId` object IDs as an explicit 8-byte member at record offset `+0x10` even though the process is 32-bit. Model this as `uint64_t NetworkObjectId`, never as a pointer-sized integer.
- Treat four-character tags/CRCs as `uint32_t`, not pointer-sized values.
- Do not mirror original vtable/object memory layouts in new code.
- Convert archive offsets and uncompressed sizes to checked 64-bit internal arithmetic while validating format-specified 32-bit bounds.
- Replace pointer truncation and pointer-as-ID patterns with typed handles.
- Parse packed file/network structures field-by-field to avoid host padding/alignment.
- Bound all length prefixes before allocation; checked-add embedded account/token sizes.
- Decode customization v1 as an explicitly byte-oriented format: 8-bit variable IDs/count, escaped byte values and optional signed 16-bit values. Never widen its fields or replace its `FF 01`/`FF 02` escapes and `FF 03` terminator with native serialization.
- Account for float/quaternion and X/Z/Y ordering explicitly; x64 does not justify changing them.
- Make allocator and concurrency behavior independent of legacy `MemoryBlockManager` layout.

## Pre-build engine decision gate

**Status: completed.** After the broader client analysis and bounded feasibility
proof, the user selected Godot 4.7.2 .NET x64 as the host. The comparison below
is retained as the decision record; implementation must continue to enforce its
engine-neutral compatibility boundaries. See [the archived historical pre-build
report](../research/archive/superseded-progress-reports/FINAL_PREBUILD_ANALYSIS_REPORT.md),
[current status](../ANALYSIS_STATUS.md), and `ENGINE_DECISION_REQUIREMENTS.md`.

After the protocol, replication, object lifecycle, asset formats, world/cell model, animation, UI/input and audio dependencies are sufficiently characterized—and before substantial replacement-client implementation—compare at least:

- a custom native client with a narrow DX12 renderer;
- Godot as a host engine, using native integration only where protocol/resource workloads require it;
- a custom Vulkan client if cross-platform delivery has become a concrete requirement;
- any other mature engine that demonstrably satisfies the recovered compatibility constraints.

The comparison must use a written evidence matrix covering: native x64 support; ability to own the SOE transport and fixed-width codecs; deterministic lifecycle ordering; background I/O; custom TRE/IFF and SWG asset import/runtime loading; large-world coordinates; cell/portal visibility; skeletal animation customization; terrain; shader/material translation; UI flexibility; Windows debugging/profiling; licensing and redistribution; source-level escape hatches; upgrade risk; and long-term maintenance cost.

Godot must be evaluated as an architectural option, not assumed to be either a drop-in solution or unsuitable. The decisive question is whether its scene/resource/rendering model reduces implementation risk without forcing protocol, object replication, asset identity or world-streaming behavior into brittle engine-specific shapes. The narrow networking POC may still be implemented as an engine-independent executable/library after this gate, but no build begins until the user approves the resulting decision.

The transform boundary is no longer an engine-evaluation unknown. Stardust uses
Y-up/+Z-forward transforms and `[w,x,y,z]` quaternions; Godot uses Y-up/-Z-
forward. A single Z reflection maps positions/directions, while quaternion
construction uses `[-x,-y,z,w]`; authored triangle indices are preserved after reflection and tangent
handedness flips once. See [geometry facing](GODOT_GEOMETRY_FACING_CONTRACT.md). Principal-axis and mixed-pose matrix fixtures pass. Keep original assets
in engine-neutral SWG space and apply this conversion only at the Godot adapter.

Likewise, Godot's animation facilities must sit behind a compatibility graph,
not replace it directly. The client invalidates a per-frame pose cache, advances
ordered logical/physical controllers, accumulates root motion per substep and
emits animation messages synchronously before installing returned/queued
replacement animation. A Godot `AnimationTree` may implement blending, but the
track handles, transition queue, message order and root-motion contract remain
owned by the compatibility layer.

## Proposed phases

### Phase 0 — evidence and reproducibility

Hash artifacts, preserve tool versions, recover call graphs/RTTI/vtables, catalogue message names and server constructors, and document uncertainty. No production client code.

### Phase 1 — x64 networking POC

Build platform logging/config, UDP/socket abstraction, the source-specified SOE transport, minimal application codec, and one deterministic login response parser. No assets, renderer, UI, characters, or gameplay. Implement from [SOE_TRANSPORT_PROTOCOL.md](SOE_TRANSPORT_PROTOCOL.md) and verify primitives against [SOE_TRANSPORT_TEST_VECTORS.md](SOE_TRANSPORT_TEST_VECTORS.md), followed by server-generated/live-server vectors.

### Phase 2 — protocol/session harness

Login token, clusters, character enumeration, zone authentication, permissions, selection, scene-start/ready sequencing; transcript tests against a controlled server.

### Phase 3 — replicated object model

Scene create/destroy, containment, baseline/delta framework, initially only enough schemas to log a selected player and known objects. Unknown fields remain preserved/diagnosed.

### Phase 4 — resource core

TRE mounts, IFF primitives, object templates, data tables, typed cache, fuzz/bounds tests.

### Phase 5 — minimal world visualization

Use the engine/renderer selected at the pre-build decision gate for a window, placeholder geometry, camera, transforms, then terrain/static appearances. Do not block protocol progress on visual fidelity.

### Phase 6+ — animation, cells/portals, effects, audio, UI, gameplay breadth

Add vertical slices with trace comparisons and compatibility tests.

## Concrete Phase 1 specification

### Implementation status (2026-09-27)

The engine-neutral .NET 8 core under `poc/Stardust.Core` now implements and
tests the offline portion of this phase: exact SessionRequest/SessionResponse,
seeded CRC widths 1–4, selector-4 transformation, uncompressed send framing,
bounded zlib receive framing, four-channel sequence retention/reorder/ACK and
retransmission, 16-bit wrap, 1/3/7-byte sizes, multipayload expansion, and
bounded inbound/outbound fragmentation. It also matches the documented
`LoginClientId` datagram and parses the synthetic `LoginClientToken` and
`ErrorMessage` fixtures while preserving credential bytes. Cluster list/status
fixtures and a bounded `EnumerateCharacterId` parser retain separate zone/ping
ports, explicit LE64 object IDs, and strict UTF-16LE names. A socket-independent
client transport drives the Godot-hosted offline transcript through these four
login responses. A socket-independent coordinator now exposes the complete
tick/receive/outbound flow with validated endpoint-redacted options.

At that checkpoint, still open in Phase 1: the actual UDP/config-loading/clock adapters, endpoint
resolution, disconnect handling, outbound compression policy, structured
redacted logging sinks, and controlled-server validation.
No live endpoint has been contacted.

### Implementation update (2026-09-29)

The core adds framing-corruption recovery, failure/deadline cleanup, queued
fragmentation across ACK windows, outer SOE multipackets, ping and matching
peer disconnect, strict JSON configuration and redacted diagnostic events.
Independent peer scenarios exercise these paths. The resource-core slice also
starts TREE 0005 mounting/extraction against one original archive; it does not
establish mesh/texture rendering or complete Phase 4.

Current open Phase 1 gates: status/liveness and CRC exceptions, actual UDP
ownership/endpoint filtering, clock and journal binding, outbound compression
policy, and a separately authorized controlled-server run. See the
[implementation acceptance record](CLIENT_IMPLEMENTATION_ACCEPTANCE.md) and
[session handoff](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_ONE_HOUR.md).

### Inputs

- Config: host, login port, client version, test credential source, timeout, log path.
- Credentials must not be written to logs or embedded in binaries.
- Test only against a server/account the operator controls.

### Required behavior

1. Launch as native x64 and log build/OS/config (with secrets redacted).
2. Initialize Winsock and a monotonic clock.
3. Resolve the configured login endpoint.
4. Establish the minimum SOE transport session. Model four channel indices even if the POC uses channel zero; derive payload limits from negotiated datagram size and trailer/transform overhead; preserve the one/two-byte SOE opcode prefix during transforms; compute the confirmed seeded CRC-32 and append its negotiated big-endian width.
5. Encode and send LoginClientID `0x41131F96` using exact fixed-width wire rules.
6. Receive, validate bounds, and identify one expected server response—prefer `LoginClientToken`; accepting a deliberate `ErrorMessage` is an earlier smoke checkpoint, not final POC success.
7. Parse the response fields into typed values, redact token/credential material, and emit opcode/name/field-length/result logs.
8. Perform protocol/socket shutdown and exit zero on success, nonzero with categorized error otherwise.

For the complete login-success milestone, parse all four application responses into owning x64 types: preserve the LoginClientToken credential blob byte-for-byte; key clusters by fixed-width `uint32_t` galaxy ID; store hostname, zone port and ping port separately; and store character object IDs as wire-defined `uint64_t`. Parse Unicode names by explicit little-endian code-unit count. Do not reproduce the original client's 0x14/0x2C/0x20 record memory layouts.

The initial happy path may emit selector-1 framing with compression flag `0`, avoiding dependency on byte-identical historical deflate output while remaining compatible with the negotiated transport. It must still implement selector-4 seed-chain transformation and the two-byte seeded CRC. Zlib decompression is required before accepting compressed server responses; server configuration should be recorded because the checked-in default disables server send compression, but deployed properties may differ.

For the first successful exchange, implement channel-zero data (`0x0900`), fragment (`0x0D00`), out-of-order (`0x1100`), acknowledgement (`0x1500`), and multipayload (`0x1900`) handling with the confirmed 1/3/7-byte embedded-length codec. Retain immutable prepared datagrams by logical sequence until cumulative ACK. A minimal happy-path test may avoid fragmentation and reordering, but those paths must exist before transport compatibility is considered complete.

### Explicit non-goals

No launcher, patching, credential UI, cluster UI, zone connection, assets/TRE, rendering, audio, input, gameplay, character selection, or persistence.

### Acceptance tests

- x64 process verified by PE header/CI.
- Golden-byte codec tests for LoginClientID and chosen response.
- Golden transport vectors for session response parameters, seeded CRC-32 widths 1-4, channel-zero data/ACK, multipayload length boundaries `0xFD`, `0xFE`, `0xFFFE`, `0xFFFF`, and first/following fragments.
- Truncated, oversized, wrong-opcode, timeout, and connection-refusal tests fail safely.
- A controlled-server integration run reaches and parses the selected reply.
- Repeated connect/disconnect runs produce no leaked sockets/threads under diagnostics.
- Logs contain no password or full session token.

## Risk controls

Keep protocol codecs in a standalone library with golden fixtures; keep renderer absent from early milestones; keep asset decoding sandboxable and bounds-checked; document server revision alongside every capture; and do not normalize uncertain fields into guessed semantics.

The environment clock is a concrete 32-to-64-bit trap. The wire time is and remains LE64, but legacy GroundScene separately computes a signed-looking offset from only the low U32 and a 32-bit local wall clock. Do not reproduce that truncation in the new architecture. Parse to `uint64_t`, calculate modulo/corrections in full-width integer or double precision, use a monotonic local clock for frame advancement, and isolate any wrap-compatibility test behind the protocol layer. PTAT cycle duration is a serialized float read field-by-field, not a pointer-sized or packed host member.

Particle descriptions require the same field-wise treatment. Legacy `ParticleEffectAppearance`, emitter and 0xA8-byte particle-record sizes are not disk contracts. Preserve PEFT time/scale composition, EMTR orientation modes, pool-admission priority and deterministic spawn/drop decisions while replacing 32-bit pointers, copied vector layouts and the global fixed-record particle pool.

CLEF records make the boundary concrete: `CLGT` occupies **23 bytes on disk**
but 24 bytes in the x86 runtime vector due to a color-byte alignment pad;
`CPAP` uses a 40-byte runtime entry containing a 32-bit appearance handle,
while its versioned disk leaf is a NUL path plus explicit scalar/byte fields.
Decode each authored field, preserve literal logical paths (including a
trailing space when present), and construct native x64 objects separately.
Do not memcpy x86 vectors or trim a path to force an unintended asset lookup.
An enabled PTEX empty name is a defined fallback-material case, distinct from
a missing named resource.

## Textured preview increment (2026-09-29)

Engine-neutral DDS and owned material graphs feed an explicit original Godot preview with byte-verified uploads and GPU coordinate tests. See the [session](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_TEXTURED_PREVIEW_HOUR.md). Next asset gate is parent appearance/LOD loading; faithful lighting and BC7/HDR extensions remain bounded follow-ups.

## Overnight implementation progression

Owned DTLA parsing, explicit authored LOD inspection, strict APT exterior loading, dimensioned auxiliary coordinates and verified BC7 are now implemented. See the [overnight session](../research/sessions/IMPLEMENTATION_SESSION_2026-09-29_TO_2026-09-30_OVERNIGHT.md). Bounded CMPA neutral geometry composition now passes; the garage sorted-index mesh boundary now passes; shared material composition now passes; next is APPR/PIVT distance origin; BC6H requires HDR representation. Live networking and faithful full materials remain separate gates.


## APPR/PIVT camera selection increment — 2026-09-30

The [camera LOD checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_PIVT_CAMERA_LOD.md) closes the primary extent distance origin: false/absent pivot uses the transformed authored sphere center, true uses world translation, and NULL uses zero local offset. All 6,198 original primary extents agree independently. Native malformed/transform fixtures and standalone/composed headless/GPU selection gates pass. The inspector supports opt-in camera distance and manual selection. Original DPVS, lazy residency and full lighting remain deferred. Five confirmed address labels bring the canonical registry to 2,590 distinct entries; research scores and packet counts are unchanged.


## BC6H HDR extension — 2026-09-30

The [HDR checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md) adds a separate owned RGB half output for typed signed/unsigned BC6H. Forty standards-only fixtures/264 mips match independent Godot bcdec bit for bit and pass real GPU half upload/readback. Native endpoint, truncation, surface and budget gates pass. Static material integration and HDR presentation remain separate; this is not original executable acceptance evidence. Existing pinned dependencies, research metrics and registry counts are unchanged.


## APPR hardpoints — 2026-09-30

The [hardpoint checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_HARDPOINTS.md) adds owned exact names/CRC/transforms and explicit duplicate queries to mesh/detail/component templates. All 31,151 effective APPR bases/13,253 hardpoints agree independently. The old serialized count/name-prefix claim is corrected. Ninety-one duplicate-name groups contain different transforms; ambiguous attachment choice remains unverified. Native and focused host gates pass; metrics/registry counts are unchanged.


## Owned portal layout — 2026-09-30

The [portal reader checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LAYOUT_READER.md) closes native deployed PRTO/CELL/PRTL parsing against all 291 layouts, 3,470 cells and 7,642 portal references. Owned geometry, names/door fields, optional bounds and paired zero-based adjacency pass independent agreement and negative fixtures. Scene loading, floor behavior and runtime visibility remain separate. The older 00B43E80 neighbor-helper attribution is corrected to a byte toggle. Metrics and registry counts are unchanged.
