# Round24 owned STF table — 2026-10-03 UTC

## Question and stopping condition

Can an owned bounded reader preserve the exact variant 1 value/name relation from two selected original STF tables while rejecting malformed lengths, duplicate/orphan identities and budget failures? Stop after independent fixtures, two original byte oracles, default/albedo regression and scope validation. No whole-corpus sweep, locale manager, new VA claims, executable run or live contact.

Orientation completed: START/latest accepted23, status/ledger/UNKNOWN, curated localization names, the [September investigation](ANALYSIS_CHECKPOINT_2026-09-21_LOCALIZATION_STF_RUNTIME.md), existing `tools/python/parse_stf.py` and asset pipeline context. Original bytes were inspected before fixing header offsets. No new addresses are relied upon; executable hash verification was therefore not needed for this bounded asset implementation.

## Inputs and reproducibility

Round24 snapshot: 2,519 public files. Manifest SHA-256 `b601923e5e05c33cb41ade174c72773b7d9ece509eb30ba8d4d09d0d2c1f7f19`; ZIP `7f87d3b2bb0644a3e237711cc953f8685914f623e93ff0c7607dbc5fe0764e95`. Exact unique entries, size/hash, containment and absence of archive/destination/ancestor symlinks or Windows reparse points were preflighted; all overlay hashes rechecked. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`, snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`, index empty.

Readonly root `C:/Stardust-DEV`, configuration `swgemu_live.cfg`, SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Both selected payloads resolve at priority 53.

| Effective path | Bytes | SHA-256 | Records | Units | Key bytes |
|---|---:|---|---:|---:|---:|
| `string/en/obj_n.stf` | 2527 | `3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054` | 40 | 561 | 592 |
| `string/en/terminal_name.stf` | 4610 | `CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603` | 57 | 1163 | 1131 |

Requested `string/en/terminal_n.stf` is missing. A broad terminal metadata filter yielded 32 names and stopped at its 20-name cap without reading another STF payload. Refined root-level `string/en/terminal_` discovery yielded four candidates: terminal_detail, terminal_lookat, terminal_name and terminal_ui. Only terminal_name was selected. Exactly two present original STF payloads were read; detailed strings/keys remain private.

Seven additions: reader (external historical reference), native scenarios (external historical reference), [contract](../../docs/LOCALIZED_STRING_TABLE_CONTRACT.md), this checkpoint, project (external historical reference), entry point (external historical reference), Python oracle (external historical reference). No baseline file or frozen/shared API is modified. Shared navigation, status, ledger and registry consolidation belongs to the lead.

## Evidence, interpretation and consequences

**Observation / Confirmed:** obj header is `cdab0000012900000028000000`; terminal_name is `cdab0000013a00000039000000`. Direct little-endian decoding gives magic U16@0, reserved U16@2, variant U8@4, raw max-next U32@5 and count U32@9. Both variants are 1, reserved zero; raw max-next is 41/58. Values then names consume all bytes, with unique IDs and exact keys and equal pass ID sets. The existing parser and independently authored Python decoder corroborate this structure; native output agrees record by record.

**Interpretation / Unknown:** reserved-field purpose, raw max-next allocator semantics and per-value CRC semantics are not established by these observations. Preserve their bytes. Prior static research distinguishes variant 0 and variant 1 readers, but does not supply a verified variant 0 record schema here. Reject/defer variant 0 rather than reuse variant 1 assumptions.

**Implementation / Confirmed modern policy:** immutable owned raw units and narrow keys, ID joins independent of pass ordering, atomic publication, strict exhaustion and bounded allocations. Duplicate IDs/keys, orphan relation, overflow-sized/truncated extents and all configured budgets fail. Zero and maximum U32 IDs, empty fields and arbitrary narrow bytes remain accepted when unique. Display conversion is separate from raw lookup; it does not claim legacy narrow encoding or locale fallback. See the contract for exact schema, ceilings and exception behavior.

## Validation

Offline cached .NET 8 restore/build with cleared NuGet sources and audit disabled; bundled Python. Final build: zero warnings/errors. Synthetic corpus: 102 fixtures, four positive and 98 malformed, including every truncated prefix of the 85-byte boundary fixture; six independent budget failures and invalid settings. Exact-boundary positive limits: 85 source bytes, two records, nine units/value, six bytes/key, 11 aggregate units and ten aggregate key bytes. Ownership, read-only collections, canonical map references, prior-result stability after failures and 32 independent concurrent lookups pass.

Python independently encodes/decodes bytes and compares all 97 original records, raw header/CRC/units/key bytes, pass order and display. Archive handles close before native parsing; source arrays are cleared afterward. Fresh default/albedo regression passes 16 reader negatives and two initializer failures with unchanged 37/39 source modes. No GPU or runtime-manager verification is claimed.

Reproduce from this checkout in PowerShell with cached dependencies:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round24-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config contains <configuration><packageSources><clear/></packageSources></configuration>.
& $python poc/round24-tests/Assets/verify_stf.py generate local-private/round24/fixtures.json
dotnet restore poc/round24-tests/Assets/Assets.csproj --configfile local-private/round24/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round24-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round24-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round24/fixtures.json > local-private/round24/native.txt
& $python poc/round24-tests/Assets/verify_stf.py verify C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round24/native.txt
 dotnet poc/round24-tests/Assets/bin/Debug/net8.0/Assets.dll default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json
& $python tools/python/validate_organized_workspace.py .
```

Scope review verifies all 2,519 baseline files unchanged, exactly seven additions, empty index, unchanged worker HEAD, ignored private/build output and whitespace/EOF checks. Relative links resolve. No original content is promoted into curated data.

## Resume point

This bounded variant 1 table milestone is complete. Next investigate locale selection and missing table/key fallback at the already documented runtime manager boundary, then cache ownership and object-name consumers. Variant 0 remains explicitly unsupported; no claim of full STF corpus or localization coverage follows from two tables.
