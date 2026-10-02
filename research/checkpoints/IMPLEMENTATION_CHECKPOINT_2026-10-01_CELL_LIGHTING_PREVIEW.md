# Retained original-cell character lighting — October1,2026

## Question

Can the existing original character/three-room chain consume owned CELL lights without replacing GPU resources or using camera-cell ownership? Stop at opt-in character rendering plus bounded GPU/route/failure checks.

## Evidence and confidence

Input remains the pinned original POB and object IFF→SAT→skeleton/MGN→original materials/textures/animations, effective48-mount overlay. No synthetic replacement of the original character chain. Source union remains148/16,612,055 bytes; inline LGHT adds no source reads. [Record evidence](IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_CELL_LIGHTS.md) and [independent math](IMPLEMENTATION_CHECKPOINT_2026-10-01_CELL_LIGHTING_MATH.md) establish input and explicit policy.

Observation: owned worker collections match19 original records exactly. The original288-case producer/full independent source verifier pass. GPU32 math cases agree at max0.002961 and12 normal-material cases at max0.00304, within2/255 display tolerance. The original route visits all three owner cells and publishes50 light updates, preserving mesh/material/pose identities. Two different camera/actor-cell cases preserve actor-owned lighting across visibility toggles.

Interpretation: recovered light creation data can drive retained character shading. Selection, ambient0.1, eight-record budget, near-light cap and literal vertex power remain explicitly modern; original global light selection/LIT behavior is Unknown. Room/door MAIN inspection remains unlit pending its own material contract.

## Changes and checks

Added optional worker light collection, reflected GPU arrays, shared vertex arithmetic and retained material publication. The test-only shader initially omitted its display helper, causing a compilation failure; corrected it to use the existing shader header and repeated the GPU gate successfully. Exact array upload checks and all-eight-slot fixtures pass. Positive power0 is explicit to avoid GLSL pow(0,0) ambiguity.

Full native suite/build pass, zero compiler warnings/errors. Final rendered log6802e3474307450aab9e402f8d7ab436 was superseded by b17dda1df927409fad707f03bdce78ff with active normal-material tests. Headless260a81e1d093419e881616e63c33d907 and cancellation2f52ed2a9f0644389b2afe155fa81108 pass. Original interior capture visually inspected: retained character behind camera correctly faces into the room. Generated evidence remains ignored; nothing staged/published.

## Next probe

Trace the existing selected room materials and packed vertex colors before applying authored lights: distinguish precalculated lighting/material branches from dynamic inputs to avoid counting lighting twice. See [contract](../../docs/CELL_LIGHTING_PREVIEW_CONTRACT.md).
