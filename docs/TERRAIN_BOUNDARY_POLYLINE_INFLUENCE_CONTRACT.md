# Fresh polyline boundary influence

[TerrainBoundaryPolylineInfluence](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainBoundaryPolylineInfluence.cs) evaluates supplied coordinates against a fresh owned [BPLN input](TERRAIN_BOUNDARY_POLYLINE_INPUT_CONTRACT.md). It preserves ordered points and raw width, amount and selector. Cache and query/shaping have separately required nearest-even 24/53/64 profiles. Original active precision, rounding and exception controls remain Unknown. The [Round 41 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R41_POLYLINE_INFLUENCE.md) records reproducibility and scope.

This is the directly recovered finite fresh cache/query and local enabled/shaping boundary. It assigns no coordinate axes, units or full/half-width convention, performs no sorting, winding repair or implicit closure, and does not model edited point state, layers, filters or generated terrain. No original executable query was run.

## Direct evidence and confidence

All addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before this round's static inspection. Previous raw-input evidence was independently rechecked.

| Observation | Interpretation / confidence | Implementation consequence |
| --- | --- | --- |
| Constructor `00B74A20` passes BPLN/kind 3 through `00B736E0`, stores raw 2.0 at +38 and installs vtable `015ED474`. Its +24/+38 slots are `00B74AD0`/`00B74AA0`. | Width default and actual query/cache identity, Confirmed. | Both accepted serialized versions explicitly replace width; do not invent extra serialized defaults. |
| Width setter `00B74A80` stores +38 at `00B74A88`, calls virtual cache +38 at `00B74A8B`; amount setter `00B65FC0` stores +18 directly. Neither loader clamps metadata. | Raw width and amount survive fresh loading, Confirmed. | No absolute value, amount clamp or sign normalization. |
| Cache `00B74AA0` calls `00B738B0`, then subtracts width from each minimum and adds width to each maximum, with F32 stores `00B74AAB/B4/BD/C6`. | Direct bounds expansion, Confirmed; geometric units Unknown. | Preserve strict extrema selection and each cache arithmetic/store operation. Negative width can reverse bounds. |
| Query `00B74AD0..00B74B8D` compares both cached components with endpoint-order handling, returning +0 at `00B74CF0` when outside. | Inclusive finite cached box gate, Confirmed. | Query uses cached F32 values, with its separately selected profile. |
| `00B74B93..BA0` loads width, squares it, stores while retaining the result. Vertex loop `00B74BB0..BDE` processes all N points and stores each distance before comparing its unspilled value. | Retained minimum and conditional F32 reload, Confirmed. | No mathematical recomputation of a minimum after projection. |
| `00B74BE1` forms N-1, `00B74BEF` begins at the second point, loop `00B74BF4..C99` accesses the previous pair at -8/-4 and increments eight bytes. | Open consecutive traversal, Confirmed. | Exactly N-1 segments; a repeated first point creates closure only when explicitly serialized. |
| Delta stores `00B74C08` and `00B74C14` pop the first delta but retain the second; denominator `00B74C2B/2D` squares the retained second delta. `00B74C39` pops parameter to F32; both eligibility tests reload it. | Asymmetric delta lifetime, fully spilled endpoint eligibility, Confirmed. | Preserve the graph below; do not substitute BPOL projection rules. |
| `00B74C9F/CA8` rejects minimum >= stored width-square. `00B74CB6` stores threshold without popping, `00B74CB9` multiplies retained threshold by its stored copy; `00B74CC5` sends equality to feather. | Strict outer and inner distance gates, Confirmed. | No BPOL tolerance band or blanket edge inclusion rule. |
| `00B74CD5..CE2` computes square root, subtracts stored threshold, divides by width minus stored threshold, then subtracts from one; no final query F32 store. | Retained feather value, Confirmed. | Query inspection projection is not the Shape input. |
| Local caller `00B63F8E/FAF` gates enabled and calls query +24; table `00B642A0` dispatches unsigned selectors 0..3 and final `00B63FE0` stores shaped F32. | Shared caller shaping, Confirmed. | Unknown selector yields +0 after query. Disabled Evaluate skips unused geometry. |
| Destructor `00B74A70` tail-calls vector teardown `00B73740`, releasing storage then boundary/IHDR cleanup; deleting wrapper `00B74A40`. | Legacy owned vector/header lifecycle, Confirmed. | Replacement uses immutable managed snapshots and no external handles; managed cleanup timing differs. |

