# CLEF `CAMS` camera-shake field order and runtime

Evening follow-up (2026-09-22): the second authored CAMS scalar is copied to
camera `+0x224` by `0x007886B0`, but `0x00788740`'s traced per-frame shake
update reads duration at `+0x218` and amplitude at `+0x220`, not `+0x224`.
A bounded disassembly scan found no direct `+0x224` reads in the camera-family
`0x00780000..0x0078FFFF` range; similarly sized offsets elsewhere belong to
other classes. This narrows the unknown but does **not** prove the field is
globally inert (aliased/derived consumers remain possible). Keep it serialized
under a neutral name until a final consumer or explicit absence is established.

Date: 2026-09-22. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Code evidence is reconstructed pseudocode plus direct x86 operand checks.

## Bounded result

The three CLEF version readers store each `CAMS` leaf as four little-endian F32 values. Both effect-instance execution methods (`0x005F05B0` and `0x005F0B70`) pass the **first** camera record to `0x005F0350`. The helper checks camera availability and the active `GameCamera`, computes squared world distance to the effect, and calls `0x007886B0` only when inside the authored radius. It passes a distance factor `1 - distance²/radius²`, multiplied by the first record field. **Confirmed by code.**

| CAMS byte offset | Consumer evidence | Interpretation/confidence |
|---|---|---|
| `+0` | Multiplies distance factor; stored at camera `+0x220` | Shake angular magnitude, **confirmed** by later transform rotations |
| `+4` | Forwarded unchanged and stored at camera `+0x224` | Meaning **unknown**; no read found in the bounded `0x00787000..0x0078A000` camera code scan |
| `+8` | Stored at camera `+0x218`; per-frame `0x00788740` compares accumulated time at `+0x21C` against it | Duration threshold, **confirmed** |
| `+12` | Squared for camera-to-effect distance gate and distance attenuation | Spatial radius, **confirmed** |

`0x00788740` reduces the active magnitude with remaining-time fraction and draws a random value between approximately `-0.5` and `+0.5` times that magnitude. It applies two independent angular rotations via `0x00AB8E10` and `0x00AB8EA0`, both of which use sine/cosine to rotate transform basis vectors. It stops the active shake at the duration threshold. This is an angular camera perturbation, not just an arbitrary screen-space translation. Exact frame timing, random generator properties, and the `+4` field remain open.

The corrected overlay contains **279 CAMS records in 279 distinct live CEFs**; no file contains more than one. All four fields parse as finite F32 in this corpus. Radius spans `1..1500`, with **zero zero or negative radii**. The first magnitude spans approximately `0.001..1`, and duration threshold `0.25..135`. The original code's division by squared radius has no observed zero-radius corpus trigger; malformed or future assets still need an explicit policy in a new client.

Evidence: CLEF readers at `0x00698560/0x00698B70/0x00699100`, execution methods, `0x005F0350 → 0x007886B0 → 0x00788740`, transform helpers, and full effective-CLEF corpus scanner `tools/python/cef_cams_corpus.py`. The private per-asset JSON stays under `local-private/`. Three new stable names. No packet-map change; planning estimates remain 96% implementation-grade / 90% comprehensive.
