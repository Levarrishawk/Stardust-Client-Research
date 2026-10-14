# Terrain BCIR circle influence

TerrainBoundaryCircleInfluence (external historical reference) evaluates one supplied coordinate pair against a decoded [BCIR input](TERRAIN_BOUNDARY_CIRCLE_INPUT_CONTRACT.md), then separately applies the layer caller's selector shaping. Cache/loading precision and query/shaping precision are **two mandatory explicit inputs**. Neither is an inferred original runtime default. The bounded evaluator does not assign world axes, activate ancestors, combine boundaries/filters, or generate original terrain.

## Evidence and phase boundary

Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are VAs. No original executable execution or server formula supplies the arithmetic claim.

| Direct observation | Confidence and interpretation | Compatibility consequence |
| --- | --- | --- |
| Constructor `00B72330` clears geometry/cache and calls base `00B63320`; stores `00B63333/36` clear selector+14/amount+18. | Confirmed fresh-object defaults. | Absent0000/0001 metadata resolves to runtime positive-zero selector/amount while nullable serialized fields remain preserved. |
| Loaders0000/0001/0002 multiply stored radius by itself at `00B726D2`, `00B7277F`, `00B72822`, then store F32 cache+28 at `00B726DB`, `00B72788`, `00B7282A`. | Confirmed cache graph and F32 phase store. | Compute cache with supplied **cache/loading precision**, then reload its F32 value for the later query. |
| Serialized radius is stored directly. Separate editing setter `00B723C0` uses `fabs` at `00B723CC`. | Confirmed loading/editing distinction. | No absolute-value conversion or editing operation belongs in this fresh-input evaluator. Negative finite radius bits remain intact. |
| Version0002's ordered comparisons at `00B7284F..81` clamp amount below0/above1; vtable+10 resolves to plain amount store `00B65FC6`. | Confirmed finite clamp and field ownership. | Clamp finite amount once into runtime amount bits; retain the serialized bits independently. Equal negative zero survives. Nonfinite behavior is unsupported here. |
| Query `00B72430` subtracts the supplied first/second pair from center+1C/+20, squares/sums, and spills F32 squared distance at `00B72449`. | Confirmed arithmetic order/storage. | Preserve this spill before comparisons; use supplied query precision for preceding arithmetic. |
| Comparisons/branches at `00B72453..81` reject strictly greater than cached radius square, then accept less-than/equal to unspilled inner radius square. | Confirmed finite branch priority. | Preserve outer comparison first, including equality and degenerate cases. |
| Query remainder `00B7248F..A4` performs two differences, division, then one minus quotient. There is no query `fsqrt` or F32 influence store. | Confirmed graph and unspilled return. | Retain the query value for shaping; an inspection projection must not become the caller input. |
| Caller `00B63F8E/95` checks local enabled, `00B63FAF` calls query virtual+24, then selector table `00B642A0` handles0..3; larger U32 values select positive zero. | Confirmed local gate and caller shaping. | Keep direct Query and gated Evaluate separate; preserve unsigned selector dispatch. |
| `00B63FC4` uses `fsqrt`; `00B63FC8..D4` doubles u, subtracts from3, multiplies by u, then by u again. `00B63FE0` stores shaped F32. | Confirmed exact selector order. | Implement rounded square root and polynomial order; no reassociation or premature spill. |

[Runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md) closes the worker/CRT route but leaves its active control word unresolved. Radius cache is created during loading, while `0084A530 ->00B65400 ->00B65280 ->00B63D20` can query on the chunk worker; a synchronous builder path is separate. This evidence does **not** establish equal control words across loading and query phases. Query itself has no calls or local control-word change, and the caller's shaping follows directly, so the software model uses one explicitly supplied policy for that uninterrupted query/shaping graph. Actual PC/RC/exception state in either phase remains Unknown.

## Exact finite graph

Let `R_c` and `R_q` mean nearest-even rounding after one arithmetic instruction to the supplied cache or query significand width24/53/64, with wide intermediate exponents. `F32` means the separate nearest-even storage operation. Let radius be `r`, runtime-clamped feather amount `f`, center pair `(c1,c2)` and supplied pair `(x1,x2)`.

```text
cache = F32(R_c(r*r))
dx = R_q(c1-x1)
dy = R_q(c2-x2)
D = F32(R_q(R_q(dy*dy)+R_q(dx*dx)))
if D > cache: u = +0
else:
  inner = R_q(R_q(1-f)*r)
  I = R_q(inner*inner)             # no F32 spill
  if D <= I: u = 1
  else:
    numerator = R_q(D-I)
    denominator = R_q(cache-I)
    u = R_q(1-R_q(numerator/denominator))
```

For finite supported operands, the feather branch implies `cache>I`; its denominator is positive with these wide exponents. No clamp, epsilon, endpoint shortcut or Euclidean-distance square root is introduced. Negative radius follows the actual multiply/square graph. Radius zero returns one when **stored** D is zero, including distances that underflow at the F32 spill, and zero otherwise. With f=0 the inner square and cached square can differ because only the latter was stored to F32; do not replace the observed comparisons with a guessed hard-edge shortcut.

Caller shaping uses the retained u:

| Selector U32 | Ordered result before caller F32 store |
| ---: | --- |
| 0 | u |
| 1 | `R_q(u*u)` |
| 2 | `R_q(sqrt(u))` |
| 3 | `R_q(R_q(R_q(3-R_q(u+u))*u)*u)` |
| other | positive zero, after the geometry query |

