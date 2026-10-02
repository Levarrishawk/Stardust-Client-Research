# Owned character palette defaults — October1,2026

## Bounded question and inputs

Can original authored palette factors travel with the object-root character material owner, preserving names/namespace, fallback and exact colors before shading translation? Same pins as the [material checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_MATERIAL_DEFAULTS.md). Existing CSHD palette bundle/query/RIFF decoder are reused; no new palette format is invented.

## Findings and confidence

Explicit `loadPaletteDefaults` requires owned material defaults and retains per-shader authored factor bundles. Three original palette resources supply five factors across body/head/mouth; the face shader has no palette factors. Exact names, private flags, tags, authored index, selected index, raw flags and opaque ARGB are retained. The raw255 flag in the eye palette remains a flag, not alpha. Missing/masked palette uses the existing diagnostic opaque-cyan fallback; malformed present palette rejects. Confirmed original-byte/query behavior; shading remains unexecuted.

Native `--skeletal-palette-defaults` closure is35 sources/4,670,496 bytes. Independent source/default/color/namespace/mip and sampled pose audits pass. Original fallback and malformed dependency cases pass; invalid dependency option rejects before lookup. Interior union with the same opt-in is100 sources/14,284,581 bytes, each externally resolved once; unchanged2,880-tick route/geometry plus independent palette/mip audit pass. Standalone/interior GPU animation/readbacks and both cancellation fixtures pass. Build/native suite pass.

## Compatibility consequence and next action

Launcher `-CreaturePaletteDefaults` requires `-CreatureMaterialDefaults`; `-SceneCharacterPaletteDefaults` requires `-SceneCharacterMaterialDefaults`. These options load/retain color evidence without tinting the preview or choosing an original effect branch. The base texture-only modes retain their previous32/97-source closures. Next: implement the original head TRTS-selected BTRT envelope/live command description before generated-face execution. Palette shading, generated texture preparation/draw, normal/specular lighting and animation state graphs remain open. Scores/registry counts unchanged.
