# Explicit TGEN height-fractal rule selection

Round 53, 2026-10-04. This engine-neutral boundary selects exactly one caller-supplied index in an immutable generator's `Rules` list and binds its typed AHFR/0003 to an exact signed family ID. It does not traverse or activate layers, construct a sampler, read assets, mutate caches, or supply a fallback.

Implementation: [TerrainGeneratorHeightFractalRule](../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainGeneratorHeightFractalRule.cs). Reproduction: [isolated runner](../../SWG-Godot-Client/poc/round53-tests/Assets/Invoke-SelectedRule.ps1), [independent oracle](../../SWG-Godot-Client/poc/round53-tests/Assets/peer.py), and [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainGeneratorHeightFractalRuleScenarios.cs). See the [bounded investigation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R53_ORIGINAL_HEIGHT_FRACTAL_RULE.md), [owned generator](TERRAIN_GENERATOR_INPUT_CONTRACT.md), [family binding](TERRAIN_GENERATOR_FRACTAL_FAMILY_CONTRACT.md), and [AHFR evidence](TERRAIN_HEIGHT_FRACTAL_CONTRACT.md).

## Selection and identity

`Select(generator, binding, ruleIndex, policy, limits, token)` requires an already accepted owned `TerrainGeneratorInput` and `TerrainGeneratorFractalFamilies`. The caller must explicitly select either `TypedEvidence` or `EnabledOperationalSubset`. Negative or out-of-range indexes and unknown policies are rejected. No first-match search occurs in production.

Before copying, the selector checks cached generator SHA-256 and length against the binding; the unique FractalMap role, FORM MGRP/0000 recognition, group offset, length and record count; the FORM AHFR/0003 rule extent; and the rule's layer and child metadata. `ChildOrdinal` includes the inherited header and ADTA slots, while the layer's `Children` starts after these two slots. Containment identifies the owner; its enabled state does not activate or suppress this selected rule.

The source descriptors and accepted binding are immutable, so identical generator hashes and lengths bind the group's already verified bytes. A separately decoded byte-identical generator is eligible. The selector does not copy or hash the entire generator again.

After strict typed decoding, the uint32 family key is reinterpreted as a signed int32 and resolved exactly. Missing families, opaque AHFR versions, unrecognized inherited headers and malformed modern layouts are rejected under both policies. Accepted family extents remain inside the exact bound group. The returned immutable family object is retained from the accepted owned binding; this does not borrow caller byte arrays.

The result retains generator and group SHA/length/offset, rule SHA/length/offset, explicit rule index, layer index, child ordinal, owned encoded and decoded rule, inherited header, family identity and caller policy. Family provenance is the bound group hash plus relative family offset, encoded length and signed ID. `FamilyGeneratorOffset` adds the group offset. Byte, name and raw-word exports remain independent copies.

## Raw state and caller policy

`IsEnabled` reports the raw nonzero inherited enabled word. `HasOperationalInputs` reports necessary input eligibility independently of that word. `TypedEvidence` accepts disabled rules and ineligible operation/family inputs once the modern structure and exact family are known. It preserves their raw bits. `EnabledOperationalSubset` additionally requires an enabled header and eligible inputs.

Eligibility matches the frozen height route: operation 0..3, finite scale, family mode 0..5, both curve flags zero, signed octave count 1..16, signed seed other than `int.MaxValue`, and finite frequency/amplitude multipliers, both coordinate scales and both offsets. Disabled curve parameters are unused. Eligibility does not establish successful normalization or sampling for every coordinate.

This API supplies no implicit operational policy, no whole-layer or ancestor activation, no original traversal order, and no original cache/invalidation/thread guarantees. The immutable selector's caller controls these choices.

## Budgets and atomic publication

Options are validated before copying. Defaults: selected rule 65536 bytes, typed decoded copy 65536 bytes, name 4096 bytes, logical snapshot and work 1048576 each. Hard maxima are rule/decoded 4194304, name 65536, snapshot/work 16777216. The decoder has fixed 64-node and depth 8 bounds.

Before the first section export, the selector reserves `3 * encodedRuleBytes + min(encodedRuleBytes, MaximumDecodedCopyBytes)`. The three encoded charges cover the selected temporary export, decoder-owned snapshot and temporary IFF snapshot. The decoded upper bound is conservative; actual accounting is `3 * encodedRuleBytes + DecodedCopyBytes`. Work uses the same byte-processing charge. These are deterministic logical byte charges, not measured CLR heap usage, timing or object overhead. Existing generator/family storage and later caller exports are outside this incremental selection budget.

Encoded limits and both aggregate reservations are checked before copying. The typed decoder enforces exact decoded/name caps before typed field/name publication. Cancellation is checked before copies, between decode and family resolution, and before returning; the bounded synchronous decoder does not poll inside every instruction. Failure publishes no result and leaves previous results intact.

## Original occurrence and verification

**Confirmed observation:** existing pinned raw metadata identifies the first original Tatooine AHFR at generator rule index 0, layer 0, child ordinal 2, TGEN offset 12346, encoded length 114 and SHA-256 `51735AECA9C172F0BD1E8194ACBCECD8CEF03041075CF307822E275D5E936B1E`. It is typed 0003 with IHDR 0001 and 33 decoded-copy bytes. Fresh archive reads under the pinned priority 42 overlay confirmed the same occurrence. One candidate was inspected and selected; zero earlier candidates were excluded. Its raw rule and resolved family meet the explicit enabled operational policy. Original names, field words and encoded corpora remain private.

**Confirmed implementation result:** 59 independently compared cases, 37 expected rejections, 9 ownership checks, 96 static instruction anchors and 8 executable-identity negatives passed. Exact raw selected bytes, decoded/header fields, offsets, hashes and signed family provenance matched the independent parsers. Tests include signed key extremes, second explicit rule selection, wrong binding/index/type/version, unknown inherited header, malformed PARM, disabled rules, missing families, each operational family guard, exact and failing caps, cancellation, export/borrowed-input mutation and prior-result preservation.

One copied original selected rule was passed to the frozen native fresh height route and matched its independent retained-fraction and output-bit oracle. The test supplied synthetic coordinates `(0.125, 0.375)`, initial height `-0.25`, influence `0.75`, a 2x2 cache cell `(0,0)`, and 53-bit table/construction/query/consumer controls. This is a bounded replacement-code calculation, not execution or parity validation of the original executable. Original traversal, active controls and ancestor activation remain **Unknown**.

A 60000-byte synthetic rule name with zero aggregate selection budget also rejected while allocating less than 16384 bytes on the calling thread, after descriptor/binding construction. This guards against copying the large selected rule before the preflight rejection. The isolated build and full default Core smoke suite passed. Scope and workspace validation are recorded in the checkpoint. No function-name or protocol registry is changed by this implementation boundary.


## Round54 accepted increment

The [selected original filter](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md) and [per-cell field](TERRAIN_FAMILY_HEIGHT_FIELD_CONTRACT.md) pass independent replays. Lead Version6 composes selected root AHFR and layer3 FFRA under explicit synthetic coordinates, prior heights and controls;400 native/headless/GPU cells, mixed/zero-state checks and late zero-visual-allocation rejection pass. Original layer admission, traversal and active controls remain Unknown.
