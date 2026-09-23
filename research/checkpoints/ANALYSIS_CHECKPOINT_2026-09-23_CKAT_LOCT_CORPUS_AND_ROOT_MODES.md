# Analysis checkpoint — CKAT LOCT corpus and root-motion modes

Target: effective configured original animation assets and verified `Stardust.exe`. This complements the executable root-motion dispatch checkpoint with full-corpus evidence.

`LOCT` has a U32 leading field, a signed-16 key count, then `count` records of U16 time and three little-endian F32 position components (14 disk bytes each). The loader at `0x00825B30` expands each to a 20-byte runtime record and prepares reciprocal time intervals; `0x00825070` linearly samples the resulting 3D track. In **1,354** effective clips, all LOCT payloads match `6 + 14*count`, all **98,901** keys have strictly increasing U16 time and finite XYZ floats, and the smallest/largest key counts are 3/748. The leading U32's semantic label remains unconfirmed.

The deployed root-motion option combinations exactly exercise all four constructor branches: neither optional track **6,387** clips; LOCT only **1,352**; top-level QCHN only **30**; both **2**. The top-level QCHN total remains **32** and is not the 171,000 per-transform AROT channels. A replacement importer must preserve all four modes, even though the combined mode is uncommon.

Evidence: `0x00825B30`, `0x00825710`, `0x00825070` read-only pseudocode and ignored scratch corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`. Confidence: confirmed for disk grammar, corpus counts and dispatch reachability; exact combined-mode coordinate frame remains open. The leading field's speed invariant is resolved in the subsequent frame-rate/LOCT-speed checkpoint.
