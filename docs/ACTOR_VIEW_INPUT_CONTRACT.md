# Camera heading and actor input

`ActorViewOrientation` is an immutable modern heading/pitch value. Heading wraps to the signed full-turn remainder; pitch clamps at ±85 degrees. Finite angles and deltas are bounded by one million radians. A finite nonzero horizontal reference direction is normalized, then heading rotates about native +Y. `PlanarForward` supplies movement heading; `LookForward` adds camera pitch. Pitch never introduces vertical locomotion.

The scene applies the shared owner transform to both directions, then reflects Z at the prototype host boundary. Mouse relative X turns heading positively, relative Y turns pitch negatively, at 0.0025 radians/pixel. The host currently rejects nonfinite or above-4,096-pixel event components. Every accepted turn submits the current normalized planar keyboard velocity to the existing timestamped timeline. Simulation retains tick-start sampling; camera orientation updates on rendering.

Mouse look is available in continuous interior view. Tab toggles it; Escape releases it. Pause, focus loss, manual mode, reset, owner reconfiguration, overview and exit release capture. Captured input commands prototype host's captured mouse mode; release commands visible mode. Bounded self-tests exercise actual dispatched motion/key events while suppressing OS capture, so interactive OS grabbing has not been manually verified. Mode restoration does not replay suspended motion.

Controls use a scrollable panel bounded to the viewport. Camera/visibility toggles preserve actor state. Owner changes and explicit reset restore initial heading/pitch. Manual step buttons continue to follow the authored route; continuous keys follow camera heading.

[Acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ACTOR_VIEW_INPUT.md) passes native axis/wrap/clamp/ownership negatives, two render partitions with a 35ms turn, and original headless/GPU event/camera/visibility/focus/pause gates in both owner poses.
