# Terrain constant-height operation contract

[TerrainHeightConstantOperation](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainHeightConstantOperation.cs)
applies one immutable [AHCN input](TERRAIN_GENERATOR_INPUT_CONTRACT.md) to explicit
raw F32 input-height and supplied-influence bits. It implements the recovered
arithmetic graph and local rule gate with an **explicit** nearest-even software
precision choice. It does not produce influence, evaluate a terrain layer graph,
resolve ancestor activation, or claim original generated heights.

## Direct operation and caller evidence

Executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
was verified before VAs. AHCN process slot at `0x015ED688` is `0x00B76540`.
Constant `0x015DC6A0` is raw F32 zero and `0x015DC6F8` is raw F32 one.
At `0x00B76544..0x00B76552`, influence is compared against zero; testing x87
status bits `0x41` skips less-than, equal, and unordered comparisons. Thus
negative finite influence and either signed zero leave the grid untouched.
A masked NaN comparison also skips; nonfinite exception behavior depends on the
unclosed control word. This is stricter than a generic nonzero gate.

The affector loop in layer process `0x00B63D20` calls local enabled getter
`0x00B62F20` at `0x00B64166`, skips at `0x00B6416D` when false, then multiplies
two caller values and stores the supplied influence to F32 on the stack at
`0x00B6417C` before invoking virtual slot `+0x10` at `0x00B6418C`.
Getter `0x00B62F20` returns the inherited header's local byte at `+8`.
The AHCN virtual itself contains no enabled check; the modern wrapper combines
this observed caller gate with the process operation. It uses the immutable
input's local IHDR flag, not a live legacy mutation or an ancestor policy.
The complete producers of those caller values remain outside this kernel.

Let `b` be input height, `c` constant height, `w` supplied influence, and `R_p`
nearest-even significand rounding after each indicated arithmetic instruction.
For enabled rules with `w>0`, exact dispatch and order are:

| Raw operation | Recovered branch | Ordered arithmetic before final F32 store |
|---|---|---|
| 1 | Add | `R_p(R_p(w*c)+b)` |
| 2 | Subtract | `R_p(b-R_p(w*c))` |
| 3 | Multiply-blend | `R_p(b+R_p(R_p(R_p(b*c)-b)*w))` |
| 4 | Error/zero | call reporter, then store positive zero if it returns |
| 0 and every other U32 | Default blend | `R_p(R_p(R_p(1-w)*b)+R_p(w*c))` |

The jump table at `0x00B76624` contains targets `0x00B7657B`,
`0x00B76593`, `0x00B765AB`, and `0x00B765D6`. The unsigned range test after
subtracting one routes every raw value outside1..4 to default blend, including
high-bit and negative signed spellings. These descriptions are independently
authored operation labels, not recovered original enum names. There is no
clamp to0..1 and no endpoint shortcut. At `w=1`, default blend still evaluates
both terms; signed-zero results can therefore differ from copying `c`.

Operation4 calls reporter `0x00AA1D80` with the string at `0x018B8A1C`,
then loads the initialized local positive zero. The modern result reports
`InvalidOperationZero` and returns positive-zero bits without invoking that
reporter. It does not emulate reporting hooks, exceptions, or process failure.
The finite arithmetic branches have one `fstp dword` at `0x00B76618`, with
no intervening F32 spills inside this graph. No FMA or algebraic rearrangement
is compatible with the stated instruction order.

## Precision boundary and explicit policy

**Confirmed:** one final F32 store and exact arithmetic instruction order.
**Unproven:** active terrain worker x87 precision, rounding-control and
exception-mask state. Existing control helpers were inspected rather than
assuming the host's float arithmetic matches them. Startup `0x00AA7B00`
clears precision/rounding bits through `and eax,0xF0FF` and installs a
nearest-even/24-bit candidate. Main frame path `0x00AA0720` calls
`0x00AA7BD0` to synchronize stored control state. Precision setter
`0x00AA7C30` has callers in another module; the complete worker state and
changes along its generation path remain unclosed. No original executable
was run. Startup policy alone is not proof of active worker policy.

`Evaluate` and `EvaluateBatch` therefore require
`TerrainHeightArithmeticPrecision.Binary24`, `.Binary53`, or `.Binary64`;
there is no default selection. These are explicit conditional arithmetic
models, all nearest-even, not a claim that any selected mode is the original
terrain worker. Directed rounding and legacy floating-point exceptions are
unsupported. A later worker-control closure may select a mode at integration.

Finite dyadic arithmetic uses bounded `BigInteger` significands, rounds after
each arithmetic instruction to the selected width, and then rounds once to
F32, including subnormal ties and signed zero. Intermediate exponent behavior
is wider than F32; no finite input to this fixed graph approaches x87's wide
exponent limits. This matters for overflow/cancellation paths: per-instruction
F32 exponent overflow would not reproduce an x87 wide intermediate.
The independent oracle uses `Fraction` arithmetic and a separate quantization
procedure. Tests contain28 input sets distinguishing intermediate precisions;
2763 agreeing model vectors do not close original runtime parity.

