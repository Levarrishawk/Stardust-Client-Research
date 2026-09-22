# Analysis checkpoint: ClientChunk auxiliary grids and PTAT maps

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

Constructor callback evidence closes most of the previously unnamed ClientChunk generated arrays. The arrays form a renderer-neutral terrain sample package covering surface material, flora families, terrain color, environment and normals. PTAT `WMAP` and `SMAP` are confirmed by Stardust source as water and seed maps, but their client runtime consumers remain unproved.

## Recovered ClientChunk grid map

| Field | Width | Meaning | Confidence/evidence |
|---:|---:|---|---|
| `+0xA0` | 3 bytes | terrain shader/classification sample | Confirmed by 3x3 material selection and shader-group lookup |
| `+0xA4` | 2 bytes | static collidable flora packed sample | Strongly inferred from shared base-chunk flora sampler and PTAT category ordering |
| `+0xC4` | 3 bytes | packed terrain/flora color | Strongly inferred; copied into both dynamic-near and dynamic-far result records as three color bytes |
| `+0xC8` | 2 bytes | static non-collidable flora packed sample | Confirmed by `FloraStaticNonCollidable` manager callback `0x0084AD30 -> 0x008626F0` |
| `+0xCC` | 2 bytes | dynamic-near flora packed sample | Confirmed by `FloraDynamicNear` callback `0x0084AD70 -> 0x00862880` |
| `+0xD0` | 2 bytes | dynamic-far flora packed sample | Confirmed by `FloraDynamicFar` callback `0x0084ADB0 -> 0x008628B0` |
| `+0xD4` | 1 byte | environment-family ID | Strongly inferred from one-byte family lookup and `terrain/environment/%s.iff` consumer architecture |
| `+0xD8` | 12 bytes | surface normal Vector3 grid | Confirmed by mesh primitive construction |
| `+0xA8` | 1 byte | binary terrain lighting/shading mask | Confirmed role; exact original field label unavailable |

The dynamic flora packed value is two bytes: the first selects a family and the second becomes a normalized 0..1 selection value. The group resolver uses that normalized value to choose a weighted child entry. Dynamic query results contain the packed three-byte color plus the selected family child pointer.

The static non-collidable sampler resolves its two-byte key through a different family group and returns a record containing a byte, a float and an object/template pointer. This is compatible with static flora placement metadata, but the exact byte/float field names remain unknown.

## PTAT auxiliary maps

Stardust server source names the `MAPS/0001` children explicitly:

- `WMAP`: `waterMap`;
- `SMAP`: `seedMap`.

Each representative installed map payload is 524,288 bytes. The containing `DATA` chunk holds two floats and two integers whose semantics remain unlabeled. Server source parses both maps but exposes no runtime accessor or consumer; repository-wide search found no use beyond loading and destruction.

A complete immediate-constant scan of executable code found no direct `WMAP` or `SMAP` FourCC comparison. This is negative evidence, not proof that the client ignores them: the shared IFF parser may consume them positionally or through a data-driven template path. No claim is made that either map is optional until the client load path is traced from `ProceduralTerrainAppearanceTemplate` into its `MAPS` object.

## Replacement consequences

- Define a generated terrain sample structure with explicit typed channels instead of reproducing pointer-bearing legacy array wrappers.
- Preserve two-byte flora family/selection values and three-byte color precision until corpus testing proves safe conversion.
- Keep collidable, static non-collidable, dynamic-near and dynamic-far flora domains distinct; they have different runtime consumers.
- Preserve the binary lighting/shading mask as a byte in generated terrain data until its renderer-neutral representation is validated.
- Preserve `WMAP`/`SMAP` bytes in the importer even before their consumers are implemented.

## Remaining unknowns

1. Exact static-flora result byte and float semantics.
2. Final proof that `+0xC4` is RGB rather than another three-channel color encoding.
3. Exact original editor-facing name of the `+0xA8` lighting/shading mask.
4. `MAPS/0001 DATA` field meanings and WMAP/SMAP sampling dimensions/encoding.
5. Whether WMAP influences local water generation, seed determinism, editor output, or only legacy preprocessing.
