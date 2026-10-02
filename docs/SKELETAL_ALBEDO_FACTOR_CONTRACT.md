# Skeletal albedo factors

## Scope

`loadAlbedoFactors` explicitly requires owned palette defaults. It selects one documented PS1.1 inspection candidate per material, owns/deduplicates the referenced program, validates its instruction pattern and pass factor/stage identities, combines authored TFNS with selected CSHD palette factors, and retains an engine-neutral `SkeletalAlbedoFactorBundle`. This is the unlit albedo-factor slice; normal maps, lighting, specular, bloom, fade and hardware capability choice remain unexecuted.

`StaticShaderFactors.Decode` owns TFNS0000 tag/ARGB pairs with exact extent/cardinality/duplicate validation. Absence stays explicit. `BlueprintFacePreparer` now reuses this decoder. `SkeletalAlbedoProgramDecoder` validates PSHP0000/PSRC/PEXE and shader1.1, ignores only bounded leading COMMENT records, and recognizes the two observed full instruction sequences. Unsupported opcode/register/mask/envelope/truncation patterns reject.

## Confirmed original semantics

For body, face and mouth, the compiled PS1.1 program samples texture0, computes LRP from texture0 alpha between constants3/4 and multiplies diffuse RGB by that hue. The head's single-factor program multiplies diffuse RGB by constant3. Original PASS0009 maps factor3 to MAIN and factor4 to HUEB for the two-color materials; the head has no second tag. Existing backend factor upload establishes registers3/4. Original HLSL source independently agrees.

`SkeletalAlbedoEvaluator.Evaluate` consumes normalized sampled source-encoding RGBA and returns sampled RGB×factorRGB for single-color, or sampled RGB×lerp(firstRGB,secondRGB,sampledAlpha) for the two-color case. Factor alpha is irrelevant to this RGB slice. Texture alpha is a hue mask, not transparency. Filtering precedes evaluation; tinting mip texels before sampling would give a different result. Finite0..1 sample bounds validate.

Authored selections give skin MAIN on body/head, eye MAIN on mouth, HUEB white on body/mouth, and two white factors on face. The generated face already contains its layer palette colors; white outer factors preserve it. Palette flags remain raw metadata, including eye255. Missing palettes use the existing diagnostic cyan policy; malformed present palettes reject. Empty/duplicate selected factors or absent required static factors reject.

## Godot inspection policy

`-CreatureAlbedoFactors` requires `-CreaturePaletteDefaults`; both still require material defaults. Existing texture-only and palette-ownership modes remain available. The core supplies validated factor identities/colors; Godot evaluates them after sampling original MAIN, including generated TRTS overrides. Original textures/mips remain unchanged. Parameters and retained material/texture identities are verified during animation.

The shader samples source-encoding RGBA without `source_color` conversion, evaluates hue, then adapts to the renderer's output color space. Unspecified legacy color space follows the existing sRGB display convention. This is an explicit modern display policy, not recovered legacy gamma behavior. Compatibility reports sRGB shader output; Forward+/Mobile report linear. [Godot spatial documentation](https://docs.godotengine.org/en/4.6/tutorials/shaders/shader_reference/spatial_shader.html#global-built-ins). Compatibility's renderer also applies an approximate transfer pair after ALBEDO; its dark output is not exact identity. GPU fixture expectations record that adapter step separately. [Official renderer source](https://github.com/godotengine/godot/blob/4.6/drivers/gles3/shaders/scene.glsl), [transfer formulas](https://github.com/godotengine/godot/blob/4.6/drivers/gles3/shaders/tonemap_inc.glsl). These sources explain the adapter discrepancy; local4.7.2 GPU tests establish observed behavior.

## Verification

Native `--skeletal-albedo-defaults` follows the complete pinned original object/material/palette/idle/face chain:74 sources/5,927,038 bytes, adding two shared programs. Independent raw effect/pass/TFNS/CSHD/RIFF/program instruction/source checks agree on four materials, three alpha-mask cases, all factor colors and exact source union. Earlier geometry/mip/face/animation audits remain.

Portable fixtures verify both program patterns with/without comments, malformed/truncated/altered programs, TFNS ownership/duplicates/extent, sample bounds and hand-calculated mask endpoints/midpoint/single-factor behavior. Original missing/masked program gates pass. GPU synthetic2×2 filtered samples prove post-sampling hue evaluation and display boundary in three cases, maximum observed normalized error0.001545 (tolerance2/255). Material parameters, raw texture and generated-face bytes,480 idle updates/two loops, controls and cancellation cleanup pass. Updated capture inspected: visible skin coloration and generated head detail. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_ALBEDO_FACTORS.md).

Face-only profile and authored body/eye modes remain available. The new [shared customization snapshot](SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md) drives both through the confirmed private/shared-owner identity; nonzero original standalone/interior audits pass.
