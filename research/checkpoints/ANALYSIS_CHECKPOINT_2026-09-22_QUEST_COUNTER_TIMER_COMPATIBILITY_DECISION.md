# Quest Counter/Timer Compatibility Decision

Date: 2026-09-22

## Decision

The controller-ID discrepancy is sufficiently resolved for implementation. For
compatibility with `Stardust.exe`, the canonical mapping is:

| Inner ID | Payload |
|---:|---|
| `0x440` | counter: ASCII quest, U32 maximum, Unicode label, U32 reserved zero, U32 current |
| `0x441` | timer: ASCII quest, U32 task ID, Unicode label, U32 seconds |

Current Stardust server source uses `0x441` for the five-field counter and
`0x444` for the four-field timer. Both Lua APIs are reachable from
`bin/scripts/managers/quest/journal.lua`, so this is not merely dead packet
code.

## Evidence and consequence

The executable factory registry, concrete RTTI and parsers prove `0x440` is the
counter type and `0x441` the timer type. The server's counter field order already
matches the executable counter codec; only its ID is wrong. The server's timer
field order matches the executable timer codec; only its ID is wrong.

Sending the current counter as `0x441` selects the shorter timer parser and
leaves an extra U32, while `0x444` has no factory in this executable. Therefore
the current server messages cannot be treated as harmless aliases.

## Required future action

Before quest counter/timer integration testing against the original executable,
change or version-profile the server mapping to counter `0x440` and timer
`0x441`. A replacement client may optionally recognize the current erroneous
pair for migration diagnostics, but should not normalize two schemas under
`0x441` without an explicit negotiated profile.

This checkpoint documents the required change only; no server source is
modified during analysis.

## Confidence

**Confirmed** for IDs, field widths/order, live Lua reachability and failure
mode. The only remaining unknown is deployment usage frequency, which does not
change the compatibility decision.

