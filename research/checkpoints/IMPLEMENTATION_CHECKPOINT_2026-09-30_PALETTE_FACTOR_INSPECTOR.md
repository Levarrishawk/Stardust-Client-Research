# Palette-factor inspector — 2026-09-30

## Question and stopping condition

Can prototype host expose the verified factor-query state as editable swatches, preserving shared variable keys and per-factor authored defaults, and reproduce actual RGB output? Close on controls plus headless/GPU gates for a present and missing-palette original shader.

## Inputs and evidence

Uses the [factor-bundle contract](../../docs/PALETTE_DECODER_CONTRACT.md) and pinned original configured overlay, unchanged executable identity from [checkpoint 24](IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_BUNDLES.md). Prototype host 4.7.2 .NET x64, OpenGL compatibility, RTX 4070 SUPER. Replacement proof only; no original execution or live sockets.

Present sample: `shader/yt1300_a_patterna_swap_hcsc22.sht`, SHA `B301C855F3AA3C94E62A1CC26B2062C5E63F25412B9A409F5CC1DE6F2DCA10C1`, priority 52. Three sources, two palettes, two factors: authored MAIN trim entry zero `0xFFFEFEFE`, HUEB base entry 58 `0xFF925858`.

Missing sample: `shader/armor_swamptrooper_leggings_hcsb21.sht`, SHA `DE3EAE24D93E8114518E2A05FCB5C9B63177338189D28986B798B15A32D13B5C`, two factors and one unavailable palette. MAIN displays confirmed opaque cyan; the present stripes palette remains distinct.

## Findings, confidence and consequence

**Confirmed by native/corpus plus host gates:** inspector rows show exact tag/scope/name, palette path, requested index, selected entry, opaque ARGB and raw flags. Authored checkboxes remove supplied keys; spin controls supply exact Int32 state. Repeated name/private keys share supplied state, while reset restores each record's own serialized default. Model factor results are projected without mutating scene materials.

**Confirmed by GPU:** both original shaders display two swatches with per-channel readback within 2/255. Visible swatches are read from the viewport image after two frame completions. Captures stay private and were visually inspected. No original shader lighting or factor semantic translation is inferred from canvas colors.

A synthetic repeated-variable fixture initially failed because its controls were outside the scene tree and emitted no value-change callbacks. The fixture now enters the same hidden scene host before exercising extremes/reset; headless and GPU gates pass. Failed logs are retained privately. This was a test-host lifecycle correction, not a query-policy change.

## Validation and reproduction

Build succeeds without warnings. Native smoke/full factor agreement remains as checkpoint 24. Actual original controls and synthetic shared controls pass minimum/maximum overrides and authored reset. Headless gate explicitly does not claim GPU readback. Final missing-shader GPU capture passes both swatches and the shared fixture. Existing nonfatal prototype host certificate-store startup error persists with exit zero.

Run `Invoke-StardustAssetPreview.ps1` with `-PaletteInspector`, exact archive/config/member/SHA, then `-Headless`, bounded default GPU validation, `-CapturePath` under private storage, or user-visible `-Interactive`. This mode rejects incompatible appearance/material switches and caps UI factor rows at 128. Core parsing caps remain separate.

## Next probe

Factor controls are an inspection aid; original material shading and live owner state remain open. Next viable milestone: compose owned APPR hardpoints through static appearance instances and verify the transform boundary for attachment inspection. Registry/protocol/research-score counts are unchanged.
