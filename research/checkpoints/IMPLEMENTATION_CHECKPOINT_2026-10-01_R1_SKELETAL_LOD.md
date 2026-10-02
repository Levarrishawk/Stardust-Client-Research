# R1-ASSETS-01: original human detail1 closure

Date: October1,2026. Result: ready for lead review; acceptance remains the lead's responsibility.

## Question and stopping condition

Does explicit mesh detail1 close the pinned original human object -> SAT -> MLOD -> selected MGN -> SKTM/bind geometry/material dependency chain without hidden detail0 substitution or repeated source reads? Stop after original-byte comparison and strict failure/ownership/budget gates. No distance selection, GPU integration or next milestone.

## Baseline and evidence

Checkout: `C:/Users/ancie/.codex/worktrees/520a/Stardust-Client`; HEAD `65de8c88141071744dc75594be126b3bab3696a5`. Read repository instructions, START_HERE/latest named session, parallel rules, current status/ledger/function terminology, existing descriptor/skin contracts and prior skeletal tools/scenarios. Preserved the pre-existing untracked parallel preparation session.

Pinned inputs and exact results are consolidated in the [durable contract](../../docs/SKELETAL_LOD_SELECTION_CONTRACT.md). Source research is static and asset reads use `C:/Stardust-DEV/swgemu_live.cfg`. No executable virtual addresses, original program execution or server contact. The independent Python verifier reuses established TRE/IFF, template, skeletal geometry, skeleton and material/DDS audit helpers; no parser duplication.

Observation: detail1 selects body/head l1 MGNs;29 source identities/4,167,914 bytes,968 positions/1,382 weights/1,064 normals/1,094 tangents/1,904 retained triangles, four material defaults/ten textures/80 decoded mips independently agree. Detail0 comparison is29 sources/4,271,530 bytes/1,454 positions/2,810 retained triangles. Each load resolves each source once. Scoped transform mappings against level0 body/face skeletons agree with original names. Native bind deformation preserves positions within1e-5. Clearing resolver byte arrays preserves geometry hashes and decoded pixel hashes.

Interpretation/confidence: explicit selected asset closure and byte structures are Confirmed for this pinned original sample. Native bind identity is Confirmed for the modern diagnostic policy. Legacy automatic skeleton/detail coupling, visibility/distance scheduling and rendered detail1 appearance remain Unknown here. Compatibility consequence: retain explicit selection and strict source failures; no production reader change is justified.

## Validation

Task environment: APPDATA=`$env:TEMP/stardust-assets-appdata`; NUGET_PACKAGES=`C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages`; PYTHONUTF8=1. Offline private NuGet.Config clears package sources. Python executable: bundled runtime `C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

Commands and results:

- `dotnet restore poc/round1-tests/Assets/Assets.csproj --configfile local-private/round1/NuGet.Config -p:NuGetAudit=false`: pass offline.
- `dotnet run --project poc/round1-tests/Assets --no-restore -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round1/native.txt`: pass; positive details0/1, invalid detail before lookup, selected missing/masked dependency, malformed MGN, singleton unavailable detail, count/byte exhaustion and borrowed-input ownership gates.
- `python tools/python/verify_skeletal_lod_selection.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round1/native.txt`: pass; independent source union/identity/selection/skin/geometry/joint/material/mip agreement.

Initial restore attempts failed on inaccessible user NuGet.Config, then restricted remote vulnerability queries; resolved using task-local configuration and existing private packages. Shared compilation initially could not write the copied Core intermediate DLL; `UseSharedCompilation=false` resolved it without deleting copied outputs.

## Handoff and next probe

Production reader unchanged. Added isolated scenarios, console project, independent verifier, durable contract and this checkpoint only. Root ledgers/status/navigation/curated registries remain lead-owned: lead should link the accepted contract/checkpoint and record Round1 acceptance after integration verification; no coverage score increase is implied.

Remaining uncertainty: details2/3, automatic distance/fallback policy, skeleton-detail negotiation and GPU detail1 rendering. Suggested next bounded milestone: independently validate detail2/3 source and transform availability; do not begin automatically.
- `dotnet build poc/Stardust.Core --no-restore -p:UseSharedCompilation=false`: pass, zero warnings/errors. Organized-workspace validator: 1007 Markdown files, zero broken links/private links; canonical names2602 with zero duplicates, packets345 unchanged. Focused whitespace/diff checks pass; all generated evidence and build outputs remain ignored.
