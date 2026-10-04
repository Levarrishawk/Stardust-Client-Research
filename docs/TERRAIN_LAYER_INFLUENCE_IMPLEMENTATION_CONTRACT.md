# Conditional terrain layer influence implementation

This API implements the finite general per-sample combination graph documented in the [static layer specification](TERRAIN_LAYER_INFLUENCE_CONTRACT.md). It consumes ordered supplied query results and a supplied incoming F32 weight. It does not execute filters, affectors, a layer callback or TGEN traversal. Querying all layers before height mutations is not established by this work.

## Evidence and supported arithmetic

The executable baseline is SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The independent [static checker](../poc/round40-tests/Rendering/static_layer.py) checks identity before consuming addresses, rechecks the 58 coordinate prerequisites and 60 layer anchors, adds 41 exact graph anchors and two enabled-getter instructions, and checks both selector tables, three constants and twelve status outcomes. No original execution or original generated-influence comparison occurred.

**Confirmed observations:** `00B63F44` loads the signed boundary gate at +40; `00B63F78` reads count +18 and `00B63F88` loads the array +14. `00B62F20` reads the local enabled byte +8. `00B63FAF` queries vtable +24. The filter gate at +44 is loaded at `00B64054`, count +24 at `00B6405F`, array +20 at `00B64070`, and query vtable +14 at `00B640B8`. Selector words come from object +14. The general path has special filter-type-five setup at `00B64083..00B6408D`; this is not modeled as a real filter query by the supplied-value API.

The two tables `00B642A0`/`00B642B0` select the same caller graph:

| Unsigned selector | Ordered operation graph |
|---|---|
| 0 | retain query arithmetic value |
| 1 | query * query |
| 2 | square root of query |
| 3 | twice = query + query; difference = 3 - twice; intermediate = difference * query; result = intermediate * query |
| larger | discard query result; load positive zero |

Each arithmetic operation uses the explicit caller combination precision and nearest-even rounding with a wide exponent. Identity has no extra rounding operation. Query arithmetic was performed under its separately retained query precision, which remains provenance. Product multiplication uses an independently supplied precision. Supported precisions are `TerrainHeightArithmeticPrecision.Binary24`, `Binary53` and `Binary64`; active original query/caller/product profiles and exception policy remain **Unknown**. These are conditional arithmetic graphs, not a selection of the original runtime mode.

## Public immutable inputs

[TerrainLayerInfluenceQuery](../poc/Stardust.Core/Geometry/TerrainLayerInfluenceQuery.cs) provides:

```csharp
FromRawBits(uint bits, TerrainHeightArithmeticPrecision precision)
FromCircle(TerrainCircleQuery query)
FromRectangle(TerrainRectangleQuery query)
ShapeProjectionBits(uint selectorBits, TerrainHeightArithmeticPrecision combinePrecision)
```

`FromRawBits` explicitly marks synthetic or supplied query input. It retains the raw F32 bits exactly, including signed zero. Circle and rectangle factories retain the frozen query's internal arithmetic Number and immutable source receipt without projecting it before caller shaping. They retain source, query precision and inspection projection; `RawBits` is null for a geometric carrier. Public `ProjectionBits` and `ShapeProjectionBits` are F32 inspections; the comparison graph consumes the retained unspilled Number. The internal arithmetic helper stays internal to Core.

`TerrainLayerInfluenceEntry(bool Enabled, uint SelectorBits, TerrainLayerInfluenceQuery? Query)` is immutable. Disabled entries may carry null; enabled entries require a query even if a gate or earlier exit would skip them. Enabled is a supplied normalized predicate, not a decoder for an original enabled word.

`TerrainLayerInfluenceSample.Create` takes the incoming raw F32 word, boundary/filter spans, signed gates, boundary/filter inversion flags, combination/product precisions and `maximumQueries` (default 256, hard ceiling 4,096). It snapshots both ordered entry collections. Counts derive from their complete lengths and remain distinct from gate positivity and entry enabled state. Gates are retained signed values; inversion flags are normalized booleans. All query references point to immutable retained carriers.

[TerrainLayerInfluence](../poc/Stardust.Core/Geometry/TerrainLayerInfluence.cs) exposes:

```csharp
TerrainLayerInfluenceReceipt Evaluate(
    TerrainLayerInfluenceSample sample, TerrainLayerInfluenceLimits? limits = null)
TerrainLayerInfluenceBatch EvaluateBatch(
    ReadOnlySpan<TerrainLayerInfluenceSample> samples, TerrainLayerInfluenceLimits? limits = null)
```

## Combination, storage and exits

1. Start the local accumulator at positive zero and save the incoming F32 word separately. If boundary gate/count permit work, visit boundary entries in source order. Disabled entries are recorded as checked but are not shaped.
2. For each enabled boundary, shape the retained query. Store a candidate F32 word while retaining its unspilled arithmetic result. Compare the old F32 accumulator against that unspilled result; select candidate bits only when old < candidate. Equal values retain the old word. The selected F32 word exits the loop only when numerically equal to one. Values above one do not themselves trigger this exit; there is no added clamp.
3. If no enabled boundary was reached, use one. This includes nonpositive gates, an empty collection and all-disabled boundaries. An enabled boundary returning zero does not get this fallback.
4. Apply boundary inversion as an F32 store of 1 - accumulator. If the result is not positive, skip filters and filter inversion as well as affector eligibility.
5. If the first gate passed and filter gate/count permit work, visit filter entries in source order. Store each candidate while comparing its unspilled result against the current F32 combined boundary/filter accumulator. Select only when candidate < old. Equal values retain old bits. Exit only on exact numerical zero, including negative zero.
6. After a successful first gate, optional filter inversion stores 1 - the combined accumulator, including when the filter loop was empty, disabled or gated off. Apply the second positive gate.
7. Return an F32-stored local * incoming product under its explicit product precision. Incoming zero or negative values do not gate earlier boundary/filter work or local affector eligibility.

