# Opt-in authored movement resources

The engine-neutral creature resource closure can retain authored movement evidence alongside its appearance, geometry, prepared clips, materials, and optional albedo resources. `SceneCreatureResourceOptions.LoadAuthoredMovementInputs` defaults to `false`. When enabled, `SceneObjectResources.Creature.Creature.AuthoredMovement` exposes a `CreatureAuthoredMovementResources` containing the frozen movement profile and authored snapshot, with their logical decoded-byte and work totals.

This is evidence retention. No speed, unit, controller formula, host behavior, locomotion seed, or runtime default is selected here. Unsupported authored parameter encodings remain Unsupported, without inferred numeric values.

## Loading and ownership

`SceneObjectResourceRouter` passes the copied option and policy through `SceneCreatureResourceReader` to `CreatureAppearanceDescriptorBundleReader.Load`. `SceneObjectResourceRequest.CopyOptions` preserves both. The bundle loader invokes the existing `SharedCreatureMovementInputProfile.Load` and `SharedCreatureAuthoredMovementInputs.FromProfile` implementations, using the same outer `OwnedAssetSourceCache.Read` as the appearance closure.

The existing collision-profile traversal already reads the pinned human root and four necessary parents. Movement traversal reuses those cache entries. A retained movement source has the same source identity as its corresponding outer resource source; movement does not add its overlapping source bytes to the outer total. The frozen profile and snapshot own their encodings independently. Their logical retained sizes are both charged, even where the snapshot derives from the profile. These sizes are accounting measures, not measured CLR heap usage.

For the pinned human route tested in [the checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R49_MOVEMENT_RESOURCE_CLOSURE.md), enabling movement adds zero external reads: the albedo/prepared closure remains 39 distinct sources and 4,686,371 source bytes. The five overlapping template sources comprise 8,290 bytes. Retained movement decoding contributes 21,018 logical bytes and 8,467 work units. The existing prepared route without albedo remains independently verified at 37 sources and 4,683,848 bytes.

## Policies and failure behavior

`CreatureMovementResourceLimits` copies and validates its nested profile and snapshot policies before any resolver call. Supplying a movement policy while the flag is false is rejected, rather than silently ignored. Enabling the flag without a policy uses the frozen defaults. The combined movement decoded limit defaults to 12 MiB and accepts 1 through 68 MiB. Individual frozen parser and snapshot limits remain enforced; the profile receives at most the combined cap, then the snapshot receives the remaining allowance. An exhausted remainder fails before snapshot construction. The reader also includes retained movement bytes in its existing aggregate decoded-resource budget alongside unique DDS mip data.

Profile limits retain their frozen hard ranges: depth/resources 1–16, template bytes 1–1 MiB, total source bytes 1–16 MiB, parameter bytes 2–65,536, nested references 1–16, array elements 1–64, and decoded bytes/work 1–64 MiB. Snapshot limits are sources 1–16, evidence records 1–80, raw bytes 0–1 MiB, and snapshot bytes/work 1–4 MiB.

An enabled, already-cancelled request fails before source access. Cancellation during inherited loading or before publication fails without returning a partial resource result. Missing or masked parents, inheritance cycles, root pin mismatches, and source or decoded limits remain errors. Exact limits accept the demonstrated closure; one-byte undersized limits reject it. The existing aggregate image accounting remains checked after image decoding; the new movement profile and snapshot have their own bounded allocation policies.

False/default behavior is verified by comparing the appearance, geometry, source identities, selection/preparation, and albedo mip fingerprints against the enabled route. Only the optional authored evidence changes.

## Reproduction and next boundary

[Invoke-MovementResource.ps1](../poc/round49-tests/Assets/Invoke-MovementResource.ps1) builds the actual core offline from cached dependencies, generates the frozen synthetic fixtures, regenerates the independent static original-data oracle, runs the new resource scenarios and existing prepared/albedo scenarios, compares their results, and runs the default smoke executable. All generated output is restricted to a fresh ignored private directory. It requires explicit original executable, installed assets/configuration, server source, Python/static-analysis packages, and cached NuGet paths.

The result applies to one pinned human route plus synthetic ownership, malformed-input, cancellation, and budget cases. It does not establish all creature templates or final runtime arbitration. The next probe is an explicitly enabled host consumer that names its interpretation policy and preserves Unsupported/Unknown evidence when selecting movement behavior.
