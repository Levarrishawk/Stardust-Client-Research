# 2026-10-03 Round 39: fresh rectangle influence

## Bounded question and stop

What cache, query and selector graph does a freshly decoded BREC execute, and can an immutable engine-neutral evaluator reproduce that graph conditionally under separate nearest-even 24/53/64 cache/query profiles? Stop after direct transform/default/load/setter/cache/query/shaping proof, independent arithmetic fixtures, one existing raw corpus occurrence and scope/validation checks. Transformed or edited runtime state, full layer/filter composition, axes, original precision policy and GPU are excluded.

Durable contract: [fresh rectangle influence](../../docs/TERRAIN_BOUNDARY_RECTANGLE_INFLUENCE_CONTRACT.md). New implementation: TerrainBoundaryRectangleInfluence.cs (external historical reference); public smoke scenarios (external historical reference); independent Fraction verifier (external historical reference); runner project (external historical reference) and entry (external historical reference). All existing public production/shared docs remain frozen for lead reconciliation.

## Orientation and identity

Read synchronized START_HERE, accepted Round 38 session, status, ledger, unknown/provisional terminology, accepted BREC raw-input and circle arithmetic contracts/checkpoints. Inspected existing static disassembler, raw BREC verifier, public Core APIs and exact arithmetic/oracle helpers before writing the bounded graph. Circle Number primitives are reused as arithmetic mechanisms; no rectangle formula is derived from circle behavior or server code.

Original executable `C:/Stardust-DEV/Stardust.exe`: SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, checked before VAs. It is only read statically with PE/Capstone. Original configuration `C:/Stardust-DEV/swgemu_live.cfg`: SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective priority-42 `terrain/tatooine.trn`: 1,188,446 bytes, SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. TGEN offset 210, 139,608 bytes, SHA-256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.