The direct store/compare sites are `00B63FE0..00B6400B` and `00B640E9..00B64110`; inversions and positive gates are `00B6402D..00B6404E` and `00B64124..00B64145`. Affector product/store/call are `00B6416F..00B6418C`; conditional cache product/store is `00B641D3..00B641E3`.

The modern receipt always asks for a product, including when local gates fail. It does not model the original cache-activation gate or an empty/disabled affector collection that might avoid a product instruction. Therefore finite-product rejection is a modern output requirement, not a legacy acceptance claim. `AffectorEligible` means that the two local gates permit affector work; actual affector enabled state, order, mutation and callbacks remain outside the API.

## Receipts and ownership

The sample retains `IncomingWeightBits`, `Boundaries`, `Filters`, `BoundaryGate`, `FilterGate`, both inversion flags and both arithmetic profiles. Receipt properties are:

- `LocalInfluenceBits` and `ProductBits` for raw output inspection.
- `BoundaryFallback`, `BoundaryEarlyExit` and `FilterEarlyExit`.
- `BoundaryPositive` for the first gate, `LocalPositive` for successful local gates, `AffectorEligible` (the same local eligibility), and `ProductPositive` for the separately stored product. A negative incoming word can yield an eligible negative product; two negative factors can yield a positive product while local affector eligibility is false.
- `BoundaryVisits` and `FilterVisits`, immutable ordered lists of `TerrainLayerInfluenceVisit`. Each visit retains index, enabled predicate, selector, optional query projection, optional candidate, old and selected accumulator words, replacement and early-exit flags. Disabled visits have no candidate. List count is entries checked; count of enabled visits is queries shaped. Unvisited supplied entries remain accessible through the immutable sample.

Equal signed-zero comparisons retain the old stored word. No clamp normalizes query ranges. Raw negative-zero queries and products retain their own relevant store semantics.

The batch snapshots the input reference collection and returns immutable `Results`; `CopyResults()` copies the outer array. Immutable sample/query/source receipts may be shared and are retained by the result. There are no engine resources, disposal obligations or callbacks. Caller mutation after snapshot cannot change results. Concurrent mutation during a call is outside the caller contract.

## Modern finite domain and budgets

All supplied raw queries and incoming weights must be finite. All carrier inspection projections, visited shaped F32 candidates, inversion stores and final product stores must be finite. A visited square root of a negative finite query is rejected; an unvisited query is not shaped. Values beyond [0,1] are supported whenever the actual visited graph meets these rules. There is no guessed saturation. NaN/infinity, floating-point exception equivalence, denormal-control modes and non-nearest rounding are unsupported.

`TerrainLayerInfluenceLimits` defines logical work budgets:

| Budget | Default | Hard ceiling |
|---|---:|---:|
| MaximumSamples | 256 | 65,536 |
| MaximumQueriesPerSample (boundaries + filters) | 256 | 4,096 |
| MaximumTotalQueries | 65,536 | 1,048,576 |

Zero budgets allow empty work. Every complete supplied entry counts, including disabled, unvisited and repeated immutable references. Sample shape/caps, precisions, incoming finiteness and enabled-query requirements precede the entry snapshots. Batch null/per-sample/aggregate checks precede its reference/result allocations and any combination. Visit storage is bounded by those complete entry counts; arithmetic has fixed supported precision and finite F32 source/final stores. These are work limits rather than a byte-accurate heap allocator.

A failed create/evaluation/batch call publishes no new result. Earlier results and inputs remain unchanged even if a later batch sample fails during shaping or product conversion. Exceptions and private temporary allocations are managed by .NET; no explicit external cleanup is required.

## Verification and remaining work

The independent [Fraction/integer oracle](../poc/round40-tests/Rendering/verify_layer.py) generates synthetic inputs and checks exact receipts through a runner referencing the actual separate Core assembly. It checks all three query/cache/caller/product precisions, all selectors, gate/count/enabled cases, inversions, ordering, exact exits, signed zeros, subnormals, finite overshoot/overflow, budgets, ownership and late failures. The [replay entry point](../poc/round40-tests/Rendering/Invoke-LayerInfluence.ps1) also builds actual Core/SmokeTests and rejects changed/truncated executable identities before decoding or creating fixtures.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R40_LAYER_INFLUENCE.md) records final counts, commands and confidence. This scope establishes a conditional combine/product boundary. Real filter queries, transformed boundary runtime state, recursive/cache activation, affector traversal and original generated heights remain open. Next probe: trace one exact original filter query and its inputs/state dependencies before introducing a full layer evaluator.


## Round45 accepted compatibility increment

Follow-up: [FFRA retained carrier](TERRAIN_FILTER_FRACTAL_CONTRACT.md). The original unresolved boundaries above remain explicit.
