# Explicit TGEN fractal-filter rule selection

Round 54, 2026-10-04. This engine-neutral boundary selects one explicit index in the owned generator's `Rules` and resolves its FFRA/0005 family exactly. It retains raw evidence and provenance without traversing layers, evaluating a filter, constructing a sampler, reading assets or applying a fallback.

Implementation: [selector](../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainGeneratorFractalFilterRule.cs). Verification: [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainGeneratorFractalFilterRuleScenarios.cs), [independent oracle](../../SWG-Godot-Client/poc/round54-tests/Assets/peer.py) and [isolated replay](../../SWG-Godot-Client/poc/round54-tests/Assets/Invoke-SelectedRule.ps1). See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R54_ORIGINAL_FRACTAL_FILTER_RULE.md), [typed FFRA/consumer evidence](TERRAIN_FILTER_FRACTAL_CONTRACT.md), [accepted owned family binding](TERRAIN_GENERATOR_FRACTAL_FAMILY_CONTRACT.md) and [reviewed AHFR selection discipline](TERRAIN_GENERATOR_HEIGHT_FRACTAL_RULE_CONTRACT.md).

## Identity and selection

`Select(generator, binding, ruleIndex, policy, limits, token)` requires accepted immutable `TerrainGeneratorInput` and `TerrainGeneratorFractalFamilies` descriptors. Policy is mandatory: `TypedEvidence` or `EnabledOperationalSubset`. Negative/out-of-range indexes and unknown policies reject. The caller selects the index; production performs no candidate search.

Before section export, checks establish the exact cached generator SHA-256/length, unique FractalMap role and MGRP/0000 recognition, bound group offset/length/record count, selected FORM FFRA/0005 type and extent, and layer/child identity. `ChildOrdinal` includes the two IHDR/ADTA slots omitted from the layer's `Children`; access subtracts two. Layer containment is provenance, not activation. The immutable accepted binding already verifies group bytes against the exact root; a separately decoded byte-identical generator is accepted without copying the entire root again.

The strict decoder requires IHDR/0000 or 0001 and exactly six LE uint32 PARM words in serialized order: family ID, feather selector, amount, lower, upper, scale. The uint32 family word is reinterpreted as signed int32 and looked up exactly. Missing family, malformed modern input, opaque header or non-0005 version rejects under both policies. Recognized legacy FFRA 0000..0004 remain opaque; recognition does not make them typed.

The immutable result retains generator/group/rule SHA, lengths and offsets, explicit rule index, layer index, child ordinal, owned encoded rule, decoded fields/header, selected family and caller policy. Exact family identity is group SHA plus relative family offset/encoded length/signed ID; `FamilyGeneratorOffset` adds the group offset. The family retains its already owned raw/name storage. Byte, word and name exports are independent copies.

## Evidence and operational policy

`IsEnabled` reports the raw nonzero inherited flag. `HasOperationalInputs` reports necessary input eligibility independently of enabled state. Evidence policy preserves all typed raw fields, including disabled rules and nonfinite or sampler-ineligible values, provided the family exists. Operational policy additionally requires enabled state, finite lower/upper thresholds, finite serialized feather amount and finite sample scale.

The family subset matches frozen modes 0..5 with both curve flags off, signed octave count 1..16, seed other than `int.MaxValue`, and finite used frequency/amplitude multipliers, coordinate scales and offsets. Disabled curve parameter words remain unused. Input eligibility does not guarantee successful normalization or every numeric query.

**Confirmed consumer rules, not extra selector restrictions:** [QueryScaled and Shape](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainFilterFractalInfluence.cs) and the durable FFRA instruction evidence preserve reversed/equal thresholds and return zero when the threshold comparisons classify the sample outside. Unknown feather selectors shape to zero. Finite serialized amount is clamped only by the consumer to [0,1]. The selector does not require lower<upper, selector<=3 or an amount already in range; it performs no repair or clamping. Positive scenarios retain reversed/equal thresholds, unknown selector, negative/large finite amount and zero scale exactly.

## Pre-copy accounting and publication

Default caps: rule 65536 bytes, decoded copy 65536 bytes, name 4096 bytes, snapshot/work 1048576 each. Hard maxima: rule/decoded 4194304, name 65536, snapshot/work 16777216. Synchronous typed decoding uses 64-node/depth 8 bounds.

Before any section copy, reserve `3 * ruleBytes + min(ruleBytes, MaximumDecodedCopyBytes)`: selected export, decoder-owned encoded copy, temporary IFF snapshot, and a conservative typed-data allowance. Exact accounting after decode is `3 * ruleBytes + DecodedCopyBytes`; FFRA decoded copy is 24 bytes plus raw header name/legacy bytes. Work uses the same deterministic byte-processing charge. Existing generator/family storage, future caller exports and CLR object overhead are excluded; these are logical buffer charges rather than measured heap or timing bounds.

Options, extents, input cap and both aggregate reserves are checked before copying. The decoder checks exact name/decoded caps before allocating typed name/words. Cancellation checks bracket copying/decode/resolution/publication; the bounded decoder does not poll every instruction. Failure publishes no partial result or mutates prior results. A 60000-byte synthetic name and zero aggregate selection cap reject while allocating less than 16384 bytes on the calling thread after descriptor/binding construction, testing the pre-copy order.

## Original occurrence and validation

**Observation, Confirmed:** fresh priority 42 Tatooine source metadata selects the first eligible FFRA at generator `Rules[8]`, layer 3, child ordinal 3, TGEN offset 13554, 118 encoded bytes, SHA-256 `038FA063AD5C2B8A10562A0040748B9C0CB247645418F38B5ACAE682409A1136`. It is enabled typed FFRA 0005/IHDR 0001, resolves an exact signed family and satisfies the frozen operational subset. One candidate was inspected; zero exclusions. This is a different occurrence from the older offset 14086 FFRA selected for the FHGT investigation. Raw fields/names and extracted bytes remain private.

**Implementation verification, Confirmed:** 76 independent cases matched, 44 expected rejections and nine ownership/allocation checks. The suite compares actual selected bytes, six decoded fields, inherited header hashes, source/group/rule hashes, extents, signed family identity and budget charges. It covers all legacy versions, malformed modern PARM, wrong binding/index/type/header/policy, disabled/missing-family/nonfinite inputs, each frozen family guard, finite unusual consumer inputs, exact/failed caps, cancellation, borrowed/export mutation and prior-result preservation.

One copied original selected rule and its exact family were evaluated through a frozen native modes 0..5 sampler and retained FFRA consumer only in the test runner. Synthetic inputs were coordinates `(0.125,0.375)`, cell `(0,0)` of a 2x2 cache, and 53-bit table/construction/query/filter precision. Independent Fraction arithmetic matched retained sample, projection, mandatory scaled F32 store, clamped amount, branch, retained query projection/span and final shaped influence. Two cleanup checks verify post-disposal query rejection and retained-sample consumer use after disposal. Sampler disposal is scoped to the test; the production selector never creates it.

The isolated and default Core builds passed with zero warnings/errors; default smoke passed. Workspace/scope validation is recorded in the checkpoint. Original terrain traversal, ancestor activation, active arithmetic controls, original cache/pointer lifetime and arbitrary-coordinate parity remain **Unknown**. Next probe: evidence one original layer filter admission/combination decision before wiring this selected receipt into traversal.
