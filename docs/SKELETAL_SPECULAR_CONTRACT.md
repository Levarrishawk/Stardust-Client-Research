# Selected skeletal material and specular contract

## MATL serialized order and shader constants

MATS0000 pairs TAG/MATL; MATL is17 F32 values. Existing finite-value parsing remains unchanged. Direct loader007843B0 reads four ordered colors through00A95860 into runtime+00/+10/+20/+30, then power+40. Color reader preserves serialized alpha/red/green/blue order.

Backend62A0E9D0 reads the selected PASS+7C material through the delayed DllExport.dll getMaterial import at62B82CCC. Direct instructions62A0EABA–62A0EB26 reorder colors into compiled pass records;62A0EF30 uploads vertex constants c11–c15 and pixel c5/c6. Original owned vertex/pixel includes name these constants. Together these confirm the mapping:

| Serialized float indices | Meaning | Vertex constant | Pixel constant |
|---|---|---|---|
|0–3|Ambient ARGB|c12 RGBA|—|
|4–7|Diffuse ARGB|c11 RGBA|—|
|8–11|Emissive ARGB|c14 RGBA|—|
|12–15|Specular ARGB|c13 RGBA|c5 RGBA|
|16|Specular power|c15=(power,0,0,0)|c6=(power,0,0,0)|

`StaticShaderMaterialDecoder` preserves finite values and returns owned immutable vectors. A nonzero selected tag missing from the shader map causes the observed backend white ambient/diffuse, black specular/emissive with alpha1, power0 fallback. A zero PASS material tag disables this upload; it is not that fallback. The current selected specular description requires present MAIN and rejects missing records.

Original body/head/face MAIN values are ambient/diffuse white, emissive allzero, specularRGBA=(0.2,0.2,0.2,1),power20. Mouth has white specular,power300. All four records are independently audited. Mouth uses the separately verified [vertex-lit pixel/preview contract](SKELETAL_VERTEX_LIT_SPECULAR_CONTRACT.md).

## Selected compressed-normal pixel equation

Exact two compiled PS20 families remain identified by the [normal contract](SKELETAL_COMPRESSED_NORMAL_CONTRACT.md). Body/face use MAIN alpha hue masking and separate SPEC sampler2 alpha. Head uses MAIN alpha as the specular mask and has no sampler2. NRML and MAIN both select UV0. MAIN material tag and sampler membership are validated against owned effect descriptions.

Let N be the recovered normalized alpha/green normal, H the normalized supplied tangent half-angle, L the supplied tangent light direction, m the sampled mask alpha. The compiled equation is:

- d=saturate(dot(L,N)).
- D=saturate(d*dot3DiffuseColor+vertexDiffuse).
- s=pow(saturate(dot(H,N)),materialPower).
- S=(s*dot3SpecularColor*materialSpecularRGB+vertexSpecular)*m.
- RGB=sampledDiffuseRGB*hue*D+S*(bloom?0.5:1).
- Alpha=dot(S,(0.3,0.59,0.11))*(bloom?0.65:1), overridden by explicit fade opacity when enabled.

There is no compiled N.L gate on specular. Diffuse light saturates before multiplication; final RGB and specular alpha are not saturated by the program. Filtering and interpolation occur before this evaluator. Supplied vertex light terms, light selection, vertex compilation, fog and final framebuffer/display behavior remain separate.

`SkeletalSpecularEvaluator` applies modern float32 policy. Its explicit bounded input policy requires nonzero half-angle, power>0 and≤4096, mask/fade in0..1 and finite used values. Zero/negative powers and degenerate vectors are outside this bounded implementation; they are not claims about original rejection. Bloom equations do not implement an image bloom postprocess.

## Evidence and verification

Original vertex constants include priority40/3,339 bytes/SHA C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9; pixel constants priority52/784 bytes/SHA36B679E349F22E95569E9E68CBDAC0FDA689015AD203FA901E624568D0997D67. Direct compiled instructions establish actual channel/equation behavior; source names corroborate semantics. [Microsoft POW instruction](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/pow---ps) supports opcode meaning, and [D3DMATERIAL9](https://learn.microsoft.com/en-us/windows/win32/direct3d9/d3dmaterial9) corroborates constant roles rather than serialized order.

Native `--skeletal-specular` uses the same original pinned object/SAT/configuration and explicit color profile arguments as `--skeletal-normal-diffuse`. The79-source closure does not grow. Independently decoded MATL and a Python interpreter of original PEXE instructions agree for192 supplied lighting/point-texel cases over three materials: maximum9.305903359013712e-8,tolerance1e-5. Point fixtures are explicitly supplied inputs, not recovered scene light observations. Portable asymmetric ARGB/constant/ownership, mask/bloom/fade/HDR/back-light and negative tests pass. Missing MAIN/texture/program description and incorrect sampler gates pass.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SPECULAR_EQUATIONS.md). Next: GPU sampled specular fixtures and an opt-in diagnostic-light preview using these material values.

## Diagnostic specular GPU preview

`CreatureSpecular` requires `CreatureNormalDiffuse`; `SceneCharacterSpecular` requires the corresponding interior normal mode. These enable specular only on the three validated compressed-normal materials. Mouth now uses the separate vertex-lit program; full selected specular closures are80/145 sources while the14-texture cache remains unchanged.

GPU material uniforms use recovered specular RGB and power. Separate SPEC bindings and MAIN-alpha mask selection are validated against owned descriptions and retained through color/shape/age revisions. Sampling occurs before hue/mask equations. Original material program equations are used with explicit diagnostic vertex diffuse/parallel light inputs; per-fragment world-space light/view half-angle construction and orthonormal prototype host basis are adapter choices, not recovered vertex interpolation. A degenerate half-sum gets a zero-specular preview fallback.

Opaque character preview does not bind the program's specular-luminance alpha as transparency. Twelve GPU fixtures separately visualize that alpha in RGB, testing fade/bloom alpha without changing original opaque inspection. No bloom image postprocess is claimed. Final display/framebuffer transfer remains the existing approximation; all compared fixtures stay within representable range.

Twelve filtered point fixtures cover distinct MAIN/SPEC alpha, hue mask, bloom RGB scaling, fade/alpha output and zero diffuse with retained specular. Maximum GPU display-channel error0.002764 at2/255 tolerance. Original standalone idle480 ticks/all27 shapes/color/detail interactions and interior routes/reflected owner/door guard/idle controls pass. GPU material/texture readback and headless checks pass; captures were visually inspected. [Preview checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SPECULAR_PREVIEW.md).

The mouth/no-normal pixel family and explicit diagnostic vertex-light adapter are documented in the [vertex-lit contract](SKELETAL_VERTEX_LIT_SPECULAR_CONTRACT.md); exact runtime LIT/power behavior remains open.
