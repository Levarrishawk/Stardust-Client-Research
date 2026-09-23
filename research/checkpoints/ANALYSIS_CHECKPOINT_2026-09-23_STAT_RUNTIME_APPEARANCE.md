# STAT space-terrain client runtime (overnight block 22)

Target: `Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Evidence is read-only Ghidra decompilation in ignored `local-private/session-2026-09-23-marathon`; addresses are stable provisional identifiers, not original symbols.

## Factory and object path

- `0x00854480` registers tag `DAT_015ed0ec` with the appearance-template factory and exit-chain label `ClientSpaceTerrainAppearanceTemplate::remove`. The callback `0x008544B0` allocates a template and calls `0x00854520`, which invokes the parsed space-terrain template base constructor `0x00B60D00` and sets vtable `0x015E6E24`.
- Virtual slot `0x00854590` allocates a 0x60-byte appearance and calls `0x0084DE20`. The latter sets runtime vtable `0x015E684C`, creates a 0x18-byte helper via `0x0084CB10`, and creates a DPVS object through `0x00766840`. This confirms a real runtime appearance, not merely a parser/cache resource.
- Runtime virtual `0x0084DF60` invokes the base update then `0x0084DA50`, which iterates contained object pointers and calls virtual `+0x10` on each with the update argument. The helper also stores the argument globally; the exact meaning of that global remains unknown.

## Runtime composition observed in `0x0084CB10`

The 620-line decompilation constructs an ambient light from the parsed AMBI values, loops over PARА records to create named `parallel_light_%i` objects and applies direction from their stored vector, configures fog/environment/texture state through the template getters, conditionally creates a skybox (`skybox` name) using either a texture-backed or appearance-backed branch, and creates a named `stars` appearance when STAR count is positive. It loops over celestial records and other appearance records, fetches referenced appearance templates, instantiates object appearances, applies stored transforms, and appends resulting objects to retained vectors. One loop explicitly RTTI-checks `PlanetAppearance` and, on match, invokes `0x008682D0` and `0x008682B0` for positive scalar values. Thus the STAT-to-PLNT chain is confirmed *in client code*, not only in asset references.

Do not yet collapse the `PLAN`, `DIST`, and `CELE` loops into one generic record: the code uses distinct getter families (`0x00B611D0`/`0x00B612xx`, `0x00B61110`/`0x00B611xx`) and different construction/transform paths. The precise getter-to-child-field mapping, camera-relative placement, draw order, and per-frame visibility remain follow-up items. The decompiler's types around pointers/floats are clearly unreliable in places; the assembly and template storage offsets are authoritative for exact semantics.

Confidence: **confirmed** for registration, allocation/vtables, contained update iteration, object creation, ambient/parallel lights, skybox/stars branches, and PlanetAppearance RTTI gate; **strongly inferred** for the overall space backdrop render role; **unknown** for camera-relative transforms and complete child-loop mapping. Provisional names: `0x00854480 register_client_space_terrain_template`, `0x00854590 create_client_space_terrain_appearance`, `0x0084CB10 build_space_terrain_scene_contents`, `0x0084DA50 update_contained_space_terrain_objects`.
