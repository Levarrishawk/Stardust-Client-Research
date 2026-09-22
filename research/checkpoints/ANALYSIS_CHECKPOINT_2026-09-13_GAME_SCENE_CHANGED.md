# GameSceneChangedMessage negative route closure

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Conclusion

`GameSceneChangedMessage / 0xBBADAEB9` is a dormant server header with no producer and no supported route in the analyzed Stardust.exe. It is not a missing function waiting to be named.

Confidence: **Confirmed for the current server source and exact executable**. Historical intent in other server/client revisions remains unknown.

## Server evidence

The class constructor writes only:

| Field | Encoding | Value |
|---|---|---|
| operation count | LE16 | 1 |
| opcode | LE32 | `0xBBADAEB9` |

There is no payload. An exhaustive search of `MMOCoreORB/src` finds the class only in its defining header. No manager, callback, session or scene-transition path constructs or sends it.

## Executable evidence

- The ASCII/string inventory has no `GameSceneChangedMessage` identity.
- A byte-exact scan finds zero occurrences of little-endian opcode bytes `B9 AE AD BB`.
- No matching RTTI parser class, listener registration or dispatcher branch was found.
- The normal Stardust named-message architecture requires an identity/hash comparison or registered parser path; neither exists for this packet.

This is stronger than merely failing to find a handler by name: the executable does not embed the numeric opcode either.

## Important non-correlation

The executable does contain RTTI such as:

- `MessageDispatch::Transceiver<..., Game::Messages::SceneChanged>`
- owned `SceneChanged` transceivers associated with hyperspace, space death and toolbar systems

These represent internal process-local notifications. They have a C++ event type and subscribers, but no evidence connects them to server opcode `0xBBADAEB9`. The legitimate scene network flow remains the separately recovered `CmdStartScene`, object create/baseline/end-baselines sequence, `CmdSceneReady`, and related scene state machinery.

## Replacement-client policy

Do not implement a behavioral handler solely because this unused header exists. For compatibility with the current Stardust server:

1. do not emit this packet from the server;
2. permit the protocol registry to identify `0xBBADAEB9` as a known dormant zero-body legacy value;
3. log and safely ignore it if unexpectedly received;
4. keep internal `SceneChanged` application events independent of wire opcodes;
5. promote it to an implemented message only if a future producer and intended state transition are established.

This prevents an obsolete packet name from bypassing the reconstructed, ordered scene-transition state machine.

## Coverage effect

- Direct executable correlations remain **34/37**.
- One additional exact-name miss is now disposition-classified as confirmed dormant.
- Overall classified exact-name misses: **35/37**.
- Identity-resolved lower bound remains **111/114 (97.37%)** because absence is not a client identity.
- Remaining active direct-correlation questions: `ConnectionServerTestMessage` and the `LinkDeadMessage`/`HeartBeat` opcode collision.
- Overall weighted client-research estimate remains **74%**.

## Residual unknown

The historical reason the unused server header was retained is unknown and not recoverable from this executable/source pair. That provenance is not required for present interoperability.
