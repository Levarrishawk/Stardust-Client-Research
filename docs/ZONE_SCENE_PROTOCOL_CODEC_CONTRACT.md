# Zone scene application codec contract

R1-NET-01, October 1, 2026. Offline engine-neutral boundary; no transport framing,
connection, authentication, scene application or ready acknowledgement.

## Evidence and confidence

[Packet map](PACKET_HANDLER_MAP.md) records the client SelectCharacter constructor
at VA `00AE4B60`, send path and CmdStartScene parser at VA `00AC9E20`.
[Selection checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_SELECT_CHARACTER_SESSION.md)
confirms the original request has exactly 14 bytes. The baseline executable SHA-256
was rechecked as `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
[Login/zone state specification](LOGIN_AND_ZONE_STATE_MACHINE.md), lines 120–146,
records the scene parser, lifecycle and server correlation.

Static server correlation in sibling Stardust-3:
`MMOCoreORB/src/server/zone/packets/zone/SelectCharacterCallback.h`,
`SelectCharacterCallback::parse` (lines 40–42), reads U64 at offset 6.
`SelectCharacter.h`, constructor (lines 12–18), appends a helper SWGEmu hash;
that extension is absent from the client-backed request and rejected here.
`CmdStartScene.h`, constructor (lines 24–40), emits count/opcode, zero byte,
object ID, terrain, X/Z/Y, template and galactic time. Its zero byte is server
behavior; the flag's general client semantics remain **Unknown**.
Wire structure/order is **Confirmed** by the durable client evidence and server
boundary. Strict malformed-input rejection below is an explicit replacement
policy, not a claim about all legacy parser failure paths.

## Wire and API

All multibyte fields are little endian, packed without native padding.

| Message | Fields in wire order |
|---|---|
| SelectCharacter | U16 count=2, U32 opcode=B5098D76, U64 object ID |
| CmdStartScene | U16 count=9, U32 opcode=3AE6DFAE, U8 raw flag, U64 object ID, ASCII16 terrain, F32 X, F32 Z, F32 Y, ASCII16 template, U64 galactic time |

ASCII16 is an unsigned U16 byte count followed by exactly that many ASCII bytes,
without a terminator. Empty strings, ASCII control bytes and the entire U16
length range are accepted. Raw strings are copied into immutable owned strings;
slashes, backslashes, casing and path-like data are preserved with no filesystem
normalization. Identifiers/time are unsigned full-width values. All byte flag
values are preserved, with no Boolean conversion or semantic rejection.

`ZoneSceneProtocolCodec.EncodeSelectCharacter(ulong)` returns a fresh owned
14-byte array. `DecodeSelectCharacter(ReadOnlySpan<byte>)` and
`DecodeCmdStartScene(ReadOnlySpan<byte>)` return sealed immutable records holding
scalars and owned strings, retaining no source storage. Parsing does not require
selected-object state, validate ownership or perform state transitions. A future
state machine must separately validate legal phases and expected objects.

Both decoders throw `InvalidDataException` for incorrect operand count/opcode,
truncated header/body, a string count exceeding remaining bytes, any non-ASCII
string byte, trailing bytes and (for scene coordinates) NaN or either infinity.
No partially decoded DTO escapes on failure; there are no retained resources
requiring cleanup. Finite negative/zero coordinates are accepted.

## Compatibility consequence and verification

Preserve X/Z/Y explicitly at the protocol boundary; map to an engine only in a
later adapter. Preserve the flag without inferring semantics. The operational
sequence in [network prose](NETWORK_ARCHITECTURE.md) takes precedence over the
older abbreviated diagram: scene bootstrap baselines precede client ready,
and server ready echo precedes post-ready traffic. This codec emits no ready.

The native scenarios contain hand-authored synthetic golden hex, high-bit U64s,
unequal coordinates, every truncated prefix, corrupt headers, lengths/ASCII,
trailing bytes including helper suffix, non-finite coordinates, arbitrary flags,
zero ID, empty strings and source-mutation ownership checks.
`tools/python/verify_zone_scene_protocol.py` independently packs/parses with
Python struct and compares actual C# encoded bytes and decoded JSON exports.
These fixtures are synthetic, not captured traffic or original assets.

Proposed shared smoke runner call: `ZoneSceneProtocolScenarios.Run()`.
The worker's standalone runner links that scenario file; lead integration owns
any edit to shared Program.cs. Next bounded milestone: offline selected-object
and scene transition state policy against independently specified transcripts.

## Offline deferred readiness

[Scene transition coordinator](ZONE_SCENE_TRANSITION_CONTRACT.md) now consumes these DTOs, stages bounded opaque application messages and emits header-only CmdSceneReady once after a current-generation minimum-resource barrier. Inbound ready marks final state. No terrain implementation or connection is added; header bytes contain no generation token.
