# Skeletal mesh shader geometry

## Implemented structure

`SkeletalMeshGeometryDecoder` consumes the owned SKMG0004 skin-source description. It decodes optional global DOT3 (U32 count/four F32 per record), exact PSDT count, shader position/normal/tangent maps, optional raw four-byte vertex colors, all declared coordinate sets and ITL/OITL triangles. Remaining blend/hardpoint/occlusion metadata is retained in Skin; no execution follows from retention.

PSDT order: NAME NUL path; PIDX U32 vertex count plus countU32 global position indices; optional NIDX countU32 normal indices; optional DOT3 countU32 tangent indices; optional VDCL count four-byte raw colors; optional TXCI U32 set count plus U32 dimensions followed by FORM TCSF with one TCSD F32 array per set; final FORM PRIM/INFO U32 draw-chunk count and ITL /OITL chunks. Coordinate dimensions1..4, set count<=8. Every map/index/array width is bounded and checked. Shader vertex and serialized triangle aggregate capacities default262,144 and may explicitly reach1,048,576. Unsupported draw/child ordering rejects.

ITL is U32 triangle count followed by threeU32 shader-local indices per triangle. OITL adds a leading I16 occlusion-combination field to each record. Shader-local indices map through PIDX into global POSN. Every serialized triangle receives a stable mesh-global SourceIndex. Modern deterministic double cross-product zero-area filtering preserves serialized records while returning retained triangles separately. Original loaders remove exact zero-area triangles with their float arithmetic; tiny numerical edge equivalence is not claimed. Both original L0 sources have zero degenerates.

TRTS retention follows the prior [texture-renderer binding specification](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_MGN_TEXTURE_RENDERER_BINDINGS.md): INFO twoU32 counts, TRT NUL renderer path/U32 binding count/U32 shader-index+texture-tag pairs. Counts/widths/target shader bounds reconcile before publication. The original head's face renderer binding targets local shader0/MAIN. Blueprint execution is not supplied.

## Verification and confidence

Exact byte structure and mapped-position triangle filtering are Confirmed by loader007FCB50 and ITL007FB380/OITL007FBA00. Tangent naming and occlusion/tag meanings retain prior evidence confidence; no new blend/occlusion policy is inferred. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_MGN_GEOMETRY.md) records native malformed-input/owned-array tests and independent raw-array canonical geometry agreement.

Original twelve-file object-root L0 closure has1,790 shader vertices/2,810 triangles across five shader slots; zero degenerates; one retained generated face-texture reference. Every source remains unchanged and externally resolves once. This is decoded geometry, not rendered/animated character compatibility. Next: declared neutral bind-pose inspection, then skinning/customization/face generation boundaries.
