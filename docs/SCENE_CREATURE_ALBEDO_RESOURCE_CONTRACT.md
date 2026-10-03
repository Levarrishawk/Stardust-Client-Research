# Optional scene creature albedo resources

R10-ASSETS-01, October 2, 2026. [SceneCreatureResourceReader.Load](../poc/Stardust.Core/Assets/SceneCreatureResourceReader.cs) appends `bool loadAlbedoFactors = false` as its final parameter. Explicit `true` forwards to the existing descriptor reader's albedo option. Default behavior remains the accepted [scene creature resource contract](SCENE_CREATURE_RESOURCE_CONTRACT.md). This bounded CPU composition adds no new shader/profile/palette policy and makes no full-lighting or rendering claim.

## API and retained behavior

The reader already prepares skeletons, skin sources, detail0 geometry, logical tables, idle/walk/run and authored material/palette defaults. The new flag forwards only `loadAlbedoFactors`; every other argument, validation gate, cancellation check, source budget, decoded-image limit and raw-create snapshot remains unchanged. Existing positional calls retain the false default.

False/omitted leaves `Creature.MaterialBundle.AlbedoFactors` null and introduces no pixel-program dependency. Explicit true produces the existing owned `SkeletalAlbedoFactorBundle`, which contains one validated factor description per material and deduplicated owned PSHP program metadata. Missing/masked programs, malformed/truncated PSHP or unsupported altered PEXE instruction sequences reject the load; no partial resource result is returned. All source paths stay in the existing owned cache and measured source union.

The existing [albedo factor contract](SKELETAL_ALBEDO_FACTOR_CONTRACT.md) remains authoritative for PS1.1 recognition, pass/stage/factor identity checks, TFNS and CSHD palette composition, bounded leading COMMENT handling and sample algebra. Existing palette policy remains: missing palettes can use diagnostic cyan, whereas malformed present palettes reject. This addition neither strengthens nor replaces those gates.

Object, appearance and selected animation SHA pins remain caller requirements. Program SHA values below are measured audit identities, **not newly enforced production pins**. An unsupported instruction alteration rejects semantically; this is not a promise that every source-byte change rejects. Supported metadata/comment changes may still satisfy the existing decoder and receive a different recorded source SHA. Hardware capability selection, compiled-program execution and access-control behavior are not introduced.

The evaluator operates on sampled source-encoding RGBA. For one factor, output RGB is sample RGB times first-factor RGB. For the alpha-hue case, hue is first RGB plus `(second RGB - first RGB) * sampled alpha`, then sample RGB times hue. Texture alpha is used as a hue mask, not interpreted as transparency. Retained texture bytes/mips do not change. Color-space, filtering, lighting, normals, specular, opacity, bloom, face generation and GPU adaptation are separate existing boundaries.

## Original human closure and evidence

