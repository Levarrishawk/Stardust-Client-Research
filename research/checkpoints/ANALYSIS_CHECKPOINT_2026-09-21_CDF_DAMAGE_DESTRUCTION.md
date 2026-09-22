# ClientDataFile Damage, Destruction, Breakpoint, and Thruster Block

Date: 2026-09-21

Scope: static analysis of `Stardust.exe` and effective CDF assets only. Decompiled output is reconstructed pseudocode, not original source.

## Result

This block closes the principal runtime meanings and on-disk layouts of four high-value `ClientDataFile` (`CLDF/0000`) record families: `DAMA`, `DSTR`, `BRKP`, and `VTHR`. These records are presentation metadata selected by normalized object damage state. They are not network structures and must retain their original 32-bit file encodings in a replacement client.

## Confirmed loader routing

`FUN_00606250` is the large CLDF record dispatcher/loader. It recognizes, constructs, and stores the following records:

| Tag | Loader path | Destination in ClientDataFile | Meaning | Confidence |
|---|---|---:|---|---|
| `DAMA` | inline in `00606250` | pointer-vector at `+0x48` | damage-level presentation record | Confirmed |
| `BRKP` | ctor `007010A0`, load `00701290` | container at `+0x74` | procedural destruction breakpoint | Confirmed |
| `VTHR` | ctor `007002D0`, load `00700110` | container at `+0x60` | vehicle/ship thruster state | Confirmed |
| `DSTR` | inline in `00606250` | single handle at `+0x78` | final destruction client effect | Confirmed |

Duplicate `DSTR` records are rejected by the loader because `+0x78` is singular. `DAMA`, `BRKP`, and `VTHR` are accumulated.

## DAMA layout and behavior

Observed nested chunks:

- `INFO`: two little-endian `float32` thresholds followed by an optional NUL-terminated client-effect name.
- `APPR`: NUL-terminated appearance path, or an empty string.
- Exactly one placement source:
  - `PSOR`: six little-endian `float32` values. The loader reads a position vector and three angular values; angles are converted to the engine's internal angular units.
  - `HARD`: NUL-terminated hardpoint name.
- `ASND`: NUL-terminated sound-template path, or an empty string.

The first two floats are lower and upper normalized-damage bounds. Representative ground-object tiers are `[0.0, 0.25]`, `[0.25, 0.5]`, `[0.5, 0.75]`, and `[0.75, 1.0]`. The gunboat sample uses damage thresholds near 0.26/0.45, 0.46/0.65, 0.66/0.85, and 0.86/0.99.

`FUN_00605BB0` clamps old/new damage fractions to `[0,1]`, finds the corresponding ordered damage records, traverses every crossed tier in the correct direction, applies an entering tier through `FUN_00605EA0`, removes a leaving tier through the complementary cleanup path, stores the selected tier index, and refreshes the object state. This proves transitions are stateful and must process skipped intermediate tiers rather than selecting only the final tier.

`FUN_00605EA0` applies a record by creating either a normal appearance object or particle-effect object, attaching it by hardpoint or explicit transform, starting the optional sound, and invoking the optional client effect. Runtime child objects receive stable names of the form `damage_<index>` for later cleanup.

## DSTR layout and behavior

`DSTR/INFO` is one NUL-terminated client-effect path. Examples include:

- `clienteffect/cbt_explode_asteroid_gas_large.cef`
- `clienteffect/cbt_explode_yt1300.cef`

The loader resolves the name through the client-effect template manager and retains a single handle. Load failure emits the embedded `ClientDataFile ... failed to load` diagnostic. This is final-destruction presentation, distinct from the ordered `DAMA` overlays.

## BRKP layout and behavior

`BRKP/INFO` is a fixed semantic structure loaded by `FUN_00701290`:

1. four NUL-terminated strings;
2. base position vector (`3 x float32`);
3. absolute position-randomization extents (`3 x float32`);
4. angular-randomization vector (`3 x float32`, converted to internal angular units);
5. one trailing `float32` probability/timing value.

