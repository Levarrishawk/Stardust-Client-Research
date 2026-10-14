# AHFR raw input and supplied fractal height consumer

TerrainHeightFractalInput (external historical reference) owns AHFR raw records and decodes version0003 fields. TerrainSuppliedHeightFractal (external historical reference) implements only the direct height consumer with an explicitly supplied F32 fractal value. It does not generate noise, resolve a family, reproduce sampler/cache state, mutate original grids, or select original active floating controls. This provides a conditional consumer for the actual writer established in the [height-order contract](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md), not original generated-terrain parity.

All addresses refer to EXE SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The the separate implementation record and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R45_HEIGHT_FRACTAL.md) preserve reproduction and open questions.

## Identity, loaders and raw boundary

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Factory compares AHFR tag `41484652` at `00B7A696`, allocates0x28 at `00B7A6AD`, constructs `00B767E0` at `00B7A6C7`, loads through virtual+1C at `00B7A6E5`, passing generator+40. Constructor calls base `00B63440`, installs vtable `015ED698`; COL `016262C0` names descriptor `018B8A30`, `.?AVAffectorHeightFractal@@`. | Actual AHFR identity, Confirmed. | Do not substitute AHCN or an adjacent affector. |
| Constructor sets cached pointer+14=0, desired key+1C=0, operation+24=0, cached key+18=FFFFFFFF and scale+20=F32 one. | Fresh defaults, Confirmed. | Sentinel collision/invalid key reachability is not assumed safe. Raw fields preserve all U32 patterns. |
| Dispatcher `00B76990` selects0000/1/2/3 through table `00B76A6C`: loaders `00B76A80`, `00B76BC0`, `00B76DC0`, `00B76EE0`. | Four recognized original versions, Confirmed. | ClientVersionRecognized is separate from modern typed support. |
| 0000 reads operation, constructs/transfers embedded fractal state and reads scale;0001 includes counted legacy records and embedded state;0002 enters FORM DATA, invokes fractal loader `013E8410`, transfers through `00B76040`, then reads PARM operation/scale. | Legacy conversion branches, Confirmed at observed calls; full embedded schema/semantics Unknown. | Versions0000 through 0002 remain bounded whole-record opaque inputs with null fields/header and empty interpreted words. No fake family key is extracted. |
| 0003 enters ordered IHDR then FORM DATA/PARM, reads three four-byte words at `00B76F2A/42/5A`, passes them to setters `00B76880/70/90`. Writer `00B76FA0` emits0003 and writes fields+1C,+24,+20 at `00B76FEA/FFD`, `00B77010`. | Family key U32, operation U32, scale raw F32 bits in order, Confirmed. Family-map interpretation Strongly inferred from lookup boundary. | PARM is exactly12 bytes in the modern typed parser; no clamp or conversion is applied. |

The typed envelope is `FORM AHFR / FORM 0003 / (FORM IHDR, FORM DATA / PARM)`, with ordered children and no extras. IFF extents are big-endian; PARM words are little-endian. Known IHDR0001 contains enabled U32 and a raw NUL-terminated byte name;0000 includes three legacy bytes after the terminator. The existing [generator input contract](TERRAIN_GENERATOR_INPUT_CONTRACT.md) supplies that common header. Names have no inferred text encoding. All scale categories, signed zeros, infinities and NaNs are accepted as raw structure; `HasFiniteScale` is inspection only.

Unknown AHFR versions retain owned opaque bytes after generic bounded framing. An unknown IHDR version within the otherwise valid0003 IHDR/DATA/PARM envelope also remains opaque. This is a modern inspection policy, not recovered safe original unknown-version handling. Malformed known envelopes, incorrect tags/order/counts/extents, missing/extra PARM data, unterminated names and exceeded budgets reject.

Raw limits default to65536 source bytes,64 nodes, depth8,4096 name bytes and65536 logical decoded-copy bytes. Hard maxima are4194304 source/copy bytes,65536 nodes/name bytes and depth64. Source minimum24, node/depth minimum1, name/copy minimum0. `DecodedCopyBytes=12+nameBytes+legacyBytes`, excluding terminator, source snapshot, parse temporaries and object overhead. Source caps precede copies; typed-copy/name caps precede typed allocations. Accessor arrays copy, fields/header are immutable, and a late decode failure cannot alter prior inputs.

## Lookup and bounded cache lifetime

For positive product, process `00B768A0` compares desired+1C and cached+18, and only on mismatch writes the cached key, calls `00B75B70` on state+58 and stores returned pointer+14. Same key reuses that pointer without rechecking map identity. Key setter changes+1C only; the next eligible process performs lookup. Nonpositive finite product returns before lookup. Destructor `00B76850` clears+14 and tails to common header cleanup `00B63460`, without deleting the cached target.

`00B75B70` obtains a keyed map item via `00B75DE0`, then getter `00B757A0` returns item+8. The former can return zero for a missing entry; the getter does not validate its input. No null/validity check appears before process calls `013E7C00`. Borrowed embedded sampler ownership is **Strongly inferred** from the getter and pointer-clearing destructor. Map replacement, stale-key validity, missing/sentinel keys, allocation failures, thread coordination and mutation-driven invalidation remain **Unknown**. The supplied API neither promises this cache behavior nor dereferences a legacy pointer.

