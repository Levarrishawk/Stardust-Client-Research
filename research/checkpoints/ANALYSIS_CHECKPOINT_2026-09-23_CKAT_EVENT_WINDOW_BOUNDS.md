# Analysis checkpoint — CKAT timed-message selection bounds

Target: verified `Stardust.exe`, checked at instruction level because endpoint semantics matter for the 2,305 frame-zero and 1,517 end-frame occurrences in deployed clips.

The per-message collector `0x008245D0` uses two binary searches. Its first finds the first occurrence **greater than or equal to** the requested start frame (`CMP occurrence,start; JGE`); its second finds the first occurrence **greater than** `endFrame-1` (`CMP endMinusOne,occurrence; JGE`), equivalent to the first occurrence **greater than or equal to** the requested end frame for integer frame indices. It therefore selects the half-open interval **`[startFrame, endFrame)`**. The template method `0x00825610` applies this selection to each named MSGS group. The compressed-animation virtual bridge `0x00822F30` converts two runtime float frame positions to integers before calling the template method, then transforms returned frame positions into relative playback timing with a wrap branch.

This bounds one key compatibility rule: a message at the requested end frame is not selected in that interval, while one at the start frame is. It does **not** yet prove the owning track controller's exact sequence of intervals across a looping endpoint, so whether a frame-zero or final-frame message fires once or twice across a loop remains a fixture item. The integer conversion helper's rounding-mode details are also not assigned here.

Evidence: disassembly `0x008245D0`–`0x00824662`, `0x00822F30`–`0x00822FAB`; pseudocode `0x008245D0`, `0x00825610`, `0x00822F30`; deployed MSGS endpoint audit. Confidence: confirmed for the template's half-open integer interval; unknown for higher-level loop scheduling.
