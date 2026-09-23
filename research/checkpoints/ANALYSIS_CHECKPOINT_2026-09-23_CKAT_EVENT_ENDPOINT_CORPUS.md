# Analysis checkpoint — CKAT message and root-channel endpoint audit

Target: all 7,771 effective original `CKAT/0001` clips. This tests the declared INFO end-frame field against event and optional root-channel data, complementing the sampled-pose timing audit.

The **6,952** MSGS groups contain **51,837** individual occurrence frames. None lies after the INFO-declared end frame. **2,305** occurrences are exactly at frame zero and **1,517** exactly at the end frame. The **32** optional top-level QCHNs likewise have no key later than the declared end frame. Among the 266 clips without AROT/ATRN/LOCT sampled pose tracks, **35** still contain MSGS events, so an importer must not discard a clip or its events merely because it has no sampled pose channel.

The runtime message bridge `0x00822F30` has loop/wrap logic, making exact start/end inclusion significant. This audit confirms valid deployed bounds but does **not** itself determine whether a frame-zero or end-frame event fires once or twice at a looping boundary. That requires a timed playback fixture or a more exact trace of the collector's interval comparisons.

Evidence: exhaustive ignored corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`; INFO/MSGS/top-level QCHN loader `0x00825B30`; message collector `0x008245D0` and runtime bridge `0x00822F30`. Confidence: confirmed for counts and bounds; unknown for exact loop-boundary event-delivery policy.
