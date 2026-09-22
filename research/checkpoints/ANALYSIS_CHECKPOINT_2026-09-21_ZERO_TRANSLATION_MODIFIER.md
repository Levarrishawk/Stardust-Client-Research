# ZeroTranslationTransformModifier closure

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## RTTI inventory result

An exhaustive RTTI-name search for `TransformModifier` identifies exactly five concrete/abstract names in this executable:

| Class | Vtable |
|---|---:|
| `TransformModifier` | `0x015E555C` |
| `AlignmentTransformModifier` | `0x015E554C` |
| `RiderSpineTransformModifier` | `0x015E0E70` |
| `LookAtTransformModifier` | `0x015E5394` |
| `TargetPitchTransformModifier` | `0x015E53A8` |
| `ZeroTranslationTransformModifier` | `0x015E53B4` |

The last class was the missing concrete subclass in the prior pose-modifier checkpoint.

## Confirmed behavior

- Constructor `0x007D9270` creates the base modifier and installs the `ZeroTranslationTransformModifier` vtable.
- Apply method `0x007D92D0` copies the supplied 48-byte transform, replaces its translation components with the shared zero vector, and multiplies it with the incoming transform, returning success.
- Its only direct construction site is the general skeletal-appearance modifier setup routine `0x004355B0`.
- Setup attaches it through the modifier registry `0x007CA520` to the exact transform name `root`.
- Attachment is conditional on a byte at skeletal appearance `+0x98D` being false and a pointer/state at `+0x990` being non-null. The accessors are trivial; their unpublished semantic names cannot be proven from this build.

## Behavioral interpretation

The modifier removes the local/root animation translation before composing the pose. This prevents selected skeletal assets or controller modes from applying root-motion displacement through the bone hierarchy while leaving orientation intact. Object/world locomotion remains owned by the scene/controller transform.

A replacement animation adapter should expose an explicit `suppressRootTranslation` pose stage, attach it to the asset-contract bone `root`, and activate it under the equivalent appearance/controller policy. It should not encode legacy object offsets or raw pointers.

## Confidence

- Class/vtable, one construction site, `root` target, transform copy, zeroed translation and multiplication: **confirmed**.
- Root-motion-suppression description: **strongly inferred from exact math**.
- Names of appearance `+0x98D/+0x990` predicates: **unknown**, but their truth table is documented and bounded.

