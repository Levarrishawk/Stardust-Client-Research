# Stardust.exe One-Hour Analysis Session: Protocol and UI Closure

Date: 2026-09-15  
Requested window: 11:30-12:30 America/New_York  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

Sixteen bounded analysis blocks were completed and published. The server-packet matrix advanced from **244 documented / 101 reachable gaps / 35 literal-opcode gaps** to **268 documented / 77 reachable gaps / 14 literal-opcode gaps**. This closes **24 reachable matrix rows** and removes **21 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%** because these closures materially improve protocol certainty but do not justify rounding up broad renderer, gameplay and asset-pipeline coverage.

## Completed blocks

1. Friend mutation legacy messages: proved `ChatOnAddFriend` and `ChatOnChangeFriendStatus` are unsupported preambles; `PLAY:9[7]` is authoritative.
2. Chat avatar connection: mapped `ChatOnConnectAvatar` to deferred friend/ignore reconciliation.
3. Chat server status: classified duplicate opcode `0x7102B15F` definitions as dormant.
4. Holocron UI: mapped open-page and close-mediator messages.
5. Newbie tutorial trigger protocol: mapped request flags and the actual response/echo opcode.
6. Generic chat text: mapped `ConGenericMessage` through parsing and presentation.
7. Object client-event legacy message: separated dormant `PlayClientEventObjectMessage` from active client effects.
8. Client inactivity: mapped the one-byte reliable state sender and server log consumer.
9. Player resource queries: mapped paired stomach/money requests and their different response authorities.
10. `unkZone`: bounded the test-only literal packet without inventing zone semantics.
11. Customer-service responses: mapped cancel, article and new-ticket-activity paths and corrected an opcode ambiguity.
12. Ticket create/list responses: mapped both codecs and proved the dormant create-response server/client field-count mismatch.
13. Permission-list creation: mapped both Unicode arrays and list name into `/PDA.PermissionList`, correlated to active structure and ship producers.
14. Stat-migration targets: mapped ten signed integers into `WS_StatMigration`, correlated to base HAM and session targets.
15. Server-directed game command: mapped one ASCII command into the internal game-console/UI-action namespace and documented the required modern allowlist boundary.
16. Tutorial HUD element: mapped ASCII HUD role, enable flag and blink count to semantic HUD visibility/animation, including the `all` selector.

## High-value findings

- Several legacy notification packets are intentionally not the authoritative state channel; player-object baselines/deltas carry the real friend/ignore/resource state.
- The customer-service create-response server class is structurally incomplete relative to the client codec. Compatibility code must not silently treat it as the canonical contract.
- Permission-list and stat-migration messages now have complete server-message-to-parser-to-handler-to-state mappings.
- `ExecuteConsoleCommand` is an internal game command mechanism, never an operating-system execution facility. Current Stardust uses it for `/conversationstop` and `/ui action questJournal`.
- Wire-width discipline is explicit: stat values remain signed 32-bit, ticket time remains U64, permission strings remain counted Unicode, and x64 host types must not leak into any of them.

## Confidence and residual uncertainty

- **Confirmed:** all opcodes/codecs/routes described in the individual checkpoints; active versus dormant server producer status; matrix counts.
- **Strongly inferred:** a few downstream helper roles where no symbols survive, notably the internal console submission boundary.
- **Unknown:** historical meaning of the permission packet's currently empty second list; full server-directed game-command grammar; several customer-service secondary/result fields.

These unknowns are localized and recorded. They do not invalidate the closed wire layouts or the recommended networking proof-of-concept.

## Next recommended area

The highest-value next block is the structural object-replication family: creature view-6 and view-4 deltas, tangible view-3 baselines/deltas and view-6 deltas, followed by parent-relative transforms. These have the most active server references and directly affect object baselines, state deltas, containment and movement—the core of a behaviorally compatible client after login. Exact unresolved class identifiers remain in the generated gap matrix rather than prose so merely naming a target cannot falsely mark it documented.

## Workspace integrity

All research artifacts remain private under `D:\Development\Experimental-Client`. No analysis documents were added to the Stardust-3 Git repository, and no executable or server source was modified.
