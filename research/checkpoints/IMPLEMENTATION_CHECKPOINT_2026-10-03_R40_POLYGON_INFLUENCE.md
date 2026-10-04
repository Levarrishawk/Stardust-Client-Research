# Round 40: fresh BPOL influence

Date: 2026-10-03. Question: what exact fresh BPOL cache/query graph and local shaping behavior can be implemented from direct static evidence while preserving ordered raw input, arithmetic phases and ownership? Stop condition: one bounded finite evaluator, independent deterministic fixtures, one raw occurrence correlation and reviewable owned files. No original process execution, live contact, sweep, gameplay or terrain generation.

Canonical findings and confidence are in the [polygon influence contract](../../docs/TERRAIN_BOUNDARY_POLYGON_INFLUENCE_CONTRACT.md). The [raw input contract](../../docs/TERRAIN_BOUNDARY_POLYGON_INPUT_CONTRACT.md) remains unchanged. Direct query/cache evidence is Confirmed for the finite graph; original active floating controls, exceptional behavior and semantic units remain Unknown. This checkpoint adds no function/protocol/research completion percentage.

## Inputs and scope

- Original EXE `C:/Stardust-DEV/Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before VA reads.
- Config `C:/Stardust-DEV/swgemu_live.cfg`, SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
- Effective priority-42 `terrain/tatooine.trn`: 1,188,446 bytes, SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
- TGEN offset 210, 139,608 bytes, SHA-256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
- One BPOL occurrence: TGEN offset 15,437, layer 6, child 8, version 0005/IHDR 0001, enabled bits 1, ten points, 187 encoded bytes, SHA-256 `56979D03D59C2F76D672986F0D6D1797A46C8A2B225B4D331433F89A88B0AD00`. Existing record enumeration locates it; only this raw BPOL is decoded for comparison. No original influence is evaluated.
- Frozen Round-40 public baseline: 3,127 files; manifest SHA-256 `32f715040a532c164c056e76d2eb9547f5b592edb3dc3dda2090c5afbde62bfe`; ZIP SHA-256 `c46605bde6a8c9b6e698de63d4395da2929b20d0f9615c8d3399b42d2b8f7d95`. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`; frozen main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`. Overlay was verified before and after its single application; reviewed Round-39 bytes matched the accepted baseline.

Raw assets, disassembly, fixtures/results, build receipts and review inventories remain ignored private output. Curated public files contain independently authored descriptions and schemas, not bulk decompiler or original asset data.

## Findings and consequences

The query is `00B73D00`, cache `00B738B0`, vtable `015ED434`. Cache is comparison/copy only: all finite values are exact under every profile and tied extrema preserve selected bits. Query uses a half-open vertical interval and strict horizontal crossing on the original closed-loop order. After odd parity, zero amount returns 1; otherwise it computes vertex and eligible segment distances, retains arithmetic until explicit stores, and compares against the inclusive raw `38D1B717` tolerance band. Projection eligibility compares unspilled lower bound and spilled upper bound. Shared shaping executes after query and stores once to F32. Fresh BPOL amount is not clamped. Full equations, observations, lifecycle and confidence are consolidated in the contract.

Implementation consequences: preserve raw order and separate precision provenance; do not substitute circle/rectangle distance formulas, winding repair, endpoint clamping or a general edge-inclusion predicate. Negative amounts and repeated points remain represented. Modern undefined/nonfinite arithmetic rejects explicitly without asserting legacy rejection. Immutable owned cache/query/batch receipts prevent caller mutation; conservative preallocation work caps bound the new finite evaluator. Geometry/serialization stay engine-neutral.

## Reproduction

Tools: .NET 8 Core and public smoke runner; bundled Python with accepted static-analysis packages; SHA-gated PE/static instruction inspection; independent Fraction/integer oracle and square-root primitive. Accepted raw framing and overlay helpers are reused without changing their public files. The evaluator uses the existing exact arithmetic primitive, not its circle semantics.

In PowerShell at the repository root, set `PYTHONUTF8=1`, `PYTHONPATH` to the existing static-analysis package directory, and `NUGET_PACKAGES` to the existing offline package cache. Use a temporary APPDATA directory and an ignored NuGet config containing an empty package-source list. No download is required. Invoke the installed Python executable for the following `python` commands:

```powershell
python poc/round40-tests/Assets/verify_polygon.py generate local-private/round40/fixtures.json
dotnet restore poc/round40-tests/Assets/Assets.csproj --configfile local-private/round40/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round40-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round40-tests/Assets/bin/Debug/net8.0/Assets.dll local-private/round40/fixtures.json local-private/round40/native.json C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
python poc/round40-tests/Assets/verify_polygon.py verify local-private/round40/native.json C:/Stardust-DEV/Stardust.exe C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round40/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
python tools/python/validate_organized_workspace.py .
git diff --check
```

## Verification and handoff

All 18,120 independent comparisons pass: 16,872 accepted and 1,248 rejected. Forty-eight ownership/budget/identity checks and six null negatives pass. Static verification passes 93 new instruction/data anchors and 87 finite status-mask truth-table cases; 71 raw-input anchors are separately rechecked. The query contains zero calls; cache contains zero arithmetic instructions. Sensitivity checks distinguish 210 query-profile inputs, zero cache-profile inputs and 256 premature-projection shaping cases. One original raw occurrence matches. These counts measure this bounded test/evidence set, not original execution parity or overall coverage.

The separate public runner and actual Core/default smoke project build with zero warnings/errors and execute with exit 0. No build or oracle repair was required. Workspace validation passes across 1,398 Markdown files with zero broken links and zero links into private output; provisional rows remain 2,626 with zero duplicate addresses and packet rows remain 345. Focused source/oracle/document review and whitespace checks pass. Manifest-relative review confirms all 3,127 baseline files unchanged, exactly seven additions, unchanged worker HEAD, an empty index and ignored private/build outputs.

Seven additions are owned: geometry evaluator, public smoke scenarios, this checkpoint, durable contract, and Round-40 Assets project/runner/oracle. Existing canonical/shared files are frozen; lead reconciliation of living ledgers remains outside this worker's scope. No staging, commit or publication is authorized or performed.

Remaining uncertainty: original PC/RC/exception state, zero-length/nonfinite legacy behavior, coordinate units/axes, edited point state, extension semantics and layer composition. Exact next bounded probe: fresh BPLN query `00B74AD0`, width setter `00B74A80` and cache `00B74AA0`, starting from the accepted raw polyline contract. Do not infer its influence from BPOL or dispatch it without the next assignment.
