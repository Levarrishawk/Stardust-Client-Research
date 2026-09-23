# Analysis checkpoint — CKAT animated-translation channels

Target: verified `Stardust.exe` and the effective original-asset overlay. Ghidra output is reconstructed pseudocode.

The `CKAT/0001` reader `0x00825B30` reads the INFO animated-translation count, opens optional `FORM ATRN`, and invokes `0x00824B80` once per declared channel. That reader requires a `CHNL` child with U16 key count followed by `keyCount` pairs of U16 and U32 (six bytes per key). It expands to 12-byte runtime records using `0x008240B0` and post-processes intervals with `0x00825230`. The static-translation array is a separate `STRN` chunk of four bytes per declared entry, already validated in the preceding CKAT checkpoint.

Across all **7,771** effective clips, the INFO animated-translation count equals the number of nested `CHNL` children in `FORM ATRN`: **40,701** channels total. There are **zero** count, child-tag, or payload-width discrepancies; **688** clips have an empty ATRN form and zero animated-translation count. These checks make a concrete importer-validation rule. The exact quantized value scale and interpolation semantics are not established by this block.

Evidence: `0x00825B30` and `0x00824B80` read-only pseudocode; ignored scratch corpus audit `local-private/session-2026-09-23-marathon/probe_ckat.py`. Confidence: confirmed for structural layout and loader invocation; unknown for playback math.
