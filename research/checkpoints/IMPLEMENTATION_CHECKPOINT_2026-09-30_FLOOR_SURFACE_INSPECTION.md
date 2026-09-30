# Floor surface query and cell inspection — 2026-09-30

## Bounded question

Can an independently authored, engine-neutral accelerated surface query agree with exhaustive original geometry probes and support bounded named-cell floor inspection? Stopping condition met. This is a replacement inspection capability, not newly recovered original movement semantics.

## Evidence and validation

Inputs use the previously verified [floor corpus](IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_MESH_READER.md), configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, and the pinned [YT-1300 scene](IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_CELL_SCENE.md). Query fixture SHA-256 `64FD6678FF3DF7CB4A5928A5B13B72994BBC433AD1C5938E1C8340B7610FDF28`.

`verify_floor_queries.py` generates original first/middle/last triangle-center vertical segments with both non-solid policies, plus a guaranteed exterior miss, retaining only private fixture/output. Its exhaustive plane intersection and dot-product barycentric reference differs from the native accelerated Möller–Trumbore path. **32,918 queries across 4,730 floors agree; 26,317 hits.** It compares hit ordinal/ID, parameter within 1e-8 and position within 1e-4; reference boundary tolerance is 1e-10 while native has no interior epsilon. No mismatch occurred. This samples query behavior rather than proving every ray or original floor-walk equivalence.

Native analytic tests additionally exercise 1,000 seeded grid probes, nearest reversed layers, endpoint inclusion, equal ties across leaves, empty/degenerate/coplanar queries, non-solid policy, source ownership and finite rejection. The canonical [query contract](../../docs/FLOOR_SURFACE_QUERY_CONTRACT.md) records the numeric policy and distinction from navigation.

Optional cell floors share the POB/appearance aggregate cache. Independent `verify_portal_scene.py --floors` verifies **eight cells, seven floors/149 floor triangles, 21 sources**, all appearance geometry/sorted hashes and floor field hashes. Missing floors are explicit local diagnostics; aggregate budget tests remain fatal. Godot floor-only display and reflected right-click picking verify upload data and seven center probes. Headless and hidden GPU runs pass; the original hall floor capture was visually inspected and remains private. Builds/smoke pass; no live networking or original execution occurred.

## Next probe

Decode deployed PGRF graph records and cell/global-node associations before considering bounded route inspection. Original edge/path-walk statuses, door collision coupling and live allow-entry remain separate open questions.
