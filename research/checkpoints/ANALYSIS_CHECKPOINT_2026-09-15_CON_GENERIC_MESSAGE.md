# Stardust.exe Analysis Checkpoint: ConGenericMessage

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

`ConGenericMessage` (`0x08C5FC76`) is an active client chat-presentation message. Listener `0x00F38C20` matches descriptor `0x015F60AC`, parses the compressed payload through `0x013F4E70`, converts the ASCII message to Unicode, and submits it through chat presentation helper `0x00F3BFD0`. Delivery is gated by a listener/client-ready byte at `+0x34`.

## Wire contract

Stardust emits operand count three, opcode `0x08C5FC76`, ASCII16 text and a fixed LE32 zero, with compression enabled. The executable parser registers both the string field and a 32-bit field. The semantic branch consumes the string and does not consult the trailing integer.

## Client flow

```text
descriptor 0x015F60AC
  -> listener 0x00F38C20 branch 0x00F38C3E
  -> require ready byte (+0x34)
  -> parser 0x013F4E70
  -> ASCII-to-Unicode 0x00489300
  -> chat presentation helper 0x00F3BFD0
       `-- route formatted chat payload among registered chat receivers/tabs
```

The lower helper builds a chat-message structure, attempts delivery across registered receivers and contains a fallback route. The exact chat channel identity selected by its synthesized metadata remains below this block's boundary; the server header describes intended display in spatial chat.

No active constructor call site exists in the current Stardust server tree beyond the packet definition, so this is supported client functionality with a presently dormant server producer.

## Replacement requirements

- Decompress at the message envelope, then decode ASCII16 text plus the required trailing U32.
- Convert text into the client's Unicode domain and enqueue it onto the chat/UI owner thread.
- Preserve readiness gating so pre-UI messages cannot touch uninstalled chat receivers; logging/delayed buffering is safer than a crash.
- Treat received text as display data, never markup, command, format string or executable content without explicit sanitization.
- Retain the trailing value for forward compatibility even though this build ignores it.

## Confidence

- **Confirmed:** executable class/descriptors, registration, listener branch, parser fields, Unicode conversion, delivery helper and readiness gate; server bytes/compression.
- **Strongly inferred:** presentation targets spatial/system-visible chat routing consistent with the server comment.
- **Closed for current compatibility:** the trailing U32 is required, always zero from Stardust, retained by the parser and ignored by presentation. Preserve it as reserved U32; no current producer exists beyond the packet definition.

## Progress

One reachable packet moves to documented: **253 documented / 92 reachable gaps / 26 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
