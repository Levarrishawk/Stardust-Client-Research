# Original tangent lighting varying contract

## Selected source observation

The owned door VSH and `vertex_program/include/functions.inc` establish the selected source-stage equation. The include is priority40/12,318 bytes, SHA256 CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15; the door VSH identity is in [the static normal contract](STATIC_NORMAL_MATERIAL_CONTRACT.md). Both are fetched through the same source cache. The full union becomes157 sources/16,636,397 bytes, each read once.

For raw vertex N, tangent XYZ T and handedness W, B=cross(N,T)*W. Object-to-tangent projection of any vector Q is(dot(Q,T),dot(Q,B),dot(Q,N)). The helper does not normalize N/T/B or repair zero tangents. Vertex light output projects the supplied object toward-light vector directly. Object half-angle is normalize(normalize(camera−position)+towardLight), then projected into tangent space. The door VSH writes these vectors to TEXCOORD2/3. The selected compiled pixel program normalizes the sampled AG normal and interpolated half-angle, but does not normalize the interpolated light vector before diffuse dot/saturation.

**Confidence: Confirmed** source equation, selected coordinate outputs and compiled pixel operations. Runtime compiled vertex instructions/precision are not recovered from plain source files. Hardware interpolation details remain separate from the source expression.

## Owned evaluator

`TangentLightingVertexEvaluator` computes raw basis projection without changing source data. Its explicit finite-zero normalization policy divides by max(length,1e-6); original normalize(0) behavior remains Unknown. Positive bounded clip-W and nonnegative screen barycentric weights provide a separate perspective-correct interpolation helper: normalize the three weights after division by clip-W, then blend each raw varying. Half/light normalization is not added by that helper. Strict finite/bounds/triangle/weight rules are replacement policy.

660 original vertex/view/light cases agree with independent double-precision projection at maximum1.440e-6 (tolerance2e-6). Twenty-four cases retain the four original zero tangents under six supplied lighting configurations; their tangent X/Y outputs stay zero. Hand calculations test both W signs, nonunit raw tangents, zero preservation and perspective weights. Negative inputs fail deterministically.

## Compatibility consequence

The existing modern normal/specular adapter recomputes viewer/half-angle and transforms a normal per fragment. That is algebraically different from projecting at vertices and interpolating vectors, especially with varying tangent bases or near-camera geometry. The owned door path should use vertex-produced tangent light/half-angle varyings, preserving the independently verified pixel equation. Adapter orthonormalization and zero-tangent repair remain explicit modern choices, not recovered source behavior. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_TANGENT_LIGHTING_VARYINGS.md) records the bounded source closure; GPU interpolation is next.

## Retained door GPU stage

The static door adapter now projects light and normalized half-angle at vertices and uses interpolated tangent-space vectors in the fragment equation. Normal-map/sample-half normalization and unnormalized light-dot staging match the selected source/pixel contract. Adapter orthonormalization, degenerate tangent repair and finite-zero normalization remain explicit policies. Shared LOD meshes/textures/material instances stay retained.

48 synthetic GPU cases cover differing per-vertex tangent bases, both W signs, rigid owner rotation/translation, orthographic/perspective cameras, two powers and three sampled pixels. Independently supplied screen barycentric/clip-W values drive the owned evaluator; maximum display error0.003136. The fixture's previous fragment-policy RGB-vector difference reaches0.119721, establishing that it can detect the closed staging difference. Original157-source route, previous64 compiled door pixel cases/660 source cases, camera/control/resource gates, headless/cancellation and capture inspection pass. [GPU checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_DOOR_TANGENT_VARYING_PREVIEW.md).

## Original character vertex-family closure

The original body, head and face compressed-normal PS20 effects all select the same VSH described above. Body/face use h_color2_specmap_cbmp_ps20.psh; head uses h_specmap_cbmp_ps20.psh. Every original TCSS map binds MAIN0,NRML0,DOT3=1. Exact VSH/functions identities are owned and bounded by SkeletalTangentLightingBundle through the shared source cache. Full interior remains157 sources/16,636,397 bytes, each externally read once; standalone specular loading adds the two resources when absent. Missing/masked/truncated/changed/oversized input and invalid tangent-set gates pass. Independent raw SHT/EFCT/TCSS/hash audit confirms all three materials. Confidence Confirmed selected source/bindings; compiled vertex precision/normalize-zero remain Unknown. Mouth retains its distinct vertex-lit program. [Character source checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_TANGENT_VERTEX.md).

## Retained character GPU stage

The verified TangentSpecularPreview vertex stage is shared by owned character normal/specular and door materials. Character tint and MAIN/separate-SPEC masks retain their closed pixel-family distinctions. Material emissive joins the vertex diffuse term. Mouth remains separate. Initial shader-cache identity now includes mask selection and vertex-lit bloom scaling, preventing the head from reusing the body mask branch; exact retained shader/emissive checks cover original and generated bindings. Customization retains the owned tangent closure.

48 character GPU cases combine hue/separate mask with varying tangent bases, both W signs, owner poses, perspective/orthographic projection and powers2/20. Maximum display error0.002878; old fragment policy differs by RGB-vector0.056866. Prior mask/bloom/fade/HDR/active-cell fixtures, full original route, retained publication, headless/cancellation and inspected capture pass. [Character GPU checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_TANGENT_VARYING_PREVIEW.md). Original vertex compiler/LIT precision and normalize-zero remain Unknown.

## Complete selected include graph

The selected VSH includes vertex_shader_constants.inc and functions.inc; the latter includes ../../shared_program/functions.inc. These four exact identities now form one owned read-only closure used by character and door readers. Independent original include traversal closes3 edges/20,146 bytes. Newly owned constants/shared functions add2 sources/5737 bytes: current full interior159 sources/16,642,134 bytes, each externally read once. All four source dependencies have missing/masked/truncated/changed/oversized gates. Prior two-source observations are historical subsets, not full compiler graphs. Independently authored shaders retain the closed equations; original compilation/device precision is still outside the owned source identity. [Source graph checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_TANGENT_VERTEX_SOURCE_CLOSURE.md).

## Combined point-color acceptance

The corrected combined point-color/normal/tangent/hue/mask fixture passes48 cases at max0.003225 within2/255. A synthetic row/column light-direction setup error caused the initial failure; production arithmetic and tolerance were unchanged. Per-fragment point recomputation differs by0.474411. It remains an opt-in diagnostic. [Exact evidence and next probe](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_POINT_COLOR_GPU_PROBE.md).
