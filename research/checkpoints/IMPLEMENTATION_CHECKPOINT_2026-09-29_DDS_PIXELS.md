# Original DDS pixels and mip boundaries

Date: 2026-09-29. Question: can owned native RGBA pixels reproduce the original
static mesh's textures independently, including small mips and alpha branches?
Stop at pixel agreement and malformed-input rejection, before GPU upload.

## Evidence and identity

Input executable SHA-256 rechecked:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The [previous asset checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_SHADER_OVERLAY.md)
contains mesh/config identities and the exact configured archive selection.
The [DDS research](ANALYSIS_CHECKPOINT_2026-09-12_DDS_TEXTURE_RESOURCE.md)
distinguishes original acceptance from effective corpus formats.

`texture/stco_tan.dds`, priority 37, `mtg_patch_008_texture_04.tre`:
43,832 bytes, DXT1, 256×256, nine mips; SHA-256
`B883C89B69FB258B62EF680A25A729510AE5FB3B235749A07FAB073771EED003`.
Mip 0 RGBA SHA-256:
`68341FB18144C2C2880D27F9641DA17F75499473DB8783D97E0B070C712C4E7D`.
Native and Pillow 12.3.0 agree on every mip, down to 1×1.

Separate original alpha samples also match every mip:

| Member | Archive | Identity / levels |
|---|---|---|
| `texture/anim_bank_side_screen_00.dds` | `mtg_patch_005_texture_01.tre` | DXT3, 5,616 bytes, 64×64, 7 mips; SHA `D80CCB062356C3D4C76BCD808A92A8112F5DFC75936D251596BD9F0A96796F53` |
| `texture/stco_buff_dirtmask_tower.dds` | `mtg_patch_008_texture_04.tre` | DXT5, 87,536 bytes, 256×256, 9 mips; SHA `E2F18F974A157F58FF74A961F5C3A46344B0524E4B03B4DE5C4DB65598516CD5` |

## Implementation and independent verification

**Confirmed for this slice:** `DdsDecoder` owns RGBA8 mip output, bounds all
extents before allocation, preserves BC1 transparency and both BC3 alpha modes,
forces four-color BC2/3 RGB interpolation, clips partial 4×4 edge blocks, and
reorders tightly packed BGRA32. Source input is not retained. The
[durable contract](../../docs/DDS_DECODER_CONTRACT.md) records precise limits,
channel policies and unsupported formats. No legacy GPU bit-exact rounding claim
is made; the independently chosen CPU reference is Pillow.

An independent Python bundle verifier reads all 48 configured raw-name TRE
indices, honors priority/masks, extracts the mesh's shader dependencies and
compares native versus Pillow pixels for all **80 mip levels in nine textures**.
Eight are BC1; `texture/stco_smooth_fallback.dds` is BC3. One texture has eight
mips; the others have nine. Mesh geometry remains independently matched.

```powershell
python tools/python/verify_dds_pixels.py C:\Stardust-DEV\mtg_patch_008_texture_04.tre texture/stco_tan.dds --expected-sha256 B883C89B69FB258B62EF680A25A729510AE5FB3B235749A07FAB073771EED003 --core-project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
python tools/python/verify_textured_mesh_bundle.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh CA4F3070B8BBC9CD50DEC617AC2C4C249F3E888E15EB10E6F832113F7C6D126B poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
```

Synthetic tests cover exact palettes/alpha, row order, owned storage, clipped
3×5 blocks, 4/2/1 mip sizes, all truncated prefixes, trailing bytes, invalid
dimensions/counts/headers/flags/surfaces and input/decode budgets. Full suite passes.
These are replacement policies where stricter than legacy behavior.

Next: carry declared mip pixels into Godot unchanged, verify upload readback,
and establish UV orientation separately from DDS row decoding. Original assets
and derived images remain private; no publication or original execution occurred.
