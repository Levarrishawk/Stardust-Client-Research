# CREO6 narrow HAM codec and state

## Result and API

[CreatureHamView6Codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureHamView6Codec.cs) decodes/encodes a nonempty ordered CREO view6 delta containing only fields13 and14. `Decode` accepts application bytes or an owned envelope; `Encode` and `EncodeEnvelope` preserve raw declared-count metadata, repeated field order, counters, command extent and signed/full-bit values. Sealed command/update/plan models freeze their collections.

[CreatureHamView6State](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureHamView6State.cs) exposes `FromBaseline`, `Reseed` and stateless immutable `Apply`. It owns only HAM and maximum-HAM vectors/counters, ObjectId and branch revision. The accepted [complete CREO6 baseline](CREATURE_VIEW6_CODEC_CONTRACT.md) remains separate and retains every other field and baseline metadata unchanged. No existing codec, world, registry or shared runner is modified.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R33_CREO6_HAM.md) records reproduction. This is an engine-neutral raw-vector kernel with modern bounded atomic publication and mutation descriptions; it executes no callbacks or weak-target/world operations.

## Direct specialization and caller evidence

Pinned SHA256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [static verifier](../../SWG-Godot-Client/poc/round33-tests/Networking/static_audit.py) checks identity before PE imports/VAs,55 focused HAM instruction anchors plus the accepted109-anchor inheritance audit, exact command jump table and RTTI/vtable identity.

Creature constructor builds fields at+404 and+43C with the same `Archive::AutoDeltaVector<int,CreatureObject>` vtable015DD07C (`0042EDDB/0042EE2E`), then registers them at `0042F9B7..0042F9CE` as indices13/14. Its snapshot and delta slots are0043B050/0043B1A0. This is exactly the specialization already closed in the [recovered integer-vector policy](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md), not an analogy to U64 defenders or a different wounds implementation. Round33 separately verifies the two view6 registrations, parser mutation/helper branches and concrete callback installation.

The common indexed dispatcher0118AA00 consumes U16 metadata, then U16 field indices until stream exhaustion; the metadata is not a loop bound. Snapshot0043B050 reads U32 count/counter then count S32 values. There is no version branch. Existing [prefix evidence](CREATURE_VIEW6_PREFIX_CONTRACT.md) establishes its CREO6 routing.

## Consume and mutation rules

All multibyte values are little endian. Each update is U16 field13/14, U32 raw loop extent N, U32 incoming counter C, followed by commands until the raw cursor reaches or exceeds N:

| Opcode | Body | Cursor advance |
|---:|---|---:|
| 0 | U16 index | 1 |
| 1 | U16 index, S32 value | 1 |
| 2 | U16 index, S32 value | 1 |
| 3 | U16 assignment length k, k S32 values | k+1 |
| 4 | empty | 1 |

An assignment always consumes its whole body, including final extent overshoot. Zero extent has no commands. Unknown commands, unsupported fields, empty whole plans, partial/trailing encodings and impossible encoded command/extent shapes reject under explicit modern strict policy. The original applied unknown-opcode switch falls through byte-only, while its skipped path consumes a different body; that discrepancy is deliberately not exposed as a permissive codec.

For each field, load its own local U32 counter L and compute unsigned `skip=min((N-C+L) modulo 2^32,N)`. Evidence0043B1E8/0043B1EA/0043B1F1 establishes subtraction/addition/unsigned clamp. A command whose starting cursor is below skip is consumed whole without mutation; a skipped assignment can cross skip. Repeated fields recompute against their actual updated local counter. There is no incoming-counter resynchronization at return0043B3B9..0043B3C9.

| Command | Effect | Counter change |
|---|---|---|
| remove | valid index removes; invalid index ignores | +1 only if valid |
| insert | zero-grow to index, then insert, preserving order | +1 |
| replace | equal existing value ignores; otherwise zero-grow through index and replace | +1 if changed/new |
| assign | replace complete sequence, even empty/equal | +(k+1) |
| clear | clear only if nonempty | +1 if nonempty |

Counter arithmetic wraps modulo32 bits. Direct evidence: remove size/branch0043B2B8 and write0043B2DC; insert counter/growth0043B34D/0043B351/0043B367/0043B36E; replacement equality0043B404, increment0043B41D and zero argument/growth0043B436/0043B43D; assignment0043B579 calls0043AD50 with increments0043ADA9/0043AEEE; clear0043B594 calls0043A7F0 with empty branch0043A7FF and increment0043A83D. The same four-byte resize/insert/erase helpers are reached by both fields. AllS32 bit patterns, zeros and duplicates remain intact.

