# Block 2 — PEFT/0002 time and spatial scale controls

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

| Description offset | Role | Runtime evidence |
|---:|---|---|
| `+0x28` | initial time scale | selected by `0x007A9060` while elapsed time is at/below `+0x2C` |
| `+0x2C` | time-scale transition time | compared against effect elapsed time at runtime `+0x60` |
| `+0x30` | sustaining time scale | selected after the transition |
| `+0x34` | overall spatial effect scale | consumed by `0x007A90D0`, cached at runtime `+0xD4` |

Time and spatial scale recursively multiply through nested parent effects. The parent pointer is runtime `+0x5C`; instance factor `+0x64` also multiplies effective time scale.

The effective corpus has 1,305 PEFT/0002 records and no parse errors. All have one emitter group. Initial scale has 4 values, transition 8, sustaining scale 18 and spatial scale 22. Constructor defaults are `1.0, 0.0, 1.0, 1.0`.

## Replacement rule

Carry the fields as named float32 values, calculate effective nested scaling recursively or during hierarchy flattening, and keep time scaling separate from spatial scaling.

Confidence: **Confirmed**.
