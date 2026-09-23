# Analysis checkpoint — CKAT channel-key time invariants

Target: all 7,771 effective original `CKAT/0001` clips and verified `Stardust.exe`. This audits *deployed assets*, not every theoretically accepted file.

The 171,000 animated-rotation QCHNs contain **12,059,807** keys; the 40,701 animated-translation CHNLs contain **1,979,749** keys. Every channel has at least two keys, and its U16 time values are strictly increasing. There are **zero** nonmonotonic or duplicate adjacent times across the deployed corpus. This matches the loader's interval-preparation routines `0x008251D0` and `0x00825230`, which write `1/(nextTime-currentTime)` into each 12-byte runtime key and set the final key's reciprocal interval to zero.

The ATRN U32 value field is copied into the runtime key unchanged (`0x008240B0`), and all such bit patterns decode as finite IEEE-754 floats. The AROT U32 field is **not safely interpretable as a float**: 1,530 deployed bit patterns decode as NaN/Inf, and the reader's `0x00AC12F0` merely copies the bits. A rotation importer must preserve those U32 patterns pending the quaternion unpacking trace, not apply ordinary float validation to them. The three U8 QCHN header fields are separate from these key values.

Evidence: read-only decompilation `0x00824870`, `0x00824B80`, `0x008240B0`, `0x008240D0`, `0x008251D0`, `0x00825230`, `0x00AC12F0`; ignored corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`. Confidence: confirmed for key counts, time monotonicity, reciprocal setup and bit preservation; unknown for the AROT packed value decoder.
