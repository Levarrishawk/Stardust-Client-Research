# Round 42: one real height-range filter

Date: 2026-10-03. Question: which factory/tag/version fields and height source does one height-range filter use, and can its fresh finite conditional query be reproduced with owned raw input and retained shaping? Stop after direct identity/loader/query/caller evidence, one pinned raw occurrence, independent byte/arithmetic fixtures and proportional validation. No other filters, original process, live contact, GPU, broader corpus sweep or full generator.

The [height filter contract](../../docs/TERRAIN_FILTER_HEIGHT_CONTRACT.md) consolidates exact observations, interpretation, confidence and implementation consequences. The directly recovered tag is FHGT, RTTI is FilterHeight, constructor `00B776C0`, vtable `015ED6E0`, dispatcher `00B777E0`, query `00B77730`. The finite structure/arithmetic graph is Confirmed. Runtime controls, units and complete traversal remain Unknown. No registry or combined research/protocol/function percentage changes follow.

## Orientation and overlay admission

Read synchronized START and accepted Round 41, status/ledger/unknown/provisional searches, existing raw input/arithmetic/layer contracts, the accepted AHCN operation contract and its oracle, and existing frame/static tools. Corrected two read-only discovery lookups: the guessed `verify_terrain_ahcn_operation.py` and `TERRAIN_AHCN_OPERATION_CONTRACT.md` were absent; canonical evidence is `poc/round34-tests/Assets/verify_ahcn_operation.py` and `docs/TERRAIN_HEIGHT_CONSTANT_OPERATION_CONTRACT.md`. No source mutation or behavioral claim came from those failed lookups.

Authoritative Round-42 frozen manifest/ZIP contain 3,222 public files. Manifest SHA-256 `c4dd7ca90525964037c264ee757e3678defd252425e6243eec33473bdf1ddbe7`; ZIP SHA-256 `3be9a0a1071ab564808f035510573c216478b91e32bf55cf82641d5727d056a9`. Before the single overlay, all members/canonical paths/casefolds/sizes/hashes/containment and every lexical root/ancestor/target symlink/junction/reparse condition were checked. Prior Round-41 accepted baseline was separately hash-pinned. Main's final Assets-integrate seven hashes exactly matched the final local reviewed inventory; destinations could match only prior accepted, incoming or those final owned hashes. All 3,222 postflight hashes passed. Private/unlisted bytes, worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` and empty index are preserved; frozen main HEAD is `5493338d9cd4255cf58f25de6c4cd15f6507b818`.

## Inputs and evidence boundary

EXE `C:/Stardust-DEV/Stardust.exe`: SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before each current static PE/Capstone/RTTI decode and by the verifier. Config `C:/Stardust-DEV/swgemu_live.cfg`: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Priority-42 effective `terrain/tatooine.trn`: 1,188,446 bytes, `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. TGEN offset 210, 139,608 bytes, `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.

One original raw FHGT at TGEN offset 14,204, layer 4, child 5, version 0002/IHDR 0001, enabled 1, 97 encoded bytes, logical decoded-copy charge 28: SHA-256 `61773ADF9E1765EEAB0D87844C0E9779578FDAAA22D276103426D4EBAC20B99C`. Existing ordered TGEN enumeration finds 37 matching sections; only the first is decoded and compared. No influence evaluation uses this original record. Private asset bytes/name/raw fields, disassembly, detailed fixtures/results and review receipts remain ignored; curated output contains independently authored contracts and synthetic fixtures only.

## Findings and implementation consequences

Factory/RTTI independently identify FHGT/FilterHeight. Versions 0000/0001/0002 contain respectively two range words, one discarded prefix plus range, and range/selector/amount. Constructor supplies absent selector/amount zero. The version-2 loader clamps finite amount to [0,1]; its setter is raw and does not itself clamp. Neither range setter normalizes endpoints. The writer independently emits four words in version 2. Raw decoder preserves all words and all F32 categories; unsupported versions remain modern opaque records.

