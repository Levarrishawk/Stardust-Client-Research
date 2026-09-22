# CDF Vehicle Surface and Link Effects Block

Date: 2026-09-21

## Result

This block resolves the `VGEF` vehicle ground/water-contact presentation record and identifies a material compatibility caveat for the later `VLEF` link-effect record found in patched Stardust assets.

## VGEF: vehicle ground effect

`FUN_00606250` recognizes `FORM VGEF`, constructs a 44-byte record through `FUN_007006A0`, loads it through `FUN_00700620`, and appends it to the `ClientDataFile` collection at `+0x64`.

`VGEF/INFO` is:

1. `uint8` water/surface variant flag;
2. optional sound-template path string (present when the flag is nonzero in observed assets);
3. hardpoint/group name string;
4. appearance-template path string.

`FUN_00700620` confirms the byte read, three dynamic strings, and eager resolution of the final appearance name through the appearance-template manager. The record constructor initializes three small-string/vector members, the flag, and a cached appearance handle.

Representative effective assets:

- `air2_swoop_speeder.cdf` dry record: flag `0`, no sound, group `ground_effect`, appearance `appearance/pt_vehicle_dust_trail.prt`.
- water record: flag `1`, `sound/amb_river_large_lp.snd`, the same group, and `appearance/pt_vehicle_water_trail.prt`.
- `balta_podracer.cdf` uses the analogous group `ground_effect_0`.

The two-record pairing strongly indicates environment selection: a vehicle presentation system chooses dust or water trail by the current supporting surface/water state while attaching the effect at the named group/hardpoint. This selection behavior is strongly inferred from the paired data; the loader layout itself is confirmed.

## VLEF: vehicle link effect

Patched vehicle CDFs contain `FORM VLEF/INFO`. Every inspected record consists of exactly three NUL-terminated strings:

1. source hardpoint;
2. destination hardpoint;
3. link appearance, commonly an `.ltn` lightning template.

The effective `clientdata/vehicle/balta_podracer.cdf` contains sixteen directed records pairing `beam_1` through `beam_4` and appearances such as `appearance/podracer_energy_binders_01.ltn` through `_03.ltn`. Reverse-direction pairs are explicitly represented rather than implied.

### Important Stardust.exe observation

The analyzed `Stardust.exe` CLDF dispatcher contains no explicit `VLEF` case. Its unknown-form path enters and exits/skips an unrecognized form, so these records do not corrupt parsing, and the executable build does not consume them through `ClientDataFile`. A subsequent executable-wide immediate/reference audit found no `VLEF` recognizer elsewhere. This is a tolerated extension or patch artifact for this client version, not an unresolved active subsystem.

For a replacement client, preserve forward-compatible unknown-form skipping and retain `VLEF` support as an optional typed extension. Do not make it a launch/networking milestone dependency.

## Replacement-client consequence

The asset layer needs:

- an environment query exposing dry-ground versus water contact;
- hardpoint lookup on the vehicle appearance;
- attachment and lifetime management for particle/link appearances;
- optional looping surface audio;
- tolerance for unknown CDF forms;
- `.ltn` rendering support if/when `VLEF` is enabled.

No part of these records should be widened for a 64-bit process. Parse the original byte/string grammar and materialize native internal handles afterward.

## Confidence

- VGEF loader and INFO fields: confirmed by decompilation and two effective assets.
- VGEF dry/water selection semantics: strongly inferred from paired records and resource names.
- VLEF three-string schema: confirmed from sixteen records in the effective podracer CDF.
- VLEF ignored by this executable's CDF path: confirmed by dispatcher control flow and executable-wide tag-reference absence.
