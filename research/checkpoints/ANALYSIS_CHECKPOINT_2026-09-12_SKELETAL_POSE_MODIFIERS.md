# Analysis checkpoint: skeletal pose modifiers and yaw selection

Date: 2026-09-12

## Scope

This block follows animation-controller output into three character-pose behaviors in `Stardust.exe`: signed yaw animation selection, look-at yaw, and target pitch. It also identifies the skeletal-appearance routine that attaches the modifiers to named transforms.

## Confirmed findings

### Yaw animation template and runtime

`YawSkeletalAnimationTemplate` installs its typed factory at `0x0080E610`. The binary loader at `0x0080EB30` accepts version `0000`; `0x0080ED20` conditionally loads three nested skeletal-animation templates. The writer at `0x0080E7A0` emits the same three child roles and the XML labels identify them as:

- `yawLeftChoice` at template offset `+0x18`;
- `yawRightChoice` at `+0x1C`;
- `yawNoneChoice` at `+0x20`.

The outer XML element is `yawAnimation`. The on-disk tag immediate is `0x59574154`; its raw little-endian bytes are `TAWY`. This report records the immediate and bytes rather than silently normalizing the historical FourCC convention.

Runtime construction at `0x00839C40` instantiates the available child animations, creates a timed two-animation blend delegate at `+0x1C`, resolves the installed animation-environment variable `yaw_dir` into `+0x24`, and installs vtable `0x015E616C`.

Selection at `0x00839FA0` is exact:

| `yaw_dir` value | Selected child |
|---:|---|
| less than zero | `yawLeftChoice` (`+0x10`) |
| greater than zero | `yawRightChoice` (`+0x14`) |
| zero | `yawNoneChoice` (`+0x18`) |

The selected child is changed only when its pointer differs from the cached selection at `+0x20`. The runtime starts the selected animation and asks the blend delegate (`0x0083FF10`) to transition to it using an installed blend-time value. All ordinary skeletal-animation virtual operations then forward through the delegate at `+0x1C`. This is a discrete signed selector with cross-fading, not a continuous yaw-angle blend.

### Look-at yaw modifier

`LookAtTransformModifier` installs at `0x007D0700`; its constructor is `0x007D07B0`, vtable `0x015E5394`, and per-pose application path `0x007D0820`. Configuration is under `ClientSkeletalAnimation/LookAtModifier` with `disableModifier` plus global client-skeletal-animation limits. The installed defaults observed in the executable include a 70-degree yaw clamp and a maximum turn speed of 600 degrees per second.

The modifier:

1. resolves an explicit target/dependency when supplied, otherwise derives a look direction from the owning appearance/object state;
2. transforms that direction into the character's local frame;
3. computes yaw with `atan2`, handles angular wrapping, and clamps it to the configured left/right limit;
4. rate-limits the change by elapsed time and the configured turn speed;
5. composes the resulting rotation into the incoming skeletal transform;
6. caches the frame number so a frame is not recomputed twice.

The broad behavior, clamping and rate limiting are confirmed. Some object-accessor names in the target fallback path remain provisional.

### Target-pitch modifier

`TargetPitchTransformModifier` installs at `0x007D8ED0`; its constructor is `0x007D8F80`, vtable `0x015E53A8`, and application path `0x007D8FE0`. Configuration is under `ClientSkeletalAnimation/TargetModifier` with `disableModifier`. The shared target-pitch default is 45 degrees.

When enabled, the modifier validates that the owner has a distinct live target, computes the relative target vector in owner space, derives pitch from vertical displacement and horizontal magnitude, clamps to the configured pitch range, and composes the rotation into the supplied transform while preserving translation. It returns false when no valid target exists.

### Skeletal-appearance attachment

All direct constructor call sites converge at `0x004355B0`, a skeletal-appearance modifier setup routine. The static name initialization proves the relevant transforms are `spine3`, `neck`, and `head`; the same region also contains skeleton identities `appearance/skeleton/all_b.skt` and `appearance/skeleton/ithorian.skt` used by setup-time compatibility tests.

