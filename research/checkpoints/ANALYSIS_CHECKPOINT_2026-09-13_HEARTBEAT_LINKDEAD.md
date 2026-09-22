# HeartBeat / LinkDeadMessage opcode-collision closure

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Conclusion

Opcode `0xA16CF9AF` is canonically the exact hash of `HeartBeat`. Server `LinkDeadMessage` is an erroneous duplicate alias with an unexplained operation count, not a separate client message. Neither is active in the current server or supported by Stardust.exe.

Confidence: **Confirmed** for hash identity, current reachability, executable absence and historical producer distinction. The effect expected by an old client during character creation remains Unknown.

## Identity proof

Using the engine's confirmed non-reflected CRC/hash algorithm:

| Candidate name | Hash |
|---|---:|
| `HeartBeat` | `0xA16CF9AF` |
| `Heartbeat` | `0x4B85BF5E` |
| `LinkDeadMessage` | `0x740D0DBC` |
| `LinkDead` | `0xB80A60B9` |

This is not a true hash collision between two valid names. Both server headers merely write the same literal opcode, while only `HeartBeat` produces it by the normal naming algorithm.

## Competing server definitions

| Source class | Operation count | Body |
|---|---:|---|
| `HeartBeat` | 1 | none |
| `LinkDeadMessage` | 5 | none |

An operation count of 5 without four serialized fields makes `LinkDeadMessage` internally suspect. No executable parser exists from which a missing schema could be recovered, and no producer ever exercises it in the searched history. The replacement protocol must not fabricate four fields.

## Current reachability

- Current source search finds both names only in their defining headers.
- No live server component constructs either class.
- Stardust.exe contains no little-endian `AF F9 6C A1` opcode bytes.
- It contains no `HeartBeat`, `Heartbeat`, `LinkDead`, or `LinkDeadMessage` strings.
- No RTTI parser, message registration or dispatcher route was found.

## Historical evidence

Old source in the repository history constructed `HeartBeat` in `PlayerManagerImplementation::attemptPlayerCreation`. It was sent immediately after successful character creation and immediately before `ClientCreateCharacterSuccess`. That producer disappeared during the July 2009 IDL-related refactor.

No `new LinkDeadMessage` producer appears in the searched repository history. The header was introduced alongside many early packet stubs.

The server's actual link-dead state is a player/session lifecycle condition: disconnect logic marks a combat-connected player link-dead, sets the character flag, schedules recovery/unload, and clears it on reconnect. There is no evidence that this state depends on sending `0xA16CF9AF` to Stardust.exe.

## Replacement-client policy

1. Catalog `0xA16CF9AF` under canonical historical identity `HeartBeat`.
2. Retain `LinkDeadMessage` only as a documented invalid server alias.
3. Do not implement the operation-count-5 form or infer missing fields.
4. Do not send the message from the current server.
5. If unexpectedly received, validate that no payload is required, log it as obsolete, and ignore it safely.
6. Implement disconnection/link-dead behavior through transport/session state and server replication, not this packet.

## Reconciliation result

- Executable-correlated exact-name misses: **34/37**.
- Confirmed dormant/obsolete artifacts: **3/37**.
- All exact-name misses are now disposition-classified: **37/37**.
- Unresolved entries in this reconciliation ledger: **0**.
- Identity-resolved executable lower bound remains **111/114 (97.37%)**; three absent artifacts are deliberately not counted as executable identities.
- Overall weighted client-research estimate remains **74%**.

Closing this ledger does not mean the total client is fully analyzed. It closes one bounded inventory of server class names absent from the executable and prevents these legacy headers from becoming false implementation requirements.
