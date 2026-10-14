# Fresh polygon boundary influence

TerrainBoundaryPolygonInfluence (external historical reference) evaluates a supplied coordinate pair against a fresh decoded [BPOL input](TERRAIN_BOUNDARY_POLYGON_INPUT_CONTRACT.md). Ordered points, raw metadata and immutable owned receipts are preserved. Cache and query/shaping use separately required nearest-even 24/53/64 precision profiles; the original active precision, rounding and exception controls remain Unknown. This contract covers direct fresh cache/query behavior and the local enabled/shaping caller. It does not assign coordinate axes or units, repair topology, reproduce edited runtime state, combine layers or generate terrain. See the [Round 40 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R40_POLYGON_INFLUENCE.md) for reproduction.

## Evidence and interpretation

Addresses are baseline VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, verified before static reads. The executable was not run.

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Constructor `00B73BA0` calls base `00B736E0` at `00B73BAA`; common base `00B63333/36` clears selector +14 and amount +18. Vector capacity/data/count are +1C/+20/+24; bounds are +28/+2C/+30/+34. Vtable `015ED434` points +24 to `00B73D00` and +38 to `00B738B0`. | Fresh metadata defaults and query/cache identity, Confirmed. | Absent version-0/1 fields resolve to runtime zero without changing nullable serialized fields. |
| Accepted loaders preserve ordered points and raw finite amount; `00B65FC6` stores +18 directly. | No fresh BPOL amount clamp, Confirmed. | Preserve negative amounts and signed zero; rectangle normalization is inapplicable. |
| Cache `00B738B0` initializes minima to `7F7FFFFF`, maxima to `FF7FFFFF`, then selects only strict smaller/larger point components. It contains no arithmetic instructions. | Bounds are ordered load/copy selections, Confirmed. | Equal extrema preserve first-selected bits; cache precision is explicit provenance but does not change finite bounds. |
| Query `00B73D00` has no calls and reads points, count, bounds and amount, not extension flag/scalars/name. | Recovered geometry is independent of extension metadata, Confirmed for this path. | Retain unknown extensions without inventing geometry rules. |
| Query performs bounding rejection, half-open vertical ray tests, strict horizontal crossing, vertex and eligible segment distances, then a tolerance comparison. | Exact conditional graph below, Confirmed for finite defined arithmetic. | Preserve order, stores, retained values and branch sequencing. No general polygon-edge inclusion rule follows. |
| Runtime methods `00B73A20/00B73A60` can change points. | Edited state differs from the fresh domain, Confirmed; caller semantics remain open. | No inferred translation or editing API. |
| Destructor `00B73C00` releases extension name then `00B73740` releases vector storage and clears vector state, followed by boundary/common cleanup `00B63350`/`00B62EF0`; deleting wrapper `00B73BD0`. | Legacy ownership chain, Confirmed. | Replacement owns immutable arrays and has no external handle or callback; managed collection timing does not reproduce destructor timing. |
| Caller `00B63F8E` tests enabled, invokes query +24 at `00B63FAF`, then dispatches unsigned selector through `00B642A0`; final shaped F32 store is `00B63FE0`. | Local gate and shared shaping, Confirmed. | Unknown selector returns +0 after query; disabled evaluation skips geometry. |

## Exact finite graph

`R` rounds each query arithmetic instruction nearest-even to the selected significand width with wide exponents. `F32` is an additional nearest-even store; `reload` reads that stored value. Numeric comparisons do not normalize raw signed-zero bits.

The bounding test accepts endpoints inclusively and handles reversed endpoints. Outside returns +0. Empty cache retains reversed sentinel bounds; the subsequent empty parity loop returns +0.

For each current point `(cx,cy)`, previous `(px,py)` starts at the last point. A crossing candidate requires `cy <= y < py` or `py <= y < cy`. Compute without an F32 store:

```text
intersection = R(R(R(R(y-cy) * R(px-cx)) / R(py-cy)) + cx)
```

Toggle parity only when `intersection > x`, then advance previous to current. Even parity returns +0. Odd parity with numerically zero amount, including -0, returns +1. For the integer square `(0,0),(2,0),(2,2),(0,2)`, zero-feather lower/left edges include their lower corner; upper/right edges are parity-outside. This example is not a universal edge classification: order and finite rounding remain significant.

For odd parity and nonzero amount `f`, retain `minimum = R(f*f)` while also storing `squareBits = F32(minimum)`. For every vertex:

```text
dx = R(x-vx); dy = R(y-vy)
candidate = R(R(dy*dy) + R(dx*dx))
candidateBits = F32(candidate)
if candidate < minimum: minimum = reload(candidateBits)
```

The comparison precedes reload. A stored candidate may round upward; do not recompute a mathematical minimum after the store.

For every ordered segment from previous `(ax,ay)` to current `(bx,by)`, including implicit closure:

