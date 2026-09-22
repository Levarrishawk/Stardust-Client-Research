# EMTR flocking, cage, and environment-color controls

Date: 2026-09-13  
Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

The late `EMTR` control group is now structurally decoded across versions 0010-0014 and connected to live simulation/render-preparation code. It implements flocking forces, a confinement cage, terrain-environment color influence, and two dormant compatibility floats. The result combines loader assignments, runtime arithmetic, the established particle class boundary, and a zero-error scan of 6,364 effective modern emitter records.

## Loader evolution

The three booleans after the optional sound string are followed by version-sensitive data:

| Versions | Stable portion after booleans | Flocking representation |
|---|---|---|
| 0009-0011 | nine legacy dwords | 0010/0011 append one byte plus seven floats |
| 0012-0014 | eight legacy dwords | twelve dwords, two booleans, then three floats |

This corrects the earlier scanner assumption that the first 39 tail bytes were common through 0014. In 0012 the ninth legacy dword disappears and flocking mode becomes a full dword. The corrected scanner uses a 39-byte extension boundary only for 0009-0011 and a 35-byte boundary for 0012-0014.

The old 0010/0011 form stores a boolean enable, separation distance, alignment/cohesion/separation gains, cage width, and lower/upper vertical limits. Loaders derive `+0x200 = upper - lower`. The 0012 form stores the resulting cage height directly and adds explicit cage enums and later controls.

## Runtime member map

| Legacy member | Provisional semantic | Confidence | Evidence |
|---|---|---|---|
| `+0x1E8` | flocking mode | Confirmed | Gates aggregate calculation and force/cage branches; corpus values 0-3 |
| `+0x1EC` | separation distance | Confirmed | Pairwise distance threshold in the `0x007A53C4` force path |
| `+0x1F0` | separation gain | Confirmed | Scales repulsive pair force |
| `+0x1F4` | alignment gain | Confirmed | Gates average velocity and scales alignment force |
| `+0x1F8` | cohesion gain | Confirmed | Gates average position and scales center-seeking force |
| `+0x1FC` | cage width | Confirmed | Horizontal confinement extent |
| `+0x200` | cage height | Confirmed | Vertical extent; derived from old min/max in 0010/0011 |
| `+0x204` | cage shape | Confirmed as role | Enum used by confinement geometry; deployed values are all 0 |
| `+0x208` | cage edge behavior | Confirmed as role | Selects hard/reflection versus smoothed edge response; values 0/1 |
| `+0x20C`, `+0x210` | reserved compatibility floats | Confirmed dormant in deployed corpus | Both are zero in every 5,562 effective 0012-0014 record and have no simulation/render consumer, only construction/copy/load/save references |
| `+0x214` | terrain/environment color influence | Confirmed | `0x007A2660` blends evaluated particle RGB toward `TerrainObject` environment color by this factor |

`+0x214` is not velocity inheritance. `0x007A2660` starts with three normalized color bytes obtained through the `TerrainObject` singleton and its appearance virtual slot `+0x12C`. Per-particle `CLRR` output is linearly interpolated toward those three environmental components by `+0x214` before vertex preparation. The effective corpus uses values primarily in `[0,1]`: 0011 has 503 zeros, 117 at 0.5, and 54 at 0.75; 0014 has 3,443 zeros and 239 nonzero records.

## Effective-asset evidence

- Flocking is uncommon but real: 76/1,039 v0012 and 68/3,682 v0014 emitters use a nonzero mode.
- All four mode values 0-3 occur in both v0012 and v0014.
- Cage edge behavior 1 occurs in 14 v0012 and 38 v0014 records.
- Cage shape is 0 in all deployed 0012-0014 records. The alternate executable branch must still be implemented for format fidelity.
- Terrain/environment color influence is nonzero in 214 v0012, 13 v0013, and 239 v0014 records, so it is not safely optional.
- `+0x20C` and `+0x210` are serialized but zero throughout every deployed record where present. Their editor names and historical use remain unknown.

Full per-record data is retained in `emtr_flocking_fields.csv`; the corrected raw inventory is `emtr_scalar_fields_corrected.csv`.

## Replacement contract

The new client should convert all versions into an engine-neutral structure with explicit fields for flocking mode and gains, cage dimensions/shape/edge policy, and environment-color influence. Old cage limits must be normalized exactly to `height = max - min`. The two dormant floats should be retained as opaque serialized compatibility values, not exposed as active behavior until evidence appears. Simulation owns flocking and cage response; render preparation applies the environment-color blend to evaluated particle color.
