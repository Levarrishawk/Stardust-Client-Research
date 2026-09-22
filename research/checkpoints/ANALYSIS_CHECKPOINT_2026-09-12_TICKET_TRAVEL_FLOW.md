# Ticket purchase and travel-point flow

Scope: static analysis of `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`) correlated with the current Stardust server tree. No runtime interception was used.

## Result

The ordinary planetary ticket-purchase round trip is now reconstructed end-to-end at the message, parser, mediator, command, and server-validation boundaries.

### EnterTicketPurchaseModeMessage (server to client)

The server writes:

1. `LE16 4`
2. `LE32 0x904DAE1A`
3. `ASCII16 departurePlanet`
4. `ASCII16 departurePoint`
5. `U8 0`

The client branch at `0x00E410A4` constructs the parser at `0x013F37A0`, obtains its two string fields through `0x013F3A30/0x013F3A40`, and calls `0x00D85160`. That mediator method replaces `SwgCuiTicketPurchase` members `+0xE4` and `+0xF0` with the departure planet and point. Only the two strings are registered in the recovered parser. The trailing server byte is therefore compatibility residue ignored by this build's typed object, not a third ticket-mode field.

### PlanetTravelPointListRequest (client to server)

On mediator activation, `0x00D84500` subscribes to `PlanetTravelPointListResponse`, increments `m_requestsOutstanding` at `+0xFC`, enumerates the client planet list, and sends one request for each successfully obtained planet name.

The typed request constructor is `0x013F1BF0`. It registers:

- a NetworkId at object `+0x38` (eight wire bytes); and
- an ASCII string at object `+0x48` (the requested zone/planet).

Object member `+0x58` holds the local request correlation/count argument but is not registered for serialization. The send site is `0x00D846F7` -> reliable named-message send `0x00462670`. The current server registers opcode `0x96405D4D`, skips the eight-byte player NetworkId because the session already identifies the player, and parses the following ASCII zone name.

### PlanetTravelPointListResponse (server to client)

Opcode is `0x4D32541F`. The server sends a compressed reliable message with this exact columnar payload:

1. `ASCII16 planet`
2. `LE32 nameCount`, followed by `nameCount * ASCII16 pointName`
3. `LE32 coordinateCount`, followed by `coordinateCount * {F32 X, F32 Z, F32 Y}`
4. `LE32 taxCount`, followed by `taxCount * LE32 tax`
5. `LE32 starportCount`, followed by `starportCount * U8 isInterplanetary`

Only points whose `incomingAllowed` flag is true are emitted. All four counts are normally the same filtered count. The tax column is nonzero only for the point selected as the origin and only when its shuttle resolves to a city; other rows receive zero. For an unknown/disabled zone the server returns one empty zero-valued record.

The client listener is `0x00D850A0`. It recognizes type global `0x0160972C`, constructs `PlanetTravelPointListResponse` at `0x013F1120`, and passes it to `0x00D86380`. The parser fields and accessors are:

| Object member | Accessor | Meaning |
|---|---:|---|
| `+0x38` payload / `+0x3C` value | `0x013F14B0` | planet ASCII string |
| `+0x48` / `+0x4C` | `0x013F14C0` | vector of point-name ASCII strings |
| `+0x58` / `+0x5C` | `0x013F14D0` | vector of three-float coordinate records |
| `+0x68` / `+0x6C` | `0x013F14E0` | vector of U32 taxes |
| `+0x78` / `+0x7C` | `0x013F14F0` | packed/vector boolean interplanetary flags |

`0x00D86380` decrements `m_requestsOutstanding` and diagnoses an underflow, rejects a response that does not correspond to the relevant active planet/selection, requires a nonempty point-name vector, then joins the parallel columns by index into 32-byte client destination records. It rebuilds destination-map/UI state, including localized planet names and map buttons. The response route is therefore:

`0x4D32541F -> 0x013F1120 -> 0x00D850A0 -> 0x00D86380 -> destination records/map and combo-box UI`

### purchaseTicket ordinary command (client to server)

The purchase-button branch in `0x00D848D0` obtains departure planet, departure point, arrival planet, and arrival point through mediator selection helpers, appends them space-separated, then appends `1` when round trip is selected or `0` otherwise. It queues command name `purchaseTicket` through the ordinary CommandQueueEnqueue path.

The current server parses:

`departurePlanet departurePoint arrivalPlanet arrivalPoint [type]`

`type == "single"` or `type == "0"` means one-way; every other supplied value means round trip. It authoritatively validates proximity to a travel terminal, current/destination travel bans, named points and routes, inventory capacity, fare/tax and funds, then creates one or two ticket objects. The replacement client should reproduce argument spelling and ordering, but must not duplicate server authority.

## Confidence

- Message routes, parser VAs, registered field types/order, request opcode, response opcode, response array organization, mediator counter, and command argument order: **Confirmed by code**.
- Meaning of client response columns: **Confirmed** by exact agreement between client container types/consumption and server insertion order.
- Server trailing zero in `EnterTicketPurchaseModeMessage` being ignored compatibility residue: **Strongly inferred** from the two-field client parser and direct two-accessor handler call.
- Fine-grained labels for individual ticket-map widget helpers: intentionally left provisional; they are not protocol unknowns.

## Replacement-client implications

- Preserve the eight-byte player NetworkId in the request even though Stardust skips it.
- Preserve X/Z/Y wire order; adapt into the replacement engine's coordinate convention at the protocol boundary.
- Validate all four response counts independently and reject inconsistent or truncated columns before joining them.
- Treat response compression as SOE transport framing, not message-schema behavior.
- Keep travel presentation separate from the protocol model: the legacy client couples this flow to `SwgCuiTicketPurchase`, but a modern client need only expose an asynchronous destination-query service and a command submission interface.
