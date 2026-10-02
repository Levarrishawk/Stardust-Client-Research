# Original room material/pixel boundary

## Scope and selected variants

`StaticRoomLightingReader` explicitly selects one supported original variant for each of three precalculated room families. This is not reconstruction of hardware capability selection. The [COLOR0 boundary](STATIC_VERTEX_COLOR_CONTRACT.md) remains separate from pixel arithmetic and the [modern cell-light selection](CELL_LIGHTING_PREVIEW_CONTRACT.md).

| Effect family | Selected original vertex asset | Pixel program | Secondary alpha |
|---|---|---|---|
| c_specmap | tfcsl_2uv | smap_mt, PS1.1 | SPEC texture |
| c_emismap_bloom | c_simple_vs11 | a_emismap_bloom_ps20_nodot3 | MAIN |
| c_specmap_emismap_bloom | c_spec_vs20_nodot3 | a_specmap_emismap_bloom_ps20_nodot3 | EMIS texture |

The owned reader checks exact pixel instruction identities and vertex-source hashes, one matching opaque pass, sampler tags/stages, MAIN UV0, source textures, material packing and selected factor presence. Secondary sampling uses the selected vertex asset's MAIN-coordinate output. A separate TCSS entry for SPEC/EMIS is not required. Other families remain outside this boundary; the original door normal-map variant is not replaced.

## Pixel equations

Let C be MAIN RGB, a its alpha, D/V supplied vertex diffuse/specular, e secondary alpha, q selected texture-factor red, B bloom, F fade and f opacity.

- Separate specular: RGB=`C*D + V*e`; A=`f`. The original PS1.1 program always writes the fade-opacity constant.
- Emissive: RGB=`C*(D*(1-a)+a)`; A=`F ? f : q*a`. Bloom Boolean does not change this selected pixel equation.
- Combined specular/emissive: S=`V*a`; RGB=`C*saturate(D+e)+S*(B?0.5:1)`; A=`F ? f : luminance(S)*(B?0.65:1)+q*e`, luminance weights0.3/0.59/0.11.

Masks/fade are0..1; RGB remains finite/HDR and is not finally clamped. Emissive strength is explicit and nonnegative. The helper neither chooses scene lights nor emulates raster interpolation precision.

## Factor absence and evidence boundary

Seventeen of18 original selected passes have first texture-factor tag0. Their typed `TextureFactor` is null, not a guessed color. The screen material selects BLUM and its RGB is88/255. Backend62A0EB8E..62A0EC21 skips first-factor lookup for tag0;62A0EFA4..62A0EFC5 conditionally uploads the two factor constants together. Missing/unselected factor state and inherited original bloom-alpha behavior remain unresolved. RGB equations above are independent of q; an opaque preview may supply an explicitly documented alpha-only policy, without claiming original bloom equivalence.

## Vertex evidence

Original c_simple source assigns COLOR0 directly to diffuse. The c_spec nodot3 source adds baked COLOR0 to computed dynamic diffuse; shared original helper starts diffuse with material emissive, not global ambient. Assembly c_ambient seeds the diffuse accumulator from COLOR0, and diffuse_specular finishes by adding material emissive and multiplying accumulated specular by material specular color. This supports baked-plus-dynamic separation, while original selection limits, literal LIT behavior and runtime compilation precision remain Unknown.

## Ownership, closure and validation

Opt-in content loading adds six original program/source files,6,924 bytes, to the same cache. Complete selected chain:154 sources/16,618,979 bytes, each external read once.18 material bindings/MATL/factor maps and three instruction hashes independently agree.192 supplied-input cases execute the actual original PEXE with independent interpreters; maximum error7.27177e-8. All truncated synthetic envelopes derived in memory and changed instruction identities reject; missing/masked/oversized program gates pass. No original code/assets are written to curated fixtures.

## Retained room GPU adapter

`-SceneRoomLighting` enables42 original room surfaces through42 per-cell/material overrides. The18 authored material descriptions share cached shaders and existing28 room/door GPU textures. Each mesh instance owns its override; immutable template meshes/base materials remain unchanged. Door surfaces retain the earlier inspection policy.

Specular families compute dynamic vertex diffuse/specular from the explicit eight-light owner-cell policy, add baked COLOR0 and material emissive to diffuse, and use zero extra ambient. The screen family preserves pure baked diffuse. Interpolated vertex colors pass through the legacy pixel range boundary. Selected original pixel equations then consume MAIN/secondary textures. Unselected first factors get an explicit zero **alpha-only** strength in this opaque preview; no original bloom-alpha equivalence is claimed.

Building owner transforms republish all selected cells' light positions/directions into retained overrides. Actor/camera movement does not reassign room light ownership, and visibility toggles do not republish room lighting. Camera/view direction is evaluated in shader world coordinates.

Forty-eight GPU material cases verify all three pixel families, masks, emissive, bloom/fade/alpha display and HDR/negative legacy inputs, max0.002881 within2/255. The original route/camera/live-character controls pass with12 owner publications, unchanged shared resources and no duplicate texture uploads. Headless/cancellation pass; the rendered room capture was visually inspected. Exact original light ranking/LIT/raster interpolation and unselected factor state remain Unknown.

See the [GPU checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_ROOM_LIGHTING_PREVIEW.md). See [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_ROOM_LIGHTING.md).

Apply the [legacy COLOR input range](LEGACY_PIXEL_COLOR_INPUT_CONTRACT.md) before these pixel equations. The192 instruction fixtures deliberately supplied raw registers, including diagnostic HDR, independently of that raster boundary.

The door family now has its own [static normal material contract](STATIC_NORMAL_MATERIAL_CONTRACT.md); it remains separate from baked room inputs.

The [recovered slot adapter](LEGACY_LIGHT_SELECTION_CONTRACT.md) now supplies confirmed secondary-only cell inputs to baked rooms; optional recovered selection also covers unbaked character/doors. Prior all-record room diffuse contributions are superseded. Ambient/attenuation/vertex exponent remain explicit policies.
