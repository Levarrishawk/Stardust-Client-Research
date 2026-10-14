# Bounded nominal speed transition

Date: 2026-10-04. This engine-neutral Session component implements one explicit nominal transition from supplied current/target F32 words, caller seconds, and an optional owned supplied retained rate. It is a pure, bounded arithmetic slice. Supplied rates and test trajectories are synthetic caller policy; physical SI units, effective getter/template state, vehicle behavior, active original floating-point controls and full locomotion remain Unknown.

## Evidence boundary

[Accepted speed-input evidence](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md) identifies player update `00629750`, ordered target/current selection, rate helper calls at `0062A009/0062A03E`, multiplication by incoming argument at `0062A00E/0062A045`, current addition at `0062A011/0062A048`, and F32 stores retaining the comparison operand at `0062A014/0062A04B`. Ordered overshoot/undershoot copies raw target words at `0062A024/0062A05B`. Equality bypasses those rate paths; unordered raw inputs are rejected by the modern API rather than modeled as original exceptional execution.

The [accepted carrier closure](TANGIBLE_CONTROLLER_UPDATE_CARRIER_CONTRACT.md) supports incoming seconds only under its explicit scheduler/current-receiver/type/flag/normal-ABI conditions. The [helper contract](CREATURE_ACCELERATION_HELPER_CONTRACT.md) and [getter contract](CREATURE_ACCELERATION_TEMPLATE_GETTER_CONTRACT.md) remain separate from this supplied nominal rate. This implementation invokes neither original helper nor getter.

Observation: the closed rise comparison uses `fcomp`, `test AH,41`, and a branch that bypasses raw-target replacement for candidate <= target; the closed fall comparison uses `test AH,05`/parity to bypass replacement unless finite candidate < target. Interpretation: **strict** retained overshoot/undershoot comparison is required. Confidence: Confirmed instruction graph for finite ordered operands. Consequence: narrowing before comparison must not replace the retained operand. The closed50/55 producers were rerun as input-contract checks; only a small comparison witness within the already closed body was inspected, with no new body or caller inventory.

Executable baseline: `C:/Stardust-DEV/Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Original execution, live contact and proprietary fixture publication are outside this work.

## API and ownership

[Implementation](../../SWG-Godot-Client/poc/Stardust.Core/Session/CreatureNominalSpeedTransition.cs):

```csharp
CreatureNominalSpeedTransition.Evaluate(
    uint currentBits, uint targetBits, uint secondsBits,
    CreatureNominalSpeedPrecision precision,
    CreatureNominalRate? suppliedRate = null,
    CreatureNominalSpeedLimits? limits = null,
    CancellationToken cancellationToken = default)
