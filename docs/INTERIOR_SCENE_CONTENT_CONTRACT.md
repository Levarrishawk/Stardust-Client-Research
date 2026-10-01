# Owned interior scene content

`InteriorSceneContentBundleReader` combines the pinned actor scene and optional selected room geometry/material loading behind one owned normalized source cache. The caller supplies archive resolution, specification/table pins and an explicit layout member/pin. Layout member must equal the normalized member retained by `InteriorDoorSceneBundle.LayoutPath`; substituting another already-loaded asset rejects even when its hash matches.

Materials require appearances. Only loaded floor-cell appearances are selected; floor/path loading through the appearance reader is disabled. Default global input limits are 256 attempted resources and 128 MiB of successful source bytes; depth/edge limits also pass to the geometry reader. Material decoded-pixel limit remains separate. All stage source identities are reconciled against the shared cache. Returned scene/room/material objects are decoded owned data; each source is externally resolved once even across stage boundaries.

The scene host consumes this bundle. [Acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_CONTENT.md) independently reconciles room source closure with the original actor source closure: 61 unique successful assets / 9,262,168 bytes and one external lookup each, preserving the 2,880-tick scene trace. Malformed specification/options, substituted layout and count/byte exhaustion reject. Exact decoder/rendering policies remain in their own contracts.

## Shared visual composition

The current bundle also supplies a synthetic geometry root: child0 room group, child1 explicit leaf-instance group. Room CELS ordinals and leaf selection order remain distinct. A single material graph attaches to rooms and all leaf LOD templates, using one decoded-content cache and aggregate decoded-pixel budget. [Shared visual acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_SCENE_VISUALS.md) independently verifies the expanded 65-source / 9,614,085-byte union and five templates / 28 shared decoded textures. The earlier 61-source count is the dated room-only stage.


## Pinned actor profile

Optional paired `actorTemplatePath`/`actorTemplateSha256` inputs resolve a deterministic inherited `SharedCreatureTemplateProfile` through the same source cache. Pair, object-root path and SHA format validate before lookup. Returned `ActorProfile` owns decoded values/provenance; character appearance reference is retained but not instantiated. Profile identities reconcile against the shared source registry.

The original profile union has70 unchanged sources/9,622,375 bytes, each externally resolved once. This is the prior65 visual sources plus five original human templates/8,290 bytes. All effective source pins and the original sphere-driven frame invariants pass independent audit. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_PROFILE_PREVIEW.md).


## Explicit modern room-triangle sphere guard

Optional `InteriorSceneTriangleGuard` owns direct static-mesh room triangles selected by explicit collision selector0/1. Source/retained capacity and cell limits are enforced before publication. Content creation requires actor profile, appearances and materials; the original host selects selector1/capacity65,536 using `-SceneTriangleGuard`. All42,986 original triangles and the70-source union agree independently.

The sphere scene preflights static work before controller mutation, compares static and captured door/barrier progress, and validates moved landings against both obstacle sets. Door contact wins exact ties; unsupported combined landing retains initial owner/position. No stepping/sliding or original actor selection claim follows. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMBINED_SPHERE_SCENE.md) records native positive contacts, original2,880-tick audit, resource atomicity, local timing and headless host acceptance.
