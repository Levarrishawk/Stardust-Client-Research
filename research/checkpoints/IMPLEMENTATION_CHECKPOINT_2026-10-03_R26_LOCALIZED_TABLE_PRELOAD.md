# Round26 explicit localized table preload — 2026-10-03 UTC

## Question and stopping condition

Can a finite explicit set of STF resources be validated and preloaded atomically, preserving raw identity and exact counters while bounding reads, allocations, cancellation and callback reuse? Stop after independent synthetic/original byte gates, resolver/STF regressions and scope validation. No locale discovery, lazy loading, background work, cache expiry, original executable run or live contact.

Orientation: repository AGENTS, START/latest accepted25/session26, status/ledger/UNKNOWN and curated localization rows, accepted [STF contract](../../docs/LOCALIZED_STRING_TABLE_CONTRACT.md), [name contract](../../docs/LOCALIZED_OBJECT_NAME_CONTRACT.md), prior localization/table/name checkpoints and existing reader/oracle/resource/cancellation tools. No new VAs or executable observations are used.

## Input identity and scope

Round26 authoritative snapshot: 2,596 public files. Manifest SHA-256 `2f5f65bd69ac86003ab9ddb14ecb01e411d4702a069934147a2d57d2607eecc9`; ZIP `f847efa6c85734f9078c53eeb55dda7a33bafac6aee89bfed0178121b12010ad`. Complete unique archive/manifest member set, all sizes/hashes, lexical/resolved destination containment and absence of archive/target/ancestor symlink or Windows reparse points preflighted before overlay writes. All members reverified afterward. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`, snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`, empty index and unlisted/private files preserved.

Readonly original root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Exactly two unique original tables, both configured priority 53:

| Path | Bytes | SHA-256 | Records | Value units | Key bytes |
|---|---:|---|---:|---:|---:|
| `string/en/obj_n.stf` | 2527 | `3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054` | 40 | 561 | 592 |
| `string/en/terminal_name.stf` | 4610 | `CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603` | 57 | 1163 | 1131 |

Original aggregate exact limits: two paths, 7,137 source bytes, 97 records, 1,724 units and 1,723 key bytes. No broader corpus query or discovery. Detailed original rows/text/keys are ignored private output.

Seven additions: set (external historical reference), scenario (external historical reference), [contract](../../docs/LOCALIZED_STRING_TABLE_SET_CONTRACT.md), this checkpoint, project (external historical reference), entry point (external historical reference), independent oracle (external historical reference). All baseline/frozen/shared files, main preview and canonical ledgers remain unchanged; lead owns registration/consolidation and the subsequent world join.

## Evidence and implementation consequences

**Observation / Confirmed accepted byte boundary:** the owned variant 1 reader preserves raw header fields, CRCs, code units, exact keys and pass order; both pinned original tables exhaust the verified schema. The new independent oracle reads their bytes through configured archive precedence and agrees with every retained record plus aggregate counts. This confirms the selected preload inputs, not locale selection or object-name references.

**Implementation / Confirmed modern policy:** Build validates/snapshots the complete bounded request enumeration before any callback. Paths are exact safe relative `string/<directory>/<identifier>.stf` identities and duplicates reject. Null is explicit absence, while present empty or malformed data rejects. Aggregate source preflight precedes copying; remaining record/unit/key budgets narrow each individual parser before field allocation. No hidden provider/cache or lazy lookup is introduced. See the contract for exact defaults/hard ceilings and exceptions.

**Ownership / Confirmed:** private source snapshots clear in finally without clearing caller buffers. Each table finishes ownership before the next callback reuses memory. Requested-path mutation after preflight cannot alter the result. Read-only tables/missing/request maps preserve canonical objects. Cancellation and same-thread reentry during enumeration/callback/snapshot preparation reject the entire build and recover the guard; prior results are stable. Find is a pure validated ordinal lookup. Callback side effects and concurrent buffer mutation are outside rollback/detection.

**Compatibility consequence:** build this explicit provider outside rendering/frame callbacks and pass Find to the accepted name resolver. Locale choice and font policy remain explicit caller concerns. Variant 0 is still unsupported. Set budgets are decoded/source quantity bounds, not a CLR resident-memory estimate.

## Validation and reproduction

Cached offline .NET 8 restore/build with cleared sources/task APPDATA; final build zero warnings/errors. Independent synthetic corpus: 110 fixtures, seven positive /103 rejected. Positive exact limits use three paths (including one absence), 105 source bytes, three records, seven units and five key bytes. Individual exact bounds are 62 bytes, two records, four units/value, three bytes/key, four aggregate units/table and three aggregate key bytes/table. Five aggregate and six individual one-less cases fail with independently specified callback counts. Unsafe late paths/duplicates fail before reads; all 62 truncated prefixes and bad later variant/magic/orphan/trailing records fail.

Twenty-one controlled native checks cover immutable collections/pure Find, later callback exception identity, pre/first/last callback cancellation, final enumeration cancellation, unswallowed/swallowed/enumerator reentry, guard recovery, endless enumeration stopped at cap+1, callback request-list mutation, mutable buffer reuse, cancellation triggered while copying custom memory, independent callback thread and prior-result stability. Thirty-two concurrent independent builds pass.

The first native run exposed a test callback conditional that converted its missing response into a present empty memory value; the parser correctly rejected it. The adapter now returns explicit nullable null. The own entry point catches failures and exits nonzero, avoiding an unhandled Windows crash process retaining the test DLL. After stopping the failed own sessions, the fresh final build and full gate/oracle passed. No production policy was weakened.

Native original archives close before Build; test-owned source arrays clear afterward. All 97 exact key/ID lookups remain canonical. Independent Python confirms pins, table structure/order/CRC/raw units/keys and counters. Fresh resolver regression/oracle passes 89 vectors/14 controlled/32 concurrent/two lookups; fresh STF regression/oracle passes 102 fixtures/98 malformed/six budgets/all97 records. No GPU or runtime locale/cache behavior is claimed.

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round26-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config: <configuration><packageSources><clear/></packageSources></configuration>
& $python poc/round26-tests/Assets/verify_preload.py generate local-private/round26/fixtures.json
dotnet restore poc/round26-tests/Assets/Assets.csproj --configfile local-private/round26/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round26-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round26-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round26/fixtures.json > local-private/round26/native.txt
& $python poc/round26-tests/Assets/verify_preload.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round26/native.txt
& $python poc/round25-tests/Assets/verify_names.py generate local-private/round25/fixtures.json
dotnet poc/round26-tests/Assets/bin/Debug/net8.0/Assets.dll name-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round25/fixtures.json > local-private/round26/name-regression.txt
& $python poc/round25-tests/Assets/verify_names.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round26/name-regression.txt
& $python poc/round24-tests/Assets/verify_stf.py generate local-private/round25/stf-fixtures.json
dotnet poc/round26-tests/Assets/bin/Debug/net8.0/Assets.dll stf-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round25/stf-fixtures.json > local-private/round26/stf-regression.txt
& $python poc/round24-tests/Assets/verify_stf.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round26/stf-regression.txt
& $python tools/python/validate_organized_workspace.py .
```

Final scope/links/whitespace/EOF review verifies exactly seven additions, all 2,596 baseline files unchanged, frozen APIs preserved, unchanged HEAD/empty index, ignored private/build output, zero broken or private links. No original material is promoted to curated data.

## Resume point

Explicit atomic preload is complete. Next supply the set to an optional name consumer and specify caller locale/config and recoverable load-error policy. Reload/expiry/shared cache lifetime and variant 0 remain separate gaps. This stage does not claim original cache behavior or whole-corpus coverage.
