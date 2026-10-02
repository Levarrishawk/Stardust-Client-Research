# Vertex-lit skeletal specular pixel boundary

## Selected original family

Materials without CNRM select the explicit `_ps20_nodot3.psh` inspection candidate; this is not original capability negotiation. MAIN material and factors, UV0 and sampler0 MAIN/sampler1 SPEC are validated before use. The current male human mouth is the sole occurrence in this selected closure. Its new owned program adds1,916 bytes:80 total resources/6,984,406 bytes.

`h_color2_specmap_ps20_nodot3.psh` source SHA4A9E641779D5E9D12FB2EEBA07728E0A8FA54719CC5EF06BE48DA331E98B599C; compiled instructions after comments SHA55E5591A9D73350E633491F80DE030921C7702D2E08BC3581DAE406D4EBC3D25. Decoder also recognizes the inspected single-color instruction family SHA D794150290AC181EEF093C4FD974832CFF55039DA606FE749C6BC4A8E94212F2; it is parser acceptance and is not another occurrence in the selected character.

## Recovered pixel equation

The compiled pixel program receives supplied interpolated vertexDiffuse and vertexSpecular. MAIN sample RGB is hue-tinted with MAIN alpha; SPEC sampler1 alpha masks vertexSpecular. For the selected two-color program:

- S=vertexSpecular*sampledSpecAlpha.
- RGB=sampledMainRGB*hue*vertexDiffuse+S.
- Alpha=dot(S,(0.3,0.59,0.11))*(bloom?0.65:1), overridden by explicit fade opacity.

Bloom does **not** scale this family's RGB specular term. Original HLSL computes a scaled local but returns the unscaled value, and compiled PEXE independently confirms that behavior. The single-color accepted family scales RGB specular by0.5 with bloom. No final color clamp is inserted by the evaluator. Supplied vertex lights remain separate from their scene selection/calculation and hardware interpolation limits.

## Vertex source observation and limits

The selected PVSH names vertex_program/a_specmap_vs20_nodot3.vsh, priority52/1,346 bytes/SHA AD5D1F09358DA7B9F20A82E126538297C333A502E291CB9F0DD005C9B6B5E408. Source calls calculateDiffuseSpecularLighting(false), adds ambient to diffuse and multiplies specular by material.specularColor. Its included parallel-light helper uses HLSL lit with the material power. This is a source observation/strong inference about runtime computation; the original runtime-generated vertex bytecode has not been captured or reproduced. Power300 is uploaded, but its exact compiled LIT/power range behavior remains open. No preview should label literal power300 evaluation as confirmed legacy shader identity.

## Implementation and proof

Owned program/material DTOs retain exact instruction identity and metadata. Explicit loading requires albedo factors and rejects invalid option combinations before lookup. Missing/masked program, truncated prefixes/changed code, sample/mask/factor mismatch, finite/HDR/bloom/fade and ownership gates pass.

Native `--skeletal-vertex-lit-specular` uses the full pinned original object/SAT/configuration and color profile. Independent raw effect sampler/coordinate, source/mip, material/face/shape/normal/animation and compiled pixel interpreter audits agree.64 original supplied point-texel/vertex-light cases over the mouth have max5.4694348272654736e-8 error at1e-5 tolerance; the prior192 normal-material cases remain verified. The two-color bloom difference is reachable in the actual selected program. These are supplied lighting fixtures, not observed original scene lighting.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_VERTEX_LIT_SPECULAR.md), [material/normal specular contract](SKELETAL_SPECULAR_CONTRACT.md). Next: GPU mouth pixel fixtures and explicit diagnostic vertex-light adapter; preserve original light selection/LIT compilation uncertainty.

## GPU and original interior integration

Full `CreatureSpecular`/`SceneCharacterSpecular` preview now loads this mouth program alongside the three normal-material families. Current closures80 sources/6,984,406 bytes and145 sources/16,598,491 bytes; GPU texture count/decoded bytes remain14/18,786,972. Retained material uniform/mask readback verifies the mouth material power300 and separate SPEC binding. Color/shape/age revisions retain both program families.

Mouth GPU shader calculates explicit diagnostic parallel-light diffuse/specular at vertices and interpolates those values before MAIN/SPEC/hue operations. It uses literal positive material power, a positive N.L specular gate and normalized world view/half-angle; this is an adapter policy, not confirmed original runtime LIT translation. Full multi-light selection, exact LIT exponent range and COLOR register interpolation/clamping remain open. The two-color program's unscaled bloom RGB remains preserved; evaluated alpha is inspected separately in RGB while the character stays opaque.

Eight GPU fixtures cover filtered hue/SPEC mask/bloom/fade/alpha and positive/negative diagnostic vertex-light cases. Max0.003146 channel error at2/255 tolerance. The previous12 normal-material GPU fixtures remain green. Original standalone/interior texture/geometry/material/readback,480 idle ticks,27 shapes,live color/four age choices and actor/door/collision/reflected owner/portal routes pass. Independent full interior audit includes all256 supplied compiled pixel cases and145-source identity union. Headless and first-lookup cancellation pass.

One headless run failed before asset loading in the common six-send offline login transcript fixture. Inspection found that exact accounting used production elapsed time/ticks and could vary under scheduling delay. That synthetic fixture now supplies controlled time and cancellation-only ticks; independent manually advanced timeout/retry tests still execute in the full native suite. Subsequent built GPU/headless proofs pass. Production LoginDatagramHost clocks/timers are unchanged.

[GPU/interior checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_VERTEX_LIT_SPECULAR_PREVIEW.md). Next implementation slice: original locomotion selections and movement-driven animated preview, while retaining source-backed asset chains and established live material/shape state.
