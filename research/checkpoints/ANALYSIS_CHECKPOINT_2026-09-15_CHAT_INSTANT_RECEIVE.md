# Block 6: Incoming Instant Message

Authority: `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)

`ChatInstantMessageToClient` / `0x3C565CED` is confirmed at listener branch `0x00900587`, parser `0x00AF2610`, and presentation/state handler `0x00900FB0`.

```text
ASCII16 game
ASCII16 galaxy
ASCII16 sender
Unicode32 message
LE32 trailingValue       // Stardust sends 0
```

The parser registers `ChatAvatarId` at `+0x38`, Unicode text at `+0x60`, and the trailing dword at `+0x70`. The handler checks the normalized sender against the ignore system before publication. Accepted messages update retained last-tell/reply identity, publish the instant-message event to chat UI, trigger configured notification behavior, and may enter the automatic reply path when the local player state requires it.

The trailing dword is consumed but its nonzero semantics are unknown. The response is transport-compressed. Confidence: schema, ignore gate, reply-state update, and UI publication **Confirmed**; exact legacy notification preferences **Strong**.

