# Selected human head material gap

## Finding

The current39-source human route leaves the generated face MAIN unprepared. The selected face surface therefore displays authored `texture/hum_m_face.dds` with two white factors, while the torso and separate `hum_m_head.sht` surfaces receive the authored skin palette color. This resource/adapter configuration is **Confirmed**. It is **Strongly inferred** to explain the reported pale/white-looking face in the accepted continuous GPU preview. A fresh GPU parameter/screen mapping was not run in this diagnostic; the expected original player's complexion remains **Unknown**.

The face texture is not solid white: its mip0 has356 white-RGB pixels of1,048,576; channel minima are74/81/74. Treating the symptom as a missing DDS or white-texture fallback would misidentify this sample. No production material or renderer change is made.

[Diagnostic producer](../poc/round18-tests/Assets/Program.cs), [verifier](../poc/round18-tests/Assets/verify_head_gap.py) and [dated checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-02_R18_HEAD_MATERIAL_GAP.md) reproduce the exact current closure. Prior authority: [material defaults](SKELETAL_MATERIAL_DEFAULTS_CONTRACT.md), [albedo factors](SKELETAL_ALBEDO_FACTOR_CONTRACT.md), [scene albedo route](SCENE_CREATURE_ALBEDO_RESOURCE_CONTRACT.md), [face composition/binding](BLUEPRINT_FACE_COMPOSITION_CONTRACT.md) and [explicit customization](SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md).

## Exact selected chain

The pinned root `object/creature/player/shared_human_male.iff` (`E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243`) resolves `appearance/hum_m.sat` (`951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C`). The selected detail0 closure has39 sources/4,686,371 encoded bytes. The exporter owns descriptors, clears borrowed arrays and closes the readonly overlay before examining them.

| Current face chain role | Path | Priority / bytes | SHA-256 |
| --- | --- | --- | --- |
| Head MGN; slot0,644 retained triangles | `appearance/mesh/hum_m_head_l0.mgn` | 53 /148489 | `84DFF58013C8374D80D691549A67E009A84344649010C9A4365552C9FCE48F19` |
| Slot0 CSHD0001 with inline SSHT | `shader/hum_m_face.sht` | 53 /709 | `C19E1442D813922467DEFCECA36DD25F3EB27D3825675297CBDAA35AAC061143` |
| Selected effect | `effect/h_color2_specmap_cbmp.eft` | 40 /2239 | `FDEE1E277B1002996F35EB1058EAD59ADB5F785E05B323B95E5BD841AC07CC11` |
| Validated PS1.1 alpha-hue albedo program | `pixel_program/h_color2_specmap_ps11.psh` | 40 /1399 | `95E44154F4107866CF23FD5C77E082573211135BE17E1CE679243A2AEB85EB92` |
| Named MAIN/UV0 authored image; BC1,1024²,11 mips | `texture/hum_m_face.dds` | 55 /699192 | `984110FED11EF8DD18AD6F927254B08DE369A89E3A6409FD98AAFB2E211CEF35` |

MGN TRTS targets `(head mesh,shader index0,MAIN)` through `texturerenderer/hum_m_face.trt`. The geometry reference is present, but that renderer is **absent from the current39-source map**. `FaceBlueprints`, `FaceResources`, `GeneratedFaces` and `CustomizationValues` are all null. The historical [blueprint fixture](BLUEPRINT_FACE_DESCRIPTION_CONTRACT.md) already establishes this renderer's bounded preparation path; its data is not reread or promoted here.

The face has no CSHD palette factors. Its authored TFNS MAIN/HUEB and retained albedo FirstArgb/SecondArgb are both `FFFFFFFF`. PASS factor tags are MAIN/HUEB; stage0 samples MAIN. The validated albedo equation reduces to sampled RGB because both hue endpoints are white. CSHD's `index_age` authored0 selects CNAM wrinkle detail, not a skin MAIN factor; it does not supply generated face color in this route. Normal/specular execution is outside the current albedo inspection.

## Torso versus head binding

