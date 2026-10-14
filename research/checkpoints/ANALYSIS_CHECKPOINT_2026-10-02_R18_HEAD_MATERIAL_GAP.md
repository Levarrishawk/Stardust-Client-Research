# R18 selected human head material gap — 2026-10-02

## Question and stopping condition

Why does the selected original head look white/pale while the torso is colored in the accepted continuous preview? Stop when one exact current material/resource/adapter chain reproduces a cause or precise gap, with metadata/pixel-hash verification and a smallest next integration test. No production renderer/material edits or expanded corpus.

## Input identity and orientation

Round18 snapshot:2,310 exact unique public members; manifest SHA-256 `023b090af15474a13a76ba1836d59cfc3800baab6dc27057436a165608e8a910`; ZIP SHA-256 `e2fe55b4f1a8c4c21da802d28dc922930f8963d002ee38b5c9f71129145fc43d`. Manifest HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c` is authoritative; worker checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` is preserved independently. Complete member/hash/size/path containment/no-symlink preflight preceded overlay writes; all outputs verified. Unlisted/private/index/HEAD preserved.

Oriented repository instructions/START/accepted Round17, living status/ledger/unknown terminology, current skeletal material/albedo/palette/face contracts and prior checkpoints/tools. Frozen local motion driver/motion source and optional pump callback are read-only and unused. Scope is diagnostic Assets only. No original executable probes or execution, new addresses/names/registry rows, GPU/engine or network work.

Original root `C:/Stardust-DEV` is read-only, configuration pin `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; existing scene-object catalog/human locomotion profiles and accepted human detail0/albedo options are reused. Root/SAT pins and exact material-chain source priorities/lengths/hashes are in the [durable finding](../../docs/SCENE_HUMAN_HEAD_MATERIAL_GAP.md). Detailed metadata/sample/decoded hash receipts remain ignored/private; original bytes/pixels are not published.

## Observation, interpretation, confidence and consequence

[Scoped native exporter](../../../SWG-Godot-Client/poc/round18-tests/Assets/Program.cs) loads the exact39-source/4,686,371-byte closure through the accepted router, clears borrowed arrays and closes archives before diagnostic iteration. Existing resource validation passes. Five geometry slots/four material identities are followed from original MGN into CSHD0001 inline SSHT, named MAIN/UV0, effect, validated albedo program, authored palette selections and renderer destinations. No further source reads or resource mutation occur. Precancel rejects before the resolver; zero pending/retained workers exist in this synchronous exporter.

**Observation / Confirmed:** head MGN slot0 has644 triangles and uses `shader/hum_m_face.sht`, `effect/h_color2_specmap_cbmp.eft`, `pixel_program/h_color2_specmap_ps11.psh`, authored `texture/hum_m_face.dds` and a MAIN TRTS reference to `texturerenderer/hum_m_face.trt`. The latter source is absent from39-source closure. Face blueprints/resources/generated outputs/customization values are null. Face CSHD palette factors are empty and TFNS/albedo MAIN/HUEB are both white. Ordinary head material occurrences already receive the same `FFE2B7AF` skin factor as torso; no missing palette fallback occurs.

**Observation / Confirmed:** face texture is1024² BC1/11 mips; mip0 is not all white (356 white-RGB pixels of1,048,576; minima74/81/74). Its decoded hash is `46F2C0B7003C2FAEBC2EC803E1B4A5DF54DBA3DA56095441FBCACDB59D1CC7E5`. White factors preserve the actual sampled face RGB; the midpoint evaluates about0.906/0.875/0.871, while torso's factor equation yields0.769/0.611/0.584. These are CPU source-encoding samples, not screen/legacy expected colors.

**Observation / Confirmed source behavior:** SceneCreatureResourceReader does not opt into face blueprints/resources/programs. SceneCreatureVisual passes the owned bundle to SkeletalMeshPreview, which resolves per-geometry slot material identity. SkeletalMaterialPreview binds authored MAIN plus owned factors and selects generated overrides only by exact mesh/slot/MAIN when outputs exist. The diagnostic blue mesh fallback is not used with the present material bundle. The completion gate explicitly rejects unrequested face extensions.

**Interpretation / Strongly inferred:** omitted generated-face preparation explains the pale face relative to correctly skin-tinted torso in the reported accepted GPU preview. This is an integration/explicit-route limitation; no named MAIN/DDS mismatch is found. **Unknown:** actual original player's customization values/complexion, active original effect capabilities/lighting and exact symptom-region GPU mapping in this round. Existing full face composition can consume an explicit snapshot but does not invent replicated player values. Correct outer white factors preserve generated layer color; blindly adding skin tint would not implement the missing pipeline.

Compatibility consequence: an opt-in face route must request/own blueprint/resources/programs, compose supplied values, authorize requested extensions in validation and account for generated images. Default39-source behavior and unrelated material/animation/motion boundaries should retain their identities. The existing adapter already exposes the generated-binding destination; no ad hoc global tint is recommended.

## Verification and exact commands

[Verifier](../../../SWG-Godot-Client/poc/round18-tests/Assets/verify_head_gap.py) uses existing configured-archive Index and accepted DDS/Pillow inspector. It verifies all39 actual original source priorities/lengths/hashes, exact slot/material/MAIN/program/source references and four MAIN mip0 decoded hashes. It independently evaluates the retained factor/sample equations and rejects five altered receipt claims (generated flag, renderer-loaded flag, face tint, wrong MAIN, wrong pixel hash). Native code computes RGB aggregate statistics; the verifier compares the recorded bounded facts and decoded hashes rather than adding another pixel decoder. No random sweep or synthetic expected original pixels.

Only cached .NET8/offline NuGet and bundled Python/Pillow are used. Native arguments are root/config/catalog/bank; output is `head_gap_receipt=` JSON then PASS. Verifier arguments are root/config/native-output.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round18-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
dotnet restore poc/round18-tests/Assets/Assets.csproj --configfile local-private/round18/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round18-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round18-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round18/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round18-tests/Assets/verify_head_gap.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round18/native.txt > local-private/round18/python-audit.json
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Restore/build/native/audit pass, zero warnings/errors. Archives close, borrowed cleanup/no further reads/no mutation and precancel pass. Five receipt negatives pass. Scope/link/whitespace checks pass:1,167 Markdown files,zero broken/private links,2,602 provisional rows/zero duplicates and345 packet rows; exactly five additions, all2,310 baseline files unchanged, preserved HEAD/empty index, ignored private/build outputs. Exact final baseline-relative hashes/diff/status are retained privately. No shared production material/renderer/router/Program/project/interface/status/ledger/registry change, stage/commit/push/publication/download/external mutation. Fresh GPU parameter inspection/capture, live AppearanceData/customization and original executable verification are intentionally unrun.

## Precise next action

Lead can assign an opt-in generated-face resource vertical slice using existing helpers and an explicitly labeled supplied inspection snapshot. Require the head slot0 MAIN generated binding, independent CPU output and actual GPU texture-parameter/face-region evidence, with body/head-cap bindings unchanged, explicit measured budgets, completion validation/accounting and malformed/cancel/stale cleanup gates. Prior full-face source counts are historical; measure the new movement-enabled union instead of asserting them. No fix is implemented here. The bounded gap is reproducible; stop after Round18 handoff.
