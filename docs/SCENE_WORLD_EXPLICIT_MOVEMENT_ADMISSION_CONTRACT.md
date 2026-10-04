# Explicit modern world movement admission

`SceneWorldExplicitMovementAdmission` connects one explicitly chosen authored or
received speed to an owned actual `SceneWorldLocalMotionDriver`. This is a modern
planar admission policy, not recovered original speed arbitration, controller
semantics, coordinate conversion, slope/water scaling, or angle interpretation.

## Inputs and binding

The required `SceneWorldMovementSpeedSource` has invalid zero and exactly four
operational values: `AuthoredSpeed0`, `AuthoredSpeed1`, `ReceivedField7`, and
`ReceivedField11`. A source is fixed for the adapter lifetime. No absent,
unsupported, nonfinite, negative, or stale selected value falls back to another
slot or source. Unselected values do not affect selected-speed eligibility.

Construction borrows the owner-thread world, motion store, immutable authored
root evidence, and optional received reader. It requires an active world, a
finalized selected creation incarnation, current typed CREO3, and the normalized
root path CRC equal to that object's create template CRC. The caller supplies an
independent expected root SHA-256. The authored evidence must contain the same
normalized root and SHA; its frozen profile loader already verifies bytes against
the pin. The root binding is required for received sources too, without selecting
an authored speed.

**Caller trust:** the caller must supply the root/pin belonging to its selected
catalog recipe. The coordinator's catalog is private. This adapter proves the
supplied evidence/pin and create path CRC agreement; it does not independently
prove the world's private catalog SHA or recipe pin. CRC agreement alone is not
SHA-256 identity. No reflection or frozen coordinator changes establish binding.

The adapter constructs and owns a new sealed local driver after those checks.
It never pairs a supplied old driver by ObjectId, so a recreated object cannot
accidentally admit into an old creation's timeline. Successful construction
intentionally publishes the driver's initial local pose. It does not submit a
movement input or load resources.

## Admission and captured receipts

`Submit(timestampMicroseconds, normalizedNativeXZ, receivedReceipt)` accepts a
supplied native XZ unit direction. Its finite component squared-length tolerance
is 0.000001. It does not normalize or convert coordinates. Zero direction is
ineligible even when speed is zero. Finite nonnegative signed zero is eligible;
its exact speed bits survive in the receipt.

An authored choice uses the frozen reader's explicit one-slot nonnegative literal
selection. A received choice requires an explicit receipt owned by the supplied
reader, current in that same world, generation and exact creation incarnation,
with identical registry context, finalized phase, and present finite selected
field 7 or 11. The field's raw word and provenance remain available. A received
receipt supplied to an authored-only choice throws; no received receipt for a
received choice returns null.

Velocity is binary32 `(direction.X * speed, 0, direction.Y * speed)`. Each finite
component must have absolute value at most 10. This is the existing driver bound,
not a scalar speed cap: speed 12 with direction (0.6, 0.8) is admitted, whereas
speed 12 along one axis is rejected. Entry permission is null. No slope, water,
acceleration, angle, inferred default, or received multiplier is applied.

All eligibility and currency checks precede exactly one driver `Submit` call.
Ineligibility returns null without a published receipt or input prefix. Existing
driver time/order/capacity exceptions propagate before timeline mutation: time
must be nonlate, ordered against prior submissions, within its 60,000,000 us
horizon, and within 1,024 pending transitions. Candidates are local until driver
admission succeeds; the adapter retains no growing receipt/history collection.
Neither rejected admission nor receipt inspection publishes a local pose.

A successful immutable receipt records owner, source choice, exact target and
CREO3 snapshot, generation/incarnation, timestamp, direction, raw speed bits,
velocity, root binding/SHA, and either authored selection evidence or received
source receipt. `IsSourceCurrent` checks source currency, not whether an input is
still queued. CREO3 wrapper references are recreated by inspection; currency uses
the world's public `IsCurrent`, not wrapper reference identity.

**Captured velocity policy:** a later CREO4 publication may make the source receipt
noncurrent but does not alter an already admitted input. The owned actual driver
runs the captured velocity at its established tick-start schedule. A fresh source
requires a fresh explicit submission. Old receipts remain historical evidence.

## Lifecycle and limits

All operations are serialized on the construction thread and reject reentry.
No callbacks run inside the adapter. Admission rechecks active world/current
finalized typed CREO3/exact incarnation before entering the driver, so destroy and
recreate, generation reset, and close return null without latching the driver's
stale-incarnation fault during admission. `Advance` and `Reset` explicitly delegate
the established driver lifecycle, tick, suspension, fault and recovery behavior.
Reset discards its queue; it does not make an old source receipt current.

Disposal is idempotent on the owner thread. It disposes the owned driver and clears
borrowed references; it never closes the world, disposes the borrowed reader or
clears borrowed motion rows. Historical receipts are immutable independent
references retained by their callers; they may still hold source evidence.

## Evidence, validation and compatibility consequences

Observation: frozen authored selection, received currency, world typed-state
currency and actual local-driver admission are composed directly in the
[adapter](../poc/Stardust.Core/Session/SceneWorldExplicitMovementAdmission.cs).
[Native scenarios](../poc/Stardust.Core.SmokeTests/SceneWorldExplicitMovementAdmissionScenarios.cs)
execute actual ticks over synthetic pinned SCOT roots and literal received wires.
The [independent peer](../poc/round48-tests/Runtime/peer.py) authors fixture bytes and
compares binary32 planar integration without using native receipt outputs as
expectations. The [fixture](../poc/round48-tests/Runtime/ordered-fixtures.json) is
explicitly synthetic and contains no original assets or private source data.

Confirmed implementation behavior: 840 cases, 9,154 native checks and 2,988
independent comparisons. Cases include both authored speeds, both received
fields, missing/short/unsupported authored values, negative values, signed zero,
received NaN/infinity/absence, invalid directions and component edges. Additional
checks exercise stale publications, captured future inputs, exact field choice,
foreign worlds/readers/stores, root identity and CRC mismatch, owner/reentry,
queue capacity, time/order, reset, recreation/generation/close, and disposal.
Frozen round18 local motion, round39 terrain motion, round47 received inspection
and default core smoke pass without changing their source.

Interpretation: this closes a bounded explicit modern admission slice. It does
not prove original movement field semantics, source arbitration, or gameplay
motion fidelity. Preserve wire/raw source evidence and the existing local-driver
schedule; defer original scaling and controller integration until separately
specified. The earlier [explicit authored/received join](EXPLICIT_AUTHORED_RECEIVED_MOVEMENT_JOIN_CONTRACT.md)
remains a separate boundary. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R48_EXPLICIT_MOVEMENT_ADMISSION.md)
for reproduction and the exact next probe.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
