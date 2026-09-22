# Sprite and Quad Appearance Closure

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

`SPRT` and `QUAD` share one installed factory, template class and `0000` payload. The format is a shader-backed quad/billboard appearance, not an image container. This closes the `.spr` boundary used by resource icons and the sprite fallback children found in legacy laser-bolt CMPA resources.

## File contract

Both outer tags contain one `0000` chunk with:

| Field | Wire type | Runtime role |
|---|---|---|
| center/offset | 3 x little-endian F32 | local center used for extent/quad placement |
| radius/half-size | F32 | multiplied by `sqrt(2)` for the runtime quad primitive and used with center for bounds |
| color | 4 x little-endian F32 | RGBA modulation passed to the quad primitive |
| shader | null-terminated path | resolved shader-template resource |

`sprite_quad_template_load_v0000_VA_0079BC50` decodes this exact sequence. `SPRT` and `QUAD` differ by outer identity but use the same loader and runtime constructor.

## Effective corpus

`tools/python/sprite_corpus_inventory.py` applies configured overlay priority and parses every effective `.spr` winner:

- 469 live effective resources, zero tombstones and zero parse failures;
- 369 outer `QUAD`, 100 outer `SPRT`;
- 385 distinct shader references;
- all 469 store `(1,1,1,1)` RGBA in the deployed corpus;
- 102 use zero center/offset and 99 use size `1.0`; the remainder carry authored geometry-specific centers and sizes.

The former 1,282 count is raw archive copies, not the effective resource set.

## Runtime contract

`sprite_quad_appearance_construct_VA_00789AF0` retains the template, scales the authored size by `sqrt(2)`, creates the shader-backed quad primitive, applies RGBA, constructs the render/visibility object and exposes its extent. The template factory builds an explicit box extent from center plus/minus the authored radius and associates it with the appearance template.

The resource-icon path then requires a `SpriteAppearance`, a `StaticShader`, and texture tag `MAIN`; this confirms that the sprite's shader indirection is externally observable and cannot be flattened to a DDS filename during import.

## Replacement contract

- Preserve `SPRT` and `QUAD` aliases and the exact v0000 payload.
- Resolve the shader through the overlay-aware shader system and keep tag-driven texture lookup.
- Convert center, size, color and quad orientation through the renderer/engine coordinate adapter.
- Preserve bounds independently from camera-facing presentation policy.
- Permit engine-native billboard/quad primitives, but retain shader parameters and object visibility/lifecycle behavior.

## Confidence

- Format, endian/width, version, effective corpus and factory aliasing: **Confirmed**.
- Runtime quad creation, size conversion, color/shader and bounds: **Confirmed**.
- Original human-readable distinction between outer `SPRT` and `QUAD` presentation policy: **Unknown**; no incompatible payload exists.
