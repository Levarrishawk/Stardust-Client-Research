# CDF Animated Wing Attachment Block

Date: 2026-09-21

## Result

This block reconstructs the `WING` record that drives movable/animated ship attachments such as X-wing S-foils.

## Loader and storage

`FUN_00606250` recognizes repeated `FORM WING` records, constructs a large owned record, parses `DATA`, parses one placement chunk, and appends the record to the `ClientDataFile` collection at `+0x20`.

### DATA layout

1. object-template path string, resolved through the object-template manager;
2. `float32` target/open angle, converted to the engine's internal angular units;
3. `float32` transition duration in seconds;
4. sound-template path string, resolved through the sound-template manager when nonempty.

### Placement

Exactly one placement source follows:

- `PSOR`: position vector and three angular floats; or
- `HARD`: hardpoint name.

This reuses the same explicit-transform-versus-hardpoint convention seen in DAMA and ONOF.

## Effective X-wing evidence

The effective `client_shared_xwing.cdf` defines two WING records:

- positive wing attachment `shared_xwing_wing_pos_s01.iff`, target angle `-14.0`, scalar `3.0`, open sound `sound/wings_open_xwing.snd`, identity PSOR transform;
- negative wing attachment `shared_xwing_wing_neg_s01.iff`, target angle `+14.0`, scalar `3.0`, no sound, identity PSOR transform.

Other corpus users include the advanced/player X-wing, ARC-170, B-wing, Jedi starfighter, V-wing, and B-wing component data. There are 19 WING records across eight effective CDFs.

The paired positive/negative records confirm coordinated symmetric motion around the parent ship. A single side owns the sound to avoid duplicate playback.

## Replacement-client consequence

The ship presentation layer needs a data-driven deployable-attachment controller:

1. instantiate the referenced wing object template as a child;
2. attach via explicit transform or hardpoint;
3. interpolate from closed to configured target angle over the configured duration;
4. play the optional transition sound once per coordinated action;
5. expose state changes to the network/gameplay ship component layer without coupling wire data to renderer nodes.

The original angle is a 32-bit degree value on disk and should be converted to radians (or the modern engine's unit) only after parsing. The second float is confirmed as transition duration: the controller accumulates frame delta, clamps at this value, and computes `elapsed / duration` before linearly interpolating between zero and the configured angle.

## Confidence

- DATA field order, resource resolution, and placement grammar: confirmed.
- Symmetric wing/S-foil purpose: confirmed by object/resource names and values.
- Transition duration and interpolation equation: confirmed by controller `0x006FFB30`.
