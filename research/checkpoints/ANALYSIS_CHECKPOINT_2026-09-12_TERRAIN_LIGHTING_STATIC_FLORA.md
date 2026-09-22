# Analysis checkpoint: terrain lighting mask and static-collidable flora

Date: 2026-09-12

Scope: resolve `ClientChunk +0xA8` and locate the non-radial static-collidable flora path. Static analysis only; `Stardust.exe` SHA-256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## `ClientChunk +0xA8` is not passability

Confirmed dataflow:

1. `client_chunk_build_render_data_VA_008618D0` allocates a one-byte-per-sample grid at `ClientChunk +0xA8` from generated chunk data `+0x84/+0x88/+0x8C`.
2. It copies the grid byte-for-byte at `0x00861B0E..0x00861B2F`.
3. The only bounded terrain-runtime read is at `0x00861E4A..0x00861EAF`, during packed three-byte terrain vertex-color construction.
4. When the feature byte returned by `0x0084A4D0` is active, zero/nonzero selects one of two global color endpoints. The original color is interpolated toward that endpoint by `0.6f` (`0x3F19999A`).
5. No collision, pathing, height, flora or environment query reads the field.

Conclusion: this is a binary baked-lighting/shading mask used to modify generated terrain vertex colors. Its exact historical name and endpoint-color semantics remain unknown. The former tentative `passability mask` label is rejected.

Passability is separate in Stardust server source: `ProceduralTerrainAppearance::isPassable()` evaluates layers using `AffectorProceduralRule::PASSABLE`.

Confidence: **confirmed role**; **unknown historical name**.

## Static-collidable flora is a separate deterministic system

The camera-radial managers cover `FloraStaticNonCollidable`, `FloraDynamicNear` and `FloraDynamicFar`. Static-collidable flora lives in shared `ProceduralTerrainAppearance` code around `0x00B5E780`.

```text
world position
  -> floor-aware 16 m cell coordinates (0x00B5ED20 / 0x00B5ED70)
  -> linear cell key (0x00B5EDD0: x + z * rowWidth)
  -> cell-residency maps at owner +0x1A8/+0x1AC
  -> deterministic RNG seeded from the cell key
  -> candidate X/Z and validity tests
  -> terrain height query through virtual +0xB8
  -> 0x00B5D6E0 -> ClientChunk +0xA4 flora query
  -> weighted family/template result
  -> full Object and Appearance construction
  -> transform/orientation/scale and world registration
```

Evidence:

- `0x00B5E740` finds a chunk and forwards a world-coordinate query to `0x00B5D6E0`.
- `0x00B5D6E0` samples `ClientChunk +0xA4` with the packed static-flora resolver.
- `0x00B5E780` quantizes both horizontal coordinates by exactly `16.0f`, builds the cell key, seeds local random state, rejects invalid candidates, gets height, resolves flora, creates an object/appearance, applies transform and scale, and inserts it in cell maps.
- The flora entry supplies a template/resource pointer at `+8` and optional scale range at `+0x19/+0x1C/+0x20`.
- Nearby debug strings include `SharedTerrain`, `logFloraCreation`, and `ProceduralTerrainAppearance::remove`.

Confidence: **confirmed broad architecture and 16 m cells**; **strongly inferred entry fields**; exact RNG algorithm and full rejection policy remain open.

## Replacement implications

- Decorative radial flora may use engine/GPU instances.
- Static-collidable flora requires deterministic residency and collision-bearing entities or equivalent proxies independent of visual LOD.
- The legacy lighting mask may become an optional terrain attribute or shader/vertex-color preprocessing input. It is unrelated to wire compatibility and authoritative passability.
- Server passability must follow PTAT procedural rules, not this lighting mask.

## Remaining unknowns

- Exact lighting-mask name and endpoint-color meanings.
- Full candidate rejection/density loop and RNG algorithm.
- Whether `WMAP`/`SMAP` participate through generic client loaders.
- Exact collision-proxy policy after static-flora object creation.
