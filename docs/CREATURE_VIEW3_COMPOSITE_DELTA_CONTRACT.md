# Atomic CREO view 3 composite delta

The composite codec supports field 5 (components), fields 11-16 (scalars), and field 17 (wounds) in received order. It decodes one owned plan and returns a new state only after every field applies successfully. Other indices reject. This bounded implementation does not establish complete eighteen-field delta coverage or live interoperability.

## API and integration

Types reside in `Stardust.Core.Protocol`:

```csharp
public sealed record CreatureView3CompositeDeltaLimits(
    int MaximumPayloadBytes, int MaximumFields, int MaximumScalarUpdates,
    int MaximumWoundsCommands, int MaximumWoundsValues, int MaximumWoundsElements,
    int MaximumComponentsCommands, int MaximumComponentsKeys,
    int MaximumComponentsElements);

CreatureView3CompositeState.FromBaseline(CreatureView3BaselineSnapshot baseline,
    int maximumRawComponentsKeys, int maximumComponentsElements,
    int maximumWoundsElements);
new CreatureView3CompositeState(ulong objectId, CreatureView3ScalarState scalars,
    CreatureView3WoundsState wounds, CreatureView3ComponentsState components,
    CreatureView3ComponentsSnapshot rawComponents);

CreatureView3CompositeDeltaCodec.Decode(ReadOnlySpan<byte> message,
    CreatureView3CompositeDeltaLimits limits,
    CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval);
CreatureView3CompositeDeltaCodec.Decode(ReplicationEnvelope envelope,
    CreatureView3CompositeDeltaLimits limits,
    CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval);
CreatureView3CompositeDeltaCodec.Encode(CreatureView3CompositeDeltaPlan plan,
    CreatureView3CompositeDeltaLimits limits);
plan.Apply(CreatureView3CompositeState initial);
```

Decode returns `CreatureView3CompositeDeltaPlan`, Encode returns `byte[]`, and FromBaseline/Apply return `CreatureView3CompositeState`. State properties are ObjectId, Scalars, Wounds, Components and RawComponents. RawComponents preserves the baseline's original signed key order, duplicates and counter; Components is the separate normalized signed ascending unique set. Applying deltas retains the immutable raw snapshot.

Plan properties are ObjectId, DeclaredCount, Policy, MaximumWoundsElements, MaximumComponentsElements and read-only Updates. An update exposes Index and nullable Scalar, Wounds and Components properties; exactly one is populated. Constructors for plans and updates are internal. Existing immutable field types are reused. Seed constructors reject mismatched object IDs and nonfinite height.

The caller selects the wounds policy before parsing, applies against the current object's complete state, then publishes the returned state once. Object ID matching is enforced; incarnation/generation freshness and store publication remain caller responsibilities. A successful prefix must not be published independently.

## Wire, kernels and bounds

Application framing uses the accepted delta envelope: operand count 5, opcode `0x12862153`, CREO tag `0x4352454F`, view 3, little-endian fields and exact payload size. Raw U16 DeclaredCount is preserved but does not control traversal: bounded field exhaustion does. Duplicate fields and scalar/component/wounds interleaving retain their wire order. Parsing is direct, without reconstructed per-field wire envelopes.

Field structures and evidence are inherited from the [scalar contract](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md), [mixed contract](CREATURE_VIEW3_MIXED_DELTA_CONTRACT.md), [components contract](CREATURE_VIEW3_COMPONENTS_DELTA_CONTRACT.md) and [legacy wounds contract](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md). Components use the recovered set skip calculation, membership-sensitive increments and unsigned final counter maximum. Wounds retain the independently selected StrictInterval or LegacyBounded behavior, including their different assign cursor/count and final-counter rules. No common counter policy is introduced.

Payload, total fields, scalar updates, wounds command bounds/values and components commands/keys have separate cumulative decode and encode budgets. Components keys count Drop/Add operands; Clear consumes none. Legacy wounds assign consumes its recovered value-count-plus-one cursor units. Both result-container element caps are retained in the plan and enforced during Apply, including transient growth and initial state. All commands are structurally parsed and budgeted even when later skipped by a counter kernel. Counts and remaining bytes are checked before command/value allocations. These are logical bounds, not a measured total heap allocation cap.

Apply uses temporary immutable states and the accepted field kernels. Wrong object, strict counter rejection, late container growth or another failure returns no replacement state and leaves all input channels intact. Malformed/truncated fields, unsupported indices and nonfinite height reject during Decode. Encode preflights cumulative limits and preserves exact raw count, field order, integer bits and finite floating-point bits, including negative zero.

## Validation and confidence

Observation: the native scenarios and independent Python peer agree on synthetic baseline seeding, exact re-encoding and all state channels for 18 cases. Cases include both wounds policies, repeated/interleaved fields, wrapping counters, future/stale components, no-ops, empty deltas, wrong policy, late growth/counter failures, unsupported/truncated fields, NaN and separate key/value budgets. Native checks additionally exercise every truncated prefix, each aggregate cap, object mismatch, raw ownership and unchanged inputs. Accepted mixed/components regression scenarios pass.

Interpretation: the accepted independent kernels can compose atomically without changing their boundary semantics. Confidence: Confirmed for tested byte structure and bounded implementation behavior; inherited executable evidence retains the confidence stated in the source contracts. Synthetic fixtures provide no new real packet occurrence or server interoperability evidence.

Reproduce with `tools/python/verify_creature_view3_composite_deltas.py --generate <peer.json>`, the offline runner `poc/round13-tests/Networking/Networking.csproj` with peer/export arguments, then the Python verifier's `--verify <export-directory>`. Generated artifacts belong in ignored storage. Proposed shared smoke registration: `CreatureView3CompositeDeltaScenarios.Run(peerPath, exportDirectory)`; the lead owns shared registration edits.

Next bounded action: integrate composite state publication into the existing world transaction while preserving incarnation checks and a single atomic publication. Other CREO:3 delta fields remain unsupported.


## Round13 accepted world caller

The [incarnation store](SCENE_CREATURE_REPLICATION_STORE_CONTRACT.md) now applies this complete plan in one candidate/budget/revision commit; the [new world mode](SCENE_WORLD_REPLICATION_CONTRACT.md) fully decodes before staging and publishes one acknowledged change per message. Complete baseline data stays unchanged while current normalized components/wounds/scalars evolve. Lead original-resource integration and independent oracle verify both kernels, ownership, aggregate rollback and late whole-generation failure. Codec tests retain18 independent cases. Original component callbacks/gameplay semantics remain separate.
