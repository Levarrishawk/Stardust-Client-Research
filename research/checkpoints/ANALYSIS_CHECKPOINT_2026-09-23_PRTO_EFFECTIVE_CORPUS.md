# Effective portal-layout corpus (overnight block 24)

Read-only configured-overlay audit of `C:/Stardust-DEV` assets, using the established TRE winner order and SWG IFF boundaries. The client-side loaders and runtime portal graph were traced earlier in `ANALYSIS_CHECKPOINT_2026-09-12_PORTAL_DOOR_RUNTIME.md`; this block tests the deployed corpus against that contract.

There are **292 effective `.pob` paths**: 291 live `PRTO` and one zero-length overlay mask (`appearance/thm_all_test_falcon.pob`). Live outer versions are `0001` ×3, `0003` ×215 and `0004` ×73. All fall within `Stardust.exe`'s observed 0000–0004 loader range. Each live PRTO has `DATA`, `PRTS`, `CELS`, and `CRC ` at the outer level. The 288 v0003/v0004 layouts additionally have a `PGRF` child; the three v0001 layouts do not. The 288 path graphs have observed outer `PGRF/0001`.

The `CELS` collections contain **3,470** `CELL` records: `CELL/0005` ×3,316 and `CELL/0004` ×154. Both are inside the known client 0001–0005 cell-loader range. The `PRTS` collections contain **1,061** immediate `IDTL` geometry records. The probe verified IFF extents for all 291 live layouts without an overrun. This does *not* prove that every deeper child resource reference exists or that all portal polygons pass geometric validation; those are separate corpus checks.

The source corpus probe is ignored under `local-private/session-2026-09-23-marathon/portal_corpus.py`. It intentionally counts only configured winners, not all 58 archive copies. Structural counts above are **confirmed**; the interpretation of `IDTL` as portal geometry is **strongly inferred** from the enclosing `PRTS` and executable loader; reference closure and in-world visibility behavior remain **unknown** at corpus scale.

Replacement implication: the effective deployed interior corpus does not require a new PRTO/CELL outer version beyond the already traced executable dispatch, but it does require both `CELL/0004` and `0005`, optional PGRF, and explicit handling of the zero-length mask.
