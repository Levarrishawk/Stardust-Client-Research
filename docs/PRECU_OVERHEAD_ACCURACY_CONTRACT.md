# Original overhead accuracy estimate

Bounded question, October 16–17, 2026: why does the conversion show `+50%`
beside every NPC, and what should the original Accuracy widget display?

## Identity and evidence

Static observations use Stardust.exe SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Addresses are baseline VAs. The conversion's effective original GroundHUD
sampleStatus contains Accuracy/text with authored `+50%`; its NGE
SwgCuiStatusGround does not bind that child. Cloning the sample preserves the
literal. This is Confirmed placeholder leakage, not a server percentage.

The original all-target renderer 00FD5F80 looks up `accuracy` at 00FD65FF.
00FD6613–00FD6654 requires an attackable tangible and a match against either
the player's cached look-at ID or the first inherited defender ID. The
look-at member is registered at +588 with its cached value at +598; the
defender vector starts +2F0, with begin/end at +2FC/+300. Empty defenders use
an invalid ID. See [CREO6 prefix](CREATURE_VIEW6_PREFIX_CONTRACT.md) and
[replication schema](REPLICATION_SCHEMA_MAP.md). Other objects hide the
Accuracy page at 00FD6791. Structure and comparisons are Confirmed; the two
field names are Strongly inferred from registration and existing schemas.

00FD6672–00FD66EC obtains world positions, computes 3D distance and subtracts
the target's appearance sphere radius (00B23040). Wrapper 010A9C70 forwards
to estimate helper 010A9560. Success displays signed integer
`truncate((chance - 0.5) * 100)` followed by `%`; failure displays `---`.
00FD66F8–00FD6719 and literal references 018D28D0/DC establish this behavior.
It is an accuracy adjustment relative to 50%, not the absolute chance and
not an authoritative Core3 hit prediction. These arithmetic/display facts
are Confirmed. No original dynamic execution is claimed.

## Calculation

010A9560 clamps a negative distance to zero and requires the attacker
controller. Start at F32 0.35; state bit 3 adds F32 0.25. Aiming semantics
are Strongly inferred from the shared state enumeration. If a weapon exists,
010A9520 adds its integer range accuracy multiplied by F32 0.01. Missing or
out-of-range weapon evaluation fails. No weapon skips that contribution.
The ranged table is selected only for GOT 0x20002, 0x2000A, 0x2000B or
0x2000C; all other types use the other table. Do not broaden this to every
weapon described as ranged by a later game version.

00709DC0 uses the five original weapon values: ideal/max range and
point-blank/ideal/max accuracy. For finite, nonnegative ordered ranges:

- At 0 through ideal: blank plus truncation of
  `(idealAccuracy - blank) / idealRange * distance`; zero ideal returns blank.
- Above ideal through max: ideal accuracy plus truncation of
  `(maxAccuracy - idealAccuracy) / (maxRange - idealRange) * (distance - ideal)`.
- Beyond max: failure. The upper segment's equal-range guard returns max
  accuracy, though ordered finite inputs reach the lower segment at that point.

Truncation is toward zero on the interpolation increment, before adding its
integer endpoint. Target distance is not rounded. Float precision at integer
boundaries needs explicit fixtures; bit-exact x87 arithmetic is not promised.
The shared conversion helper 0131E27C saves the x87 control word, sets rounding
control to 11, performs FISTP and restores the original word. This directly
confirms truncation rather than inferring it from the compiler helper's name.

Controller vtable +30 supplies current speed. 00431920/00438950 supplies the
walk threshold (Strongly inferred by speed field and mounted-state handling,
correlated with source getWalkSpeed(false)). Moving means speed > 0; running
means speed > walk threshold. 010A9460 selects posture/motion contribution:

| Posture group (numeric value) | Table entry |
|---|---|
| Upright 0, blocking 4, flying 6, riding 11, knocked-down 12 | running ? 3 : moving ? 2 : 0; flying additionally adds 8 |
| Sneaking 3 | 1 |
| Crouched 1, climbing 5, sitting 8, driving 10 | moving ? 5 : 4 |
| Prone 2, lying-down 7 | moving ? 7 : 6 |
| Skill-animating 9, incapacitated 13, dead 14, invalid | zero |

The switch bytes are Confirmed; posture names correlate the source enum.
Entries are F32, in index order 0–8:

| Contribution | Values |
|---|---|
| Attacker ranged, 018D6D4C | 0, -.06, -.2, -.3, .15, 0, .3, -.1, -.1 |
| Attacker other, 018D6D70 | 0, -.2, -.5, -.15, .1, 0, -.3, -.4, -.1 |
| Defender ranged, 015F8E1C | .1, -.05, -.05, -.15, .05, 0, -.15, -.05, .1 |
| Defender other, 015F8E40 | 0, .05, .05, .1, .1, 0, .3, .45, .4 |

Add attacker contribution; add defender contribution only if the target is
a creature with a controller. Clamp the final result to [0,1]. These tables
and arithmetic are Confirmed by static reads and call arguments. No skill,
armor or server defense lookup occurs in this helper; it is a local estimate.

## Replacement boundary and intentional validation

Keep a pure policy over copied weapon, posture, state and motion values.
The UI adapter supplies object identity, geometry and lifecycle-safe model
reads; it does not change combat, targeting messages or weapon replication.
The conversion has split intended selection/local hover: use intended
selection plus local look-at and primary defender as visibility candidates,
explicitly a compatibility mapping rather than an original third ID.

Hide unselected/nonattackable pages. Clear the authored sample when binding.
For an eligible target with missing required model/range/geometry, show `---`.
Nonfinite inputs, negative motion thresholds, reversed/negative range shapes
or unrepresentable integer interpolation fail deterministically. These are
modern display validation rules, not claims about legacy malformed behavior.
They must not narrow the separately documented five-value wire codec.
The legacy missing-attacker-controller path has an anomalous nonzero AL
without writing the result; do not preserve that undefined display accident.

Positive and negative synthetic fixtures must cover endpoints, degeneracy,
integer truncation, negative modifiers, posture/motion combinations, clamping,
missing values, malformed floats, identity changes and output atomicity.
Live acceptance must distinguish no-target, selected target, movement and
target-clear behavior. No whole-gate or coverage-score increase follows.
