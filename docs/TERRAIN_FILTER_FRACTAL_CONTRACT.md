# Conditional terrain fractal filter contract

Date: 2026-10-04. Scope: one FFRA occurrence immediately preceding the selected FHGT in the [height layer order investigation](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md). This closes the finite consumer of a supplied F32 fractal sample for version 0005. Original noise generation, family lookup validity, sampler precision, and full terrain admission remain Unknown.

## Identity and evidence

The static executable baseline SHA-256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses below are VAs. The verifier checks 93 instruction anchors, one RTTI identity, nine vtable words, and the scalar-read counts for six loader versions without executing the original.

Observation: constructor `00B77B50` passes kind 1 and tag `46465241` (FFRA) to `00B633E0`, then installs vtable `015ED704`. RTTI type descriptor `018B8A94` identifies FilterFractal; complete-object locator is `016263A4`. Factory compare `00B7A02D`, constructor call `00B7A05D`, and load dispatch `00B7A07B` establish virtual slot +20, whose target is `00B77CB0`. Slot +8 instead names the common IHDR loader. Confidence: Confirmed. Consequence: route FFRA through its class dispatcher, rather than treating the common header loader as the entire codec.

Constructor defaults are cached pointer +1C = null, cached family +20 = FFFFFFFF, desired family +24 = 0, scale +28 = F32 one, lower +2C = 0, and upper +30 = 0. Destructor `00B77BC0` clears the cached pointer and tails to the base destructor. Observation establishes these stores; it does not establish ownership or lifetime of the looked-up fractal object.

| Version | Loader VA | Scalar reads | Modern typed support |
| --- | --- | --- | --- |
| 0000 | 00B77DD0 | 4 | Opaque |
| 0001 | 00B77F20 | 13 | Opaque |
| 0002 | 00B78140 | 14 | Opaque |
| 0003 | 00B78370 | 15 | Opaque |
| 0004 | 00B785F0 | 5 | Opaque |
| 0005 | 00B78790 | 6 | Yes |

The dispatcher recognizes 0000 through 0005. Versions 0000 through 0003 read inline legacy fractal configuration and convert it through `013E6880` / `00B76040`; 0004 reads an embedded fractal object through `013E8410` before five PARM words. These paths are observed, but their complete legacy schemas remain unclosed. The modern decoder preserves them as owned opaque envelopes and rejects attempts to query them. Unknown versions are also opaque. Recognized does not mean typed or implementation-ready.

## Version 0005 bytes

The strict typed envelope is FORM FFRA, FORM 0005, IHDR, then FORM DATA containing one PARM leaf. PARM has exactly 24 payload bytes: six little-endian U32 words in this order.

| Word | Interpretation | Runtime field |
| --- | --- | --- |
| 0 | Fractal family identifier | +24 |
| 1 | Feather selector | +14 |
| 2 | Serialized F32 feather amount | +18 after finite clamping |
| 3 | Raw F32 lower sample threshold | +2C |
| 4 | Raw F32 upper sample threshold | +30 |
| 5 | Raw F32 sample scale | +28 |

Loader calls at `00B787DA`, `00B787F2`, `00B7880A`, `00B7885A`, `00B78872`, and `00B7888A` establish the six assignments. Writer `00B788D0` emits six words in the same order at `00B7891A/2D/40/53/66/79`. Range and scale setters are raw stores. Confidence: Confirmed field order and widths; family meaning is Strongly inferred from lookup linkage. Implementation consequence: preserve raw bits and order; do not silently swap thresholds or clamp scale.

IHDR versions 0000/0001 follow the [existing header contract](TERRAIN_FILTER_HEIGHT_CONTRACT.md). The decoder snapshots the encoded envelope, words, name, and legacy header bytes. Raw NaN and infinity payloads are preserved; only active evaluation requires finite values. An unsupported header remains opaque.

## Original occurrence

