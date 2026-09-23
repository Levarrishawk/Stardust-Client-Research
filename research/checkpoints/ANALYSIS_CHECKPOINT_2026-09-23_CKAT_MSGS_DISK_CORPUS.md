# Analysis checkpoint — CKAT timed-message disk grammar

Target: all 7,771 effective original `CKAT/0001` clips and verified `Stardust.exe`. This block establishes message timing storage, not every downstream listener.

`FORM MSGS` appears in **2,954** clips. It contains an `INFO` chunk with a U16 message-group count, followed by exactly that many `MESG` chunks. Each `MESG` is U16 occurrence count, a NUL-terminated ASCII message name, then that many U16 frame/time positions. The loader `0x00825B30` dispatches `MESG` to `0x008242A0`/`0x00824340`, retaining the name and occurrences. Full-corpus audit finds **6,952** message groups, **118** distinct names and zero INFO-count, tag, payload-width or within-group time-order discrepancies. `event_footstep` appears in 1,234 message groups; `fire1` in 801. These names are asset-authored animation events, not network opcodes.

The owning template stores message data at offset `+0x5C`. A later access routine `0x00825610` iterates groups and calls `0x008245D0`, which binary-searches occurrence times in a requested interval and appends matches to caller-provided output containers. The higher-level `GameAnimationMessageCallback` route for six prefixes was established in an earlier checkpoint and is cross-linked in the CKAT client-event integration checkpoint; non-prefix consumers remain open. One message name alone does not prove its action.

Evidence: read-only pseudocode `0x00825B30`, `0x008242A0`, `0x00824340`, `0x00825610`, `0x008245D0`; ignored corpus probe `local-private/session-2026-09-23-marathon/probe_ckat.py`. Confidence: confirmed for disk grammar, counts and time-window query behavior; unknown for individual event-name subscribers.
