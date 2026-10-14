# 2026-09-30 camera heading and mouse input

## Question and inputs

Can camera heading drive timestamped horizontal movement and portal visibility without changing vertical actor policy or leaking input across focus/pause boundaries? Stop at native direction/timing fixtures and original headless/GPU event/camera tests. Reuse [current scene pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md) and existing typed scheduling. No original camera behavior claim.

## Confirmed replacement observations

[Immutable view model](../../docs/ACTOR_VIEW_INPUT_CONTRACT.md) passes asymmetric 90-degree heading/pitch, full-turn wrap, ±85-degree clamp, planar diagonal speed, owner direction mapping and invalid angle/axis rejection. A 35ms turn from +Z to +X produces displacement (0.032, 0, 0.064) after three 20ms ticks under both 10ms and 20ms render updates, in identity and rotated/translated owner poses. Tick-start semantics preserve two forward ticks and one rightward tick. The first owner-direction assertion was corrected to the established row-dot transform convention; implementation uses the existing transform API.

Prototype host handles dispatched InputEventMouseMotion with timestamped velocity submission. Original headless/GPU gates check both owner poses: Tab activation, a reverse camera turn and pitch, no immediate actor mutation, one queued transition, horizontal camera-relative velocity, reflected pitched camera basis and reverse-facing portal visibility. Large vertical motion clamps pitch; Escape, focus loss and pause ignore subsequent motion and clear the logical look mode. Existing physical keys, multi-door timing/ownership, transformed placement and guarded closing gates still pass. A bounded scrollable panel keeps controls reachable. The inspected final capture shows original textured corridor, two visible rooms and held denied-entry second leaf.

Confidence: Confirmed modern view/timeline/event boundary and rendered camera placement. Self-tests suppress physical OS cursor capture, so interactive mouse grabbing has not been manually exercised. Original camera control, root motion, vertical locomotion and world collision remain separate. No score changes.

## Next action

Inspect the current scene's authored CELL collision bounds and appearance collision metadata. Establish a bounded reusable collision-mesh decoder only if exact structure is supported; do not substitute visual surfaces for authoritative collision without labeling the policy.
