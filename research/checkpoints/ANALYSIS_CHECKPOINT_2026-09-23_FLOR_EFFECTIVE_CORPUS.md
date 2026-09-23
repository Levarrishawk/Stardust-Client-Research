# Effective floor-mesh corpus (overnight block 28)

Configured TRE overlay audit of `C:/Stardust-DEV` `.flr` winners, complementing the executable floor-loader/runtime trace from 2026-09-12. This is a read-only structural census, not a movement simulation.

There are **4,776** effective `.flr` paths: 4,730 live `FLOR` files and 46 zero-length masks. All live files use executable-supported outer versions: `0003` ×472, `0004` ×177, `0005` ×366 and `0006` ×3,715. No effective 0000–0002 files remain despite loader support. Every live file has `VERT` and `TRIS`; no top-level IFF overrun was found.

Top-level optional structures across the live corpus are `BEDG` ×4,081, `BTRE` ×3,618, `PGRF` ×3,626, plus the v0004-only legacy `PNOD`/`PEDG` ×177 each. The common v0006 signature (`VERT`, `TRIS`, `BTRE`, `BEDG`, `PGRF`) occurs 3,101 times, but v0006 also has **285** without BTRE, **258** without PGRF, and **71** without either. Consequently a replacement must not require a stored bounding tree or path graph for every v0006 floor; it needs a deterministic fallback for collision acceleration and a deliberate pathing policy where PGRF is absent. The three remaining v0005 signature variants likewise vary BTRE/PGRF independently.

All 4,730 live outer forms are within the client loader's documented 0000–0006 range. Structural counts and empty-mask classification are **confirmed** by the effective archive bytes. The runtime effect of each optional combination is **strongly inferred** from the previously traced v6 loader. Reference closure from PRTO cells to floor files and actual cross-cell route equivalence are **not yet proven**. Scratch probe: ignored `local-private/session-2026-09-23-marathon/floor_corpus.py`.
