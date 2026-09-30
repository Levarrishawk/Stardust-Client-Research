# 2026-09-30 session: original interior door movement

Completed [interior doorway integration](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md) for the original cantina/private-room `cantina_door`. Named POB reference, authored transform, both floors and original leaf now feed the bounded actor-box/floor transfer query. Closed/partial leaf retains source ownership; clear leaf plus allowed entry permits one transfer. Permission remains independent of leaf motion.

Independent verification matches 24 cases over both source references, two owner poses and six states. Synthetic current-snapshot, ownership, malformed-input and unsupported-contact gates pass. Godot build/headless/GPU controls pass; original capture inspected and kept private. Existing YT-1300 floor/portal/barrier controls still pass. Metrics and canonical name/packet counts are unchanged. No commits or staging; unrelated dirty work preserved.

## Exact resume point

The [contract](../../docs/INTERIOR_DOOR_MOVEMENT_CONTRACT.md) defines the seam. Next implement persistent actor movement in this two-cell scene: consume the returned floor locator, advance door contacts from current actor position, reverse through the opposite portal reference and verify repeated transitions/reset/permission changes. The current “Test crossing from source cell” control replays fixed inspection points. One visible physical leaf with two directional bindings needs explicit owned coordination; legacy paired-controller semantics and general solver fidelity remain unresolved.
