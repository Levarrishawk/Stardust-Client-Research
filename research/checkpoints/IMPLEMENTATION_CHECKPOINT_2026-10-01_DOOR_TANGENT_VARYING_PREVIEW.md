# Retained door vertex tangent varying preview — October1,2026

Question: can the recovered vertex/pixel staging render with perspective interpolation and retained door resources, and can the test distinguish the previous fragment-recomputed half-angle policy? Stop at GPU projection/interpolation, original scene and failure gates.

Inputs: same157-source/16,636,397-byte original union, previously closed door programs/660 source-stage vectors. No original execution/server use. Synthetic triangle fixtures are explicitly authored, with varying tangent bases/depth and controlled texels; original scene assets remain unchanged.

Changes: static door shader emits tangent light/normalized-half vectors at vertices and consumes their perspective interpolation in the recovered pixel equation. Original light interpolation is not normalized. Existing source-preserving IR and explicit adapter zero-tangent/orthonormal policies remain separate.

Observation:48 GPU cases cover both W signs, two owner poses, both projection modes, two powers and three pixels. Owned perspective interpolation agrees within display error0.003136; old fragment-policy RGB-vector difference reaches0.119721. Twelve prior door pixel cases and room/cell/character fixtures pass. Original route/camera/live controls/source closure retain four door surfaces/two materials/28 shared geometry textures and12 door owner publications. Build zero warnings/errors; headless/cancellation pass and capture inspected.

Evidence: GPU6c0e4a67686f4c0a8ffb0360ca2f1f5c; headless055658152d984e128e318a4d0ac8fd33; cancellationdbe154c8b03a43d191c297f6df03fe0e. [Durable contract](../../docs/TANGENT_LIGHTING_VARYING_CONTRACT.md). Generated/private outputs are unstaged.

Confidence: Confirmed owned source equation/selected pixel staging and retained GPU interpolation at the stated tolerance. Original vertex compiler/LIT precision and normalize-zero behavior remain Unknown; modern repaired basis is explicit. Metrics/registries unchanged.

Next: verify whether the original character normal/specular vertex families use the same varying contract before sharing this corrected stage with retained skeletal material shaders.
