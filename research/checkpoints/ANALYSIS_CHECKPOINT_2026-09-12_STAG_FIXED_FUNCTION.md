# Analysis Checkpoint: STAG Fixed-Function Contract

Date: 2026-09-12

## Scope

This block followed the shader texture-stage (`STAG`) representation from the executable loader into the deployed `s207_r.dll` raster backend, then validated the representation against the effective configured asset overlay. It did not attempt to emulate the renderer or assign unsupported semantics to unnamed fields.

## Confirmed findings

- `Stardust.exe` VA `0x00762CA0` loads `STAG/0000` as exactly 33 serialized bytes. Runtime offsets `+0x00..+0x44` receive 18 byte-sized scalar values, `+0x3C` and `+0x40` receive two 32-bit tags, `+0x44..+0x58` receive six more byte-sized values, and `+0x5C` receives the final byte.
- VA `0x00762F10` loads legacy `STAG/0001` as the same first 18 bytes plus the final `+0x5C` byte, for 19 bytes total. The two tags and six overrides retain constructor defaults.
- VA `0x00762BA0` supplies stable sentinels: address-like fields `+0x44/+0x48/+0x4C` default to 5 and filter-like fields `+0x50/+0x54/+0x58` default to 6.
- The effective configured overlay contains 951 `STAG/0000` records, all 33 bytes, representing 128 unique payloads. No effective `STAG/0001` record was found.
- `s207_r.dll` VA `0x62A0C2C0` initializes cached Direct3D 9 texture-stage states, including D3DTSS IDs 1-11, 22-24, 26-28 and 32, plus sampler defaults.
- VA `0x62A0B230` applies cached `(D3DTSS, value)` pairs through the Direct3D device `SetTextureStageState` slot.
- The export/xref inventory contains no code or data reference to `0x62A0B230`, and a raw stored-pointer scan found no pointer to it in the DLL.
- The recovered active implementation factory at `0x62A04D40` constructs the `0x62A0BAA0` pass compiler. Its pass build route reaches `0x62A0B340`, which consumes a programmable pixel-shader reference.

## Strong inferences

- The first 18 serialized bytes are ordered like the classic D3D texture-stage combiner contract: color operation and arguments/flags, alpha operation and arguments/flags, then result argument.
- Operation values are most consistently explained as `D3DTEXTUREOP - 1`; observed values correspond to familiar operations such as SELECTARG1, SELECTARG2, MODULATE, MODULATE2X, ADD and alpha blends. This is a format inference, not proof of a presently reachable translation table.
- The two 32-bit values are parameter/coordinate-selection tags. The six override bytes are U/V/W addressing and three texture filters, although the exact tag roles and min/mag/mip order remain to be proved.
- The D3DTSS route is retained legacy/fallback code in this deployed raster DLL, while the recovered normal pass route is programmable. A replacement should compile STAG into a renderer-neutral fragment expression rather than reproduce the D3D9 state calls as its architecture.

## Corpus evidence

Representative distributions over all 951 effective records:

- First operation byte: 3 (640), 1 (143), 12 (75), 15 (39), 6 (31), 2 (20), 4 (2), 13 (1).
- First color argument byte: always 0.
- Second color argument byte: 4 (670), 5 (155), 0 (124), 1 (2).
- Alpha operation byte: 1 (529), 3 (421), 13 (1).
- First tag: `MAIN` (522), zero (106), `SPEC` (60), `HUEB` (41), `DETA` (34), `ENVM` (29), plus a long tail.
- Second tag: `MAIN` (731), zero (106), plus a small tail.
- U/V addressing overrides: sentinel 5 (528), value 0 (395), value 2 (28).
- Filter-like overrides: sentinel 6 (528), otherwise values 0, 1 or 2.
- Final byte: 0 (907), 3 (34), 4 (7), 5 (3).

## Unknowns carried forward

1. Exact names and roles of the two tags at runtime offsets `+0x3C/+0x40`.
2. Exact minification/magnification/mipmap ordering of `+0x50/+0x54/+0x58`.
3. Exact semantic meaning of the final byte at `+0x5C`.
4. Whether another unobserved factory/configuration route can activate `0x62A0B230`.
5. Exact enum-to-D3D translation tables for every combiner argument flag and result selector.

## Architectural consequence

Original shader assets remain a non-negotiable compatibility input. The future importer should preserve the serialized STAG record, normalize it into a typed intermediate representation, and emit equivalent fragment-shader logic for the selected modern renderer. It should retain a diagnostic mode that prints the original fields and chosen interpretation so corpus edge cases can be corrected without changing the archive decoder.

## Progress

- Comprehensive weighted analysis: approximately 51.1%, reported as 51%.
- Replacement-architecture readiness: approximately 69%.
- Asset workstream: 68%.
- Renderer workstream: 36%.

These figures measure durable subsystem research, not the percentage of raw functions assigned names.
