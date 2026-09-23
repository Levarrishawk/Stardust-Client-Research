# Analysis checkpoint — CKAT optional root-motion channels

Target: verified `Stardust.exe`; Ghidra output is reconstructed pseudocode. This resolves the previously open playback role of the **top-level** QCHN and keeps it distinct from 171,000 per-transform AROT QCHNs.

The CKAT template constructor `0x00825710` loads the clip and installs one of four function pointers at template offset `+0x6C`, based on presence of optional top-level QCHN at `+0x60` and optional `LOCT` position track at `+0x64`:

| Top-level QCHN | LOCT | Selected routine | Observed output |
|---|---|---|---|
| absent | absent | `0x00826660` | Identity quaternion and zero translation |
| present | absent | `0x008266B0` | Evaluates root quaternion at two times; relative rotation; zero translation |
| absent | present | `0x00826930` | Identity rotation; difference of two sampled LOCT positions |
| present | present | `0x008267A0` | Relative rotation plus LOCT position difference transformed by a quaternion-derived matrix |

QCHN samples use the same packed-quaternion evaluator `0x00824F00` as per-transform animated rotations. LOCT samples use `0x00825070`, which scans its cached 20-byte key interval and linearly interpolates a 3D float position. The caller passes two times and cache pointers to the selected function; the output is a relative motion delta, not a single absolute transform. The exact multiplication/coordinate-frame convention of the combined translation requires disassembly or a differential playback fixture before porting.

Evidence: read-only pseudocode `0x00825710`, `0x00826660`, `0x008266B0`, `0x008267A0`, `0x00826930`, `0x00824F00`, `0x00825070`, `0x0081EE40`; direct template `+0x60/+0x64/+0x6C` field references. Confidence: confirmed for dispatch conditions, optional-channel consumers and output categories; tentative on frame label for combined translation.
