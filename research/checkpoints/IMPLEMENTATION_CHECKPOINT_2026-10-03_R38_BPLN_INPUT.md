# 2026-10-03 Round 38: owned BPLN serialized input

## Question and stopping condition

Can an engine-neutral BPLN decoder preserve independently verified version layouts, raw metadata and ordered point pairs, with bounded immutable ownership, and reproduce the same 34 pinned Tatooine records? Stop after direct factory/inheritance/version/read/header/default/cache/teardown evidence, independent fixtures and this ordered corpus comparison agree. No influence evaluation, axis assignment, layer generator or new corpus sweep is included.

Canonical conclusion: [BPLN input contract](../../docs/TERRAIN_BOUNDARY_POLYLINE_INPUT_CONTRACT.md). New files: decoder (external historical reference), public Core smoke scenarios (external historical reference), runner project (external historical reference), runner entry (external historical reference), independent verifier (external historical reference), that contract and this checkpoint. Existing public files are frozen; lead owns living-doc reconciliation.

## Orientation and input pins

Read synchronized START_HERE, accepted Round 37, relevant BPOL/TGEN contracts and checkpoint, status, ledger and unknown/provisional entries. Inspected existing disassembly, IFF, overlay and independent TGEN tools before reusing their framing/extraction mechanisms. No BPLN schema or canonical provisional label was found to supersede. Direct BPLN analysis establishes its layout separately from BPOL.

Original executable: `C:/Stardust-DEV/Stardust.exe`; SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, checked before VAs. It was read statically using PE/Capstone helpers and never executed. Root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective priority-42 `terrain/tatooine.trn`: 1,188,446 bytes, SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. TGEN: offset 210, 139,608 bytes, SHA-256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. These are unchanged prior-round pins.

