# Stardust Client Analysis Checkpoint — Four-Hour Block Interrupted

Date: 2026-09-11

Target: `C:\Stardust-DEV\Stardust.exe`

Verified SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope and timing

This checkpoint preserves approximately 1 hour 48 minutes of the requested four-hour block. The task was interrupted before the planned duration. Findings below are complete analysis units; the connection-options/default-timeout unit was in progress and is explicitly left as the continuation point.

## Completed findings

- Closed the outer malformed-Archive path: receive service `0x012B3B80` logs `Unhandled Archive read error: %s`, rethrows, and no higher application layer catches it before the CRT. Malformed application input can terminate the legacy process.
- Reconstructed `ConnectionServerConnection` event/send/stall vtable behavior.
- Closed representative high-value replicated schemas and callbacks for SHIP, GRUP, GILD, MSCO, MISO and ClientWaypointObject.
- Found a confirmed GILD delta incompatibility: client clear command is 2; current server helper `removeAllGuilds()` emits 3 and is ignored.
- Closed containment-driven skeletal and alternate wearable attachment ordering.
- Classified `ParametersMessage` as retained but statically dormant and `ConnectPlayerResponse` as customer-service connectivity, not zone login.
- Classified `PlayerMoneyResponse` as decoded then discarded by this executable.
- Closed avatar creation request/success/failure/random-name paths and exact request field order.
- Closed gameplay command enqueue `0x116`, command remove/result `0x117`, target update `0x126`, and ship target-slot update `0x3FA` end-to-end against server callbacks and replicated deltas.
- Closed mission list `0xF5`, details `0xF7/0xF8`, accept `0xF9/0xFA`, and abort `0x142` client state paths. Current server implements list refresh via MISO deltas plus accept/abort, but has no `0xF7` handler or `0xF8` emitter; its defined `PopulateMissionBrowserMessage` has no call site.
- Resolved `ClientIdMsg.gameBits` to `[ClientGame] gameBitsToClear`, default zero. Current server parses but does not enforce it.
- Carried `ClientPermissionsMessage` into cluster-selection/avatar-selection UI. The fourth server byte remains ignored. The executable's effective regular-creation check ORs server permission with a global set by `Game::install`, a legacy gating quirk that should not be copied.
- Proved reliable fragment reassembly has no declared-total cap and reads the first four payload bytes without a first-fragment minimum-length check. This is a replacement security requirement.

## Confidence

- Confirmed: all addresses, codecs, RTTI names, branch effects and server correlations stated above.
- Strongly inferred: some UI helper labels where behavior is clear but public method symbols are absent.
- Unknown: full historical `MessageQueueMissionDetailsResponse` field labels; selector-2 transport transform public semantics; exact purpose of several SharedNetwork option fields beyond their embedded configuration names.

## Progress estimate

- Replacement-relevant implementation-grade research: **57%** (up from 53%).
- Pre-build engine/POC decision analysis: **100%** remains complete.
- Exhaustive raw-function semantic coverage: still well below 1%; this is intentionally not conflated with replacement readiness.

The increase reflects closed end-to-end behavioral slices, not merely additional function names.

## Resume point

Continue at `ConfigSharedNetwork::install` candidate `0x012BBA30` and default-options constructor `0x012B9E10`. Embedded defaults were recovered (including CRC bytes 2, 4 MiB incoming/outgoing buffers, raw/fragment size 496, outstanding packet limits 400, 90-second oldest-unacknowledged timeout, 15-second keepalive, 46-second no-data timeout and 2 MiB reliable-overflow bytes), but their exact consumption and the separate `[ClientGame] connectionTimeout` consumer still require correlation before durable conclusions are added.
