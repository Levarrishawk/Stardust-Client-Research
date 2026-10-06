# Full ObjectController transform codec

Round64 implements the full application frame in `Stardust.Core.Protocol`, independently of the [compact observer codec](COMPACT_SCENE_TRANSFORM_CONTRACT.md). It performs no SOE framing, lookup, physics, quaternion normalization, interpolation, or world mutation.

`ObjectControllerTransformCodec.Decode(ReadOnlySpan<byte>, ObjectControllerTransformDirection)` requires an explicit validated direction. `Encode(ObjectControllerTransformMessage)` returns a fresh owned array. The sealed immutable record has raw integer fields and individual float components; `ClientToServer` and `ServerToClient` factories accept a typed timestamp/scalar respectively. `Timestamp`, `ServerScalar`, `WithTimestamp`, and `WithServerScalar` reject the wrong direction. The raw `OuterWord` preserves the four bytes without forcing a symmetric interpretation. Vector arguments to factories use ordinary component order; serialization writes position X/Z/Y.

| Offset | Width | Field |
| --- | --- | --- |
| 0 | U16LE | Operand count 5 |
| 2 | U32LE | Hash 0x80CE5E46 |
| 6 | U32LE | Flags, preserved without invented bit policy |
| 10 | U32LE | Inner type 0x71 root or 0xF1 parent |
| 14 | U64LE | Object ID |
| 22 | 4 bytes | Direction-specific outer word |
| 26 | U32LE | Movement counter |
| 30 | U64LE for parent only | Parent ID, including zero |
| 30 root / 38 parent | 16 bytes | F32LE quaternion X/Y/Z/W |
| 46 root / 54 parent | 12 bytes | F32LE position X/Z/Y |
| 58 root / 66 parent | F32LE | Signed speed |

Exact complete lengths are 62 and 70 bytes. Decode rejects truncation, trailing bytes, wrong operand/hash/type, and invalid direction. Encode rejects invalid enum values and kind/nullable-parent mismatch. Integer fields admit their entire unsigned ranges.

## Evidence and direction semantics

Observation: current server `MMOCoreORB/src/server/zone/packets/object/transform/Transform.h:15` declares `uint32 timeStamp`; `parseDataTransform` at lines48-56 and `parseDataTransformWithParent` at58-67 consume `parseInt()` before movement counter. Interpretation: the client-to-current-server outer word is a U32 timestamp. Confidence: Confirmed for the inspected server boundary. Consequence: all 32-bit timestamp patterns are admitted, including patterns that would represent float NaN/infinity. No timestamp unit, arithmetic, or wrapping policy is implemented by this codec.

Observation: `ObjectControllerMessage.h:16-21` emits count/hash/flags/type/object ID followed by `insertInt(0x00)`. `DataTransform.h:29-50` and `DataTransformWithParent.h:28-51` emit counter, optional parent, quaternion X/Y/Z/W, position X/Z/Y, speed. Confidence: Confirmed server emission structure. Zero is the observed server scalar; an admitted finite nonzero inbound scalar is retained without assigning gameplay semantics.

Client correlation: [full envelope and dispatch](OBJECT_CONTROLLER_PROTOCOL.md) and [active schemas](ACTIVE_OBJECT_CONTROLLER_SCHEMAS.md), plus [root transform analysis](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_DATA_TRANSFORM.md) and [parent-relative analysis](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_PARENT_RELATIVE_MOVEMENT.md), identify inbound unpackers 00AD4860 and00AD43B0 starting at the movement counter after the outer word. The client outer AutoVariable<float> storage observation does not establish a float timestamp contract at the current server parser. The direction distinction resolves that evidence discrepancy. Existing client evidence is correlation, not a new executable reanalysis.

Finite-only inbound scalar and quaternion/position/speed admission is an explicit modern policy. Legacy root parsing includes NaN quaternion repair; this implementation does not reproduce that repair. All admitted F32 bits, including signed zero, subnormals and extrema, survive encode/decode. A zero or non-unit finite quaternion and negative speed remain raw values. Unknown inbound scalar semantics are Unknown; bit retention is Confirmed by fixtures.

## Reproduction and limits

Run the independent synthetic Python byte oracle and native scenarios with an installed .NET8 SDK and the existing offline package cache:

```powershell
python poc/round64-tests/Networking/replay.py --output-dir local-private/round64-replay
```

The output directory must be fresh and guarded under the ignored private root. Python constructs little-endian frames with `struct.pack` before invoking native code; native decoding checks individual fields and reencoding against that independent oracle. All fixtures are synthetic, not captured traffic. The runner covers both directions and types, asymmetric components, integer extremes, signed zero, subnormals, finite extremes, zero quaternions, negative speed, every truncation, extra bytes, bad headers/types/directions/shapes, and NaN/infinities at each float slot on encode and decode. Buffer ownership and direction-specific factories/accessors are checked.

Verification: 32 positive cases and4,000 malformed checks passed; build had zero warnings/errors. No original executable execution, network contact, or gameplay behavior was tested. This codec is ready for bounded fixture-backed integration; caller selection of direction and actual transform state application remain separate work.