## Eligibility, result ownership and bounded batches

`Evaluate(rule, inputHeightBits, influenceBits, precision)` returns a value
record with original height/influence/constant/operation/enabled bits, selected
precision, result bits and outcome. Gate order is deliberate:

1. Reject null rule or invalid precision configuration.
2. Disabled local header: return input bits unchanged; unused nonfinite bits
   remain lossless.
3. Enabled rule: reject nonfinite influence as a **modern policy**. Legacy
   masked NaN skipping and infinite arithmetic/traps are not reproduced.
4. Finite nonpositive influence: return input bits unchanged, without inspecting
   unused base/constant values for finite eligibility.
5. Operation4: return positive zero and diagnostic outcome, without reading
   unused base/constant values numerically.
6. Other active operations: require finite base and constant; use exact ordered
   conditional arithmetic. Reject nonfinite final F32 result before publication.

Overflow rejection and nonfinite influence rejection are modern eligibility
rules, not evidence of legacy rejection. Skipped outputs may retain nonfinite
input bits. Nonfinite active base/constant arithmetic, NaN propagation,
exception flags/traps and reporter side effects are explicitly unsupported.
Finite negative heights, subnormals, signed zeros and influence above one
are supported; unknown operation bits use the observed default branch.

`EvaluateBatch` requires equal-length input-height/influence spans and an
explicit modern sample cap (default65536, hard maximum1048576; zero allows an
empty batch). Invalid cap throws `ArgumentOutOfRangeException`; shape/cap or
numeric eligibility failures throw `InvalidDataException`. Caps precede
snapshots. Both inputs are copied, all candidates are computed before returning,
and neither input is modified even when spans alias or a later sample fails.
An earlier returned batch survives subsequent failures. `Results` is a
read-only collection of value records; `CopyHeightBits` returns new owned words.
The rule is borrowed immutable input. There are no callbacks or external writes.

Work is fixed arithmetic per sample, bounded by sample count and F32 operand
widths; software significand sizes stay bounded by the fixed graph/exponent
range and selected width. There is no input-controlled numeric iteration.
Logical sample caps do not account for arbitrary caller-retained batches or
inspection copies; each batch owns two temporary input snapshots during
construction and one retained result array. GC reclamation timing is not a
legacy cleanup guarantee.

## Same original input, distribution only

Readonly root `C:/Stardust-DEV`, config `swgemu_live.cfg`, config SHA
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,
path `terrain/tatooine.trn`, priority42,1188446 bytes, full SHA
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`.
TGEN SHA `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
The accepted PTAT/TGEN readers establish87 AHCN rules in this same input:

| Operation | Records | Locally enabled | Finite constant |
|---:|---:|---:|---:|
| 0 | 62 | 61 | 62 |
| 1 | 14 | 13 | 14 |
| 2 | 11 | 11 | 11 |

Operations3,4 and other values have no occurrence in this one sample and are
covered by labeled synthetic vectors plus direct static evidence. The sample
was not evaluated to produce original heights. No new corpus sweep occurred.

## Reproduction and verification

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainHeightConstantOperationScenarios.cs)
and [independent oracle](../../SWG-Godot-Client/poc/round34-tests/Assets/verify_ahcn_operation.py)
pass2763 vectors (2528 accepted/235 rejected), ten batch/ownership/cap/failure
checks,21 process anchors, five caller anchors, exact jump/constants/error
string and same original distribution. Build has zero warnings/errors.

```powershell
python poc/round34-tests/Assets/verify_ahcn_operation.py generate local-private/round34/fixtures.json
dotnet restore poc/round34-tests/Assets/Assets.csproj --configfile local-private/round34/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round34-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round34-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round34/fixtures.json > local-private/round34/native.txt
python poc/round34-tests/Assets/verify_ahcn_operation.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round34/native.txt
```

Use cached .NET8/Python, clear package-source NuGet config/private packages,
task-specific APPDATA and PYTHONUTF8=1. Static audit uses installed pefile and
capstone via PYTHONPATH. The oracle reuses the accepted33 independent TGEN
parser for original structure; it does not duplicate that earlier milestone.
No downloads, original executable execution, live contact or proprietary public
fixture promotion is needed.

Next exact probe: trace the terrain worker thread's creation/control-word
installation and any precision/rounding changes before generation; then select
one supported profile if proven. Influence production, ancestor activation,
boundary/filter composition and complete procedural terrain remain separate.

## Round35 bounded follow-up

Round35 traces the actual CreateThread/CRT/wrapper/terrain path. The inspected worker bodies do not install the foundation word; the CRT startup function pointer resolves to ret in the baseline. OS-initial control word and transitive/virtual mutations remain unproven. Retain explicit precision selection; main-thread configuration is insufficient. See [runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md).
