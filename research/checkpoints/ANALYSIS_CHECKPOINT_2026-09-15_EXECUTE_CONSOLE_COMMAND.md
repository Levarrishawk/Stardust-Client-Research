# Stardust.exe Analysis Checkpoint: Server-Directed Console Command

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes active `ExecuteConsoleCommand` (`0xB1CFCE1C`). Stardust sends one length-prefixed ASCII command string. Client listener `0x00F38C20` recognizes descriptor `0x016098FC`, parses at `0x013F51F0`, converts/prepares the command and submits it to the client console/command-processing path at `0x009141D0`.

## Server correlation

`CreatureObjectImplementation::sendExecuteConsoleCommand` is the reusable producer. Current concrete call sites include `/conversationstop` from the space-communication timer and `/ui action questJournal` from the force-crystal menu component. The server header notes that the text includes the leading slash and may contain semicolon-separated commands.

## Replacement requirements and risk

- Preserve the length-prefixed ASCII wire field for compatibility.
- Do not map this message to an operating-system shell. It targets the game's own command/UI-action interpreter only.
- Parse command separators using the original in-game grammar, not host shell rules.
- Initially implement an explicit compatibility allowlist (at minimum the two commands currently produced by Stardust), log unsupported commands, and add handlers only from correlated server usage.
- Apply strict length/count bounds and execute on the game/UI thread.

The allowlist is a modern safety boundary, not an attempt to bypass any protection. It prevents a compromised or malformed server message from gaining broader local execution semantics than the original game-command namespace.

## Confidence

- **Confirmed:** opcode, single ASCII codec, client descriptor/listener/parser route and active Stardust producer/call sites.
- **Strongly inferred:** `0x009141D0` is the game console/command submission boundary from its prepared command arguments and surrounding listener context.
- **Follow-up:** the command-list layer is now mapped: UTF-16 LF groups, recursive case-insensitive alias matching with a recursion guard, semicolon insertion between expanded commands, and special `/pause <float>` scheduling. Ordinary command argument quoting remains a separate parser-fidelity item. See `ANALYSIS_CHECKPOINT_2026-09-21_CONSOLE_COMMAND_GRAMMAR.md`.

## Progress

One reachable packet moves to documented: **267 documented / 78 reachable gaps / 15 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
