# Stardust client research: start here

This is the canonical entry point for reviewing the `Stardust.exe` research before designing or implementing a replacement client.

Latest development session: [Round60 outcomes and resume point](research/sessions/PARALLEL_DEVELOPMENT_ROUND_60_2026-10-04.md).
Latest accepted parallel round: [Round60 assignments and acceptance](research/sessions/PARALLEL_DEVELOPMENT_ROUND_60_2026-10-04.md). Levarris requested completion of Round60 then a stop for weekly usage; continuation is paused. See the [daytime record](research/sessions/DAYTIME_DEVELOPMENT_2026-10-04.md) and [overnight handoff](research/sessions/OVERNIGHT_DEVELOPMENT_2026-10-03_TO_04.md).
Earlier accepted parallel round: [Round 3 results and integration record](research/sessions/PARALLEL_DEVELOPMENT_ROUND_3_2026-10-01.md); [Round 2 accepted results](research/sessions/PARALLEL_DEVELOPMENT_ROUND_2_2026-10-01.md).
Current executable acceptance gates: [implementation record](docs/CLIENT_IMPLEMENTATION_ACCEPTANCE.md).
Latest research checkpoint: [queue entry producer](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R60_QUEUE_ENTRY_PRODUCER.md).
Latest follow-up: [direct status CRC receive rule](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-29_STATUS_CRC_RECEIVE.md).
Latest implementation checkpoint: [owned terrain and paired current-speed composition](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R60_OWNED_TERRAIN_AND_CURRENT_SPEED_COMPOSITION.md).
Latest collision research: [creature fallback dimensions](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md).

## Scope and current position

- Primary executable: `C:\Stardust-DEV\Stardust.exe`
- SHA-256 baseline: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
- Server correlation source: the local Stardust/Core3 source tree
- Canonical workspace: `C:\Users\ancie\Documents\GitHub\Stardust-Client`
- Objective: behavioral, wire-protocol, and original-asset compatibility in a native 64-bit replacement client
- Current evidence-backed baseline: **89% comprehensive behavioral coverage / 94% implementation-and-engine-decision research readiness**. These are weighted observable-contract scores, not function percentages. Function/address coverage is separately **uncalibrated**. See the [current rescore](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_EVIDENCE_BASED_COVERAGE_RESCORE.md), [measurement reset](RESEARCH_COVERAGE_REBASELINE_2026-09-25.md), and [model-transition audit](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_MODEL_TRANSITION_AUDIT.md).
- Server packet-source matrix: **345 documented / 0 reachable gaps**
- Stable provisional-name registry: **2,626 distinct addresses / 0 duplicate rows** (current canonical registry)

The user has selected Godot 4.7.2 .NET x64 as the host after the bounded
feasibility proof passed. Work is now development-led: implement the smallest
fixture-backed compatibility slice, and return to targeted executable/server
analysis only when a concrete implementation uncertainty blocks or risks it.
Function-level accounting remains separate from behavioral readiness.

Parallel development: [adopted operating rules](docs/PARALLEL_DEVELOPMENT_RULES.md). Rounds4 through60 are accepted. [Latest four-round assessment](research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_9_12_ASSESSMENT_2026-10-02.md). [Five-round outcomes and next milestone](research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_4_8_ASSESSMENT_2026-10-02.md). Overnight development stopped October4 at05:00 Eastern; see [outcomes and prepared next assignments](research/sessions/OVERNIGHT_DEVELOPMENT_2026-10-03_TO_04.md). [Prior organizational assessment](research/sessions/PARALLEL_DEVELOPMENT_ROUNDS_2_3_ASSESSMENT_2026-10-01.md).

## Recommended review order

1. [Executable overview](docs/EXECUTABLE_OVERVIEW.md)
2. [Current analysis status](ANALYSIS_STATUS.md)
   [Workstream inventory and deduplication gate](WORKSTREAM_INVENTORY.md) identifies closed boundaries, named open questions, and evidence required before counting a new block.
   [Research coverage measurement reset](RESEARCH_COVERAGE_REBASELINE_2026-09-25.md) is the current priority and score policy.
   [Latest vtable methodology audit](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_TEN_VTABLE_AUDIT_BLOCKS.md) corrects mixed export attribution and identifies executable slot targets missing from the Ghidra inventory; it is one audit with ten findings, not ten behavioral blocks.
