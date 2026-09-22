# Stardust.exe Analysis Checkpoint: Chat Avatar Connection

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

`ChatOnConnectAvatar` (`0xD72FE9BE`) is fully recognized by `Stardust.exe` and closes a chat-session initialization edge. It is a payload-free readiness signal: parser `0x00B0A600` constructs the named message from the archive, chat listener `0x00A6E9C0` matches its descriptor and calls `0x005AB170`, which sets retained flag `0x01914211`. The CommunityManager update routine at `0x005AB180` consumes that flag after installation, dispatches the named `getFriendList` and `getIgnoreList` operations, then runs the social-list reconciliation/update routine at `0x005AAD30`.

## Wire and server boundary

Stardust's constructor writes operand count one and opcode `0xD72FE9BE`, with no application payload. Within the current server tree the class has no production call site beyond its definition, so its exact server transmission point remains unconfirmed. The executable behavior is nevertheless direct and complete.

## Client call chain

```text
ChatOnConnectAvatar descriptor 0x015E9F74
  -> listener 0x00A6E9C0 branch 0x00A6EA0F
  -> archive/message constructor 0x00B0A600
  -> handler 0x005AB170
       `-- connected/refresh-pending byte 0x01914211 = 1

CommunityManager update 0x005AB180
  -> if installed byte 0x01914212 and pending byte 0x01914211
  -> clear pending
  -> dispatch getFriendList (0x005AA7F0)
  -> dispatch getIgnoreList (0x005AA820)
  -> social-list reconcile/update (0x005AAD30)
```

This delayed consumption prevents the network callback from refreshing social UI state before CommunityManager installation is complete. Repeated connect signals coalesce through one Boolean pending flag.

## Evidence

- Embedded message name at `0x018AB7FC` and RTTI at `0x018B2F9C`.
- Descriptor pointer `0x015E9F74`, registered by the listener constructor and compared in the listener callback.
- Both empty/default constructor `0x00B0A510` and archive constructor `0x00B0A600` register no payload fields.
- Branch `0x00A6EA25` constructs the message and directly calls `0x005AB170`.
- `0x005AB170` performs exactly one retained-byte write.
- `0x005AB180` gates consumption on manager installation and invokes both named social-list operations.

## Replacement requirements

- Register opcode `0xD72FE9BE` as a payload-free chat-session readiness event.
- Do not perform UI work synchronously in the network callback; set/coalesce a refresh request and consume it on the owning game/UI update context.
- Refresh friend and ignore state together when the community subsystem is ready.
- Make processing idempotent: duplicate signals only need one pending refresh.
- Because Stardust currently has no proven producer, do not require this signal for the minimal login POC; log and handle it when received and retain an independently triggered initial social replication path.

## Confidence and unknowns

- **Confirmed:** client message registration, zero-payload construction, listener branch, retained flag write, deferred friend/ignore operation dispatch.
- **Strongly inferred:** the flag means chat-avatar connected/social refresh pending.
- **Unknown:** active Stardust producer; exact implementation behind generic named operation dispatch and the full internal behavior of `0x005AAD30`.

## Progress

One reachable packet moves to documented: **247 documented / 98 reachable gaps / 32 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
