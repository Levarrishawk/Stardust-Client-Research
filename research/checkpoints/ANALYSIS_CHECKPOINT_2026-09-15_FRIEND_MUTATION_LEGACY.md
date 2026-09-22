# Stardust.exe Analysis Checkpoint: Legacy Friend Mutation Messages

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes the paired server messages `ChatOnAddFriend` (`0x2B2A0D94`) and `ChatOnChangeFriendStatus` (`0x54336726`). Neither is implemented by this exact executable: complete name, RTTI, opcode, registration and adjacent-DLL scans are negative. Stardust sends them before the already-confirmed `PLAY:9[7]` friend-vector delta, which is the effective membership source. Presence remains separate in the previously mapped `ChatFriendsListUpdate` path.

The executable does contain a class named `ChatAddFriend` at RTTI `0x018B2AB0`, with constructors around `0x00AFFDB0` and `0x00AFFF40`. It is not the server's zero-ID `ChatOnAddFriend`: its exact name differs and its reconstructed object registers additional identity/request fields. No semantic equivalence is asserted.

## Server wire contracts

### `ChatOnAddFriend`

The message declares three operands, opcode `0x2B2A0D94`, and one LE64 value fixed to zero. Stardust emits it only immediately before a successful add notification.

### `ChatOnChangeFriendStatus`

| Order | Field | Wire representation | Value |
|---:|---|---|---|
| 1 | owning creature ID | LE64 | caller object ID |
| 2 | game | ASCII16 | `SWG` |
| 3 | galaxy | ASCII16 | configured galaxy |
| 4 | character | ASCII16 | normalized target |
| 5 | reserved/result | LE32 | zero |
| 6 | add flag | LE32/bool | one add, zero remove |
| 7 | reserved | U8 | zero |

The header declares six operands. Compatibility should reproduce serialized bytes, not derive the header count from host-language fields.

## Producer behavior

Successful add order is:

1. normalize and validate the target, establish reverse-friend state;
2. send the zero-ID legacy preamble;
3. send the legacy change notification with add=true;
4. if the target is online and does not ignore the requester, send presence=true;
5. mutate the friend vector and send `PLAY:9[7]`;
6. send localized `cmnty:friend_added` feedback.

Removal sends the change notification with add=false, removes and replicates `PLAY:9[7]`, then sends `cmnty:friend_removed`. Missing/invalid targets use localized `friend_not_found` feedback. Reverse-friend bookkeeping is server-internal and must not be inferred from client membership alone.

```text
normal command request -> server validation
  +-- failure -> localized system message
  `-- success
       +-- legacy preamble/change messages -> no Stardust.exe consumer
       +-- optional presence update -> mapped CommunityManager presence path
       +-- PLAY:9[7] delta -> authoritative membership
       `-- localized success message
```

## Executable evidence

- Neither server message name occurs in the complete PE string set.
- Neither opcode occurs as raw little-endian data or an executable immediate.
- No listener registration, parser RTTI or adjacent-DLL name exists.
- Positive controls remain present: `getFriendList`, `addFriend`, `removeFriend`, `FriendListChanged`, `FriendOnlineStatusChanged`, `ChatFriendsListUpdate`, and the distinct `ChatAddFriend` class.

## Replacement requirements

- Make `PLAY:9[7]` authoritative for membership.
- Keep presence separate from membership and respect server-side ignore suppression of presence.
- Use normal command submission for UI add/remove operations unless later evidence proves a distinct active route.
- Preserve both legacy message byte layouts only if compatibility with another client generation is desired; do not apply duplicate membership mutations.
- Preserve U64 object identifiers and original string/integer widths independently of x64 internal representation.
- Model reverse-friend relationships as server behavior, not client-visible list symmetry.

## Confidence and unknowns

- **Confirmed:** both wire layouts and producer order; both messages absent from this executable; view-9 membership and named presence paths; localized-result behavior.
- **Strongly inferred:** both named server messages are retained for other client generations.
- **Unknown:** historical consumers; exact callback that publishes `FriendListChanged` after every replicated vector operation; the precise protocol role of the distinct executable `ChatAddFriend` class.

No DRM, licensing, authentication protection or access-control mechanism was examined or altered.

## Progress

Two reachable packets move to documented: **246 documented / 99 reachable gaps / 33 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
