# Round20 customization ID dictionary — 2026-10-03 UTC

## Question and stopping condition

Can the replacement load the proven CIDM/0001 dictionary into bounded owned records, preserve every original ID/name and project only explicit known namespaces? Stop when synthetic positive/negative/ownership fixtures, all 121 original rows and an independent byte oracle agree; leave packed value decoding and state/presentation integration to their assigned work.

Read START_HERE/latest accepted Round19, current status/ledger/UNKNOWN, appearance customization durable documentation, the September12 customization-manager checkpoint, original inspection tool and existing IFF/TRE/namespace APIs. Prior evidence defines the schema; no executable-address claim is newly investigated or relied upon. Original executable execution/live contact and corpus expansion are absent.

## Input identity and scope

Round20 full public snapshot: 2,374 manifest entries. Manifest SHA-256 `52d1c308ffd18481ff6a30ccc334a59c98f9dd700b201838e1bb83dcc3fee167`; ZIP SHA-256 `5f379b875a8c3af1c19b8fe1607851c4a252c395c893b322a795bb82d47048e5`. Every entry's path containment, ZIP/destination/ancestor symlink absence, size and SHA were checked before writes; all manifest hashes rechecked after overlay into the specialist checkout. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`; snapshot bytes define baseline. Ignored/unlisted files and empty index preserved. Both frozen new interfaces and all existing source remain unchanged.

Readonly original root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective CIDM path `customization/customization_id_manager.iff`, priority 53 `stardust_3_01.tre`,3,697 bytes, SHA-256 `FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2`. Cached .NET8 and bundled Python only; offline package source, no downloads. Detailed original row output stays ignored/private.

Allowed additions only: [reader](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/CustomizationIdDictionary.cs), [native scenarios](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CustomizationIdDictionaryScenarios.cs), [contract](../../docs/CUSTOMIZATION_ID_DICTIONARY_CONTRACT.md), this checkpoint, own [project](../../../SWG-Godot-Client/poc/round20-tests/Assets/Assets.csproj), [entry point](../../../SWG-Godot-Client/poc/round20-tests/Assets/Program.cs) and [oracle](../../../SWG-Godot-Client/poc/round20-tests/Assets/verify_cidm.py). No shared Program/project/navigation/ledger/status/registry edits, same-round dependencies or Git mutations.

## Evidence, interpretation and compatibility consequences

**Observation / Confirmed:** September12 checkpoint established no-count U16LE/NUL ASCII records, sequential IDs 1..121, duplicate-name diagnostics and IDs below128. Original byte oracle independently verifies exact FORM CIDM/0001/DATA tags, big-endian extents, full exhaustion, every ID/name and pinned resource. Native output matches all 121 rows. Structure is recovered; values and palette semantics are not part of these records.

**Observation / Confirmed:** ID 118 is `private/alternate_shader_blade`;120 other original names are slash-rooted and project through exact `/private/` or `/shared_owner/` prefixes. Native and Python projections agree on 120 projected/one unsupported. **Consequence:** preserve the anomaly verbatim and return an explicit unsupported result; do not repair it by adding a slash. Projection returns the existing key with private=true/shared-owner=false and leaves supplied values absent.

**Implementation / Confirmed modern policy:** reader uses existing owned IFF parsing, retains immutable ordered entries/read-only ordinal indices, validates bounded counts/bytes/name lengths and sequential IDs, and publishes only after full success/cancellation checks. Printable nonempty ASCII names, exact single DATA, nonempty dictionaries and exceptions are modern rejection policy, not asserted legacy crash/diagnostic equivalence. Cancellation occurs before parse, per record and before return; existing bounded IFF parsing does not interrupt mid-copy/tree traversal. No global customization setter, namespace guessing, source caching API or packed codec is added.

**Verification / Confirmed:** zero-warning/error build; synthetic positives include ID127, case-distinct names, input mutation, immutable records and exact limits.89 negatives include every truncated prefix of a valid synthetic IFF plus unsupported root/version/chunk, missing/extra DATA/form, empty/partial/unterminated/control/non-ASCII names, zero/skipped/duplicate/wide IDs, duplicate names, trailing bytes/padding, source/record/name budgets, invalid limit and precancellation. Prior successful objects survive failures; original archives are disposed before dictionary parsing, borrowed input clears afterward. No asynchronous worker or disposable dictionary lifecycle exists to drain.

Independent oracle audits all original row bytes directly and source selection via existing Python archive indexing. Existing default/albedo regression passes 16 reader negatives/two initializer budget failures; unchanged 37/39-source modes and ownership/cancellation checks. GPU/live/packed semantics are not exercised by this dictionary assignment.

## Reproduction

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round20-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
dotnet restore poc/round20-tests/Assets/Assets.csproj --configfile poc/round19-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round20-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round20-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round20/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round20-tests/Assets/verify_cidm.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round20/native.txt > local-private/round20/python-audit.json
dotnet poc/round20-tests/Assets/bin/Debug/net8.0/Assets.dll default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round20/default-regression.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Native, independent oracle and baseline regression pass. Final baseline-relative scope/link/whitespace receipts accompany handoff; exact seven additions, all 2,374 baseline files unchanged, frozen interfaces preserved, HEAD unchanged/index empty. Counts/links and whitespace are checked after final documentation. Private no-index checks use a real zero-byte private file for additions; no Windows `/dev/null` dependency.

## Remaining gaps and exact next action

Lead should register `CustomizationIdDictionaryScenarios.Run(new[]{archiveRoot,configPath})` in its shared gates, load the dictionary once as an owned dependency, and join it to the independently reviewed packed-value codec. Unknown IDs require an explicit codec/application policy; unsupported projections remain visible and must not silently default to private. Consumer variable validation, typed setter conversion, palette/ranged semantics and current-incarnation atomic appearance updates remain outside this assignment. No necessary out-of-scope source change was found. Stop after Round20 handoff; no further milestone dispatched here.
