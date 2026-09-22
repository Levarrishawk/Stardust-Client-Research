# System Message Manager

This block closes the active `ChatSystemMessage` path from the current Stardust
server constructor through the client parser, prose expansion, and UI fanout.

## Confirmed wire contract

The server emits outer opcode `0x6D2A6413` with a U8 display policy followed by
one of two representations:

1. Literal form: UTF-16 message followed by a zero U32 parameter length.
2. Prose form: empty UTF-16 message followed by a U32 word-count and serialized
   `StringIdChatParameter` data (STF identity, TT/TU/TO/NU substitutions, DI,
   DF, and the preserved auxiliary byte).

`DISPLAY_CHATANDSCREEN` is 0 and `DISPLAY_CHATONLY` is 2 in current Stardust.
The structured field is length-delimited and must be bounds-checked and safely
skipped if a future tag cannot be interpreted.

Evidence: `ChatSystemMessage.h`; `StringIdChatParameter.h`; executable parser
`0x00AEE970`; embedded message name and RTTI.

## Confirmed client path

```text
ChatSystemMessage / 0x6D2A6413
  -> CuiSystemMessageManager listener 0x008ABDA0
  -> message decoder 0x00AEE970
  -> presentation/prose formatter 0x009CAA40
  -> semantic handler/publisher 0x008ABEB0
  -> typed Received transceiver
  -> chat-window and SwgCuiSystemMessage consumers
```

`0x008ABD20` installs the listener. The handler passes the display byte,
literal Unicode field, and structured prose archive to the formatter, then
publishes one `CuiSystemMessageManagerData const&` event. UI consumers choose
chat-only versus transient screen presentation. `systemMessageDuration` and
`showSystemMessages` govern the latter.

## Replacement consequence

Expose a value object containing the exact U8 policy, optional literal text,
and optional decoded prose object. Localization/substitution belongs above the
codec; chat-log and transient presentation remain separate consumers. Original
STF/string-table lookup is required because structured system messages depend
on original assets for their text.

## Confidence

- Opcode, server wire variants, display values, parser, formatter, and fanout:
  **Confirmed by code**.
- Protocol/localization/UI ownership split: **Strongly inferred**.
- Friendly names of every rare prose tag: **Unknown but bounded** by the
  length-delimited archive; this is not an unknown handler path.

