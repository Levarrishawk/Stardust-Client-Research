# Round32: complete CREO6 baseline and supported ordered delta codec

Date:2026-10-03. Question: can accepted31 CREO6 structure support a complete owned baseline and ordered scalar/string/pair delta union, with global preflight bounds and independent exact byte fixtures? Stop condition: full model/wire/budget/ownership oracle, required static and Core/prior regression gates, reconciled durable contract and exact scoped handoff. No state/world or live work.

## Inputs and evidence

Frozen snapshot2786 files, manifest SHA256 `07d6abdbdab081bad0d3fb3c67a4724c6cd2865ac5411e04e9dc6b2821d9dd04`, ZIP SHA256 `e98dc53bba40cc13dda5298b5c713a51d870bfc3ee43944239a6d62e5e627487`. Complete member uniqueness/casefold/path/size/hash, archive/target/ancestor symlink/reparse preflight preceded every listed-byte write; post-overlay hashes, worker HEAD/index and unlisted/private preservation passed. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`; main's supplied snapshot HEAD is `5493338d9cd4255cf58f25de6c4cd15f6507b818`.

Read AGENTS, START_HERE, accepted31 session, relevant status/ledger/unknown terms, [prefix contract](../../docs/CREATURE_VIEW6_PREFIX_CONTRACT.md), prior codec/state checkpoints and curated provisional/server-gap rows. Reused accepted envelope/raw-byte-string/freeze facilities after inspection. No existing production/shared code changes.

Reran [round31 static verifier](../../poc/round31-tests/Networking/static_audit.py) against `C:/Stardust-DEV/Stardust.exe`, SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, before relying on VAs. Passed109 anchors,18 registrations,16RTTI. In particular00B94C60/00439F60 confirm wearable string plus4/8/4 body and extended string branch;0044180D confirms baseline element invocation. Existing cached pefile/Capstone only. Server source is separately correlated in the durable contract, not used as client proof.

## Findings versus interpretation

Observation: all eighteen baseline fields independently match literal fixture bytes, including inherited raw defenders, two S32 vectors, variable wearable elements, counters, full-bit scalars and raw short/extended strings. Interpretation: accepted schema is executable as a bounded owned codec. Confidence: Confirmed deterministic native/Python comparison, with structural compatibility inherited from direct static evidence. Consequence: implementation may consume complete CREO6 baselines without inventing fields from server-declared22.

Observation: ordered deltas0,2â€“12,16â€“17 retain repeats/count metadata and scalar/string/pair bodies. Valid field-boundary prefixes tolerate count drift; partial fields and unsupported indices1,13â€“15 reject. Interpretation: an explicit supported union is ready for later recipient integration. Confidence: Confirmed fixtures. Consequence: no automatic routing or vector state semantics are implied.

Observation: preflight scans all nested bodies before decoded allocation; encode measures before its field buffer. Exact/one-less global budgets and mutable-source/copy-accessor failures preserve complete existing models. Interpretation: owned modern bounded publication boundary. Confidence: Confirmed implementation tests. Consequence: stronger whole-message error behavior than legacy mutation is explicit; logical byte/work budgets are not recovered heap/CPU costs.

Canonical API, grammar and limits are in the [durable contract](../../docs/CREATURE_VIEW6_CODEC_CONTRACT.md). No new opcode, address name, registry row or coverage denominator is added.

## Reproduction and results

Use bundled Python, offline .NET8 and task-specific APPDATA. The private NuGet.Config clears sources; packages are existing cache contents.

```powershell
$env:PYTHONUTF8='1'
$env:PYTHONDONTWRITEBYTECODE='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-appdata-networking-r32'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$py='C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $py poc/round31-tests/Networking/static_audit.py --exe C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages --receipt local-private/round32/static-audit.json
& $py poc/round32-tests/Networking/peer.py --generate local-private/round32/peer.json
dotnet restore poc/round32-tests/Networking/Networking.csproj --configfile local-private/round32/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round32-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round32-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round32/peer.json local-private/round32/export.json
& $py poc/round32-tests/Networking/peer.py --peer local-private/round32/peer.json --verify local-private/round32/export.json
```

Passed23 exact synthetic wires,3870 negatives,282 budget and122 ownership checks. Python independently parses complete field order, signed/raw values, counters, string spelling and aggregate costs; it regenerates fixture identity and compares all native exports. Boundaries65534/65535/65536, extended-small/empty strings, embedded zero/high bytes, duplicate IDs/entries, nested count overflow, all supported single fields and repeated/reversed union are included. Truncations test original outer mismatch and adjusted partial bodies; complete adjusted delta prefixes are independently positive. Both byte/envelope overloads and encoder policies are exercised.

Shared Core smoke suite restored/built offline with zero warnings/errors and exited0. Accepted round30 native state regression passed21 sequences,700 negatives,315 budget and114 ownership checks; its independent Python state/wire/event/work oracle passed using `local-private/round32/prior30-export.json`.

First build caught C# span capture restriction in a ref-struct primary constructor; corrected to explicit readonly span storage. First test run incorrectly expected every size-adjusted delta prefix to reject. Because count is metadata, complete field prefixes are valid; the independent peer now distinguishes them and compares exact models/bytes. Production consume semantics were unchanged for that test correction. Reruns passed.

Private outputs: `local-private/round32/peer.json`, `export.json`, `static-audit.json`, `prior30-export.json`, `shared-smoke.log`, `baseline.json`, and final scope/validator/status receipts. Fixture generation is synthetic. Original image/disassembly is never executed or promoted.

Synthetic peer SHA256 `99fdc4a28fe97cf7465bd6ff7b2c28733dbacf491688bec862aa11d6860a1dee`; native export SHA256 `1f114d956282931c81ae1408d275082e6b4f1f9e7625cfd799a3156e5706b5ae`. Workspace validation passed1310 Markdown files, zero broken/private links,2626 provisional rows/zero duplicate addresses,345 packet rows and no public files above25MiB. Exact snapshot review passed seven additions with all2786 snapshot files unchanged. `git diff --check`, UTF-8/LF/EOF and Python syntax checks passed; HEAD and empty index preserved. Review receipt `local-private/round32/changed-files.json`, validator `workspace-validation.txt`, status `git-status.txt` and whitespace advisory `diff-advisories.txt` retain the final audit.

## Scope and resume point

Seven additions: codec, smoke scenarios, durable contract, this checkpoint, and own Networking32 project/Program/peer. No existing canonical/global/shared files are modified. No files staged/committed/pushed, no live contact/downloads; unrelated/private material preserved.

Unsupported field1 deltas are an optional-composition scope choice;13â€“15 specialization state policies remain separate. Noncanonical bool semantics, invitation arithmetic, callbacks/weak targets/current incarnation, presentation and live interoperability remain unproven.

Next bounded action: current-incarnation owned publication for the supported CREO6 union, or independently specify field1 composition and13â€“15 delta/state policies before expanding coverage. Do not relabel the TANO-only whole-envelope codec as CREO or silently borrow integer/wearable mutation behavior.

Lead identity reconciliation: the frozen manifest records main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`, the progress commit already present at snapshot capture. An initial dispatch advisory retained the previous overnight HEAD; manifest/SHA-verified working bytes were authoritative throughout. No agent changed HEAD/index.