The representative gunboat records use names such as `break_req_01`, `break_01` through `break_04`, with empty optional strings and trailing values `1.0` or `0.7`.

`FUN_00609E90` is the breakpoint/destruction-sequence evaluator. It filters candidate records by existing state and a time/probability test, resolves a named hardpoint when supplied, otherwise uses the record's explicit transform, randomizes translation and rotation within stored extents, and instantiates the associated break presentation. Missing hardpoints produce the diagnostic `ClientDataFile ... breakpoint hardpoint ...`.

`FUN_006FC950` is a destruction-sequence object/effect attachment helper: it creates or obtains an object, attaches it to the requested hardpoint transform when available, and logs the embedded `ClientDataFile::DestructionSequence...` warning when the hardpoint cannot be resolved.

## VTHR layout and behavior

Each `VTHR` record contains:

- `INFO`: one little-endian `float32` normalized damage threshold;
- zero or more `HOBJ`: two NUL-terminated strings, appearance path then hardpoint name;
- optional `VSND`: a sound-group name followed by either four or eight NUL-terminated sound-template paths.

The gunboat sample has four VTHR states at `0.0`, `0.25`, `0.5`, and `0.75`. Each state attaches thruster particle appearances to `exhaust1` through `exhaust3`. Higher states replace selected normal thrusters with `pt_space_burning_thruster.prt`. The sound payload begins with a group key (`engine_sound1`), then idle/accel/decel/run sounds; damaged tiers append damage-specific variants for the same four phases.

`FUN_00700110` confirms this layout: it reads `INFO`, repeatedly constructs 28-byte hardpoint-object records for every `HOBJ`, then loads an optional `VSND` through the vehicle-sound loader. `FUN_007002D0` constructs the enclosing state with default threshold `1.0`, a dynamic HOBJ vector, and four sound-state collections.

## Related CONT record

The same ship sample confirms `CONT/0000/INFO` stores:

1. source hardpoint string;
2. contrail child/object name string;
3. a one-byte flag;
4. appearance path string;
5. a one-byte flag;
6. one trailing `float32` width/scale parameter.

`FUN_00700DE0` validates the nested version as `0000` and dispatches to `FUN_00700E80`; invalid versions produce the embedded contrail-version warning.

## Effective-asset evidence

| Effective member | Archive | Key structure |
|---|---|---|
| `clientdata/client_shared_antenna_tatt_style_1.cdf` | `mtg_patch_012_configurable_01.tre` | 4 `DAMA` tiers with appearance, transform/hardpoint placement, sound, and optional client effect |
| `clientdata/ship/asteroid/mining_asteroid_acid_large.cdf` | `mtg_patch_012_configurable_01.tre` | singular `DSTR` effect |
| `clientdata/ship/client_shared_gunboat_rebel_player.cdf` | `mtg_patch_017.tre` | 5 `BRKP`, 5 `DAMA`, 2 `CONT`, 4 `VTHR`, and 1 `DSTR` |

Corpus prevalence from the effective CDF inventory: `DAMA` 1,225 instances in 300 files; `DSTR` 221 in 218 files; `BRKP` 511 in 166 files; `VTHR` 403 in 107 files.

## Replacement-client consequence

A compatible loader should preserve the original CLDF chunk grammar and numeric widths, but translate records immediately into typed, 64-bit-safe internal objects. Damage fraction is a presentation selector. Network baselines/deltas update object condition; the object presentation layer then crosses DAMA/VTHR thresholds and activates/deactivates assets and sound. Destruction must be a separate terminal effect path. Breakpoint processing needs hardpoint lookup, deterministic object ownership, random transform ranges, and cleanup, but does not require duplicating the original allocation layout.

## Remaining uncertainty

- Precise names/meanings of all four BRKP strings and the trailing scalar require more sample correlation or original asset-tool documentation.
- Exact VSND interpolation/crossfade policy remains to be traced in its runtime consumer.
- The complete destruction-sequence grammar outside the simple DSTR client-effect reference remains a separate block.

