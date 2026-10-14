# Optional fractal curve finish: static contract and power boundary

The optional finish in sampler `013E7C00` has a closed store/operand/branch graph. Its first curve is mathematically a power bias; its second is a two-sided power curve. Exact x87 logarithm/power values, shared helper controls/exception behavior and original effective controls remain unclosed. This research adds no numerical sampler or `Math.Pow` approximation.

All VAs refer to executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. [Static verifier](../../SWG-Godot-Client/tools/python/verify_terrain_fractal_curve_finish.py) reuses the accepted230 sampler anchors and adds raw-file/mapped-PE equality, bounded region hashes, operand/branch checks and one pinned serialized occurrence. The [existing sampler contract](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md), [MFRC mapping](TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md) and [closed modes](TERRAIN_CONDITIONAL_FRACTAL_MODE_CONTRACT.md) remain unchanged.

## Serialized flags and order

MFRC words1/2 supply first flag/parameter and words3/4 second flag/parameter. The 0001 loader compares the entire U32 flag with zero (`013E86A4/A6`, `013E86D5/D7`), normalizes the low byte via SETNE and calls setters `013E6C50/70`. Thus arbitrary nonzero words, including256, enable the corresponding runtime byte; inspecting only the serialized low byte would be wrong. The setters place that byte at+24/+2C and the F32 parameter at+28/+30, then clear cache under the accepted setter contract. No parameter range check is observed here. Finite parameter values pass the load/store chain; nonfinite x87 load/store exceptions are not made into a safe policy.

Modes0/1 reach the common curve entry after normalized sum, plus one, times half; modes2..5 reach it after normalized sum. Optional sine precedes that finish and stores F32. It remains outside this investigation. `013E7F91` FST records a F32 snapshot u32 while retaining u in ST0. First-flag processing occurs before second-flag processing. If both flags are off, final cache FST preserves retained miss return; hits reload F32.

## First optional curve

At `013E7F99`, zero byte+24 bypasses this curve. Enabled processing explicitly pops retained u and reloads u32 (`013E7F9B/9D`). It loads parameter p from+28 and forms these stack operands:

| Instructions | Observation / consequence |
| --- | --- |
| `013E7FA3/A5/A7` FLDLN2, FXCH, FYL2X | Retained logarithmic numerator is the x87 primitive `L2 * log2(p)`, where L2 is FLDLN2. |
| `013E7FA9` FST F32 temporary | Inspection store does not pop or replace retained numerator. |
| `013E7FAC/AE/B4` FLDLN2, FLD F64 half, FYL2X | Retained denominator is `L2 * log2(0.5)`; half is exact F64 `3FE0000000000000`. |
| `013E7FB6/B9` FST F32 temporary, FDIVP ST(1) | Temporary is overwritten; retained exponent is numerator/denominator. No F32 exponent reload occurs. |
| `013E7FBB` call0131E2B0 | ST0=retained exponent, ST1=u32 base. |
| `013E7FC0` FST F32 snapshot | Power-helper return remains retained while the new u32 snapshot is stored. |

Ignoring all machine rounding/approximation, the role is `u32 ^ (ln(p)/ln(0.5))`. This is a mathematical interpretation, not permission to replace the two logarithms/divide with algebraically simplified `-log2(p)` or to project exponent early. Confidence: Confirmed instructions/stack/store order; power role Strongly inferred with the callee graph and prior audio/door correlation. Exact numeric parity Unknown.

There is no first-stage guard for base sign/zero or parameter domain. For finite p>0 the real logarithm argument is positive; p=0 has a logarithmic singularity and p<0 a real-domain defect. Positive finite base is a narrow mathematical power domain; zero/negative base and nonfinite inputs enter unclosed helper/exception paths. p=1 mathematically gives exponent0, but exact special-case return/control behavior is not established. With both flags enabled, this stage executes before the second stage's low/high clamps, so those clamps cannot be treated as protection for invalid first-stage logarithms/power.

## Second optional curve

Byte+2C is tested at `013E7FC3/C6/C8` after the first stage. Thresholds compare retained u, not its u32 snapshot. The low F64 constant at015DE0D8 is `3F50624DD2F1A9FC` (the stored approximation of0.001); high at01609620 is `3FEFF7CED916872B` (approximation of0.999).

- `FCOM low; TEST AH,5; JP` continues for finite equality/greater; finite u<low returns exact zero. Equality is not clamped.
- `FCOM high; TEST AH,41; JNE` continues for finite less/equal; finite u>high returns exact one. Equality is not clamped.
- The middle branch compares retained u with F32 half using FCOMP and pops it. `TEST AH,5; JP` selects upper for equality/greater. Lower is strictly u<0.5; upper includes0.5.

For the middle branch, parameter c at+30 is used as `1-c` via FSUBR with F32 one, then FLDLN2/FXCH/FYL2X gives the retained logarithmic numerator. `013E801F` divides by a **reloaded F32 writable global** at019BB4C4. `013E8025` FSTP stores the quotient F32; later power calls reload this exponent. This mandatory spill differs from the first curve.

The global is zero-fill in the image, not a read-only ln constant. A pointer at01866B08 addresses the forwarding entry013E6E20; its JMP reaches013E6E30. Selected writer instructions load FLDLN2 and exact F64 half, apply FYL2X, and FSTP to019BB4C4 before RET. Therefore, **if this initializer has executed**, the denominator is a F32 store of the x87 ln(0.5) calculation. Startup-table dispatch order, effective initialization controls, all possible indirect writers and subsequent mutation remain Unknown. Do not use file-image zero as the runtime denominator or silently substitute an exact real logarithm.

