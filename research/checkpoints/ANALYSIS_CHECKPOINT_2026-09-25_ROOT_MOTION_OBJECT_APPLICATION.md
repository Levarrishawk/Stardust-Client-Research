# CKAT root-motion object application and basis closure

Date: 2026-09-25. Question A03. Target: verified `Stardust.exe`. Decompiler output is reconstructed pseudocode; transform order is checked against the already recovered scalar multiply backend.

## Result

The animation system's locomotion output is an **object-local/model-local delta**. `CreatureController` applies it to the controlled object's local-to-parent transform after each eligible update:

- accumulated rotation is post-composed with the object's existing local rotation;
- accumulated translation is rotated by the object's existing local orientation into parent space, then added to the object's local translation.

This closes the external application/basis boundary left after recovering the rare combined QCHN+LOCT arithmetic.

## End-to-end path

```text
CKAT selected root evaluator
  -> PhysicalAnimationTrack accumulated {quaternion, translation}
  -> 0x0083EC70 physical-track getter
  -> 0x0082F990 TrackAnimationController getter (vtable slot +0x0C)
  -> 0x007D6A90 controller-aggregate getter
  -> 0x007D7810 appearance/controller wrapper
  -> 0x007CA220 SkeletalAppearance2 forwarding method
  -> 0x00639100 CreatureController root-motion application
  -> controlled Object local transform
```

`0x007D6A90` has one direct caller and dispatches the selected animation controller's virtual slot `+0x0C`. `0x007D7810` supplies identity rotation plus a scale-adjusted fallback translation only when no controller exists. `0x007CA220` is the thin `SkeletalAppearance2` forwarding method. Its only direct executable caller is `0x00639100`.

## Exact application

`0x00639100` runs only when its controller gate is enabled, a controlled object and appearance exist, and the appearance accepts the locomotion query. It obtains `q_delta` and `d_local` through `0x007CA220`.

For non-identity rotation it builds a zero-translation transform from `q_delta`, copies the object's current 3x4 local transform, and calls the confirmed affine multiply contract:

```text
objectLocal' = objectLocal * rotationTransform(q_delta)
```

The existing translation is retained while the rotation change is notified through the Object virtual interface.

For nonzero translation it explicitly computes:

```text
d_parent = R(objectLocal) * d_local
objectLocal.translation += d_parent
```

Helper `0x00B23960` performs the final component-wise addition to Object translation fields and invokes the transform-change callback. Thus the animation delta is not world-space and is not directly copied into object coordinates. It is converted by the object's current local orientation into the parent's coordinate frame.

Outer `CreatureController` update `0x00638810` conditionally invokes `0x00639100` after its ordinary controller/object update. RTTI and neighboring known dispatcher `0x006392E0` corroborate the class owner.

## Meaning for the combined CKAT formula

For the two-channel CKAT mode, the previously recovered translation expression using `q1` produces the model/object-local locomotion delta expected by this application layer. The outer controller then rotates that delta once more by the object's current local orientation before moving it in parent space. A replacement must preserve both stages; treating the CKAT result as world-space or applying `q_relative` in place of `q1` changes motion.

The two installed combined-mode clips remain strongly inferred dormant in current configured content, but their exact compatibility contract is now complete enough to implement without a live fixture.

## Confidence

- **Confirmed by code:** getter chain, sole direct caller chain, affine multiplication order, translation matrix multiply, parent-space addition, and Object notification calls.
- **Strongly inferred:** historical friendly labels for individual methods and the `CreatureController` eligibility gate's original name.
- **Unknown but non-blocking:** original C++ member spellings and whether future/indirect content selects the two dormant combined clips.

Ignored reproduction evidence: `local-private/generated-evidence/a03_locomotion_xrefs.tsv`, `a03_indirect_call_disp_0c.tsv`, and `local-private/ghidra/decompiled/a03-application/`.
