# Transport recovery implementation checkpoint — 2026-09-29

## Bounded questions and stopping condition

Can the offline coordinator recover from wire corruption/loss/reordering,
clear owned state deterministically on failure, and send a message larger than
one reliable window? Stop after an independent in-memory peer validates those
paths, all affected fixtures pass, and the remaining operational gaps are explicit.

## Inputs and method

- Client starting commit: `6e75e9e` (`Offline transport milestone1`), initially clean.
- Server source repository HEAD: `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`.
- .NET SDK 8.0.300; existing Python transport/application vectors; Godot 4.7.2 .NET.
- Existing durable [SOE specification](../../docs/SOE_TRANSPORT_PROTOCOL.md) and
  [wire vectors](../../docs/SOE_TRANSPORT_TEST_VECTORS.md) supply the recovered
  contracts. No new executable analysis, address names, or packet rows were added.
- All peer scenarios are in memory and use synthetic credentials. No sockets,
  live endpoint, original executable execution, or packet captures were used.

## Observation, interpretation, consequence

| Observation | Interpretation / confidence | Implementation consequence |
|---|---|---|
| CRC failure previously propagated into login's fatal parser catch | Confirmed implementation defect: ordinary corruption ended the session | Framing rejection now precedes channel mutation, produces no ACK, and preserves login state |
| Fatal `ErrorMessage` previously changed only `State`; ACK removal dropped arrays without clearing them | Confirmed lifetime defects | Failure clears owned transport/token/identity/list state; ACK removal zeros retained buffers |
| Fragment creation required every fragment to fit the current window | Confirmed artificial message-size ceiling | Add a bounded per-channel owned queue and ACK-driven pump; retain atomic API as a convenience |
| Selective ACK removed entries while slot count used only dictionary count | Confirmed risk of advancing beyond a missing head's span | Available slots are derived from oldest retained sequence through next outbound sequence |
| Multipayload expansion used wire-datagram size after reassembly | Confirmed rejection of valid larger reconstructed payloads | Apply the message-size limit at this boundary, not the smaller wire limit |
| Coordinator could wait forever after a partial login response | Confirmed missing replacement policy | Configurable 30-second response deadline; timeout clears state |
| Outer `00 03` may contain several sequenced leaves | Confirmed by existing executable-backed specification and server handler | One-byte lengths; bounded nesting; return every generated control |

The independent peer uses bitwise seeded CRC and manually packed/chained
frames, not production wire codecs. Its reference fragment receiver owns a
separate sequence/reorder/reassembly implementation. Fixed Python vectors
remain the independent primitive anchors.

## Disconnect byte-order chain

Server paths are relative to the Stardust-3 root:

1. `MMOCoreORB/utils/engine3/MMOEngine/src/engine/service/proto/packets/SessionIDRequestMessage.h:25`
   reads request bytes 6–9 with `parseInt`, retaining their raw host interpretation.
2. `SessionIDResponseMessage.h:19` writes that stored value with `insertInt`,
   reproducing the request bytes in the response.
3. `DisconnectMessage.h:19` uses the same `insertInt` operation; line 20
   appends `0x0600`, producing trailing wire bytes `00 06` on the current server.
4. `system/io/ObjectInputStream.h` and `ObjectOutputStream.h` implement these
   primitives with native memory copies; `system/net/Packet.h` delegates them.

Thus request ID bytes `12 34 56 78` are also the disconnect ID bytes. Reading
the disconnect as LE32 and comparing it to the client's BE32 ID would be wrong.
The independently authored untransformed fixture is `00 05 12 34 56 78 00 06`.
This wire equivalence is **Confirmed for the inspected server construction**;
the trailing field's broader enum semantics remain **Unknown**.

Exact source-file SHA-256 identities:

| File | SHA-256 |
|---|---|
| SessionIDRequestMessage.h | `7931adfa1b9266b2a2b0f751503e58a02013aa6f2e39ed35d6c7ae50d61a6477` |
| SessionIDResponseMessage.h | `f75ac86502089294e819febbf4305352491e63046938b2818f8349ca84f796e4` |
| DisconnectMessage.h | `b1a69027aa756723f57a849d8bdfcd43eaeb110f0993a16a1c030ae508dbf8b3` |
| BasePacketHandler.cpp | `fabcb3db85f3f04baf0a152d6926f008b7a9100448ed793aaa99f730330599fe` |
| BaseClient.cpp | `7a1f6942d944676b2e8d75201aecb27dff5fba548831ab67582e6849d4e760ef` |

## Validation

- Core suite: success, including reordered/duplicate login replies, lost login
  request/ACK, corrupt datagram recovery, fatal batch ordering, deadline and disposal.
- Independent peer: exact reconstruction of a 1,500,000-byte queued message;
  32 seeded transfers with data/ACK loss, corruption, duplication and reordering.
- Direct checks of owned allocations: cumulative/selective ACK buffers, queued
  fragment cancellation buffer and fatal-token buffer are zeroed.
- Strict configuration rejection and diagnostic redaction/sink isolation pass.
- Existing Python scripts: all 22 transport assertions and login/zone assertions pass.
- Godot: build succeeds and headless offline transcript exits 0; sandbox root
  certificate-store diagnostic remains visible and is not claimed resolved.

## Limits and next probe

No live-server interoperability or socket lifetime has been verified. Status
opcodes `00 07`/`00 08`, CRC-disabled status framing, additional control leaves,
adaptive retry timing, and historical zlib behavior remain open. The shared
bounded zlib decoder now checks header and Adler-32 explicitly after a truncated
trailer defect was reproduced during the [TRE slice](IMPLEMENTATION_CHECKPOINT_2026-09-29_TRE_READER.md).

Next bounded question: what exact status request/response bytes, transform
exceptions and connection-state effects must the adapter support? Use the
existing static specification and source call chains before adding a socket.
See the [acceptance record](../../../SWG-Godot-Client/docs/CLIENT_IMPLEMENTATION_ACCEPTANCE.md)
for caller ownership, policy limits and reproduction commands.