```

`CreatureNominalSpeedPrecision` explicitly selects `NearestEven24`, `NearestEven53`, or `NearestEven64`. `CreatureNominalRate(ulong Magnitude, int Exponent, bool IsNegative=false)` is an owned value encoding signed `Magnitude * 2^Exponent`; sign also represents negative zero. No public speed type labels that value as noise or fractal. Internally, the component reuses the accepted finite `TerrainCircleArithmetic.Number` primitive for multiply, add, compare and F32 projection; it does not silently use host `double`.

The immutable result exposes `StoredBits`, `CandidateBits`, `RetainedCandidate`, optional `RetainedStep`, `Branch`, `Clamped`, `Precision`, `ReservedWork`, optional used `SuppliedRate`, `RatesUsed`, and `RateProvenance`. Retained values own U64 magnitude, signed exponent and sign; `NegativeZero` is explicit. An exact carry of `2^64` is reduced by a trailing power of two before U64 export without changing its value. `CandidateBits` is the pre-clamp F32 store; `StoredBits` is the eventual candidate or raw supplied target. Input current/target words are never mutated.

`GetRequiredWork` uses the same primitive-only validation/reservation plan and can supply an exact work cap before evaluating. It performs no BigInteger arithmetic. It still validates all supplied inputs/options, including any supplied rate even when unused. No mutable controller, borrowed buffer, queue, callback, cache, vector, or engine adapter is accepted or retained.

## Operation order

1. Cancellation and the finite raw-word/options/rate profile are checked before BigInteger construction or shifting. Numeric comparison of the two finite F32 words uses integer encoding order, treating both zero signs as equal.
2. Equality returns the **original current bits**, including signed zero. Optional rate may be absent; a supplied rate is safety-validated but unused. `RatesUsed=0`, provenance `NotUsed`, no retained step and zero work reservation. There is no claim of helper invocation.
3. Ordered rise/fall requires exactly one supplied rate. Fall negates that signed rate before multiplication. Finite negative seconds and finite negative supplied rates are accepted as supplied; neither is clamped, absolutized, or repaired.
4. Multiply signed rate by caller seconds, rounding nearest-even to selected precision. Add exact current F32 value, again rounding nearest-even to selected precision. Both retained values remain distinct from F32 storage.
5. Narrow the retained candidate to F32 nearest-even. Reject a nonfinite candidate projection before publishing a result, **even if an ensuing target clamp could produce finite output**. Finite subnormal results and signed underflow zero are supported.
6. Compare retained candidate numerically against the exact stored-F32 target. Rise clamps strictly when retained candidate > target; fall clamps strictly when retained candidate < target. Equality at this stage does not copy a differently signed raw zero target. A clamp copies raw target bits.
7. Return one immutable result after the final cancellation check. Errors/cancellation publish no result and cannot change a prior result.

This graph models the immediate nominal speed store/clamp only. Later original `+64` zeroing, target construction, selected object/helper arbitration, vector writes, root motion, collision, reconciliation, predicted simulation, and physical acceleration are excluded from the API.

## Bounded modern policy

`CreatureNominalSpeedLimits` defaults to magnitude width 64, absolute rate exponent 512, and work 8192. Configurable caps must be within `0..64`, `0..512`, and `0..8192`; all supplied rate widths/exponents must fit. Binary32 current, target and seconds must be finite, even on an equality branch. Invalid precision, nonfinite input, absent ordered rate, profile violation, finite candidate overflow and cancellation reject explicitly. These are **modern supported/rejection policies**, not claims about original invalid-input recovery or ambient x87 exception state.

For ordered inputs, let C/T/S denote primitive F32 magnitude widths and exponents, and R the supplied rate. Product exponent P=R.exponent+S.exponent. Reserve:

- low = min(C.exponent, T.exponent, P)
- high = max(C.exponent+C.width, T.exponent+T.width, P+R.width+S.width+2)
- work = 64 + 8*(high-low+2)

This conservative logical bit-work reservation covers operand alignment, product/add/carry, comparison and narrowing within the hard exponent/width profile. It is not a measurement of CLR bytes, exact BigInteger instruction count, helper-call count, or native x87 exponent range. It is computed and cap-checked before all BigInteger operations. Equality takes the explicit zero-work path without BigInteger construction. `RatesUsed` counts synthetic supplied rates consumed, not recovered runtime helper events.

## Validation and reproduction

[Standalone scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureNominalSpeedTransitionScenarios.cs) are invoked by [the dedicated runner](../../SWG-Godot-Client/poc/round56-tests/Runtime/Runtime.csproj), without editing the existing smoke or host entrypoint. [Independent Fraction oracle](../../SWG-Godot-Client/poc/round56-tests/Runtime/verify_transition.py) owns its dyadic nearest-even, F32 projection, signed-zero and retained comparison model; it does not call the C# arithmetic implementation.

Generate with `verify_transition.py generate --out FRESH_PRIVATE_CASES`, run the dedicated DLL with `CASES NATIVE_OUTPUT`, then verify with `verify_transition.py verify --cases CASES --native NATIVE_OUTPUT --receipt FRESH_PRIVATE_RECEIPT`. Generator and verifier outputs require fresh ignored private destinations. Build/restore uses installed offline .NET 8, existing cached packages, the runner's clear-source [NuGet configuration](../../SWG-Godot-Client/poc/round56-tests/Runtime/NuGet.Config), and isolated private APPDATA. No package download is required. Source projects and existing entrypoints remain unchanged apart from the two new allowlisted classes automatically compiled by their SDK projects.

2,041 synthetic cases match at three precisions: 1,896 positive, 144 input rejects, one cancellation. Thirteen retained-versus-F32 clamp witnesses pass, plus three exact/three one-below profile cases, signed zeros, negative rates/seconds, overshoot/undershoot, subnormal/overflow, U64 carry and missing/nonfinite/invalid options. Native checks cover 100 repetitions, 64 parallel calls, readonly result properties and preservation of a prior result across every success/failure/cancellation. The synthetic 60-step caller-rate32 route with seconds word `3C23D70A` is included without a physical getter or legacy movement-parity claim.

The initial restore encountered an inaccessible roaming NuGet configuration; a subsequent no-restore build consequently lacked assets. Isolated private APPDATA and the clear-source configuration corrected that environment issue. Failed and corrected logs are retained; successful standalone/default smoke builds have zero warnings/errors. Workspace links and exact frozen-relative source hashes are recorded in the handoff.

## Next implementation gate

Lead host integration may precompute an entire explicit synthetic transition plan and then feed resulting speeds into its existing modern motion driver, under separately owned host scope. Before claiming recovered physical acceleration, close effective helper/current-speed units, template/vehicle and receiver lifetime conditions, active callback arithmetic/exception policy, and later store/vector/root-motion arbitration. The next bounded static probe remains one selected queue update `00AA6670` path for held-controller invalidation before `00B3D1D6`; no such investigation is silently added here.
