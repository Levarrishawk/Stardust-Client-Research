# Round 36 BCIR supplied-coordinate influence

2026-10-03. Question: what finite graph does circle query `00B72430` evaluate, how do radius-cache/loading and query phases differ, and how does caller `00B63D20` shape one query result? Stop condition: exact supported finite evaluator with separate phase policies, immutable batch boundary, independent arithmetic/sqrt/native oracles and preserved scope. No original runtime precision or procedural output parity is claimed.

## Inputs and evidence method

Original executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` freshly gates PE/VA use. Cached Python/pefile/Capstone, .NET8 SDK8.0.300 and accepted Fraction arithmetic helpers. Native runner disables shared compiler/apphost, uses cleared offline sources and private outputs. No original process execution, live contact, downloads, GPU, server-formula proof or new asset sweep.

Read START/latest accepted35, AGENTS/current status/ledger/unknown terminology, BCIR input and runtime-policy contracts/checkpoints, accepted34 software arithmetic, relevant curated rows and existing tools. Frozen baseline has2,937 files, source HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`, manifest SHA `766d99456ebd6b3f781e14a2e07122d142ea26d085230349fe402e070aae756e`, ZIP SHA `871bca6403228b05ac7a43fea2942ea4b20c24cc0b5053618a38aad72ea1f0c1`. Every member was preflighted for uniqueness/casefold/size/hash/containment/target-ancestor symlink-reparse before overlay writes, then all files postflighted. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`, empty index.

## Recovered findings and consequences

The [durable contract](../../docs/TERRAIN_BOUNDARY_CIRCLE_INFLUENCE_CONTRACT.md) contains exact graph, API, confidence, policies and counterexample.

- **Confirmed:** loader radius multiply/F32 cache stores at `00B726D2/DB`, `00B7277F/88`, `00B72822/2A`; serialized radius bypasses editing `fabs`. Query differences/squares/sum spill at `00B72449`; outer comparison first, inner-square comparison second, then rounded differences/division/one-minus quotient. Query has no sqrt and returns an unspilled value.
- **Confirmed:** finite feather clamp, equal negative-zero preservation and plain field-store callback. Absent0000/0001 metadata uses directly established fresh base zeros; serialized absence remains nullable in retained fields. No editing state is modeled.
- **Confirmed:** caller's local enable gate occurs before query; selector0 identity,1 square,2 sqrt,3 doubles/subtracts3/multiplies by u twice, invalid U32 selects positive zero after query. Caller F32 store is `00B63FE0`; broader maximum/inversion/filter/ancestor behavior is excluded.
- **Unknown:** active control word in either loading or query phase. Round35 establishes a normal chunk-worker query route but no shared loading/worker policy. Lead review prompted two mandatory explicit precision inputs; no original active default is supplied. Query/shaping is modeled as the uninterrupted selected graph under its supplied nearest policy.
- **Confirmed conditional model:** all arithmetic instructions rounded to chosen24/53/64-bit significand with wide exponents, explicit loader/distance/caller F32 stores, exact dyadic division and integer midpoint sqrt. Independent square-root oracle checks full dyadic result as numerator/denominator as well as F32 bits, so PC64 is not substituted with double.

Cross-phase example: radius/first bits `1F3C0AAB`, center/second zero, amount `3F000000`, selector0, query precision53. Cache24 ->cache `001143F6`, distance `001143F5`, result `359E285B`; cache53/64 ->cache/distance `001143F5`, result zero. This is synthetic, not a proprietary original input occurrence.

## Implementation boundary

New evaluator (external historical reference) exposes direct Query, separate Shape, gated Evaluate and bounded atomic EvaluateBatch. Query owns an immutable unspilled arithmetic value and inspection-only projection; input fields are borrowed immutable values, not original byte buffers. Evaluate preserves disabled raw inputs with no influence result. Negative/zero radius and finite clamp/subnormal/signed-zero cases follow the graph. Modern active finite and cache/distance spill eligibility reject unresolved exception domains; original masked/nonmasked failure behavior is not reproduced.

New arithmetic helper (external historical reference) is internal and scoped to this graph. BigInteger operations preserve per-instruction nearest-even order, exact quotient midpoint rounding and sqrt midpoint rounding with a32-step guard. Reuses the existing precision enum without modifying accepted sources. Batch logical caps/owned snapshots/read-only receipts/copy accessors precede publication; later failures preserve earlier valid output. No world-axis assignment, input-coordinate production, ancestor enable policy, compound boundaries, filters or generated terrain.

## Validation

Runner (external historical reference), the separate implementation record, native scenarios (external historical reference) and identity negatives (external historical reference) pass:

- 46 instruction anchors, four selector jump targets, three constants and exact query F32 spill/no-sqrt observation.
- 15,922 cases:14,820 accepted/1,102 rejected; all three versions, local gate/direct query distinction, selectors, finite clamp/negative radius/degenerate radius/edges, signedzero/subnormal/nonfinite/overflow/invalid precision and deterministic random samples.
- 3,069 exact sqrt vectors across finite F32 exponent bands plus signedzero and nearest-even midpoint ties; independent Fraction/isqrt oracle compared with native exact dyadic numerator/denominator and F32 output.
- 19 batch ownership/caps/alias/retention/opaque/read-only checks and four null negatives. Cache/query phase precision choices differ explicitly;96 phase-sensitive groups,228 query-sensitive groups and348 premature-spill-sensitive cases are present.
- Native build zero warnings/errors; all outputs match independent oracle. Wrong identity and one-byte truncation reject before decode/receipt creation. All original proprietary detail remains ignored privately.

Workspace validator, focused links/whitespace/syntax, status, diff check and all-baseline preservation audit are recorded in the handoff. Existing production/shared/durable files remain unchanged; canonical ledger/status and older contract links belong to the lead. No score/denominator update.

## Precise next question

Recover the source of the supplied first/second coordinates, or one bounded layer boundary-combination step with exact spill/max/inversion order. Keep loading/cache precision separate from query/shaping precision until actual runtime phase values are established. Full generator and original influence corpus evaluation remain outside this task.

Integration repair: the shared SmokeTests scenario uses only public Core APIs and returns its receipt. Exact square-root checks remain in `CircleSquareRootChecks.cs` within the source-linked Rendering runner, preserving the internal production arithmetic helper. The runner composes the unchanged oracle receipt. Both the standalone runner and the actual Core/SmokeTests project-reference build pass with zero warnings or errors; all vector counts are unchanged.
