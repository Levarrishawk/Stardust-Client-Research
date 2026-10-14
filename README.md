# Stardust Client Research

Godot client implementation, tests and build tooling live in [SWG-Godot-Client](../SWG-Godot-Client/README.md). This repository contains research and its supporting evidence tools. See [the research-only boundary](docs/RESEARCH_ONLY_SCOPE.md).

This repository is the curated, resumable research record for understanding the legacy `Stardust.exe` client well enough to build a clean, native 64-bit replacement that remains compatible with the Stardust/Core3 server and the original client data formats.

This is a research-only repository. It contains executable-backed findings, protocol and asset specifications, evidence checkpoints, machine-readable registries, and reproducibility tools. Reconstructed behavior is not claimed to be original source code.

> Start with [START_HERE.md](START_HERE.md) for the current status, authoritative metrics, recommended reading order, and latest resume point. Agents must also read [AGENTS.md](AGENTS.md) before changing research artifacts.

## Research baseline and boundaries

The primary analysis subject is:

| Artifact | SHA-256 |
|---|---|
| `C:\Stardust-DEV\Stardust.exe` | `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` |

Addresses and executable observations are valid only for that exact binary. Re-hash the executable before relying on them. Stardust/Core3 server source may be used to correlate client-visible protocol behavior, but it is not evidence of how the client itself implements a feature.

Unless a task explicitly expands the scope, this work is static-analysis-only. Do not bypass DRM, authentication, licensing, access-control encryption, or other protections. Do not add the executable, original game assets, bulk decompiler output, credentials, or private service data to Git.

## Repository contents

| Path | Contents | Role |
|---|---|---|
| [START_HERE.md](START_HERE.md) | Current scope, metrics, review order, and next research milestone | Canonical entry point |
| [ANALYSIS_STATUS.md](ANALYSIS_STATUS.md) | Current coverage, established conclusions, and remaining work | Living status |
| [COMPREHENSIVE_ANALYSIS_LEDGER.md](COMPREHENSIVE_ANALYSIS_LEDGER.md) | Chronological whole-client evidence history | Living ledger |
| [UNKNOWN_FUNCTIONS.md](UNKNOWN_FUNCTIONS.md) | Address-based observations and stable provisional names | Function registry |
| [`docs/`](docs/README.md) | Durable architecture, protocol, asset, rendering, UI, and porting specifications | Consolidated knowledge |
| [`research/checkpoints/`](research/README.md) | Small, dated reports for bounded investigations | Evidence trail |
| [`research/sessions/`](research/README.md) | Multi-block session summaries and explicit continuation points | Handoffs |
| [`research/archive/`](research/README.md) | Superseded reports preserved for provenance | Historical record |
| [`data/curated/`](data/README.md) | Canonical provisional-name and server-packet CSV registries | Machine-readable truth |
| [`tools/`](tools/README.md) | Python, PowerShell, and Ghidra helpers for reproducible analysis | Verification tooling |
| `local-private/` | Ghidra state, raw extracts, toolchains, generated corpora, and sensitive inputs | Ignored; never publish |

[WORKSPACE_MANIFEST.md](WORKSPACE_MANIFEST.md) records how the workspace was organized. [WORKSPACE_HYGIENE.md](WORKSPACE_HYGIENE.md) defines retention and maintenance rules. Read [PUBLICATION_GUIDE.md](PUBLICATION_GUIDE.md) before staging a broad change, creating a remote, or publishing anything.

## Key research areas

The durable documentation covers these major compatibility surfaces:

- executable layout, startup, game loop, scenes, and subsystem ownership;
- SOE transport, login/zone transitions, packet dispatch, object controllers, and replication;
- TRE/IFF lookup and asset formats, rendering, shaders, terrain, portals, appearance, and animation;
- UI mediators, input, audio, gameplay-facing managers, and lifecycle behavior;
- compatibility risks, engine requirements, and the proposed native x64 boundary.

