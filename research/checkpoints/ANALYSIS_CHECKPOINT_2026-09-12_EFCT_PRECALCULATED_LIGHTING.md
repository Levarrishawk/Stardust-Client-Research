# EFCT precalculated-vertex-lighting flag closure

Scope: `C:\Stardust-DEV\Stardust.exe` and the configured `swgemu_live.cfg` TRE overlay. This block resolves the previously unknown second byte in `EFCT/0001 DATA`.

## Confirmed findings

- `shader_effect_ctor_load_VA_007833E0` initializes effect byte `+0x18` to false.
- `shader_effect_load_v0000_VA_00783580` reads only the U8 implementation count, so version 0000 retains false.
- `shader_effect_load_v0001_VA_00783630` reads `{U8 implementationCount, U8 containsPrecalculatedVertexLighting}` and normalizes/stores byte 1 at effect `+0x18`.
- `shader_effect_selected_implementation_get_VA_00783700` returns the first capability-supported implementation selected by either loader.
- `StaticShaderTemplate` virtual slot 7 is `static_shader_template_contains_precalculated_vertex_lighting_VA_0077FFF0`; it returns the EFCT object's `+0x18` byte.
- The PE export directory names `0x0076A4C0` as `?containsPrecalculatedVertexLighting@StaticShader@@QBE_NXZ`. Its body loads the shader template and tail-dispatches virtual slot 7. This closes the semantic chain without relying on filename inference.
- Other observed `ShaderTemplate` implementations (`CustomizableShaderTemplate`, `SwitchTextureShaderTemplate`, `SwitchShaderTemplate`, and `OwnerProxyShaderTemplate`) use the common false-return stub at slot 7 rather than forwarding the property.

## Live asset evidence

The configured overlay has 356 winning `EFCT/0001` records: 316 carry false and 40 carry true. All 40 true records currently use `effect/c_*.eft` names. The prefix correlation is a consistency check only; the exported symbol and call chain are the semantic proof.

## Reimplementation consequence

The asset IR must preserve this EFCT-level boolean independently of implementation/pass render state. A loaded static shader must expose it to scene submission so geometry carrying precalculated vertex lighting follows the correct sorter/render path. Version 0000 defaults false. Wrapper-template behavior should initially match the observed legacy false default unless later evidence demonstrates a required modern normalization.

## Confidence

- Field layout and meaning: Confirmed.
- Version-0000 default: Confirmed.
- Virtual-interface position and exported consumer: Confirmed.
- `c_` naming convention itself: Strongly inferred; not required by the implementation.
