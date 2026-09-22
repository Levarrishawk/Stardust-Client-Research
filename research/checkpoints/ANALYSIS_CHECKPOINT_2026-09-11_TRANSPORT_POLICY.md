# Transport Creation Policy Checkpoint — 2026-09-11

## Scope

Static analysis of `C:\Stardust-DEV\Stardust.exe` only, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, correlated with local Stardust server source. No executable was run and no live service was contacted.

## Confirmed findings

1. The normal game UDP path passes establishment timeout `0` through `0x012BCDA0` → `0x012C1990` → `0x012C2810`. The connection tick tests this deadline only when positive, so normal attempts have no lower state-0 deadline.
2. Session-request retransmission remains active despite the absent deadline.
3. `SharedNetwork.useTcp` selects a real stream implementation. The true path opens a nonblocking TCP socket with `TCP_NODELAY`, initiates `WSAConnect`, registers close notification and creates an IOCP.
4. The false path constructs the UDP manager and SOE reliable-datagram connection.
5. UDP opcode `0x001E` performs authenticated endpoint remapping/NAT rebinding by connection ID and negotiated token, then rekeys the endpoint lookup table.
6. Current loose client configuration does not override the executable default `useTcp=true`.
7. Current Stardust login and zone server source creates datagram services, producing a deployment/topology question rather than evidence that either client branch is dead.

## Confidence boundaries

- **Confirmed by code:** zero UDP establishment timeout; TCP construction; UDP remap validation and endpoint rekey; loose CFG lacks a `useTcp` override.
- **Strongly inferred:** opcode `0x001E` exists specifically for authenticated NAT/source-port rebinding.
- **Unknown:** whether a TCP proxy/front-end is deployed; whether packed configuration overrides `useTcp`; which path the deployed client uses.

## Disconnect reason map

| Reason | Trigger | Confidence |
|---:|---|---|
| 2 | established connection exceeds no-data timeout | Confirmed |
| 5 | state-0 establishment timeout | Confirmed |
| 9 | session-response identity mismatch | Strong |
| 10 | newly accepted inbound connection remains unaccepted after callback | Strong |
| 11/12 | invalid/rejected session-request identity or listener state | Strong |
| 13 | reliable pending-byte threshold exceeded | Confirmed |

Labels are provisional and do not claim original enum names.

## Progress model

The earlier replacement-decision metric remains **58%**. At the user's direction, the stricter whole-client ledger starts at **39%**. The lower value reflects expanded scope, not lost work.

## Next unit

Resolve the TCP/datagram deployment discrepancy by tracing configuration precedence and TCP framing without contacting the service. Then close remaining connection callbacks and perform the full login-to-zone state-machine audit.
