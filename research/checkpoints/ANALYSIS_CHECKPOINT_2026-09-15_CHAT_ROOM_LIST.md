# Block 3: Chat Room List

Authority: `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)

`ChatRoomList` / `0x70DEB197` is confirmed at listener branch `0x00A6EA79`, parser `0x00B09940`, and room-registry handler `0x00A2AC70`.

```text
LE32 roomCount
repeat roomCount:
  LE32 roomId
  LE32 privateFlag
  U8 moderated
  ASCII16 fullPath
  ChatAvatarId owner       // game, galaxy, name
  ChatAvatarId creator     // game, galaxy, name
  Unicode32 title
  LE32 moderatorCount
  repeat moderatorCount: ChatAvatarId
  LE32 memberCount
  repeat memberCount: ChatAvatarId
```

Stardust currently emits zero moderator/member counts in this list response, but the executable's room record supports both arrays and they remain part of the compatible schema. The response is transport-compressed.

The handler inserts/updates room objects by ID/path, reconciles the hierarchical room tree, removes stale list-derived nodes, and notifies existing room observers. It preserves separately maintained entered-room/member state rather than blindly replacing every room object.

Confidence: identity/schema/registry route **Confirmed**; detailed observer ordering **Strong**. Counts and nested strings require independent bounds in x64.

