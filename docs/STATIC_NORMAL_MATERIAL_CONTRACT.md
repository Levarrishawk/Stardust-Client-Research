# Static compressed-normal material contract

## Selected original boundary

The original mining-outpost door uses `effect/a_specmap_cbmp.eft`, one explicitly selected opaque PS2.0 pass, `vertex_program/a_specmap_bump_vs20_for_ps20.vsh` and `pixel_program/a_specmap_cbmp_ps20.psh`. This selection is an inspection policy; original capability ranking is not reproduced.

Confirmed raw SSHT mappings: MAIN=0, NRML=0, DOT3=1. Both MESH LODs use declaration `0xD205`, stride48, UV dimensions2/4. Position and normal precede UV0; the additional four-float set contains tangent XYZ and handedness W. Runtime vertex variants remap source macros from material coordinate tags, as established by DLL cache/compiler `0x62A15070`/`0x62A14740`; source defaults0/1/2 are not the deployed mappings. See [the shader-version evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_SHADER_VERSION_BOUNDARY.md).

`StaticNormalMaterialReader` owns source/program/texture/material descriptions, checks the selected pass, two sampler bindings MAIN0/CNRM1, BC3 normal storage, coordinate mappings and bounded tangent data. The worker uses the existing source cache. The complete original scene union becomes156 sources/16,624,079 bytes, each read once; the two added shader programs total5,100 bytes.

## Pixel equation

Confirmed compiled instruction identity after leading comments: `923751B744946EC571EC39AAB8DF590A8AC962D36AC70730B68E03043ECEBE0C`. The normal uses alpha/green and the absolute-radicand reconstruction in [the compressed-normal contract](SKELETAL_COMPRESSED_NORMAL_CONTRACT.md). MAIN RGB is unchanged by tint factors; MAIN alpha is the specular mask. The diffuse/specular/bloom/fade equation equals the selected no-hue skeletal pixel equation described in [the specular contract](SKELETAL_SPECULAR_CONTRACT.md), with white identity factors. Pixel specular has no effective N.L gate. Legacy interpolated COLOR inputs obey [the saturation boundary](LEGACY_PIXEL_COLOR_INPUT_CONTRACT.md).

Independent execution of64 supplied point-input cases agrees within1.725e-7. This establishes selected pixel instructions, not the original vertex LIT implementation, shader interpolation or GPU precision.

## Degenerate source tangents

All110 original tangent float4 records match independent raw hashes. Each LOD has two zero XYZ tangents with W=1: indices34/36 in L0 and16/18 in L1. They are referenced by four triangles per LOD and have nonzero normals. Both handedness signs occur in each mesh. The owned reader preserves zero tangents; they are original accepted data. Original device behavior for degenerate bases remains Unknown. Any adapter fallback must be explicit modern policy and must not rewrite source records or claim recovered hardware behavior.

## Validation and next action

Native original-chain route, source identity/one-read accounting, missing/masked/oversized programs, every synthetic-envelope truncation, changed instructions and invalid tangent coordinate/dimension/finite/handedness gates pass. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_DOOR_NORMALS.md) records the closure. Next: a retained GPU door adapter with verified reflected tangent upload, explicit degenerate fallback and per-instance light ownership.

## Retained door GPU adapter

`-SceneDoorLighting` requires owned materials/cell lights. Shared static meshes upload tangents only for validated material descriptions. The adapter projects T off unit N, normalizes XYZ, reflects Z and negates W. A degenerate projected tangent uses the least-aligned axis projected off N, with deterministic X/Y/Z tie order; raw source bytes remain unchanged. Independent double-precision checks of110 preview vectors agree within3.868e-8; GPU tangent XYZ readback tolerance0.003 and W exact preserve both original signs. Four fallback vertices occur across the two shared LOD templates.

Two door-instance materials override four surfaces without changing shared base materials, meshes or the existing28 geometry textures. The normal/specular shader uses white identity factors, MAIN alpha and shared explicit cell-lighting arithmetic plus material emissive. Each doorway uses its authored source-cell light records, fixed across actor/camera transfers; ambient0.1 and primary-light selection remain modern policy. Building-owner changes update uniforms; door-opening transforms automatically move the vertex positions within the same shader. Twelve owner publications and camera/actor independence pass.

Twelve GPU no-tint/MAIN-alpha/normal/specular/bloom/fade cases with diagnostic and active cell inputs pass at maximum display error0.003254. These prove the owned adapter, not original interpolation or vertex-device behavior. Original route/camera/live character controls, headless and worker cancellation pass. Capture inspected. See [the GPU checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_DOOR_NORMAL_PREVIEW.md).

The [recovered slot adapter](LEGACY_LIGHT_SELECTION_CONTRACT.md) now supplies confirmed secondary-only cell inputs to baked rooms; optional recovered selection also covers unbaked character/doors. Prior all-record room diffuse contributions are superseded. Ambient/attenuation/vertex exponent remain explicit policies.

The [tangent varying contract](TANGENT_LIGHTING_VARYING_CONTRACT.md) now closes the selected original vertex-source light/half-angle projection, separating it from modern fragment basis and zero-tangent repair.

The door now uses the [verified vertex tangent varyings](TANGENT_LIGHTING_VARYING_CONTRACT.md), superseding its earlier per-fragment half-angle recomputation.

The [original positive attenuation contract](LEGACY_POINT_ATTENUATION_CONTRACT.md) now closes coefficient order and uncapped reciprocal behavior. Recovered-slot preview uses that equation for positive finite inputs, with explicit modern singular omission; historical capped preview remains a separate policy. Original high-power vertex LIT behavior remains unresolved.
