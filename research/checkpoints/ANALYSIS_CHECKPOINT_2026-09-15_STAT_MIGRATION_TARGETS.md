# Stardust.exe Analysis Checkpoint: Stat Migration Targets

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes active `StatMigrationTargetsMessage` (`0xEFAC38C4`). The packet carries nine signed 32-bit target attributes followed by signed 32-bit points remaining. The client parses all ten values, replaces the stat-migration UI model and refreshes/activates `WS_StatMigration`.

## Client evidence

The executable embeds the class and RTTI strings at `0x01906B08` and `0x01906B2C`; descriptor pointer `0x0160977C` is installed into the stat-migration mediator during construction/destruction. Incoming dispatch reaches `0x00DB96D0`, verifies the descriptor, invokes archive parser `0x013F22B0`, obtains the decoded values through `0x013F24C0`, copies the first nine integers into the mediator's nine attribute slots, stores points remaining, rebuilds the internal target vector and calls refresh `0x00DB9940`. It then activates the mediator.

The codec contains ten independently registered 32-bit archive variables. This matches the server's exact ten-integer payload; compression remains a transport concern and is not part of the message codec.

## Server correlation

`RequestStatMigrationDataCommand` is an active command-queue producer. On first request it creates `MigrateStatsSession`, seeds all nine targets from base HAM, and sends the base values plus zero remaining. Later requests send session targets and compute remaining as the species total-attribute limit minus the nine targets. `RequestSetStatMigrationDataCommand` is the complementary client-to-server commit/update path and performs server-side validation.

## Replacement requirements

- Preserve exactly ten signed 32-bit wire values; host pointer size is irrelevant.
- Keep the canonical nine-stat ordering used by HAM replication and the current UI.
- Recompute and validate totals server-side; never trust client targets or remaining-points claims.
- Represent UI state separately from the packet object and atomically replace all nine targets.
- Retain compressed-packet support in the SOE transport layer.

## Confidence

- **Confirmed:** message identity, ten-field codec, nine target assignments, remaining-points assignment, mediator refresh/activation and both active server construction modes.
- **Strongly inferred:** individual indices use the established nine-value HAM ordering; their human-readable labels come from mediator widgets/localization rather than this packet.
- **Unknown:** no additional wire fields or host-sized values remain unknown for this message.

## Progress

One reachable packet moves to documented: **266 documented / 79 reachable gaps / 16 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
