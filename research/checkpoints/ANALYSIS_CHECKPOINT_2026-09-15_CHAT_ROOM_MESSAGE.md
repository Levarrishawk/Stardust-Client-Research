# Block 2: Chat Room Message

Authority: `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)

`ChatRoomMessage` / `0xCD4CE444` is confirmed at listener branch `0x00A6EC40`, parser `0x00B08480`, and room-message handler `0x00A2D790`.

```text
ASCII16 game
ASCII16 galaxy
ASCII16 sender
LE32 roomId
Unicode32 message
LE32 trailingValue       // Stardust sends 0
```

The parser registers `ChatAvatarId` at `+0x38`, room ID at `+0x60`, Unicode text at `+0x68`, and the trailing dword at `+0x78`. The handler suppresses three reserved room IDs handled by dedicated system/group/guild paths; otherwise it resolves room context, constructs the client room-message model, and publishes `CuiChatRoomManager::Messages::MessageReceived`. The trailing dword is consumed but no Stardust-specific nonzero meaning is established.

Transport compression is removed before this codec. Confidence: schema/route/publication **Confirmed**; trailing value **Unknown**. A replacement must consume and retain it diagnostically.

