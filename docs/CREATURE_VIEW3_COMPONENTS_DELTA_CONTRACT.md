# CREO view 3 components set contract

This standalone codec supports only CREO:3 field5, the inherited tangible visible-components `AutoDeltaSet<int>` at object +0x328. It preserves raw snapshots separately from signed ordered unique state, and applies complete delta plans atomically. Other field indices reject, including scalar/wounds fields. There is no mixed/world/store integration or live interoperability claim.

## Public API

All types are in `Stardust.Core.Protocol`; the source is [CreatureView3ComponentsDeltaCodec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3ComponentsDeltaCodec.cs).

- `CreatureView3ComponentsDeltaLimits(int MaximumPayloadBytes, int MaximumFields, int MaximumCommands, int MaximumKeys, int MaximumElements)` supplies all delta budgets.
- `CreatureView3ComponentsSnapshot.FromBaseline(CreatureView3BaselineSnapshot baseline, int maximumRawKeys)` returns an owned raw snapshot of the accepted full baseline's Components field.
- `CreatureView3ComponentsDeltaCodec.DecodeSnapshot(ulong objectId, ReadOnlySpan<byte> fieldPayload, int maximumRawKeys)` returns `CreatureView3ComponentsSnapshot`; input is only the field body, not a new baseline application envelope.
- `EncodeSnapshot(CreatureView3ComponentsSnapshot snapshot, int maximumRawKeys)` returns raw-preserving `byte[]` field bytes.
- `snapshot.ToState(int maximumElements)` returns normalized `CreatureView3ComponentsState`.
- `new CreatureView3ComponentsState(ulong objectId, uint counter, ReadOnlySpan<int> inputKeys, int maximumElements)` normalizes an explicit seed.
- `CreatureView3ComponentsDeltaCodec.Decode(ReadOnlySpan<byte> message, CreatureView3ComponentsDeltaLimits limits)` and `Decode(ReplicationEnvelope envelope, CreatureView3ComponentsDeltaLimits limits)` return immutable `CreatureView3ComponentsDeltaPlan`.
- `Encode(CreatureView3ComponentsDeltaPlan plan, CreatureView3ComponentsDeltaLimits limits)` returns `byte[]`; `plan.Apply(CreatureView3ComponentsState initial)` returns a new normalized state only after complete success.

Snapshot properties: ObjectId, Counter, RawCount and read-only RawKeys. State properties: ObjectId, Counter, signed ascending read-only Keys, and `Contains(int key)`. Plan properties: ObjectId, DeclaredCount, Updates and retained MaximumElements. Each `CreatureComponentsUpdate` exposes Counter, CommandCount and read-only Commands. Commands are immutable `CreatureComponentsCommand(CreatureComponentsCommandKind Kind, int Key)` records with Drop0/Add1/Clear2; Clear has no wire key (its model Key is zero). Plans/updates/raw snapshots have no public mutable constructors.

Future callers should retain the raw snapshot for evidence/re-encoding, normalize explicitly, and publish the returned state once inside an incarnation-checked transaction. This codec checks object ID but carries no registry/store/generation token.

## Wire, baseline and ordering

The existing [CREO baseline](CREATURE_VIEW3_BASELINE_CONTRACT.md) stores field5 as `U32 rawCount | U32 rawCounter | S32[rawCount]`. Snapshot parsing consumes exactly that body. Raw order and duplicates remain unchanged in the owned snapshot; RawCount can exceed normalized member count. ToState deduplicates and sorts by signed S32 value, retaining the snapshot counter unchanged. This closes the previously Unknown original normalization boundary without rewriting accepted raw-baseline behavior.

Direct evidence: snapshot `00642F00-00643042` clears the old tree, reads the counter directly to +0x18 (`00642F52`), reads all declared keys and performs signed tree comparisons (`SETL` at `00642F94`, `JGE` at `00642FEA`). Equal keys reach the noninsert branch at `00643005`. Insertion helper `00645CE0` attaches lower/upper nodes and updates size; delta insertion helper `00645560` returns inserted flag1 or duplicate flag0. The original serializer `00642B80-00642BDD` starts at the leftmost node (`00642BAF`) and advances through in-order iterator `012F6420`. Thus signed ascending unique runtime/serialization order is Confirmed. Our EncodeSnapshot intentionally writes preserved raw entries; it is not a claim that original runtime serialization emits duplicates.

Delta application framing is the accepted [23-byte application envelope](REPLICATION_ENVELOPE_CODEC_CONTRACT.md): operand count5, opcode `0x12862153`, U64 object ID, numeric tag `0x4352454F`, view3, exact U32 payload size and raw U16 declared field count. Counts do not drive outer field iteration. The body is repeated `U16 index=5 | U32 commandCount | U32 incomingCounter | commands`; repeated fields retain wire order. All multibyte values are little endian. Commands are U8 drop0 plus S32 key, add1 plus S32 key, clear2 with no body. Exactly commandCount records precede the next field/end. Signed extremes/high bits and duplicate commands are retained in plans and exact encoding.

