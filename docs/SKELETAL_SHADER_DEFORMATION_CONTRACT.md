# Skeletal shader deformation

SkeletalShaderDeformation consumes owned geometry, explicit rigid transform palette and selected shader index. It evaluates all original TWHD/TWDT influences selected by PIDX for positions and for NIDX/DOT3-selected directions. Translation affects positions only. Normals and tangent XYZ preserve raw weighted sums, without renormalization; tangent W stays authored. Absent maps remain absent. Output owns arrays; no engine types or animation player are required.

Complete positions/weights/palette validation precedes publication. Shader map count and index errors, nonfinite direction/fourth components and declared vertex budgets reject. Native unequal position/normal/tangent maps demonstrate weights follow PIDX; mixed25%/75% rotations demonstrate nonunit weighted directions and unchanged tangent W. Negative count/index/float and absent-map fixtures pass.

Original-body/head twelve-source329,296-byte closure remains unchanged. Independent raw-byte double oracle checks5,370 shader vertex results under bind/common rigid/split-rotation diagnostic poses, including position/normal/tangent components. Maximum component error4.284464e-7 at2e-5 tolerance. Complete prior53-joint/52-name/4,362-position audit also passes.

Influence association and raw direction output originate in the [original consumer closure](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_SKELETAL_INFLUENCE_CONSUMERS.md). Palette poses are explicit modern diagnostics; the original animation/matrix producer is still open. GPU packing may normalize directions at its declared engine boundary; this core operation does not. No x87 bit identity or optimized branch equivalence claimed.

Native `--skeletal-shader-deformation` uses existing pinned object/SAT/configuration arguments. Independent `verify_skeletal_joint_binding.py` automatically checks shader rows in that output. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SHADER_DEFORMATION.md). Next: main-thread upload of explicit diagnostic poses and inspected reset/deformation capture. Animation playback/material customization/generated face/active occlusion remain separate.

The subsequent [weighted diagnostic preview](NEUTRAL_CHARACTER_PREVIEW_CONTRACT.md) completes main-thread pose upload and inspected reset/deformation capture. Original animated pose evaluation remains separate.
