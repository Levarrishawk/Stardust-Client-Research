# Supplied ordered flat layer sequence

[TerrainSuppliedLayerSequence](../poc/Stardust.Core/Geometry/TerrainSuppliedLayerSequence.cs) threads one raw F32 height through an ordered flat plan of immutable supplied layer descriptors. Each stage invokes the frozen [supplied layer/AHCN executor](TERRAIN_SUPPLIED_LAYER_HEIGHT_CONTRACT.md), preserving its query precision, local/product gates, rule source order and F32 stores.

The ordering of this supplied flat plan is a **modern contract**. It does not establish original recursive layer traversal, cache/ancestor activation, real height-dependent filter evaluation, ADTA flags or original generated terrain. Fixed supplied queries are reused even after preceding heights change; their validity as precomputed real filters is not claimed.

## API and ownership

```csharp
TerrainSuppliedLayerSequenceLayer.Create(
    TerrainLayerInfluenceSample layer,
    ReadOnlySpan<TerrainHeightConstantInput> rules,
    TerrainHeightArithmeticPrecision affectorPrecision,
    int maximumRules = 256)

TerrainSuppliedLayerSequencePlan.Create(
    uint initialHeightBits,
    ReadOnlySpan<TerrainSuppliedLayerSequenceLayer> layers,
    int maximumLayers = 64)

TerrainSuppliedLayerSequence.Evaluate(
    TerrainSuppliedLayerSequencePlan plan,
    TerrainSuppliedLayerSequenceLimits? limits = null)

TerrainSuppliedLayerSequence.EvaluateBatch(
    ReadOnlySpan<TerrainSuppliedLayerSequencePlan> plans,
    TerrainSuppliedLayerSequenceLimits? limits = null)
```

A descriptor snapshots ordered AHCN references using the frozen height-sample factory. It retains an immutable layer sample, query carriers, explicit affector precision and frozen `OperationCharge`. Its private template has initial height zero solely for validated storage; traversal always replaces that word with the current raw height. The template height is not an input to execution.

A plan snapshots the descriptor reference collection, exact initial bits and checked-derived aggregate charges/maxima. Descriptors, rules, layer samples and query receipts are immutable and may be shared. Caller array changes after creation cannot alter a plan. Concurrent array mutation during a call is outside the caller contract. Plan construction requires nonnull descriptors and `maximumLayers` in0..4096; the default is64. Descriptor construction uses frozen rule/precision/null validation and its own rule cap0..4096.

## Source order and receipts

Evaluation initializes current height from `Plan.InitialHeightBits`. For each descriptor in source order it creates a frozen height sample with this current word, evaluates it once, records a stage and assigns `Receipt.FinalHeightBits` to the next stage's initial word. Queries, gate/count/enabled concepts, incoming weight and arithmetic policies come from that descriptor and are not inherited from the previous layer. No product or ancestor influence is threaded implicitly.

A locally skipped stage leaves height unchanged and has the frozen empty rule visits. A local-positive stage still uses the frozen AHCN nonpositive-product gate. Disabled rules, operation4 recovery, unknown-operation default blend, raw unused nonfinite words and per-rule F32 stores remain unchanged. There are no endpoint substitutions, callbacks, normal/material generation or engine resources.

`TerrainSuppliedLayerSequenceReceipt` exposes `Plan`, ordered read-only `Stages`, `FinalHeightBits` and `FinalHeightIsFinite`. Each `TerrainSuppliedLayerSequenceStage` contains `Index` and the complete frozen height `Receipt`. That child retains the actual stage initial height, unchanged supplied layer provenance, owned rule snapshot, influence decisions and rule visits. Final finiteness is inspection rather than a hidden clamp/rejection of skipped raw words. Empty plans retain the initial raw word, including signed zero or a NaN payload.

`TerrainSuppliedLayerSequenceBatch` exposes read-only `Results`, complete aggregate `Charge` and `CopyHeightBits()`, which returns a new array. No partial plan or batch is published on a later layer/rule failure. Previously returned receipts, immutable inputs and caller arrays remain unchanged. Temporary partial receipts are managed by .NET; GC timing is not a legacy destruction guarantee.

## Complete preflight and frozen child budgets

The plan caches immutable complete counts and per-layer maxima at creation. Evaluation validates all configuration, sample/layer caps, query/rule maxima and aggregate charges before allocating result/stage arrays, transfer arrays or child samples. Batch admission uses each immutable cached charge and checks progressive totals, so oversized plans do not cause unbounded nested rescanning. Every occurrence of aliased plans/descriptors is charged; disabled, skipped and unvisited entries still count.

