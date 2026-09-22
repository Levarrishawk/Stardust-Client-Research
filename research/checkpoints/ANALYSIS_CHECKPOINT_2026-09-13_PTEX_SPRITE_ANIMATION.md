# PTEX sprite-sheet and shader-binding checkpoint

Date: 2026-09-13

Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Outcome

`PTEX/0000` is now behaviorally closed. It is a quad-particle shader binding plus square sprite-sheet animation descriptor. Its final Boolean enables shader resolution/rendering. It supports lifetime-normalized frame selection or elapsed-time animation at an explicit frames-per-second rate. The installed format family uses 1×1, 2×2 and 4×4 atlases.

## Serialized/runtime fields

`0x007B7220` accepts `FORM PTEX` with chunk version 0000 only. `0x007B7370` reads the payload; `0x007B74E0` writes the identical order.

| Legacy offset | Serialized type | Meaning | Confidence |
|---|---|---|---|
| `+0x04` | cstring container | shader logical path | Confirmed runtime binding / Strong generic label |
| `+0x14` | U32 | square-atlas layout selector/total cell capacity: 1, 4 or 16 | Confirmed |
| `+0x18` | U32 | inclusive first frame | Confirmed |
| `+0x1C` | U32 | inclusive last frame | Confirmed |
| `+0x20` | F32 | normalized UV cell extent | Confirmed |
| `+0x24` | U32 | cells/frames per row | Confirmed |
| `+0x28` | F32 | frames per second, or -1 for normalized-lifetime mode | Confirmed |
| `+0x2C` | U8 | shader/texture enabled | Confirmed |

Setter `0x007B75F0` maps layout capacity 1→1, 4→2 and 16→4 cells per row, then derives UV extent as `1 / cellsPerRow`. The file also stores the derived values explicitly; a robust decoder should validate but preserve them rather than assume host object layout.

## Frame-selection algorithm

`0x007B7100` receives normalized particle age and elapsed-time state and returns four UV boundaries.

- If frames-per-second is `-1`, it selects from the inclusive frame range using normalized age.
- Otherwise, it computes animation duration as `frameCount / framesPerSecond`, wraps elapsed time within that duration, and derives the frame. This proves `+0x28` is FPS rather than seconds per frame.
- It divides the selected frame by cells-per-row; quotient and remainder select atlas row and column. UV extent expands these into the rectangle edges.

Quad render preparation `0x007A2660` calls this function once per particle and places the returned UVs into the generated quad vertices.

## Enabled/shader binding

`0x007A15C0`, the quad local-shader-primitive constructor, obtains PTEX from the PTQD description. It calls `0x007B75B0` to test `+0x2C`. When false, it skips resource lookup. When true, it obtains the logical name through `0x007B75C0` and fetches the shader used by the primitive. This closes the formerly unknown Boolean.

The resource namespace is predominantly `.sht` (5,607 records). There are 332 empty strings and 34 `.dds` suffixes. Enabled records can still have unusual names, so compatibility code should use the shader/resource manager's normal lookup semantics and not enforce a filename extension at the PTEX layer.

## Effective corpus census

`scan_particle_textures.py` scanned all effective `.prt` winners without errors:

| Property | Distribution |
|---|---|
| PTEX version | 0000: 5,973 |
| Enabled | true: 5,965; false: 8 |
| Layout capacity | 1: 5,004; 4: 282; 16: 687 |
| Cells per row | 1: 5,004; 2: 282; 4: 687 |
| Common frame ranges | 0..0: 5,068; 0..15: 607; 0..3: 122; 0..1: 66 |
| Animation mode | normalized lifetime (`-1` FPS): 5,172; explicit FPS: 801 |
| Unique resource strings | 595 |

Common explicit rates include 30, 16, 8, 20, 13, 40 and 15 FPS; the loader must retain arbitrary positive float rates rather than constrain them to a preset enum.

All 5,973 records satisfy `firstFrame <= lastFrame < layoutCapacity`; every layout is canonical and every stored UV extent equals `1 / cellsPerRow` within floating-point tolerance. These are suitable importer validation invariants, while tolerant diagnostics remain preferable to hard failure for future custom assets.

## Replacement-client contract

Store shader identity separately from atlas animation. Decode explicit integers/floats, validate `first <= last < layoutCapacity` where assets permit, and preserve authored values on tolerant import. Particle simulation/render preparation selects the frame from normalized age or elapsed time and publishes a UV rectangle. The renderer binds the resolved material/shader and may instance all particles sharing it. Disabled PTEX records produce no quad shader primitive and are valid for sound-only/no-visual particle effects.

## Remaining risks

1. Some resource strings have empty or `.dds` names; downstream shared shader lookup behavior, not PTEX, decides whether those resolve.
2. The exact edge behavior at normalized age 1 should be duplicated in tests to prevent selecting one past the inclusive last frame.
3. Resource-loss/reload behavior belongs to the shared shader system and remains separate from PTEX serialization.

Overall weighted client-research progress remains approximately 74%. PTEX no longer contributes a material unknown field.
