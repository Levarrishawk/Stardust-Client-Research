# Retained original room material preview — October1,2026

Question: can the closed original room pixel variants render with baked COLOR0 and explicit dynamic inputs while preserving per-instance room ownership and shared resources? Stop at GPU arithmetic/original route/owner/visibility/failure validation.

Inputs: same original154-source/16,618,979-byte chain;18 material descriptions/three selected original programs, independently audited by the [previous checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_ROOM_LIGHTING.md). No original runtime/server use.

Changes: optional room-lighting launcher/worker flag; per-instance surface overrides retaining existing shared meshes/base materials/textures; three pixel adapters; baked plus dynamic/emissive specular inputs and pure baked screens; legacy COLOR saturation. Room ambient is zero to avoid adding global ambient to baked lighting. An unselected factor remains nullable in the owned model; the opaque GPU preview explicitly supplies zero alpha-only strength. Original bloom-alpha equivalence remains Unknown.

Observation:42 room surfaces/42 per-cell materials use the existing28 GPU textures; template materials remain unchanged. Building-owner changes publish12 retained light updates. Camera visibility toggles leave room publications unchanged; actor-light and room-light ownership are distinct. Forty-eight GPU pixel cases agree at max0.002881; all previous character/camera/route/live-control/negative gates pass. Final source verifier still matches all18 bindings/192 actual-bytecode cases, source identities and one-read closure. Build zero warnings/errors; headless pass; rendered capture inspected.

Confidence: Confirmed owned source boundary, selected pixel equations and retained adapter behavior. Explicit modern light selection/attenuation cap remain policy. Unknown original LIT/light ranking/precise interpolation and omitted unselected-factor alpha state remain recorded.

Evidence logs: GPU069c0ae1fb24423d917c42178272c80a, headlesscbca09712b154afdada744332e29e5a2. Detailed/generated files remain private; no staging/publication.

Next: original static door compressed-normal/tangent/material boundary, beginning with its existing uncolored MESH declaration and selected a_specmap_cbmp program. Keep the investigation in the existing POB chain.
