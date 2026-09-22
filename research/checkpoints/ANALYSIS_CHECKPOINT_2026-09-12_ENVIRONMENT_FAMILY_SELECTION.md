# Environment-family selection and visual descriptor checkpoint — 2026-09-12

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes the previously open algorithm inside `0x0085D7F0` and reconstructs the 25-column outdoor environment descriptor that it returns. Position does not select an arbitrary datatable row directly. The terrain chunk supplies a one-byte environment-family key, the PTAT family table resolves that key to a family ID/name, and the current server weather index selects a row within that family. A descriptor change starts a fixed five-second visual transition.

## Selection chain

```text
player terrain position
  -> terrain chunk lookup
  -> ClientChunk byte grid +0xD4
  -> PTAT environment-family ID
  -> current ServerWeatherMessage index
  -> key = (familyId << 16) | weatherIndex
  -> datatables/environment/<terrainName>.iff descriptor map
  -> current/previous GroundEnvironment descriptors
  -> five-second visual interpolation
```

`0x0084ADF0` resolves the chunk containing the supplied position and calls `0x008628E0`. The latter converts position to chunk-grid coordinates, reads one byte from ClientChunk grid `+0xD4`, and resolves it through the terrain family-group table. It fails explicitly if the byte has no corresponding family.

## Exact lookup and fallback

Selector `0x0085D7F0` receives `familyId` and `weatherIndex` and searches an ordered map using the 32-bit key:

```text
(familyId << 16) | weatherIndex
```

Its fallback order is:

1. exact family/weather pair;
2. if weather is nonzero, the same family with weather zero;
3. the manager's empty/default descriptor at member `+0x08`.

There is no fallback to another named family such as `global`. `global` is an ordinary PTAT family selected by terrain-family data. Loader `0x0085D880` validates each datatable family name against the PTAT family table, obtains the numeric ID, builds the composite key, and inserts one descriptor. Unknown family names are skipped.

## Outdoor datatable contract

The effective Tatooine `datatables/environment/tatooine.iff` is `DTII/0001` and contains exactly the 25 columns read by `0x0085D880`:

| Index | Column | Type | Descriptor role |
|---:|---|---|---|
| 0 | Environment Family Name | string | validated against PTAT; converted to numeric family ID |
| 1 | Weather Index | integer | low 16 bits of lookup key in ordinary deployed values |
| 2 | Gradient Sky Texture (256x256 dds) | string | creates/caches gradient-sky texture resource |
| 3 | Cloud Layer Bottom Shader (sht) | string | bottom cloud-layer material |
| 4 | Cloud Layer Bottom Shader Size | float | bottom cloud spatial scale |
| 5 | Cloud Layer Bottom Speed | float | bottom cloud motion speed |
| 6 | Cloud Layer Top Shader (sht) | string | top cloud-layer material |
| 7 | Cloud Layer Top Shader Size | float | top cloud spatial scale |
| 8 | Cloud Layer Top Speed | float | top cloud motion speed |
| 9 | Lighting Color Ramps (256x8 tga) | string | decoded into eight ordered time-indexed ramp channels |
| 10 | Shadows Enabled | integer/bool | shadow-state input |
| 11 | Fog Enabled | integer/bool | fog-state input |
| 12 | Minimum Fog Density | float | fog-density endpoint |
| 13 | Maximum Fog Density | float | fog-density endpoint |
| 14 | Attached Camera AppearanceTemplate | string | optional weather/atmospheric appearance attached to camera |
| 15 | Day Environment Map (dds) | string | day reflection/environment texture |
| 16 | Night Environment Map (dds) | string | night reflection/environment texture |
| 17–23 | day/night ambient 1/2, first, sunrise, sunset sound paths | strings | adaptive audio policy already mapped |
| 24 | Wind Speed Scale | float | descriptor-specific wind multiplier |

The loader reads each field explicitly. No packed file struct exists.

### Color-ramp decoding

`0x008510F0` loads the named 256×8 image and iterates all 256 time samples. Rows 0–5 become six RGB arrays plus six separately normalized alpha/scalar arrays. Row 6 becomes another RGB plus scalar pair. Row 7 becomes an RGBA float array. GroundEnvironment blends the corresponding arrays from previous and current descriptors, indexed by normalized cycle time. Subsequent consumer closure identified all behavioral channels: ambient plus repeated-CELS opacity; main primary and secondary light; fill; bounce; fallback sky plus principal sun/moon intensity; fog plus sky/cloud/stars opacity; and shadow-volume RGBA. See the ramp and ENVM-CELS checkpoints.

If the image cannot be loaded or fails the expected form/size check, the function fills deterministic fallback colors/scalars rather than leaving the descriptor uninitialized.

## Descriptor change behavior

GroundEnvironment stores current descriptor at `+0x0C` and previous descriptor at `+0x10`. At `0x008485DE`, a pointer change moves current to previous, installs the new descriptor, replaces sky and bottom/top cloud appearances, selects the day or night environment map, and handles the optional camera-attached appearance.

