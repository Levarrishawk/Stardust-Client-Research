# R19 explicit face inspection resource route — 2026-10-02

## Bounded question and stopping condition

Can an optional frozen face-inspection policy extend the accepted original human route to owned generated MAIN data, preserving defaults/other descriptors and enforcing key closure/source/decoded/cancellation boundaries? Stop after direct reader/router positives, negatives/cleanup, independent source/composition verification and exact baseline scope review. No renderer/shared initializer/validator/accounting implementation is included.

## Input identity and orientation

Round19 snapshot2,346 exact unique members; manifest SHA-256 `6c642fef4824b7b07cd596e2be2fe1041f6286a505dc4a64a356938b3453e047`; ZIP SHA-256 `4add5c5a3869686632288e4cb259406ee50174a44f3a9c715fdb8a87eda93893`; authoritative manifest HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`. Worker checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` is preserved separately. Full exact member/hash/size/path containment/no-symlink preflight preceded overlay writes; all output hashes verified. Unlisted/private/HEAD/index preserved.

Read AGENTS/START/accepted Round18, living status/ledger/UNKNOWN terminology, head-gap/albedo/palette/face composition and prior fixture/checkpoint/tool boundaries. Frozen `SceneCreatureFaceInspectionPolicy` and option definitions are unchanged; only the router's specified creature forwarding call changes. No original executable address analysis or execution.

Readonly originals: `C:/Stardust-DEV`, config SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, existing scene-object catalog/human locomotion bank and supplied `human-face-zero.json` (SHA-256 `C4F7CF06AB8C4083A2510556550C047DC0ADD5A5A0040829A81CBB047CCCBD9B`). Established root/SAT/idle pins remain as in prior accepted contracts. No downloads/new dependency/corpus, live contact, original executable or GPU/engine execution.

## Implementation evidence, interpretation and consequences

[Reader](../../poc/Stardust.Core/Assets/SceneCreatureResourceReader.cs) adds final optional face policy, requires albedo for requested faces, opts into existing blueprint/resource/pixel-program loading, calls the existing generated builder with exact frozen values/token and assigns only `GeneratedFaces`. [Router](../../poc/Stardust.Core/Assets/SceneObjectResourceRouter.cs) forwards `c.FaceInspection` in its creature call. No frozen policy/option constructor/member, global customization, default loader choice, material/animation behavior or new decoder/compositor is changed. [Durable contract](../../docs/SCENE_CREATURE_FACE_RESOURCE_CONTRACT.md) defines the API/error/budget boundary.

Checked logical decoded accounting unions original texture paths across material/face maps and adds generated outputs. The known output dimensions are included before composition; actual output lengths are checked after. Cancellation is checked around reads, after descriptor loading, during accounting, before/during composition and before returning. Source cache enforces caller count/encoded limits. These are admission/compatibility safeguards, not heap usage or recovered timing.

**Observation / Confirmed native result:** default albedo-on39 sources/4,686,371 bytes remains; opted76 sources/5,940,390 bytes retains10 material+32 face-resource texture paths,41 unique originals. Original decoded sum17,203,152 plus generated262,144 yields17,465,296 logical bytes. Exact limits succeed; source count/bytes and decoded one below reject. Binding is head MGN slot0 MAIN to hum_m_face renderer/shader; supplied-zero256×256 output hash `A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808`. Interpretation: the missing generated resource can now be prepared by a direct explicit route. Consequence: world/renderer integration can request it after option-copy/gate/accounting review; this alone does not establish a GPU fix or original complexion.

**Observation / Confirmed alias identity:** only `texture/hum_m_face.dds` overlaps material/face maps. Image references are distinct; all11 mip bytes/dimensions and format/color-space/alpha/DXGI metadata agree. Logical budget counts it once. Runtime accounting should accept equivalent aliases and reject represented contradictory pixels/metadata rather than rejecting every distinct image object. The exact native alias facts are in compact `face_resource` JSON.

[Native scenario](../../poc/Stardust.Core.SmokeTests/SceneCreatureFaceResourceScenarios.cs) independently loads defaults/opted originals, snapshots caller mutation, verifies complete non-face descriptor equivalence, exact keys/destination/golden output, source/read uniqueness, closed archive/borrowed-buffer cleanup, successful-result preservation, exact budgets and12 rejection cases. Valid Int32 minimum/maximum remain supplied values and match established fallback output hashes; wrong key/domain/syntax rejects. No invalid value-range policy is invented. Precancel performs zero resolver calls; first-read cancellation performs one. A10ms timer scheduled after the known final source read produced an observed compositor-stack cancellation with no returned prefix. The probe checks its stage; it is timing-sensitive on a different host and does not recover original timing. The final return cancellation check is additionally confirmed by source inspection.

[Verifier](../../poc/round19-tests/Assets/verify_face_resources.py) audits all76 actual source priority/size/SHA identities, unique DDS-header/mip lengths and the exact decoded sum. It composes the accepted independent rectangular face raster/reference and raw TRTS binding verifier, matching all five cumulative layer hashes and final output. Prior acceptance supplies preparation/format authority; no new copied parser/compositor or fabricated original pixels. Default albedo native regression passes16 reader negatives/two initializer failures and unchanged37/39-source modes. Detailed outputs/arrays/source data remain ignored/private.

## Reproduction and results

Use cached .NET8/offline NuGet and bundled Python/NumPy/Pillow. Own `Run` takes root/config/catalog/bank/explicit-face-profile; `default-regression` mode takes root/config/catalog/bank. Main output includes compact `face_resource` and detailed oracle rows, then PASS. Verifier takes root/config/face-profile/native-output.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round19-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
dotnet restore poc/round19-tests/Assets/Assets.csproj --configfile local-private/round19/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round19-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round19-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json poc/preview-profiles/human-face-zero.json > local-private/round19/native.txt
dotnet poc/round19-tests/Assets/bin/Debug/net8.0/Assets.dll default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round19/default-regression.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round19-tests/Assets/verify_face_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-face-zero.json local-private/round19/native.txt > local-private/round19/python-audit.json
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Final restore/build/native/default-regression/oracle pass, zero warnings/errors. Test-harness corrections before final execution: structured-key dictionaries required projected fingerprint rows; the wrong-domain fixture initially selected a key whose opposite already existed; alias diagnostics required a nullable annotation. Product loaded/composed correctly before those fixture repairs. Final scope/link/whitespace pass; two allowed existing files modified and six additions, all other2,344 baseline files unchanged. Router frozen-definition prefix and reader existing copy-selection suffix are byte-preserved. Private scope/hash/diff/status receipts accompany handoff. No stage/commit/push/publication/external mutation or shared project/Program/interface/ledger/status/registry change.

## Joined dependency and next action

The snapshot's `SceneObjectResourceRequest.CopyOptions` in `SceneObjectResourceInitializer.cs` reconstructs creature options without `FaceInspection`. Lead explicitly acknowledged this and owns its main/joined fix; this file remains unchanged here. Without it, world initialization silently drops the request. Requested face completion validation/accounting and GPU acceptance/capture are same-round independent integration work and intentionally unrun in these standalone tests.

Lead should register `SceneCreatureFaceResourceScenarios.Run(new[]{root,config,catalog,bank,faceProfile})`, preserve policy through option copying, join requested-extension gates/equivalent-alias logical accounting, compare the compact receipt and verify actual generated MAIN/face region in the continuous GPU path. Replicated appearance translation, original player complexion, legacy driver parity and full shader behavior remain unknown. Stop after Round19 handoff; no further milestone is dispatched here.
