# Parallel development Round 1 — October 1, 2026

Levarris authorized Round 1. Complete; one milestone per worker, Low reasoning. No further milestone is authorized. Lead reviewed, integrated and validated all four handoffs.

## Setup recovery and baseline

Initial worktree creation requests returned only client IDs and never became usable tasks. They are superseded; do not dispatch to them if they later appear. Local task creation followed by tracked worktree handoff succeeded. Preparation turns were stopped/interrupted by handoff before any implementation. Workers received a full orientation requirement with their assignments.

The main checkout advanced externally to commit 65de8c88141071744dc75594be126b3bab3696a5 during preparation. Lead did not commit. Each active worker starts from that commit in an isolated managed checkout. The older private snapshot remains historical preservation. Command-local safe.directory handles sandbox identity differences; no global Git config is changed.

## Active workers

| Worker | Actual task ID | Checkout | Milestone |
|---|---|---|---|
| Assets | 01a0fa12-c0a9-72e3-a017-107d40faaeae | C:/Users/ancie/.codex/worktrees/520a/Stardust-Client | R1-ASSETS-01 |
| Rendering | 01a0fa12-cc3c-79f1-8056-03cf02336290 | C:/Users/ancie/.codex/worktrees/ed09/Stardust-Client | R1-RENDER-01 |
| Runtime | 01a0fa12-dc27-7020-9e25-07331e9f2b90 | C:/Users/ancie/.codex/worktrees/9175/Stardust-Client | R1-RUNTIME-01 |
| Networking | 01a0fa12-e968-7450-862a-39cc18d4affb | C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client | R1-NET-01 |

## Assignment scopes

### Assets

