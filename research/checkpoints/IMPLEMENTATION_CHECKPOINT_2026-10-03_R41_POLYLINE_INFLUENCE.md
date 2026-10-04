# Round 41: fresh BPLN influence

Date: 2026-10-03. Bounded question: which fresh BPLN ordered cache/query graph, endpoints, width/feather stores and local shaping can be implemented from direct executable evidence with owned immutable receipts and explicit arithmetic phases? Stop after the finite graph, deterministic independent fixtures, one raw occurrence comparison and proportional validation. No original executable execution, live contact, GPU, new corpus sweep or terrain generator.

Canonical observations, interpretations, confidence and implementation consequences are in the [polyline influence contract](../../docs/TERRAIN_BOUNDARY_POLYLINE_INFLUENCE_CONTRACT.md). Accepted [raw BPLN](../../docs/TERRAIN_BOUNDARY_POLYLINE_INPUT_CONTRACT.md) and [Round 38 evidence](IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_BPLN_INPUT.md) remain unchanged. No new provisional names, packet claims or coverage percentage follows.

## Orientation, pins and scope

Read synchronized START, accepted Round 40, current status/ledger, unknown/provisional searches, accepted raw BPLN and polygon arithmetic contracts, relevant checkpoints, existing static/overlay/oracle tools and curated rows. A read-only lookup initially used `R38_POLYLINE_INPUT` rather than canonical `R38_BPLN_INPUT`; it was corrected before substantive checkpoint use. No file or build mutation resulted from that lookup.

Authoritative frozen Round 41 is the `round41/frozen` manifest/ZIP, not the preliminary parent snapshot: 3,174 public files; manifest SHA-256 `74cc0552f0715e52b782c9aec68dc003584fabbdd8987f55cc6684c971238ea8`, ZIP SHA-256 `c788a27f07bb5d5b5c4613c6b54a015ecf673288e3518fadff2cad79dd94c746`. Entire member/path/casefold/size/hash/containment and every lexical root/ancestor/target symlink/junction/reparse condition were checked before the single overlay. Existing bytes had to match accepted prior baseline, final reviewed Round-40 owned hashes or incoming hashes. All 3,174 postflight hashes passed. Private/unlisted data, actual worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` and index are preserved. Frozen main HEAD is `5493338d9cd4255cf58f25de6c4cd15f6507b818`.

Input executable `C:/Stardust-DEV/Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed for the current PE/Capstone static decode and again by final verifier. Config `C:/Stardust-DEV/swgemu_live.cfg`: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective priority-42 `terrain/tatooine.trn`: 1,188,446 bytes, SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. TGEN offset 210, 139,608 bytes, SHA-256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.

One original raw BPLN occurrence: TGEN offset 84,862, layer 135, child 2, six points, version 0001/IHDR 0001, enabled 1, 149 encoded bytes; record SHA-256 `E8D9547DD5C649A01C26646336F24549CACEA8C4E50E25EBED62E8DFECA0F3DF`. Existing ordered record enumeration locates the first BPLN; only this occurrence is decoded and compared. No original influence is evaluated. Private asset bytes, disassembly, fixtures/results and review inventories stay ignored.

## Findings and consequences

Direct query `00B74AD0`, width setter `00B74A80`, cache `00B74AA0`, constructor `00B74A20` and vtable `015ED474` establish the graph independently of BPOL. Cache selects ordered extrema then stores four width-expanded bounds. Query first gates the inclusive, potentially reversed box; it processes all vertices and exactly N-1 consecutive segments. No implicit closing edge exists. Both projection eligibility tests use stored parameter; denominator retains second-component delta while numerator reloads it. Strict minimum comparisons select stored candidate values only after comparing retained candidates. Final width comparison uses stored width-square; inner comparison mixes retained threshold with its stored copy. The final feather value remains unspilled until shared shaping. These finite structure/arithmetic conclusions are Confirmed by the direct anchors and status-mask checks; original active controls and exceptional behavior remain Unknown.

