# Pinned interior scene asset bundle

`InteriorDoorSceneBundleReader.Load` consumes an explicitly authored JSON selection with `member`, `layout_sha256` and `doors` (each `cell`, `portal`, `leaf_sha256`). The specification bytes have their own required SHA-256 pin. This JSON is a modern inspection selection, not recovered original protocol or asset format.

One to sixteen physical door selections are permitted, within 16,384 specification bytes and JSON depth eight. Each selected doorway uses the established [single-leaf bundle](PERSISTENT_DOOR_ACTOR_CONTRACT.md), pinned table/layout/leaf, uniquely paired floors and fallback rejection. Duplicate physical geometry rejects even when the opposite reference was selected. All selections use the same pinned POB.

A shared normalized source cache resolves each asset once across selections, with the existing 256-resource / 128 MiB source-byte limits. The bundle returns read-only selected doorways, merged loaded floors, explicit actor bindings, reconciled source identities and specification hash. `CreateActor` applies the [scene construction](INTERIOR_DOOR_SCENE_CONTRACT.md) constraints, including complete binding of eligible paired doors among the loaded floors.

[Bundle acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_BUNDLE.md) verifies original identities, one lookup per normalized asset, duplicate and bad leaf rejection, malformed shape/bytes/pin rejection before lookup, and unchanged 2,880-tick independent original scene behavior. The bundle selects loaded floors; it does not discover a complete world or choose player spawn/navigation policy.