## Exact arithmetic and branches

`R_c` and `R_q` round each arithmetic instruction nearest-even to the chosen cache/query significand width with wide exponents. `F32` is a separate nearest-even store. `reload` denotes a value loaded from that store. Finite numeric equality does not repair signed-zero raw bits.

Shared extrema computation begins with minima `7F7FFFFF`, maxima `FF7FFFFF`. Iterate original point order and replace only strict smaller/larger components. Equal extrema keep first-selected bits. For each component, cache stores:

```text
minimumBits = F32(R_c(selectedMinimum - width))
maximumBits = F32(R_c(selectedMaximum + width))
```

No extra half-width conversion occurs. Empty input retains sentinel extrema before these operations. It is accepted only if all reached modern finite stores remain finite; the query is not given a special empty shortcut.

Outside either cached component interval, query returns +0. Endpoints are inclusive and reversed intervals are tested in either order. Inside, retain `minimum = R_q(width*width)` while storing `squareBits = F32(minimum)`. For every vertex `(vx,vy)`:

```text
dx = R_q(x-vx); dy = R_q(y-vy)
candidate = R_q(R_q(dy*dy) + R_q(dx*dx))
candidateBits = F32(candidate)
if candidate < minimum: minimum = reload(candidateBits)
```

Every reached candidate is stored, including candidates that are not selected. Compare the unspilled candidate; replace with its F32 reload only if strictly smaller. Do not algebraically reassociate squared distances or normalize ties.

For consecutive pair `(ax,ay)` to `(bx,by)`, for indices 1 through N-1:

```text
dxBits = F32(R_q(bx-ax)); dx = reload(dxBits)
dyRetained = R_q(by-ay)
dyBits = F32(dyRetained); dy = reload(dyBits)
dot = R_q(R_q(R_q(y-ay)*dy) + R_q(R_q(x-ax)*dx))
length = R_q(R_q(dyRetained*dyRetained) + R_q(dx*dx))
tBits = F32(R_q(dot/length)); t = reload(tBits)
```

The y contribution precedes the x contribution. Denominator uses retained dy but reloaded dx; numerator uses both reloaded deltas. Accept projection iff `0 <= t <= 1`, using stored t for both tests. For eligible projections:

```text
qx = R_q(R_q(dx*t)+ax); qy = R_q(R_q(dy*t)+ay)
```

Compute and select its squared distance with the vertex graph. Skip outside projections, without endpoint clamping; the vertex loop provides endpoint candidates. Zero-length consecutive segments can divide 0/0 when the box path is reached, even with zero width or zero feather. The modern evaluator rejects this undefined arithmetic. Original exception/NaN handling remains Unknown. Duplicate points are preserved and can complete if bounding rejection skips the segment loop.

After all vertices and open segments, if `minimum >= reload(squareBits)`, return +0. Otherwise let `f` be the raw amount:

```text
T = R_q(R_q(1-f)*width)
TBits = F32(T)
thresholdSquare = R_q(T*reload(TBits))
u = 1 if minimum < thresholdSquare
    otherwise R_q(1 - R_q(
      R_q(sqrt_Rq(minimum)-reload(TBits)) /
      R_q(width-reload(TBits))))
```

Inner comparison is strict; equality follows the feather formula. Threshold multiplication mixes retained and stored values. No final query store exists in the original graph. A receipt's F32 projection is inspection; its retained value feeds shaping. Neither raw width nor amount is clamped, and no universal output-range guarantee is imposed. Width zero reaches the outer distance rejection for ordinary exact-zero minima, without a special zero-width implementation branch. Narrow extreme cases follow the retained-versus-stored comparisons, not an inferred geometric shortcut.

Shape uses the query profile: unsigned selector 0 identity, 1 `R_q(u*u)`, 2 `sqrt_Rq(u)`, 3 ordered `R_q(R_q(R_q(3-R_q(u+u))*u)*u)`. Other values return +0 after query. Store the shaped result once to F32. Negative square-root input or undefined division is outside the modern finite domain; no legacy rejection is inferred.

