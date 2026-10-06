# SOE Transport Protocol Specification

## Direct Stardust client correlation

Direct static analysis now confirms that `Stardust.exe` contains and wires a UDP transport into `Connection`/`GameNetworkConnection`:

- RTTI exposes `UdpSock`, `UdpManagerHandlerInternal`, `UdpManagerHandlerMT`, `UdpConnectionHandlerInternal`, and `UdpConnectionHandlerMT`.
- `0x012C1230` creates `socket(AF_INET, SOCK_DGRAM, 0)` and binds it.
- The manager receives through `recvfrom` at `0x012C1D02` and transmits through `sendto` at `0x012C20ED`.
- `0x012B3930` constructs the connection infrastructure and allocates a `UdpManagerHandlerMT` adapter through `0x012BCF50`/`0x012BD480`.
- Above that UDP layer, `0x012B7840` reconstructs four-byte-length-prefixed logical messages before application-envelope parsing.

This confirms the broad client layering independently of the server source. Session negotiation, checksum/transform installation, delivery-mode routing, reliable-channel selection, ACK/retransmit ownership, multipacket aggregation, and fragmentation now have direct Stardust address-level correlation. Some algorithm labels and edge-case policies remain source-derived where noted.

Status: source-derived specification, checkpoint 2026-09-08. This describes the Stardust server implementation that the replacement client must interoperate with. It does not claim every historical SOE client quirk is represented.

Primary evidence:

- `MMOCoreORB/utils/engine3/MMOEngine/src/engine/service/proto/BaseProtocol.cpp`
- `BaseClient.cpp`, `BasePacketHandler.cpp`, `BasePacket.cpp`
- `BaseMultiPacket.cpp`, `BaseFragmentedPacket.cpp`
- transport packet classes under `engine/service/proto/packets`
- `system/net/Packet.h`

## Layering and byte order

```text
UDP datagram
└─ SOE transport packet
   ├─ transport opcode and optional transport fields
   ├─ one or more SWG application messages
   └─ trailer: compression flag + 2-byte seeded CRC
```

The protocol deliberately mixes byte orders:

- `insertShort`, `insertInt`, `insertLong`, and corresponding parsers use engine/native stream order, which is little-endian in this deployment.
- `insertShortNet`/`parseNetShort` and `insertIntNet`/`parseNetInt` explicitly use network (big-endian) order.
- Transport sequence numbers and fragmentation total length use network order.
- SWG application operand counts, opcodes, lengths, object IDs, floats, ASCII lengths, and Unicode lengths use little-endian engine serialization.

Do not parse a datagram with one global endian setting.

## Session handshake

Handshake packets are raw `Packet` objects and bypass the post-session CRC/transform pipeline.

### Session request (`0x0100` as parsed transport opcode)

The authoritative Stardust executable request is 14 bytes:

| Offset | Size | Encoding | Value |
|---:|---:|---|---|
| 0 | 2 | wire opcode | bytes `00 01` |
| 2 | 4 | network order | protocol/version `2` |
| 6 | 4 | network order | random client connection ID |
| 10 | 4 | network order | requested maximum datagram size |

`outbound_udp_connection_ctor_VA_012C2810` generates and stores the connection ID. While the connection remains in state 0, `connection_tick_VA_012C4A80` constructs this request and retransmits it at the configured session interval through `udp_connection_raw_send_VA_012C54B0`. Expiry of the connection timeout stored at `+0xF8` closes with reason 5. **Confirmed in Stardust.exe.** The server's shorter 10-byte constructor is legacy/source drift; its parser accepts the executable's 14-byte request.

### Session response (`0x0200`)

Exact 17-byte constructor:

| Offset | Size | Encoding | Meaning |
|---:|---:|---|---|
| 0 | 2 | little-endian constant `0x0200` | bytes `00 02` |
| 2 | 4 | little-endian | echoed connection ID |
| 6 | 4 | network order | server-generated CRC/transform seed |
| 10 | 7 | bytes | `02 01 04 00 00 01 F0` parameters |

The connecting side extracts the seed with `ntohl(parseInt(6))`, stores it, and treats the connection as established. Server-side `BaseClientProxy::init` generates the seed using `System::random()`. **Confirmed.** The seven parameter bytes need semantic naming; their required values are confirmed, their meanings are unknown.

Client connection timeout in the shared `BaseClient` implementation is 15 seconds. Net-status requests are scheduled after establishment.

## Standard trailer and send pipeline

`BasePacket::close()` appends three bytes:

1. compression flag: requested value for packets of at least 100 bytes, otherwise forced to `0`;
2. two placeholder bytes for the CRC.

Standard sequenced send order is:

1. append trailer;
2. assign 16-bit transport sequence into offset 2 in network order;
3. zlib-compress the payload region if enabled and beneficial;
4. apply the seed-based XOR chaining transform if enabled;
5. compute and write the low 16 bits of the seeded CRC into the final two bytes, most-significant byte first;
6. send UDP datagram and retain it until acknowledged.

