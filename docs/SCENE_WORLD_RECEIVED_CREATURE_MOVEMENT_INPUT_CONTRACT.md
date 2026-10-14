# Current-world received creature movement inputs

SceneWorldReceivedCreatureMovementInputs (external historical reference) reads ten correlated CREO4 F32 slots from an exact current [partial-world publication](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_CONTRACT.md). It preserves received observations only. It supplies no authored/default values, numeric conversion, modifier composition, angle conversion, motion formula or walk/run selector. Field6 (listen target) is not movement and is excluded.

## Fixed slots and raw classification

The immutable receipt contains exactly ten fields in order0,1,4,5,7,8,9,10,11,12. Each field has Index, nullable U32 RawBits, nullable exact CreatureView4PartialProvenance, Kind, and Eligibility. Missing has null bits and provenance; a received zero has nonnull bits0 and its source provenance. Negative-zero, subnormal, negative finite, infinity and every NaN payload remain received words. No float conversion canonicalizes NaN or changes a sign bit.

Kind is Missing, Finite or NonFinite. Classification uses only the IEEE F32 exponent mask: exponent255 means nonfinite. Eligibility is Missing, NonFinite, PendingPhase or FiniteReceived. A received finite word is FiniteReceived only in Finalized; in PendingBaselines it is PendingPhase. This is a modern finite-input inspection policy, not a speed validity rule or authorization to apply movement. Negative finite words remain finite observations. Selecting fields11/7 as walk/run later requires an explicit separate finite nonnegative policy and missing-state result; no such selector is implemented here.

`Get(index)` returns the exact immutable field from the receipt and rejects listen6, collections2/3/13 and unsupported indices. There is no conversion to a full baseline or merge with [authored movement-input provenance](SHARED_CREATURE_MOVEMENT_INPUT_PROFILE_CONTRACT.md). Updating only listen6 still changes source-publication currency but does not fill any of the ten movement fields.

## API, phase and currency

Construct with a borrowed opt-in partial SceneWorldCoordinator and optional SceneWorldReceivedCreatureMovementPolicy. InspectionPhase defaults to PendingOrFinalized; FinalizedOnly suppresses pending reads. Operational eligibility is always finalized-only, regardless of inspection policy. The constructor validates world owner affinity/open state and rejects opaque/full-only worlds.

`Read(generation,objectId)` finds the current partial source; `Read(generation,sourceSnapshot)` accepts an exact current source from that world. No partial publication, missing object, foreign/stale generation/source, replacement, destruction, reset or world close produces no receipt (null). An eligible over-budget read throws before receipt/slot candidates. No Empty state is created to satisfy a read.

A receipt exposes Source, Object, world Generation, Incarnation, PublicationRevision, StateRevision, captured Phase, IsOperationalPhase, DecodedBytes and Fields. Source is the exact frozen partial-world snapshot; fields keep the exact received source revision/declared count/ordinal/origin, which can differ from current StateRevision. IsOperationalPhase reports only the captured finalized phase: it does not imply any fields are known, finite, usable as speeds or ready for motion.

Consumer `IsCurrent(receipt)` requires the exact consumer owner, borrowed world generation, current exact partial publication and the identical immutable registry Object snapshot. This is deliberately stricter than partial-source currency: finalization or containment changes the registry snapshot and invalidates the consumer's phase-context receipt even when the partial publication remains current. A fresh read captures the new phase. Replacement/destroy/reset/close invalidate both. An unrelated object's change does not by itself change this source identity. Repeated reads of the same source/context can both remain current; reads do not establish a mutable latest-receipt epoch.

Every public operation enforces the construction thread and rejects reentry. No callbacks are accepted or invoked; delivery/transform callbacks and their side effects are outside this consumer. Source/phase currency is checked before reservation and rechecked before returning the candidate. Host/world mutation must remain serialized, as required by the borrowed world. The consumer does not implement cross-thread synchronization.

## Bounds and ownership

Logical output is fixed:64 header bytes plus10 slots at48 bytes =544. Default MaximumOutputBytes544; legal configured ceilings0..65536. All ten slots, including missing ones, are reserved before arrays/fields/receipt are created.543 rejects and544 passes. This accounting is independently defined modern output accounting, not managed allocation measurement. Read work is bounded by the existing world's object cap and ten fixed lookups; world snapshot queries inspect its bounded object inventory. The frozen coordinator has only parameterless CaptureTangibleIngress and its direct ObjectFor lookup is private; no per-object ingress overload is available. Exact registry identity is preserved using the documented bounded scan rather than expanding frozen Core or reflecting into production.

The copied policy and read-only fields cannot be mutated by callers. Failed reads do not modify partial state, world changes, event journal, source revision or previously issued receipts. Returned observations remain immutable historical data after invalidation/disposal. Dispose is owner-thread, idempotent, clears the consumer's borrowed world reference and invalidates its current-receipt checks. It does not close or mutate the world or erase caller-owned historical receipt data. Read after consumer disposal throws; IsCurrent returns false for valid nonnull receipts.

## Evidence and implementation consequence

Observation: actual Core source and an independently authored literal-byte/presence/provenance/lifecycle model agree on128 cases,1408 actions and10240 field observations. Cases include all ten F32 slots, missing/listen-only state, repeated assignments, signed-zero, subnormal, finite negative/max values, signed infinities and NaN payloads, pending/finalized phase transitions, containment, destroy/recreate and reset. Native negatives add replacement, foreign-world/source/consumer, owner affinity, output bounds, immutability, atomic failure and deterministic disposal/close. Confidence: Confirmed for this bounded modern implementation under serialized synthetic inputs. No original executable behavior or runtime-default inference is asserted.

[Round47 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R47_RECEIVED_MOVEMENT_INPUTS.md) records frozen identities, reproduction, regressions and corrections. The scenario suite (external historical reference) uses a separate actual-Core runner (external historical reference); its independent peer (external historical reference) generates literal envelopes and models lifecycle/raw classification without importing production/native expectations.

Next bounded join: pair exact-current nullable received eligibility with separate authored provenance. Preserve both origins, require an explicit policy for any selected walk/run field, and provide no fallback, default seed, modifier arithmetic or original-runtime claim.

## Round48 compatibility increment

[Explicit movement admission](SCENE_WORLD_EXPLICIT_MOVEMENT_ADMISSION_CONTRACT.md) composes source currency with an owned current-incarnation driver. The [144-case actual join](SCENE_WORLD_CAPTURED_MOVEMENT_JOIN_CONTRACT.md) preserves later source staleness and captured velocity. The reader remains received-only and does not seed or modify raw state.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
