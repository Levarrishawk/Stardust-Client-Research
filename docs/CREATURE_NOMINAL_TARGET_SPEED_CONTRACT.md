# Supplied nominal target-speed construction

Date: 2026-10-04. This pure engine-neutral Session slice constructs a target F32 word from two supplied getter-return words, a supplied raw run flag, three supplied direction words, and an owned retained nominal factor. All arithmetic precision and resource policy are explicit. The values are caller-supplied; original getter execution, physical SI units, dynamic receiver lifetime, effective gameplay/global flag meaning and actual callback floating-point controls remain Unknown.

## Confirmed operation graph

The executable baseline is `C:/Stardust-DEV/Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The SHA-gated [closed speed-input verifier](../tools/python/verify_creature_controller_speed_inputs.py) was rerun as input evidence. Only its already closed player `00629750` target window `00629FAE..00629FF2` was read for exact arithmetic order. No new full function, helper body or caller inventory is introduced.

| Observation | Interpretation and confidence | Compatibility consequence |
| --- | --- | --- |
| `00629FAE/B1/B3` checks local raw byte; `00629FB5/B8/C0/C3` compares run F32 against zero and routes nonpositive/unordered to walk; `00629FC5/CA` loads run or walk. | Confirmed finite selection: run only when flag is nonzero and run > 0; otherwise walk. Gameplay meaning is Unknown. | Supplied flag is a byte. Finite negative run, signed-zero run, and zero flag select supplied walk without repair. Nonfinite inputs are unsupported by modern policy. |
| `00629FCD/D0` square local `[-8]`; `00629FD3/D6` square `[-C]`; `00629FD9` adds those retained squares; `00629FDB/DE` square `[-4]`; `00629FE1` adds that third square. | Confirmed grouping `(first^2 + second^2) + third^2`, with first/second/third in recovered load order. Axis labels are unproved here. | Round each square and each addition at explicit precision. Do not regroup or silently assign XYZ labels. |
| `00629FE3` applies `fsqrt`; `00629FE5` multiplies magnitude by selected speed; `00629FE7` performs F32 `fst` into controller `+154`. | Confirmed distinct retained magnitude, retained selected-speed product, and F32 snapshot. | Square-root rounding is an operation boundary. The snapshot does not replace its retained source. |
| `00629FED` multiplies the retained product by preceding factor; `00629FEF` spills target F32 to local `-14`. There is no `+154` reload. | Confirmed retained intermediate feeds factor multiplication. Factor computation/meaning remain outside the supplied API. | Factor must consume `RetainedIntermediate`, including values lost or rounded by the `+154` snapshot. |

The [historical speed-input contract](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md) supplies call/receiver context. The [conditional seconds carrier](TANGIBLE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md) and [nominal transition](CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md) are separate downstream boundaries. This component does not run getters or factor helpers and does not change their semantics.

## API and owned values

[Implementation](../poc/Stardust.Core/Session/CreatureNominalTargetSpeed.cs):

```csharp
CreatureNominalTargetSpeed.Evaluate(
    uint runBits, uint walkBits, byte runFlag,
    CreatureNominalTargetDirection direction,
    CreatureNominalTargetFactor factor,
    CreatureNominalSpeedPrecision precision,
    CreatureNominalTargetLimits? limits = null,
    CancellationToken cancellationToken = default)
