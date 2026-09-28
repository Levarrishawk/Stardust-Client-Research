# Stardust client research: start here

This is the canonical entry point for reviewing the `Stardust.exe` research before designing or implementing a replacement client.

## Scope and current position

- Primary executable: `C:\Stardust-DEV\Stardust.exe`
- SHA-256 baseline: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
- Server correlation source: the local Stardust/Core3 source tree
- Canonical workspace: `C:\Users\ancie\Documents\GitHub\Stardust-Client`
- Objective: behavioral, wire-protocol, and original-asset compatibility in a native 64-bit replacement client
- Current evidence-backed baseline: **89% comprehensive behavioral coverage / 94% implementation-and-engine-decision research readiness**. These are weighted observable-contract scores, not function percentages. Function/address coverage is separately **uncalibrated**. See the [current rescore](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_EVIDENCE_BASED_COVERAGE_RESCORE.md), [measurement reset](RESEARCH_COVERAGE_REBASELINE_2026-09-25.md), and [model-transition audit](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_MODEL_TRANSITION_AUDIT.md).
- Server packet-source matrix: **345 documented / 0 reachable gaps**
- Stable provisional-name registry: **2,528 distinct addresses / 0 duplicate rows** (2026-09-24 session closeout)

The user has selected Godot 4.7.2 .NET x64 as the host after the bounded
feasibility proof passed. Work is now development-led: implement the smallest
fixture-backed compatibility slice, and return to targeted executable/server
analysis only when a concrete implementation uncertainty blocks or risks it.
Function-level accounting remains separate from behavioral readiness.

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

The remaining first executable milestone is the bounded socket/config/logging
adapter and a separately authorized controlled-server run. It still requires
no world rendering, character rendering, gameplay UI, or asset conversion.
