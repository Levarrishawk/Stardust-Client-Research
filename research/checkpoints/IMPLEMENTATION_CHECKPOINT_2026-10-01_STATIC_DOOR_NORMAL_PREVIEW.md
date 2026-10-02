# Retained static door normal/specular preview — October1,2026

Question: can the verified original door tangents/material render while retaining shared LOD meshes/textures, independent instance transforms and fixed door light ownership? Stop at GPU arithmetic, source/converted-tangent agreement and original route/failure/resource gates.

Inputs: same156-source/16,624,079-byte original object/POB/appearance/material chain. [Prior closure](IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_DOOR_NORMALS.md) establishes110 raw tangent records and64 compiled pixel cases. No original executable/server use.

Changes: worker/launcher opt-in; validated static tangent upload; deterministic orthonormal fallback for degenerate projected tangents; reflected Z/W boundary; per-door-instance surface overrides sharing existing meshes/textures; recovered no-tint MAIN-alpha normal/specular equation and material emissive. Explicit modern source-cell lighting/ambient0.1 policy; actor/camera movement never selects the door's light cell.

Observation: four surfaces/two materials retain the existing28 geometry textures. Two original LOD templates share four fallback vertices; independent110-vector double-precision audit agrees within3.868e-8, GPU readback checks XYZ within0.003 and W exactly. Twelve GPU equation cases agree within0.003254; existing48 room/32 cell-lighting and character/camera/control fixtures also pass. Original route validates all leaf placement, collisions and24 native transfers;12 owner-light publications and camera independence pass. Headless and worker cancellation pass. Build zero warnings/errors; rendered capture inspected.

Evidence: GPU2e47964990754280ad3118d59baeeeec, headlesseaa537935f66407ab13be8c65c6df756, cancellation3424673b95d1498a890846e08da59081. Native/source/instruction and conversion outputs remain under ignored generated-evidence. No staging/publication.

Confidence: Confirmed selected source/pixel contracts and retained adapter behavior. Modern light selection, zero-tangent fallback, per-fragment half-angle/basis policy are explicit; exact original vertex LIT, shader interpolation and degenerate-device behavior remain Unknown. [Durable contract](../../docs/STATIC_NORMAL_MATERIAL_CONTRACT.md).

Next: recover the backend's bounded light slots/ranking/filter contract so the explicit eight-record preview policy can be compared with the executable without claiming original rendered equivalence.
