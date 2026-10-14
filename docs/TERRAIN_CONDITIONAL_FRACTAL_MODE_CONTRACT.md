# Opt-in conditional fractal modes

Date:2026-10-04. [TerrainConditionalFractalSampler](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainConditionalFractalSampler.cs) now has a distinct `CreateForSupportedModes` factory for the recovered finite modes0..5. Existing `Create` remains mode0-only, with its frozen factory rejections and default arithmetic unchanged. Optional sine and both curves remain unsupported. All arithmetic is conditional on explicit nearest-even table, construction/normalization and query precision24/53/64; original controls and generated-terrain parity remain Unknown.

## Evidence and finite mode graph

Baseline EXE SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` gates VA interpretation before decoding. The [mode verifier](../../SWG-Godot-Client/poc/round50-tests/Rendering/verify_modes.py) checks42 additional instruction anchors and reuses all230 [frozen46 sampler anchors](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md), including the six dispatch targets. Modes0/1 both dispatch to `013E7CDC`; mode1 is a confirmed same-body route, not an inferred alias.

Let R mean query nearest-even arithmetic. Noise is the retained dyadic result of the actual [owned kernel](TERRAIN_SEEDED_NOISE_KERNEL_CONTRACT.md); no F32 spill precedes the following contribution.

| Mode | Target / direct observation | Contribution | Finish |
|---|---|---|---|
| 0,1 |013E7CDC, unchanged recovered raw-noise body| noise | R(R(R(stored sum * normalization) + 1) * half) |
| 2 |013E7DAF FABS,013E7DB1 reverse subtraction from one| R(1 - abs(noise)) | R(stored sum * normalization) |
| 3 |013E7E23 FABS before amplitude multiplication| abs(noise) | R(stored sum * normalization) |
| 4 |013E7E90..BC clamp comparisons,013E7EBD/C3 one minus retained clamp| R(1 - clamp(noise)) | R(stored sum * normalization) |
| 5 |013E7F2F..5B clamp comparisons then amplitude multiplication| clamp(noise) | R(stored sum * normalization) |

Here clamp replaces values strictly below zero with positive zero and strictly above one with one; it retains values at either endpoint. Lower comparisons use `TEST AH,5 / JP`, which retains finite equal zero (including negative zero). Upper comparisons use `TEST AH,41 / JNE`, retaining finite less/equal and replacing greater. Six ordered-status checks independently validate those branch predicates. FABS clears the sign of zero without rounding. The one-minus operation is rounded under query precision; clamp comparisons introduce no rounding/store.

Observation: instructions, stores, dispatch and finite branch predicates are Confirmed. Interpretation: the table above is the exact finite contribution/finish contract under supplied controls. Consequence: do not replace retained contributions with prematurely stored F32 or erase negative zero at a strict clamp boundary. Nonfinite/unordered original behavior is not admitted by this implementation.

## Shared stores, ownership and eligibility

All construction normalization, coordinate scaling/offset stores, octave noise-coordinate stores, amplitude/sum/frequency stores and cache policies remain those of the [conditional mode0 contract](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md). Each octave computes R(contribution * stored amplitude), adds the previous stored sum, and stores F32. Frequency then amplitude update and store F32, including final unused updates. Modes2..5 reach the common normalized finish at `013E7F8B/8E`; modes0/1 alone retain the shift/half finish. The common cache FST stores F32 while returning the retained miss; cache hits use `FromCacheF32`.

The shared private factory preserves old `Create`'s mode0 predicate and rejection message. `CreateForSupportedModes` admits only raw mode words0..5, fresh sine-disabled and both curve flags disabled. These checks precede normalization/table builds. Disabled curve-parameter NaN/Infinity words remain unevaluated metadata; finite used fields, bounded positive octaves, fixed shape, seed/zero-gradient and recovered-store eligibility are unchanged.

There is no added mode mutation, cache reset inference, family-set routing or sibling50 API. Result computation/cache origin remains distinct from the nested supplied representation. Candidate cache commit, late-failure recovery, explicit clear, cancellation, owner-thread operations, immutable snapshots, disposal and all conservative logical byte/work caps remain unchanged. Reentry remains structurally excluded by bounded spans/immutable inputs and no callbacks, with the active-operation guard retained. No original failure/thread/request semantics follow from these modern policies.

## Reproduction and verification

The [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainConditionalFractalModeScenarios.cs) use the actual Core decoder, explicit factory and kernel. The independent verifier composes frozen49 normalization/store/cache semantics with frozen47 Fraction/integer noise, adding independently authored finite contributions. Expected values never come from native output. One pinned first original MFRC family supplies private dynamic metadata; mode overrides and two authored parameter frames (including negative amplitude) are explicitly synthetic. All coordinates/control profiles are synthetic. Public sources contain no original selected word array/seed/table corpus.

Final exact comparison:530 scenarios,44 factory rejections,14094 actions,11826 returned queries,2268 deliberate action rejections and1350 retained-miss/F32-hit differences.972 returned negative-zero values match independently.81 profile/parameter pairs confirm mode0/1 equivalence.26 API negatives include five explicit legacy-factory mode1..5 rejections, plus precision/budget/late failure/index/cancellation/owner/snapshot/dispose checks. Raw-input and snapshot preservation pass.

Thirty pure-model contribution-boundary checks cover negative, negative zero, zero, one and above-one values across query profiles, complementing the six static ordered-status checks. Whole native sampler scenarios use real generated kernel noise; they do not inject arbitrary noise into Core to force the upper clamp branch. The upper replacement branch is retained from direct executable evidence, rather than claimed as a demonstrated original runtime occurrence.

Frozen49 remains exactly274 scenarios/28 factory rejections/5940 returned queries/1134 action rejections/792 miss-hit differences. Lead432 sampler-to-consumer joins preserve four unsigned-high-bit samples and108 final differences. Frozen kernel1155, raw330/U642592, retained7762/786 and prior terrain/sampler gates pass; default smoke passes. All final builds have zero warnings/errors. The [runner](../../SWG-Godot-Client/poc/round50-tests/Rendering/Invoke-Modes.ps1) requires a fresh private output, rejects lexical reparse ancestors before creation, uses explicit cached/offline dependencies, and runs frozen49 plus lead432 by default. `ModesOnly` repeats the affected mode suite when frozen gates are already retained.

Remaining Unknown: active original controls/exception masks, FSIN/log/curve helpers, mode words outside0..5, legacy mutation/invalidation/reload/lifetime, complete work-request/terrain recursion and generated parity. Next: a bounded accepted-family/layer consumer composition with explicit caller cell/clear/control policy, retaining computed/cache origin. Lead owns canonical reconciliation and acceptance.