## Exact recovered set kernel

Pinned executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Decoder `00643050-006432F2` reads N command count and C incoming counter; L is local counter at +0x18.

1. If unsigned `(L + N) mod 2^32 <= C`, skip0 (`00643081` LEA; unsigned JBE `00643086` to zero path `00643134`).
2. Otherwise skip unsigned `min((L - C + N) mod 2^32, N)` (`0064308C-00643094`).
3. Consume that prefix without mutation; every nonclear record still consumes its S32 key (`006430B3-006430C6`). Apply the suffix in received order.
4. Drop mutates only when a key exists (`00643259-00643260`); Add mutates only when unique insertion succeeds (`006431D5-006431DF`); Clear mutates only when nonempty (`00643100-00643105`). Each effective operation increments L once modulo U32 at `006432A2`, `00643225` or `00643152` respectively.
5. Finish each field with unsigned `L = max(L, C)` (`006432E2-006432E7`), even for zero-command/no-op packets. Repeated fields use that actual final counter.

This initial sum gate is distinct from the vector's unconditional skip-clamp, and the final maximum is present here but absent from the recovered vector. Future gaps can execute operations and then raise the counter. Wholly stale packets commonly skip all and preserve L. Wraparound can cause apparently old high local counters to execute a suffix; mutation wrap followed by unsigned maximum may lower the counter from its pre-field value. No serial-number-order interpretation is invented. Native/oracle cases discriminate these branches and no-op increments at wrap boundaries.

Tree insertion/lower/upper-bound helpers use signed comparisons (`00645583`, `0064562D`, `00644E24`, `00644E54`) and enforce uniqueness; erase returns membership count. Clear destroys the tree. Nullable aggregate/add callback adapters `00645200/00645220` dispatch only when installed; inline removal callbacks and clear iteration can run before original outer completion. Their downstream effects and original allocation/outer exception recovery are outside this kernel and are not reproduced.

The existing [width/registration checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_TANGIBLE_COMPONENT_WIDTH_CORRECTION.md) maps this set to field5 and distinguishes the separate cached-network-ID vector. Common caller `0118AA00` reads fields until exhaustion and calls the registered delta virtual; checked reads reach the exception path in `0118B030`. No version branch appears in the set decoder. Server `AutoDeltaSet<int>::insertToMessage` uses HashSet iteration with unspecified order; that correlation does not override the client's signed tree order.

## Modern bounds and verification

Default behavior is the proven set kernel with explicit modern bounds, immutable ownership and whole-plan atomic publication. It does not invent a strict counter policy. Payload, fields, aggregate commands, aggregate decoded keys and transient unique elements are capped; raw snapshots have an independent raw-key cap. Duplicate snapshot entries still consume raw-key budget. Skipped delta operations still consume command/key budgets. Successful duplicate Add consumes no new element capacity. Counts/remaining bytes are checked before allocation; size arithmetic uses long before conversion. Limits are logical counts/bytes, not total CLR heap overhead.

Unknown commands and unsupported mixed fields reject. Original active unknown commands fall through without a key read, whereas stale unknown nonclear commands consume four bytes; this inconsistency is documented rather than accepted by the modern codec. Bounds errors and late field failures leave the initial state unchanged. Original in-place mutation/callback side effects are intentionally replaced by atomic immutable output. There are no retained native handles, threads, sockets, callbacks or external cleanup obligations.

The static exports contain 2,484 bounded bytes: snapshot/delta998, necessary helpers1,020, caller/read/bounds context372 and serializer94. Full original-derived output remains private. Independent Python verifies the executable pin and27 instruction anchors, hand-packs signed-key/raw-duplicate packets and models the unsigned arithmetic/membership rules separately from C#. Sixty-nine native cases agree on raw/normalized baseline, exact encoded messages, counters and members, including49 adversarial high/low/wrap pairs. Native tests additionally cover empty snapshots/envelopes, repeated-field rollback, all command/no-op forms, exact/over-budget limits, truncation, unknown fields/commands, ownership and both decode overloads.

Lead correction: replace generic 'same stale-prefix pattern as vector' phrasing for this specialization with the conditional unsigned sum gate; retain final unsigned maximum specifically for this set. Original component baseline duplicate normalization is now Confirmed; preserve raw baseline snapshots separately. No new names, counts, shared schema/status edits or overall completion metric changes.

Next probe: compose field5 with stable-incarnation state and mixed plans under one bounded transaction, then trace concrete installed component callbacks only if presentation requires them. No mixed/world integration this round.
