# Round22 ACST directory — 2026-10-03 UTC

## Question and stopping condition

Can the confirmed twelve ACST tables become an owned bounded directory with exact direct/inherited precedence and order-independent CRC queries, preserving the shipped appended root? Stop after structural/cycle/budget fixtures, original schema aggregates and independent human/Togruta queries agree. Palette contents, full current seed, live mounts/linked variables and GPU application remain deferred.

Orientation included START/latest21, status/ledger/UNKNOWN, September12 manager checkpoint, current appearance/customization durable docs, existing IFF/TRE/CRC APIs and inspect_customization_managers.py. This implements prior proven schema rather than new VA research. No executable address was newly relied upon, no original execution/live contact/download/static naming/count-coverage change.

## Inputs, tools and scope

Round22 public snapshot 2,443 entries. Manifest SHA-256 `ef026386b3fcb2bbb0c03340e75e761dcd62f856798dda7ff1f00aa917894ecb`, ZIP SHA-256 `2e52e818dbcd3a713bfb0827b6b3663c98573371fe47f562997c6cedfcd01746`. Exact entry set, every size/hash, path containment and ZIP/lexical destination/ancestor symlink or Windows reparse-point absence checked before writes; every overlaid file rehashed. Snapshot bytes authoritative; worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` and empty index preserved. Unlisted/ignored/private bytes preserved.

Readonly root `C:/Stardust-DEV`, configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. ACST priority 53 `stardust_3_01.tre`, 458,340 bytes, SHA-256 `5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20`. Cached .NET8/offline private NuGet/task APPDATA and bundled Python/existing raw TRE index only. Original payloads read into memory, archives dispose before decode; bytes clear afterward. No copied original table fixture, 26k-query dump or POB scan.

Seven additions: [directory](../../poc/Stardust.Core/Assets/AssetCustomizationDirectory.cs), [native scenarios](../../poc/Stardust.Core.SmokeTests/AssetCustomizationDirectoryScenarios.cs), [contract](../../docs/ASSET_CUSTOMIZATION_DIRECTORY_CONTRACT.md), this checkpoint, [project](../../poc/round22-tests/Assets/Assets.csproj), [entry point](../../poc/round22-tests/Assets/Program.cs), [generator/oracle](../../poc/round22-tests/Assets/verify_directory.py). Exact four core/doc/checkpoint paths match supplied Assets allowlist; other additions are own test directory only. Shared projects/Program/preview/navigation/status/ledger/registry and frozen scalar/customization union remain untouched. No unfinished worker dependency.

## Evidence, findings and consequences

**Observation / Confirmed prior research:** twelve record widths/indices and direct-before-inherited first-name semantics are established in [September12 evidence](ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_MANAGER_TABLES.md). That evidence also identifies malformed CIDX tail, signed range/default fields, palette-vs-range RTYP and shared-owner suppression. Existing analysis oracle was inspected as correlation/reference; new Python verifier independently composes only the two targeted original roots and synthetic fixtures.

**Implementation / Confirmed modern policy:** exact ordered ACST0000 chunks parsed by existing IFF. All table widths, NAME start offsets, one-based references, zero-based slices, target types/min<exclusiveMax and root/link keys validate before publication. Every graph key is cycle/height checked, including unreachable keys. Duplicate key rows within an index and duplicate CRCs fail; same key in UIDX/LIDX and repeated component/name/inheritance references are legal. Duplicate NAME spellings at different starts remain valid and diagnostic. Exact names, signed defaults and bounds are retained. Authored defaults are metadata, including outside a range; palette count is not guessed. Resource/directory/query admission is explicitly bounded and atomic.

**Observation / Confirmed original bytes:** all table counts match prior evidence: NAME 9730 bytes/336 strings, PNOF 226, VNOF 110, DEFV 193, IRNG 28, RTYP 254, UCMP 1079, ULST 30108, UIDX 11594, LLST 33308, LIDX 20359, CIDX 26080; known key union 26082. These are schema reproducibility aggregates, not a new coverage denominator. One raw inversion row 26079 `FFFD72EB -> E201EE84` is retained; no sorted-index rejection or binary-search miss in replacement lookup.

**Independent targeted query / Confirmed:** human SAT CRC 3607156086/root 11821 produces 36 variables,8 suppressed; Togruta CRC 3791777412/root 52048 produces 6,1 suppressed. Every output full name, order, signed default/range and palette path agrees with independent Python direct/inherited expansion. The directory uses explicit CRC; optional already-normalized path matches existing Stardust CRC and rejects mismatches. Unknown root reports Found=false/null key/empty values.

**Synthetic fixtures / Confirmed:** 50 cases include five positives (all 12 chunks/inherited precedence, redundant NAME spelling, empty directory, signed Int32 extrema, out-of-range authored default) and 45 malformed schema/reference/width/cycle/envelope cases. Seven budget negatives cover source/table/name/depth/validation visits/query visits/expanded values; two normalized-path/CRC failures. Exact source/output limits, input snapshot, immutable metadata/results, prior-result preservation and 32 concurrent independent queries pass. The unreachable self-cycle proves validation is not restricted to CIDX roots. Query visits charge repeated edges; visited-key reuse preserves deterministic first-name order.

Build zero warnings/errors. Fresh default/albedo regression passes 16 reader negatives/two initializer budget failures, 37/39-source modes unchanged. Fresh CIDM regression passes 89 rejections and all 121 original rows. No GPU run; all such integration remains lead-owned this round.

## Reproduction

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round22-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
$python='C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python poc/round22-tests/Assets/verify_directory.py generate local-private/round22/fixtures.json
dotnet restore poc/round22-tests/Assets/Assets.csproj --configfile poc/round19-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round22-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round22-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round22/fixtures.json > local-private/round22/native.txt
& $python poc/round22-tests/Assets/verify_directory.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round22/native.txt > local-private/round22/python-audit.json
dotnet poc/round22-tests/Assets/bin/Debug/net8.0/Assets.dll default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round22/default-regression.txt
dotnet poc/round22-tests/Assets/bin/Debug/net8.0/Assets.dll dictionary-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round22/dictionary-regression.txt
& $python tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Scope/link/whitespace/new-file EOF receipts accompany handoff: exact seven additions and all 2,443 baseline files unchanged; frozen union, HEAD/index and unrelated bytes preserved. No stage/commit/push/publication. Curated files have no private links; detailed original results/generated fixtures/build artifacts are ignored/private. Scope uses real private zero-byte baseline for no-index checks on new files.

## API and exact next action

`AssetCustomizationDirectory.Load(bytes, limits)` then `Query(filenameCrc, suppressSharedOwner=false, normalizedAssetPath=null)`. Query exposes Found/RootKey/typed immutable Variables. Concrete types `AssetPaletteCustomizationVariable(FullName,PalettePath,AuthoredDefault)` and `AssetRangedCustomizationVariable(FullName,MinimumInclusive,MaximumExclusive,AuthoredDefault)` share `AssetCustomizationVariable`. Metadata has owned counts/key union/name duplicate count/CRC inversion list. Register `AssetCustomizationDirectoryScenarios.Run(new[]{archiveRoot,configPath,fixturePath})` with generated fixtures.

Lead's next bounded action is resolve selected palette paths into owned entry counts, then construct an explicit full current-value context against these descriptions without confusing authored defaults with current player state. Actual remote mount/linked variable construction/notifications and palette fallback remain unresolved runtime boundaries; this reader does not claim them. No necessary out-of-scope change found. Stop after Round22 handoff.