Receive order is:

1. verify the two-byte seeded CRC over the still-transformed datagram;
2. reverse the XOR-chain transform;
3. inspect the byte at `size - 3`; if it is `1`, zlib-inflate the payload region;
4. remove the three-byte trailer;
5. dispatch by transport opcode, validate sequence, then extract application message(s).

This transform is protocol obfuscation/integrity, not an authentication or access-control bypass. Authentication remains at the login and zone-session layers.

## Seeded CRC

The implementation uses the standard reflected CRC-32 table with a nonstandard seed initialization. It folds the four bytes of `crcSeed` into the CRC state, processes every byte except the reserved CRC suffix, complements the result, masks it to the requested CRC length, and writes two bytes in big-endian order.

For normal packets `crcLength = 2`. `NetStatusResponseMessage` explicitly disables CRC checking on send; handshake messages also bypass normal processing. The server receive dispatcher skips opcode `0x0800`. Direct baseline client instructions establish no opcode-08 CRC exception at positive trailer width; see the [receive-rule checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-29_STATUS_CRC_RECEIVE.md).

The [2026-09-29 status checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_BOUNDARY.md)
traces the server send chain: `BasePacket::close` still reserves two zero CRC
bytes, disables compression below 100 body bytes, and selector-4 excludes those
two bytes. Thus its status response is 43 bytes, including the encrypted marker
and two unchanged zeros. This is confirmed server-source behavior; the original
client requires CRC equality for that standalone packet at positive width.
The source/client mismatch remains unresolved operationally. The replacement observes
CRC-validated request bodies of 4/40 bytes and response bodies of 40 bytes, with
an explicit tick and unnamed wire words. It sends no automatic status response.

The [extended reply checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_REPLY.md)
recovers the 40-byte client reply: opcode/tick, BE32 local timestamp, two BE64
echoed peer counters, then BE64 local sent and received counts. Local counters
are sampled before sending the reply; receive accounting precedes validation.
An explicit engine-neutral builder now creates normally protected unsequenced
replies for full requests. Short requests lack echo counters and are rejected
by that builder. Request fields at offsets 4–23 remain unnamed.

Golden-vector tests should be generated directly from the server algorithm before implementing an independent version.

## XOR chaining transform

The code calls this encryption, but structurally it is a seed-based XOR chaining transform:

- If the first datagram byte is zero (normal two-byte SOE opcode), transformation begins at offset 2 and excludes four bytes from the length calculation.
- Otherwise it begins at offset 1 and excludes three bytes.
- Each aligned 32-bit word is XORed with the current seed; the resulting ciphertext word becomes the seed for the next word.
- Remaining bytes are XORed with low seed bytes through the implementation's byte operation.
- Decryption saves each ciphertext word before XOR and uses it as the next seed.

The implementation casts packet bytes to `uint32*` and therefore embodies host little-endian word interpretation. A portable client must reproduce the byte result explicitly without unaligned pointer casts.

## Compression

Compression uses zlib with default compression. The compressed region excludes:

- the first two bytes when the first opcode byte is `0`;
- otherwise only the first byte;
- and always the final three-byte trailer.

Compression is retained only when `compressed_size + 3 < original_size`; otherwise the compression flag is reset to zero. Packets under 100 bytes are forced uncompressed by `close()`.

Important server defaults/configuration:

- `BaseProtocol.disableSendCompression` defaults to `1`, so server send compression is disabled unless configured otherwise.
- `BaseProtocol.disableSendEncryption` defaults to `0`, so the XOR transform is enabled by default.
- The receive inflate buffer is fixed at 800 bytes in this implementation. This deserves validation and defensive redesign; a new client should use bounded dynamic storage while honoring protocol limits.

## Transport packet types

The dispatcher parses a two-byte transport opcode using little-endian `parseShort`, yielding these constants:

| Parsed opcode | Wire prefix | Name | Fields after opcode before trailer | Sequenced |
|---:|---|---|---|---|
| `0x0100` | `00 01` | Session request | reserved/version + connection ID | No normal pipeline |
| `0x0200` | `00 02` | Session response | connection ID, network-order seed, 7 parameter bytes | No normal pipeline |
| `0x0300` | `00 03` | Multi-SOE | repeated length-prefixed transport blocks | outer behavior source-dependent |
| `0x0500` | `00 05` | Disconnect | connection ID + `0x0600` field | No sequence |
| `0x0600` | `00 06` | Ping | none | constructor default requires validation |
| `0x0700` | `00 07` | Net-status request | network-order tick | No sequence |
| `0x0800` | `00 08` | Net-status response | tick + nine 32-bit fields | No sequence; CRC disabled by constructor |
| `0x0900` | `00 09` | Data channel | network-order sequence + application payload/multipayload | Yes |
| `0x0D00` | `00 0D` | Fragment | network-order sequence; first fragment also has network-order total size | Yes |
| `0x1100` | `00 11` | Out of order | network-order sequence | No |
| `0x1500` | `00 15` | Acknowledge | network-order sequence | No |
| `0x1D00`, `0x1F00`, `0x2000` | corresponding | Unknown/ignored | unknown | Unknown |

