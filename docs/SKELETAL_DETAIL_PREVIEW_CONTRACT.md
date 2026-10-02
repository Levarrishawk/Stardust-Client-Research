# Explicit skeletal detail preview contract

## Scope and controls

The standalone original-human inspector accepts `-CreatureMeshDetail 0..3` (CLI `--creature-mesh-detail`). Omission preserves detail 0. The value selects the exposed MLOD member for both body and head; skinning keeps the existing level-0 skeleton policy. This is explicit selection, with no distance switching or interior-scene LOD integration.

Detail 1 retains the existing material, palette, albedo, normal-diffuse and specular options. Details 2/3 accept `-CreatureVertexLitOnly` (`--creature-vertex-lit-only`) with `-CreatureMaterialDefaults -CreaturePaletteDefaults -CreatureAlbedoFactors`. This selects the already implemented nodot3 material program without loading compressed normals or uploading tangents. Neutral/material-only lower-detail inspection remains available without vertex-lit-only.

Strict launcher parsing rejects negative, fractional and out-of-range values. Both launcher and direct CLI validate combinations before opening archives. Lower details reject generated face, shared customization and shape profiles as bounded preview policy. Details 2/3 reject normal-diffuse, normal-specular and texture-choice profiles. Vertex-lit-only requires detail 2/3 and owned material/palette/albedo resources. These restrictions describe this adapter, not executable parser acceptance.

## Verified original-human boundary

| Detail | Surfaces | Expanded vertices | Triangles | Sources with selected idle | Source bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 1256 | 1904 | 48 | 5648263 |
| 2 | 2 | 542 | 756 | 25 | 580246 |
| 3 | 2 | 163 | 190 | 25 | 539928 |

Expanded vertices are Godot surface upload counts, not unique MGN positions. Counts include the explicitly selected idle and material closure; they are not the material-only counts in [the lower-detail research contract](SKELETAL_LOWER_DETAIL_CONTRACT.md).

Details 2/3 share `shader/hum_m_body_sm.sht`, `effect/h_specmap.eft`, `texture/hum_m_sm.dds` and `pixel_program/h_specmap_ps20_nodot3.psh`. The latter has SHA-256 `C7A5218B031D9C0CD80C33C7D31611CAC4AFD7A241C794354879070DFD7DDB85`, 1672 bytes, overlay priority 40. Existing Core readers expose single-color hue, no alpha hue mask, `ScalesBloomRgb=true`, MATL specular power 20. Detail 1's mouth retains the two-color nodot3 family, alpha hue mask, bloom RGB scaling false and power 300. This adapter adds no shading equations. Source-backed pixel semantics and the unresolved literal compiled LIT boundary remain as documented in [mouth lighting](SKELETAL_MOUTH_LIGHTING_GPU_CONTRACT.md).

Godot retains MAIN/SPEC decoded mip bytes, texture bindings, MATL uniforms and program bloom-family identity. Mesh detail selects `[meshDetail]` rather than the former adapter's hardcoded index 0. Source lookup counts are checked once per path after successful loading. The existing background-load cancellation, owned bundles, deformation and idle controls remain in use.

## Validation and reproduction

[Rendering tests](../poc/round3-tests/Rendering/Program.cs) use the existing Core readers against an explicit original root/configuration and print compact source identities. [Source verifier](../poc/round3-tests/Rendering/verify_sources.py) compares these with each Godot `skeletal_detail_source=` log. [Option verifier](../poc/round3-tests/Rendering/verify_options.ps1) checks 13 invalid combinations and verifies no new engine log appears.

Run the console with `dotnet run --project poc/round3-tests/Rendering/Rendering.csproj -- <archive-root> <configuration>`. The option verifier requires explicit `-GodotPath -ArchiveRoot -Configuration`. Use the existing launcher with the pinned human object/SAT/idle identities in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R3_DETAIL_PREVIEW.md), body/head MGN pins from the lower-detail contract, and a fresh capture path. Details 1–3 each passed GPU capture, headless and cancellation checks. GPU checks include retained weighted deformation, 480 idle ticks/two loops, play/scrub/focus controls, mip/uniform verification and albedo fixtures. The new single-color GPU oracle covers eight bloom/fade/alpha combinations with fixed vertex inputs; maximum display error 0.003052, tolerance 2/255, bloom discriminator 0.056471. It verifies the pixel-family boundary; diagnostic lighting is not proof of every legacy lighting path.

The unchanged detail-0 retained interior scenario passed room/cell lighting, material/geometry retention, owner changes and its existing GPU fixtures. Validation used Godot 4.7.2 .NET, OpenGL compatibility, RTX 4070 SUPER. Vulkan and literal original compiled-program execution were not tested. Startup certificate-store errors persisted in Godot logs; acceptance assertions still passed.

Confidence: confirmed for explicit selection, original corpus identities and replacement assertions; diagnostic lighting remains a bounded approximation. Next action: lead integration review and registration of this explicit adapter contract. Automatic LOD requires a separately bounded task.
