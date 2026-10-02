# Shared skeletal position deformation — October 1, 2026

## Question and stopping condition

Can each owned mesh deform global positions once per pose while preserving every shader's independent PIDX/NIDX/DOT3 mapping? Stop at native malformed-input gates, original independent agreement and GPU playback/readback.

## Inputs and method

The same pinned configuration, object, SAT, body/head MGN and idle clip as the [pose checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_ANIMATION_POSE_PREVIEW.md) are used unchanged. No executable address claims are added. Native probes use `--skeletal-shader-deformation` and `--skeletal-animation-pose`; existing Python joint-binding and descriptor/animation oracles verify their output. Generated evidence stays ignored.

## Observation and interpretation

`SkeletalShaderDeformation.CreateAll` validates the complete shader batch, computes all global positions once, then maps each shader and computes its directions from that shader's position-associated influences. Owned results remain separate. Aggregate vertex and shader budgets, late invalid maps, absent direction maps and singular palettes reject before publication. The single-shader API remains available.

For the selected original body/head, global position evaluations per pose fall from 3,478 to 1,454 because two body and three head surfaces share their mesh's positions. This is a mechanically derived reduction in position work, not an FPS claim. Godot retains materials while rebuilding deformed surfaces.

## Verification and confidence

- Native full suite passes, including unequal shader mappings and late-invalid batch rejection.
- Original joint/deformation oracle passes 4,362 positions and 5,370 shader component checks; maximum error 4.284464e-7.
- Original animation oracle passes 265 joint poses and 8,950 vertices; maximum error 1.330e-6.
- GPU preview passes 480 updates/two loops, readbacks and play/focus/pause/seek controls. Local measured mean 2.266ms; run-to-run timing is not a controlled benchmark.

Confirmed prototype behavior. Original optimized branch scheduling remains unclaimed. Source closures and research scores do not change.

## Compatibility consequence and next probe

Batching avoids repeated position work without truncating original influences or sharing shader direction maps. Next: decode the four original character materials through existing CSHD authored-default policy, audit their original texture/effect dependencies, and render a bounded MAIN texture-only preview. Palette colors and generated face rendering remain explicit gaps.