The table describes constructors and dispatch behavior. Final on-wire lengths include the standard three-byte trailer unless the packet bypasses `prepareSend`.

## Data channel and application framing

A normal `BaseMessage` begins with:

```text
00 09                    SOE data-channel opcode
SS SS                    16-bit transport sequence, network order
CC CC                    application operand count, little-endian
OO OO OO OO              application opcode, little-endian
...                       application fields
compression + CRC trailer (before receive processing removes it)
```

After sequence validation, the transport layer passes a message beginning at the application operand count to the login/zone service handler.

### Multi-message data channel

`BaseMultiPacket` produces:

```text
00 09 | sequence | 00 19 | repeated blocks
```

Each block contains a one-byte payload length followed by the original message bytes after its four-byte data-channel opcode/sequence prefix. If length is `0xFF`, a network-order 16-bit extended length follows. Server sending deliberately avoids bundling application payloads of 255 bytes or more because the source notes original-client out-of-order behavior for those packets.

The aggregate is capped when adding a packet would make it exceed 460 bytes. **Confirmed server behavior.** Treat the 255-byte restriction as a compatibility quirk until client behavior is independently verified.

### Multi-SOE (`0x0300`)

The outer format is repeated one-byte block lengths followed by embedded transport packets. The handler recognizes embedded data, fragment, acknowledge, out-of-order, and `0x0001` OK blocks. Complete construction paths and extended-length handling for outer blocks require another audit; do not conflate this with the `0x1900` multi-message payload.

## Sequencing and reliability

### Stardust delivery-mode contract

The mode switch in `soe_send_dispatch_VA_012C3180` is now byte- and branch-confirmed:

| Mode | Client behavior | Provisional public meaning |
|---:|---|---|
| 0 | aggregate through `soe_multipacket_aggregate_VA_012C5B60` | unordered/aggregate |
| 1 | prepare and send immediately through `datagram_prepare_VA_012C5980` | unordered/immediate |
| 2 | wrap in internal `00 1B`, then aggregate | sequenced-wrapper/aggregate |
| 3 | wrap in internal `00 1B`, then prepare immediately | sequenced-wrapper/immediate |
| 4 | enqueue in reliable channel object 0 through `0x012C6A40` | reliable channel 0 |
| 5 | enqueue in reliable channel object 1 | reliable channel 1 |
| 6 | enqueue in reliable channel object 2 | reliable channel 2 |
| 7 | enqueue in reliable channel object 3 | reliable channel 3 |

Oversized mode 0/1 or 2/3 submissions are promoted to mode 4. The numeric routing is **Confirmed**; the descriptive names for modes 0-3 are provisional because their higher-level API names are stripped.

`serialized_message_transport_submit_VA_012B7F60` maps its boolean delivery argument to mode 0 when false and mode 4 when true. The true path calls `0x012B7800` with the payload size to enforce/reserve pending-byte capacity; it does **not** prepend a length. It then passes the original buffer address and size plus numeric mode 4 to `udp_adapter_send_VA_012BD9D0`. The direct `LoginClientId` call at `0x0069BC67` and zone `ClientIdMsg` call at `0x0069B46D` both pass true, proving that both authentication messages use reliable channel 0. The other two direct callers also pass true, making reliable channel 0 the confirmed default for this connection wrapper's application messages.

- Each direction maintains its own monotonically increasing logical counter; only the low 16 bits are transmitted.
- A sequenced sender retains prepared datagrams in `sequenceBuffer` until cumulative acknowledgement.
- The receiver expects `clientSequence & 0xFFFF`.
- Older/duplicate sequence: send an acknowledgement for that sequence and discard.
- Future sequence: buffer by sequence, send `OutOfOrderMessage` carrying the received future sequence, and discard from immediate processing.
- Exact expected sequence: acknowledge, increment expectation, process, then drain contiguous buffered packets.
- Acknowledgement removes all retained packets through the acknowledged sequence, with wraparound adjustment logic.
- Sending pauses when the unacknowledged window exceeds 50 packets; configurable overload protection can disconnect.
- Checkup/resend timing is adaptive in surrounding event code; the baseline balance value is 2000 ms and reset value is 100 ms.