```

`CreatureNominalTargetDirection(FirstBits, SecondBits, ThirdBits)` owns three F32 words in local `[-8], [-C], [-4]` load order. `CreatureNominalTargetFactor(ulong Magnitude, int Exponent, bool IsNegative=false)` owns signed `Magnitude * 2^Exponent`, including negative zero. It is a supplied nominal value, not an original factor getter or physical-unit promise. The frozen speed-specific `CreatureNominalSpeedPrecision` enum selects nearest-even 24, 53, or 64; no host `double` is used internally.

The immutable result exposes `Selection`, `SelectedSpeedBits`, `RunFlag`, copied `Direction`/`Factor`, `RetainedSquaredMagnitude`, `RetainedMagnitude`, `RetainedIntermediate`, `IntermediateBits`, `RetainedTarget`, `TargetBits`, `Precision`, `ReservedWork`, counts and supplied-input provenance. Retained values reuse the frozen speed-specific `CreatureNominalRetainedValue`: U64 magnitude, signed exponent and sign. Rounding carries exceeding U64 are reduced by an exact trailing power of two before export. No borrowed array, engine object, controller or callback is retained.

`IntermediateBits` is the candidate controller `+154` F32 snapshot; `TargetBits` is the later target F32. Counts are two **supplied getter words**, three direction words, one selected-speed use, one factor value use, eight graph arithmetic operations and two F32 stores. They do not count original getter invocations, primitive internals, CPU instructions or native controller events.

## Arithmetic and atomicity

1. Check cancellation, precision, caps, both finite getter words, all three finite direction words, and supplied factor width/exponent using primitive values before any BigInteger construction/shift. All inputs are validated even when a getter is unselected or a mathematical zero would bypass its influence.
2. Select run only for nonzero flag and strictly positive finite run; otherwise select walk. Negative finite walk/run values are supported as supplied.
3. Compute first square, second square, their sum, third square, then final sum, rounding nearest-even after every recovered operation at selected precision. The sum is nonnegative; signed-zero inputs square to positive zero.
4. Apply correctly rounded square root at selected precision, then multiply by selected speed at that precision. Retain each result. A wide finite magnitude is not projected to F32 prematurely: huge directions can validly produce zero intermediate when selected speed is zero.
5. Project the retained intermediate to nearest-even F32 for the `+154` snapshot. Reject a nonfinite snapshot before result publication, even if a supplied zero factor would later make target finite.
6. Multiply the **retained** intermediate by the supplied factor at selected precision, without reloading the snapshot. Project target to F32 nearest-even and reject nonfinite target before publication. Finite negative factors are accepted; no absolute-value or sign repair occurs.
7. Export owned retained values and return one immutable complete result after a final cancellation check. Errors/cancellation cannot alter any previous result or expose a partial `+154`/target publication.

The implementation privately reuses the frozen exact finite `TerrainCircleArithmetic.Number` multiply/add/square-root/projection APIs. That internal primitive name does not label the public factor as noise/fractal or introduce terrain dependencies into the caller-facing speed types. The square-root uses bounded integer Newton/isqrt logic with exact midpoint comparison; original x87 exponent/exception/control policy is not inferred from this nominal precision selection.

## Modern bounded policy

`CreatureNominalTargetLimits` defaults/hard ceilings: magnitude bits 64, absolute factor exponent 512, work 65536. Configurable caps must be in `0..64`, `0..512`, `0..65536`; supplied factor width and absolute exponent must fit. Unsupported precision, nonfinite raw words, cap violations and finite F32 overflow reject explicitly. Subnormal stores and signed underflow zeros are accepted. These are modern finite-slice policies, not recovered invalid-input/error-recovery behavior.

For primitive word inspections, width is the significand bit length (zero width for zero) and exponent is the finite F32 decoding exponent. Let low=min(2*direction exponent), high=max(2*direction exponent+2*direction width)+2. Let S be selected speed and F the supplied factor. Reserve:

`1024 + 16*(high-low + abs(S.exponent)+S.width + 2*abs(F.exponent)+F.width + precision + 128)`.

This conservative logical bit-work profile bounds alignment/carry, square-root precision and wide products/projections before BigInteger arithmetic. It is not CLR allocation bytes, an exact primitive operation count, or an original x87 range limit. `GetRequiredWork` exposes the same primitive-only validated preflight reservation. Zero directions/factors still follow the complete supported operation graph and retain truthful counts; they do not suppress input safety checks.

## Independent verification and reproduction

[Native scenarios](../poc/Stardust.Core.SmokeTests/CreatureNominalTargetSpeedScenarios.cs) run through the new [standalone runner](../poc/round57-tests/Runtime/Runtime.csproj); existing smoke/project/host entrypoints remain unchanged. [Fraction/isqrt oracle](../poc/round57-tests/Runtime/verify_target.py) reuses frozen independent dyadic arithmetic from56 and adds an independent square-root midpoint oracle: scale the exact radicand, integer-square-root its floor, then compare the exact squared half-step and break ties to even. It calls no C# arithmetic.

Generate: `verify_target.py generate --out FRESH_PRIVATE_CASES`. Native: `dotnet Runtime.dll CASES NATIVE_OUTPUT`. Verify: `verify_target.py verify --cases CASES --native NATIVE_OUTPUT --receipt FRESH_PRIVATE_RECEIPT`. Generator/verifier outputs require fresh ignored private paths. Restore/build uses installed offline .NET 8, cached packages, clear-source [NuGet configuration](../poc/round57-tests/Runtime/NuGet.Config) and private APPDATA. No download is needed.

All **1,032 synthetic cases** agree at precisions24/53/64:742 positive,289 input rejects,one cancellation. Eighty-seven cases distinguish retained factor multiplication from a `+154` reload; twenty-two distinguish the recovered squared-sum grouping. These include finite underflow snapshots whose retained value is later amplified by a factor, without claiming such a factor occurs originally. Three exact/three one-below work/magnitude/exponent cap cases pass. Coverage includes flags0/1/255, positive/nonpositive run selection, finite negative getters/factors, zero directions, signed zeros, subnormal/large directions, intermediate/target overflow, U64 factors, nonfinite unselected inputs and unsupported test-fixture flag/vector shapes.

Each of742 positive native cases mutates its source direction array and local value copies after evaluation and confirms result isolation.100 repeats,64 parallel calls, readonly result properties and prior-result preservation across every success/reject/cancel pass. Build/default smoke/workspace/link/text/hash receipts are retained in the handoff. Test fixtures are explicitly synthetic, not original numeric asset/getter corpora or executable runs.

## Implementation consequence and next boundary

Root may join `TargetBits` into the frozen nominal transition and then its existing motion driver after independent review. This worker adds no host/transition edits, target getter invocation, global/gameplay runflag interpretation, physical acceleration, later speed-zero/vector/root-motion arbitration, predicted simulation or original parity claim. Effective original arithmetic controls, factor/helper units, vehicle/template state and controller lifetime remain Unknown.

The next separately bounded static probe remains one selected `00AA6670` queue-update path for held-controller invalidation before `00B3D1D6`. Before a physical target/speed model, separately close selected receiver/factor/getter semantics and active arithmetic policy. Do not infer these from supplied-input arithmetic agreement.
