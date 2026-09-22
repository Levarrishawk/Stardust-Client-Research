# Stardust server/client schema risk audit

Date: 2026-09-21  
Client target: `C:\Stardust-DEV\Stardust.exe`  
Server target: current local `Stardust-3` source tree

## Result

The zero-gap packet inventory does not mean every current server producer is byte-compatible with Stardust.exe. This block rechecked all previously identified contradictions against the current source and separates active risks from dormant headers.

| Severity | Transaction | Current exposure | Client contract | Current server behavior | Required decision |
|---|---|---|---|---|---|
| high | quest task counter/timer ObjectController | active Lua API and `journal.lua` callers | counter `0x440` with five body fields; timer `0x441` with four | counter uses `0x441` and five fields; timer uses unsupported `0x444` | correct/version the server IDs, or deliberately add a Stardust-server compatibility profile; never choose parser from body length after dispatch |
| high | `VerifyPlayerNameResponseMessage` `0xF4C498FD` | request callback registered and live | U8 exists + UTF-16 echoed queried name | U8 then U32 value 1, which becomes a string length with no code unit | fix producer or add explicitly correlated compatibility recovery; ordinary strict decoder must reject truncation |
| medium/dormant | `CreateTicketResponseMessage` `0x550A407A` | manager construction commented out | two U32 fields | only one U32 field | do not activate until corrected; retain bounded legacy diagnostic mode only |
| low/dormant-dangerous | `HarvesterObjectDeltaMessage3` | no construction site found | active harvester/tangible hierarchy and tags | helper declares `ITNO` tag with view/index combination inconsistent with active class | keep unused; delete or correct before any future activation |
| low/dormant | `GalaxyLoopTimesResponse` | server deliberately does not send | two U32 values consumed; extra bytes ignored by named-message framing | wrapper contains three U32 zeros | harmless while dormant; normalize if diagnostic feature is restored |

## Active quest mismatch

The source comments explicitly identify the current classes as NGECore2 layouts. `LuaPlayerObject` exposes both methods and `bin/scripts/managers/quest/journal.lua` calls them, so this is not a dead header. The Stardust.exe registry instead constructs its counter codec at `0x440` and timer codec at `0x441`. A counter sent as `0x441` will be selected as the shorter timer body before its extra U32 remains, while `0x444` has no recovered factory in this executable.

The safest interoperability direction is to correct or negotiate the server profile. A replacement client may intentionally support both profiles, but must bind `(protocol profile, direction, controller ID)` to one fixed codec. Heuristic body-length dispatch would make malformed traffic ambiguous and is rejected.

## Active name-verification mismatch

`VerifyPlayerNameCallback` parses the Unicode query and requester ID, performs `PlayerManager::existsName`, then invokes a response constructor that receives only the Boolean. The response writes the Boolean followed by integer 1. Stardust.exe expects the queried UTF-16 name after the Boolean, so the integer is interpreted as a one-code-unit string length and the packet ends before that code unit.

Because the request carries a requester ID but the response does not preserve it, a client-side workaround is safe only if the client serializes outstanding requests and associates the malformed response with exactly one pending query. Fixing the server to echo the queried name is cleaner and compatible with the original executable.

## Replacement policy

1. Maintain one generated/hand-reviewed schema table keyed by direction, outer message type and inner controller ID.
2. Mark every schema with `original-client`, `stardust-current`, or both.
3. Decode strictly into temporary values; never consume “whatever remains” to guess a conflicting codec.
4. Permit compatibility recovery only for a named, tested defect and log the selected profile.
5. Keep dormant malformed constructors out of normal tests except as negative fixtures.

## Confidence

- Client codecs/dispatch IDs: **Confirmed by executable code**.
- Current server bytes and producer reachability: **Confirmed by current local source**.
- Quest methods being callable in deployed content: **Confirmed by Lua bindings and `journal.lua` call sites**.
- Whether those quest paths are exercised on a particular live shard: **Unknown**; runtime telemetry is outside static analysis.
