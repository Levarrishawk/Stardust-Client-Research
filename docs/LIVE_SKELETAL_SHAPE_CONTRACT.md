# Live original skeletal shape preview

`SkeletalAppearanceShapes.Apply` consumes an exact explicit shared-owner selection across all owned mesh descriptions. Each mesh receives its relevant subset. Every edit evaluates from original geometry, preserving geometry/source identities and producing owned selected arrays. Input profile uses the existing strict version1 schema with factoryFlagfalse; the authored human zero profile is a diagnostic selection, not recovered defaults.

Godot `-CreatureShapeProfile` loads descriptors and initial selection on the existing asset worker before main-thread publication. Shape controls use a scrollable Shape tab alongside Colors. Individual controls recompute from original data and reapply the retained current local pose. Existing ArrayMesh/material objects, UVs, topology, weights, animation preparation and textures persist. Bind reset refers to selected shape geometry. Live control evaluation is synchronous and bounded to65,536 sparse deltas; the selected original pair has9,314. No archive access occurs during edits.

All CPU shader arrays validate before replacing surfaces. Invalid topology/identity rejects before publication; failed numeric preparation restores previous selected sources/pose. Main-thread shape publication changes geometry/bounds intentionally and preserves root placement. General driver allocation-failure recovery and asynchronous high-density shape controls remain future work.

Original standalone GPU/headless/cancellation checks pass with74 sources/5,927,038 bytes. All27 controls at255, repeated selection without accumulated deltas, exact reset at retained sampled frame30, unchanged topology/material/texture identities and rejected/failed-edit preservation pass. Existing480-tick idle/color/face checks remain green. Capture inspected. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LIVE_SKELETAL_SHAPES.md). Interior integration is next.


## Interior integration

`-SceneCharacterShapeProfile` requires an owned pinned scene character. The strict authored profile is parsed on the scene worker; the shared source cache loads blend descriptions and evaluates initial shapes before publication. Live edits use Colors/Shape tabs within the existing scrollable interior controls. Root placement remains the authoritative actor feet; shape changes never alter collision dimensions, floor locator, door states or owner transforms.

Original139-source15,541,123-byte single-read closure is unchanged. Zero/varied initial profiles and all27 maximum/repeat/reset/failed-edit GPU checks pass with continued original idle playback and preserved actor/root state. The varied native scene independently agrees on26,484 vectors across six selections (five diagnostics plus authored profile), maximum2.1351779699863016e-7. Existing2,880 ticks/24 transfers/182 contacts and reflected floor/owner invariants pass. These floor checks do not independently reimplement the dynamic door controller. [Interior checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_SKELETAL_SHAPES.md).
