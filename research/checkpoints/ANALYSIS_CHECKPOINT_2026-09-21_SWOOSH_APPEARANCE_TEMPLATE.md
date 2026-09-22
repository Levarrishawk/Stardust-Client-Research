# Swoosh Appearance Template (`.swh`, `SWSH`)

Date: 2026-09-21

## Result

This block follows CDF contrail references into the actual `.swh` appearance resource and resolves its factory, supported versions, embedded particle-texture definition, and complete serialized field widths. RTTI confirms `SwooshAppearanceTemplate`.

## Registration and type boundary

`FUN_007ac9c0` registers the RTTI-derived appearance tag through the appearance-template factory with factory `0x007ac950`. The allocated template is 0xb4 bytes in the original 32-bit implementation. `0x007ad260` creates a separate 0x344-byte live swoosh appearance instance from the template.

The four-character tag is `SWSH`. The executable also uses `SWSH` in an unrelated shader-template registry for `SwitchShaderTemplate`; these are separate registries and must not be conflated. File context (`appearance` versus `shader`) determines the decoder namespace.

## File envelope and versions

`SwooshAppearanceTemplate_load_VA_007AD2D0` accepts:

- `FORM SWSH/FORM 0000`;
- `FORM SWSH/FORM 0001`.

Both begin with an embedded particle-texture/template block parsed by the existing PTEX-family base loader, then a leaf `0000` parameter chunk. Version 0001 is the current save format.

A configured-overlay scan found 99 effective `.swh` names. Ninety-three have the expected readable SWSH envelope: 52 version 0001 and 41 version 0000. The other six are confirmed zero-byte overlay tombstones in `mtg_patch_016.tre` (`podracer_energy_binders*`, `pt_lightning_cannon`, and `pt_pulse_cannon_explosion`), not unsupported live formats.

## Parameter chunk

Both versions read, in order:

1. three `float32` values forming the swoosh RGB/color triple;
2. two `float32` presentation parameters;
3. three NUL-terminated strings;
4. four `float32` values;
5. one byte Boolean;
6. two `float32` tail parameters.

Version 0001 then adds three more `float32` tail parameters. Thus version 0000 has 11 serialized floats plus three strings and one Boolean after its embedded PTEX data; version 0001 has 14 floats plus the same strings/Boolean. Constructor defaults for the three 0001-only fields are 30.0, 10.0, and 5.0, so loading a 0000 asset preserves those defaults.

The three strings are stored as ordinary strings, not eagerly loaded resource pointers. Their runtime meaning is not exposed by the loader and remains unknown. The color triple is supported by a dedicated three-float setter and the template also exposes setters/getters for the first two presentation floats and two of the later float fields. Exact editor labels for the remaining numeric values are not recoverable from this block.

The effective `appearance/sw_ship_contrail_white.swh` is version 0001, embeds `PTEX/0000` using `shader/sw_gradient.sht`, and has an outer size of 160 bytes. This proves the trail is a shader-driven ribbon/swoosh appearance, not a mesh.

## External configuration tables

Template installation also loads:

- `datatables/weapon/swoosh.iff`, with columns explicitly referenced as `ObjectTemplateName`, `SwooshName`, and `UseObjectHue`;
- `datatables/weapon/swoosh_hue.iff`, including `HueMin` and RGB channel columns.

This supplies weapon-template-to-swoosh selection and optional object-hue remapping outside individual `.swh` files. Original asset compatibility therefore requires these datatables as well as the template parser.

## Replacement-client consequence

- Register `SWSH` in an appearance-template namespace separate from shader-template tags.
- Reuse the common PTEX decoder and bind its shader through the normal material pipeline.
- Represent a swoosh as a camera-facing or path-oriented ribbon with the decoded parameter set; do not attempt to load `.swh` as geometry.
- Preserve version-0000 defaults and all three strings, even before their runtime labels are completely recovered.
- Load the weapon swoosh and hue datatables to reproduce weapon trail selection and color behavior.
- The legacy 0xb4/0x344 sizes and pointer offsets are not portable data contracts.

## Confidence

- RTTI class, appearance registration, factory, versions, and field sequence: confirmed.
- Embedded PTEX/shader-driven nature: confirmed.
- RGB triple: strong (dedicated triple setter, hue-table integration, corpus values).
- Exact editor names for most scalar/string fields: unknown.
- Effective-corpus format coverage: confirmed complete (93 live templates plus six tombstones).
