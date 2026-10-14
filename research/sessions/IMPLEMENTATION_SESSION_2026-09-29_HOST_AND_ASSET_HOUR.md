# Host and original asset implementation hour — 2026-09-29

Started 20:46:18 EDT under the user's request for multiple checkpoints over one
hour; closeout 21:46 EDT. Work is offline implementation and read-only
static asset verification. No original executable execution, socket construction,
server contact, captures, commits, staging or publication occurred.

## Five checkpoints delivered

1. [Accounting and clock](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_HOST_ACCOUNTING.md):
   actual I/O attempt counters, peer/foreign receive separation, failed-send
   counts, pre-send status snapshots and guarded monotonic clock.
2. [Async login host](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_LOGIN_HOST.md):
   one outstanding receive across ticks, pinned IPv4/port, handshake/login
   deadlines, retries, cancellation, sanitized failures, owned completion and
   deterministic cleanup. Concrete UDP adapter compiled only. Godot awaits the
   injected offline host; this transfers the core I/O lifecycle into the engine.
3. [Bounded persisted journal](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_BOUNDED_JOURNAL.md):
   allowlisted JSONL, whole-record byte/event caps, explicit no-overwrite files,
   partial-I/O stop, redaction and temporary-file readback tests.
4. [Original CPU mesh](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_STATIC_MESH_SLICE.md):
   owned unpadded IFF, strict MESH/VTXA geometry slice; original nine batches,
   190 vertices and 159 triangles match independent Python exactly. Corrected
   primitive-version-dependent U32/U16 index widths in the durable spec.
5. [Configured mounts and shader references](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_SHADER_OVERLAY.md):
   explicit owner opens all 48 configured archives; nine original shaders resolve
   to nine DDS envelopes and one effect. A mixed-case raw index name revealed
   an overly strict reader CRC check; raw-name validation and normalized-request
   lookup now preserve that entry without creating an alias.

## Verification and limits

- Full .NET 8 x64 core smoke suite passes, including independent impaired peer,
  1.5 MB/32-trace transport tests, new host/journal/mesh/shader/mount negatives.
- Godot Debug build: zero warnings/errors. Headless `--self-test`: exit 0,
  six sends, seven peer receives, one foreign drop, 18 allowlisted journal events,
  zero journal failures. Existing `Failed to read the root certificate store`
  sandbox diagnostic remains; startup is not claimed error-free.
- Original mesh direct and configured-overlay C# probes pass. Python independently
  returns geometry SHA-256
  `39B94C0F1BAC9FD33633CFDB25CD88C44AC7DBD625017A52677315E325F40800`.
- Independent raw CRC audit: 208 valid records in `stardust_3_02_n.tre`, one
  normalized CRC mismatch caused by a mixed-case raw name, not corrupt bytes.
- Final workspace validator: 711 Markdown files, zero broken links, zero links
  into private evidence; 2,585 provisional rows / zero duplicate addresses;
  345 packet rows, unchanged. `git diff --check` passes. Focused implementation
  and documentation review completed, and `git status --short` was inspected.
  The Git index is empty.
  Existing unrelated/prior dirty files are preserved; generated/private files
  remain ignored. Curated registry counts and research scores remain unchanged.

The host closes login transport on completion and hands the caller an owned
credential clone; it does not connect to zone. Arbitrary injected ports must
honor cancellation. UDP execution, DNS, short-status reply policy and automatic
liveness remain open. Journal persistence is synchronous and bounded.

The asset slice preserves raw geometry and opaque appearance/shader metadata.
It does not decode DDS pixels, bind EFCT passes, convert axes/winding, render
original meshes or establish broad mesh/asset coverage. Godot's visible scene
still uses synthetic geometry. The acceptance record is the living implementation
boundary; 89%/94% research scores are not client completion percentages.

## Exact next checkpoint

Use the winning `texture/stco_tan.dds` from the explicit overlay. Confirm DDS
header, format, mip byte ranges and dimensions, decode to engine-neutral RGBA,
and compare pixels with an independent decoder plus truncation/size negatives.
Then inspect `effect/a_simple.eft`'s selected pass before a Godot adapter for the
already verified mesh. Establish an asymmetric axis/UV/winding fixture before
calling the resulting view compatible. No live-server permission is needed
for this bounded offline asset milestone.

Use the original mesh/member/config identities and commands in checkpoints 4/5;
keep raw assets and any generated previews ignored. Keep status/liveness policy
and zone-state work separate from this asset milestone.

## Canonical handoff

[Implementation acceptance](../../../SWG-Godot-Client/docs/CLIENT_IMPLEMENTATION_ACCEPTANCE.md),
[asset specification](../../docs/ASSET_PIPELINE.md),
[prototype README](../../../SWG-Godot-Client/poc/README.md),
[current status](../../ANALYSIS_STATUS.md) and
[chronological ledger](../../COMPREHENSIVE_ANALYSIS_LEDGER.md) are reconciled.
No new provisional semantic names or packet rows were necessary.
