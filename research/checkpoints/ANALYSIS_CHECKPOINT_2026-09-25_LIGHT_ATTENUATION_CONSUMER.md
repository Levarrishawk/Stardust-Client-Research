# Light attenuation consumer closure

Date: 2026-09-25. Question A07. Static targets: verified `Stardust.exe` and configured `s207_r.dll`.

## Result

The three floats at `Light +0x100,+0x104,+0x108` are the ordered point-light attenuation coefficient vector. Their original field spellings are unavailable, but the conventional interpretation is constant, linear, and quadratic attenuation respectively. The vector role is **confirmed by code**; the individual conventional names are **strongly inferred** from order, constructor default `(1,0,0)`, and the D3D9 lighting convention.

## Typed path

1. `Light` virtual method `0x0076D8A0` registers or removes the live object through `0x00773590` / `0x00773670`.
2. The shader sorter stores at most 32 registered objects and materializes the active mask into a pointer vector at `0x007736D0`.
3. Wrapper `0x00755E40` invokes raster API slot `+0xE8` with that vector.
4. `GetApi` returns base `0x62B835B8`, so slot `+0xE8` is `s207_r.dll!0x62A04F60`, not `0x62A04FE0`. `0x62A04F60 -> 0x62A09EF0` copies the selected pointer vector into the backend and marks lighting state dirty.
5. The sole vector consumer `0x62A0A160` classifies each pointed-to `Light` using its type at `+0xA0`. Type-3 records are selected into the point-light arrays.
6. Shader upload builder `0x62A0A3C0` reads the object's world-space translation, color callbacks, and then reads `+0x100,+0x104,+0x108` consecutively into one four-float record with a zero fourth component. It uploads the assembled point-light constant block through `0x62A0E210`.

The fields are therefore neither placement nor unused metadata. They are GPU-visible coefficients adjacent to each point light's position and color. Constructor `0x0076D500` initializes them to `(1,0,0)`, the no-distance-falloff coefficient set. CLEF and CDF records deliberately override them.

## Corrected false lead

The first backend lookup used table base `0x62B835C0` and landed on `0x62A04FE0`, a vertex-format/graphics-data constructor. That base was eight bytes late: `0x62B835B8` includes the two bootstrap entries. The impossible type interpretation was rejected, the documented base was rechecked, and the corrected `+0xE8` mapping produced the typed light-vector chain above.

## Replacement requirement

Preserve the three authored coefficients in order and evaluate point-light attenuation using constant/linear/quadratic terms. A modern renderer need not reproduce the legacy object layout or raster API table. It should retain the original values in asset IR, diagnose pathological denominators, and apply any safety clamp explicitly rather than silently rewriting source data.

## Evidence and confidence

- **Confirmed by code:** Light registration, selected pointer-vector copy, type-3 point-light selection, direct reads of all three fields, four-float GPU constant packing, and upload.
- **Strongly inferred:** coefficient names constant/linear/quadratic, based on exact order, `(1,0,0)` default, and D3D9 convention.
- **Unknown but non-blocking:** historical C++ member names and the precise legacy shader equation/disassembly for every capability branch.

Ignored reproducibility evidence is under `local-private/generated-evidence/s207_light_globals_xrefs.tsv`, `light_a07_vtable.tsv`, and `local-private/ghidra/decompiled/s207-light-api/`.