The polynomial is mathematically smoothstep, but its recovered instruction order matters. The API returns the F32 result stored by the caller, not the later layer maximum/inversion/filter composition.

A concrete cross-phase counterexample is synthetic radius/first-coordinate bits `1F3C0AAB`, centers and second coordinate positive zero, amount `3F000000`, selector0 and query precision53. Cache precision24 produces cache `001143F6`, distance `001143F5` and influence `359E285B`. Cache precision53 or64 produces cache/distance `001143F5` and influence positive zero. The test set contains96 groups where cache phase precision changes the final result. A shared precision default would hide this distinction.

## API, ownership and modern eligibility

`Query(input, firstBits, secondBits, cachePrecision, precision)` ignores the local enable flag, as the legacy query does, and returns immutable `TerrainCircleQuery`. It retains raw coordinates/fields, both precision choices, clamped runtime amount, cache/distance bits, branch and an internal unspilled number. `QueryProjectionBits` is **only** an F32 inspection projection. `Shape(query, selectorBits)` uses the internal value and an independently supplied caller selector. No public mutable numeric state escapes.

`Evaluate` checks the decoded header and local enabled flag before numeric eligibility. Disabled receipts preserve raw coordinate/flag/selector bits and return null Query/influence, meaning **skipped**, not invented zero or one. Direct Query still evaluates a disabled input when explicitly called. Enabled Evaluate uses decoded selector, or the confirmed zero base default when absent. Unknown/opaque input is rejected, including empty batches. Precision choices are validated even for skipped or empty work. The existing `TerrainHeightArithmeticPrecision` enum is reused without changing its source.

**Explicit modern restrictions:** active geometry, coordinates and serialized amount must be finite. Thus NaNs and infinities reject, even where a masked legacy comparison might propagate/skip or clamp infinity. Infinite F32 radius-square cache or squared-distance spill also rejects before returning a result. A masked legacy distance overflow could instead reach an exterior-zero branch; trap/flag behavior remains unknown and is not emulated. Finite negative radius, finite amount outside0..1, subnormals, signed zero and degenerate radius are supported. There is no claim for nonfinite propagation, directed rounding, exception traps, diagnostic hooks, altered editing caches or original generated coordinates.

`EvaluateBatch` requires equal coordinate-span lengths. Modern default cap65536, hard maximum1048576; cap0 permits an empty supported batch. Invalid precision/cap throws `ArgumentOutOfRangeException`; shape/cap/opaque/numeric failures throw `InvalidDataException`; null inputs throw `ArgumentNullException`. Shape/caps and decoded metadata precede snapshots. Both coordinate inputs are copied; aliasing is supported; every candidate is computed before returning an owned read-only collection. Copy accessors return independent arrays. Later failure leaves earlier receipts, input model and caller spans unchanged.

Arithmetic uses immutable dyadic `BigInteger` values and bounded exact quotient rounding in TerrainCircleArithmetic (external historical reference). Square root scales to the target quantum, computes an integer floor root with a32-iteration guard, and compares the exact squared midpoint to select nearest-even. The scaled root operand has at most128 significant integer bits at precision64; the guard bounds work. No double approximation, FMA, epsilon or repeated convergence on caller-controlled tolerance is used. This is a precise conditional software arithmetic model, not a hardware-runtime parity observation. Logical sample/work bounds do not measure CLR heap/RSS or caller-retained input/batches.

## Verification and resume

Runner (external historical reference) builds linked sources offline, disables shared compilation and apphost, and runs the DLL with cached .NET8. Independent oracle (external historical reference) reuses accepted Fraction rounding helpers, adds independent `isqrt`/midpoint square-root quantization and models the exact graph. Synthetic fixtures contain no original asset bytes.

```powershell
.\poc\round36-tests\Rendering\Invoke-Circle.ps1 -Exe C:\Stardust-DEV\Stardust.exe -Packages <cached-static-analysis-packages> -Python <python.exe> -Dotnet <dotnet.exe>
```

Results:46 static anchors, four selector targets, three constants, exact query spill/no-sqrt check;15,922 cases (14,820 accepted/1,102 rejected);3,069 exact square-root vectors including all finite exponent bands, signed zero and nearest-even ties;19 batch/ownership/cap/opaque checks and four null negatives. The fixtures expose228 query-precision-sensitive groups,96 cache-phase-sensitive groups and348 premature-query-spill-sensitive cases. Full native outputs match the rational oracle; build has zero warnings/errors. Modified executable identity and one-byte truncation reject before decoding. Workspace links and scope validation are recorded in the checkpoint/handoff.

Next: recover one supplied-coordinate producer or a bounded layer boundary-combination stage with its own exact storage/rounding points. Retain the separate explicit cache and query policies until both runtime phase control words are established. No new original corpus evaluation, GPU work, full-layer semantics or global coverage claim follows from this kernel.

Integration repair: the shared SmokeTests scenario uses only public Core APIs and returns its receipt. Exact square-root checks remain in `CircleSquareRootChecks.cs` within the source-linked Rendering runner, preserving the internal production arithmetic helper. The runner composes the unchanged oracle receipt. Both the standalone runner and the actual Core/SmokeTests project-reference build pass with zero warnings or errors; all vector counts are unchanged.
