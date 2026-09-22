# ConnectionServerTestMessage historical and negative route closure

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Conclusion

`ConnectionServerTestMessage / 0x0E20D7E9` is an obsolete 2008 developer packet test whose unused header survived in the server tree. It has no producer in the current server and no route in Stardust.exe. It is not part of connection establishment, authentication, heartbeat, ping, or lag reporting.

Confidence: **Confirmed** for present reachability and historical source provenance. Behavior in an unspecified 2008-era client build is unknown and irrelevant to the analyzed executable.

## Wire definition

The orphan constructor writes:

| Field | Encoding | Value |
|---|---|---|
| operation count | LE16 | 2 |
| opcode | LE32 | `0x0E20D7E9` |
| test text | ASCII16 | `ConnectionServer:1348` (21 bytes) |

`BaseMessage()` allocation behavior does not add a separate application field.

## Current source evidence

An exhaustive search of `MMOCoreORB/src` finds the class only in its own header. No session, manager, callback, command or service constructs it. The hard-coded text is not a configurable address and must not be treated as the current connection-server endpoint.

## Executable evidence

A byte/string inventory of Stardust.exe finds:

- zero little-endian occurrences of opcode bytes `E9 D7 20 0E`;
- no `ConnectionServerTestMessage` string;
- no `ConnectionServer:1348` string;
- no corresponding RTTI parser identity or registered dispatch branch.

The executable does contain real connection identities such as `ConnectionServerConnectionOpened`, `ConnectionServerLagResponse`, and lag configuration. Those have distinct hashes and proven paths.

## Historical provenance

Local Git history supplies unusually strong context:

1. Commit `ade150f21e` dated 2008-12-03 added the packet.
2. Its only producer was `GameCommandHandler::sendp`, explicitly marked `TESTING PURPOSES ULTYMAS.`
3. Integer test case 5 constructed and sent `ConnectionServerTestMessage`; adjacent cases sent other experimental/unknown packets.
4. Commit `c093b0719d` dated 2008-12-08 removed the whole test switch, including the only producer.
5. The packet header remained orphaned through later repository evolution.

This provenance rules out treating the class as an undocumented production requirement.

## Real connection flows that replace the misleading name

```text
selected cluster address
  -> SOE connection opens
  -> ConnectionServerConnectionOpened internal event
  -> ClientIdMsg authentication request

periodic lag diagnostics
  -> LagRequest
  -> ConnectionServerLagResponse + GameServerLagResponse
  -> LagReport
```

`ConnectPlayer`/`ConnectPlayerResponse` is another separate customer-service connectivity test and is likewise not this packet.

## Replacement-client policy

- Do not send or depend upon `0x0E20D7E9`.
- Retain it in the protocol catalog as an obsolete zero-side-effect diagnostic value.
- Log and safely ignore it if received unexpectedly.
- Never interpret its hard-coded text as an address or command.
- Keep the implementation focused on the confirmed SOE lifecycle and named authentication/lag messages.

## Coverage effect

- Direct executable correlations remain **34/37**.
- Confirmed dormant/obsolete artifacts: **2/37**.
- Total exact-name misses disposition-classified: **36/37**.
- Identity-resolved lower bound remains **111/114 (97.37%)** because proving absence does not create a client identity.
- Only the `LinkDeadMessage`/`HeartBeat` opcode collision remains in this reconciliation ledger.
- Overall weighted client-research estimate remains **74%**.

## Residual unknown

What the test caused in whichever historical client motivated it is unknown. Recovering that obsolete client behavior would not improve compatibility with Stardust.exe or the current server and is not recommended.