`OutOfOrderMessage` has an important client/server implementation divergence. The receiver sends the future sequence it observed. In `Stardust.exe`, the `00 11` handler expands that sequence and calls `release_acked_packet_VA_012C82C0` for exactly that retained entry. In other words, the control packet acts as a selective acknowledgement of the future datagram: that future packet is released, while earlier missing packets remain retained for timer-driven retransmission. This is **Confirmed by executable control flow**. Stardust server dispatch instead calls `BaseClient::resendPackets(seq)`, but the current implementation returns immediately at its first statement (`return; //disabled`). The replacement must model the executable behavior and test it explicitly rather than assuming the server method name defines the protocol semantics.

## Fragmentation

Messages at least 490 bytes are fragmented before preparation. Each fragment is a sequenced `0x0D00` packet.

- First fragment: opcode (2), sequence placeholder (2), total unfragmented payload length in network order (4), then up to `496 - 8 - 3 = 485` payload bytes, then standard trailer.
- Later fragments: opcode (2), sequence (2), up to `496 - 4 - 3 = 489` bytes, then trailer.
- The reassembler consumes the total size from the first fragment and concatenates subsequent payload bytes in sequence order.
- Server reassembly rejects totals above 1,500,000 bytes and detects overrun.

The fragmented content is the original packet after its four-byte data-channel/sequence prefix. When complete it is passed through data-channel application extraction.

## Disconnect and liveness

- Explicit disconnect sends a `0x0500` transport packet unless the peer already initiated disconnect.
- Net-status request interval constant: 5,000 ms.
- Net-status checkup timeout constant: 50,000 ms.
- Ping `0x0600` is accepted and ignored by the dispatcher.
- Connection setup waits up to 15,000 ms for the session seed.

The replacement POC can initially implement handshake, data, ACK, duplicate handling, timeout, and disconnect. Fragmentation, multipacket receive, out-of-order buffering, and resend should still be implemented before declaring transport compatibility complete, even if the first login request fits one datagram.

## Confirmed versus unresolved

## Direct Stardust.exe transport correlation

## Confirmed session negotiation layout

The connection handshake is now byte-bounded from `Stardust.exe`. An unknown-peer request must be exactly 14 bytes:

```text
00 01 | protocol/version BE32 (= 2) | client connection ID BE32 | requested max datagram BE32
```

The 17-byte response is:

```text
00 02 | echoed connection ID BE32 | session/CRC seed BE32
      | CRC width U8 | transform selector 1 U8 | transform selector 2 U8
      | negotiated max datagram BE32
```

For the current Stardust server the response tail is `02 01 04 00 00 01 F0`: a two-byte CRC trailer, selectors 1 and 4, and a 496-byte negotiated datagram. Direct client use confirms the CRC-width and datagram-size meanings. Selector 1 is strongly correlated with compression and selector 4 with the seed-chained XOR transport transform; those public labels remain **Strong**, while the selector values and callback installation are **Confirmed**.

The server tree contains a 10-byte `SessionIDRequestMessage` constructor, but the actual server receive parser only consumes the connection ID at offset 6 and tolerates the executable's trailing request field. The constructor is therefore treated as legacy/source drift, not as the authoritative client request format.

Byte-exact examples and boundary cases are maintained in `SOE_TRANSPORT_TEST_VECTORS.md`.

The executable independently confirms the server-derived transport model. These addresses apply only to SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

| Wire opcode byte(s) | Candidate client function | Directly observed behavior | Confidence |
|---|---:|---|---|
| manager receive | `0x012C1CA0` (`recvfrom` at `0x012C1D02`) | rotates receive buffers and records source IPv4 address/port | Confirmed |
| manager dispatch | `0x012C2190` | handles connection-management packets before established-peer lookup; creates a `0x340`-byte connection for a valid 14-byte `00 01` request whose version field is 2 | Confirmed behavior; semantic labels Strong |
| established packet | `0x012C36B0` | accounts bytes, checks the configured 1-4 byte trailer, applies configured receive transforms, then calls `0x012C3B60` | Confirmed |
| all established opcodes | `0x012C3B60` | dispatches the second byte of a leading-zero two-byte SOE opcode through a compact table | Confirmed |
| `00 01` | branch `0x012C3BCB` | parses session request IDs and constructs a `00 02` response | Confirmed |
| `00 02` | branch `0x012C3D0E` | accepts the session response, stores connection ID/CRC seed and negotiated parameters at `+0x11C`, marks connected, invokes the connected callback | Confirmed |
| `00 03` | branch `0x012C3FB5` | walks one-byte-length-prefixed embedded packets and recursively dispatches each | Confirmed Multi-SOE |
| `00 05` | branch `0x012C3E9D` | verifies the connection ID and enters close/disconnect processing | Confirmed |
| `00 06` | early branch in `0x012C36B0` | accepted without normal payload dispatch | Confirmed ping behavior |
| `00 07` | branch `0x012C3FF7` | parses a status/statistics request and constructs a `00 08` reply | Confirmed behavior; field labels Strong |
| `00 08` | branch `0x012C446F` | parses the large status response and updates latency/statistical fields | Strong; detailed fields not yet named |
| `00 09`-`00 10` | branch `0x012C484D` | maps both data (`09`-`0C`) and fragment (`0D`-`10`) variants to one of four channel indices, lazily creates a `0xD8`-byte per-channel receive object, and submits the packet to `0x012C7CF0` | Confirmed combined reliable-receive path; names Strong from server correlation |
| `00 11`-`00 14` | branch `0x012C48D2` | selects one of four channel-associated objects, expands the 16-bit wire sequence, and selectively releases exactly that retained outgoing entry through `0x012C82C0`; missing earlier entries remain eligible for timer resend | Confirmed |
| `00 15`-`00 18` | branch `0x012C4921` | selects one of four channel-associated objects and calls `0x012C8210` with the received packet | Confirmed mechanics; acknowledgement label Strong from server correlation |
| `00 19` | branch `0x012C493E` | walks variable-length application-message blocks after the opcode and recursively dispatches their contents | Confirmed behavior; multi-message label Strong from server correlation |
| `00 1D`/`00 1E` | branches `0x012C3EEC` / manager `0x012C2190` | liveness/mapping control, including a ten-byte `00 1E` response containing connection IDs | Confirmed behavior; protocol names Unknown |

