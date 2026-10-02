# CREO view3 scalar delta plan contract

R7-NET-01, October2,2026. Exactly indices11..16. No baseline/container decoding,
registry binding, resources, engine presentation, scene readiness or callbacks.

## Evidence and confidence

[Replication schema](REPLICATION_SCHEMA_MAP.md), complete CREO:3 ordering,
records original constructor/RTTI registrations and fixed widths below.
Existing delta decoder `0118AA00` consumes declared count but iterates until
bounded payload exhaustion. [Malformed boundary](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_REPLICATION_DECODE_FAILURE_BOUNDARY.md)
records original checked-read failure and lack of local recovery.
The baseline executable SHA256 was rechecked:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
No new labels or executable behavior recovered.

Static sibling Stardust-3:
`MMOCoreORB/src/server/zone/packets/creature/CreatureObjectDeltaMessage3.h`,
constructor, selects raw tag4352454F/view3. Its updatePosture, updateFactionRank,
updateCreatureLinkID, updateHeight, updateShockWounds and updateStatesBitmask
methods independently emit indices0B..10 with matching scalar widths.
`DeltaMessage.h` addByte/Long/Float/IntUpdate writes U16 index then scalar.
These widths/registration order and count tolerance are **Confirmed** existing
client-backed evidence plus server correlation. No additional original field
semantics or notification behavior is inferred from method names.

| Index | Typed update / wire scalar | Client evidence |
|---|---|---|
| 11 | Posture / U8 | ServerPostureChanged<byte> |
| 12 | Rank / U8 | RankChanged<byte> |
| 13 | LinkId / U64 | AutoDeltaVariable<NetworkId> |
| 14 | Height / F32 | ScaleFactorChanged<float> |
| 15 | Shock / U32 raw bits | integer AutoDelta; server addIntUpdate |
| 16 | States / U64 | StatesChanged<uint64> |

All index/scalar bytes are little endian. Raw tag is numeric4352454F, without
spelling swap; view must be3. [R6 envelope](REPLICATION_ENVELOPE_CODEC_CONTRACT.md)
validates count5/delta opcode12862153 and23-byte application header/minimum25.
No four-byte transport prefix is accepted. Original shock's semantic signedness
is not imposed: state uint preserves every32-bit value, including high bits.
No posture/rank enum restrictions, link-ID ownership or states-bit meanings.

## API and atomic policy

`CreatureView3ScalarDeltaCodec.Decode(message,maximumPayloadBytes,maximumUpdates)`
uses the accepted Delta envelope, then validates tag/view and parses U16
index/value pairs to complete bounded opaque exhaustion. Overload
Decode(ownedEnvelope,maximumUpdates) uses that already budget-checked envelope.
DeclaredCount is retained in the plan and never controls iteration. Empty payload
with count65535 is accepted. Supported updates can repeat or appear in any order.
All received order/duplicates are retained in an owned read-only update list.

The plan has ObjectId, raw DeclaredCount and typed immutable update records.
`Encode(plan,maximumPayloadBytes,maximumUpdates)` reconstructs the exact envelope,
order, duplicates and float bits; it does not recompute the declared count.
No writable/source array escapes. Payload budget includes inner U16 count;
maximumUpdates counts actual decoded updates and may be0 for an empty plan.
Invalid negative update budget/configured envelope budget throws
ArgumentOutOfRangeException. Envelope size/opcode/count checks remain R6 policy.
Unsupported indices (including otherwise real CREO fields0..10/17), wrong
kind/tag/view, budget excess, partial index/value or nonfinite height throws
InvalidDataException. Null inputs throw ArgumentNullException. All plans are
constructed only after complete validation; no partial plan or callback escapes.

`CreatureView3ScalarDeltaPlan.Apply(initialState)` returns a new immutable six-
scalar state, preserving untouched values and processing updates in wire order;
last ordered update wins. Empty plan returns the same immutable initial object.
The input is never mutated. It checks initial height is finite and then processes
already validated updates without notifications. ObjectId is metadata only;
this pure Apply does not bind/verify registry identity or scene generation. A
later binding layer must supply freshness and matching-object checks.

Finite binary32 height—including negative/nonunit values, subnormal/extreme
finite values and negative zero—is not clamped, normalized or repaired. Nonfinite
height rejection is explicit **modern policy**, not original presentation
behavior. All byte and unsigned-ID/state/shock bits remain raw. Typed operation
objects are sealed immutable records; replacing an inspection-list element is
blocked. Returned encoded arrays are fresh copies. No tasks/resources retained.

## Reproduction and limits

Hand-packed synthetic golden includes all six fields, raw bytes255/254, high-bit
object/link/states/shock, height-1.75, then duplicate posture7 and height negative
zero. Count0 and65535 produce identical ordered plans/states while round-tripping
their distinct headers. Native tests cover all outer truncated prefixes, every
partial indexed-value boundary, unsupported index after a valid update, wrong
schema/budgets/nonfinite values, signed zero, empty plan and input/output/list
ownership. A malformed later field never applies earlier fields.
Independent Python reuses the R6 envelope oracle, hand-packs all scalar fields,
checks actual C# encoded count0/65535 files, typed ordered plan and initial/applied
state export. HeightBits is exported so JSON numeric formatting cannot hide -0.
[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R7_CREATURE_SCALAR_DELTAS.md)
records exact commands and scope. Synthetic packet data are not captures/assets.

Lead registration: `CreatureView3ScalarDeltaScenarios.Run()`. Registry binding,
baseline requirements and presentation behavior are deferred; no next milestone
starts in this assignment.