The pinned effective priority-42 `terrain/tatooine.trn` is 1,188,446 bytes, SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. Its TGEN starts at offset 210, is 139,608 bytes, and hashes to `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. Configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

Selected layer 4, parent 0, has ordered rules BCIR, BCIR, FFRA, FHGT, AHFR. The FFRA is rule ordinal 4 when IHDR/ADTA occupy ordinals 0/1. It starts at TGEN offset 14086, occupies 118 bytes, and hashes to `A601BE86EAB8D9405F8F567F2E27DF65AC2A208C6A556E564AA6B14E13C8C12D`; the FHGT starts at 14204. Its six PARM words are `[27, 3, 1058776547, 1057786692, 1058642330, 1065353216]`, and its header enables the rule. Confidence: Confirmed original occurrence and raw layout. Only this one FFRA was selected; synthetic numeric fixtures do not establish original terrain samples or reachability at a particular coordinate.

## Conditional arithmetic and retained query

Observation: `00B77BE0` compares cached family +20 with desired family +24. On mismatch it writes the cached key before lookup through state+58 / `00B75B70`, then stores the returned pointer at +1C. It forwards four sampler arguments to `013E7C00` at `00B77C13`. `00B77C18` multiplies the returned value by scale; `00B77C24` mandatorily stores F32 before threshold testing. Confidence: Confirmed instruction sequence. Missing-family behavior, cache validity, synchronization, and sampler implementation remain Unknown.

The modern boundary accepts an explicitly supplied **F32** sampler return. This narrows an original x87 return that may be unspilled; no equivalence for an arbitrary wide sampler return is claimed. A caller explicitly chooses arithmetic precision 24, 53, or 64 bits; original floating control state remains Unknown.

At chosen precision p, multiply the supplied sample by scale, store F32, and reload it as h. Clamp finite serialized amount to [0,1]. If h is at or below the lower threshold, or at or above the upper threshold, return zero. Otherwise retain span = ((upper - lower) * amount) * 0.5 without an intervening F32 store. Test the lower ramp first: lower + span > h yields (h - lower) / span. Otherwise upper - span < h yields (upper - h) / span. Otherwise return one. Reversed/equal thresholds follow these comparisons and yield zero rather than being repaired.

The query retains its arithmetic Number and provenance. Its F32 projection is an inspection value. Shared shaping uses the retained value: selector 0 is identity, 1 squares, 2 square-roots, 3 computes ((3 - (u+u)) * u) * u, and other selectors yield zero. The [layer carrier](TERRAIN_LAYER_INFLUENCE_IMPLEMENTATION_CONTRACT.md) receives this retained query through `FromFractalFilter`; shaping and combination use the caller's explicit combine precision. Existing source identifiers 0 through 4 are preserved; FractalFilter is appended as 5.

This FFRA consumer uses the supplied fractal sample, not the mutable current-height cell used by FHGT. The modern RuntimeCache is an immutable snapshot of input/family/scale/thresholds/runtime amount. It is not an implementation of the original map or borrowed-pointer cache.

## Modern bounded API policy

Decode defaults: 65,536 input bytes, 64 nodes, depth 8, 4,096 name bytes, and 65,536 decoded-copy bytes. Hard maxima are respectively 4 MiB, 65,536 nodes, depth 64, 65,536 name bytes, and 4 MiB. Exact limits accept; one below rejects. Snapshot bounds precede copying. PARM copy charge is 24 bytes plus copied header name and legacy bytes.

Query defaults allow 65,536 samples and 196,608 work units; hard maxima are 1,048,576 samples and 3,145,728 units. Direct Query charges 2 units. Enabled Evaluate charges 3 units. Disabled evaluation skips numeric validation and charges zero. Batch preflights global sample/work caps before snapshots or result allocation, owns sample copies, and publishes only a completed immutable result. A late invalid sample cannot return a partial batch. Managed objects own no external handles; this is not a claim of deterministic original cache destruction.

Active evaluation rejects nonfinite sample, scale, thresholds, or amount, scale-store overflow, and nonfinite inspection projections. In particular, projecting span for inspection narrows the supported domain if a wide finite span would overflow F32. These are deliberate modern finite-policy restrictions, not recovered original malformed-input behavior.

## Verification and remaining work

The independent Python producer uses raw struct packing and Fraction arithmetic, rather than invoking the C# implementation. It generates 152 raw cases (134 rejected) and 2,326 query cases (29 rejected), crosses all nine query/combine precision pairs, and includes 118 witnesses against premature query projection and 128 witnesses against omitting the mandatory scale store. It checks the one pinned original snapshot. Native scenarios also check ownership, mutation isolation, global caps, exact/one-less budgets, disabled unused values, unsupported inputs, late failure, and six null arguments.

Successful replay: all 152 raw and 2,326 query receipts matched, including 30 ownership/budget checks and six null-argument rejections. The frozen 6,354 carrier cases and 1,141 filtered-sequence cases matched unchanged, including their ownership checks. Both actual-Core runner and separate Core/SmokeTests builds had zero warnings and errors. Default smoke completed with 18 journal events and zero failures. Four mutated/truncated executable/config identity tests rejected before decoding. Workspace and focused file-scope validation are recorded in the private hash handoff.

Next bounded probe: establish `013E7C00` sampler arithmetic and `00B75B70` family-map lookup validity/lifecycle under evidenced floating controls before admitting original numeric terrain samples or implementing AHFR. Legacy versions 0000 through 0004 require separate schema closure.

## Round46 accepted compatibility increment

The [family lookup](TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md) and [sampler](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md) establish static prerequisites. A miss may retain wide ST0 after writing the F32 cache, whereas a hit reloads F32. Existing supplied-F32 APIs therefore do not establish original sampling parity. The [903-case supplied consumer join](TERRAIN_SUPPLIED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) independently verifies mutable height, query timing and retained influence composition without generating noise. Exact seed/table/interpolation, optional postprocessing, effective controls and reload quiescence remain open.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.


## Round54 accepted increment

The [selected original filter](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md) and [per-cell field](TERRAIN_FAMILY_HEIGHT_FIELD_CONTRACT.md) pass independent replays. Lead Version6 composes selected root AHFR and layer3 FFRA under explicit synthetic coordinates, prior heights and controls;400 native/headless/GPU cells, mixed/zero-state checks and late zero-visual-allocation rejection pass. Original layer admission, traversal and active controls remain Unknown.
