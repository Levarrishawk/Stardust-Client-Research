# Portal path associations — 2026-09-30

## Falsifiable question

Do floor global keys identify layout graph node ordinals or portal geometry, and what contradictions must a replacement retain before linking floors? Closed as an association audit and conservative inspector; runtime traversal is not closed.

## Input identity and rejected hypothesis

The same pinned 291-layout and 4,730-floor effective inventories/configuration (SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts) are used. Native association member-list SHA `62B9699D6942ED27BACD080044363AFBB8C08E95A75760FB57BC8DF7312AA986`.

**Rejected:** treating floor key as layout-node ordinal generated 16,146 audit contradictions. The failed detailed result remains private and can be reproduced with `audit_portal_path_associations.py --key-policy ordinal`. Corrected geometry-key interpretation is separately reproducible with default policy. Source bytes and parser hashes are unchanged.

## Observations and interpretation

3,429 live floor references/3,412 with graphs yield **14,814 keyed nodes**, all raw type 0. All 30,606 nodes in these floor references have field04=-1; it is not a proven floor triangle hint. Layout raw types 3/5 associate their keys with portal geometry; raw type 4 keys fit cell ordinals. **Strong interpretation:** geometry key binds floor portal nodes to the owning cell's portal side. 14,783 satisfy that relation; **31 conflict**, all in `appearance/poi_all_impl_bunker_small_outpost_s01.pob`.

The conservative native inspector preserves multiple nodes, excludes contradictory cell/geometry bindings and yields **3,581 paired candidates, 977 multi-node groups and 14 incomplete groups**. The original raw grouping (before exclusion) reports 983 duplicate-cell groups and 18 side mismatches; these denominators/policies must not be combined. Group size reaches 24 authored nodes. Candidate membership does not assert that every cross-product pairing is legal.

The earlier dependency gaps persist: **13 missing police-station floors**, **17 live floor references without graph**, **28 cells with no floor**. No floor is fabricated. Maximum floor/layout portal-node position separation is 121.125489 units; automatic position merging is unjustified. Server equal-key cross-cell linking is correlation only.

## Validation and implementation consequences

`audit_portal_path_associations.py --core-dll DLL` independently agrees on ordered valid bindings, candidates and diagnostics for all 291 layouts. Synthetic repeated-key, typed lookup and contradictory-key isolation pass. A resource-only native bundle avoids unrelated mesh reads. Shared owned source caching now accounts for extraction failures; explicit appearance root pins remain fatal even when unsupported roots are tolerated. Focused identity/attempt-budget fixtures and smoke pass; original scene source/field agreement remains unchanged.

A focused headless graph check exposed the dummy renderer's unavailable MultiMesh transform readback. It now reports that limitation explicitly; actual GPU glyph transforms are verified. CPU edge/route arrays and headless surface queries still pass. This is not represented as headless GPU verification.

Canonical [association contract](../../docs/PORTAL_PATH_ASSOCIATION_CONTRACT.md). Next: textured interior bundles under shared aggregate source/pixel budgets, while original cross-cell movement semantics remain a precise later probe.