The executable groups four adjacent opcode values for each reliable-transport operation. `09`-`0C` and `0D`-`10` share the same receiver branch and channel calculation, while ordering and acknowledgement controls occupy `11`-`14` and `15`-`18`. This strongly indicates four reliable channels, with data and fragment forms per channel. Only the base opcodes used by the current Stardust server are presently named. The replacement client should initially implement that base channel and retain an explicit channel index in its design rather than discarding the variant information.

`reliable_channel_receive_VA_012C7CF0` confirms the receive algorithm itself. It reads the big-endian 16-bit sequence from bytes 2-3, expands it around the channel's wider logical receive counter, and computes the channel/form from the opcode. For the exact expected sequence it passes bytes after the four-byte SOE header into `reliable_payload_or_fragment_VA_012C80E0`, increments the expected counter, and drains consecutively buffered packets. Future packets are copied into a sequence-indexed buffer; older/duplicate or already occupied entries are counted and discarded. The response constructed at the end is `00`, then either `0x15 + channel` carrying `expected - 1`, or `0x11 + channel` carrying the observed future sequence. This is **Confirmed executable behavior** and independently matches the server reliability model.

`reliable_payload_or_fragment_VA_012C80E0` receives an explicit form value. Form zero recursively dispatches the payload immediately. Form one implements fragment assembly: on the first piece it parses a big-endian 32-bit total length, allocates that exact buffer, appends fragment payload bytes with a bounded copy, and recursively dispatches the completed buffer when the accumulated byte count equals the declared total. The assembly buffer and counters are then released/reset.

There is no client-side maximum check on that declared U32 total before allocation. The routine also reads the first four payload bytes before proving the first fragment contains four bytes. Continuations compute `min(fragmentBytes, remaining)` but throw if that minimum differs from the supplied fragment length, so an overlong continuation is rejected only after the assembly allocation exists. This is **Confirmed executable behavior**. The missing limit is not hidden in `reliable_channel_ctor_VA_012C6650`: that constructor caps individual negotiated datagram payload/window parameters but does not constrain the reassembled total. A replacement must require first-fragment length at least four, enforce a configured total-message cap before allocation, reject zero/overflow/pathological totals, and discard the whole assembly on any mismatch.

On the sender-control side, `acknowledgement_receive_VA_012C8210` expands the 16-bit acknowledged sequence around the sender's wider sequence state and repeatedly calls `release_acked_packet_VA_012C82C0` over the cumulative range. `0x012C82C0` finds retained sequence entries, updates adaptive timing/statistics, releases their message references, and clears the slots. The `00 11` path calls the same release primitive only once for the named future sequence; it does not directly invoke retransmission. Retransmission is independently timer-driven by `reliable_resend_check_VA_012C7530`.

## Direct outbound path

The executable's application-to-wire path is now bounded by the following functions:

```text
typed SWG serializer
  -> message_submit_VA_005A8D60
  -> serialized_message_transport_submit_VA_012B7F60
  -> Boolean converted to numeric delivery mode 0 or 4
  -> UDP adapter send (`0x012BD9D0` -> `0x012C3020`)
  -> soe_send_dispatch_VA_012C3180
  -> immediate, batched, or reliable-channel sender
  -> datagram_prepare_VA_012C5980
  -> udp_connection_raw_send_VA_012C54B0
  -> UDP manager (`0x012C1F20`; `sendto` site `0x012C20ED`)
```

