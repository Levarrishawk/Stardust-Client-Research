# Scene object lifecycle codec contract

R4-NET-01, October 2, 2026. Exact engine-neutral application-message encode/decode
only; no object registry, replication application, terrain, scene or transport.

## Evidence and confidence

[Packet map](PACKET_HANDLER_MAP.md) records original create parser `00AD51B0`,
destroy parser `00AD55D0`, containment parser `00ADC6D0`, and end-baselines parser
`00AD58D0`. [Login/zone lifecycle](LOGIN_AND_ZONE_STATE_MACHINE.md) records create
transform order and baseline-finalization placement.
[Replication specification](REPLICATION_SCHEMA_MAP.md), replacement implications,
and [end-baselines checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-13_SCENE_END_BASELINES.md)
distinguish SceneEndBaselines from destruction and the server producer alias.
Baseline executable SHA256 rechecked:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
These already documented field widths/order and lifecycle identities are
**Confirmed**. No new executable behavior is inferred from server code.

Independent current server boundary: sibling Stardust-3 under
`MMOCoreORB/src/server/zone/packets/scene/`:

- `SceneObjectCreateMessage.h`, constructors: count=5, FE89DDEA, U64, seven floats,
  CRC, byte. First constructor emits quaternion X/Y/Z/W and position X/Z/Y.
- `SceneObjectDestroyMessage.h`, constructors: count=3, 4D45D504, U64, byte.
- `UpdateContainmentMessage.h`, both constructors: count=4, 56CBDE9E, two U64,
  `uint32 type` via insertInt. Null container in first constructor produces ID0.
- `SceneObjectCloseMessage.h`, both constructors: count=2, 2C436037, U64 only.
  Its wire identity is SceneEndBaselines, not the C++ producer class name.

Curated packet rows correlate these exact producer paths/opcodes. Existing rows,
packet maps and research metrics are unchanged. Server buffer capacities such as
BaseMessage(18/30) are allocation sizes, not wire lengths. The create overload
with explicit ID/CRC emits zero quaternion; this is server-only evidence and does
not establish any original quaternion repair behavior.

## Exact bytes and DTO API

All multibyte fields are little endian, with no alignment padding. Floats are
IEEE754 binary32. IDs are U64, template CRC is U32.

| Message | Count / opcode | Body in order | Total bytes |
|---|---|---|---|
| SceneCreateObjectByCrc | 5 / FE89DDEA | U64 ID; F32 quaternion X,Y,Z,W; F32 position X,Z,Y; U32 template CRC; U8 raw hyperspace flag | 47 |
| SceneDestroyObject | 3 / 4D45D504 | U64 ID; U8 raw hyperspace flag | 15 |
| UpdateContainment | 4 / 56CBDE9E | U64 child ID; U64 container ID; U32 arrangement bits | 26 |
| SceneEndBaselines | 2 / 2C436037 | U64 ID | 14 |

Each includes the standard U16 count/U32 opcode header. Counts are exact header
constants, not a value computed by counting individual float components.
`SceneObjectLifecycleCodec` exposes `EncodeCreate/DecodeCreate`,
`EncodeDestroy/DecodeDestroy`, `EncodeContainment/DecodeContainment`, and
`EncodeEndBaselines/DecodeEndBaselines`. Their sealed immutable records contain
only owned scalar values, with explicit field names preserving wire order.
Decode borrows a span only during the call; no source buffer/reference survives.
Encode returns a fresh independently owned byte array each time.

Hyperspace fields remain raw bytes. All 0..255 values round-trip; no Boolean
conversion or rejection of noncanonical flag values occurs. Server constructors
normally emit 0/1, but that does not justify altering other input bytes. Their
future/noncanonical semantic meaning remains **Unknown**.
`ArrangementBits` is uint, justified by server `uint32 type` and the documented
LE32 member. It preserves FFFFFFFE/FFFFFFFF without converting them to -2/-1;
the original semantic signedness of arrangement interpretations is not inferred.
A later state layer can explicitly interpret those bits when evidence requires.

Quaternion components are never normalized, reordered or repaired. Zero and
non-unit quaternions are accepted. Finite float bit patterns including negative
zero, subnormal values and extrema round-trip unchanged. Positions retain X/Z/Y
at this boundary; coordinate conversion belongs to a separate engine adapter.
Object ID0, container ID0 and arbitrary full-width CRC/arrangement bits are
accepted as bytes; existence, ownership and legal lifecycle order are separate.

## Modern rejection policy and consequences

Both decoders and encoders reject non-finite create quaternion/position values
with InvalidDataException. Decoders reject incorrect exact length, operand count
or opcode (including truncation and trailing data) with InvalidDataException.
Encoders reject null DTO with ArgumentNullException. No partially decoded DTO
escapes. These strict rejection rules are explicit **modern policy**, not a claim
that all original parsers had these failure paths. Rejected calls retain no
resources or mutable queue state and leave supplied immutable DTOs unchanged.

EndBaselines decoding does not destroy or finalize an object; it only returns
its ID. Destroy decoding similarly performs no removal. Existing lifecycle
semantics remain evidence for a later registry implementation, outside this task.

## Verification and integration

Hand-authored golden hex covers high-bit object/container IDs and CRC,
arrangement FFFFFFFE, raw flags A5/FF, asymmetric quaternion .25/-.5/.75/1.25,
and unequal negative/mixed position -10/21/-30.5. Native tests cover every
truncated prefix, extra byte, every header-byte corruption, cross-message
rejection, all seven non-finite fields (encode/decode), all 256 flags, null DTOs,
zero IDs, finite bit preservation and source/output mutation ownership.
Independent Python struct assembly/parser verifies actual C# encoded files and
full decoded DTO JSON, plus its own malformed/flag cases. All fixture bytes are
synthetic and independently authored; no proprietary asset/capture is included.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R4_SCENE_OBJECT_CODECS.md)
records exact reproduction and scope. Proposed shared runner registration:
`SceneObjectLifecycleScenarios.Run()`. Lead owns that edit. Next bounded question:
offline lifecycle registry policies on these DTOs, with object existence/readiness
kept separate from byte parsing. No additional milestone begins here.
