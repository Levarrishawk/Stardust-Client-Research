# 2026-09-30 shared room and door visuals

## Question and input identity

Can a single visual owner share immutable room/leaf meshes, decoded materials and GPU textures while retaining independent door transforms, camera LOD and controller lifetime? Stop at original leaf material/source comparisons, shared-object checks and original scene/headless/GPU regression. Reuse [scene pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md). Primary leaf LOD0 MSH SHA-256 `29DEF0C4293C91389B654A216E80710D78A6D7F11C6065BD6CD8F2CEC1640E3D`; LOD1 `990BD294C2BFC11D1E90E639A35F7A93FA4E8F5C86B6F4C2E60BF934273423D3`.

## Confirmed implementation and evidence

The combined content reader produces a clearly synthetic composite geometry root with room group0 and door-instance group1. Room children preserve authored CELS ordinals; leaf children preserve explicit specification order. One material attachment traverses the complete graph, so decoded content-identity sharing and decoded-pixel budget apply across rooms and both leaf LODs. Returned geometry is engine-neutral adapter input, not a recovered serialized family.

The existing independent textured-mesh verifier matches each original leaf LOD's geometry and both DDS textures / 20 mip levels. Regenerable private source manifests combine with the independently checked room manifest and door/floor pins. Exact source union: **65 assets / 9,614,085 bytes**, each externally resolved once. Native shared-reference checks show five material templates / 28 distinct decoded textures; low decoded-byte, source-byte and source-count budgets reject. The original 2,880-tick trace / 24 transfers / 172 blocked / zero steps remains unchanged.

One prototype host geometry owner contains all room and door nodes. Each leaf's two LOD mesh resources are identical to the other leaf's resources, while both node trees and world transforms remain independent. Camera selectors operate on their own world placements. Room group receives the shared building owner; leaf child poses remain world poses. Headless/GPU checks pass sharing, both owner poses, all previous controls/input/visibility/guarded closing and reflected placement. Scene: five textured mesh templates, 44 unique textured surfaces, two zero-pass omitted surfaces, zero neutral/material diagnostics and 28 shared GPU textures. The scene capture and separate original LOD0 textured-door capture were inspected; upload readback verifies all geometry/texture mips.

Confidence: Confirmed modern resource ownership/sharing and original input/rendering boundaries. MAIN inspection still omits original specular/emissive programs and lighting. Collision and controller objects are not shared; world obstacle collision and interactive OS capture remain unresolved. Scores unchanged.

## Next action

Move the owned CPU content load to a cancellable worker with explicit single-result transfer and stale/closed-owner rejection; keep prototype host resource creation and disposal on the main thread.
