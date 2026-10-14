# Bounded modern second-curve terrain sampling

The [second-curve helper and batch](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainSecondCurveFractalBatch.cs)
admit second-enabled MFRC/0001 families through an explicit
`ModernBinary64LogPowWithF32Exponent` policy. Every receipt reports
`OriginalNumericParity=false`. The [height traversal](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/OriginalTerrainHeightTraversal.cs)
selects this batch before its existing first-only or curves-disabled paths.
Existing first-only sampling semantics remain those in the
[first-curve contract](TERRAIN_FIRST_CURVE_FRACTAL_BATCH_CONTRACT.md).

## Evidence and confidence

The target authority is the SHA-pinned Stardust executable and original asset
bytes, as recorded in the [curve-finish research](TERRAIN_FRACTAL_CURVE_FINISH_CONTRACT.md).
For executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`,
sampler `013E7C00` tests the second flag at `013E7FC3` after the first curve.
The low/high decisions use retained values and exact binary64 constants
`3F50624DD2F1A9FC` and `3FEFF7CED916872B`; equality remains in the middle.
The half decision also uses the retained value, with equality selecting the
upper branch. Power bases reload the float32 snapshot. The exponent is
necessarily stored/reloaded float32 at `013E8025`, whereas the first curve
retains its exponent. These instructions and storage distinctions are
**Confirmed**. The two-sided gain interpretation is **Strongly inferred**;
original logarithm, power, control-word and exceptional-path parity is **Unknown**.

Supporting NGE source, pinned at
`949451032647e45e42c3aaef3f41b132c8af36e3`, agrees in
`src/engine/shared/library/sharedFractal/src/shared/MultiFractal.cpp`:
`NG_gain` at lines516–529 and `getValue2` at lines736–753 identify the gain
branches and bias-before-gain order. Source is correlation evidence; its
float variables do not replace the executable's retained/F32 distinctions.

The effective original `terrain/tatooine.trn` is1,188,446 bytes, SHA-256
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
Its generator starts at210, is139,608 bytes and hashes to
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
Independent raw framing identifies five second-enabled families:5 and29
enable both curves;36,46 and60 enable only the second. Their second parameters
all lie strictly between0 and1. These byte observations are **Confirmed**.
Family60 MFRC begins at TGEN-relative12151, is84 bytes and hashes to
`AD0AC6BA362C4A3E9E761DE1A110AFF594D6D6FE7CE9502E509190E182D88EE8`.
It is consumed by layer119, an inverted-circle RollingHills layer, through
AHFR at78356. The prior district's exclusion of this layer does not establish
its exclusion elsewhere; general regions must evaluate it when applicable.

## Implemented boundary

`TerrainSecondCurveFractalBatch.Build` requires the explicit policy, controls
24/53/64, exact MFRC/0001 framing, a nonzero full-U32 second flag, and a complete
unique finite row-major rectangle of at most256 cells. It clones the source
and clears only the second flag word at encoded bytes44–47. The derived
source's other bytes remain unchanged. Receipts expose immutable cells and
copy-only encoded accessors. Cancellation precedes allocation/evaluation and
final publication; temporary kernel ownership uses `using`.

For second-only inputs the existing exact conditional sampler produces fresh
retained misses. For both flags the unchanged first-curve batch runs on the
derived source first; its exact dyadic modern output becomes the second
curve's retained input. There is no copied noise engine or curved cache.

`TerrainSecondCurveApproximation.Evaluate` compares the exact input dyadic
against the recovered binary64 low/high constants and half, preserving
strict inequalities. Low/high branches return exact0/1. The middle computes
the explicit modern initialized denominator `F32(Math.Log(0.5))`, then
`F32(Math.Log(1-c)/denominator)` and reloads that exponent for `Math.Pow`.
The base uses the input's float32 projection: `2*u32` for the lower branch,
`2*(1-u32)` for the upper. Results are respectively half the power and one
minus half the power, without an invented final clamp.

The deliberate modern domain requires finite0<c<1 and a finite positive
stored exponent/power base. Output must be finite in[0,1] and exportable
through the existing supplied-value exponent bounds. Invalid inputs reject
before publishing a batch. These are modern safety and approximation policies,
not claims about original loader or special-case behavior. `Math.Log`/`Math.Pow`
remain host approximations; the float32 denominator is not a recovered runtime
initializer result. Whole-world height parity, slope/road/river evaluation,
materials and live terrain authority remain separate questions.

## Reproduction and independent oracle

The [focused runner](../../SWG-Godot-Client/poc/terrain-second-curve-tests/Program.cs) accepts an
optional original terrain path. It tests negative/zero/high inputs, strict
threshold centers and neighbors, nonzero full-word flags, first-before-second
composition, immutable ownership, malformed framing, invalid parameters,
duplicate/nonfinite requests and cancellation. Original data stays private;
only identities and aggregate findings are documented here.

```powershell
dotnet run --no-restore --project poc/terrain-second-curve-tests/TerrainSecondCurveTests.csproj -- $OriginalTerrain | Set-Content $PrivateReceipt
python tools/python/verify_terrain_second_curve.py --receipt $PrivateReceipt
```

For a fresh restore use the runner's explicit `NuGet.Config` and an available
local package cache. No package download is required. The
[independent verifier](../tools/python/verify_terrain_second_curve.py) uses
Fraction for exact decisions and Decimal at90 digits for logarithm/power,
checking the F32 exponent spill, exact dyadic output conversion and F32
projection independently of the .NET implementation. Its modern absolute
tolerance is4e−15. Original execution is not part of this verification.

October13,2026 candidate verification passed719 runner assertions, all five
original families,85 independent samples and three same-F32/different-branch
threshold witnesses. Maximum absolute oracle error was1.1102230246251565e−16.
Core compilation passed with zero warnings/errors. Wider-region acceptance
must independently re-run traversal scope, parent-layer ordering and mesh
grounding; family admission alone does not prove those joins.
