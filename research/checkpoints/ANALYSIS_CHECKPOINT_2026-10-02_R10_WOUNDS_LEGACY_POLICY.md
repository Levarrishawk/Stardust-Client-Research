# 2026-10-02 R10-NET-01: recovered integer-vector kernel

Question: what exact counter arithmetic, count/cursor semantics and remove/insert/replace/assign/clear behavior does the pinned integer-vector decoder implement? Stop after static recovery, selectable bounded policy and independent verification; no Round11 work.

## Inputs and bounded static plan

Round10 baseline HEAD `65de8c88141071744dc75594be126b3bab3696a5`, 2,000 files. Archive SHA-256 `b22195465dd313d509571ca0d732a5fffb058658d41ee2a41750b82448fb4e2a`; manifest SHA-256 `58c635c920215d7fbb47de5d8d7b9c636f96291b8b841e61593e195529072f82`. Membership, relative paths, destination containment and content hashes were checked before synchronization; all baseline files verified afterward. Ignored/unlisted private material preserved.

Pinned `C:/Stardust-DEV/Stardust.exe` SHA-256 confirmed before VA use: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Read-only static safety plan: exact 1,022-byte decoder extent plus direct mutation, checked-read, common-caller, copy and nullable callback-adapter extents. No target execution, live/socket/DNS contact, captures, fuzzing or downloaded dependencies. Original-derived exports remain ignored/private. The inventory and cached Capstone/pefile packages came from the accepted main private research/toolchain paths.

Static exporter outputs total 4,351 bytes: vector1,022; mutation/read helpers1,764; snapshot/size/common-caller/read context596; copy helper821; callback adapters148. Extents are inclusive VA ranges, not Ghidra discontiguous body-size counts. Linear disassembly can include embedded table data; conclusions use verified instruction boundaries and separately inspected jump-table bytes. Full instructions and raw bytes remain private, with only small necessary anchors in curated verification/contract.

## Evidence, interpretation and consequence

The [durable contract](../../docs/CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md) records exact addresses, bytes, branches, helpers, confidence and intentional modern boundaries. Key observations:

- `0043B1E8/0043B1EA` perform wrapping U32 N-C+L; unsigned JBE at `0043B1F1` clamps to N. No signed-negative normalization or strict interval rejection.
- Assign increments the operation cursor per element at `0043B233` and `0043B528`, plus the common command increment. Thus raw N counts cursor steps, not always command records. Both parse and application policies must reflect this.
- Return path `0043B3B9-0043B3C9` has no forced incoming/max counter assignment. Actual mutation paths update the local counter: valid remove/insert/changed replace +1; assign +k+1; nonempty clear +1. Equal replacement, invalid removal and empty clear do not advance it.
- Gap insert grows to index and then inserts; replace grows to index+1. Both supply literal zero to the same resize helper. U16 maximum indices imply 65,536 entries. Unknown-command active/stale parsing differs and is rejected by the modern codec.
- Common field caller dispatches the delta virtual until bounded archive exhaustion. Checked reads reach the archive exception path. Mutation callbacks are nullable adapter dispatches, without direct counter changes in the adapters; downstream effects remain outside the recovered kernel.

Interpretation: an explicit legacy grammar/kernel can coexist with the modern strict policy. Confidence: Confirmed direct decoder/helper instruction branches, not source analogy. Consequence: add `CreatureWoundsApplicationPolicy.LegacyBounded` to Decode; retain raw operation bounds independently from parsed command arrays, retain policy in immutable plans, and preserve StrictInterval defaults. Existing original prose about final counter raising and simple command-record counts requires lead reconciliation; no shared docs/registries were edited. No new function names introduced.

Implementation bounds/transactionality remain modern: aggregate raw bounds and value budgets, checked growth before allocation, immutable owned plans/states, exact schema/end handling, object-ID match and atomic full-plan application. Unknown commands/mixed fields reject. No callbacks are reproduced. Exact original allocation failure/outer recovery and registration-specific downstream notification effects remain Unknown.

## Reproduce

From this isolated checkout, use the existing Python and cached static packages:

```powershell
$py = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONPATH = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages'
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 0043B1A0:0043B59D --output local-private/round10/vector.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 0043AD50:0043AF2D --range 0043A7F0:0043A8CB --range 004468F0:00446A54 --range 00446A60:00446A8C --range 00448620:0044882C --range 00439910:0043999A --output local-private/round10/helpers.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 0043B050:0043B194 --range 0043B5C0:0043B5C9 --range 0118AA00:0118AA5A --range 0118B030:0118B0D9 --output local-private/round10/context.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 0131D240:0131D574 --output local-private/round10/copy-helper.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 0044C6E0:0044C6F7 --range 0044C700:0044C727 --range 0044C730:0044C757 --range 0044C760:0044C78B --output local-private/round10/callbacks.json
& $py tools/python/verify_creature_wounds_legacy_policy.py --exe C:/Stardust-DEV/Stardust.exe --generate local-private/round10/fixtures
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r10'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
Set-Content local-private/round10/NuGet.Config '<configuration><packageSources><clear /></packageSources></configuration>'
dotnet restore poc/round10-tests/Networking/Networking.csproj --configfile local-private/round10/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round10-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round10/fixtures local-private/round10/export
& $py tools/python/verify_creature_wounds_legacy_policy.py --exe C:/Stardust-DEV/Stardust.exe --verify local-private/round10/export
& $py tools/python/verify_creature_view3_wounds_deltas.py --generate local-private/round10/strict-fixtures
dotnet run --no-restore --project poc/round9-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round10/strict-fixtures local-private/round10/strict-export
& $py tools/python/verify_creature_view3_wounds_deltas.py --verify local-private/round10/strict-export
```

The actual accepted R9 runner used the preserved `local-private/round9/fixtures` input and `local-private/round10/strict-export` output; the regeneration command above yields the same synthetic inputs. All exports stay ignored. R10 oracle checks 29 instruction anchors and five-entry switch table directly in the pinned PE, independently hand-packs messages and models EAX/ECX/EDX/EBX unsigned arithmetic and helper state effects. It does not read replacement code. Seventy-three native cases and encoded messages agree, including 49 high/low/wrap adversarial counter combinations. Assign+following-command/end/overshoot and skip-crossing cases discriminate incompatible parsers; equal replace/invalid remove discriminate counter policies. Native scenarios add raw-count encode budgets, late bounds rollback, repeated fields, maximum assignment/index, ownership and truncation checks. Lead may register `CreatureWoundsLegacyPolicyScenarios.Run()`; shared entry points remain untouched.

Next probe: compose the selected kernel with stable-incarnation wounds/scalar state in one atomic lifecycle transaction, then trace specific callback targets only if needed for presentation. Stop idle after this milestone; Round11 requires explicit dispatch. No broad/mixed delta or original/live execution claim, and no metric denominator changes.

Final validation: both focused policies pass; unchanged R9 Python oracle passes; R10 instruction-derived oracle matches all73 cases. Core/shared smoke projects build with zero warnings/errors (shared suite built, not fully executed). Workspace validator:1,090 Markdown files,zero broken/private links,2,602 provisional rows/zero duplicates,345 packet rows. Scope review finds eight allowed paths (one codec edit/seven additions),1,999 other baseline files unchanged, unchanged HEAD and empty index. Diff whitespace check and focused baseline-relative codec diff inspection pass. New files are ASCII UTF-8/LF; existing codec retains accepted LF convention. Export metadata reports Capstone module5.0.7 and pefile2024.8.26 from the cached package path; no package installation occurred. Final baseline-relative file SHA-256 manifest, focused diff and status are retained in ignored Round10 evidence.