`appearance/mesh/hum_m_body_l0.mgn` has body shader slot0 (1,322 triangles) and head shader slot1 (428). The selected head MGN has face slot0 (644), mouth slot1 (134) and head slot2 (282). Thus a shader named `hum_m_head.sht` is not the entire visible face.

Body MAIN and both head-shader occurrences select shared-owner `index_color_skin`, authored index0 of `palette/pc_skin_hum.pal` (SHA-256 `09443BDC0DE66A85D0DEB15813CCB9FCBFE6F5F8A6E417CF6D46DC910A494160`), yielding `FFE2B7AF`. The head shader uses the single-factor program; the body uses the alpha-hue program with secondary `FFD5D5D5`. All present palettes are loaded; no missing-palette diagnostic is used. Mouth uses its separate eye factor. The face remains white-factor preserving because a generated face is expected to carry layer palette colors already; global skin tinting would also tint generated detail.

At the CPU midpoint samples, body evaluated RGB is approximately `(0.769,0.611,0.584)`; face is `(0.906,0.875,0.871)`. These are actual source-encoding sample equations, not a screen-color oracle or a proposed original complexion. Face mip0 RGBA hash is `46F2C0B7003C2FAEBC2EC803E1B4A5DF54DBA3DA56095441FBCACDB59D1CC7E5`. Existing Pillow decoding independently matches all four selected MAIN mip0 hashes. No DDS decoding mismatch is observed.

[SceneCreatureResourceReader.Load](../poc/Stardust.Core/Assets/SceneCreatureResourceReader.cs) requests materials/palettes/albedo but leaves face blueprint/resource/program preparation at defaults. [SceneCreatureVisual](../poc/Stardust.Godot/SceneCreatureVisual.cs) constructs [SkeletalMeshPreview](../poc/Stardust.Godot/SkeletalMeshPreview.cs) from that bundle. The preview assigns each geometry slot through `Get(shaderPath,meshPath,slot)`. [SkeletalMaterialPreview](../poc/Stardust.Godot/SkeletalMaterialPreview.cs) binds authored MAIN/UV0, applies the retained factors, and substitutes generated MAIN only when `GeneratedFaces.Bindings` supplies the exact mesh/slot/tag. No generated binding exists here. Its diagnostic blue geometry fallback is not selected when the material bundle is present.

## Classification and smallest next test

- **Deliberate current route limitation — Confirmed:** face preparation/composition is omitted and the authored pale texture remains the selected MAIN. The current [pure resource gate](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md) also rejects unrequested face extensions.
- **Missing replicated customization — Unknown as an original-color cause:** this fixture supplies no decoded player appearance snapshot. Existing explicit authored inspection profiles can drive the face pipeline; they do not recover an original player's values.
- **Unsupported shader semantics — separate limitation:** full lighting/specular/gamma/controller behavior is unclaimed. The relevant albedo program and generated-face pipeline already have bounded implementations and prior proofs.
- **Decoding/binding defect — no evidence in this CPU/source probe:** actual named MAIN/palette/program identities and independent decoded hashes agree. Fresh GPU parameter identity and exact symptom-region attribution remain unrun.

The smallest next integration test is an **explicit opt-in generated-face resource route** using the existing blueprint/resource/compositor/customization helpers and a labeled supplied inspection snapshot. Preserve the default39-source mode. Update recipe-specific completion validation and logical accounting for requested generated output; set explicit source/byte limits rather than assuming39-source budgets. The established full-face inspection previously used a128-source/32MiB limit. Derive the new union/counts mechanically; do not reuse its historical counts as current movement-route totals.

Require one generated binding at `(hum_m_head_l0.mgn,0,MAIN)`, independently verify its CPU output, then read back the actual Godot material texture identity and inspect the corresponding face region. Body/head-cap MAIN/factors should retain their intended bindings, animation/resource lifetime and cancellation/drain should pass, and malformed/missing values or late/stale results should publish no partial face. Only after that proof should the continuous route opt in. Automatic skin tinting or invented replicated values would not close the missing face-generation boundary. No actual fix is implemented this round.

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
