# Analysis checkpoint — deployed quaternion selector coverage

Target: all 7,771 effective `CKAT/0001` clips, interpreted with the verified SROT/QCHN readers.

The three quantizer-selector bytes are not a tiny enum. The **171,000** animated AROT channels use **127** distinct selector values, ranging 128..254, across 21,422 distinct selector triples. The **127,315** static SROT records use **75** distinct values in the same 128..254 range, across 3,711 distinct triples. The earlier all-256 SROT claim was an audit error caused by reading selector bytes from the packed U32 instead of from the first three disk bytes. The most common static triple `(159,159,159)` occurs 67,982 times. The replacement still needs all **127 initialized selector cases** used by AROT, plus the original default behavior for 0..127 and 255 if preserving the full disk grammar.

The table-build route is identified in the preceding SharedMath checkpoint and the range algorithm is resolved in the subsequent quantizer-table checkpoint. Storing original packed words and selector bytes losslessly remains the safe import boundary, especially for rare numeric edge cases.

Evidence: exhaustive ignored corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`; SROT/QCHN loader `0x00825B30`/`0x00824870`; decoder `0x00AC0D90`. Confidence: confirmed for corrected deployed selector coverage.
