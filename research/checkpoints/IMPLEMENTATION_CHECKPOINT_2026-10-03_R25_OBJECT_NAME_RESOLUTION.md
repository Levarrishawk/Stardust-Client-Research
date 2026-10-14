# Round25 stateless object-name resolution — 2026-10-03 UTC

## Question and stopping condition

Can current owned creature names produce bounded display text with custom precedence, exact STF key identity and table-load-only fallback, without hidden I/O or callback retention? Stop after synthetic policy/error/ownership gates, one lookup per two pinned original tables, prior STF/default regression and exact scope checks. Locale manager/cache, original UI wording and variant 0 remain deferred.

Orientation: START/latest accepted24/session25, status/ledger/UNKNOWN localization entries, curated localization names, [September runtime investigation](ANALYSIS_CHECKPOINT_2026-09-21_LOCALIZATION_STF_RUNTIME.md), [Round24 table evidence](IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_STF_TABLE.md), durable STF/asset contracts and existing parser/oracle/native tools. No new VA claims or static executable input are used.

## Input identity and scope

Authoritative Round25 snapshot: 2,558 public files; manifest SHA-256 `3c1f730fe25ab6570e60287d21e55dae143575d21611e89a648e886ad6e27b5f`; ZIP `d546b369229b42415b92a4a7e92131e2a30d4f1a945633c6de4a0a6cb025c334`. Complete unique member set, per-file sizes/hashes, lexical/resolved destination containment and archive/destination/ancestor symlink or Windows reparse-point absence were checked before overlay. All snapshot hashes verified afterward. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` and empty index preserved; snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`. Unlisted/private files retained.

Readonly original root `C:/Stardust-DEV`; `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Exactly two unique original STF inputs, both priority 53:

| Path | Bytes | SHA-256 | Total records | Selected lookups |
|---|---:|---|---:|---:|
| `string/en/obj_n.stf` | 2527 | `3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054` | 40 | 1 |
| `string/en/terminal_name.stf` | 4610 | `CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603` | 57 | 1 |

Selection is the first value-pass record of each table, joined to its exact name-pass key. This is a deterministic format/presentation test, not proof that either key is an actual replicated object-name reference. No broader lookup/discovery is performed; original text/raw keys stay in ignored private receipts.

Seven additions: [resolver](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/LocalizedObjectNameResolver.cs), [native scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/LocalizedObjectNameScenarios.cs), [contract](../../docs/LOCALIZED_OBJECT_NAME_CONTRACT.md), this checkpoint, [project](../../../SWG-Godot-Client/poc/round25-tests/Assets/Assets.csproj), [entry point](../../../SWG-Godot-Client/poc/round25-tests/Assets/Program.cs) and [oracle](../../../SWG-Godot-Client/poc/round25-tests/Assets/verify_names.py). Frozen name state/named plan/snapshot forwarder, shared projects/Program, canonical ledgers and main preview remain unchanged. Lead owns registration and canonical reconciliation.

## Evidence, interpretation and consequences

**Observation / Confirmed prior evidence:** runtime table loading builds a `.stf` VFS path, retries an alternate path after primary load failure, maintains exact narrow keys and numeric IDs, and distinguishes invalid table from invalid name. Reused accepted variant 1 reader retains raw Unicode units and keys. No new executable observations are needed for the stateless slice.

**Implementation / Confirmed modern policy:** nonempty custom raw units override StringId. Unsafe table/path or oversized key returns an invalid-name status without callback; explicit directory/output option errors throw. Exact primary match succeeds; a missing primary table may try one explicitly distinct fallback. A loaded table with missing key never searches fallback. Callback exceptions propagate unchanged. Diagnostic wording, custom precedence and restrictive path alphabet are modern choices, not recovered UI behavior. See the contract for all outcomes and limits.

**Ownership / Confirmed:** no model or table mutation, hidden I/O/cache/jobs, retained callback or partial publication. Raw baseline fields are borrowed from immutable frozen state, with length checks before key copying. Display walks only a bounded prefix; no full-string conversion. Output preserves complete surrogate/escape tokens, flags unconsumed raw units, and never appends ellipsis. Same-thread nested callbacks poison the outer frame even when swallowed, then recover through `finally`; independent threads remain permitted. Callback external side effects are outside rollback.

**Compatibility consequence:** callers must supply owned tables and explicit directory policy, translate intended recoverable absence to `null`, and use status/metadata separately from text. Raw StringId/spacer/custom units stay in replication state. This resolver can be joined to an optional label consumer without synchronous archive work inside a frame. No locale config, font parity or cache lifetime is inferred.

## Validation and reproduction

Offline cached .NET 8 restore/build with cleared sources/task APPDATA; final build zero warnings/errors. Independent Python producer supplies 89 vectors covering custom/empty precedence, exact case/high/NUL/empty keys, empty localized success, missing table vs key, fallback attempt bounds, unsafe names/directories, exact identifier/directory/key/output limits and every output limit 0..21 across control/backslash/surrogate tokens. Fourteen controlled native checks cover exception identity, unswallowed/swallowed/custom reentry, recovery, 70,000-unit/byte baseline fields, owned update preservation, prior-result stability, surrogate boundary, cross-thread callback, malformed-table exception and fallback exception. Thirty-two independent concurrent calls pass.

Original lookup receipts independently agree with Python parsing of all record extents and the selected exact key/raw units/display/path/ID. Native archive handles close before parsing and resolving; input arrays are cleared after table ownership. Fresh Round24 regression/oracle passes 102 fixtures, 98 malformed and six budgets plus all 97 original records. Default/albedo regression passes 16 reader negatives/two initializer failures with unchanged 37/39 modes. No GPU or live object-name verification is claimed.

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round25-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config: <configuration><packageSources><clear/></packageSources></configuration>
& $python poc/round25-tests/Assets/verify_names.py generate local-private/round25/fixtures.json
dotnet restore poc/round25-tests/Assets/Assets.csproj --configfile local-private/round25/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round25-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round25-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round25/fixtures.json > local-private/round25/native.txt
& $python poc/round25-tests/Assets/verify_names.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round25/native.txt
& $python poc/round24-tests/Assets/verify_stf.py generate local-private/round25/stf-fixtures.json
dotnet poc/round25-tests/Assets/bin/Debug/net8.0/Assets.dll stf-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round25/stf-fixtures.json > local-private/round25/stf-regression.txt
& $python poc/round24-tests/Assets/verify_stf.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round25/stf-regression.txt
dotnet poc/round25-tests/Assets/bin/Debug/net8.0/Assets.dll default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json
& $python tools/python/validate_organized_workspace.py .
```

Final scope review: all 2,558 baseline files unchanged, exactly seven additions, unchanged worker HEAD, empty index and ignored build/private receipts. Workspace links and no-private-link checks pass; focused diff/whitespace/EOF inspected.

## Resume point

This bounded resolver milestone is complete. Next join current named-wire state to an optional consumer using an explicit owned-table provider; separately specify locale/config selection, cache lifetime and load-error-to-absence policy. Variant 0 remains unsupported. Two original lookups do not establish whole localization or object-name coverage.
