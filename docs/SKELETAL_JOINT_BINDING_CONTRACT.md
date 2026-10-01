# Skeletal joint binding and position diagnostics

## Explicit owned binding

SkeletalJointBindingBuilder composes selected skeleton levels in SAT reference order. Callers provide one explicit level0..3 per path; there is no automatic attached-skeleton fallback. One unattached root, preceding uniquely folded attachment name, valid local topology and rigid affine matrices are required. Limits:64 references/8,192 combined joints/2,048 transforms per mesh. Joint records retain source path/index/name, global parent, local/world bind and inverse bind. Raw authored names remain intact.

Mesh transform names resolve only within declared SKTM paths under bounded ASCII case folding. Missing paths/names and duplicate/ambiguous matches reject. Original human body/head resolve all52 XFNM names into53 combined body/face joints at explicitly selected level0. Face root attaches to body Head.

## Modern deformation boundary

CreatePalette consumes explicit rigid world poses and mapped joint indices. System.Numerics row-vector palette is inverseBind * currentWorld. DeformPositions evaluates every authored influence in order using the position's complete TWHD range and TWDT pair array. It preserves weight values, without renormalization or truncation. Finite/budget/width/range/index/unit-sum/rigidity failures reject before returning owned results. This is a modern linear-blend position diagnostic, not recovered original animation execution or complete skin rendering. Shader normal/tangent deformation, blends, active occlusion, customization and animation playback are not provided.

## Evidence and verification

Input structures and bind order follow the earlier executable-backed descriptor/bind contracts. Attachment composition and scoped name mapping are explicit adapter policies, correlated with original source references and unique matches. Full native known noncommuting hierarchy and mixed-weight vectors, invalid attachment/levels/names/poses/weights pass. Original twelve-source329,296-byte closure remains unchanged, one lookup each. Independent original-byte double oracle checks53 joints/52 transform mappings and4,362 position results across bind/common rigid/split diagnostic poses. Maximum matrix error4.140974e-7, position error4.284464e-7, bind identity error3.976532e-7 (tolerance2e-5). The latter includes preserved near-unit original quaternions and float weight sums; exact bit identity is not asserted.

Native probe `--skeletal-joint-binding` uses existing explicit object/SAT/configuration pins. `tools/python/verify_skeletal_joint_binding.py` rereads effective original archive bytes and verifies pins, scoped names, attached matrices and weighted positions. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_JOINT_BINDING.md). Next: close original mesh-generation consumers, particularly influence transfer and normal/tangent ownership, before claiming faithful animated skin rendering.


[Original consumer closure](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_SKELETAL_INFLUENCE_CONSUMERS.md) confirms full position influence transfer and shader normal association with PIDX; original animated matrix production remains open.


CreateWorldPose composes supplied local rigid matrices through the bound global-parent order, validates size/topology/rigidity and returns owned worlds. Native noncommuting parent/attached-child vectors distinguish composition order. [Pose preview](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_WEIGHTED_CHARACTER_POSE_PREVIEW.md) uses explicit synthetic local rotation; no original animation source.
