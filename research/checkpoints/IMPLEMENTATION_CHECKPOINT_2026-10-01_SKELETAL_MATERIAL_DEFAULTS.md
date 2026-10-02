# Original skeletal texture defaults — October1,2026

## Question and stopping condition

Can original body/head shader dependencies be loaded without modifying source assets, with authored CSHD texture defaults applied and independently verified? Stop at source/default/mip agreement plus inspected GPU animation capture and cancellation.

## Inputs and tools

Same pinned configuration/object/SAT/MGN/LAT/idle as the [pose checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_ANIMATION_POSE_PREVIEW.md); source chain remains object IFF → inherited profile → SAT → LMG → selected original MGN → PSDT shader → original effect/DDS. Native `--skeletal-materials`, existing CSHD/static shader/DDS readers, existing descriptor Python oracle plus small material helper, Godot4.7.2 .NET Vulkan fixture launcher. Original assets/logs/capture stay ignored. No executable address claim or original executable execution.

## Findings and confidence

Observation: four original CSHD0001 shaders expose body/head/face/mouth MAIN bindings, three effect paths and ten selected textures. The face age default0 chooses wrinkle s01 rather than inline s04. Palette factors remain serialized descriptors and MGN generated face bindings remain unexecuted. Confirmed through original bytes and independent CSHD/default/SSHT parsing.

Observation: the complete idle/material closure has32 unique sources/4,668,368 bytes; each externally resolved once. Pillow agrees with all80 decoded mip hashes. Independent sampled-value,265-pose and8,950-vertex checks remain successful (max vertex1.330e-6). Confirmed native prototype ownership/dependency behavior.

Observation: headless and GPU preview pass, including full texture/mip readback, eight diagnostic/reset switches,480 original pose updates/two cycles and play/focus/pause/seek signals. Cancellation closes archives and publishes no geometry. Capture inspected: pale authored texture detail appears on the idle human; it is not palette-colored skin. Confirmed preview boundary; original effect execution is unclaimed.

## Rejections and verification limits

Native full suite/build pass. Original missing/masked shader/effect/texture, source resource exhaustion and invalid pre-lookup options reject. The initial independent audit caught the newly expanded source closure; the verifier was extended to derive material membership from original PSDT records and then passed. This was a verifier integration failure, not evidence of a source mismatch. Godot requires named MAIN UV0 and opaque candidate, rejects partial chains/premultiplied MAIN, and uses explicit unshaded inspection policy. Secondary normal/specular textures are owned and audited, but not shaded.

## Compatibility consequence and next action

See the [durable contract](../../docs/SKELETAL_MATERIAL_DEFAULTS_CONTRACT.md). Character texture dependency loading is now reusable outside the engine. Add the same option to interior ownership and verify source union/route/cancel/GPU behavior. Palette-factor shading, generated face compositing and complete effect semantics remain open. Scores89%/94% and curated registry counts unchanged.
