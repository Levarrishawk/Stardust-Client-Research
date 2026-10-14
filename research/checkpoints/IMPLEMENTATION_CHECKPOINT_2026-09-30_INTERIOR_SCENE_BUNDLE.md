# 2026-09-30 reusable pinned scene bundle

## Question and evidence

Can the original multi-door fixture load through one reusable engine-neutral asset boundary with explicit selections, stable identity and shared source lookup? Close after original trace regression and malformed/duplicate/identity gates.

Input specification and original POB/table/leaf pins are unchanged from the [original scene checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md). Tools: native bundle reader/core smoke and [independent original scene verifier](../../../SWG-Godot-Client/tools/python/verify_original_interior_scene.py).

## Confirmed results

The native original producer now uses `InteriorDoorSceneBundleReader`. Every normalized asset is resolved exactly once across both physical door selections. Original source pins reconcile independently. Duplicate physical geometry and bad second leaf pin reject; empty/oversized/bad-pin/malformed JSON/missing keys/nonarray/empty selections reject before any lookup. JSON and unique-source budgets are explicit.

The original route still independently matches **2,880 ticks / 24 transfers / 172 blocked ticks / zero steps**, over two poses. Core smoke and build pass. No POB parser rules or original source semantics changed.

Confidence: Confirmed bounded modern selection/load contract. Complete world discovery, spawn semantics and unsupported door variants remain Unknown. See the [durable contract](../../docs/INTERIOR_DOOR_SCENE_BUNDLE_CONTRACT.md).

## Next action

Use the bundle and shared typed scheduling in a bounded Godot preview showing both leaves, all three floors, persistent ownership and controls. Verify headless scheduling/input and rendered owner placement on this same representative route.
