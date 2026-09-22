# ExecuteConsoleCommand internal grammar

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

`0x009141D0` is not a generic process launcher and not merely a one-command dispatcher. It is the client console's internal command-list and alias-expansion engine.

## Confirmed mechanics

- Network parser `0x013F51F0` still decodes one length-prefixed ASCII field. Listener `0x00F38C20` converts it into the client's UTF-16 command representation and supplies a fresh command-result/context map.
- `0x009141D0` scans the command stream by UTF-16 line-feed (`0x000A`).
- Each parsed command is passed through the ordinary client command parser/registry. The same routine is used by local console/UI command paths, so server-directed commands enter the normal in-game namespace.
- Alias lookup is case-insensitive for the compared prefix. Alias expansion is recursive and has a dedicated guard; failure emits the exact diagnostic `Alias recursion guard hit.  Stopping alias execution.`
- Expanded commands are concatenated with semicolon (`0x003B`) separators. Completed input groups are terminated with line-feed (`0x000A`) before the reconstructed stream is submitted.
- A literal case-insensitive prefix `/pause ` receives special handling. The remainder is parsed as floating point and retained as scheduling/delay metadata for the expanded command sequence rather than dispatched as an ordinary gameplay verb.
- Empty input returns without execution. The routine returns whether any recognized/expanded command was handled.

## Grammar boundary

The evidence supports this behavioral model:

```text
commandStream := line (LF line)*
line          := ordinaryGameCommand | aliasInvocation | pauseDirective
aliasExpansion joins emitted commands with ';'
pauseDirective := case-insensitive "/pause " floatingPoint
```

The broader ordinary command parser owns argument tokenization, whitespace and quote rules. This block does not invent those rules; it establishes the command-list/alias layer surrounding them. The server header's semicolon-separated allowance is consistent with the client's expanded command representation, but the executable path normalizes complete groups through its internal parser rather than using host-shell syntax.

## Replacement requirements

- Preserve ASCII on wire and convert into an engine-owned text representation.
- Split/queue only according to the internal command grammar; never invoke `cmd.exe`, PowerShell, `system`, or a host shell.
- Implement recursion-bounded aliases and `/pause` as a scheduler primitive, not a blocking thread sleep.
- Preserve semicolons only as internal game-command separators and execute on the UI/game thread.
- For the networking proof of concept, allow only explicitly registered handlers; `/conversationstop` and `/ui action questJournal` are the currently confirmed Stardust producers.

## Confidence

- Newline delimiter, semicolon insertion, case-insensitive `/pause ` prefix, float parse, alias comparison and recursion guard: **confirmed by code/literals**.
- The exact ordinary-command quoting/escape grammar beneath this layer: **not yet exhaustively enumerated**, and is a UI/console fidelity item rather than a networking blocker.

