# Block 4: Remove Chat-Room Moderator Result

Authority: `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)

`ChatOnRemoveModeratorFromRoom` / `0x1342FC47` is confirmed at listener branch `0x00A6EDF9`, parser `0x00B06A90`, and handler `0x00A2C6A0`.

```text
ChatAvatarId removedModerator
ChatAvatarId actor
LE32 result
ASCII16 roomPath
LE32 requestId
```

The two identities occupy parser objects at `+0x38` and `+0x60`; result, path, and request ID follow at `+0x88`, `+0x90`, and `+0xA0`. On success the handler resolves the room and removes moderator authority from the target. It also constructs localized success/failure prose keyed by result, room, actor, and target and publishes it through the chat system. Unsolicited notifications use request ID zero; request correlation must therefore permit zero.

Server-defined results include success 0, failure 1, no-avatar 4, no-room 6, not-private 9, invalid-ban-state 12, not-invited 13, and no-permission 16. Confidence: schema and principal mutation **Confirmed**; individual localization token mapping **Strong**.

