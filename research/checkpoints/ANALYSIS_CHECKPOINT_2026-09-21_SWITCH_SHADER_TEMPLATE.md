# Switch Shader Template (`SWSH` in Shader Namespace)

Date: 2026-09-21

## Result

The shader-side `SWSH` implementation is `SwitchShaderTemplate`, distinct from the appearance-side `SwooshAppearanceTemplate` despite sharing the same four-character tag. Ten effective live `.sht` files use this wrapper, all version 0000.

## Loader and runtime contract

`FUN_0077f320` registers tag `SWSH` in the shader-template registry with factory `0x0077f430`. The 32-byte legacy template constructed at `0x0077f490` owns:

- a common shader-template/base selector object at `+0x18`;
- a vector of child shader templates at `+0x1c`.

`FUN_0077f610` accepts only `FORM SWSH/FORM 0000`. `FUN_0077f680` loads the common selector, reads the number of remaining child templates, recursively loads each through the shared shader-template factory, and retains them in authored order.

Creating a runtime shader allocates the 20-byte legacy wrapper at `0x00785c90`. It creates a runtime instance of the selector and one runtime shader for every child template. At render/query time, the selector's virtual index result chooses one child; the wrapper forwards operations to `children[index]` (confirmed in `0x00785c30` and `0x00785c50`).

This is an actual runtime variant switch, not a capability fallback chosen once at load time.

## Effective corpus

The configured overlay contains ten live switch shaders, including:

- lightsaber blade variants (`lightsaberblade`, `permafrost`, `blackwing`, `lava`);
- animated water shaders;
- sample delta-time, random-time, and frame-animating shaders.

All ten use version 0000. The broader `.sht` namespace contains 23,124 effective names, so registry context is mandatory: most `.sht` files correctly report as other outer tags and are not SWSH anomalies.

## Replacement-client consequence

- Key the parser registry by asset domain plus tag, e.g. `(ShaderTemplate, SWSH)` versus `(AppearanceTemplate, SWSH)`.
- Decode and retain the selector plus ordered child-template list.
- Validate the selector result before indexing; the original wrapper assumes a valid index.
- Allow the selected child to change at runtime and forward material operations without rebuilding the owning mesh.
- Compile each child into the modern material/pipeline representation and let a lightweight wrapper select among them.

## Confidence

- Class identity, registry, version, recursive child list, and runtime indexed forwarding: confirmed.
- Ten-file effective corpus and version distribution: confirmed.
- Exact class/editor name of the common selector object embedded at `+0x18`: unknown.
