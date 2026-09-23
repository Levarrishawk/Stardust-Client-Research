# Analysis checkpoint — CKAT animated-rotation corpus audit

Target: the configured, effective original asset overlay paired with verified `Stardust.exe`. This is a structural corpus validation, not a claim that every interpolation operation is recovered.

All **7,771** effective `CKAT/0001` clips parse. Each declared animated-rotation count in INFO equals the number of nested `QCHN` children of `FORM AROT`, with **171,000** nested channels in total. Each nested channel has a U16 key count, three U8 header bytes and exactly six bytes per key. There are **zero** count, child-tag, or payload-width discrepancies. **532** clips have an empty `AROT` form and a zero animated-rotation count. This is distinct from the **32** clips with an additional, optional, *top-level* QCHN.

The executable loop in `0x00825B30` invokes `0x00824870` once per declared AROT channel; its XFIN-to-runtime path is traced in the preceding QCHN routing checkpoint. Replacement importers should validate nested AROT against INFO and must not conflate it with the separate optional channel. `0x00824870` expands keys to float-based runtime entries and `0x008251D0` prepares intervals, but exact interpolation/quaternion reconstruction remains to be verified.

Evidence: read-only decompilation at `0x00825B30` and `0x00824870`; ignored scratch audit `local-private/session-2026-09-23-marathon/probe_ckat.py`. Confidence: confirmed for format, counts and loader routing; unknown for full playback semantics.