The routine conditionally installs `TargetPitchTransformModifier` on `spine3`. It then checks the active skeleton's transform names for `neck` or `head`, creates two `LookAtTransformModifier` objects, links the head-side instance to the neck-side instance, and attaches them to `neck` and `head`. This establishes a chained head/neck look-at solution rather than rotating a single bone.

The modifier registry/attachment call is `0x007CA520`. Exact labels for its two parameters beyond transform name plus modifier ownership should remain provisional until its container type is fully mapped.

### Alignment and rider-spine modifier

RTTI also resolves abstract `AlignmentTransformModifier` to vtable `0x015E554C` and `RiderSpineTransformModifier` to vtable `0x015E0E70`. The latter derives the former and reuses its application routine at `0x007EAC50`.

`AlignmentTransformModifier` stores a configured local axis at `+0x04..+0x0C` and the cosine of a one-degree tolerance at `+0x10`. Its apply path asks a derived virtual method for the desired alignment vector, composes the owning object and current skeletal-transform orientations, compares the transformed configured axis with the desired vector, and returns without modification when their dot product is within the tolerance. Otherwise it constructs and composes the shortest corrective rotation. This is a general post-pose vector-alignment primitive.

`RiderSpineTransformModifier` construction at `0x00679C30` changes the vtable but retains the alignment algorithm. Its virtual desired-vector method `0x00679C90` returns a fixed global direction. Skeletal-appearance setup at `0x004355B0` conditionally constructs this modifier and attaches it to the confirmed `spine1` transform. The conditional path includes a mounted/rider-style state test; the exact source-level predicate name and numeric state flag are not yet proven.

## Animation-environment contract

Embedded installed variable names now confirmed around this system are `loco_vel`, `tgt_dir`, `apr_scale`, `yaw_dir`, `skill`, `gender`, and `mounted_creature`. These are compatibility-facing semantic inputs to animation selection and should be represented as typed names in the replacement architecture rather than hard-coded engine node paths.

## Replacement-client implications

A replacement can implement these behaviors with an engine animation graph, but it must preserve a compatibility layer above it:

- signed `yaw_dir` chooses left/right/neutral animations and triggers a timed cross-fade;
- look-at yaw is local-space, clamped and angular-rate-limited;
- target pitch is local-space and clamped;
- head and neck modifiers form an ordered chain;
- rider posture can apply a tolerance-gated corrective alignment at `spine1`;
- skeleton transform names are asset contracts and missing transforms are tolerated;
- modifier-disable configuration remains observable behavior.

Godot, DX12, or another renderer does not change these requirements. They belong to skeletal evaluation and original-asset compatibility, not the graphics API.

For a native 64-bit implementation, child animation references, modifier ownership and bone handles must use native safe handles/pointers internally. The `yaw_dir` value, FourCC tags, serialized child order and asset transform names remain fixed-width compatibility data.

## Zero-translation follow-up

`ZeroTranslationTransformModifier` is the remaining RTTI-named concrete modifier. It is attached to `root` by the same setup routine and removes translation from the supplied local transform before composition, implementing conditional root-motion suppression. An exhaustive RTTI-name inventory found no additional classes whose names contain `TransformModifier`. See `ANALYSIS_CHECKPOINT_2026-09-21_ZERO_TRANSLATION_MODIFIER.md`.

## Confidence and remaining unknowns

- **Confirmed by code:** loader version, three child roles/order, signed selection, cached-change behavior, blend delegation, modifier classes/call sites, bone names, clamp defaults, local-space angle construction, chained head/neck attachment, and rider-spine vector-alignment algorithm.
- **Strongly inferred:** `0x004355B0` is the general skeletal-appearance modifier setup pass and the target-pitch installation condition represents player/creature eligibility.
- **Unknown:** semantic name of every setup predicate, exact blend-time configuration label/value, complete target fallback ownership, and the state identifier tested before target pitch. The RTTI-named modifier class inventory and root zero-translation stage are now closed.

## Stable resume point

Next modifier work should identify appearance-specific subclasses beyond the RTTI-named look-at, target, alignment and rider-spine set. The present block is complete: asset template, runtime selection, pose modification, and skeletal attachment are connected end to end.
