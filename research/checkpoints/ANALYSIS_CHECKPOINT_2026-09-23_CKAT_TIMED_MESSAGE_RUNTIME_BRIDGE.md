# Analysis checkpoint — CKAT timed-message runtime bridge

Target: verified `Stardust.exe`. Ghidra output is reconstructed pseudocode, and the exact meaning of returned timing units should be verified before reproducing it numerically.

The compressed-animation runtime vtable (`0x015E5DC4`, slot at `+0x38`) points to `0x00822F30`. That method converts two animation-time fields to integral frame positions and calls the template message collector `0x00825610`, which traverses `MSGS/MESG` groups and uses `0x008245D0` to select occurrences within the requested interval. The runtime then rewrites returned occurrence times using its current time, duration and reciprocal-duration fields, with a wrap branch when an occurrence lies across the clip boundary. Thus named asset events are exposed through the generic skeletal-animation virtual interface, rather than being handled directly in the CKAT disk reader.

The following is confirmed: `MSGS` disk records -> template message groups at `+0x5C` -> `0x00825610` interval selection -> `0x00822F30` playback-time conversion -> generic animation caller. Existing `GameAnimationMessageCallback` analysis identifies the `event_footstep` route; the `fire1` subscriber remains open. The name alone must not be equated to a network or gameplay action. The exact frame-to-time and boundary inclusion policy requires a specific test fixture.

Evidence: `0x00825B30`, `0x00824340`, `0x008245D0`, `0x00825610`, `0x00822F30` read-only pseudocode; static pointer to `0x00822F30` at vtable `0x015E5DFC`. Confidence: confirmed for data flow and virtual dispatch; tentative for timing-unit interpretation.
