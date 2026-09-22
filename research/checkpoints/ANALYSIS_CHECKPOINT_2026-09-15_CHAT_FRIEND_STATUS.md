# Block 1: Chat Friend Status Update

Authority: `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`)

`ChatFriendsListUpdate` / `0x6CD2FCD8` is confirmed at chat-room listener branch `0x00A6F005`, parser `0x00B055D0`, and social-state handler `0x005AC300`.

Wire payload after the standard header:

```text
ASCII16 game
ASCII16 galaxy
ASCII16 characterName
U8 online
```

The first three strings form `ChatAvatarId`; the parser registers it at `+0x38` and the flag at `+0x60`. The listener normalizes/converts the avatar identity and calls the community/social manager. The handler locates the corresponding friend record, creating/normalizing supporting state when needed, then sets the identity and online flag. It does not treat this packet as the authoritative full friend list.

Confidence: identity/schema/route **Confirmed**; fine-grained observer fanout **Strong**. Replacement rule: key presence and online state separately, preserve all three identity components, and do not use platform-sized booleans on the wire.