It then writes `5.0f` to transition duration `+0x14`, resets transition elapsed time, and sets active byte `+0x1C`. Each update advances elapsed time and invokes `0x00846D00` with `elapsed / 5.0`; the latter interpolates descriptor color/scalar channels into active lighting, fog, cloud and related environment objects. Once elapsed reaches the duration, the transition is disabled. The visual blend duration is therefore **Confirmed as five seconds**.

Sky/cloud objects are recreated when their resource identity changes, while the shared environment state is interpolated between descriptor data. Empty cloud paths yield no cloud appearance. The optional attached-camera appearance is removed/stopped from the previous descriptor and installed for the current descriptor according to the existing world/interior placement checks.

## Effective installed corpus

The configured overlay contains 50 effective `datatables/environment/*.iff` members. All parse successfully as 25-column DTII tables:

- 1,337 total rows;
- 290 named family groups across tables;
- every family has a weather-zero row, making the selector's same-family clear-weather fallback usable;
- most ground tables use weather indices 0–4;
- `09.iff` contains 0–3 and `mustafar.iff` contains 0–2;
- single-row space/dungeon/tutorial tables use weather zero only.

The exact inventory, overlay winner, row/family counts and weather sets are in `effective_environment_tables.tsv`.

Tatooine is a complete matrix: 17 families × weather indices 0–4 = 85 rows. Its global rows demonstrate that weather is a whole descriptor switch, not just fog intensity:

| Weather | Stardust name | Representative changes |
|---:|---|---|
| 0 | CLEAR | normal gradient sky/clouds, 0.005/0.002 fog endpoints, wind scale 1 |
| 1 | LIGHTSTORM | alternate sky, light dust appearance, 0.0065/0.003 fog, wind scale 5 |
| 2 | STORM | storm color ramp/appearance, 0.0075 fixed fog, wind scale 7 |
| 3 | HEAVYSTORM | heavy dust appearance, 0.025 fixed fog, wind scale 11 |
| 4 | EXTREMESTORM | heavy dust appearance and tornado audio, 0.043 fixed fog, wind scale 15 |

The weather names come from current Stardust server enum declarations. The asset itself stores numeric indices.

## ENVM celestial companion asset

GroundEnvironment also derives `terrain/environment/<terrainName>.iff`. The effective Tatooine file is `ENVM/0000`, 496 bytes. Loader `0x00845460` accepts ENVM and version 0000; `0x008455E0` recognizes:

- `NSKY` night-sky/star color-ramp reference;
- `STAR` star shader and scalar;
- `SUN ` and `SSUN` sun shader/glow variants and scalars/angles;
- `MOON` and `SMOO` moon equivalents;
- repeated `CELS` celestial-object records.

Tatooine references `terrain/colorramp/stars_tato.tga`, sun/moon shaders and repeated celestial appearances including its moon and distant ship silhouettes. This ENVM file defines shared celestial bodies; the 25-column DTII rows define regional/weather visual descriptors. Both are required for faithful original-asset rendering.

## Server correlation

Current Stardust defines weather indices 0–4 as `CLEAR`, `LIGHTSTORM`, `STORM`, `HEAVYSTORM`, and `EXTREMESTORM`. `WeatherManager::sendWeatherTo` calculates a position-dependent map value plus base weather, clamps values above four back to clear, avoids sending an unchanged index, and emits `ServerWeatherMessage` with the selected index and wind values. Thus both server position and client terrain position participate at different layers:

- server position selects the current numeric weather severity;
- client position selects the local terrain environment family;
- their pair selects the visual/audio descriptor.

## Replacement requirements

1. Parse DTII rows field-by-field and retain the exact 25-column schema.
2. Preserve PTAT family IDs and the composite lookup/fallback behavior; do not replace `global` with a hard-coded universal fallback.
3. Decode the 256×8 ramp with stable row order before assigning modern shader semantics.
4. Represent sky, two independent cloud layers, fog, shadow enable, day/night environment maps, camera-attached weather effects, wind scaling and audio paths in one immutable descriptor.
5. Maintain explicit current/previous descriptors and a five-second transition state.
6. Load ENVM celestial definitions separately from regional/weather DTII descriptors.
7. Use asset handles/IDs rather than reproducing the legacy 0x4168-byte pointer-rich descriptor layout.

## Remaining unknowns

- Exact modern shader equations needed to reproduce every legacy ramp consumer.
- Full `CELS` record semantics and celestial transform conventions.
- Whether every custom Stardust planet's referenced sky/cloud/appearance resources is present and valid after overlay resolution; table framing itself is now validated.

## Confidence

- **Confirmed:** selector key/fallback, chunk byte-family source, PTAT name/ID validation, 25 columns/types, descriptor resource construction, five-second transition, installed-table counts, current server weather enum/range.
- **Confirmed:** the eight ramp rows encode the time-varying light, sky, fog and two distinct shadow parameter sets documented in the ramp-semantics checkpoint.
- **Unknown:** only original editor-facing ramp labels and detailed celestial-record labels, not their runtime consumer behavior.
