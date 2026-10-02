# Owned replication application envelope codecs

R6-NET-01, October 2, 2026. Baselines/Deltas envelope boundary only, with opaque
schema fields. No object registry, baseline completeness, field index/value
validation, schema dispatch, application or scene/resource behavior.

## Evidence and corrected byte domains

[Replication schema map](REPLICATION_SCHEMA_MAP.md), common envelopes, records
count5, opcodes68A75F0C/12862153, ID/tag/view/size and inner U16 count. Original
baseline parser `00ADBC80` and delta parser `00ADB2D0` are already documented.
Original executable SHA256 rechecked:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Existing parser/count observations are **Confirmed**; no new VA labels recovered.

The initial assignment called the application header 27 bytes. Static source
inspection established the domain mismatch, and lead explicitly corrected the
assignment before implementation: the application header is **23 bytes**, not27.
Sibling Stardust-3 source paths/symbols:

- `MMOCoreORB/utils/engine3/MMOEngine/src/engine/service/proto/BaseMessage.h`,
  constructors, prepend SOE opcode/sequence with two insertShort calls (4 bytes).
- `MMOCoreORB/src/server/zone/packets/BaseLineMessage.h`, constructors, write
  application fields; setSize writes U32 at framed offset23 as size()-27.
- `MMOCoreORB/src/server/zone/packets/DeltaMessage.h`, constructor and close,
  use same fields; close writes payload size at framed23 and U16 count at framed27.

Existing `SoeReliableChannel.ReceiveSequencedPacket` extracts `packet[4..]`, and
`SoeClientTransport` returns reassembled application payloads without that prefix.
Thus framed27 becomes application23, and framed count27 becomes application23.
No transport prefix or invented padding belongs in this codec. Historical source
27-byte statements are not edited by this worker; lead owns durable clarification.

## Exact layout and count tolerance

All multibyte scalars are little endian and packed without native alignment.

| Application offset | Field |
|---|---|
| 0 | U16 outer operand count5 |
| 2 | U32 baseline opcode68A75F0C or delta opcode12862153 |
| 6 | U64 object ID |
| 14 | U32 raw object-tag bits |
| 18 | U8 raw view |
| 19 | U32 payload size |
| 23 | U16 raw declared variable/update count |
| 25 | Opaque schema-field bytes |

PayloadSize is exactly2+opaqueLength, including inner count and excluding all23
outer bytes. Minimum application message is25. Payload budget also includes the
count. Tags remain numeric U32 bits: no display spelling or byte reversal. All
views0..255, IDs including0/high bits, declared counts0..65535 and arbitrary
opaque byte values are preserved.

The [schema map](REPLICATION_SCHEMA_MAP.md) documents baseline snapshot decoder
`0118B740` consuming the count but iterating registered schema, and delta decoder
`0118AA00` consuming count but reading until bounded payload exhaustion.
[Common view6](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_COMMON_VIEW6_OPAQUE_FIELD.md)
records characterized surplus baseline tails. Therefore this envelope never
uses the declared count to parse/validate opaque fields or infer object readiness.
Empty opaque bytes with count65535 are accepted here. A one-byte opaque delta
tail is also accepted **as an envelope**, while schema-level delta parsing would
reject a partial U16 index. This distinction is deliberate, not truncation
permission for a future schema decoder.

## Ownership, API and modern rejection

`ReplicationEnvelope(kind,id,rawTag,view,declaredCount,opaqueSpan,maximumPayloadBytes)`
validates budget/kind and copies bytes before returning an immutable owned object.
Properties contain scalar snapshots; CopyOpaqueFields returns fresh defensive
copies. No writable memory/source reference escapes. EncodeBaseline/EncodeDelta
return fresh owned application byte arrays. DecodeBaseline/DecodeDelta validate
boundary first and return owned DTOs. There is no shared mutable state/resource.

An explicit budget is required on construction, encode and decode. Valid range
is2 through int.MaxValue-23 so enclosing managed-array length is representable.
Invalid budget/kind throws ArgumentOutOfRangeException; null encode DTO throws
ArgumentNullException. Invalid message/opcode/count/size, wrong encode kind or
payload over budget throws InvalidDataException. Declared size must be at least2,
within budget and exactly message.Length-23. Every truncated prefix and every
trailing byte is rejected, without a partially returned DTO. Decode compares by
subtraction before copying; encode computes total length with checked arithmetic.
These strict length/budget policies are **modern hardening**, not claims about
all original exception recovery. [Malformed decode checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_REPLICATION_DECODE_FAILURE_BOUNDARY.md)
records bounds throws and absence of local recovery on the original path.

## Verification and next boundary

Hand-authored synthetic vectors cover baseline high-bit ID/tag, view255,
count65535/four arbitrary field bytes, and delta high-bit ID/raw tag4352454F,
view3/count0/one opaque byte. Independent Python struct oracle verifies actual
native encoded files and decoded scalar/owned-byte JSON, source framed offsets,
all prefixes, wrong size/header/trailing/budget and raw count/view preservation.
Native tests add constructor/output/source mutation ownership, kind mismatch,
config arithmetic bounds and exact budget acceptance/rejection.
[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R6_REPLICATION_ENVELOPES.md)
records scope and commands. All fixture data are authored synthetic bytes.

Lead runner registration suggestion: `ReplicationEnvelopeScenarios.Run()`.
Next milestone may add bounded schema-index/value validation; it is not started.
No opaque delta validity or replication readiness is claimed by this codec.
