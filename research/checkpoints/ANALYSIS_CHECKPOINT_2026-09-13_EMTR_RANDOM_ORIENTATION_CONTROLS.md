# Block 4 — EMTR randomization and orientation controls

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

- `+0x114` enables randomized initialization of three per-particle scalar seeds at particle `+0x94/+0x98/+0x9C`. Disabled uses 1.0. A particle-description predicate can independently negate each seed, supporting symmetric authored variation. It is false in 4,584 and true in 1,780 modern emitters.
- `+0x118` is a three-way particle orientation/reference-frame mode. Modes 0, 1 and 2 select distinct branches in spawn and simulation; mode 1 explicitly constructs a cross-product basis and has additional reference/velocity alignment behavior. Serialized value 3 normalizes to runtime mode 2. Deployed serialized counts are 4,381/991/991/1 for values 0/1/2/3.
- `+0x11C` is true in all 6,364 modern emitters. No direct runtime read occurs outside initialization/copy/load/save paths. It is a compatibility Boolean with no demonstrated deployed behavioral distinction.

The exact editor labels for the orientation modes remain unknown. Preserve the enum, implement three separate reference-frame branches, and retain `+0x11C` as metadata without inventing behavior.

Confidence: **Confirmed** for consumers/distributions; **Strong** for broad orientation/reference-frame wording.