`serialized_message_transport_submit_VA_012B7F60` accepts a serialized message object and a Boolean delivery flag. True becomes numeric mode 4 and causes a pending-capacity check through `0x012B7800`; false becomes mode 0. No bytes are prepended: the original buffer pointer and exact serialized size are passed to the configured transport adapter. The active UDP adapter path is `0x012BD9D0`, which tests the underlying connection state and calls `0x012C3020`. Alternative branches in this wrapper support other transport/logging configurations and must not be confused with the game's UDP path.

`soe_send_dispatch_VA_012C3180` selects behavior from the delivery-mode value: 0 aggregates, 1 prepares immediately, 2 wraps as internal `00 1B` then aggregates, 3 wraps then prepares immediately, and 4-7 dispatch to reliable channels 0-3 through `0x012C6A40`. Oversized modes 0-3 promote to mode 4. Numeric routing is Confirmed; stripped public enum labels for modes 0-3 remain Unknown.

`datagram_prepare_VA_012C5980` is the final transform/checksum stage. It preserves the first opcode byte when nonzero, or both bytes when the first byte is zero, and applies as many as two configured transform callbacks to the remainder. If the negotiated trailer width at connection `+0x120` is positive, it calls `0x012C8D30` over the transformed bytes using the seed at `+0x11C`, then writes the low one through four checksum bytes in big-endian order. It finally calls `udp_connection_raw_send_VA_012C54B0` with the prepared pointer and final length. This order is **Confirmed**:

```text
SOE header/prefix preservation -> transform callback 1 -> transform callback 2
-> checksum over transformed packet -> big-endian checksum trailer -> UDP send
```

The checksum routine `seeded_crc32_VA_012C8D30` is a table-driven CRC-32. It incorporates all four bytes of the negotiated seed, processes every prepared datagram byte, and complements the final accumulator. Comparison with `MMOEngine/src/engine/service/proto/BaseProtocol.cpp::generateCrc` confirms the same operation and lookup-table shape. The wire trailer is the most-significant-to-least-significant big-endian representation truncated to the negotiated one-to-four-byte width. This algorithm identification is **Confirmed by executable mechanics and server source correlation**.

The transform setup at `0x012C63C0` loops over exactly two selector slots and accepts numeric selectors 0 through 4. Its jump table is now fully mapped: selector 0 installs identity copy callbacks `0x012C6120`/`0x012C6150`; selector 1 installs provider virtual slots `+4/+8` through `0x012C6180`/`0x012C61B0`; selector 2 installs provider virtual slots `+0x0C/+0x10` through `0x012C61E0`/`0x012C6210`; selector 3 installs seed-plus-generated-keystream callbacks `0x012C6240`/`0x012C62B0`; selector 4 installs seed-only chaining callbacks `0x012C6320`/`0x012C6370`. Values above 4 leave the slot unconfigured. Selector 1's provider is **Confirmed as the shared zlib compressor layer** by executable RTTI (`Compressor`, `ZlibCompressor`), `SetupSharedCompression::install`, embedded zlib version `1.1.4`, and concrete wrappers `0x00A88B20`/`0x00A88BE0` calling statically linked deflate/inflate. The deployed response `1,4` therefore means zlib followed by the seed-only chain. Selector 2's concrete provider methods `0x012BD590/0x012BD5C0` are length-preserving copies. Selector 3 is generated-pad XOR plus ciphertext chaining on complete LE32 words, with pad-only XOR for a final one-to-three-byte tail. Selectors 2 and 3 are supported but not selected by the current server. Their behavior is **Confirmed**; only their unpublished public names remain unknown.

Selector 3's pad generator at `0x012C8CD0` uses `next=(state%127773)*16807-(state/127773)*2836+123`, adding `2147483647` when the result is non-positive and emitting its low byte. The connection seed at `+0x11C` initializes the ciphertext chain. The inverse updates that chain from each incoming ciphertext word. `soe_selector3_vectors.py` preserves a runnable model and golden vectors; see the optional-transform checkpoint for the complete byte contract.

The server's `BaseProtocol::compress` gives the matching packet policy: preserve one opcode byte, or two bytes when the first byte is zero; compress only the body excluding the final compression flag and two CRC placeholder bytes; retain compressed output only when `compressed_size + 3 < original_packet_size`; write compression flag `1` for retained zlib data and `0` when compression is not beneficial. On receive, CRC is checked first, selector-4 decryption occurs next, and the byte immediately before the two-byte CRC is tested for compression. The executable's provider identity and transform order are confirmed, while a byte-exact zlib 1.1.4 output fixture still requires running the historical codec or a compatible linked harness.

`udp_connection_raw_send_VA_012C54B0` updates send counters/timestamps and calls the UDP manager's raw-send method `0x012C1F20`, whose system-call endpoint is the previously confirmed `sendto` at `0x012C20ED`. Reliable channel objects retain references to the prepared outgoing transport entries; the ACK path releases those retained entries. Consequently retransmission must resend the already transport-prepared representation or reproduce its exact sequence/checksum state, rather than reserializing mutable game state.

