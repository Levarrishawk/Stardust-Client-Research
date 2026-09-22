# Analysis Checkpoint: Persistent-Mail Send Acknowledgement

## Outcome

`ChatOnSendPersistentMessage` (`0x94E7A7AE`) is closed from the active Stardust mail producer through the client parser, pending-send correlation, error presentation, and UI notification path.

## Wire contract

After the standard `U16 3 + U32 opcode` game-message header, the body is:

1. `S32 result`
2. `U32 sequence`

This order is confirmed independently by the server constructor and the client archive registration order. It must not be reversed merely because the server constructor arguments are `(sequence, returnCode)`.

## Client route

```text
ChatOnSendPersistentMessage / 0x94E7A7AE
  -> CuiPersistentMessageManager listener 0x008EFE80
  -> exact-name branch 0x008F0089
  -> parser 0x00AEFBC0
  -> correlate sequence against pending outgoing-mail state
  -> success: retire pending send without an error notification
  -> failure: construct recipient-specific or generic ui_pm prose,
              publish MessageFailed, and display the system/UI message
```

The parser stores the first U32 at `+0x38` and the second at `+0x40`. The listener reads the first as the result and the second as the request sequence. Result `0` is success. The client also suppresses ordinary failure presentation for legacy result `23`. Result `4` receives the recipient-invalid path; other handled nonzero values use persistent-mail failure presentation. Unknown error values must be retained and surfaced generically.

## Server correlation

`ChatPersistentMessageToServerCallback::run()` sends the response after attempting delivery. Current source returns `0` on success, `ChatManager::NOAVATAR` (`4`) for an unknown recipient, and can return `IM_IGNORED` (`9`) if the currently disabled ignore check is restored. The active callback constructs the response with the incoming request sequence, which is the client's pending-operation correlation key.

## Confidence

- Opcode, field order/width, parser, listener branch, active producer, and sequence correlation: **Confirmed**.
- Exact presentation semantics for codes 4 and generic nonzero failures: **Strong**, supported by control flow and `ui_pm` string IDs.
- Meaning of legacy code 23: **Unknown**; confirmed only as a no-error-presentation exception. Preserve it without inventing a label.

