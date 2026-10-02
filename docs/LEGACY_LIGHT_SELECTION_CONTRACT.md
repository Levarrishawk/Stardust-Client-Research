# Legacy bounded light selection contract

## Scope and evidence

This contract begins with an already selected runtime pointer vector. It does not recover live DPVS visibility, registration order, global environment lights or every shader implementation. `verify_legacy_light_selection_static.py` verifies both documented executable hashes and25 instruction anchors before accepting VAs. No original code executes.

Sorter registration `0x00773590` maps participation byte+0x11C to mask `0x01923520` and+0x11D to mask `0x0192351C`. Submission `0x00773910`, supported by `0x00773970`, uses the latter when `StaticShader::containsPrecalculatedVertexLighting` is true and the former otherwise. `0x007736D0` materializes the selected mask in registration order, then passes the pointer vector through the known raster boundary. **Confirmed** by direct instructions/caller chain.

Cell creation `0x006E5D60` clears+0x11D on each primary light at `0x006E5EE1`. A second light is created only for nonzero source second RGB, its diffuse RGB is zeroed, and+0x11C is cleared at `0x006E5FCD`. Therefore precalculated room materials exclude primary cell diffuse lights and receive secondary specular-only instances; unbaked door/character materials use primary instances, already containing both original colors. This closes the previously provisional mask distinction. Other externally created light classes can participate in either/both groups and remain outside the cell-only fixture.

## Ranking and slots

DLL classifier `0x62A0A160` accepts runtime0 ambient,1 directional and3 point. It accumulates ambient RGB into caller/global ambient. Other type codes are ignored in this observed classifier.

| Group | Specular-capable slots | Diffuse-only overflow slots | Globals |
|---|---:|---:|---|
| Directional |1|2|specular `0x62B83890`, diffuse `0x62B83894..98`|
| Point |1|4|specular `0x62B8389C`, diffuse `0x62B838A0..AC`|

Each incoming candidate competes first for the specular slot using strict greater-than cached intensity. An empty slot accepts the first candidate even when its score is zero. The losing/displaced candidate then competes for diffuse slots using its diffuse intensity. Equal scores preserve arrival order within the relevant competition; diffuse arrival order can differ from original input order after specular displacement. Distance/range/attenuation does not appear in this ranking stage. **Confirmed** for these slots/branches; upstream visibility can still use range.

Raw diffuse color/score are+0xA4/+0xB4, raw specular+0xD0/+0xE0. Scaled counterparts are+0xBC/+0xCC and+0xE8/+0xF8. `0x62A0A050` installs the corresponding callbacks based on a caller boolean. Its sole direct caller `0x62A046B0` supplies engine StaticShader byte+0x12. The configuration/constructor/copy chain below closes its ownership and default. Cell construction initializes separate scales to1. Cached score is absolute RGB weighted by F32 constants(0.3,0.59,0.11), with one F32 store; constructor `0x0076D500`, factory `0x006E5AA0` and color helper `0x00506910` establish the calculation. Negative RGB is ranked by magnitude, not clamped. Exact x87 rounding at exceptional tie boundaries remains outside replacement precision claims.

Constant builder `0x62A0A3C0` uploads diffuse and specular for each specular-capable slot, but only diffuse for overflow slots. The first directional slot also supplies normal-mapped pixel-light constants. Empty state and changed selection trigger a full28-vector upload through `0x62A0E210`; unchanged selection uses `0x62A0A960` to refresh viewer/object-dependent primary constants. The cached state does not justify stale owner/view transforms.

## Owned model and validation

`LegacyLightSelector` is an independently authored engine-neutral partition of finite bounded active inputs, with unique keys and maximum32. It models scaled/raw colors, ambient, ignored types, stable specular competition and displacement-order diffuse competition. It does not copy legacy memory layouts. Strict finite/size/key rules are replacement malformed-input policy.

`CellInputs` is an explicit authored-order fixture: unbaked primary records or baked secondary specular-only records. It is not proof of live registration order. Twelve original three-cell/raw-scaled/material-group cases agree with an independent streaming insertion model;64 synthetic24-input cases exercise capacity, changing winners, ties, reverse order, negative RGB, ambient, ignored types and differing scales. The independent original score maximum error is3.328e-7 (JSON/F32 text precision), bounded by2e-6. Existing156-source/16,624,079-byte chain and2,880-tick route pass. No source reads are added.

Implementation consequence: baked room GPU inputs must exclude primary diffuse cell contributions. Role-specific overflow entries must not add specular. Unbaked normal-map primary selection should follow the selected directional slot, not simply the first authored direction. Ambient, attenuation safety, original vertex LIT and live visibility/order remain separately explicit or unresolved. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_LIGHT_SELECTION.md).

## Retained cell adapter

`CellLightingPreview.FromLegacyCell` materializes the selected roles using owned cell records. It preserves original source indices, keeps diffuse-only overflow specular zero, accumulates ambient separately, and orders full vertex contributions directional-specular/directional-diffuse/point-specular/point-diffuse. Original CELL scales remain1; live registration/visibility and global contributions are not reproduced. The finite/nonnegative preview and attenuation cap remain explicit modern policies.

Baked room rendering now always uses the confirmed secondary-only cell mask with ambient0, correcting the prior additional primary diffuse terms. `-SceneLegacyLightSelection` opts the unbaked character and doors into the recovered partition; their caller ambient remains0.1. Room/door/actor ownership and retained resource publication are unchanged.

576 original grouped/owner/position/normal/power/primary-inclusion cases agree with independent double math within1.530e-7. Hand arithmetic proves overflow contributes no specular and baked records contribute zero diffuse. Thirty-two additional GPU cases include all8 selected slots and raw primary/additional roles; maximum display error0.003183. Original route/camera/live-controls/source counts and retained meshes/materials/textures pass; headless/cancellation and inspected capture pass. [Integration checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_CELL_LIGHTING_PREVIEW.md).

The [original positive attenuation contract](LEGACY_POINT_ATTENUATION_CONTRACT.md) now closes coefficient order and uncapped reciprocal behavior. Recovered-slot preview uses that equation for positive finite inputs, with explicit modern singular omission; historical capped preview remains a separate policy. Original high-power vertex LIT behavior remains unresolved.

## Configuration snapshot for scaled colors

ClientGraphics.enableLightScaling defaults true in client_graphics_config_install_VA_00779A50. Getter client_graphics_light_scaling_enabled_VA_00779E50 reads global0x01923544; both engine StaticShader constructors capture it at shader+0x12, and copying preserves that byte. The raster wrapper separately reads backend data from shader+0x0C and the selector from shader+0x12. True selects scaled RGB and cached scores; false selects raw counterparts. This is a construction snapshot, with effective external overrides not inferred from absent local keys.

Twenty-three baseline instruction/literal checks, original caller/global references and owned configurable/default mode tests pass. FromLegacyCell represents both modes and defaults true. Current CELL scales are1, so576 independently audited original math cases/source union/route/headless stay unchanged. One canonical getter is added,2602 total; no function-coverage percentage follows. [Selector checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIGHT_SCALING_SELECTOR.md). Live global scale updates/registration order remain separate.

## Default selected scene lighting

SceneCellLighting now selects the recovered partition, captured-default scaled mode and closed positive point attenuation automatically. SceneLegacyLightSelection remains an accepted compatibility alias requiring cell-light resources. Original authored-order fixture membership and caller ambient0.1 remain explicit; this does not add live visibility/order reconstruction. The prior modern authored-first helper remains available for its mathematical fixture coverage. [Default checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_RECOVERED_LIGHTING_DEFAULT.md).