The most useful indexes are [docs/CLIENT_ARCHITECTURE.md](docs/CLIENT_ARCHITECTURE.md), [docs/NETWORK_ARCHITECTURE.md](docs/NETWORK_ARCHITECTURE.md), [docs/ASSET_PIPELINE.md](docs/ASSET_PIPELINE.md), [docs/PACKET_HANDLER_MAP.md](docs/PACKET_HANDLER_MAP.md), and [docs/REPLICATION_SCHEMA_MAP.md](docs/REPLICATION_SCHEMA_MAP.md). The full ordered path is maintained in [START_HERE.md](START_HERE.md).

## Evidence model

Every nontrivial technical claim should identify its evidence and confidence:

- **Confirmed** — directly supported by executable instructions/data, a wire codec or capture, server source at the relevant boundary, or asset bytes.
- **Strongly inferred** — supported by multiple consistent observations, with one indirect boundary remaining.
- **Tentative** — a plausible interpretation that still needs targeted evidence.
- **Unknown** — insufficient evidence; do not convert uncertainty into a fact.

Prefer exact virtual addresses, byte layouts, strings, import/export references, call relationships, asset tags/versions, or repository file-and-line citations. Keep observation, interpretation, and implementation recommendation distinct.

Stable provisional function names use `subsystem_role_VA_XXXXXXXX`, for example `app_entry_VA_00401050`. Never silently rename an address to a different semantic role. Record refinements in the ledger and normalize the curated registry.

## Typical research workflow

1. Verify the executable hash and read the current resume point in [START_HERE.md](START_HERE.md).
2. Define a narrow question with an explicit stopping condition.
3. Search the existing docs, checkpoints, ledgers, and curated data before creating new terminology.
4. Collect the smallest sufficient evidence from the executable, assets, wire data, or server correlation source.
5. Cross-check the result with an independent signal where possible.
6. Write a dated checkpoint, update the relevant durable specification, and reconcile any affected registries or status documents.
7. Run validation and record the exact next step for the following session.

Detailed agent procedures, subsystem-specific strategies, naming rules, and completion criteria are in [AGENTS.md](AGENTS.md).

## Tools and validation

Analysis helpers live under `tools/python/`, `tools/ghidra/`, and `tools/powershell/`. Many expect local inputs or dependencies under ignored `local-private/` paths; inspect a script before running it and do not rewrite a working local assumption casually.

After moving documentation, changing Markdown links, or editing a canonical CSV, run:

```powershell
python .\tools\python\validate_organized_workspace.py .
```

The validator checks relative links, accidental links into `local-private/`, duplicate provisional addresses, packet-matrix counts, and curated-tree size. Run narrower producer or verification scripts for any generated data you change and report their exact commands and results.

The Godot proof has separate instructions and explicit non-claims in [../SWG-Godot-Client/poc/README.md](../SWG-Godot-Client/poc/README.md) and [../SWG-Godot-Client/poc/POC_RESULTS.md](../SWG-Godot-Client/poc/POC_RESULTS.md). Its synthetic message envelope is not a recovered Stardust protocol implementation.

## Contribution and publication safety

- Preserve existing evidence and historical reports; amend living documents instead of rewriting history.
- Put reproducible, canonical data in `data/curated/`; put bulky or proprietary generated evidence in `local-private/generated-evidence/`.
- Keep machine-specific paths out of new curated artifacts unless the path itself is necessary evidence.
- Do not commit build outputs, toolchains, Ghidra databases, raw asset dumps, or proprietary binaries.
- Do not treat a clean working tree as proof that content is safe to publish. Review the staged inventory and follow [PUBLICATION_GUIDE.md](PUBLICATION_GUIDE.md).
- Do not commit, push, create remotes, or publish without explicit authorization.

## Implementation direction

The first replacement-client milestone remains deliberately small: a native x64 program that initializes configuration and logging, performs the minimum verified Stardust handshake, parses one known server response, logs it, and exits cleanly. It does not require world rendering, character rendering, broad gameplay UI, or asset conversion.

The existing Godot proof shows that an engine-neutral .NET core can be hosted by Godot; it does not settle the final engine, language, protocol, or asset-compatibility decisions.
