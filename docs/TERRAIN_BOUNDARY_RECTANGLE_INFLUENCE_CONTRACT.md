# Fresh rectangle boundary influence

[TerrainBoundaryRectangleInfluence](../poc/Stardust.Core/Geometry/TerrainBoundaryRectangleInfluence.cs) evaluates one supplied coordinate pair against a fresh decoded [BREC input](TERRAIN_BOUNDARY_RECTANGLE_INPUT_CONTRACT.md). Cache and query/shaping precision are mandatory, separate nearest-even 24/53/64 profiles. The original active profiles remain Unknown. The evaluator covers constructor-default **untransformed** state; it does not evaluate arbitrary edited/transformed rectangles, combine layers/filters, assign axes or generate terrain. The [Round 39 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_RECTANGLE_INFLUENCE.md) records reproduction and input pins.

## Direct evidence and supported state

Addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, verified before static reads. No original executable run or server analogy supplies the arithmetic claim.

| Observation | Interpretation / confidence | Consequence |
| --- | --- | --- |
| Constructor `00B72960` clears outer +1C/+20/+24/+28, inner +2C/+30/+34/+38 and transform-enabled +3C; +40/+50 receive 1 and remaining matrix words zero. Base `00B63333/36` clears selector/amount. | Fresh transform disabled and metadata defaults, Confirmed. | Absent version-0/1 amount/selector resolves to runtime positive zero; nullable serialized fields remain intact. |
| Loaders `00B73200`, `00B732A0`, `00B73350`, `00B73460` write corners/metadata, not +3C or matrix +40..+54. Known dispatch paths call `00B72AC0`. | Loading does not enable transform; final normalization/cache runs, Confirmed. | Fresh raw input suffices for this domain. Later edit state is not inferred. |
| `00B72FE0` and `00B73000` set +3C and invoke matrix helpers; query `00B72BC0` calls `00ABB330` on +40 only when +3C is set. | Runtime transform methods affect supplied coordinates, Confirmed; producer/caller semantics open. | No transform API or assumed matrix semantics here. |
| Version-2/3 loaders compare amount with 0/1 and pass clamped finite bits through slot +10. `00B72AA0` stores via `00B65FC0`, then calls `00B72AC0`. | Fresh loading clamps amount; editing setter itself does not clamp, Confirmed. | Clamp finite serialized amount once for fresh evaluation. Negative input becomes +0; above 1 becomes 1; equal negative zero survives. |
| `00B72AC0` swaps endpoints only when ordered greater, then spills first extent at `00B72AFE` and second at `00B72B07`, comparing the still-unspilled second at `00B72B0A`. | Normalization and exact extent selection, Confirmed. | Preserve equal signed-zero endpoint order and F32 stores. |
| `00B72B21/28` multiply amount by selected F32 extent, then 0.5; `00B72B47/4E/56/5E` store four inset corners. | Cache uses the smaller extent and half feather amount, Confirmed. | Each arithmetic operation and corner store uses explicit cache policy. |
| Outer comparisons `00B72BF8..C8F`, zero-amount branch `00B72C9E..CB8`, inner comparisons `00B72CBB..E37`. | Inclusive outer/inner tests with endpoint-order handling, Confirmed for finite values. | Outside returns +0 first; zero amount returns 1; inner returns 1. |
| `00B72D27/30/39/42` store four edge distances to F32; `00B72D4B/54` store extents; `00B72D6A/6C` recompute feather inset. | Query has its own arithmetic/spill graph, Confirmed. | Reload cached bounds; use query profile for distances and denominator, not cache profile. |
| `00B72D77..DB5` selects the strictly smaller distance from an initial denominator; `00B72DB8` divides edge by denominator without an influence store. | Retained query value, Confirmed. | Projection is inspection only; shaping consumes the unspilled value. |
| Layer `00B63F8E/FAF` gates local enabled then queries +24. Table `00B642A0` selects identity/square/sqrt/ordered polynomial; larger unsigned selector gives +0. `00B63FE0` stores shaped F32. | Shared caller shaping, Confirmed. | Separate direct Query/Shape from locally gated Evaluate. No ancestor/filter/product behavior is included. |

Extension flags/scalars/name do not participate in this recovered query or cache. They remain in the raw input record with unresolved feature semantics. Synthetic version-3 fixtures deliberately include a nonfinite extension scalar to verify that it does not become a geometry eligibility rule.

## Exact conditional arithmetic

Let `R_c` and `R_q` round each arithmetic instruction nearest-even to the specified cache/query significand width. Intermediates use wide exponents. `F32` denotes an additional nearest-even storage operation. Comparisons are numeric finite comparisons; equal values retain the original operand's bits where selection does not replace it.

Normalize raw endpoints component-wise by swapping only if A > B. Call them `(a,b,c,d)`, with `(a,b)` the lower outer corner. Resolve finite amount `f` from fresh loading as above. In either phase, the extent helper is:

