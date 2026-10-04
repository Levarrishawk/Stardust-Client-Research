# Received-only CREO4 scalar state

[CreatureView4PartialState](../poc/Stardust.Core/Protocol/CreatureView4PartialState.cs) accumulates only received scalar assignments from the accepted [CreatureView4Codec scalar delta](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md). It exposes neither a complete baseline nor constructor/template defaults. The [delivery investigation](CREATURE_VIEW4_DELIVERY_CONTRACT.md) establishes why a complete invented zero seed is inappropriate; this bounded state preserves availability without claiming to recover original observer initialization. No world, store, router or pending-baseline admission changes are included.

## API and explicit absence

Empty(ulong objectId, CreatureView4PartialLimits? limits=null, ulong revisionSeed=0) creates an immutable unknown state. ObjectId and modern Revision are metadata; Empty does not increment revision or mark values received. KnownMask is a U16 whose actual field-index bits identify received scalars. KnownCount and KnownFields expose only those fields, in ascending index order. IsKnown(index) and TryGetField(index,out CreatureView4PartialField?) return absence for supported unreceived indices. TryGetField returns false/null, never an internal backing zero. Collection/unknown indices reject these queries. Private eleven-slot backing arrays are not exposed.

Supported indices0/1/4/5/7/8/9/10/11/12 preserve raw U32 F32 words;6 preserves full U64 listen bits. No floating arithmetic, NaN canonicalization, signed-zero comparison, numeric range or gameplay eligibility is applied. Received zero is present and distinct from unknown. Collection fields2/3/13 and unknown indices reject through the frozen scalar grammar/typed constructor.

CreatureView4PartialField exposes ObjectId, StateRevision, Index, Bits and Provenance. Provenance contains source Revision, raw DeclaredCount, Ordinal and Origin=ReceivedScalarDelta. Origin means assignment through this supplied scalar plan; it is not a capture/authenticity claim. Source provenance changes on every assignment and remains unchanged when a different field changes. StateRevision identifies the exact returned state containing that receipt. IsCurrent(receipt) requires this immutable state's private identity, ObjectId and StateRevision. Prior, foreign and sibling-branch receipts are not current, even if bits/revisions agree. An older state remains usable with its own receipts.

## Ordered application and provenance

Apply(CreatureView4Delta plan, CreatureView4PartialLimits? operationLimits=null) returns CreatureView4PartialChange with Previous, Current, DeclaredCount, Events, WorkUsed and DecodedOutputBytes. Current advances checked modern U64 revision once per successful nonempty plan. Overflow, wrong recipient or empty plan reject. The declared U16 metadata is retained per change/source; it does not determine actual update count. No baseline conversion, reset/default origin, collection counter or full-state completeness claim exists.

Each update executes in source order, including repeated fields. CreatureView4PartialEvent contains Index, Ordinal, OldWasKnown, nullable OldBits, NewBits, nullable OldProvenance and new Provenance. An unreceived old field has false/null/null. A later update to that field in the same message observes the earlier received value/source. Equal assignments still replace source provenance and emit one ordered event. This is an explicit modern description policy, not recovered original equality/callback behavior. Previous state and all previous successful branches remain immutable after success or failure.

## Whole-plan bounds before allocation

CreatureView4PartialLimits contains MaximumUpdates=256, MaximumKnownFields=11, MaximumEvents=256, MaximumStateBytes=512, MaximumOutputBytes=32768, MaximumWork=8192 and optional MessageLimits. Hard maxima are4096 updates/events,11 known scalars,16MiB state/work and32MiB output; limits may be zero. MessageLimits uses the frozen CreatureView4Limits validation and is copied with the containing policy. Successful operation overrides become the returned state's retained policy.

Apply first validates policies, exact recipient/update/event cardinality, then frozen MeasureDelta validates every owned scalar's type/width and aggregate wire/decoded/work budget. Next checked revision and a complete presence-mask scan establish final known count. Initial/final state and full event/output/work capacities reject before candidate arrays, receipts or events are allocated. Only after those checks are the fixed private arrays cloned and all events built; the completed state/change returns atomically. Public plans/child scalar DTOs are frozen by accepted constructors. There are no callbacks, mutable publication targets, borrowed arrays or engine effects.

Deterministic logical accounting: state bytes=16+32*knownCount, output=final state bytes+64*updateCount. Empty requires16 state/output bytes and11 work units for its fixed scalar domain. Apply work=22+messageWork+2*updateCount+finalKnownCount; the frozen scalar messageWork is updateCount, hence22+3*updateCount+finalKnownCount. The two eleven-unit scans/copy domains, full message measurement, update preflight/application and known receipt construction are explicit logical charges, not exact CLR heap/CPU measurements. Presence is monotonic, so final state is the largest state candidate. Initial retained known fields still count under stricter operation limits.

## Evidence and reproduction

[Round44 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R44_CREO4_PARTIAL_STATE.md) records pinned inputs, scope and validation. [Public scenarios](../poc/Stardust.Core.SmokeTests/CreatureView4PartialScenarios.cs) run via an actual separate Core project reference in the [role runner](../poc/round44-tests/Runtime/Runtime.csproj). [Independent Python peer](../poc/round44-tests/Runtime/peer.py) builds literal little-endian scalar wires and dictionary/provenance expectations without production imports or native expected-output extraction.

115 synthetic cases/528 messages/12516 events agree. Tests cover all eleven indices, received zero versus absence, repeated/equal updates, raw NaNs/signed zero/full U64 IDs, arbitrary declared metadata, source versus receipt revisions, exact/one-less state/message/work/event limits,4096-update boundary, overflow, aliases/readonly collections, concurrent branches, stale/foreign receipts and late malformed collection/unknown tails. Accepted43 full4 world and default smoke regressions pass separately. Confidence: Confirmed for this bounded modern received-only state. Original constructor/template defaults, observer admission, later full-baseline reconciliation and owner gameplay remain separate; no networking44 dependency is consumed.

Next: independently review this partial receipt contract before a separately bounded current-incarnation partial store/admission design. Such a design must preserve the distinction among unreceived, received-partial and full/default-origin values.

## Round45 accepted compatibility increment

Follow-up: [partial store](SCENE_CREATURE_VIEW4_PARTIAL_STORE_CONTRACT.md). The original unresolved boundaries above remain explicit.

## Round46 accepted compatibility increment

The kernel is now consumed by the [current-incarnation store](SCENE_CREATURE_VIEW4_PARTIAL_STORE_CONTRACT.md), [partial world/router](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_CONTRACT.md) and [headless/GPU host](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_PREVIEW_CONTRACT.md). The original kernel-only scope and next probe above remain historical; unknown values remain absent throughout these joins.
