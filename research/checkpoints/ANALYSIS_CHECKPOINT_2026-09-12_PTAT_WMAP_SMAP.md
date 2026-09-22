# Analysis checkpoint: PTAT WMAP/SMAP terrain maps

Date: 2026-09-12  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

The PTAT `MAPS` object and its two bitmaps are now structurally closed. The Stardust server source supplies the names **water map** (`WMAP`) and **seed map** (`SMAP`), while the executable independently proves their byte layout, coordinate conversion, point/rectangle operations, and actual client consumers.

Only `WMAP` has direct runtime consumers in this executable. No direct caller of the exposed SMAP point or rectangle query was found. The replacement must decode and retain both maps, but must not invent active SMAP behavior merely from its server-side name.

## On-disk schema

`ProceduralTerrainAppearanceTemplate` loader `0x00B6E980` accepts PTAT versions `0013` and `0014`. Both construct a 24-byte `TerrainMaps` object at PTAT member `+0x9C`. `terrain_maps_load_VA_00B9A470` accepts:

```text
FORM MAPS
  FORM 0000                       // valid empty map set
or
  FORM 0001
    DATA
      F32 worldSize
      F32 metersPerBit
      U32 bytesPerRow
      U32 bitDimension
    WMAP[bytesPerRow * bitDimension]
    SMAP[bytesPerRow * bitDimension]
```

The effective Tatooine asset contains:

```text
worldSize    = 16384.0
metersPerBit = 8.0
bytesPerRow  = 256
bitDimension = 2048
WMAP bytes   = 524288
SMAP bytes   = 524288
```

Thus each map is a 2,048 x 2,048 one-bit grid spanning a 16,384-meter square at 8 meters per bit. Bit X is stored LSB-first within its byte. Byte index is `y * bytesPerRow + (x >> 3)`, mask `1 << (x & 7)`.

## Coordinate behavior

The map is centered on the origin. Integer-grid point queries subtract `floor((worldSize / metersPerBit) * 0.5)` from both supplied axes before sampling. World-coordinate queries divide X and Z by `metersPerBit`, use floor-like handling for negative values, then apply the same half-dimension offset. Out-of-range point queries return false.

Rectangle queries convert both corners, normalize min/max ordering, and return true if **any** bit is set in the inclusive rectangle. Corresponding mutation methods set or clear individual WMAP/SMAP bits.

## Virtual interface and live use

The shared `ProceduralTerrainAppearance` vtable begins at `0x015ECDFC`. Its relevant slots are:

| Vtable byte offset | Concrete client method | Operation |
|---:|---:|---|
| `+0xCC` | `0x00B5E0E0` | WMAP point query |
| `+0xD0` | `0x00B5E160` | WMAP rectangle-any query |
| `+0xD4` | `0x00B5E120` | SMAP point query |
| `+0xD8` | `0x00B5E1A0` | SMAP rectangle-any query |

The terrain-owner wrappers at `0x00B5C330`-`0x00B5C390` dispatch those four slots. Direct-call analysis found three consumers of the WMAP point wrapper `0x00B5C370`:

- `0x006C9CE0` samples a 5x5 grid around the active camera's terrain-grid coordinate and returns a bounded occupancy fraction. This is a local water-coverage estimator.
- `0x006CBA40` is a `StructurePlacementCamera` vtable method (RTTI type descriptor `.?AVStructurePlacementCamera@@`). During placement-view rendering/update it performs a larger camera-dependent terrain traversal and uses WMAP bits to admit water-marked cells into a downstream submission path (alongside a classification value of 3). The owning subsystem is confirmed; the exact historical method name is not asserted.
- `0x00B4CA80` rejects a terrain-cell processing path immediately when the WMAP bit is set, before procedural classification and height-related work. This is confirmed water-cell exclusion behavior.

No direct caller was found for the WMAP rectangle wrapper or either SMAP wrapper. The methods are nevertheless present in the common virtual interface and must remain representable.

## Server correlation

`MMOCoreORB/src/terrain/TerrainMaps.h` labels the maps `waterMap` and `seedMap` and reads the same four DATA fields followed by WMAP and SMAP. `TerrainMap.h` reads each chunk as an opaque byte array. Current server code does not consume either map, so the server is naming/schema evidence, not evidence for unobserved client-side SMAP semantics.

## Replacement requirement

- Decode version 0000 as an empty optional map set and version 0001 exactly as above.
- Validate multiplication and chunk lengths before allocation.
- Retain WMAP and SMAP losslessly in an engine-neutral terrain asset object.
- Implement centered coordinate conversion and LSB-first bit access exactly.
- Use WMAP as the fast water-cell mask where the recovered client behavior requires it.
- Treat SMAP as retained compatibility data until a real consumer or authoring contract is evidenced.
- Do not serialize native pointers or host-sized fields; all four DATA fields remain fixed 32-bit file values.

## Confidence

- File framing, field widths/order, byte counts, bit packing, point/rectangle algorithms: **Confirmed by executable code and deployed asset**.
- WMAP = water map; SMAP = seed map: **Confirmed by Stardust server source**.
- WMAP active client use: **Confirmed by direct call paths**.
- Exact historical label of each WMAP consumer: **Strongly inferred from mechanics; provisional names used**.
- Active SMAP purpose in Stardust.exe: **Unknown; no runtime caller established**.
