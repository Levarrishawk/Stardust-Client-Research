# Current creature movement raw input view

Round49, 2026-10-04. SceneWorldCreatureMovementCurrentInputs (external historical reference) derives an immutable ten-slot inspection view from explicit concrete-constructor bootstrap policy and actual received presence. A present received word replaces the bootstrap observation even when zero, signed zero, infinity or NaN. It does not seed the received-only store, mutate the world, or apply movement.

This is a modern raw-input view informed by [constructor evidence](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md) and [direct current-slot writers](CREATURE_MOVEMENT_WRITER_CONTRACT.md). It does not reproduce complete original arbitration, related-object substitutions, modifiers, runtime sampling or final movement. In particular, authored slope angle remains a **raw authored input**: the original constructor's angle conversion is not performed here. Received angle words remain exact received words. Numeric eligibility does not establish identical units between these origins.

## Explicit binding and trust

Construction requires one current object, an immutable [authored snapshot](SHARED_CREATURE_AUTHORED_MOVEMENT_INPUT_CONTRACT.md), an independent64-hex SHA-256 pin, `CallerVerifiedConcreteClientCreatureRawInputs` scope and explicit `callerCatalogTrusted=true`. Scope value0 is Invalid and rejected. The normalized authored root must equal its stored path, its source hash must match the independent pin, and its legacy path CRC must match the current object's create CRC.

The caller asserts that its catalog entry is a concrete creature using the documented constructor path. The private world catalog is not exposed, reflected into, or inferred from received field labels. CRC matching alone does not prove a resource recipe or original runtime construction. This explicit caller trust is part of the modern boundary. The consumer binds to the world's generation and the object's incarnation at construction; it cannot silently switch to a recreated object with the same ID.

The partial received-only world is required. The consumer owns a frozen [received reader](SCENE_WORLD_RECEIVED_CREATURE_MOVEMENT_INPUT_CONTRACT.md), borrows the world and immutable authored snapshot, and takes no callback. All public operations enforce the constructing thread and reject reentry. World owner/open checks use public frozen APIs.

## Fixed fields and provenance

| Field | Bootstrap observation under the explicit scope |
| --- | --- |
| 0,1,4,5,10 | Literal F32 one, raw bits `3F800000` |
| 7 | Authored Speed0 |
| 11 | Authored Speed1 |
| 8 | Authored raw SlopeAngle |
| 9 | Authored SlopePercent |
| 12 | Authored WaterPercent |

Each field exposes index, nullable raw U32 word, Origin, Eligibility, nullable exact authored input, and nullable received field. Origin is Unknown, ConstructorLiteralOne, ConstructorAuthoredRaw or Received. Absent/unsupported authored inputs stay Unknown without a guessed getter-zero result, RNG sample, relative evaluation or fallback past a nearer unresolved record.

The exact authored input retains its encoded evidence, selected field and source provenance even when received presence supersedes it. The five literal-one fields expose their bootstrap bits and direct constructor store VAs (`0042F14C`, `0042F122`, `0042F093`, `0042F071`, `0042F176`, respectively) even after override. The receipt records the constructor executable identity `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. These identify static evidence, not execution of that constructor for the current object.

Received fields preserve raw bits and exact partial-state provenance (state revision, declared count, update ordinal and origin). Missing received fields permit bootstrap observation; present nonfinite words remain Received and never fall back. A listen-only partial publication creates a received receipt without overriding any movement slot. Collections2/3/13 and listen6 are excluded; `Get` rejects them.

## Context and currency

`Read(generation)` returns null for foreign/stale generation, destroyed/replaced incarnation, world reset/close, or a phase excluded by the inspection policy. PendingOrFinalized is the default inspection phase; FinalizedOnly suppresses pending receipts. Pending inspection can exist before typed CREO3 publication and before any partial CREO4 publication.

A receipt captures exact registry object, world object context, generation/incarnation/phase, nullable typed CREO3 state, authored binding/pin, and nullable received receipt/source. It is historical immutable data. `HasCurrentTypedState` reports captured current typed CREO3 presence; `IsOperationalContext` requires finalized phase and that typed presence. Field Eligibility is Unknown, NonFinite, PendingPhase, MissingTypedState or FiniteCurrent. FiniteCurrent is raw finite-input eligibility under this context; it is not a nonnegative speed guard or permission to drive motion. Negative finite values remain inspectable.

`IsCurrent` requires the issuing consumer, bound generation/incarnation, exact registry object, compatible typed-state presence and current captured typed source. For a captured received receipt it also requires exact frozen received-reader/source currency. If the receipt captured **no partial publication**, any later first partial publication invalidates it, including listen-only updates. Late first typed CREO3 publication similarly invalidates a receipt that captured typed absence. Finalization/containment changes, typed-state replacement, received revision changes, destroy/recreate, reset and close invalidate relevant contexts. Unrelated objects do not supply values to this view.

Historical fields and bootstrap evidence remain immutable after invalidation. Dispose is owner-thread, idempotent, closes only the owned received reader and clears borrowed references. It does not close the world or erase caller-owned receipts. Read after disposal throws; IsCurrent returns false for a valid historical receipt.

## Reservation and failure policy

Logical fixed charge is1632:128 header + ten fields at96 +544 for the possible frozen received-reader output. It is charged even when no partial publication exists. MaximumSlots must permit all ten slots; MaximumOutputBytes must permit the full fixed charge. Legal configuration ranges are0..10 and0..65536, respectively; inadequate capacity rejects. Reservation occurs before construction-time borrowed snapshots/received-reader creation and before read-time snapshots or field/receipt allocation. This is deterministic logical accounting, not an exact managed heap measurement. World snapshot scans remain bounded by the frozen world's object cap; authored evidence is borrowed immutably rather than recopied.

Invalid scope/trust, path/CRC/SHA mismatch, unsupported world or argument configuration rejects without publishing a view. Reads recheck exact context before publication. There are no callbacks, driver calls, state writes, received seeds, default packets, modifier arithmetic or catalog reflection in this new implementation.

## Verification and next boundary

Native scenarios (external historical reference) passed1120 cases and13339 checks. Independent peer (external historical reference) passed105280 comparisons across20 synthetic authored profiles, all ten raw fields, partial/all/missing received data, duplicate updates, signed zero/subnormals/negative/max/nonfinite words, provenance, constructor evidence and four typed/phase contexts. Lifecycle/API negatives cover scope/trust/SHA/CRC, exact and short reservations, missing/late typed and partial publication, foreign consumer/world/generation, owner affinity, immutable fields, containment, destroy/recreate, reset, disposal and world close.

Frozen regressions passed: Round47 received reader128 cases/16947 checks with1408 actions/10240 slot observations; Round48 explicit movement840 cases/9154 checks/2988 independent comparisons; default64-bit Core smoke. Own and frozen builds passed with zero warnings/errors. Fresh static prerequisite passed109 writer/reader anchors and its frozen constructor/registration/delivery/server gates. No original executable was run and no new original asset occurrence or corpus survey was needed.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R49_MOVEMENT_CURRENT_INPUTS.md) records scope and corrections. Next bounded implementation probe: define an explicit selection policy over an exact-current derived receipt without conflating raw authored slope angle with received runtime angle, and without adding legacy modifier/vehicle arbitration. Original indirect writers and final controller semantics remain separate research questions.