```text
W1 = F32(R(c-a))
U2 = R(d-b)
W2 = F32(U2)
M = W2 if U2 < reload(W1), otherwise W1
T = R(R(f * reload(M)) * 0.5)
```

The comparison uses `U2` before its F32 projection, while the selected value is reloaded from F32. This is not an algebraically reassociated minimum-width formula.

Cache phase stores inner corners:

```text
ia = F32(R_c(T_c+a))
ib = F32(R_c(T_c+b))
ic = F32(R_c(c-T_c))
id = F32(R_c(d-T_c))
```

Query receives finite `(x,y)` and reloads those bounds. Both bounds tests are inclusive and account for endpoint order, including a degenerate/equal pair. Branch priority is:

1. Outside outer bounds: `u = +0`, branch Outside.
2. Runtime amount numerically zero: `u = 1`, branch ZeroFeather.
3. Inside inner bounds: `u = 1`, branch Inner.
4. Otherwise compute the feather query:

```text
D1 = F32(R_q(x-a))
D2 = F32(R_q(c-x))
D3 = F32(R_q(y-b))
D4 = F32(R_q(d-y))
T_q = query-phase extent helper above
E = T_q
for D in [D1,D2,D3,D4]: replace E only if reload(D) < E
u = R_q(E / T_q)
```

`u` is retained internally. `QueryProjectionBits` is its F32 inspection projection. Shape is exactly: selector 0 uses `u`; 1 computes `R_q(u*u)`; 2 computes correctly rounded square root at query precision; 3 computes `R_q(R_q(R_q(3-R_q(u+u))*u)*u)`; other U32 selectors use positive zero. Only the final shaped value is stored to F32. There is no additional output clamp.

Finite inverted raw corners normalize; zero-width/height and point rectangles can return 1 at their inclusive location. Zero amount returns 1 throughout the outer rectangle, including its edge. With positive feather, outer-edge distances can give zero; cached inner edges give 1. Negative serialized amount follows the fresh loader's clamp to +0, rather than emulating the editing setter's unclamped state. Out-of-range positive serialized amounts clamp to 1. Signed zeros are preserved by the exact arithmetic and equal-value selection rules.

## API, ownership and modern restrictions

`Cache(input, cachePrecision)` publishes immutable raw fields, normalized outer bounds, F32 inner bounds, runtime amount, extent stores and profile. `Query` accepts either fresh input with both profiles or an existing owned cache with query profile. `Shape` consumes an owned query and explicit selector. `Evaluate` uses the decoded local enabled flag and serialized/default selector. Disabled Evaluate returns an explicit skipped receipt without validating otherwise-unused geometry, amount or coordinate finiteness. Both profiles and decoded input/header validity are still checked. Direct Cache/Query is active regardless of local enabled.

The supported active domain requires finite geometry, amount and supplied coordinates, finite F32 cache/distance/extent/output stores and defined finite division/square-root behavior. Unsupported nonfinite input, overflowing stores, zero division or negative square root throw `InvalidDataException`; invalid precision throws `ArgumentOutOfRangeException`; null input/cache/query throws `ArgumentNullException`. These are modern restrictions, not recovered original exception/trap policy. The raw decoder continues to accept and preserve all serialized bits.

`EvaluateBatch` requires equal coordinate lengths and checks the sample cap before snapshots/results. Default maximum is 65,536; explicit allowed range is 0..1,048,576. It owns both input snapshots, retains immutable receipts and exposes a read-only result collection with independent influence-bit copies. Publication occurs only after the complete batch succeeds; later failure cannot change earlier results. Aliased coordinate inputs are permitted and unchanged. No external resources or callbacks are owned. The cap is a sample-count budget, not a managed-heap byte measurement.

## Verification and limits of the claim

[Independent Fraction/integer oracle](../poc/round39-tests/Assets/verify_rectangle.py) and [public Core scenarios](../poc/Stardust.Core.SmokeTests/TerrainBoundaryRectangleInfluenceScenarios.cs) agree on 10,244 vectors: 9,285 accepted and 959 rejected, 32 ownership/budget/identity checks and six null negatives. The oracle independently demonstrates 300 query-profile-sensitive inputs, 456 cache-profile-sensitive inputs and 414 cases where spilling before Shape changes output. Static verification includes 73 arithmetic anchors, four constants, selector table and transform/layout assertions; the frozen 46 raw-input anchors are separately rechecked. The actual separate Core/default smoke-test build and execution pass.

One real raw occurrence is compared by identity and ordered words from the same pinned Tatooine set of 29 rectangles: first BREC at TGEN offset 18,468, layer 11, child 2, version `0003`, IHDR `0001`. No influence is computed for original terrain records, and no original influence parity claim follows from these conditional synthetic results.

Original active PC/RC/exception policies, transformed/editing state semantics, axes/units, full layer/filter/ancestor/inversion behavior and complete generated terrain remain open. Compatibility requires preserving the proved phase graph and stores under whichever policies are established later. The next probe is the two transform-enabling methods and their callers, with a separate state/coordinate contract before extending this API.
