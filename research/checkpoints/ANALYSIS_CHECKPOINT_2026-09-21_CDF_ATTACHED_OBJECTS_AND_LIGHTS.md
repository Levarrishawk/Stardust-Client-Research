# CDF Attached Objects, Lights, and Collision Configuration Block

Date: 2026-09-21

## Result

This block resolves the common hardpoint-object records and the binary light-object family used by buildings, lamps, holograms, and presentation controllers.

## HOBJ hardpoint object

`FUN_00700A20` constructs a 28-byte record. `FUN_00700AB0` loads:

1. appearance-template path string;
2. hardpoint name string;
3. eagerly resolved appearance-template handle.

Repeated top-level `HOBJ` records are stored at `ClientDataFile +0x24`. The shuttleport example attaches five yellow blinking particle appearances and one blue appearance to `hardpoint1` through `hardpoint6`.

This same two-string subrecord is reused inside VTHR, proving it is a generic appearance-to-hardpoint attachment primitive.

## IHOB interpolated hardpoint object

`FUN_00700850` constructs a 40-byte record and `FUN_007008F0` loads:

1. appearance-template path string (eagerly resolved);
2. hardpoint name string;
3. three `float32` parameters.

Repeated `IHOB` records are stored at `ClientDataFile +0x28`. A later RTTI and firing-path trace identifies the controller as `ShipWeaponBarrelDynamics` and the floats as maximum recoil distance, recoil speed, and return speed; see `ANALYSIS_CHECKPOINT_2026-09-21_IHOB_SHIP_WEAPON_BARREL_DYNAMICS.md`.

## LOBJ light object

`LOBJ` constructs a polymorphic light record through `FUN_00609110`, loads its base fields through `FUN_00609470`, then reads one final vector3. The on-disk payload is:

1. 32-bit light type/enable value;
2. optional name/template string, eagerly resolved when nonempty;
3. three `float32` values;
4. ten more `float32` values;
5. 32-bit boolean;
6. final vector3 (`3 x float32`).

The constructor supplies two internal defaults of `1.0` that are not present on disk. The complete runtime base record is at least 88 bytes plus vtable/derived state. `LOBJ` entries are stored at `ClientDataFile +0x2C`.

This record family is confirmed to represent runtime lights by the resource corpus, class-like vtable construction, and live `Light` creation. A later trace resolves type 1 as a static point light and type 2 as a randomized point-flicker light, including all thirteen float slots; see `ANALYSIS_CHECKPOINT_2026-09-21_CDF_POINT_LIGHTS_AND_FLICKER.md`.

## HLOB hardpoint light object

`HLOB` uses the same light base constructor and loader, then appends a hardpoint-name string. It is stored at `ClientDataFile +0x30`.

The shuttleport example contains:

- light enabled/type value `1`;
- particle appearance-like reference `appearance/pt_light_blink_green.prt` in the base resource slot;
- light numeric parameters;
- hardpoint-enabled value `1` and hardpoint name `hardpoint`.

This confirms HLOB is the hardpoint-attached specialization of LOBJ.

## CNCF configuration

`CNCF` is a compact 16-byte clear-flora record: object-local center vector3 plus radius `float32`, accumulated at `ClientDataFile +0x44`. The shuttleport example is `(0,0,0,29.0)`. A later dedicated trace proves that these records are registered with the terrain/world system over object lifetime; see `ANALYSIS_CHECKPOINT_2026-09-21_CDF_CLEAR_FLORA_CONFIGURATION.md`.

## Replacement-client consequence

The asset runtime needs a generic attachment stage after the parent appearance is available:

```text
ClientDataFile
  -> resolve parent hardpoint transform
  -> instantiate appearance/particle/light resource
  -> parent child object with explicit ownership
  -> update derived/interpolated state where configured
  -> remove children before parent/scene destruction
```

Modern renderer light objects may use a wholly new internal layout, but the original LOBJ/HLOB scalar stream must be parsed without widening or reinterpreting fields prematurely. Neutral field names are preferable until additional runtime consumers prove color, range, cone, attenuation, and shadow semantics.

## Confidence

- HOBJ/IHOB string layouts and IHOB recoil dynamics semantics: confirmed.
- LOBJ/HLOB byte grammar, inheritance relationship, and light purpose: confirmed.
- Static point-light and point-flicker numeric fields and the final day/night-controlled flag behavior: confirmed; exact historic member spelling remains unknown.
- CNCF center-plus-radius layout and clear-flora semantics: confirmed.
