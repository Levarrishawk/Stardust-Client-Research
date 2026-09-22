# SunTrackingObject runtime and SUNT angular fields

Date: 2026-09-22

## Result

RTTI identifies vtable `0x015E203C` as `SunTrackingObject`. Constructor
`0x006FF6C0` stores the SUNT first angular scalar at object `+0xA0`; setup
`0x006FF7B0` stores the child panel and the other three scalars at
`+0xA4/+0xA8/+0xAC/+0xB0`. Per-frame alter is `0x006FF800`.

The four authored values map as follows:

| SUNT scalar | Runtime field | Behavior | Confidence |
|---:|---:|---|---|
| 1 | parent `+0xA0` | turntable yaw slew rate, radians/second | Confirmed |
| 2 | parent `+0xA8` | authored lower panel-pitch limit | Strong; dormant in this runtime |
| 3 | parent `+0xAC` | authored upper panel-pitch limit | Strong; dormant in this runtime |
| 4 | parent `+0xB0` | panel pitch slew rate, radians/second | Confirmed |

The two limit labels are supported by the deployed `-10,+10` pair and their
position between resource attachment and the final 10-degree/second rate, but
no method in the concrete runtime reads `+0xA8` or `+0xAC`. They must therefore
be preserved as authored constraints without claiming the original client
enforces them.

## Per-frame behavior

After base `Object::alter`, an enabled tracker obtains the current environmental
sun/light direction and transforms it into turntable-local space.

1. Desired yaw is `atan2(localX, localZ)`.
2. The applied yaw delta is clamped to
   `[-deltaTime * yawRate, +deltaTime * yawRate]`.
3. The turntable rotates about local Y.
4. The direction is transformed into panel-local space.
5. Desired pitch is `atan2(-localY, sqrt(localX^2 + localZ^2))`.
6. The applied pitch delta is clamped to
   `[-deltaTime * pitchRate, +deltaTime * pitchRate]`.
7. The panel rotates about local X.

The object marks transforms dirty after each nonzero rotation and returns the
ordinary next-frame alter result. There is no snap-to-sun operation and no
read of the stored lower/upper pitch limits in this executable.

## Replacement-client consequences

- Preserve all four file scalars, including the dormant limit pair.
- Reproduce the two-stage local-space yaw-then-pitch tracking and rate limits.
- For strict legacy behavior, do not clamp pitch to scalars 2/3. A future
  opt-in fidelity fix may enforce them, but it must be labeled a behavior
  change rather than silently attributed to Stardust.exe.
- Source the direction from the world/environment directional-light service.

## Confidence

- RTTI, object construction, yaw/pitch equations, axes, and scalar 1/4 rate
  meanings: **confirmed by code**.
- Scalar 2/3 lower/upper limit labels: **strongly inferred from deployed data**.
- Scalar 2/3 non-use by this concrete runtime: **confirmed by code**.

