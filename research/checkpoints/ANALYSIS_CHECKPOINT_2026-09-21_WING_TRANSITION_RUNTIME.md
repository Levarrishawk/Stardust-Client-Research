# Animated wing transition runtime

Date: 2026-09-21

## Result

This block follows the `WING` record into its live dynamics controller and resolves the previously open duration-versus-rate question.

`ClientDataFile_applyToObject_VA_00604420` allocates the 0x50-byte controller through `0x006FFA10`, passing the attached child object, configured target angle, second scalar and optional cached sound template. RTTI/vtable behavior is consistent with an object dynamics controller.

## State and update equation

The constructor retains:

- the child's initial local transform;
- target/open angle at controller `+0x3C`;
- transition duration at `+0x40`;
- elapsed transition time at `+0x44`;
- current and requested deployed-state bytes at `+0x08/+0x09`;
- optional transition sound handle at `+0x4C`.

`WingTransitionDynamics_alter_VA_006FFB30` reads a deployment/state bit from the owning ship state. When it changes, the controller starts a transition, clears elapsed time and plays the optional sound once. Each frame it accumulates delta time, clamps completion at the configured duration, and computes:

```text
t = duration != 0 ? elapsed / duration : 0
angle = lerp(oldState ? targetAngle : 0,
             newState ? targetAngle : 0,
             t)
```

It restores the retained base transform, applies the current rotation, and commits the child transform. At completion it adopts the requested state and stops interpolating.

This confirms that values such as `3.0` in X-wing records are seconds, not angular velocity.

## Replacement-client contract

Represent movable wings as a state-driven attachment component with a retained closed transform, configured target angle, duration and optional one-shot sound. Network/gameplay state should request deployed/retracted state; the presentation component should own interpolation. The file's float widths and angle conversion remain fixed compatibility data, while the x64 implementation may use engine-native transforms internally.

## Confidence

- Constructor fields, state-bit observation, elapsed-time accumulation, duration division, angle interpolation and sound trigger: **Confirmed by executable code**.
- User-facing terminology such as S-foils versus generic deployable wings: asset-specific; `WING` itself is generic.
- Exact original class/member spelling: **Unknown**.