## Reliable sender and resend scheduler

Four per-channel transport objects are constructed by `reliable_channel_ctor_VA_012C6650`. The constructor derives usable transport payload from the negotiated datagram limit at the owning connection's `+0x12C`, subtracting transform/checksum overhead at `+0x194` and `+0x120`, then subtracting the four-byte SOE data/sequence header. When its fragmentation option is enabled, the direct-payload threshold is reduced by another five bytes. The client therefore computes limits from negotiated/configured values rather than relying solely on a hard-coded 490-byte test.

`reliable_channel_submit_VA_012C6A40` accepts two buffer spans. If their combined size fits the derived direct threshold it passes them to `reliable_multipayload_append_VA_012C7210`. That routine creates an outer `00 19` buffer, writes a variable-length encoded submessage size with `0x012C8E60`, appends the spans, and flushes when negotiated capacity would be exceeded. If the payload is oversized, `0x012C6A40` flushes pending aggregation, creates an owned message copy, and enqueues it through `reliable_queue_append_VA_012C7340` for fragmentation by the sender pump.

The variable-length size codec is directly paired:

- `0x012C8E60`: one byte below `0xFE`; `FF` plus big-endian 16-bit below `0xFFFF`; `FF FF FF` plus big-endian 32-bit otherwise.
- `0x012C8EC0`: inverse decoder returning prefix width 1, 3, or 7.

`reliable_sender_pump_VA_012C73B0` pulls queued messages while configured outstanding/window constraints permit. It creates `0x20`-byte sequence-table entries, assigns a wider logical sequence, and writes its low 16 bits in network order. The opcode is `0x09 + channel` for ordinary reliable data and `0x0D + channel` for fragmented reliable data. The first fragment includes a four-byte big-endian total payload length. Each entry retains the prepared packet reference, send timestamp, payload contribution, and retry/accounting state; resends do not assign a new sequence.

`reliable_resend_check_VA_012C7530` walks the logical outstanding interval and locates retained entries through the sequence-indexed table. It calculates eligibility from timestamps and adaptive delay fields, resubmits eligible prepared packets, updates retry/error counters, and adjusts the next wake time. It also has a configured maximum-age/failure path that disconnects with internal reason 8. An observed initial field value is `0x320` (800), but its exact public meaning and unit remain unresolved.

The resend routine reconstructs the reliable transport prefix without allocating a new logical sequence: opcode `0x09 + channel` for ordinary entries or `0x0D + channel` for fragment entries, followed by the existing sequence in network byte order and the fragment-total field where applicable. It then routes the retained payload through the ordinary aggregation/send path and refreshes the entry timestamp. Callers at `0x012C4D9C`, `0x012C6DCC`, and `0x012C7146` show that timeout processing is integrated into connection/channel pumping, rather than being triggered by `00 11`.

Loss example: if sequences 0, 1, and 2 are retained and the peer reports `00 11 00 02`, the client releases entry 2 only. Entries 0 and 1 stay outstanding. A later cumulative `00 15 00 01` releases 0 and 1, while expiration before that ACK causes the timer path to resend them. This selective-release behavior is a required compatibility test.

This agrees conceptually with Stardust server `BaseClient.cpp`: retain sequenced packets, fragment large messages, resend bounded outstanding work, adapt checkup timing, and release cumulatively on ACK. Server constants such as 490 bytes and the 50-packet pressure limit remain interoperability targets, not proven client constants unless independently observed.

At `0x012C36B0`, opcodes `01`, `02`, `1D`, and `1E` bypass the normal validation/transform path. Other established packets use the connection's configured trailer width at `+0x120` (one through four bytes), compare a big-endian trailer with the value produced by `0x012C8D30` using the seed/key at `+0x11C`, and then pass the payload through the two configured inverse callbacks. With deployed selectors `1,4`, these are the confirmed zlib and seed-chain inverse transforms in reverse wire-processing order.

Confirmed from source:

- packet opcodes and constructors above;
- session seed exchange;
- transform/compression/CRC order and algorithms;
- independent 16-bit directional sequencing;
- ACK, buffering, multipayload, and fragment rules;
- server thresholds and default feature toggles.

Still requiring validation:

- exact original-client connection-ID generation;
- byte-exact zlib 1.1.4 compressed output and malformed-stream behavior;
- original-client behavior at sequence wraparound and prolonged loss;
- deployed values of compression/encryption-related server properties;
- operational significance of the server zero-CRC status response/client CRC-gate mismatch;
- outer Multi-SOE extended sizes;
- observed server build flags (`LOCKFREE_BCLIENT_BUFFERS`, `WITH_STM`) and their timing effects;
- byte-for-byte golden datagrams from the running Stardust server.

## Recommended validation fixtures

