# BC6H HDR material bundle contract

## Engine-neutral image boundary

Static material loading now recognizes typed DX10 BC6H95/96 and routes their validated bytes to the existing owned HDR decoder. `TexturedStaticMesh.Textures` retains RGBA8 images; `HdrTextures` separately retains RGB-half images. `TextureCount` includes both. No HDR pixels are coerced to8-bit albedo or assigned an invented alpha channel. DDS alpha metadata remains retained by the HDR decoder; the RGB format carries no alpha samples.

Per-mesh texture cardinality64 and encoded/decoded128 MiB budgets include both image families. The per-load content-identity decode cache shares HDR descriptions by encoded SHA and charges six bytes/pixel once, alongside four-byte RGBA images. Duplicate HDR references share one image; mixed-image aggregate limits are enforced. Local malformed texture failures remain explicit material fallback; aggregate shared-budget failures remain fatal. No peak-allocation guarantee is implied.

Typeless BC6H94 and unsupported surface/header layouts remain rejected. Structural HDR candidate detection does not skip full DDS validation. Existing owned half pixels, signed/HDR values, reserved-mode zero output, exact mips and source identities remain unchanged.

## Prototype host sampler and ownership

The shared preview texture cache uploads RGBH and RGBA8 under one128 MiB budget. Actual HDR texture data remains binary16 RGB; material samplers use linear data without `source_color` gamma conversion. Array meshes/material adapters share the same texture object for repeated HDR content. Full chains or one mip are accepted for prototype host upload; valid partial CPU chains still require another upload policy.

The explicit opaque/unlit MAIN inspection policy remains in force. GPU texture readback and sampler binding preserve all half bytes. Signed negatives and values above one are retained; this does not specify their final viewport appearance, exposure or tone mapping. Original shader programs, lighting and HDR display policy remain separate. This modern DDS extension is not evidence of original executable DX10 support.

## Gates and reproduction

The existing forty standards-only BC6H fixtures cover264 mips, all fourteen valid modes, signed/unsigned output, reserved modes and clipped edges. Their material extension uses independently authored synthetic MESH/SSHT/EFCT/CMPA fixtures, two mesh templates per texture, and no proprietary data. Native material loading matches independent bcdec half output for every mip. All80 template adapters retain opaque MAIN material bindings, share one texture per pair, verify linear sampler identity and RGBH bytes, and perform geometry/upload readback. Actual GPU and separate headless runs pass. The independent fixtures contain29,832 negative components and 45,349 above-one components; negative-zero/negative-infinity endpoint edges remain outside these vectors.

Native smoke also verifies repeated HDR lookup/description ownership and exact shared96-byte budget, then a mixed64-byte RGBA plus96-byte HDR graph with exact160-byte pass/159-byte fatal failure. Existing original YT-1300 authored inspection remains87 sources/321 mips/39 effect records with independent agreement.

Build historical implementation path (omitted) and run `Invoke-StardustHdrVerification.ps1 -Materialshistorical prototype identifier (omitted)-Headless` reports gpu=False; actual GPU is separately verified. `generate_hdr_material_fixtures.py` writes synthetic-only fixture assets into the private directory. The launcher reports bounded completion and validates40 fixtures/264 mips/80 templates. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_MATERIAL_BUNDLES.md).

Next: palette asset structure and exact packed-color handling, before translating customization factor colors.