## Original callbacks versus modern descriptions

Original adapters0044C6E0/0044C700/0044C730/0044C760 test nullable aggregate/remove/insert/replace holders. Constructor initially clears all four holders for both fields. It then installs00434A30 in HAM's replacement holder at object+438 (`0042F6FB/0042F72A/0042F738`). The inspected constructor shows no corresponding maximum-HAM replacement installation; absence beyond this constructor/lifetime is not inferred.

00434A30 receives index and old/new S32 pointers, compares new versus old using signed comparison, and when new is no greater than old accumulates old-minus-new into owner members+8B8/+8BC/+8C0 for indices0/3/6. These are direct numeric side effects; their gameplay/presentation meaning is not established here. The narrow state does not reproduce those owner accumulators. Assign/clear helpers and outer dirty-field commit have their separately conditional callback paths; this contract does not equate their notifications with the receipts below.

Modern `CreatureHamView6Event` is one ordered description per command that actually mutates: field, opcode, nullable index, nullable old/new S32. Remove carries old; insert new; replace old/new. Assign/clear carry null index/values and their resulting full vectors are in `Current`. Skipped/equal/invalid/empty-clear commands emit none; empty/equal assignment emits one. This deliberate description policy is not recovered legacy callback delivery.

## Atomicity, revisions and limits

Apply validates/measures the whole immutable plan before candidate copying, then privately applies both vectors and returns `Previous`, `Current`, delta declared count, events and `WorkUsed`. Failure at any later field/cap leaves the previous state and earlier separately published results unchanged. Successful valid plans, including skipped/no-op plans, advance checked U64 revision. Revision overflow rejects. Revisions identify one immutable branch, not global current-incarnation identity; independent calls may branch across threads. Reseed checks ObjectId, restores both baseline vectors/counters and advances revision.

Optional operation limits replace retained policy, may tighten/widen it, and become the returned candidate's policy; the previous state retains its original policy. Baseline seeding applies state/vector/work limits to this narrow projection; the complete baseline decoder owns its separate message bounds.

Codec limits bound payload bytes, field count, actual commands, aggregate decoded S32 values and logical decoded field bytes. Payload includes two-byte metadata, excludes outer23-byte header; decoded bytes here are the opaque field representation. Defaults16384/256/4096/65536/1048576; hard ceilings8MiB/4096/65536/1048576/16MiB. A complete preflight scan precedes decoded command/value allocations; encode measures before its exact field stream allocation. The bounded envelope itself owns its payload copy.

State limits cover each vector, combined elements, logical state/output bytes, event count and work, including intermediate growth before later shrink. Defaults64/128/1MiB/4096/1MiB/65536; hard ceilings65536/131072/16MiB/262144/32MiB/16777216. State bytes are `16+4*(HAM count+maxHAM count)`; output adds24 per event. Removal/clear prospective-event checks conservatively retain their pre-mutation vector counts.

Work begins with old combined element count plus fields, actual commands and all decoded message values, including skipped values. Applied remove charges shifted suffix count; insert charges growth plus shifted old elements; changed replace charges growth plus one; assign charges old plus new count; nonempty clear charges old count. Final publication charges final combined elements plus events. These are explicit logical policy units, not heap/CPU measurements. Defensive collection copies have additional bounded linear overhead; element/message hard ceilings bound that overhead independently.

## Validation and resume boundary

[Independent Python peer](../../SWG-Godot-Client/poc/round33-tests/Networking/peer.py) hand-packs S32 commands and independently computes both counters/vectors, ordered modern events, work and peak capacities. Its complete baseline wire constructor comes from the accepted32 independent peer. [Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureHamView6Scenarios.cs) compare exact plans/wires/states/events and exercise exact/one-less codec/state caps, aliases, reseeding, revisions, mismatched recipients, partial bodies and later-failure preservation.

Passed19 exact cases,1535 negatives,359 budget checks and114 ownership checks; independent oracle, static identity rejection, accepted32 complete CREO6 codec regression and shared Core smoke suite pass. Builds have zero warnings/errors. Fixtures are synthetic; no original execution/capture or proprietary fixture promotion occurred.

Current server delta-vector code remains correlation only. No additional server-only semantic label is required for these direct parser rules. Next bounded probe: compose this narrow union with supported CREO6 scalar/string/pair plans under one current-incarnation prepared publication, then investigate required owner side effects separately. Wearable deltas, defender composition, world routing and live interoperability remain outside this milestone.
