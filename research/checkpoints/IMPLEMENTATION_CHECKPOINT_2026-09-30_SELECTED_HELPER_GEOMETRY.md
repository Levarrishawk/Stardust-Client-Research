# 2026-09-30 selected hidden helper geometry

## Question and inputs

Do the two omitted `shader/mining_outpost_collide.sht` surfaces provide wall/ceiling geometry around the current three-cell walking route? Stop at pinned mesh geometry aggregates and comparison with the already verified route. Reuse cells5/6/7 from the [selected room checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md), configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` and the [zero-pass shader/effect observation](IMPLEMENTATION_CHECKPOINT_2026-09-30_ZERO_PASS_SURFACES.md). No additional POB is surveyed.

## Observation

`tools/python/inspect_selected_mesh_helpers.py` reuses the independent configured overlay index and static-mesh grammar. At most three explicit mesh identities from an independently verified source manifest are accepted; priority, byte count and SHA are checked before analysis. It reads source bytes without extraction and emits aggregate metadata only.

| Selected cell mesh | Mesh SHA-256 | Helper primitives / vertices / triangles | Vertical / sloped / degenerate triangles |
| --- | --- | --- | --- |
| r5 entrance_room_01 | `425D589385176D77E9F7D29508F4674D98636E99389B8E9CB2CCBA4E022F6EC2` | 1 / 56 / 48 | 36 / 12 / 0 |
| r6 connecting_hall | `8050A71B294AFEC1DD9845062C006A459C726FABDA2549CE52EA3A5BABD3E74B` | 0 / 0 / 0 | 0 / 0 / 0 |
| r7 entrance_room_02 | `2A386A135FAF9F749955F5E054C0A2A3F2FB83DB43EE4CDDBCF967BAA5A727E2` | 1 / 56 / 48 | 39 / 9 / 0 |

All three mesh records resolve at archive priority 45. Normal categories use geometric triangle cross-products and a 1e-6 normalized-Y threshold; they do not infer material semantics. Both helpers spanY≈12.281–14.056, withX≈−2.764 (r7 width≈0.0173). Neither has horizontal up/down triangles. The current route feet areY≈17.770/X≈−28.350. [Cell composition](../../docs/PORTAL_CELL_SCENE_CONTRACT.md) adds identity transforms in the layout frame. The helper bounding boxes therefore do not intersect the route actor; they cannot supply its enclosing corridor obstacles.

## Interpretation and consequence

Confidence: Confirmed selected raw geometry/count/identity and route disjointness. Tentative collision purpose from shader/effect names only. The 96 helper triangles fit the new collider's 2,048-triangle budget, but enabling only these helpers would leave the current route's wall/ceiling gap. Zero-pass rendering classification is not a general collision-source rule. Selected authored bounds remain NULL and FLOR lacks wall/ceiling triangles; legacy mesh collision selection still needs direct evidence.

## Reproduce and resume

Run the helper inspector with the configured root/configuration, the existing independently verified selected appearance source manifest, the configuration pin above and `--shader shader/mining_outpost_collide.sht`. Keep detailed JSON output ignored. Next: trace original static-mesh collision selection/eligibility and distinguish visual, hidden-helper and authored-bound candidates before choosing a replacement obstacle set. One-portal triangle guard integration can proceed on synthetic fixtures independently.

Existing entry points for that next probe are `mesh_appearance_factory_VA_007962D0`, `mesh_appearance_load_VA_007968B0` and `shader_primitive_set_ctor_load_VA_00770C30`, already named in [the stable ledger](../../UNKNOWN_FUNCTIONS.md). Follow construction to the runtime appearance collision dispatch and primitive eligibility consumer; the loader labels alone do not establish collision semantics. Confirm the documented executable SHA before using these addresses.