Sampler call receives coordinates plus column/row. Its cache-hit branch loads F32 cell+0C at `013E7C83`; a miss return path at `013E7FE5/EC` uses `FST` to cache F32 while retaining the pre-store result in ST0. Thus supplied F32 input is explicitly **narrower than the original sampler return**. The observed sampler grid uses stride+2574, data+257C and16-byte cells; noise generation and its full cache validity contract are excluded.

## Exact conditional consumer graph

The layer driver handles local enabled gating; the modern wrapper reproduces that gate using decoded IHDR. Original process itself first returns for finite supplied influence `w<=0`. For active finite operands let `H` be the current F32 height, `F` the explicitly supplied F32 sample, `S` raw scale and `R` nearest-even rounding to a selected significand precision. The retained target is `T=R(F*S)` at `00B768EE`, with no F32 spill before the operation branch. The operation table at `00B76968` selects:

| Raw operation | Retained result before final F32 store |
| --- | --- |
| 1 | `R(H + R(T*w))` |
| 2 | `R(H - R(T*w))` |
| 3 | `R(H + R(R(R(T*H)-H)*w))` |
| 4 | Original diagnostic call `00AA1D80`, then reload prior H; modern outcome InvalidOperationPreserved returns the same raw H without diagnostic side effects. |
| All others, including0 and large unsigned words | `R(H + R(R(T-H)*w))` |

The current-height descriptor is state+18, stride+0/data+8, index=row*stride+column. Load at `00B7690A`, prior-height F32 snapshot at `00B7690D`, and final `FSTP` at `00B7695F` establish the shared-cell boundary. The original diagnostic behavior is conditional on that call returning normally; its broader side effects are Unknown. The supplied kernel has no callbacks or external effects.

The kernel reuses frozen exact `TerrainCircleArithmetic.Number`, offering separately identified nearest-even24/53/64 profiles with a mandatory final F32 projection. Original active precision, rounding and exceptions remain Unknown; none is labeled the original profile. Retained scaled target/subexpressions are not converted to host float. Finite F32 inputs cannot approach x87 wide exponent limits in this fixed graph. Nonfinite active operands or nonfinite final outputs reject; these are explicit modern eligibility rules. Disabled inputs skip all arithmetic, nonpositive finite influence preserves height without requiring finite height/sample/scale, and opaque records reject even for zero samples. Operation4 still requires finite active operands in this modern policy.

`EvaluateBatch` checks equal lengths and the complete aggregate sample/work/snapshot caps before any snapshots or result arrays. It owns three input snapshots and immutable result storage, publishes only on complete success and leaves earlier batches intact after late failure. Defaults:65536 samples,393216 logical work,786432 snapshot bytes. Hard maxima:1048576 samples,6291456 work,12582912 snapshot bytes; minima0. Charges are6 work and12 snapshot bytes per sample, a modern logical policy rather than heap/CPU measurement. Caller memory must remain stable during synchronous snapshotting. No deterministic wiping or concurrency guarantee is implied.

The post-affector virtual+14 at `00B76980` returns true; the accepted layer driver marks its derived-cache dirty flag even when this process did not change height. That invalidation policy belongs to the driver and is not implemented by this isolated supplied consumer.

## Verification and exact next probe

Native scenarios (external historical reference) and the independent oracle pass151 raw cases (134 rejected),8836 supplied consumers (766 rejected),40 ownership/budget/current-height checks and one null-section negative. Three arithmetic profiles cover signed zero, subnormal, cancellation, overflow eligibility, default/known/invalid operations and disabled/nonpositive gating. There are323 witnesses where prematurely storing T to F32 changes the outcome. Static verification passes96 instruction anchors, exact factory/RTTI/vtable/dispatch/operation tables, three PARM reads and eight wrong-identity negatives before VA interpretation.

One priority42 Tatooine AHFR raw record is decoded, at TGEN offset12346, root layer0/child2,0003/IHDR0001,114 bytes, logical copy33, SHA256 `51735AECA9C172F0BD1E8194ACBCECD8CEF03041075CF307822E275D5E936B1E`. No original sampler value or generated height is evaluated. Actual separate-Core runner and default Core/SmokeTests build/run pass with zero warnings/errors; default smoke retains18 journal events/zero failures.

Next bounded probe: establish the selected family map's actual sampler return/cache validity and effective arithmetic controls, then the preceding FFRA gate. Keep this supplied F32 kernel conditional until that evidence supports original generated-cell admission. Legacy embedded versions, universal map lifetime, noise reproduction, driver/cache integration and historical implementation path (omitted) admission remain separate work.

## Round46 accepted compatibility increment

The [family lookup](TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md) and [sampler](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md) establish static prerequisites. A miss may retain wide ST0 after writing the F32 cache, whereas a hit reloads F32. Existing supplied-F32 APIs therefore do not establish original sampling parity. The [903-case supplied consumer join](TERRAIN_SUPPLIED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) independently verifies mutable height, query timing and retained influence composition without generating noise. Exact seed/table/interpolation, optional postprocessing, effective controls and reload quiescence remain open.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
