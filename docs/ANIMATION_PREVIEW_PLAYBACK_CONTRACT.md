# Bounded original animation preview playback

`AnimationPreviewClock` is an engine-neutral modern preview clock. It accepts explicit authored FPS/end frame and declared positive time-scale product, with finite bounded inputs. It exposes play/pause, focus, frame seek, loop or one-shot, current frame and completed loops. No original controller, priority, event or root-motion execution is implied.

Looping wraps at EndFrame; one-shot holds EndFrame and stops. Seek includes both endpoints and does not implicitly change play state. Pause/focus loss preserves frame and play intent; focused elapsed time resumes without accumulating paused time. Invalid elapsed/seek requests reject before frame mutation. Elapsed input range is0..60 seconds; rate≤1,000 and scale≤1e6 are modern bounds. The prototype host adapter discards stall elapsed beyond0.25 seconds per frame for smooth inspection. Focus is seeded from the window and updated by focus notifications.

## Host controls

The selected original standalone character inspector adds Play idle and a frame scrubber. `-CreatureAnimationPlay` starts playing; otherwise the supplied selected frame remains frozen until Play is enabled. Every changed frame uses the prepared clip → sampled local pose → attached world hierarchy → full-weight deformation → main-thread upload boundary. Disposal/failure clears frame callbacks before releasing meshes. Synthetic diagnostic/bind mode remains available when no clip is selected.

## Verification

Native clock fixtures check480 fixed ticks/two wraps, endpoint seek/wrap, pause/focus without catch-up, explicit2x scale, one-shot stopping and malformed inputs with unchanged state. The original selected idle headless/GPU fixtures perform480 uploads through two four-second cycles, periodic complete readbacks, endpoint/interior/reverse seeks and bind reset. Pause/focus are deterministic calls, not a claim of manual OS interaction.

Actual prototype host play/scrub control signals exercise their handlers, advance one frame, hold on focus/pause, seek the clip end and restore the selected capture frame. Full GPU readback passes. Updated frame30 capture was visually inspected with clear head/body framing, aligned attached head and neutral materials. Local GPU fixture mean2.464 ms per pose update includes deformation/surface submission and periodic readback; it is not a gameplay frame-rate promise.15-source original closure and prior independent pose/vertex audits are unchanged.

This is bounded timed playback of one explicitly selected original clip. Complete animation graphs, blending/priority/events/root motion, facial animation, materials/customization and original controller lifecycle are pending. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_ANIMATION_PLAYBACK.md), [selection/launcher](ORIGINAL_ANIMATION_POSE_PREVIEW_CONTRACT.md). Next: use the same prepared clip and modern clock for the persistent interior character, with source union and actor-foot/root-motion separation verified.


## Fractional endpoint precision

Frame accumulation is compensated. Positive nearest loop boundaries snap within eight double machine epsilons scaled by max(endFrame,absolute nextFrame); seek resets compensation. This explicit modern numerical equality policy fixes reproduced fractional-duration loop/one-shot undercount, while materially incomplete frames remain incomplete. Six durations and three scales pass native two-cycle/one-shot regressions and selected original GPU/interior host gates. [Precision checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ANIMATION_CLOCK_PRECISION.md).
