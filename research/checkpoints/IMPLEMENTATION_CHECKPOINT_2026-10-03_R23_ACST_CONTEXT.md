# Round23 selected ACST context — 2026-10-03 UTC

## Question and stopping condition

Can selected ACST descriptions bind exactly to CIDM and owned palette counts, producing a complete in-range authored-default context without repairing missing variables or inventing player state? Stop after synthetic defaults/ranges/namespaces/ownership/callback/cancellation fixtures, selected original eight-key/five-palette oracle, baseline regressions and exact scope checks. No broader corpus, static VA work, GPU or live contact.

Orientation: START/latest accepted22, status/ledger/UNKNOWN, prior ACST/CIDM/range/palette contracts/checkpoints, existing readers and palette/Python byte tools. Previously confirmed schema and setter behavior are reused; no new provisional identifiers or coverage claims. Existing original palette count/schema boundary was reviewed before implementing context binding.

## Input identity and scope

Round23 snapshot 2,480 public files, manifest SHA-256 `066fc74338f9438b21fdb4320dfb74d1e10ee15c8fe6867349f538c7a8124c81`, ZIP SHA-256 `735d279ceb20800145f65f25efb7d1e731e1abd2cac7c583d4b55ee29238e5a1`. Exact unique entries/size/hash, containment and archive/lexical destination/ancestor symlink or Windows reparse-point absence preflighted before writes, all overlay hashes rechecked. Snapshot bytes authoritative; worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`/empty index and unlisted/private bytes preserved. Frozen full-delta union/all existing files are unchanged.

Readonly root `C:/Stardust-DEV`; configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. ACST 458,340 bytes SHA-256 `5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20`; CIDM 3697 bytes SHA-256 `FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2`; both priority 53 `stardust_3_01.tre`. Existing catalog/locomotion/face-zero/full-customization-zero profiles are explicitly supplied. The original reader's 76 sources/5,940,390 bytes remain distinct from the two table resources. Detailed original rows/source identities remain private/ignored.

Cached .NET8/offline private NuGet/task APPDATA and bundled Python; no sources/downloads. Seven additions: [builder](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/AssetCustomizationContextBuilder.cs), [native scenario](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/AssetCustomizationContextScenarios.cs), [contract](../../docs/ASSET_CUSTOMIZATION_CONTEXT_CONTRACT.md), this checkpoint, [project](../../../SWG-Godot-Client/poc/round23-tests/Assets/Assets.csproj), [entry point](../../../SWG-Godot-Client/poc/round23-tests/Assets/Program.cs) and [generator/oracle](../../../SWG-Godot-Client/poc/round23-tests/Assets/verify_context.py). Exact four allowlisted paths plus own Assets23 testdir. Shared Program/projects/navigation/status/ledger/registry/Godot/frozen DTO remain untouched. No unfinished worker dependency.

## Observations, interpretation and consequences

**Observation / Confirmed:** accepted ACST human query CRC `D700D576`/root 11821 has 36 descriptors. Selected original eight profile keys map verbatim to both ACST and CIDM. Three ranged controls are eyebrow [0,7), beard [0,18), freckles [0,5). Five palette controls resolve skin/hair/highlights/eye 256 entries each and white 2 entries. Authored defaults are zero for all eight. Python independently reads ACST/CIDM and selected palette headers/source identity; it does not infer these counts from supplied DescribeControls bounds.

**Observation / Confirmed:** owned face/material palette union is exactly the selected five paths, 1026 entries. Alias versions/entry bytes/trailing data agree. Resolver calls five distinct paths once each and returns already owned palette descriptions; no new palette I/O. Original arrays clear/archives dispose before context construction. The 76-source bundle remains unchanged; ACST/CIDM are separate context inputs. This confirms selected asset construction metadata, not remote player state or every manager variable.

**Implementation / Confirmed modern policy:** selected keys/query descriptors snapshot before callbacks; exact namespace/fullname/CIDM mapping preflights all selections. Conflicting descriptions fail; identical aliases are legal. Resolve only selected paths, derive palette[0,count), preserve signed ranged bounds, require every authored default in range. Missing/empty/fallback/malformed palettes, absent selected mappings, bounds/budget failures and cancellation publish no context. No clamp/default repair. Raw invalid authored metadata remains in the directory rather than being rewritten.

Borrowed callbacks/resources are not retained or disposed. Temporary palette snapshots validate visible mutation of entries/diagnostics/trailing bytes. Query/selection stability checks surround callbacks; returned context retains only numeric ranges/defaults and exact names/IDs. Thread-local same-thread reentry rejects even when swallowed; cleanup in finally permits later calls. Independent threads are permitted. Cancellation checks also run after final collection comparisons and result construction. Input restoration between checks is not claimed detectable; external callback mutations are not rolled back.

**Verification / Confirmed:** 25 independently authored fixtures: seven positive contexts (same-path once, unselected missing/unsupported descriptions, identical aliases, signed Int32 range and upper palette default) and 18 selected namespace/type/default/range/resource failures.17 controlled native negatives cover selection/query mutation, resolver exception, swallowed reentry, pre/callback/final-traversal cancellation, all budgets, fallback/unknown query/bad options, earlier palette mutation and a lying endless palette list. Bounded copy rejects the latter instead of trusting Count/ToArray. Exact limits, result/context ownership, read-only defaults/bindings, prior-result preservation, guard recovery and 16 concurrent independent builds pass. The final traversal cancellation probe motivated a final wrapper cancellation/reentry check before returning; final gate observes the intended cancellation, not timing assumptions.

Independent Python reuses only accepted ACST decode/tables/query functions through AST loading, avoiding its CLI or broad queries. It decodes CIDM bytes and selected RIFF palette extents/version/count itself, checks source priority/size/SHA and every selected binding/default. No original corpus is exported. Final build zero warnings/errors. Fresh ACST directory regression/oracle passes 50 fixtures /45 malformed/seven budgets/two path failures and original human/Togruta queries. Default/albedo regression passes 16 reader negatives/two initializer failures and unchanged 37/39 modes. No GPU run; lead owns that integration.

## Reproduction

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round23-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
$python='C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python poc/round23-tests/Assets/verify_context.py generate local-private/round23/fixtures.json
dotnet restore poc/round23-tests/Assets/Assets.csproj --configfile poc/round19-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round23-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round23-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json poc/preview-profiles/human-face-zero.json poc/preview-profiles/human-customization-zero.json local-private/round23/fixtures.json > local-private/round23/native.txt
& $python poc/round23-tests/Assets/verify_context.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-customization-zero.json local-private/round23/native.txt > local-private/round23/python-audit.json
& $python poc/round22-tests/Assets/verify_directory.py generate local-private/round23/directory-fixtures.json
dotnet poc/round23-tests/Assets/bin/Debug/net8.0/Assets.dll directory-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round23/directory-fixtures.json > local-private/round23/directory-regression.txt
& $python poc/round22-tests/Assets/verify_directory.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round23/directory-regression.txt > local-private/round23/directory-audit.json
dotnet poc/round23-tests/Assets/bin/Debug/net8.0/Assets.dll default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round23/default-regression.txt
& $python tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Final scope/link/whitespace/new-file EOF receipts accompany handoff: seven allowed additions, all 2,480 baseline files byte-identical, frozen full union/HEAD/index preserved. No stage/commit/push/publication. Private/build evidence ignored; curated files contain no private links. Scope uses a real zero-byte private file for addition checks.

## API and precise next action

`AssetCustomizationContextBuilder.Build(query, dictionary, selectedKeys, ownedPaletteResolver, token)` returns immutable `RangeContext`, complete selected `AuthoredDefaults` and compact `Bindings`; optional final `AssetCustomizationContextLimits` supports explicit budgets. Register `AssetCustomizationContextScenarios.Run` with seven arguments shown above. Lead can use the bound RangeContext in the existing ordered projector and AuthoredDefaults only as an explicitly selected asset-construction seed fixture. Current remote state still comes from ordered live records/explicit state policy; no automatic zero reset. Full unselected context, live mounted/linked variables/notification graphs and missing palette runtime recovery remain separate. No necessary out-of-scope change found. Stop after Round23 handoff.
