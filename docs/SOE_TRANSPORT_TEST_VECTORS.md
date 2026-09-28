# SOE Transport Test Vectors

These fixtures apply to `Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. They are analysis fixtures for interoperability tests, not captured production credentials or attempts to bypass access controls.

`tools/verify_transport_vectors.py` independently replays the seeded CRC and selector-4 algorithms and checks nine assertions covering the primitive, uncompressed reliable packet, synthetic `LoginClientId`, and channel-zero ACK fixtures. On 2026-09-11 all nine passed. It performs no network access and is research validation, not replacement-client implementation.

Confidence vocabulary: **Confirmed** means directly established from executable behavior or exact server code; **Strong** means executable mechanics plus corroborating server semantics; **Tentative** remains a working interpretation.

## Session request

Example values:

- protocol version: `2`
- client connection ID: `0x12345678`
- requested maximum datagram: `496` (`0x000001F0`)

```text
00 01 00 00 00 02 12 34 56 78 00 00 01 F0
```

| Offset | Size | Value | Meaning | Confidence |
|---:|---:|---|---|---|
| 0 | 2 | `00 01` | Session request opcode | Confirmed |
| 2 | 4 | `00 00 00 02` | Protocol/version value | Confirmed value and validation; label Strong |
| 6 | 4 | `12 34 56 78` | Client connection ID, big-endian | Confirmed |
| 10 | 4 | `00 00 01 F0` | Requested maximum datagram size, big-endian | Confirmed downstream use |

The executable's manager accepts an unknown-peer session request only when it is exactly 14 bytes and the offset-2 value is 2. The current server `SessionIDRequestMessage` constructor is only 10 bytes and appears to be legacy/client-side scaffolding. Its receive parser still reads the connection ID at offset 6 and ignores trailing request fields. Therefore the executable's 14-byte request is the authoritative proof-of-concept fixture; the header constructor must not be used as a contradictory wire specification.

## Session response

Example values:

- echoed connection ID: `0x12345678`
- session/CRC seed: `0xA1B2C3D4`
- CRC trailer width: `2`
- transform selector 1: `1`
- transform selector 2: `4`
- negotiated maximum datagram: `496`

```text
00 02 12 34 56 78 A1 B2 C3 D4 02 01 04 00 00 01 F0
```

| Offset | Size | Value | Meaning | Confidence |
|---:|---:|---|---|---|
| 0 | 2 | `00 02` | Session response opcode | Confirmed |
| 2 | 4 | `12 34 56 78` | Echoed client connection ID | Confirmed |
| 6 | 4 | `A1 B2 C3 D4` | Session/CRC seed | Confirmed |
| 10 | 1 | `02` | CRC trailer width in bytes | Confirmed by direct downstream use |
| 11 | 1 | `01` | First transform selector | Confirmed selector; compression label Strong |
| 12 | 1 | `04` | Second transform selector | Confirmed selector; seed-chained XOR label Strong |
| 13 | 4 | `00 00 01 F0` | Negotiated maximum datagram size | Confirmed by direct downstream use |

The client handler at `0x012C3D0E` verifies the echoed ID, stores the remaining fields at connection offsets `+0x11C` through `+0x12C`, calls transform installation at `0x012C63C0`, marks the connection established, and invokes its connected callback. Stardust server `SessionIDResponseMessage` emits the same fixed tail `02 01 04 00 00 01 F0`.

## Seeded CRC primitive

For seed `0x12345678` and input bytes:

```text
00 09 00 00
```

the complete CRC produced by `seeded_crc32_VA_012C8D30` and server `BaseProtocol::generateCrc` is:

```text
D0 88 33 17
```

Trailer truncation examples:

| Negotiated width | Trailer bytes |
|---:|---|
| 1 | `17` |
| 2 | `33 17` |
| 4 | `D0 88 33 17` |

This is a primitive checksum fixture, not a complete established on-wire data packet. A complete fixture must first apply the negotiated transforms and then calculate the CRC over the transformed packet.

## Variable-length size codec

The encoder at `0x012C8E60` and decoder at `0x012C8EC0` use these confirmed boundary encodings:

| Decimal/value | Encoded bytes |
|---:|---|
| 0 | `00` |
| 1 | `01` |
| 253 | `FD` |
| 254 | `FF 00 FE` |
| 255 | `FF 00 FF` |
| 65534 | `FF FF FE` |
| 65535 | `FF FF FF 00 00 FF FF` |
| `0x12345678` | `FF FF FF 12 34 56 78` |

Rule:

- values below `0xFE`: one byte;
- values below `0xFFFF`: `FF` plus big-endian 16-bit;
- all other values: `FF FF FF` plus big-endian 32-bit.

## Selector-4 seed-chain transform primitive

This fixture isolates the selector-4 callback over the bytes following a preserved two-byte SOE header. It deliberately does not model selector-1 compression framing.

Seed:

```text
12 34 56 78
```

Input:

```text
00 09 00 01 10 20 30 40 50
```

Transformed output:

```text
00 09 78 57 24 32 48 38 28
```

The transform interprets each complete body word using the executable's native little-endian word order, XORs it with the current 32-bit chain value, and makes the resulting ciphertext word the next chain value. Each remaining tail byte is XORed with the low byte of that final chain value. The inverse retains each ciphertext word before XOR and uses it as the next chain value. This matches the mechanics of client callbacks `0x012C6320`/`0x012C6370` and server `BaseProtocol::encrypt`/`decrypt`.

Applying the confirmed seeded CRC primitive to the transformed nine-byte result gives full CRC `0x64AEB373`; at the deployed two-byte width the trailer is `B3 73`. The resulting selector-4-plus-CRC fixture is:

```text
00 09 78 57 24 32 48 38 28 B3 73
```

This is still not claimed as a complete deployed data packet because selector 1 may add or interpret compression framing before selector 4 runs.

## Required next fixtures

1. Byte-exact selector-1 compressed output from the identified historical zlib
   1.1.4 implementation for beneficial input. Modern zlib round trips are not a
   historical encoder golden.
2. A sanitized controlled-server transcript after that run is separately
   authorized.

## Two-fragment golden hashes

The deterministic 600-byte application payload is byte `i & 0xFF` at offset
`i`, with seed `0x12345678`, channel zero, compression flag 0, and CRC width 2.
The first fragment carries sequence 0, BE32 total 600, and the first 485 bytes;
the final fragment carries sequence 1 and the remaining 115 bytes.

| Fragment | UDP length | SHA-256 of complete transformed+CRC datagram |
|---|---:|---|
| first | 496 | `4ae6870e65d8c69dc50305226e489dc1364336ac647718a40909ff485198a6ba` |
| final | 122 | `0f3a79ad759b761a16aa6ed5d10c69024b062c823cadc41fe1d1cadcac04fa26` |

`tools/verify_transport_vectors.py` independently constructs both datagrams
and checks these hashes. The C# implementation checks the same constants before
its ordered and reordered reassembly scenarios. This records first/final wire
coverage.

The companion 1,100-byte payload uses the same `i & 0xFF` pattern and produces
an actual first/middle/final split:

| Fragment | Sequence | UDP length | SHA-256 |
|---|---:|---:|---|
| first | 0 | 496 | `1970b660cc4019fd8cc99cae115932c20f81fad03c05b0a0e3fa6a1131e0039f` |
| middle | 1 | 496 | `3b5dd8e17138ba36a1f414ad047df6111570cc3e2dd0d36119886c3382488ea4` |
| final | 2 | 133 | `be71a3c08dae3227c01bd8f8d36382b480ee8360de68b983c1c5199385f01b63` |

Both independent verifiers check all three complete datagrams.

## Selective out-of-order acknowledgement scenario

This state-machine vector is derived from confirmed executable control flow and should be implemented before live interoperability testing.

1. Sender retains reliable channel-0 entries with logical sequences 0, 1, and 2.
2. Peer receives sequence 2 while still expecting sequence 0 and sends bytes `00 11 00 02` before the normal transform trailer is applied.
3. Sender expands wire sequence 2 and releases retained entry 2 only through the same per-entry primitive used by cumulative ACK processing.
4. Retained entries 0 and 1 remain outstanding and eligible for timer retransmission.
5. If peer later sends cumulative ACK bytes `00 15 00 01`, sender releases entries 0 and 1.

Assertions:

- `00 11` must not directly resend sequence 0 or 1.
- `00 11` must not cumulatively release entries 0 and 1.
- A later timeout may resend 0 and 1 with their original sequence numbers.
- The same scenario must be repeated across `0xFFFF -> 0x0000` wrap using logical sequence expansion, not raw unsigned-16 comparison.

The current Stardust server's `resendPackets(int seq)` returns immediately and therefore is not evidence against this executable behavior; it is a server-side divergence to preserve in the compatibility record.

The independent Python verifier and C# suite both expand wire sequence `0000`
near logical `0xFFFF` to `0x10000`, expand wire `FFFF` near `0x10000` back to
`0xFFFF`, and verify that selective release of wire `0000` leaves logical
`0xFFFF` retained. The C# suite additionally sends and cumulatively acknowledges
all 65,537 logical sequences through the actual channel state machine.

## Compression framing status

Selector 1 is confirmed as the executable's statically linked `ZlibCompressor`, reporting embedded zlib version `1.1.4`. Packet framing is corroborated by server `BaseProtocol`:

```text
preserved SOE opcode prefix | body or zlib(body) | compression flag | CRC16 trailer
```

- Compression input excludes the preserved one- or two-byte opcode prefix and the final three framing bytes.
- Flag `01` means the body is zlib-compressed.
- Flag `00` means compression was skipped because it was not beneficial.
- CRC is calculated after compression framing and selector-4 transformation.

No byte-exact compressed output is recorded yet. Deflate output can vary across zlib versions despite decoding to the same bytes, so a modern library-generated stream is not accepted as a golden replacement for the executable's identified zlib 1.1.4 output.

## Complete uncompressed established-data fixture

This fixture exercises the deployed selector chain without requiring deflate output. Values are deliberately synthetic:

- seed: `0x12345678`
- reliable channel: 0 (`00 09`)
- sequence: `0`
- synthetic application payload: `AA BB`
- compression flag: `00` (body is not compressed)
- CRC width: 2

Pre-transform framed bytes, excluding the as-yet unwritten CRC trailer:

```text
00 09 00 00 AA BB 00
```

Interpretation:

```text
00 09 | 00 00 | AA BB | 00
opcode | seq   | data  | compression flag
```

After selector-4 seed-chain transformation:

```text
00 09 78 56 9E A9 78
```

The full seeded CRC is `0x00729231`, so the deployed two-byte trailer is `92 31`. Complete UDP payload:

```text
00 09 78 56 9E A9 78 92 31
```

Receiver verification order:

1. Verify CRC `92 31` over the first seven bytes with seed `0x12345678`.
2. Remove the CRC trailer.
3. Apply selector-4 inverse transform after the preserved `00 09` header.
4. Observe final marker `00`; do not invoke zlib.
5. Remove the marker and dispatch `00 09 00 00 AA BB` to reliable channel 0.

This is a complete transport-level fixture. `AA BB` is not asserted to be a valid SWG application message.

## Complete synthetic LoginClientId fixture

This fixture uses deliberately non-secret credentials to exercise the actual first application message:

- username: `u`
- password: `p`
- version: `20260817-SD:01`
- seed: `0x12345678`
- channel 0, sequence 0, compression flag 0, CRC width 2

Serialized application message:

```text
04 00 96 1F 13 41
01 00 75
01 00 70
0E 00 32 30 32 36 30 38 31 37 2D 53 44 3A 30 31
```

Field interpretation:

| Bytes | Meaning |
|---|---|
| `04 00` | serialized header operand value 4, little-endian 16-bit; this is not a count of trailing fields |
| `96 1F 13 41` | opcode `0x41131F96`, little-endian 32-bit |
| `01 00 75` | ASCII16 username `u` |
| `01 00 70` | ASCII16 password `p` |
| `0E 00 ...` | ASCII16 version `20260817-SD:01` |

The executable's `LoginClientId` class contains no fourth serialized field. In particular, do not append the `SWGEmu` name hash found in the server's convenience constructor; the server receive parser ignores it and `Stardust.exe` does not emit it.

Reliable framing before transforms:

```text
00 09 00 00 04 00 96 1F 13 41 01 00 75 01 00 70
0E 00 32 30 32 36 30 38 31 37 2D 53 44 3A 30 31 00
```

After selector-4 transformation:

```text
00 09 78 56 30 12 EE 49 23 53 EF 49 56 52 EF 39
58 52 DD 09 6A 64 ED 31 5B 53 C0 62 1F 69 F0 F1 C0
```

Full seeded CRC is `0x55CCF704`; deployed two-byte trailer is `F7 04`. Complete UDP payload:

```text
00 09 78 56 30 12 EE 49 23 53 EF 49 56 52 EF 39
58 52 DD 09 6A 64 ED 31 5B 53 C0 62 1F 69 F0 F1 C0 F7 04
```

This is a deterministic serialization/transport fixture, not authorization to send credentials or probe a server. Live validation must use an explicitly controlled Stardust test endpoint and test account.

## Channel-zero acknowledgement fixture

After accepting reliable channel-0 logical sequence 0, the receiver emits acknowledgement opcode `00 15` with sequence `00 00`. The non-compressed control packet carries marker `00` before its CRC placeholders.

```text
Pre-transform: 00 15 00 00 00
Transformed:   00 15 78 78 78
CRC32:         A8 14 39 46
CRC16 trailer: 39 46
UDP payload:   00 15 78 78 78 39 46
```

The ACK is cumulative through sequence 0. The executable delivers an in-order payload, advances the expected logical sequence, drains contiguous buffered successors, and acknowledges `expected - 1`.

## Synthetic LoginClientToken parser fixture

This application-level fixture contains no real token:

- synthetic token bytes: ASCII `T`
- embedded account ID: `0x11223344`
- station ID: `0x55667788`
- username: `u`

```text
04 00 C6 96 B2 AA
05 00 00 00
54 44 33 22 11
88 77 66 55
01 00 75
```

The opcode is `0xAAB296C6`. The declared length is 5: one token byte plus four account-ID bytes. An original-client-compatible parser preserves `54 44 33 22 11` as the opaque credential blob, then parses station ID and username. It may additionally expose decoded account ID `0x11223344`, but must not replace the stored blob with a host-layout structure.

## Synthetic ErrorMessage parser fixture

```text
03 00 1A F9 AB B5
01 00 45
03 00 62 61 64
01
```

This represents opcode `0xB5ABF91A`, type `E`, text `bad`, and fatal flag 1. It is an application-level parser fixture before reliable framing and transport transforms.
