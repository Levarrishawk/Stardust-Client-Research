# Stardust client research: start here

This is the canonical entry point for reviewing the `Stardust.exe` research before designing or implementing a replacement client.

## Scope and current position

- Primary executable: `C:\Stardust-DEV\Stardust.exe`
- SHA-256 baseline: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
- Server correlation source: the local Stardust/Core3 source tree
- Canonical workspace: `C:\Users\ancie\Documents\GitHub\Stardust-Client`
- Objective: behavioral, wire-protocol, and original-asset compatibility in a native 64-bit replacement client
- Implementation-grade replacement research: **96%**
- Comprehensive whole-client research ledger: **90%**
- Server packet-source matrix: **345 documented / 0 reachable gaps**
- Stable provisional-name registry: **2,151 distinct addresses / 0 duplicate rows**

These percentages are planning estimates, not a claim that every compiler-emitted function has been decompiled. The remaining analysis must be completed before returning to engine selection or client construction.

## Recommended review order

1. [Executable overview](docs/EXECUTABLE_OVERVIEW.md)
2. [Current analysis status](ANALYSIS_STATUS.md)
3. [Client architecture](docs/CLIENT_ARCHITECTURE.md) and [client subsystems](docs/CLIENT_SUBSYSTEMS.md)
4. [Network architecture](docs/NETWORK_ARCHITECTURE.md), [SOE transport](docs/SOE_TRANSPORT_PROTOCOL.md), and [packet-handler map](docs/PACKET_HANDLER_MAP.md)
5. [Replication schemas](docs/REPLICATION_SCHEMA_MAP.md) and [object-controller protocol](docs/OBJECT_CONTROLLER_PROTOCOL.md)
6. [Asset pipeline](docs/ASSET_PIPELINE.md), [rendering](docs/RENDERING_ARCHITECTURE.md), [terrain](docs/WORLD_TERRAIN_ARCHITECTURE.md), and [platform/input/audio](docs/PLATFORM_INPUT_AUDIO_ARCHITECTURE.md)
7. [Unknown-function ledger](UNKNOWN_FUNCTIONS.md) and [uncertainty audit](docs/FUNCTION_UNCERTAINTY_AUDIT.md)
8. [64-bit port plan](docs/64BIT_PORT_PLAN.md) and [engine-decision requirements](docs/ENGINE_DECISION_REQUIREMENTS.md)
9. [Latest consolidated session report](research/sessions/ANALYSIS_SESSION_2026-09-22_TO_1300.md)

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

## First future implementation milestone

The first executable milestone remains a native x64 program that initializes configuration and logging, performs the minimum Stardust handshake, parses a known server response, logs it, and exits cleanly. It requires no world rendering, character rendering, gameplay UI, or asset conversion.
