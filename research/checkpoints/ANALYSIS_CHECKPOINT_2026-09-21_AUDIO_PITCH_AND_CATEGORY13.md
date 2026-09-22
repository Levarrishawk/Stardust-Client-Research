# Audio pitch units and category-13 boundary

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Pitch unit is semitones

The pitch conversion at `0x00419F00` is exact:

```text
playbackRateMultiplier = pow(2.0, pitch / 12.0)
```

It returns `1.0` for zero pitch. Constants are `1/12` (`0x3DAAAAAB`) and double `2.0`; `0x0131E2B0` is the CRT-style x87 `pow` path. Template `+0x60..+0x6C`, runtime state `+0x488`, and external offset `+0x444` are therefore semitones. The result multiplies the sample base rate before the existing 5,000..60,000 Hz clamp.

## Category 13 is a confirmed legacy boundary defect

- `Sound2dTemplate` virtual `+0x4C` (`0x0041CE90`) directly returns field `+0x44`; there is no remap.
- `0x0040C780` indexes `float[category]` from `0x01907F34` without a bounds check.
- `0x0040CB20` initializes exactly indices 0..12 (`esi < 0x0D`).
- `0x01907F68`, read for category 13, is not a gain. It is the first word of a separate three-word STL-like audio container, initialized by `0x0040B820` and used as begin/end/capacity state.
- 558 effective descriptors use category 13, overwhelmingly narrative voice/dialogue, and the traced ordinary voice-volume bridge performs no static remap.

The executable therefore performs an out-of-bounds gain read for category 13. Its value depends on unrelated container state and is neither stable nor a valid 64-bit compatibility contract.

## Replacement policy

- retain all original category values;
- define a bounded 14-entry mixer table;
- map category 13 to a narrative/dialogue bus with default gain 1.0 unless later behavioral testing establishes another intended value;
- log and safely default any larger unknown category;
- preserve the explicit master-gain exemption for category 8.

This corrects a proven original defect; category is asset/runtime state, not a wire-format change.

## Confidence

- Semitone formula, direct accessor, 0..12 bound, and adjacent-container ownership: **confirmed by code**.
- Category-13 dialogue role: **strongly inferred from corpus composition**.
- Intended historic gain for category 13: **unknown**; 1.0 is an explicit safe replacement policy.
