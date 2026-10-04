# 2026-10-03 Round 37: owned BPOL serialized input

## Question and stopping condition

Can an engine-neutral decoder preserve the exact ordered BPOL version `0000`–`0006` serialized input, with bounded ownership and malformed-input behavior, and reproduce all 57 previously pinned Tatooine polygons? Stop after factory/version/header/point/metadata/default/cache/teardown evidence, independent fixtures and that existing corpus agree. Polygon influence, coordinate semantics, layer/filter composition and new corpus discovery are excluded.

The durable conclusion is [the BPOL input contract](../../docs/TERRAIN_BOUNDARY_POLYGON_INPUT_CONTRACT.md). Implementation is [TerrainBoundaryPolygonInput.cs](../../poc/Stardust.Core/Assets/TerrainBoundaryPolygonInput.cs), with [native scenarios](../../poc/Stardust.Core.SmokeTests/TerrainBoundaryPolygonInputScenarios.cs), an [isolated runner](../../poc/round37-tests/Assets/Assets.csproj) and [independent Python verifier](../../poc/round37-tests/Assets/verify_bpol_input.py). Shared canonical files remain frozen for lead reconciliation.

## Input identity and tools

The static baseline is `C:/Stardust-DEV/Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before relying on virtual addresses. Original executable execution was not used. Existing Python PE/Capstone helpers provide static reads; .NET 8 executes only the new managed decoder and existing smoke tests.

Pinned root: `C:/Stardust-DEV`; configuration: `swgemu_live.cfg`, SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective `terrain/tatooine.trn` at priority 42 has 1,188,446 bytes and SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. Its TGEN starts at offset 210, spans 139,608 bytes, and hashes to `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. Existing overlay precedence and extraction helpers are reused. No proprietary source bytes or generated disassembly are added to curated files.