1. Session request and response fixed vectors.
2. Short uncompressed transformed data packet with seeded CRC.
3. Packet where compression is requested but not beneficial.
4. Compressed payload vector.
5. Sequence values `0`, `1`, `0x7FFF`, `0xFFFF`, and wrap to `0`.
6. Duplicate, future, and reordered receive sequences.
7. One-message, `0x1900` multi-message, and outer `0x0300` Multi-SOE packets.
8. First/middle/final fragments, oversized total, truncated fragment, and overrun.
9. ACK, out-of-order, net-status, and disconnect vectors.

Fixtures should be generated by a small server-linked test harness or existing unit-test target, not copied from ad hoc pseudocode.

## Connection creation and endpoint migration addendum

The normal game UDP creation path supplies establishment timeout `0`; the state-0 deadline check is conditional on a positive value. Session requests continue to be retransmitted, but that lower layer does not terminate the normal attempt on its own.

Transport opcode `0x001E` is an endpoint-remap/migration packet. The UDP manager accepts it for an otherwise unknown endpoint only after connection-ID lookup and negotiated-token validation, then rekeys its endpoint hash entry to the packet's source address and port. A replacement should preserve authenticated NAT rebinding while rejecting unauthenticated endpoint changes.

The shared constructor also contains a real nonblocking TCP stream path. Its deployed role is unresolved because the inspected Stardust login/zone source exposes datagram services.

### TCP alternative framing

The stream path does not use the UDP SOE reliability envelope. Each application payload is framed as `length:u32le` followed by `length` raw bytes. Sends may be coalesced until the queued byte count reaches `tcpMinimumFrame=1000`; the common pump flushes smaller pending batches. Receives retain partial header and payload state across overlapped completions and dispatch multiple complete frames serially.

The legacy receive boundary has no proven maximum-frame guard before accumulation. A replacement TCP compatibility path must reject zero/oversized or arithmetically invalid lengths before allocation and should make its cap independent of pointer size.

## Replacement implementation increment (2026-09-29)

The engine-neutral core now discards invalid established framing before channel
mutation, clears owned login/transport state on fatal/malformed/deadline paths,
and pumps bounded queued fragments across ACK windows. Selective releases do
not allow the logical outstanding span to outrun the reorder budget. Outer
`00 03` uses one-byte lengths, bounded nesting and all-control return values;
protected ping is ignored, and matching peer disconnect closes without a reply.
The server's request-ID read/re-emission chain preserves the original wire bytes.

These paths have independent synthetic peer tests, including fragmented
multipayload and loss/corruption/reorder traces. They do not close status/liveness,
CRC-disabled status framing, optional leaves, adaptive timing, historical zlib,
or live-server validation. The fixed response deadline and storage limits are
replacement policies. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_TRANSPORT_RECOVERY.md)
and [acceptance register](CLIENT_IMPLEMENTATION_ACCEPTANCE.md).

## Round71 peer-disconnect lifecycle proof

[Disconnect ownership](SCENE_WORLD_PEER_DISCONNECT_CONTRACT.md) uses actual frozen codec/ingress/bridge/FIFO/pump. Matching ID closes transport without closing borrowed scene; caller drains pending scene/root owners and canceled resources.39 component checks and109-update joined closure/wrong-ID failure pass. Reason bytes remain uninterpreted; no schema or345-row matrix change, socket/send/live phase claim.

## Round72 outer disconnect completion prefix

[Ordering and ownership](SCENE_WORLD_MULTIPACKET_DISCONNECT_CONTRACT.md) pass52 component checks and109-update joined closure/failure. Outer transport validates all framing then dispatches through matching disconnect, retaining only prior application/control output. Caller explicitly applies or drops queued prefix; suffix never dispatches. Exceptional untransferred ACK arrays now clear with applications; successful caller-owned arrays survive. Source-inspected private zeroing is distinguished from executed lifetime tests. No wire schema/345-row matrix, reason semantics or live phase claim changes.

## Round74 elapsed offline scene

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R74_ELAPSED_OFFLINE_HOST.md) passes1000 motion updates with actual separate fixed presentation steps, original supplied poses/17 events/HUD/camera and90 offline protected roots/ACKs. [Planner](TERRAIN_OFFLINE_FRAME_CLOCK_CONTRACT.md) separates reserved slots and explicit time drops from completed motion; [host](SCENE_WORLD_FIXED_STEP_PRESENTATION_HOST_CONTRACT.md) preserves its exact old signature/default proof. [Reusable drain](SCENE_WORLD_OFFLINE_ROOT_MOTION_DRAIN_CONTRACT.md) validates ACK before FIFO take and owns one bridge. Measured GPU593 plans/10,013,260us/no time drop; supplied headless379 plans/exact drop accounting. Rejected first stop ACK preserves one queued frame until caller cleanup; prior Version9 passes1000. Modern offline policy, original actor/camera/cadence/live limits remain explicit.
