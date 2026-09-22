# Local-water geometry and installed shader contract

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe` plus installed TRE assets and Stardust terrain source  
Scope: `0x00862CA0`, `0x008634A0`, `0x008637D0`, PTAT BPOL/BREC records, deployed water SHT/EFT resources

## Local geometry construction

`client_local_water_primitive_construct_VA_00862CA0` consumes an arbitrary position vector and triangle-index vector. It:

- computes a bounding sphere/extent from all positions;
- determines polygon winding and reverses triangle indices when necessary;
- chooses a high-capability vertex declaration when graphics capability is greater than `0x100`, otherwise a fallback declaration;
- allocates and fills a static vertex buffer and 16-bit static index buffer;
- stores plane height, shader texture scale and bounding data;
- resolves the supplied logical shader through the ordinary shader manager.

On the high-capability path each vertex contains position. On the fallback path it additionally writes a constant normal and planar UV coordinates derived from X/Z, the primitive bounds and reciprocal shader size. The fallback declaration selector is `0x1105`; the high-capability selector is `1`.

Two producer families feed this primitive:

1. `local_water_build_polygon_primitive_VA_008634A0` triangulates an authored polygon, computes orientation/normal, and creates one local-water primitive from the resulting vertex/index arrays.
2. `local_water_build_polyline_strip_VA_008637D0` converts authored two-dimensional control pairs to positions at a fixed water height, adaptively selects tessellation spacing, generates a strip/polygon mesh, triangulates it and creates one primitive. On capable hardware spacing is bounded to `2..64`; otherwise the legacy fallback starts at `1024`.

This matches the server-side PTAT boundary grammar: BPOL and BREC records carry local-water enable, height, shader size and logical shader name. BPOL supplies arbitrary points, while BREC supplies its rectangle coordinates. The client geometry layer, however, is already generalized to arbitrary indexed local-water primitives.

## Graphics capability threshold

The numeric branch is exact: capability values `<= 0x100` use fallback vertex data; values `> 0x100` use the capable path. Global water uses the same boundary, selecting dynamic below `0x101` and static at/above it. The historical enum spelling represented by `0x100/0x101` is unavailable, but its behavior is fully specified and should become a feature-level predicate in a modern renderer rather than be copied numerically.

## Deployed water material contract

The new read-only inventory `water_shader_inventory.py` parses every installed PTAT copy, its enabled BPOL/BREC water boundaries, and matching SHT dependencies. Ordinary water shaders consistently resolve through `effect/water.eft` and use the following material tags (FourCC values shown in logical byte order):

| Tag | Observed resource role |
|---|---|
| `MFFP` | fixed-function/fallback color texture introduced by later shader patch |
| `ENVM` | runtime environment-map binding; often has no file NAME |
| `_SKY` | sky/environment cloud texture |
| `CUBE` | authored environment cube texture |
| `MAIN` | planet/local-water base color texture |
| `NRML` | water normal map |

Examples include `wter_corl`, `wter_dant_lake`, `wter_dsrt_bright`, `wter_nboo_beach`, `wter_rori_swampbrown_spec`, `wter_spec`, and many local boundary variants. `shader/wter_corl.sht`, for example, resolves to `effect/water.eft` with fallback beige, runtime ENVM, `env_cloud`, `env_nboo`, `wter_corl_beige`, and `water_base_n` inputs.

`effect/water.eft` contains three programmable implementation branches and one fixed-function fallback:

- a PS/VS 2.0-oriented pass-2 branch using MAIN, NRML twice, and ENVM;
- pass-1 plus pass-2 branches for two higher capability profiles;
- a fixed-function PFFP/STAG fallback.

The programmable resources are `water_pass1`, `water_pass2_20`, `water_pass2_25`, and `water_pass2_ps20` vertex/pixel programs. The two-pass branches bind MAIN for pass 1 and normal/environment inputs for pass 2.

Lava-like local boundaries are a distinct family. Deployed shaders such as `wter_lava_01_slow`, `wter_lava_01_still`, and `lava_test` resolve through `effect/water_lava_textured.eft` with `LKUP`, `MAIN`, `NOIS`, and `TEXT` resources. A replacement must not force these through the ordinary reflective-water material.

## Reflection conclusion

`ENVM`, `_SKY`, and `CUBE` confirm environment-based reflection inputs. Combined with the executable analysis—which finds no water-owned render target or mirrored camera—the required legacy contract is environment/cube sampling, not planar scene reflection/refraction. A true reflection pass would be an optional visual enhancement, not compatibility behavior.

## Confidence

- **Confirmed by code:** arbitrary indexed primitive construction, winding correction, vertex/index buffer formats, capability branch, polygon and polyline producer families.
- **Confirmed by assets:** ordinary and lava material/effect families, texture tags, program paths and fallback resources.
- **Confirmed by server source:** BPOL/BREC local-water fields and PTAT global-water fields.
- **Unknown naming only:** historical graphics-capability enum and several helper class names.

## Replacement-client consequence

Decode BPOL/BREC water records losslessly, triangulate authored polygons and strips, preserve water height/texture scale/logical shader ID, and translate the SHT tag set into a renderer-neutral water material. Support ordinary and lava families separately. DX12, Vulkan, or an engine renderer may replace the legacy vertex variants, but original shader IDs and texture dependencies remain compatibility keys.
