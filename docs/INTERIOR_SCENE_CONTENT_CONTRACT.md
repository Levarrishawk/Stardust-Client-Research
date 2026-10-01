# Owned interior scene content

`InteriorSceneContentBundleReader` combines the pinned actor scene and optional selected room geometry/material loading behind one owned normalized source cache. The caller supplies archive resolution, specification/table pins and an explicit layout member/pin. Layout member must equal the normalized member retained by `InteriorDoorSceneBundle.LayoutPath`; substituting another already-loaded asset rejects even when its hash matches.

Materials require appearances. Only loaded floor-cell appearances are selected; floor/path loading through the appearance reader is disabled. Default global input limits are 256 attempted resources and 128 MiB of successful source bytes; depth/edge limits also pass to the geometry reader. Material decoded-pixel limit remains separate. All stage source identities are reconciled against the shared cache. Returned scene/room/material objects are decoded owned data; each source is externally resolved once even across stage boundaries.

The scene host consumes this bundle. [Acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_CONTENT.md) independently reconciles room source closure with the original actor source closure: 61 unique successful assets / 9,262,168 bytes and one external lookup each, preserving the 2,880-tick scene trace. Malformed specification/options, substituted layout and count/byte exhaustion reject. Exact decoder/rendering policies remain in their own contracts.

## Shared visual composition

The current bundle also supplies a synthetic geometry root: child0 room group, child1 explicit leaf-instance group. Room CELS ordinals and leaf selection order remain distinct. A single material graph attaches to rooms and all leaf LOD templates, using one decoded-content cache and aggregate decoded-pixel budget. [Shared visual acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_SCENE_VISUALS.md) independently verifies the expanded 65-source / 9,614,085-byte union and five templates / 28 shared decoded textures. The earlier 61-source count is the dated room-only stage.