## Owned public API and modern policy

`TerrainPolylineCache` owns a point snapshot, immutable raw metadata, expanded raw bounds and cache profile. `CopyPoints()` returns independent storage. `TerrainPolylineQuery` retains its cache and arithmetic value, exposes an inspection projection and immutable branch/squared-width/minimum/eligible-projection-count observations when reached. `Query` accepts fresh input plus both profiles or an existing cache plus query profile. `Shape` consumes a query receipt. `Evaluate` applies local enablement; `EvaluateBatch` snapshots supplied coordinate arrays and shares one owned cache across immutable query receipts. `CopyInfluenceBits()` returns independent storage. No user callback or engine layer participates.

Default limits: 8,192 points, 65,536 samples, 1,048,576 charged point visits. Hard maxima: 65,536 points, 1,048,576 samples, 67,108,864 visits. Cache charges N; cached Query charges 3N; input Query/Evaluate charges 4N; active nonempty batch charges N+3NS. These conservative modern charges include early exits and bound point-loop work, not measured original instruction counts. Per-batch result charges omit the shared cache charge. Point/sample/work checks complete before coordinate/cache/result snapshots. Approximate array payloads are 8N point bytes and 8S coordinate bytes, not total managed heap measurements. Raw decoder limits are independent.

Null arguments throw `ArgumentNullException`; invalid limit/profile ranges throw `ArgumentOutOfRangeException`; opaque input, undefined or unsupported arithmetic, nonfinite active values or observed stores/projections, and exceeded work throw `InvalidDataException`. Active cache requires finite points, raw amount and width. Disabled evaluation still checks known metadata/header, profiles and caps, but ignores unused nonfinite geometry, metadata floats and coordinates. Empty active batches create no cache and charge zero. Disabled batches also charge zero; point/sample caps still apply. Failed batch computation publishes no partial receipt and cannot mutate existing results. Temporary managed allocations do not imply timed destruction or deterministic wiping.

Serialized empty/minimal, collinear, repeated and self-crossing sequences are not certified as valid terrain geometry. Fresh evaluation follows directly recovered defined arithmetic only. Original PC/RC/exception controls, width units/full-versus-half meaning, axes, edited point lifecycle, layer composition and malformed legacy recovery remain open.

## Verification boundary

[Public native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainBoundaryPolylineInfluenceScenarios.cs) are compiled against the separate Core assembly. The [independent verifier](../../SWG-Godot-Client/poc/round41-tests/Assets/verify_polyline.py) uses Fraction/integer primitives with a separate cache/open-segment control-flow implementation, not native-generated expectations. It covers both raw versions, point order and explicit closure, endpoints, zero/negative/subnormal width and amount, all selectors, three query profiles and cross-phase profiles, degenerate/duplicate inputs, nonfinite/overflow rejection, disabled/empty paths, aliases, immutable ownership, late batch failure and exact/one-less budgets.

All 90,965 vectors agree: 83,221 accepted, 7,744 rejected. Forty-eight ownership/budget/identity checks and six null negatives pass. Static verification checks 83 new anchors and 22 finite status-mask cases; the accepted raw verifier separately rechecks 47 anchors. Query has zero calls. Tests distinguish 595 query-profile-sensitive inputs, zero cache-profile-sensitive inputs, 1,404 retained width-square witnesses, 48 retained-delta witnesses and 110 premature-query-projection shaping cases. These observations do not identify original active precision or prove general profile equivalence.

The explicit retained-delta witness uses points `(0,1),(0,33554432)`, supplied coordinate `(0,16777216)`, raw width 2 and amount 1. Under 53/64 query precision, dy retains 33554431 while its F32 copy is 33554432. Replacing the retained denominator delta by its stored copy changes the result; reversing the supplied point order is tested independently. This confirms why the operation/store distinction is an implementation requirement, not just documentation detail.

The separate runner and actual Core/default smoke builds have zero warnings/errors and execute with exit 0. One already-pinned six-point original BPLN record is decoded and compared. No original influence execution, arithmetic parity or broader corpus coverage is claimed. Final scope and link checks are recorded in the checkpoint.