Ignoring rounding and writing d for that stored denominator, the middle curve is:

| Retained threshold decision | Actual helper operands and retained finish |
| --- | --- |
| u<0.5 | base=2*u32; exponent=F32(ln(1-c)/d); return helper(base, exponent)*F32(0.5) |
| u>=0.5 | base=2*(1-u32); same F32 exponent; return F32(1)-helper(base, exponent)*F32(0.5) |

The power calls are013E803D/8062. Base calculation uses the snapshot, while threshold/half decisions used retained u. Cache FST stores the final result without replacing ST0. There is no final clamp after these power branches. When only the second curve is enabled, sufficiently low/negative u returns zero and sufficiently high u returns one without using c. In the middle, finite c<1 gives a positive real logarithm argument; c=1 is singular and c>1 invalid for real log. Finite negative c is not rejected and can yield negative exponent, so a final [0,1] result cannot be assumed. Exact zero/nonfinite/special-case/exception results remain Unknown.

Masked unordered status bits follow the observed masks into the middle/upper paths. FCOM and logarithm instructions can signal invalid exceptions; a status-path fixture is not a claim that NaN is safe or that traps are masked. No caller validation, cleanup or atomic original cache publication is recovered here.

## Power ABI and bounded stop

Complete 25-byte wrapper0131E2B0..0131E2C8 (SHA-256 `A39B146E16CBBAB84442AD0A2AC74AD3532FEC7AD9F70BF38CF2D7D8F4A99FF9`) swaps ST0/ST1, **pops base into QWORD[ESP]**, and **FST snapshots exponent into QWORD[ESP+8] without popping it**. It copies the stored exponent's high word to EAX, calls0131E2D2, drops16 stack bytes and returns. Thus a binary64 exponent snapshot/classification exists, but the live exponent is still retained on callee entry. Do not infer that both numeric operands have simply been narrowed to host double.

Two complete helper graphs were bounded: this power wrapper and the denominator initialization chain (the explicit5-byte forwarding edge plus17-byte arithmetic tail, SHA-256 `42FBEC7A07A90212525418AB85728BBDBA825388DCD9EFA6CF109F0E1E961F9E`). The second graph includes the tiny forwarding entry; it is not silently omitted from scope. No further complete library helper body was expanded.

The power-core entry was inspected as a bounded **partial graph**,127 instructions in0131E2D2..0131E4A4 (SHA-256 `8FFBFC05A7DBB02AAD5BC74AA1CC0A1FCA098ACC5ACF913239E8D4FC9B8A160B`). It has nine unexpanded callsites and eight shared-tail edges. It snapshots FNSTCW, compares exactly027F and calls013258C5 on mismatch; it classifies stored binary64 exponent/base bits, reaches FYL2X and calls013258B0, with other branches touching zero/negative/nonfinite handling and x87 state save/restore. Outbound callees are013258C5,013258F5,013258B0,0131E4A5,01325850,01325A3C; shared exits0132594E/01325999. No helper beyond this stop boundary was expanded.027F names masked exceptions, nearest rounding and53-bit precision in the control-word encoding, but the comparison/call alone does not prove the callee sets/restores that state.

Original effective sampler controls, helper normalization/restoration, transcendental approximation, classification/exception callbacks and returned rounding remain separate Unknowns. Existing audio/door documents correlate this address with a power role; they do not prove fractal numerical behavior or authorize a host approximation.

## Original occurrence and verification

Only the already pinned original layer3 AHFR/family is inspected: AHFR TGEN offset13672,length114,hash `B85D90EC684322F07798476A55A665DCDE050D5B355D86CB3AD5ACB41883F674`; signed family4; MFRC offset7062,length84,hash `77FDAC709B0D3E3113FAAD8A82F4746FDF3969434DC8BF2BAF42273AE89D5FE3`. Its first curve is serialized enabled, second disabled and first parameter finite strictly between0and1. Original parameters/names/raw words remain private. This establishes a real unsupported input and its loader path, not actual original layer activation or numeric parity.

The [research runner](../../SWG-Godot-Client/poc/round56-tests/Rendering/research_checks.py) performs fresh static replay, independent authored raw-PE extent/truncation fixtures, Fraction threshold/status fixtures, symbolic operand/store checks, full-word flag checks, early wrong-identity and output-root guards. The linked-output fixture creates its own junction and target in the fresh private run, requiring no earlier private round artifact. It imports cached PE/Capstone only after identity and root admission; no original executable or numerical transcendental code is run. Counts and frozen/hash/HEAD/index verification are in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R56_FRACTAL_CURVE_FINISH.md).

Minimal next prerequisite: close the positive finite-base/finite-exponent path through013258F5 and013258B0 under an explicit027F precondition, preserving the wrapper's retained exponent and binary64 snapshots. Then separately close control normalization/restore and zero/negative/nonfinite handling before admitting original numerical parity. A conditional approximation prototype must be labeled as such; neither the existing sampler's curve guard nor original layer3 rejection changes in this round.


## Round57 explicit approximation experiment

[Modern first-curve batch](TERRAIN_FIRST_CURVE_FRACTAL_BATCH_CONTRACT.md) preserves the recovered base F32 store but uses explicitly named binary64 Log/Pow under a bounded domain and independent numerical tolerance. OriginalNumericParity remains false. Frozen operational selectors/samplers still reject enabled curves; original retained-logarithm/power/control research above remains open.
