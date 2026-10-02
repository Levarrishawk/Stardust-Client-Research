# 2026-10-02 R12-NET-01: components-set kernel and standalone codec

Question: what exact signed-key snapshot normalization, delta counter/skip and membership/no-op behavior does the pinned visible-components set implement? Stop at bounded static recovery plus standalone owned snapshot/state/plan and verification. No mixed/world integration or Round13.

## Inputs and bounded safety plan

Read AGENTS/START_HERE, accepted Round11 report, baseline/mixed/legacy/replication contracts, width-correction/R8 checkpoints, current status/ledger/UNKNOWN and canonical rows before implementation. Accepted Round12 dirty-main baseline:2,077 public files; HEAD `65de8c88141071744dc75594be126b3bab3696a5`; archive SHA-256 `21b81fe530b5e5d6e056014fe6d56c85e2be73a5e195fbc67f021f1c3ceae335`; manifest SHA-256 `4bb617a17cde0ae3cd13ebddb06afa2141ed29d35938028689113440b9cf2766`. Checked exact member set, safe relative paths/resolved containment and hashes before overlay; every resulting file hash verified. Ignored/unlisted private material preserved.

Pinned original `C:/Stardust-DEV/Stardust.exe` SHA-256 verified before VA use: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static-only safety envelope:323-byte snapshot,675-byte delta and only direct insertion/erase/clear/callback/iterator, bounds/caller and serializer context necessary to close the question. Total bounded export2,484bytes. No original execution, live/socket/DNS/server/capture/fuzzing/DRM work, external mutation or downloaded dependencies. Full original-derived output remains ignored/private. Reused accepted exporter and cached Capstone module5.0.7/pefile2024.8.26; no installation. Canonical `00642F00/00643050` names remain unchanged; no new name/count.

## Observations and conclusions

Exact evidence is consolidated in the [durable contract/API](../../docs/CREATURE_VIEW3_COMPONENTS_DELTA_CONTRACT.md):

- Snapshot directly reads U32 counter to +0x18; signed tree comparisons deduplicate keys. Equal duplicates do not insert or advance that counter. Signed ascending in-order traversal is visible in serializer `00642B80` and iterator helper. Preserve raw count/order/duplicates separately from normalized unique state.
- Delta `00643050` first branches on unsigned wrapping L+N <= incomingC. That case skips zero; otherwise skip is unsigned min((L-C+N) mod2^32,N). This differs from the recovered vector's unconditional skip-clamp.
- Drop-existing, add-absent and nonempty-clear increment local counter once. Missing-drop/duplicate-add/empty-clear do not. Final `006432E2-006432E7` explicitly raises L to unsigned max(L,C), including zero-count fields. This final maximum is real for the set, unlike the vector.
- Signed key comparisons, unique-insert flags, membership erase count, clear destruction and ordered iterator establish membership/ordering. Common caller iterates to bounded exhaustion; checked reads reach archive error. Original unknown active/stale command byte consumption differs and is rejected by modern codec.

Interpretation: one proven set kernel can be exposed with modern bounds/atomic immutable publication, without inventing a strict counter policy. Confidence: Confirmed direct instruction/helper evidence; original downstream callbacks, allocation failure and outer recovery remain outside the kernel. Compatibility consequence: normalize explicitly while keeping raw snapshots; handle future-gap/zero-count/wrap behavior using set arithmetic, not vector arithmetic. Shared generic-set phrasing and R8 duplicate-normalization Unknown require lead reconciliation; no shared schema/status/UNKNOWN/registry files edited.

## Implementation boundary

Added raw snapshot decoding/encoding and full-baseline snapshot extraction, normalized signed ordered state, field5-only ordered delta decode/encode, and atomic application. Raw snapshot encoding preserves duplicates/order; it is not original normalized set serialization. Repeated field5 records and raw declared counts are retained. Other indices reject. Modern payload/field/aggregate-command/key/unique-element budgets, checked preallocation, object-ID check, immutable ownership and whole-plan publication replace unsafe partial mutation/callback behavior. No callbacks/handles/threads/external cleanup lifetime.

The contract lists exact standalone signatures for future implementation. No mixed/store/world integration. Existing baseline, mixed, wounds and scalar codecs remain unchanged. No metric denominator changes.