Frozen Round 39 has 3,081 files; manifest SHA-256 `4cac8ae4fe7a5315d8958682151ae982ec85de7f41f089afc8e8694ccf3a6e65`, ZIP SHA-256 `b390959b5cb8e53e40523ea3764e7388a7975ca5708f897873d557e95b92dacb`. Complete member/listed-set, uniqueness/casefold, size/hash/containment and every target/ancestor symlink/junction/reparse preflight preceded writes; all-file postflight passes. Private/unlisted files, worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` and empty index were preserved. Frozen main HEAD remains `5493338d9cd4255cf58f25de6c4cd15f6507b818`.

## Direct evidence and interpretation

**Confirmed fresh state:** `00B72989` clears transform +3C; constructor initializes +40/+50 to one and other transform words zero. Base `00B63333/36` clears selector/amount. The four BREC loaders do not write transform flag/matrix or call transform-enabling methods. Runtime methods `00B72FE0` and `00B73000` set +3C and call matrix helpers; query uses `00ABB330` only on enabled-transform branch. Their caller/state semantics remain open. The evaluator cannot represent transformed/editing state, so raw input is sufficient only for its explicit fresh domain.

**Confirmed loading versus editing:** versions 2/3 compare serialized amount with zero and one, clamp ordered out-of-range finite values, and pass the result through vtable +10 to `00B72AA0`. That setter calls direct store `00B65FC0`, then normalization/cache `00B72AC0`; the setter itself is unclamped. Known dispatch paths call the cache after loading. Older versions retain zero runtime defaults. Serialized input and unknown extension metadata remain untouched.

**Confirmed cache graph:** `00B72AC0` compares/switches raw endpoint words only when greater. First extent is F32 at `00B72AFE`; second extent has F32 store `00B72B07` but remains unspilled for comparison at `00B72B0A`. Branch `00B72B15` selects the smaller extent's stored F32 word. Multiplications at `00B72B21/28` apply amount and 0.5. Inner corners are individually stored F32 at `00B72B47/4E/56/5E`. Cache/query profiles are separate because later query/shaping can occur on the terrain worker; no original active control word is proven.

**Confirmed query graph:** outer bounds are inclusive, with outside priority; amount numerically zero returns one; inner bounds are inclusive with endpoint-order handling. Feather branch stores four edge distances at `00B72D27/30/39/42`, recomputes extent stores at `00B72D4B/54`, then denominator at `00B72D6A/6C`. The selected minimum starts at the denominator, replacing only on strictly smaller distance. `00B72DB8` returns distance/denominator unspilled. There is no query influence store. Raw inverted endpoints normalize; degenerate bounds still participate in inclusive tests.

**Confirmed caller boundary:** local enabled checked at `00B63F8E`, query +24 at `00B63FAF`, unsigned selector 0..3 through table `00B642A0`. Identity, square, square root and `(3-2u)*u*u` preserve instruction order; larger selectors give positive zero. Final shaped F32 store is `00B63FE0`. Ancestor/filter/product/inversion handling is not implemented.

**Unknown:** original active rounding/precision/exception states, transformed/editing semantics, axes/units and full generated terrain. Modern finite-input/spill restrictions and bounded rejection are implementation policies, not evidence that the original safely rejects those inputs. Compatibility consequence: preserve the proved stores and phase graph under later-established profiles; do not promise original parity from these conditional vectors.

## Owned API and fixtures

Cache/query receipts retain immutable bounds, raw input fields, runtime-clamped amount, profile and spill receipts. Query retains the exact unspilled value for Shape; its projection is inspection only. Batch publication is atomic after all capped samples succeed; inputs are snapshotted, result collections read-only, influence copies independent, prior receipts unaffected by later failure. Disabled Evaluate skips unused arithmetic; direct Query remains active. Unsupported opaque input cannot be evaluated.

Independent Fraction/integer rounding uses accepted arithmetic primitives, derives its own cache/query branches and data framing, and never obtains expectations from native output. Synthetic vectors cover both phase profiles, all selectors, raw inverted/signed-zero/subnormal/degenerate corners, outer/inner/feather edges, amount clamp/default/zero/negative/above-one behavior, disabled versus direct query, ignored nonfinite extension scalar, nonfinite active input and overflowing stores. Ownership/cap tests cover exact/one-less sample lengths, empty batches, aliased coordinates, copied inspections, read-only mutation, cached-query overload, null/invalid profiles, opaque inputs, late failed batches and retained receipts.

One real raw occurrence from the existing 29-record set is independently compared: layer 11, child 2, TGEN offset 18,468, BREC `0003`, IHDR `0001`, 127 encoded bytes, SHA-256 `9EE02CA343E592468008D430FF3E5F85DC60A8A494D389DAC635BFAFFA94EF15`. Enabled bit is 1. Ordered words and identity match the frozen independent raw verifier. No original influence query or new corpus sweep occurs.

## Commands and results

Use .NET 8, Python UTF-8 and existing cached PE/Capstone packages. The package path in this environment is `C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages`; provide it explicitly to verification. Offline NuGet configuration contains `<configuration><packageSources><clear/></packageSources></configuration>`, with `NUGET_PACKAGES` set to the existing cache and task-specific temporary APPDATA. No download or original process is needed.

```powershell
python poc/round39-tests/Assets/verify_rectangle.py generate local-private/round39/fixtures.json
dotnet restore poc/round39-tests/Assets/Assets.csproj --configfile local-private/round39/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round39-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round39-tests/Assets/bin/Debug/net8.0/Assets.dll local-private/round39/fixtures.json local-private/round39/native.json C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
python poc/round39-tests/Assets/verify_rectangle.py verify local-private/round39/native.json C:/Stardust-DEV/Stardust.exe C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round39/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
python tools/python/validate_organized_workspace.py .
git diff --check
```

Passed: 10,244 vectors, 9,285 accepted/959 rejected; 32 ownership/budget/identity checks; six null negatives; 73 arithmetic anchors plus four constants/selector table/transform absence assertions; frozen raw-input verifier's 46 anchors separately rechecked; one original raw occurrence. Independent sensitivity checks find 300 query-profile-sensitive inputs, 456 cache-profile-sensitive inputs and 414 premature-projection/shaping differences. Both isolated runner and actual separate Core/default smoke projects build with zero warnings/errors; default smoke execution exits zero.

No build, oracle or fixture check failed. Discovery corrections: a PowerShell `rg` wildcard path for private assembly files returned OS error 123 and was replaced by explicit paths. Initial range starts `00B72F10` and `00B63F80` fell within instructions; those prefixes were discarded, evidence was read from valid function starts or exact instruction anchors. An early commentary transform-method address `00B72FD0` was corrected to the verified `00B72FE0` before use in code/evidence. No claim relies on those invalid prefixes or the incorrect address.

## Resume point

The fresh finite conditional graph is closed. Next bounded probe: trace `00B72FE0`/`00B73000` callers and matrix helper behavior to define explicit transformed rectangle state, coordinate storage and lifecycle before adding a transform API. Stop before full layer/filter or procedural generation. Original PC/RC/exception state remains a separate policy investigation.

## Final review

Baseline-relative scope passes: seven additions, zero existing-file modifications/deletions, all 3,081 frozen public files byte-identical. Exact four-file assignment plus three own runner files is preserved. Worker HEAD is unchanged, index empty; private receipts and build outputs ignored. Focused implementation/oracle/evidence changes and Git status were inspected. Per-addition whitespace checks and `git diff --check` pass.

Workspace validation passes: 1,385 Markdown files, zero broken links, zero links into private evidence, 2,626 provisional rows with zero duplicate addresses and 345 packet rows unchanged. Shared production/living documents remain frozen. No staging, commit, push, download, original executable execution, live-server contact or GPU work occurred. Ready for lead review, then idle.