Query obtains the current grid descriptor from fifth argument +18, then row-major stride/data and cell. Filter caller and AHCN share state EBX/+18, and the filter call precedes this layer's affector call. This proves a current-height dependency, not initial-grid or complete traversal semantics. Conditional API therefore takes a supplied current raw word and does not precompute whole layers.

Strict lower/upper range tests return zero at both endpoints and for equal/reversed ranges. Interior computes an unspilled range*amount*0.5 span; ordered lower/upper ramp comparisons use that retained span and return an unspilled ratio or one. Shared filter shaping consumes the retained query, then stores shaped F32. No cache arithmetic/profile is invented. Detailed equations, VAs, confidence, ownership and modern rejection policy are in the contract.

## Reproduction and results

Existing .NET 8 and cached bundled Python suffice. Set `PYTHONUTF8=1`, `PYTHONDONTWRITEBYTECODE=1`, static-package `PYTHONPATH`, offline `NUGET_PACKAGES`, and task-specific temporary `APPDATA`. NuGet config clears package sources. Here `python` denotes `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`; packages are `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages`. No downloads are needed.

```powershell
python poc/round42-tests/Assets/verify_height_filter.py generate local-private/round42/fixtures.json
dotnet restore poc/round42-tests/Assets/Assets.csproj --configfile local-private/round42/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round42-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round42-tests/Assets/bin/Debug/net8.0/Assets.dll local-private/round42/fixtures.json local-private/round42/native.json C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
python poc/round42-tests/Assets/verify_height_filter.py verify local-private/round42/native.json C:/Stardust-DEV/Stardust.exe C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round42/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
python tools/python/validate_organized_workspace.py .
git diff --check
```

Independent raw framing and Fraction/integer control-flow expectations agree with 375 raw cases: 38 accepted/337 rejected; 13,959 query cases: 13,934 accepted/25 rejected. Thirty ownership/budget checks and five null negatives pass. Ninety instruction anchors, five shared-height/caller anchors, 18 finite status cases, exact RTTI/vtable and 2/3/4 loader read counts pass. Query has zero calls; it reloads an F32 current height but never spills span or final influence in the original graph. Tests distinguish 117 query-profile-sensitive inputs and 138 premature-projection shaping cases, without identifying original active controls. One original raw occurrence independently matches.

The separate runner and actual Core/default smoke build/run pass with zero warnings/errors and exit 0; default smoke retains 18 allowlisted journal events/zero failures. One new RTTI verifier assertion initially used an 18-byte slice for the 19-byte terminated type name; corrected to literal-derived length and rerun successfully. No production/build or raw/arithmetic comparison repair was needed. The read-only discovery failures above and this corrected static-verifier attempt are explicitly retained.

Final workspace validation passes across 1,422 Markdown files with zero broken links or links into private output; provisional rows remain 2,626 with zero duplicate addresses and packet rows remain 345. Owned Python AST/project XML, focused source/contracts/oracle review, all owned whitespace checks and git diff --check pass. Baseline-relative review confirms all 3,222 existing public files unchanged, exactly eight additions, unchanged worker HEAD, an empty index and ignored private/build output. Exact final file hashes and review inventories are retained privately for lead replay. Exactly eight additions are owned: input, influence, scenarios, contract, checkpoint and three own runner/oracle files. Every existing public file, including layer carrier and shared registries/ledgers, is frozen. No staging, commit, push or publication occurs.

## Resume

Unknown: original floating controls/nonfinite recovery, units/axes, initial grid/complete traversal state, edited ranges/amount and malformed legacy recovery. Exact next action after lead acceptance: add a retained height-filter carrier to the existing layer-query boundary and replay ordered supplied-layer execution using each query-time current height. Do not project before Shape or precompute heights for later layers. This worker stops after final review and waits for the lead's next assignment.
