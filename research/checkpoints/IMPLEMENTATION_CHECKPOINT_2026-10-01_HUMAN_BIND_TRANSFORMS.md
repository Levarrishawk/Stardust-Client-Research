# October 1, 2026 — Original human bind transforms

## Bounded question

Can the recovered skeleton transform route produce owned engine-neutral body/face bind matrices with correct noncommuting hierarchy and independently matching original results? Stop before mesh skinning/rendering.

## Evidence and implementation

Used prior SKTM bind-route/backend specifications; rechecked baseline SHA in the preceding skeleton milestone. Inspected existing private00ABA530/0082BAE0 decompilation and direct constant bytes. Quaternion converter's ordinary matrix is standard scalar-first column-vector action; positive identity shortcut compares W+epsilon with1. Constant015EAB28 isD2749E12, F32 approximately1.0000000272452012e-27;015DC6F8 is1. No copied decompiler pseudocode enters the implementation: standard quaternion/affine operations are implemented from the durable specification and mathematical derivation.

`SkeletonBindPoseBuilder` builds local rotation RPST*BPRO*RPRE/BPTR, rigid inverse and localInverse*parent/external. System.Numerics matrices deliberately transpose legacy representation, reversing the hierarchical product to parent/external*localInverse. Owned local/inverse tables, explicit bounds/topology/right-handed-rigidity checks and zero-joint acceptance are documented in the [contract](../../docs/SKELETON_BIND_TRANSFORM_CONTRACT.md). No quaternion normalization or sign canonicalization is added.

## Validation

Full native smoke passes noncommuting parent/child pose (child forward origin2,1,0 rather than3,0,0), three-family quaternion order, external-root attachment, zero joints and invalid topology/translation/scale gates. Original ten-source human bundle produces four body levels and face level0 attached independently to each body's Head:176 matrices. Independent double affine oracle, reading unchanged original skeleton bytes and rechecking source priority/size/hash/configuration pin, agrees at maximum component error4.1409738826825304e-7 with2e-5 tolerance. Godot build passes zero errors/warnings. No original executable execution or rendered character occurs.

Reproduce `--skeletal-bind-pose` with the same pinned object/SAT options as preceding milestone. [verify_skeleton_bind_pose.py](../../../SWG-Godot-Client/tools/python/verify_skeleton_bind_pose.py) ROOT CONFIG NATIVE_OUTPUT `--configuration-sha256 CONFIG_PIN` audits matrices; descriptor verifier independently audits the ten-source chain and named arrays. Detailed output remains ignored/private.

## Confidence and next probe

Confirmed disk/math order and bounded implementation; numerical agreement is tolerance-based, not original x87 bit identity. Inverse-bind historical labeling is Strongly inferred. Face level0 attachment across four body levels is an explicit offline composition; dynamic skeleton LOD fallback remains separate. Scores/names unchanged. Next: decode bounded original MGN positions/weights/primitive mapping and validate bind-pose geometry before preview skinning or animation claims.