```text
nx = F32(R(ax-bx)); ny = F32(R(ay-by))
dy = F32(R(by-ay)); dx = F32(R(bx-ax))
dot = R(R(R(x-ax)*reload(dx)) + R(R(y-ay)*reload(dy)))
length = R(R(reload(ny)*reload(ny)) + R(reload(nx)*reload(nx)))
t = R(dot/length); tBits = F32(t)
```

Eligibility tests the unspilled `t >= 0`, then reloaded `tBits <= 1`. Eligible projection is `qx = R(R(reload(dx)*reload(tBits))+ax)` and similarly `qy` with dy/ay. Compute, store and conditionally reload its distance using the vertex graph above. Outside projections are skipped, without endpoint clamping; the vertex pass already supplies endpoint candidates. A repeated adjacent/closing point can cause zero-length division on this reached path. Original exceptional handling is Unknown; the modern evaluator rejects undefined arithmetic. Duplicate points are preserved and can still complete through earlier branches.

Literal `015E4F7C` has raw bits `38D1B717`. With this epsilon:

```text
lower = R(reload(squareBits)-epsilon)
upper = R(reload(squareBits)+epsilon)
u = 1 if lower <= minimum <= upper
    otherwise R(sqrt_R(minimum)/f)
```

The inclusive band uses the retained minimum. The query does not store final `u` to F32. `QueryProjectionBits` is inspection only; shaping consumes its retained value. Negative finite amounts are not clamped and can yield negative influence or -0 outside the full band.

Shared shaping uses the query profile: selector 0 identity, 1 `R(u*u)`, 2 `sqrt_R(u)`, 3 ordered `R(R(R(3-R(u+u))*u)*u)`. Other unsigned selectors yield +0 after query. Store shaped output to F32. No 0..1 clamp, inferred world width or premature query projection is permitted.

## Public ownership and limits

`Cache` owns a point snapshot, immutable raw metadata, raw bounds and cache profile. `CopyPoints()` returns an independent array. `Query` accepts either input plus both profiles or an existing cache plus query profile. Its immutable receipt records branch, crossings when reached, stored square/minimum when reached, eligible projection count and inspection projection. `Shape` consumes that receipt. `Evaluate` applies the local enabled gate. `EvaluateBatch` snapshots supplied coordinates and shares one owned cache across its immutable query receipts; `CopyInfluenceBits()` returns independent storage.

`TerrainPolygonInfluenceLimits` defaults to 8,192 points, 65,536 samples and 1,048,576 charged point visits. Hard maxima are 65,536 points, 1,048,576 samples and 67,108,864 visits. Cache charges N, direct cached query charges 3N, input Query/Evaluate charges 4N, and active nonempty batch charges N+3NS. These are conservative modern loop-work charges, including early exits, not observed instruction counts. Point/sample caps apply before snapshots; active work is checked before cache/result allocation. Empty or disabled batches charge zero and create no cache. Per-result batch charges exclude the shared cache charge. Raw decoder budgets remain separate.

Null arguments produce `ArgumentNullException`; invalid option/profile ranges produce `ArgumentOutOfRangeException`. Unsupported metadata, finite-domain violations, undefined arithmetic, nonfinite observed F32 stores/projections and excessive work produce `InvalidDataException`. Disabled evaluation still validates profiles, caps and known metadata but ignores unused nonfinite geometry/amount/coordinates. Empty active batches do not execute geometry. Nonfinite unknown extension scalars do not participate. Batch failure publishes no partial receipt; managed temporary allocation is not a deterministic wipe guarantee. Approximate payload bounds are 8N point bytes and 8S coordinate bytes, not total managed heap measurements.

Zero/one/two-point, collinear and self-intersecting sequences retain direct branch behavior where arithmetic is defined; acceptance does not certify valid polygon topology. Original NaN/exception policy, active x87 controls, units, axes, editing lifecycle and extension feature semantics remain Unknown.

## Verification

The independent verifier (external historical reference) uses Fraction/integer arithmetic and a separate control-flow implementation. All 18,120 vectors agree: 16,872 accepted, 1,248 rejected; 48 ownership/budget/identity checks and six null negatives pass. Fixtures exercise both windings, boundaries, tolerance straddles, degenerate sequences, all raw version grammars, selectors, negative/subnormal amounts, overflow, disabled/empty cases, exact caps and independent profiles. There are 210 query-profile-sensitive inputs, zero cache-profile-sensitive inputs and 256 cases sensitive to premature query projection before shaping. These are test distinctions, not original precision identification.

Static verification checks 93 new anchors and 87 finite status-mask truth-table cases; the accepted raw-input verifier separately rechecks 71 anchors. One pinned original ten-point BPOL raw record is independently decoded and compared. No original influence query is executed and no original arithmetic parity is claimed. Core/default smoke and the separate public-API runner build with zero warnings/errors and execute successfully.