## Reproduction

Run in the isolated checkout with existing cached tools:

```powershell
$py = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONPATH = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages'
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 00642F00:00643042 --range 00643050:006432F2 --output local-private/round12/set.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 00645560:006456CF --range 006445A0:0064469E --range 006446A0:006446EB --range 00645CE0:00645D8B --range 00645200:00645210 --range 00645220:0064523A --range 012F6420:012F6488 --output local-private/round12/helpers.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 00644E10:00644E37 --range 00644E40:00644E67 --range 00644570:00644580 --range 00644590:0064459D --range 0118AA00:0118AA5A --range 0118B030:0118B0D9 --output local-private/round12/context.json
& $py tools/python/export_replication_vector_instructions.py C:/Stardust-DEV/Stardust.exe --range 00642B80:00642BDD --output local-private/round12/serializer.json
& $py tools/python/verify_creature_view3_components_deltas.py --exe C:/Stardust-DEV/Stardust.exe --generate local-private/round12/fixtures
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r12'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
Set-Content local-private/round12/NuGet.Config '<configuration><packageSources><clear /></packageSources></configuration>'
dotnet restore poc/round12-tests/Networking/Networking.csproj --configfile local-private/round12/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round12-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round12/fixtures local-private/round12/export
& $py tools/python/verify_creature_view3_components_deltas.py --exe C:/Stardust-DEV/Stardust.exe --verify local-private/round12/export
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round12/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
dotnet run --no-restore --project poc/round11-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round11/fixtures local-private/round12/mixed-regression
& $py tools/python/verify_creature_view3_mixed_deltas.py --verify local-private/round12/mixed-regression
& $py tools/python/validate_organized_workspace.py .
git diff --check
```

The regression used preserved R11 fixture input; if absent, generate it through accepted `verify_creature_view3_mixed_deltas.py --generate local-private/round11/fixtures` first. All fixtures/exports remain ignored. Python imports the accepted synthetic baseline generator read-only, modifies a component key to create an explicitly synthetic duplicate and never inspects C# implementation. Fixture identities:152-byte complete baseline SHA-256 `788b64e33e8d54c10629f3e4f5caae61999332f55735015b87a3c82896dc1100`;20-byte raw snapshot `07e15ec2381f9e50313997f541415b23416254d7c10f1dfca71d37e28fd8cf43`;76-byte repeated-field golden `e286543ebe40ffa81e6d68d7affbb396464160562543ec905d764df6f2f1e6ae`. No original asset/captured/proprietary fixture bytes.

Independent oracle verifies27 instruction anchors directly in the pinned executable, independently packs wire bytes and models arithmetic/membership/signed normalization. All69 native results/encoded messages agree, including49 high/low/wrap counter pairs. Native positives/negatives cover raw duplicates vs unique ordering, empty snapshots/envelopes, repeated fields, each command/no-op, future/stale/overlap/zero/wrap counters, exact/over-budget caps, impossible counts, every truncation, late key/growth/unsupported failures, signed extremes, ownership and schema/object mismatch. Full baseline and raw field snapshots agree after source arrays are cleared. Late repeated-field failures leave input state unchanged.

Focused component tests pass; Core/shared smoke projects build with zero warnings/errors (shared full suite built, not fully executed). Prior mixed native and17-case independent oracle pass. Lead integration entry is `CreatureView3ComponentsDeltaScenarios.Run()`; shared runners remain untouched.

## Precise resume and stopping point

Lead should reconcile the integer-set sum gate and baseline duplicate normalization in shared evidence, retaining vector/set distinctions. A future authorized milestone may compose components with stable-incarnation mixed state in one transaction, then trace concrete installed callbacks if presentation requires them. Original callback/outer recovery, broader views and live compatibility remain outside this result. Stop idle after Round12; no Round13 work or staging/commit/push/publication.

Final repository gate:1,109 Markdown files,zero broken/private links,2,602 provisional rows/zero duplicates,345 packet rows. Scope review confirms exactly seven allowed additions and all2,077 accepted baseline files unchanged; HEAD/index unchanged. Diff whitespace check passes; authored files checked for ASCII UTF-8/LF and trailing whitespace, with focused source inspection. Final baseline-relative SHA-256 change manifest and status retained in ignored Round12 evidence.
