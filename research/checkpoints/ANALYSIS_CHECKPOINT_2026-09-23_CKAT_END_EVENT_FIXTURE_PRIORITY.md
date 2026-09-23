# Analysis checkpoint — final-frame event fixture priority

Target: effective original CKAT message corpus, correlated with the observed half-open message query and physical-track completion order.

The **1,517** events authored at the INFO-declared end frame span **504** distinct clips. They are not merely obscure debug markers: the most common final-frame name is `event_footstep` (**294** occurrences), followed by `fire3` (**121**), `fire10` (**97**), `fire8` (**95**) and other `fire*` labels. Start-frame events include `fire1` (**333**) and `event_footstep` (**257**). These are direct corpus counts, not a claim that all are audible/visible when played.

The ordinary CKAT advance caps the current frame at the end frame, while the named-message collector uses `[previous,current)` and the outer track queries events before replacing a completed clip. On that observed path, an exact-end occurrence is excluded by the query. Because **504 clips** exercise the condition, compatibility needs a targeted end-frame fixture, not a hypothetical edge-case note. The fixture should include at least one looping footstep animation and one fire-tag animation; observe event delivery at frame 0, at the end, and across replacement/repeat. Until then, do not either discard those disk events or assume a new engine should always emit them.

Evidence: exhaustive ignored corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`; disassembly/pseudocode `0x008245D0`, `0x00822F30`, `0x00822C40`, `0x0083E630`, `0x00830170`. Confidence: confirmed for counts and ordinary-path exclusion; unknown for every possible outer callback/repeat path.
