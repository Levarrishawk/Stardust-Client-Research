# CDF Engine and Thruster Audio Block

Date: 2026-09-21

## Result

This block closes the file-level schemas for the two major data-driven engine-audio records: `INTS` (interpolated sound) and `VSND` (vehicle sound phase set), plus the `ENGS` grouping relationship.

## INTS interpolated-sound record

`FUN_006FC440` constructs a 48-byte record and `FUN_006FC590` loads its `INFO` payload as:

1. group/attachment name string;
2. sound-template path string;
3. six `float32` parameters.

The exact encoding is confirmed. The record constructor defaults two of the floats to `1.0` and the other four to zero.

INTS appears in two contexts:

- a singular top-level `FORM INTS`, retained at `ClientDataFile +0x70`;
- repeated `FORM INTS` children inside `FORM ENGS`, accumulated as 48-byte entries in the collection at `+0x80`.

`ENGS` therefore acts as an engine-sound state/curve group, not a separate leaf schema.

Representative evidence:

- Havoc top-level INTS: group `engine_sound1`, booster loop `sound/shp_booster_rocket_lp.snd`, floats `0.5, 1.0, -16.0, 0.0, 2.0, 1.0`.
- Generic engine ENGS has four INTS entries on `engine_sound_0`: one normal run loop and 25/50/75-percent damage loops. Normal floats are `0.7, 1.0, -16.0, 0.0, 0.3, 0.3`; damaged records use `0.5, 0.8, -16.0, 0.0, 0.3, 0.3`.

This proves damage-stage selection and continuous parameterization are both data-driven. A later runtime trace resolves the six floats as volume endpoints, pitch endpoints, input rise rate, and input fall rate; see `ANALYSIS_CHECKPOINT_2026-09-21_INTS_INTERPOLATED_SOUND_RUNTIME.md`.

## VSND vehicle-sound record

`FUN_007144B0` confirms `VSND` is exactly nine consecutive NUL-terminated strings:

1. attachment/group name;
2. normal idle sound;
3. normal acceleration sound;
4. normal deceleration sound;
5. normal run sound;
6. damaged idle sound;
7. damaged acceleration sound;
8. damaged deceleration sound;
9. damaged run sound.

Empty strings represent absent phases. VTHR damage tiers each own an optional VSND, so the active normalized-damage state selects a complete phase set. Examples demonstrate both ship engine sounds and ground-vehicle sounds using the same schema.

## Architectural model

```text
object/ship state
  -> normalized damage tier selects VTHR
       -> attached thruster appearances (HOBJ)
       -> VSND normal/damaged idle-accel-decel-run set
  -> component/booster behavior selects INTS or ENGS entry
       -> continuous six-float sound parameterization
  -> audio runtime creates/updates/stops loop instances
```

`VSND` is discrete phase selection; `INTS` supplies a continuous/interpolated sound description. They should remain separate typed records in a replacement client even when backed by the same modern audio engine.

## Replacement-client consequence

Original asset compatibility requires an audio controller that supports:

- named attachment/group routing;
- idle, acceleration, deceleration, and running phases;
- normalized damage-state switching;
- continuous parameter interpolation without restarting loops unnecessarily;
- empty/missing sound phases;
- deterministic cleanup during object destruction, scene transitions, and component replacement.

Parse all six INTS floats and nine VSND strings verbatim. The INTS fields are now established as `volume0`, `volume1`, `pitch0`, `pitch1`, `riseRate`, and `fallRate`; preserve pitch values at the audio-adapter boundary until equivalent units are calibrated.

## Confidence

- INTS/ENGS storage, string order, and float count: confirmed.
- VSND nine-string order: confirmed by decompilation and asset comparisons.
- Damage-tier/phase relationship: confirmed structurally and strongly supported by resource names.
- INTS float semantics and interpolation equations: confirmed; exact historic member spelling and pitch units remain unknown.
