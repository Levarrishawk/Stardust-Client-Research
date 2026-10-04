# Supplied layer influence followed by ordered AHCN heights

[TerrainSuppliedLayerHeight](../poc/Stardust.Core/Geometry/TerrainSuppliedLayerHeight.cs) evaluates one immutable supplied layer sample and an owned ordered collection of decoded constant-height rules. It composes the [accepted layer combination](TERRAIN_LAYER_INFLUENCE_IMPLEMENTATION_CONTRACT.md) and [AHCN kernel](TERRAIN_HEIGHT_CONSTANT_OPERATION_CONTRACT.md); it introduces no second implementation of either arithmetic graph. The API consumes supplied boundary/filter query results and incoming influence. It does not resolve real filters, recursive layers, cache activation, ADTA fields, normal/material generation or original generated terrain.

## Public API and provenance

```csharp
TerrainSuppliedLayerHeightSample.Create(
    TerrainLayerInfluenceSample layer, uint initialHeightBits,
    ReadOnlySpan<TerrainHeightConstantInput> rules,
    TerrainHeightArithmeticPrecision affectorPrecision,
    int maximumRules = 256)

TerrainSuppliedLayerHeight.Evaluate(
    TerrainSuppliedLayerHeightSample sample,
    TerrainSuppliedLayerHeightLimits? limits = null)

TerrainSuppliedLayerHeight.EvaluateBatch(
    ReadOnlySpan<TerrainSuppliedLayerHeightSample> samples,
    TerrainSuppliedLayerHeightLimits? limits = null)
```

The sample retains immutable `Layer`, exact `InitialHeightBits`, an owned read-only `Rules` reference collection, `AffectorPrecision` and complete `OperationCharge`. Decoded AHCN inputs and their headers are immutable; references may be shared and are retained. The layer sample already owns its ordered boundary/filter collections and immutable query carriers. No mutable source array is borrowed.

The one authorized extension to [TerrainLayerInfluenceQuery](../poc/Stardust.Core/Geometry/TerrainLayerInfluenceQuery.cs) adds `FromPolygon(TerrainPolygonQuery)` and `Polygon` provenance. Source enum values remain raw=0, circle=1 and rectangle=2; polygon appends value3. The new factory retains the frozen polygon query's internal Number, precision and immutable source receipt. It does not substitute `QueryProjectionBits` before shaping. Existing constructors/factories retain their behavior; the private constructor merely gains an optional polygon reference. All existing factory fixtures are replayed.

Query/cache precision provenance remains distinct from explicit layer combination/product precision and executor affector precision. Supported arithmetic profiles are nearest-even Binary24, Binary53 and Binary64 with the accepted wide intermediate exponents and observed F32 stores. No active original profile is assumed.

## Execution sequence

1. Validate the complete supplied sample/batch budgets before allocating execution buffers or evaluating a layer. Charge all supplied queries and rules, including disabled, skipped, unvisited and aliased references.
2. Call the frozen layer evaluator once. Its candidate/unspilled comparisons, source-order exact exits, inversions, local gates and stored incoming product remain unchanged.
3. If `Influence.AffectorEligible` is false, return the raw initial height and no rule visits. A positive product obtained from two negative factors does not bypass the local gates.
4. Otherwise visit every supplied rule in source order through the frozen AHCN wrapper. Disabled rules produce `SkippedDisabled` diagnostics with unchanged height. Enabled rules with nonpositive product produce `SkippedNonPositiveInfluence`, including operation4. Local-positive eligibility with incoming zero or negative influence still reaches this independent kernel gate.
5. Active positive-product AHCN operations follow the frozen exact graph. Operation4 returns positive zero and `InvalidOperationZero` without invoking a legacy reporter; other out-of-range operation words follow default blend. Feed each returned F32 height word into the next rule. Do not fuse operations or defer intermediate stores.
6. Publish the complete immutable result only after all visited rules succeed. No callbacks or external writes occur between rules.

`TerrainSuppliedLayerHeightReceipt` exposes `Sample`, `Influence`, ordered read-only `Visits`, `FinalHeightBits` and `FinalHeightIsFinite`. A `TerrainSuppliedLayerHeightVisit` contains `Index` and the full frozen `TerrainHeightConstantResult`, including input/influence/constant/operation/enabled/result bits, outcome and affector precision. Disabled entries appear as diagnostic visits only when the local layer gates permit traversal. They do not represent a legacy virtual call. A locally skipped sample has an empty visit list, while complete rules remain inspectable through the retained sample.

