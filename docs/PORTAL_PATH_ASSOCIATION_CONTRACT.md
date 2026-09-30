# Portal/floor path association inspection

The floor-node global key is interpreted as portal geometry index, not layout PGRF node ordinal. Layout portal-type nodes (observed raw types 3/5) carry that geometry key; floor portal nodes (observed raw type 0) carry it too. Association requires the owning cell to be an authored side of that geometry. This is strongly evidenced corpus interpretation; original runtime linking/collision/permission remain separate.

`PortalPathAssociationInspector` keeps valid individual bindings and every same-key node. It does not collapse them to one node. A paired candidate contains both authored cells and all eligible node ordinals on each side. It requires two geometry sides and at least one valid binding per side. Conflicting keys and unavailable layout-node associations are explicit diagnostics. Multiple nodes and incomplete groups are reported. Candidates are not edges or proof of movement legality.

## Corpus agreement and contradictions

[All-layout checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_PATH_ASSOCIATIONS.md): 14,814 global floor nodes were inspected across 3,412 graph-bearing cell references. 14,783 bind to their cell's portal geometry; 31 conflict in one bunker layout and are excluded. The conservative result is 3,581 paired candidates, 977 multi-node groups and 14 incomplete groups. Some portals have up to 24 authored floor-node bindings; unique-key collapse would lose real data.

The floor-reference closure retains 13 missing police-station floors, 17 live floor references without PGRF and 28 cells without floor paths. These are original corpus gaps/optional branches and do not justify fabricating routes.

## Ownership and budgets

`PortalPathGraphBundleReader` loads pinned POB and floor/graph resources without render geometry, under the shared source cache. Defaults are 256 attempted resources and 128 MiB source bytes. Failed InvalidData extraction attempts are cached and count toward the unique source-attempt budget. Scene and appearance readers share this cache implementation. Explicit root pins remain fatal even with tolerant unsupported appearance roots. The association input has a separate 65,536-node bound. Immutable floor/graph resources may be shared; cell ownership keys are independent.

The native ordered bindings/candidates/diagnostics agree independently for every one of the 291 original layouts. Original graph cost, actor clearance, floor locators, doors and dynamic allow-entry remain unimplemented. Server `PortalLayout::connectFloorMeshGraphs` joins equal global keys across cells, but that correlation alone is insufficient to copy it as client behavior.

## 2026-09-30 portal transfer follow-up

The [bounded transfer contract](PORTAL_FLOOR_TRANSFER_CONTRACT.md) adds one owner transfer and remaining motion with current offline entry/barrier policy. Floor portal IDs index the owning cell reference vector; they are distinct from global geometry/PGRF keys. The prior tentative 0126AA80 owner-transfer interpretation is corrected: it preserves the same floor wrapper; 012670E0 / 00B2B3F0 perform cell continuation. Fourteen original directed links, 56 independent probes and 28 Godot adapter probes pass. Full actor collision, door lifecycle and exterior continuation remain open.
