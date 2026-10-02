# Godot geometry-facing boundary

Positions and normals reflect Z once: S=diag(1,1,-1). A reflection reverses the sign of cross(B-A,C-A) dot the transformed normal without changing indices. Godot's front triangles are clockwise, as stated in the [Godot4.7 ArrayMesh documentation](https://docs.godotengine.org/en/4.7/classes/class_arraymesh.html). Original tested meshes predominantly have positive cross/normal products, so preserving their indices after reflection produces the correct Godot front face. An additional index reversal was a defect.

## Original observations and confidence

Independent effective-overlay audit of seven original meshes: body1,750 positive/zero negative; head1,056 positive/four negative; door L0/L1 respectively44/20 positive; three rooms20,501/1,984/20,501 positive. Total45,860 triangles,45,856 positive/four negative. Stored normals are observations; they are not a universal proof of outwardness. Preserve the four authored opposing head triangles unchanged. No per-triangle heuristic rewriting or decoder mutation is introduced.

Confidence: **Confirmed** for tested source bytes, reflection arithmetic and renderer fixtures; **Strongly inferred** for their intended outward face from normal agreement and corrected visual capture. Untested geometry remains subject to its authored ordering. Do not claim every original format uses this convention without evidence.

## Implementation and verification

`StaticMeshPreview.CreateArrays` and `SkeletalMeshPreview.Arrays` reflect positions/normals and preserve original index order. Tangent xyz reflects Z and tangent W changes sign as before. Asymmetric coordinate/map fixtures and GPU readback compare preserved indices. Synthetic pixel quads now explicitly use source CCW indices aligned with their +Z normals; their previous opposite ordering concealed the import defect.

The GPU UV probe checks four colored samples from the front, then moves the camera to the opposite side and checks that those face samples are culled. Full original standalone/interior, normal/specular pixel, retained pose/live-control and actor/door proofs pass. Corrected capture shows the character's back when the boom is behind its forward heading and interior walls remain visible. `verify_mesh_facing.py` independently audits source cross/normal signs and exact reflection sign reversal.

## Historical correction

The September22 adapter checkpoint and September29 preview reports said to reverse imported indices. Their transform/quaternion tests remain valid; their rendering fixtures verified an internally consistent synthetic winding that did not match original asset normals. The October1 investigation supersedes the index-reversal claim. Historical captures and prior tests remain evidence of their stated execution, not evidence of correct outward-face presentation. [Investigation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHASE_CAMERA.md).