Inputs are the [authored catalog profile](../poc/preview-profiles/scene-object-catalog.json), human CRC `2937962913`, object `object/creature/player/shared_human_male.iff` SHA-256 `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243`, appearance SHA-256 `951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C`, existing idle selection and [walk/run profile](../poc/preview-profiles/human-locomotion.json). The read-only `C:/Stardust-DEV` overlay uses explicit `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

**Confirmed observation:** omitted and explicit false each retain 37 read-once sources / 4,683,848 bytes. Opt-in follows the same five inherited object templates, SAT, MLOD/detail0 body/head MGN, skeleton/material/palette/animation path and adds exactly these sources:

| Program | Priority | Bytes | SHA-256 |
| --- | --- | --- | --- |
| `pixel_program/h_color2_specmap_ps11.psh` | 40 | 1,399 | `95E44154F4107866CF23FD5C77E082573211135BE17E1CE679243A2AEB85EB92` |
| `pixel_program/h_specmap_ps11.psh` | 40 | 1,124 | `4A3155359640270C2274495AFD1E8E68E6139CCBC358DDA14B66D50BAF8B340C` |

Opt-in retains 39 read-once sources / 4,686,371 bytes, four material factor descriptions, two shared programs and the same ten textures / 80 RGBA mip hashes. Source-map equality is exactly the default union plus those two programs; all priorities/lengths/source hashes are independently checked against original effective archive bytes.

**Confirmed factor observation:** raw original pass/TFNS/CSHD/RIFF/program evidence, checked by the accepted Python albedo oracle, gives these authored-default values for this reader:

| Material | Alpha hue | First ARGB | Second ARGB |
| --- | --- | --- | --- |
| `shader/hum_m_body.sht` | yes | `FFE2B7AF` | `FFD5D5D5` |
| `shader/hum_m_head.sht` | no | `FFE2B7AF` | `FFE2B7AF` |
| `shader/hum_m_face.sht` | yes | `FFFFFFFF` | `FFFFFFFF` |
| `shader/hum_m_mouth.sht` | yes | `FF412D1C` | `FFD5D5D5` |

The earlier albedo contract describes HUEB as white. This specific pinned authored-default resource closure instead resolves `FFD5D5D5` for body/mouth; no palette or shader behavior was changed to force either description. These current values are verified directly from original selected palette records. The older wording is not evidence for this closure and remains a lead-owned documentation reconciliation point.

**Confirmed sampled-color observation:** four actual original MAIN mip0 pixels are independently decoded and evaluated. Body/head/mouth each differ from MAIN-only; the face's two white factors leave its selected sample unchanged. For example, body pixel0 RGBA `C6BEBD02` has MAIN RGB approximately `(0.7764706, 0.74509805, 0.7411765)` and factor-derived RGB `(0.68785566, 0.535405, 0.50951684)`. Python reproduces each selected pixel and factor equation within `2e-7`, with no display/gamma or lighting term. This is an albedo-data consequence, not a full original-effect/GPU-fidelity claim.

## Ownership, failures and initializer composition

The existing source cache clones resolver-owned bytes before decoding. Mutating all 39 returned source buffers after preparation leaves all four factor records, both owned program metadata fingerprints and every mip hash unchanged. Default and opt-in retained texture hashes are identical. Source count and byte gates reject at 38 resources / 4,686,370 bytes; fresh exact limits 39 / 4,686,371 succeed after failures.

Native negatives also cover both missing/masked programs, altered final PEXE instruction word, one-byte-truncated program, wrong caller object/appearance pins, changed pinned object/appearance payloads, pre-canceled preparation and first-lookup cancellation. Cancellation after the first resolver call results in exactly one call and no resource result. False still loads successfully when each optional program is masked, preserving its 37-source closure. No external file is mutated; altered payloads are private in-memory resolver fixtures.

The existing [resource initializer](SCENE_CREATURE_RESOURCE_INITIALIZER_CONTRACT.md) accepts the true-mode original bundle through an explicitly injected loader. Its request/API is unchanged; the loader chooses true. A real Task worker opens and disposes its own original archives before owner Poll transfers the result. The exact 39-source / 4,686,371-byte request succeeds, the factor fingerprint matches, and one-shot transfer leaves zero outstanding jobs. Separate initializer requests one resource/byte below the returned union reject resource accounting with Faulted completion and null resources. Initializer disposal/drain and registry closure complete. This proves composition/accounting; it adds no automatic albedo option to initializer requests or new optional-semantic validation there.

## Verification and reproduction

[Own scenarios](../poc/Stardust.Core.SmokeTests/SceneCreatureAlbedoScenarios.cs) pass 16 reader rejection cases and two initializer-accounting failures, source/program/pixel ownership, unchanged false modes, exact limits, actual sampled colors and real initializer transfer. The unchanged earlier creature native regression passes 75 rejections and its unchanged independent audit verifies the 37-source default, caller pins and raw-create identity. The shared runner is untouched.

[New Python verifier](../tools/python/verify_scene_creature_albedo_resources.py) checks the exact default-plus-program union, every source priority/size/SHA, object inheritance and SAT/MLOD selected identities, original program/pass/factor/palette semantics via `verify_skeletal_albedo`, all 80 mips via the accepted Pillow DDS verifier and four actual sampled MAIN pixels plus hand-evaluated factor color. It also checks byte-identical 47-byte create re-encoding and the recorded failure/initializer receipts. It exports only aggregate identities/descriptions, not asset bytes. **Confidence:** Confirmed finite-fixture CPU agreement; research/function/protocol coverage denominators do not change.

From the isolated checkout with cached .NET 8 packages:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round10-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force local-private/round10 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round10/NuGet.Config
dotnet restore poc/round10-tests/Assets/Assets.csproj --configfile local-private/round10/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round10-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round10-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round10/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/verify_scene_creature_albedo_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json local-private/round10/native.txt > local-private/round10/python-audit.json
dotnet poc/round10-tests/Assets/bin/Debug/net8.0/Assets.dll --default-regression C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json > local-private/round10/default-native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/verify_scene_creature_resources.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json local-private/round10/default-native.txt > local-private/round10/default-python-audit.json
python tools/python/validate_organized_workspace.py .
```

Only the reader's two-line option forwarding and own new scenario/verifier/runner/doc/checkpoint files change. No shaders, palettes, profiles, existing initializer, shared runner or rendering code changes. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R10_CREATURE_ALBEDO.md) records baseline-relative validation. Lead acceptance/integration and descriptive HUEB reconciliation are next; stop idle pending a separate Round 11 dispatch.

## Round11 integration —2026-10-02

Round11 [palette-only customization](SCENE_CREATURE_PALETTE_CUSTOMIZATION_CONTRACT.md) returns owned selected factors without faces or source reloads. The [world-owned preview](OFFLINE_REPLICATED_WORLD_PREVIEW_CONTRACT.md) uses the accepted original39-source albedo closure.

## Round19 explicit face completion

[Opt-in resource route](SCENE_CREATURE_FACE_RESOURCE_CONTRACT.md) addresses the
default route's missing generated face with supplied inspection values.
[Requested completion and logical accounting](SCENE_CREATURE_FACE_COMPLETION_CONTRACT.md)
require exact values/binding/dependencies and account unique source-path inputs
plus generated image bytes. Equivalent separately decoded face cross-map inputs
must match metadata/all pixels; contradictory aliases reject. GeneratedFaces and
GeneratedFaceBytes receipt fields are zero for default/static resources.
[GPU inspection](SCENE_CREATURE_FACE_INSPECTION_CONTRACT.md) verifies actual head
MAIN binding and scoped pixel change. Original player customization/display parity
remain unknown; this does not infer full replicated appearance behavior.
