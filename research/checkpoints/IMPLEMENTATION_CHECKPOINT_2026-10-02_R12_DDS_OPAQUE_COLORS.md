# Round12 opaque DDS color boundary

2026-10-02. R12-ASSETS-01. Question: can the existing owned RGBA8 decoder accept exact tightly packed legacy BGR24/BGRX32 and typed DX10 BGRX32 while preserving all existing compressed/HDR paths, ownership and budgets? Stop after synthetic/original-independent/native-regression/scope gates. Round12 only; no Round13.

## Orientation and input identity

Read AGENTS/START_HERE, the named October1 development session and accepted Round11 report, DDS durable contract and original/modern/HDR/face-tail checkpoints, current status/ledger/function terminology, existing decoder/scenarios/TRE overlay and Python pixel/index helpers, and curated discovery. No executable addresses, executable execution or wire claims are used. Root owns shared runners/status/ledger/registries.

Verified Round12 baseline2,077 exact public paths and bytes before/after overlay; ZIP SHA256 `21b81fe530b5e5d6e056014fe6d56c85e2be73a5e195fbc67f021f1c3ceae335`, manifest SHA256 `4bb617a17cde0ae3cd13ebddb06afa2141ed29d35938028689113440b9cf2766`, HEAD `65de8c88141071744dc75594be126b3bab3696a5`. Checked unique member/manifest sets, relative path syntax/resolved containment, input lengths/hashes; preserved ignored/unlisted files. Scope is six named files plus the own two-file runner.

Configuration `C:/Stardust-DEV/swgemu_live.cfg` SHA256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Fresh effective `texture/nebula_dant_back.dds` from `C:/Stardust-DEV`: priority35, `mtg_patch_006_texture_02.tre`,16,511bytes, SHA256 `A2627501B233EE3A795BEB5B42446BF2F9B2A60854AD3781366720B5631FDC9D`. Native configured overlay and independent Python raw-name/CRC archive index agree. No original bytes/images are added publicly.

Prior ignored inventory SHA256 `DCEDE8198E48FF68CCD65FDF2C618E50703A13F1F08BF97CA34A1D0E5665D471`: mechanically checked29,906 CSV rows/113 BGR24 records, stored as `RGB24:00ff0000/0000ff00/000000ff/00000000:f40`. This is reuse of a prior inventory, not a fresh all-format corpus census. The dated [original DDS research](ANALYSIS_CHECKPOINT_2026-09-12_DDS_TEXTURE_RESOURCE.md) separately records valid resources/tombstones.

## Observations, interpretations and consequences

- Observation: original header64×64,7mips, flags`000A1007`, pixel flags`40`, bits24, R/G/B/A masks`00FF0000/0000FF00/000000FF/00000000`, FourCC0. LINEARSIZE is12,288 (full top mip), with no PITCH flag. Declared sizes12,288/3,072/768/192/48/12/3 exactly consume16,383 payload bytes. Interpretation: this sample is tightly packed BGR24 with a top-mip linear-size declaration. Confidence: **Confirmed original bytes and independent extraction**. Consequence: accept this observed layout and validate LINEARSIZE separately from row PITCH; do not invent padding.
- Observation: native7-mip RGBA hashes match independent Pillow DDS decoding, Pillow raw decoding and a separate direct BGR-to-RGBA byte oracle. Alpha255 throughout; total owned output21,844bytes. Interpretation: bounded decoder acceptance is closed for this effective asset. Confidence: **Confirmed original asset/pixel agreement**. Consequence: source identity and CPU upload representation are available. Original runtime/GPU/catalog-wide acceptance remains separate.
- Observation:36 authored fixtures/72mips cover legacy BGR24/BGRX, DX10 IDs88/93, all alpha metadata0–4, absent pitch/exact pitch/exact linear size, non-square/non-power-of-two rows, distinct X/channel values and terminal1×1. Output alpha255 ignores X and preserves metadata; color bytes never undergo gamma conversion. Confidence: **Confirmed synthetic/native/independent byte agreement**. Consequence: append enum10/11 without changing0–9; modern output is explicit, engine-neutral owned RGBA8.
- Observation:781 negatives (657 truncations,124 explicit invalid headers/flags/masks/typeless/surfaces/pitch/linear size/count/trailing/budgets) reject; opaque tails still reject with the BC2 exception enabled. Source clearing leaves every synthetic pixel and original mip hash unchanged; readonly mip lists reject modification; configured archive resources are disposed by scope before output checks. Confidence: **Confirmed native failure/ownership/cleanup; independent Python header rejection agreement**. Consequence: no partial pixel result is returned; all extent/aggregate/trailing gates precede pixel allocation. Strict acceptance limits are modern policy, not recovered original error handling.
- Observation: existing DdsScenarios and DdsHdrScenarios pass, including BGRA/RGBA/channel/BC1–5/BC7 pixel fixtures, BC2 retained-tail rules and BC6H signed/unsigned half output. Confidence: **Confirmed bounded regression execution**. Consequence: these accepted paths and codec dependency/budgets remain intact. No GPU or complete corpus claim follows.

Durable API: [DDS contract](../../docs/DDS_DECODER_CONTRACT.md). Implementation: [decoder](../../poc/Stardust.Core/Assets/DdsDecoder.cs); tests: [opaque scenarios](../../poc/Stardust.Core.SmokeTests/DdsOpaqueColorScenarios.cs), registered by [DDS scenarios](../../poc/Stardust.Core.SmokeTests/DdsScenarios.cs). Independent [verifier](../../tools/python/verify_dds_opaque_colors.py).

## Reproduction and validation

Use the existing cached .NET8/BCnEncoder/Python/Pillow dependencies; no package downloads, new dependencies or shared lock changes. The own runner compiles existing smoke sources and runs only DDS/HDR plus the fresh sample. Private offline NuGet config clears package sources.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round12-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
dotnet restore poc/round12-tests/Assets/Assets.csproj --configfile local-private/round12/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round12-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round12-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round12/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/verify_dds_opaque_colors.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round12/native.txt --prior-inventory C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/generated-evidence/dds_overlay_inventory.csv > local-private/round12/python-audit.json
```

Build passes with zero warnings/errors. Initial oracle run found the isolated runner invoked opaque scenarios twice after registration in DdsScenarios; removed the duplicate runner call and reran the final native/oracle gates. No product pixel mismatch was found. Detailed hashes, synthetic malformed receipts and audits remain ignored/private. Workspace validation passes:1,108 Markdown files,zero broken/private links,2,602 distinct provisional rows/zero duplicates and345 packets. Whitespace and exact baseline-relative hashes pass:2,074 baseline files unchanged,three focused existing files modified andfive additions, all within the exact eight allowed paths. HEAD unchanged/index empty; private/original/derived/build material ignored. Scope/hash/diff/status receipts are under the ignored round12 directory. No staging/commit/publication.

## Remaining uncertainty and next action

BGRX is synthetic standards-format evidence; original BGRX occurrence/client acceptance is not claimed. RGB16/luminance, cubes, padded rows and full corpus decode acceptance remain outside this task. No rendering/GPU, original runtime, protocol or research-score increment. Lead reviews/integrates the six scoped paths and own runner, reconciles living docs and consolidates authorized Rounds9–12. Stop idle after this handoff; no Round13 authorization inferred.