R1-ASSETS-01: Original human detail1 skeletal resource closure. Existing CreatureAppearanceDescriptorBundleReader.Load already accepts meshDetail; validate detail1 through the ORIGINAL object IFF -> SAT -> MLOD -> selected MGN -> SKTM/bind geometry/material dependencies. Do not implement distance-driven selection or silently pick a fallback LOD. Compare detail0/detail1 selected paths, joint mappings/weights/normal/tangent records, source hashes and source read-once behavior using an independent original-byte audit. Stop after detail1 closure and strict missing/masked/invalid detail/budget gates. Only fix reader defects if directly demonstrated.
Allowed tracked paths: poc/Stardust.Core/Assets/CreatureAppearanceDescriptorBundle.cs (only if a proved defect), new poc/Stardust.Core.SmokeTests/SkeletalLodSelectionScenarios.cs, tools/python/verify_skeletal_lod_selection.py, docs/SKELETAL_LOD_SELECTION_CONTRACT.md, research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_SKELETAL_LOD.md, poc/round1-tests/Assets/**. Existing descriptor/geometry/material docs/tools are read-only leads.
Inputs: object/creature/player/shared_human_male.iff SHA E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243; SAT SHA 951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C; config SHA 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Use explicit aggregate limits when complete material closure exceeds defaults; document policy. Acceptance: own console suite, independent audit of selected original detail1 sources/geometry/materials, Core build, focused diff/link checks.

### Rendering

R1-RENDER-01: Close original selected mouth vertex source and color-output staging. The original mouth PVSH vertex_program/a_specmap_vs20_nodot3.vsh is 1346 bytes SHA AD5D1F09358DA7B9F20A82E126538297C333A502E291CB9F0DD005C9B6B5E408. Establish exact original EFCT/PVSH binding and include graph, calculateDiffuseSpecularLighting(false), ambient/emissive/material-specular placement and primary directional participation. Use existing shared source identities/helpers and select original channels without inventing compiler precision. Add owned bounded source-closure identity/validation for this family and independently audited CPU vertex-color cases including diffuse/specular channel roles. Original compiled HLSL LIT/power300 behavior remains UNKNOWN; owned literal power must be labeled modern. No GPU production change in this milestone.
Allowed: poc/Stardust.Core/Assets/SkeletalVertexLitSpecular.cs, new poc/Stardust.Core/Assets/SkeletalVertexLitLightingSources.cs, new poc/Stardust.Core.SmokeTests/SkeletalVertexLitSourceScenarios.cs, tools/python/verify_skeletal_vertex_lit_sources.py, docs/SKELETAL_VERTEX_LIT_SOURCE_CONTRACT.md, research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_MOUTH_VERTEX_SOURCE.md, poc/round1-tests/Rendering/**. TangentLightingSources.cs, CellLightingPreview.cs and shared registries/runners are read-only. Avoid extracting source into public fixtures. Original human/config pins are in existing specular/locomotion docs and prior session. Acceptance: original binding/hash/include audit, source missing/masked/truncated/changed/oversized gates, hand directional+point channel cases, own verification console, Core build and focused checks.

### Runtime

R1-RUNTIME-01: Implement an engine-neutral bounded animation-event cursor on existing CompressedLocomotion.Messages. Recover no new MSGS grammar: use its already closed integer [start,end) collector and original walk/run records as evidence. Define explicit MODERN playback policy for fractional advance, looping, exact end-frame/frame0 boundary handling, pause/resume/reset and duplicate occurrences. Preserve authored duplicate occurrences but never emit the SAME occurrence twice for the same cycle under repeated ticks. No effects/audio dispatch, root translation, Godot or animation graph changes. Define finite monotonic time/phase guards and per-advance cycle/event budgets; reject invalid/overbudget advances before mutating cursor state. Test partitioning invariance (one interval vs subdivisions), fractional boundary, terminal/frame0 events, duplicate frames/names, several loops, paused/no-progress, backward/NaN, reset and ownership. Original scheduler semantics remain UNKNOWN.
Allowed: new poc/Stardust.Core/Assets/AnimationEventCursor.cs, new poc/Stardust.Core.SmokeTests/AnimationEventCursorScenarios.cs, docs/ANIMATION_EVENT_CURSOR_CONTRACT.md, research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_ANIMATION_EVENTS.md, poc/round1-tests/Runtime/**; existing CompressedLocomotion.cs and clocks are read-only. Own console may resolve original pinned walk/run using existing Core loader; config/member pins in docs/COMPRESSED_LOCOMOTION_CONTRACT.md. Acceptance: own deterministic suite including independence/partitioning properties, original MSGS sample, Core build/focused checks. Report exact API and lead-runner integration call.

### Networking

R1-NET-01: Implement offline application codecs for SelectCharacter (LE64 only after standard header, no helper SWGEmu suffix) and CmdStartScene (raw flag,U64 objectID,ASCII16 terrain,F32 x/z/y,ASCII16 template,U64 time), preserving coordinate order and unnamed flag. Add immutable owned DTO and explicit malformed header/opcode/truncation/string-length/ASCII/trailing/finite-coordinate rejection policies. No zone connection, credential/auth codec, automatic CmdSceneReady or scene application in this milestone. Correlate EXACT client-backed durable specs and server boundary; do not trust the old sequence diagram's ordering when prose corrects it. Preserve raw terrain/template strings as protocol data; no filesystem normalization. Keep forward-compatible flag semantics unknown. Add deterministic hand-authored golden byte fixtures and INDEPENDENT Python wire oracle; include high-bit U64s and unequal X/Z/Y. Do not conflate selected-object state validation with byte parsing.
Allowed: new poc/Stardust.Core/Protocol/ZoneSceneProtocolCodec.cs, new poc/Stardust.Core.SmokeTests/ZoneSceneProtocolScenarios.cs, tools/python/verify_zone_scene_protocol.py, docs/ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md, research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_ZONE_SCENE_CODECS.md, poc/round1-tests/Networking/**. Existing protocol readers, login state, shared packet maps/registry are read-only; lead reconciles shared documents after review. Evidence: docs/LOGIN_AND_ZONE_STATE_MACHINE.md, docs/NETWORK_ARCHITECTURE.md, docs/PACKET_HANDLER_MAP.md, curated packet rows, corresponding Stardust-3 serializer/parser. Hash-check baseline EXE before relying on VAs. Acceptance: own console positive/negative fixtures, independent Python oracle, Core build and focused checks. Propose runner call.


## Lead gates

Shared Program.cs, root status/ledger/navigation, project-wide shared integration and curated registries remain lead-owned. Integrate only baseline-relative worker changes, preserving main changes. Use worker-specific test consoles first, then register smoke tests as appropriate and rerun combined suites. No source/environment publication, original executable execution, network socket/DNS contact, commits or staging are authorized. Close the round only after every worker is accepted/incomplete/blocked and combined validation is recorded.

## Status

All four workers completed their assigned milestone and are idle. All four handoffs accepted in the main checkout. No next milestone was sent.

## Accepted outcomes and integration

- **Assets R1-ASSETS-01:** [Contract](../../docs/SKELETAL_LOD_SELECTION_CONTRACT.md). Production reader needed no change. Original detail0/detail1 each own 29 sources; detail1 has 968 positions / 1,904 triangles, 4,167,914 bytes. Explicit level0 skeleton binding is independently checked. Distance selection and automatic skeleton coupling remain open.
- **Rendering R1-RENDER-01:** [Contract](../../docs/SKELETAL_VERTEX_LIT_SOURCE_CONTRACT.md). Owned mouth source identity validates four sources / three edges / 19,401 bytes. Independent binding and 64 modern CPU cases pass, maximum error 3.4835412372480334e-6 at 1e-5. Original compiled LIT/exponent behavior remains unknown; production GPU equations unchanged.
- **Runtime R1-RUNTIME-01:** [Contract](../../docs/ANIMATION_EVENT_CURSOR_CONTRACT.md). Engine-neutral event cursor preserves authored duplicates and occurrence identities, bounds work and rejects invalid advances atomically. Independent 160-target partitioning oracle and original walk/run footstep records pass. Modern interval policy is explicit; original scheduler and effect/audio delivery remain open.
- **Networking R1-NET-01:** [Contract](../../docs/ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md). SelectCharacter encoder/decoder and CmdStartScene decoder preserve raw flag, full U64s, strings and X/Z/Y. Independent wire fixtures and malformed inputs pass. Scene lifecycle, connection and automatic scene-ready remain unimplemented.

Lead copied only reviewed baseline-relative changes. No commits, staging, pushes, socket contact or original-executable execution. New protocol/event/mouth math scenarios run in the shared native suite. Integration fixed a nullable Assets test reference caught by the shared warnings-as-errors project. Mouth source metadata is emitted once; scene and descriptor audits check the graph and 64 CPU cases independently. Descriptor verification also now consumes the existing tangent graph metadata; its previous expected-source set omitted the bump VSH. Auditors retain historical configuration counts and require one mouth identity marker. Shared includes mean the full scene adds only one VSH / 1,346 bytes: 160 sources / 16,643,480 bytes, read once.

## Lead verification

All milestone console suites and independent audits passed in the main checkout, including original Runtime walk/run and original Assets detail1. Rendering tests include missing/masked/truncated/changed/oversized and binding rejection. The shared native suite passes inside the original interior and descriptor runs. Godot build passes with zero warnings/errors. The combined original interior runs 2,880 ticks, 24 transfers and 182 portal-blocked contacts; independent bytes/source/math/floor/owner audit passes. A fresh GPU capture was inspected: original character, room, door and chase controls render; existing GPU fixtures and retained-resource assertions pass. Headless first-lookup cancellation passes. Original compiled mouth LIT remains unknown despite replacement tests.

Detailed exports, native/independent logs, GPU capture and cancellation logs remain regenerable and ignored. Public artifacts retain authored contracts, aggregate counts and hashes. Final workspace validation is recorded below.

## Resume point

Round 1 is complete; workers remain idle. Levarris must authorize another round before new assignments. Suggested planning boundaries: explicit lower-detail rendering with a justified skeleton policy; bounded diagnostic delivery of modern event batches; offline zone scene state transitions; next mouth lighting integration slice while preserving compiled-LIT uncertainty. These proposals have not been dispatched. Research/function/protocol scores and denominators are unchanged.

## Final workspace checks

Workspace validator passes: 1,014 Markdown files, zero broken links, zero links into private evidence, 2,602 canonical names / zero duplicate addresses, and 345 packet rows. `git diff --check` passes; the index is empty. Main HEAD remains 65de8c88141071744dc75594be126b3bab3696a5. Only reviewed milestone and lead integration/documentation files are changed.

GPU and cancellation logs contain a Godot startup root-certificate-store warning/error message; both bounded offline runs continue and pass their assertions. No certificate/network setup was changed. This message does not establish a rendering or cancellation failure, and execution logs are not described as error-free.