Frozen Round 38: 3,030 public files; manifest SHA-256 `d28af4bf1510c02eb3e6f91b1232fc9d7e3318021a294691c4366cd662a8ba76`, ZIP SHA-256 `c60ea7c6452d3cbf4920373f19517973afdf9c95b48dc6b8f28b9043f948ee36`. Full member/listed-set, uniqueness/casefold, sizes/digests, containment and target/ancestor symlink/junction/reparse preflight completed before first overlay write; all-file postflight passed. Private/unlisted files and worker HEAD/index were preserved. Worker HEAD is `65de8c88141071744dc75594be126b3bab3696a5`; baseline main HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`.

## Evidence, interpretation and consequence

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Factory compares BPLN at `00B79E3D`, allocates 0x3C, constructs at `00B79E6D`, loads via +8 at `00B79E84`. | Actual BPLN owned factory path, Confirmed. | Separate family and object contract. |
| Constructor `00B74A20` calls `00B736E0` with BPLN/kind 3; vtable `015ED474` has loader `00B74D00`, serializer `00B75000`, amount setter `00B65FC0`, query `00B74AD0`, cache `00B74AA0`. | Shared point storage with BPLN overrides, Confirmed. | Reuse framing/ownership conventions, independently decode payload. |
| Dispatcher subtracts `0000` at `00B74D1E`, selects `00B74DD0`; decrement selects `00B74EE0` for `0001`. | Exactly two recognized layouts, Confirmed. | Higher versions stay bounded opaque. |
| `0000` read sites: `00B74E0D`, `00B74E25`, `00B74E3D`, then pairs `00B74E68`/`00B74E7B`; remaining/8 at `00B74E51`. | Metadata before implicit pairs, Confirmed. | Require 12-byte prefix and exact modern pair extent. |
| `0001` count `00B74F1D`, pair reads `00B74F38`/`00B74F4B`, then metadata `00B74F90`/`00B74FA8`/`00B74FC0`. | Explicit count before pairs, metadata afterward, Confirmed. | Preserve count bits and non-point word order. |
| Pair stores `00B74E9D`/`00B74EA3` and `00B74F6D`/`00B74F73` use +0/+4 and eight-byte stride. | Ordered raw first/second words, Confirmed; axes Unknown. | Preserve exact pairs without normalization. |
| Both loaders call `00B62FA0`; IHDR0 reads enabled word plus three individual legacy bytes. | Frozen IHDR convention, Confirmed. | Owned local enabled/name/header bytes. |
| Width setter `00B74A80` stores +38 directly and calls cache slot; cache `00B74AA0` subtracts/adds it from inherited bounds. | Bounds padding role Confirmed; full/half width and units Unknown. | Raw `WidthBits`, no inferred geometric conversion. |
| Amount `00B65FC0` stores +18; neither loader clamps. Constructor +38 is raw 2.0. | Raw metadata and runtime defaults, Confirmed. | Accept full bits, inspect finite values separately. |
| `00B74A70` tail-calls `00B73740`; vector +20 freed, then boundary teardown frees IHDR name; `00B74A40` optionally deletes object. | Owned legacy vector/header lifecycle, Confirmed. | Immutable managed result, no external handles. |

Static verification checks 47 instruction anchors plus exact vtable values, dispatcher branches, read-site order/count/width, eight-stride stores, cache add/subtract counts, string-read absence and header legacy reads. Version read-site counts are five and six, not expanded runtime point-call totals. Serializer `00B75000` emits version `0001` with count, pairs and three words, providing a separate client serialization direction.

Legacy negative/nonpositive count uses signed `jle` at `00B74F27`, skipping pairs before metadata; modern negative rejection is stricter policy. Implicit legacy remaining division floors partial pairs. Original malformed close/recovery behavior and unknown-version diagnostic/cache safety remain Unknown. The decoder does not claim to emulate unsafe reads.

## Implemented ownership and budgets

Published point arrays are private and exposed as a read-only collection; source/header/point/metadata-word accessors copy. Metadata is an immutable record with nullable count only for absent implicit serialization. Unknown versions/headers preserve whole source with null typed fields. No extension fields or discarded words are invented. The snapshot and parsed IFF are separately input-bounded; typed arrays are allocated only after point/string/aggregate preflight.

Logical copy charge counts point words, metadata words and header name/legacy payloads. Defaults and malformed-input exceptions are in the contract. Synthetic fixtures include all short DATA/full-record extents, explicit count mismatches and negative counts, implicit partial/whole-pair tails, wrong tags/order, missing/duplicate records, NUL and byte caps, exact/one-less charges for both versions, unknown versions and headers, invalid options, zero/minimal points and all F32 categories. Mixed finite-point/nonfinite-width and nonfinite-point/finite-metadata cases demonstrate separate inspection properties.

## Reproduction and results

Use .NET 8 and Python with cached PE/Capstone packages. Set `PYTHONUTF8=1` and `PYTHONPATH` to the existing static-analysis package directory. In this checkout that directory is `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages`. Offline NuGet config clears package sources; `NUGET_PACKAGES` points at the existing cache and `APPDATA` at a task-specific temporary directory. No download is required.

```powershell
python poc/round38-tests/Assets/verify_bpln_input.py generate local-private/round38/fixtures.json
dotnet restore poc/round38-tests/Assets/Assets.csproj --configfile local-private/round38/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round38-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round38-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round38/fixtures.json > local-private/round38/native.txt
python poc/round38-tests/Assets/verify_bpln_input.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round38/native.txt > local-private/round38/python-audit.json
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round38/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
python tools/python/validate_organized_workspace.py .
git diff --check
```

Passed 325 fixtures: 55 accepted, 270 rejected; 18 ownership checks; 47 static anchors and additional structural assertions; all 34 original records exactly agree in ordered identity, version, header, raw pairs/metadata and hashes. Actual separate Core and default smoke-test project build and default smoke execution pass with zero warnings/errors and exit zero. The isolated runner also builds cleanly. No failed checks or repair attempts occurred.

Original aggregate: all BPLN/IHDR `0001`, 163 points total, minimum 2/maximum 15, all 34 locally enabled, selector 0/3 counts 15/19, all point/amount/width values finite, maximum logical decoded-copy payload 152 bytes. No broader corpus coverage or generated terrain parity is claimed.

Server correlation: `MMOCoreORB/src/terrain/layer/boundaries/BoundaryPolyline.h::parseFromIffStream(Version<'0001'>)` has matching count/pair/selector/amount/width order; its coordinate names and influence behavior are not client evidence.

## Open question and next milestone

Serialized layouts and ownership are closed for this bounded decoder. Width units/full-versus-half interpretation, point axes, query edge/degenerate behavior, runtime arithmetic precision, layer/filter composition and original malformed recovery remain open. Next bounded probe: trace query `00B74AD0` with width and point consumers, establishing distance/edge/zero-width branches and arithmetic operations before any evaluator implementation. Stop at the query boundary; no layer generator.

## Final review

Passed baseline-relative review: seven additions, zero existing-file modifications/deletions, all 3,030 frozen public files byte-identical. Exact assigned scope and own runner directory are preserved; worker HEAD is unchanged and index empty. Private/generated receipts and build outputs are ignored. Per-addition whitespace checks and `git diff --check` pass; focused implementation, oracle and evidence changes were inspected.

Workspace validation passes: 1,372 Markdown files, zero broken links, zero links into private evidence, 2,626 provisional rows with zero duplicate addresses, 345 packet rows unchanged. No stage, commit, push, download, original executable execution or live contact occurred. All shared production and living documents remain immutable for lead reconciliation. Ready for review, then idle.