3. [Client architecture](docs/CLIENT_ARCHITECTURE.md) and [client subsystems](docs/CLIENT_SUBSYSTEMS.md)
4. [Network architecture](docs/NETWORK_ARCHITECTURE.md), [SOE transport](docs/SOE_TRANSPORT_PROTOCOL.md), and [packet-handler map](docs/PACKET_HANDLER_MAP.md)
5. [Replication schemas](docs/REPLICATION_SCHEMA_MAP.md) and [object-controller protocol](docs/OBJECT_CONTROLLER_PROTOCOL.md)
6. [Asset pipeline](docs/ASSET_PIPELINE.md), [rendering](docs/RENDERING_ARCHITECTURE.md), [terrain](docs/WORLD_TERRAIN_ARCHITECTURE.md), and [platform/input/audio](docs/PLATFORM_INPUT_AUDIO_ARCHITECTURE.md)
7. [Unknown-function ledger](UNKNOWN_FUNCTIONS.md) and [uncertainty audit](docs/FUNCTION_UNCERTAINTY_AUDIT.md)
8. [64-bit port plan](docs/64BIT_PORT_PLAN.md) and [engine-decision requirements](docs/ENGINE_DECISION_REQUIREMENTS.md)
9. [Latest overnight session index](research/sessions/ANALYSIS_SESSION_2026-09-23_TO_2026-09-24_0500_INDEX.md) and [report](research/sessions/ANALYSIS_SESSION_2026-09-23_TO_2026-09-24_0500_REPORT.md)

## Workspace map

| Path | Purpose | Intended for Git |
|---|---|---:|
| Root control documents | Current status, ledgers, navigation, hygiene | Yes |
| `docs/` | Durable subsystem and protocol specifications | Yes |
| `research/checkpoints/` | Bounded evidence reports | Yes |
| `research/sessions/` | Consolidated session reports and resume points | Yes |
| `research/archive/` | Superseded historical reports retained for provenance | Yes |
| `data/curated/` | Canonical machine-readable registries | Yes |
| `tools/` | Reproducible analysis and verification scripts | Yes, after license/path review |
| `poc/` | Existing bounded Godot feasibility proof | Yes, after dependency review |
| `local-private/` | Ghidra projects, toolchains, decompilation, raw extracts, generated corpora, manifests | **No** |

See [WORKSPACE_HYGIENE.md](WORKSPACE_HYGIENE.md) for maintenance rules and [PUBLICATION_GUIDE.md](PUBLICATION_GUIDE.md) before initializing or pushing a repository.

## Confidence vocabulary

- **Confirmed**: directly supported by executable code, wire codec, server source, or asset bytes.
- **Strongly inferred**: multiple consistent evidence sources, but one boundary remains indirect.
- **Tentative**: plausible interpretation requiring more evidence.
- **Unknown**: no semantic claim should be made yet.

## Current implementation milestone

The engine-neutral .NET 8 core now completes an offline SOE session-to-login
transcript through cluster and character enumeration, hosted by Godot in
headless mode. It includes fixture-backed session codecs, seeded CRC,
selector-4, bounded zlib receive, four reliable channels, ACK/reorder/retry,
multipayload, and fragmentation. See [prototype status](poc/README.md) and the
[Phase 1 status](docs/64BIT_PORT_PLAN.md#implementation-status-2026-09-27).

The 2026-09-29 increment hardens corruption/failure recovery, adds queued
fragmentation and independent peer scenarios, outer multipacket dispatch,
strict configuration loading and redacted diagnostics. An engine-neutral TRE
0005 reader also reproduces an original entry's Python-verified bytes.

The injected socket/clock/journal adapter is implemented. Live networking still
requires status/liveness policy and a separately authorized controlled-server
run. One original textured static mesh now renders in an explicit Godot preview.
Parent appearances and all-four skeletal details are implemented in bounded original previews. Stable-incarnation state, typed baseline/scalar and atomic mixed/components transactions, acknowledged two-actor presentation and generic creature/static world delivery are accepted. A mixed original human/terminal preview now passes guarded lifecycle/GPU/headless/cancel checks. Reusable acknowledged heterogeneous presentation is implemented and its combined original/native/GPU/headless/cancel checks pass. Continuous bounded scene pumping is implemented and its joined original/native/GPU/headless/cancel checks pass. Native pose/wire/body joins also pass. Input locomotion, unified ingress and interior hosting are next; modern DDS completion extensions remain separately scoped.
See the acceptance record and latest session for verified scope and limits.

Latest material milestone: [original skeletal texture defaults](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_MATERIAL_DEFAULTS.md).

Latest scene milestone: [textured animated interior character](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_MATERIALS.md).

Latest bounded implementation: [original blueprint face description](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_BLUEPRINT_FACE_DESCRIPTION.md); [completed run report](research/sessions/IMPLEMENTATION_SESSION_2026-10-01_UNTIL_0700.md) gives the next shader/face execution probe.