The replacement preserves raw signs/order and explicit profiles, with no guessed coordinate units, sorting/repair, width conversion, amount clamp, BPOL tolerance or BPOL projection formula. Undefined divisions and nonfinite observed stores are modern rejections, not legacy claims. Owned cache/query/batch receipts bound and isolate supplied input; no engine-specific layer or sibling Round-41 dependency is introduced.

## Reproduction

Use existing .NET 8 and bundled Python. Set `PYTHONUTF8=1`, `PYTHONDONTWRITEBYTECODE=1`, `PYTHONPATH` to `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages`, `NUGET_PACKAGES` to the existing ignored offline cache and `APPDATA` to a task-specific temporary directory. The ignored NuGet config clears package sources. No downloads are needed. Here `python` denotes `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`:

```powershell
python poc/round41-tests/Assets/verify_polyline.py generate local-private/round41/fixtures.json
dotnet restore poc/round41-tests/Assets/Assets.csproj --configfile local-private/round41/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round41-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round41-tests/Assets/bin/Debug/net8.0/Assets.dll local-private/round41/fixtures.json local-private/round41/native.json C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
python poc/round41-tests/Assets/verify_polyline.py verify local-private/round41/native.json C:/Stardust-DEV/Stardust.exe C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round41/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
python tools/python/validate_organized_workspace.py .
git diff --check
```

The independent oracle reuses accepted Fraction/integer primitives and raw framing/overlay extraction, with its own BPLN cache/control-flow equations. Native public scenarios compile against the separate actual Core assembly. Original executable influence is never called.

## Results and final review

All 90,965 comparisons pass: 83,221 accepted and 7,744 rejected; 48 ownership/budget/identity checks and six null negatives. Static verification passes 83 new anchors, 22 finite status-mask cases and separately rechecks 47 accepted raw-input anchors. Query contains zero calls. Tests distinguish 595 query-profile-sensitive inputs, zero cache-profile-sensitive inputs, 1,404 retained width-square witnesses, 48 retained-delta witnesses and 110 premature-query-projection shaping cases. These are finite fixture distinctions, not original runtime-control identification or total subsystem coverage. The explicit dy witness uses `(0,1),(0,33554432)` and query `(0,16777216)`, width 2, amount 1, both source orders and nine cache/query combinations: retained dy 33554431 differs from stored 33554432 under 53/64 query precision. The initial random fixture set had no delta witness; the exact targeted case was added and passed, without altering production arithmetic. A final targeted group of non-power-of-two widths (0.1, 1.1 and -0.1), threshold-adjacent raw coordinates, empty/singleton inputs and all nine profiles closes the retained initial width-square test gap. Alternative-spill metrics compare full observable receipts, including branches, not only shaped influence.

The separate public runner and actual Core/default smoke project build with zero warnings/errors and execute with exit 0. Default smoke retains 18 allowlisted journal events/zero failures. One original raw occurrence independently matches identity, header, ordered points, metadata and hash. No build or oracle failure/repair occurred; the corrected read-only checkpoint lookup above is the only failed lookup.

Workspace validation passes across 1,409 Markdown files with zero broken links and zero links into private output. Provisional rows remain 2,626 with zero duplicate addresses; packet rows remain 345. Manifest-relative review verifies all 3,174 existing public files unchanged and exactly seven additions, unchanged worker HEAD, empty index and ignored private/build output. Focused source/contract/oracle review, Python AST, project XML and whitespace checks complete the review; final inventories and full file hashes are retained privately for lead replay.

## Remaining questions and resume

Original PC/RC/exception state, zero-length/nonfinite legacy recovery, width units/full-versus-half convention, coordinate axes and edited-state producers remain Unknown. Serialized versions and fresh finite graph are separated from complete terrain generation. Exact next action after lead acceptance: add a retained `FromPolyline(TerrainPolylineQuery)` carrier at the existing layer-query boundary, then independently replay supplied BPLN/local-layer combination without projecting before shaping. This is a future integration assignment, not a sibling dependency or work performed here. The lead selects and authorizes the next assignment; this worker stops after final review.

Owned additions are geometry evaluator, public smoke scenarios, durable contract, this checkpoint and the three Assets runner/oracle files. Every existing public file is frozen. Lead owns living canonical reconciliation; no staging, commit, push or publication occurs.