| Limit | Default | Hard ceiling |
|---|---:|---:|
| MaximumSamples (plans) | 256 | 65,536 |
| MaximumLayersPerSample | 64 | 4,096 |
| MaximumTotalLayers | 16,384 | 65,536 |
| MaximumQueriesPerLayer | 256 | 4,096 |
| MaximumTotalQueries | 65,536 | 1,048,576 |
| MaximumRulesPerLayer | 256 | 4,096 |
| MaximumTotalRules | 65,536 | 1,048,576 |
| MaximumOperations | 327,680 | 5,242,880 |
| MaximumRuleReferenceSlots | 131,328 | 2,101,248 |

Zero caps allow empty work. Complete queries are the sum of supplied boundary/filter entries. Rule and operation charges come from the frozen child descriptor: operations1/2 reserve3 arithmetic/store steps, operation4 reserves1, and all other words reserve5, regardless of later eligibility. These logical reservations are not executed CPU/BigInteger counts.

For each stage the child receives explicit limits `(1, MaximumQueriesPerLayer, MaximumTotalQueries, MaximumRulesPerLayer, MaximumTotalRules, MaximumOperations)` and its factory receives `MaximumRulesPerLayer`. No hidden child defaults weaken or unexpectedly narrow a valid admitted plan. Profile selection remains explicit in each immutable descriptor.

### Rule-reference duplication and other bounded buffers

The frozen public factory accepts a span while an immutable descriptor exposes a read-only collection. Therefore each stage uses one temporary `ToArray()` transfer and one child-owned rule snapshot. This necessary duplication is explicit rather than hidden behind an unbounded adapter.

For complete aggregate rule count `R` and largest layer rule count `M`, `RuleReferenceSlots` conservatively reserves **2*R + M**:

- `R` for descriptor-owned template rule arrays retained by the supplied plans;
- `R` for child-owned rule arrays retained by all possible completed stage receipts;
- `M` for the current transfer array.

This is an upper bound on reachable logical AHCN reference slots. Aliases are charged repeatedly even when actual storage is shared. It excludes arbitrary caller-retained previous calls, referenced rule payload objects, array/object headers and unreachable storage awaiting GC; it is not a byte-accurate heap cap. No transfer array is retained in the returned receipt.

Other bounded structures remain explicit: plan descriptor references and returned stage references are each bounded by total layers; batch copied-plan/result references are each bounded by sample count; child AHCN visit value records are bounded by total rules; child influence visit records are bounded by total queries. Frozen child temporary list/snapshot storage retains its own documented bounds. Query/cache/descriptor construction predates this execution admission and keeps its own factory limits.

## Evidence and verification

**Confirmed prerequisites:** frozen layer and AHCN arithmetic/store semantics, immutable decoded rules and retained query carriers. **Modern implementation policy:** this flat ordering, complete admission budgets, peak reference reservation, receipts and atomic publication. No new original runtime field or recursive order is inferred.

The [independent oracle](../poc/round42-tests/Rendering/verify_sequence.py) composes the accepted Fraction/integer height model in source order while independently calculating complete counts, child budget mapping and reference reservations. Synthetic fixtures include raw/circle/rectangle/polygon carriers, all five phase profiles, source-order mutations, disabled rules, local/incoming gates, signed zeros, unused nonfinite recovery, finite overflow, exact caps and late failures. The [native scenario](../poc/Stardust.Core.SmokeTests/TerrainSuppliedLayerSequenceScenarios.cs) checks actual separate Core behavior, aliases/retained results, aggregate preflight before failure and full supported per-plan/per-layer caps.

The [isolated replay](../poc/round42-tests/Rendering/Invoke-LayerSequence.ps1) performs SHA-gated static prerequisites, exact native comparison, actual Core/SmokeTests builds and wrong/truncated executable identity negatives. It uses cached tools, cleared offline NuGet feeds, private environment/output and no Python bytecode. Final counts, input pins and commands are recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R42_LAYER_SEQUENCE.md).

Original active arithmetic controls, real filter/state dependencies, recursive/cache/ancestor behavior and complete generated heights remain unknown. Next probe: determine whether one real filter query observes height changed by preceding affectors before allowing precomputed queries in a full generator.