Frozen Round 37 contains 2,981 public files; manifest SHA-256 `de3bf3446f9233c613a848c251494e06ff1b67b6a1614b65e6ee24e5964f5f5f`, ZIP SHA-256 `9ba86cda0979b203eea36ff52dcdfaccfb9ea637982e94f9ced53bd7cea741bf`. Sync validates every member identity, size, digest, path, case-fold uniqueness and containment, including symlink/reparse ancestors, before writing. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`; frozen main HEAD is `5493338d9cd4255cf58f25de6c4cd15f6507b818`.

## Evidence chain

| Observation | Interpretation / confidence | Implementation consequence |
| --- | --- | --- |
| `00B79E1A` compares BPOL; `00B79F29` constructs through `00B73BA0`; `00B79F40` calls slot +8. | BPOL factory ownership and loader path, Confirmed. | Decode this family explicitly; reject wrong known family. |
| Vtable `015ED434`, dispatcher `00B73FE0`, table `00B7412C` selects seven loaders. | Versions 0000–0006, Confirmed. | Preserve version-dependent order; opaque unknown versions. |
| `00A94EE0(8)` derives remaining pair counts in versions 0–4; versions 5/6 read signed counts at `00B7469D`/`00B747ED`. | Implicit versus explicit point extent, Confirmed. | Enforce exact pair divisibility and bounded nonnegative explicit counts as modern safety policy. |
| Pair reads `00B746B8`/`00B746CB` and `00B74808`/`00B7481B` use width four and store at offsets zero/four with stride eight. | Ordered raw pair words, Confirmed. | Preserve raw bits and order without coordinate assignment. |
| Version 6 reads a discarded word at `00B748CE`, then string at `00B748D5`. | Extra word precedes name, Confirmed; meaning Unknown. | Retain it in raw metadata; do not adopt server 0007 order. |
| Common IHDR loader `00B62FA0`; version 0 pushes one-byte reads at `00B630D2`/DF/EC. | Enabled U32 + NUL name, plus three version-0 bytes, Confirmed. | Reuse established owned header convention. |
| `00B65FC0` directly stores amount +18. | No loader clamp, Confirmed. | Preserve nonfinite and out-of-range raw values; finite inspection is separate. |
| Constructor initializes +40 to `0x40000000`; version 3 never reads that word. | Runtime default differs from serialized presence, Confirmed. | Absent serialized properties stay null. |
| `00B738B0` initializes bounds and compares both float components from the pair vector. | Runtime cache computation, Confirmed; axes/units Unknown. | Document cache separation; do not compute or expose inferred bounds. |
| `00B73C00` frees +44; `00B73740` frees +20, resets vector, then base teardown frees IHDR name. | Legacy owned lifetime, Confirmed. | Publish managed immutable owned data atomically; failure leaves prior models intact. |

All known loader read-site counts are 2/3/4/6/7/8/9. These count static read sites, not dynamically expanded point reads. Legacy implicit division floors partial pairs and nonpositive explicit counts skip their loop; stricter modern rejection must not be reported as recovered legacy rejection. Unknown-version error recovery and unsafe extent handling are not closed.

## Validation and reproducibility

Run from the repository with Python static-analysis dependencies available and .NET 8 installed. The verifier generates synthetic fixtures explicitly; original records are independently selected from the pinned overlay.

```powershell
python poc/round37-tests/Assets/verify_bpol_input.py generate local-private/round37/fixtures.json
dotnet restore poc/round37-tests/Assets/Assets.csproj --configfile local-private/round37/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round37-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round37-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round37/fixtures.json > local-private/round37/native.txt
python poc/round37-tests/Assets/verify_bpol_input.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round37/native.txt > local-private/round37/python-audit.json
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round37/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
python tools/python/validate_organized_workspace.py .
git diff --check
```

The offline NuGet configuration has cleared package sources; restore uses the already installed runtime/cache. Private files are regenerable receipts, not curated dependencies. Required inputs are identified above; no download or original executable run is needed.

**Passed:** 670 synthetic cases, 146 accepted and 524 rejected; 19 ownership checks; 71 static instruction anchors plus version/vtable/read/store/header/callback/teardown assertions; all 57 original records agree byte-for-byte in ordered raw fields and source hashes. Exact and one-less budgets, every short DATA extent, full-record truncations, explicit count disagreements, unknown versions/headers, string termination, malformed tags/order and signed-zero/nonfinite retention are covered. Copies of source/points/metadata/names and the read-only point collection resist mutation. Section decoding, failed subsequent parsing and null/invalid options are checked.

Both the isolated runner and the actual combined Core/default smoke-test project build with zero warnings and zero errors. The default smoke-test DLL exits zero. No failed checks or repair attempts occurred during this round.

Original aggregate: 406 points total, 4–18 per polygon; all 57 BPOL `0005` and IHDR `0001`; 54 locally enabled; selector 0/1/2/3 counts 19/3/2/33; four nonzero extension flags; all point/amount words finite. Maximum retained logical decoded-copy payload is 181 bytes. These observations cover the same existing 57 records, not a new corpus survey.

## Remaining uncertainty and next probe

Extension flag/scalar/name meanings remain Unknown; server names are only leads. Geometry axes/units, polygon influence, runtime cache precision/exceptions and ancestor/filter shaping are excluded. The next bounded probe is to trace extension getter `00B73C60` and consumers of +3C/+40/+44 to establish whether each field is used, under which discriminator, and with what ownership. Stop at those consumers before implementing influence arithmetic.

## Final scope review

Baseline-relative review passes: seven additions, zero existing-file modifications or deletions, all 2,981 frozen public files byte-identical. Frozen interfaces and worker HEAD are unchanged; the index is empty. Private receipts and build outputs are ignored. Per-addition whitespace checks and `git diff --check` pass; the focused changes were inspected.

Workspace validator passes: 1,360 Markdown files, zero broken links, zero links into private evidence, 2,626 provisional rows with zero duplicate addresses, and 345 packet rows. The seven additions comprise the four assigned production/scenario/contract/checkpoint files and three isolated runner/oracle files. No staging, commits, publication or canonical metric updates were performed. This round is ready for lead review and reconciliation.
