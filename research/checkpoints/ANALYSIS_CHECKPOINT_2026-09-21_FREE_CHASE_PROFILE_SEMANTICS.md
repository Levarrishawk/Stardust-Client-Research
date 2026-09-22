# FreeChase camera profile semantics

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Asset: `camera/freechasecamera.iff` from the configured overlay

## Result

The four previously unnamed `FCHS/0000` chunks now have replacement-relevant semantics. Their four-character tags can be retained verbatim; the behavioral names below are evidence-based provisional labels.

| Chunk | Payload | Effective value(s) | Behavior |
|---|---|---|---|
| `FRST` | F32 | `0.5` | first-person transition threshold as a multiplier of the watched appearance's height/radius metric |
| `DEFH` | F32 | `1.7` | default vertical target-height multiplier; loaded into vector `(0, value, 0)` |
| `PSHM` | repeated `(S32 posture, S32 state, F32 multiplier)` | `(8,19,1.1)`, `(0,27,0.7)` | posture-plus-active-state target-height override |
| `PSTM` | repeated `(S32 posture, F32 multiplier)` | postures `1:1.2`, `2:0.35`, `7:0.35`, `8:0.7`, `12:0.35`, `13:0.35`, `14:0.35` | posture-only target-height multiplier |

## Consumer proof

`0x006D6660` reads the watched creature's posture byte and current state bits. It searches the `PSHM` map first, matching the high half of the packed key to posture and testing the low-byte state index through the creature-state predicate. If no pair matches, it looks up the posture in `PSTM`; if that also misses it uses the `DEFH` vector. The selected vector is multiplied by the watched appearance metric at `+0x734`, optional mount/rider displacement is added, and the camera target offset is smoothed toward the result.

The loader packs `PSHM` as `(posture << 16) | state`. This resolves the apparent range discrepancy: posture is constrained to `[0,14]`, while the state selector is constrained to `[0,33]`.

The per-frame camera collision branch compares zoom distance with `FRST * watchedAppearanceMetric`. At or below that threshold it enters the first-person/near-camera branch; beyond it the ordinary chase-camera collision and offset solution is used.

## Replacement contract

- Preserve the profile as original asset data; do not hard-code these values into a Godot or renderer camera.
- Resolve `(posture,state)` overrides before posture-only values, then `DEFH`.
- Smooth target-offset changes so posture/state transitions do not snap the camera.
- Treat first-person selection as camera-controller state, independent of the graphics backend.
- Retain posture and state as fixed protocol/gameplay enum widths; host pointer size is irrelevant.

## Confidence

- Chunk payloads and effective values: **Confirmed by original asset bytes**.
- `PSHM` packed-key order and override precedence: **Confirmed by loader and `0x006D6660`**.
- `PSTM` posture role, `DEFH` vertical fallback and `FRST` near/first-person threshold: **Confirmed by consumer mechanics and value distribution**.
- Exact unpublished editor-facing expansions of the four tags: **Unknown and non-material**.