`TerrainSuppliedLayerHeightBatch.Results` is read-only; `CopyHeightBits()` returns a new array. All returned receipts and earlier inputs survive a later failure. Immutable reference sharing has no disposal obligations or engine resource lifetime; .NET owns temporary storage. Concurrent mutation of caller arrays during a call is outside the caller contract.

## Raw height and finite policies

The initial height is raw F32 data. The executor preserves frozen kernel eligibility: unused nonfinite initial/constant words may survive local skips, disabled rules, nonpositive products or an empty rule list. A positive-product operation4 may recover such an initial word to positive zero before a later finite rule. Active arithmetic otherwise requires finite height/constant inputs and finite final F32 stores. Layer inputs, shaped stores and product follow the frozen finite policies.

`FinalHeightIsFinite` is an inspection flag, not an implicit display clamp or blanket rejection of skipped raw words. A renderer or supplied-patch consumer must apply its own existing finite/display eligibility. Exceptional legacy floating-point behavior, masked NaN execution, reporter effects and original acceptance are not emulated.

The layer receipt always requests a product under its modern finite-output policy; original cache/affector activation may avoid that instruction. This executor does not close that original lifecycle. Supplied fixed filter queries do not prove that precomputing a whole layer before earlier height mutations is valid.

## Complete work reservation

`Create` accepts `maximumRules` in0..4096, defaults256, checks the entire rule count and null references, validates affector precision, computes a conservative graph charge and then snapshots references. The aggregate executor limits are:

| Limit | Default | Hard ceiling |
|---|---:|---:|
| MaximumSamples | 256 | 65,536 |
| MaximumQueriesPerSample | 256 | 4,096 |
| MaximumTotalQueries | 65,536 | 1,048,576 |
| MaximumRulesPerSample | 256 | 4,096 |
| MaximumTotalRules | 65,536 | 1,048,576 |
| MaximumOperations | 327,680 | 5,242,880 |

Zero caps allow empty work. Queries are the complete boundary+filter count, not the number reached. Rules are the complete supplied count, not enabled or applied rules. Operations are a documented worst-case reservation for arithmetic plus final F32 store per complete AHCN rule:

| Raw operation | Arithmetic plus store reservation |
|---|---:|
| 1 or2 | 3 |
| 4 | 1 |
| 0,3 and every other U32 | 5 |

Disabled/local-skipped/nonpositive-product rules reserve the same complete graph charge. This is not a count of actual executed arithmetic or BigInteger/CPU instructions. It distinguishes ordered rule storage from worst-case operation work and prevents eligibility from weakening budgets. Layer/query construction work predates this executor; its own frozen factory/cache budgets remain the caller's responsibility.

Single evaluation checks complete counts and totals before layer/rule result allocation. Batch evaluation checks every sample, per-sample count and aggregate query/rule/operation reservation before copying the outer collection or allocating results. A later shaping, product or AHCN overflow failure publishes no partial batch. No user inputs or earlier results are changed; collection aliases are charged on every occurrence.

## Evidence, confidence and verification

**Confirmed direct prerequisites:** the general layer graph and AHCN source-order caller/process stores described in their frozen contracts. AHCN process `00B76540` has one final F32 height store; its caller checks local enabled state and supplies the F32 local*incoming product before the virtual call. The new wrapper's ownership, budgets, diagnostics and raw output behavior are modern implementation policies. No new original field mapping is inferred.

The [independent oracle](../poc/round41-tests/Rendering/verify_height.py) uses the frozen Fraction/integer query, layer and AHCN references. Its polygon adapter returns the Fraction query before the inspection/shaping section, preserving the unspilled value independently of production. Synthetic decoded fixtures exercise mixed raw/circle/rectangle/polygon carriers, all five independently selected phase profiles, selectors, operations, gates, products, signed zeros/subnormals, nonfinite unused heights, operation4 recovery, source-order stores, overflow, late failure and complete caps. The [native scenario](../poc/Stardust.Core.SmokeTests/TerrainSuppliedLayerHeightScenarios.cs) checks immutable aliases, budgets and retained failures through the actual separate Core assembly.

The [isolated replay](../poc/round41-tests/Rendering/Invoke-SuppliedHeight.ps1) builds actual Core/reference-runner and Core/SmokeTests, replays every frozen factory fixture and SHA-gates static prerequisite checks. It uses explicit cached tool paths, offline NuGet and private output. Wrong/truncated identities reject before address decode or fixture creation. Final counts and commands are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R41_SUPPLIED_LAYER_HEIGHT.md).

**Unknown:** active original arithmetic controls, real filter/height state dependencies, recursive/cache activation and generated original heights. Exact next probe: trace one filter query's dependence on preceding height mutations before introducing any full layer traversal.
