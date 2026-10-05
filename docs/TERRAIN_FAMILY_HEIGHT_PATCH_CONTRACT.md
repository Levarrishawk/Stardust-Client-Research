# Controlled family-backed height patch

[TerrainFamilyHeightPatch](../poc/Stardust.Core/Geometry/TerrainFamilyHeightPatch.cs) publishes one immutable rectangle of actual computed F32 heights using the frozen [family-routed AHFR](TERRAIN_FAMILY_HEIGHT_FRACTAL_CONTRACT.md). It evaluates one explicitly supplied enabled rule over an owned family set. This is a modern height-only composition with supplied controls, not a recovered terrain generator or original traversal policy.

## Request and coordinate policy

`Build(families, ruleEncoded, request, controls, limits, cancellation)` accepts the immutable family snapshot, borrowed encoded AHFR bytes, a scalar request and all four frozen route precision controls. `Width` and `Rows` count samples, not tiles or padding. Each dimension is 2..128; the default complete-sample cap is 256 and hard cap is 1024. The rule, initial height and positive influence are uniform over the rectangle. Rule eligibility and exact signed family selection are inherited unchanged from the frozen route, including explicit rejection of disabled, missing, opaque and unsupported inputs.

The request supplies finite F32 origin-X, origin-Z, spacing-X, spacing-Z, initial-height and influence bits. Both spacings and influence must be strictly positive. Every stored axis and resulting height must be finite and within one million in magnitude. Axes must be strictly increasing after storing to F32; a spacing that collapses adjacent values rejects before the first route evaluation.

Coordinate production is explicitly authored: for index i, compute and separately store `offset=F32(i*spacing)`, then `axis[i]=F32(origin+offset)`, using nearest-even binary24 operations with F32 storage after each operation. The index is exact within the bounded dimensions. There is no accumulated stepping, fused multiply-add or inferred original coordinate routine. Negative-zero origin plus positive-zero offset follows this specified arithmetic graph. Native X increases with column, native Z with row, and native Y is the final height; original world-axis/unit equivalence remains Unknown.

Sample index is `row*Width+column`. Every sample invokes `TerrainFamilyHeightFractal.Evaluate` with a fresh 1x1 caller cache, cell(0,0), the generated X/Z words and explicit table/construction/query/consumer controls. Every returned sample is therefore an actual `ConditionalComputedMiss`; there is no cache reuse across neighboring or repeated samples. The retained sample object flows unchanged to AHFR before its final F32 height store. Each frozen route disposes its local sampler before returning or throwing.

## Output, bounds and adapter handoff

The output retains the request and controls, family-set and rule SHA-256, owned encoded rule, read-only X/Z axes, row-major height words and one actual immutable route receipt per sample. Those receipts preserve family/raw/rule identity, request, arithmetic controls, retained sample, consumer result and miss provenance. Axis/height/rule export methods return copies. No caller byte span or mutable array is exposed.

`Bounds` records the first/last ordered X/Z words and minimum/maximum computed height words. Numeric ties retain the first row-major height occurrence; signed-zero bits follow that value. Bounds describe these sample points, not a collision enclosure or estimated unsampled terrain extrema.

This API emits no positions, normals, materials, colors, triangle indices, seams or engine objects. An adapter may form native positions `(X[column],height[index],Z[row])` and explicitly supply its other channels to the accepted [assembly](TERRAIN_PATCH_ASSEMBLY_CONTRACT.md). For a simple rectangular quad with a=upper-left sample, b=next column, c=next row and d=next row/column, order `(a,c,b)` and `(b,c,d)` gives positive-Y orientation in these native axes. This is an explicit adapter convention; it does not replace accepted nine-sample topology/seam rules. Godot's accepted Z reflection/index handling remains the adapter's responsibility. The standalone CPU probe supplies modern up normals/color to frozen assembly and surface-query APIs for one eligible 3x3 result; no host or GPU change is included. Additional adapter projected-area and channel eligibility still apply, including for very small accepted spacing.

## Aggregate limits and atomicity

Limits are nonnegative, with hard maxima: samples 1024, rule bytes 65536, copy bytes 4194304, aggregate route bytes 268435456 and work 67108864. Default rule/copy/route/work caps are 65536/1048576/134217728/16777216. Input rule length must be at least 24. Dimensions, scalar fields, controls, declared counts, rule extent and the complete aggregate reservations are checked before input copying or the first expensive route evaluation.

For W columns, R rows, N=W*R and rule length L, the complete logical reservations are:

- Copy bytes: `L+4*(W+R+N)+8*N+256`, covering one owned rule, axes/heights, receipt references and a fixed header allowance.
- Per-route bytes B: `8*L+65536+9512+136+32+160+16*80+256`, the frozen route allowance with exactly one cache cell.
- Per-route work T: `L+20000+16*16+1+16*512+64+32`.
- Aggregate route bytes: `N*B`; total work: `L+32*(W+R)+N*(T+16)`.

The per-route byte/work budgets B/T are passed explicitly to the frozen route. Every sample is reserved even though construction is sequential and sampler resources are released between samples. These are conservative logical payload/work allowances, not measured CLR heap, peak RSS or elapsed-time limits. The preowned family snapshot, runtime object overhead, unrelated allocations and caller-requested export copies are excluded.

All axes are checked before sampling. Intermediate arrays and receipts remain local until every sample passes final height eligibility and the final cancellation check. A late route overflow, late display-height rejection or cancellation returns no patch or prefix and mutates no family, prior patch or external state. Cancellation is cooperative: checks occur before copies, during axis production, before each sample, within frozen route construction/query and before publication. Callers must not mutate the borrowed span concurrently during the synchronous snapshot.

## Verification and evidence

The [independent verifier](../poc/round52-tests/Rendering/verify_patch.py) generates private original-family and authored synthetic fixtures. It uses exact Fraction-based F32 axis arithmetic and the frozen independent kernel/mode/AHFR models to compute expected complete heights and retained sample provenance. Expected samples are never supplied to native evaluation. Original EXE SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` is gated before reliance on that component chain; the original executable is never executed.

151 cases pass exact native agreement:106 accepted and 45 rejected, with 2211 computed heights. There are 88 accepted calls using the original first family under supplied synthetic rules/coordinates/controls, and 18 limited synthetic cases across six modes. Original calls include all 81 four-control profiles, exact display/copy/route/work boundaries, default 256/hard 1024 sample counts and dimension 128. Rejections include malformed/opaque/disabled/missing rules, unsupported raw fields, nonfinite/nonpositive values, collapsed/out-of-range axes, dimensions and aggregate caps, and pre-cancellation. Independent late failures occur at row-major indices 1 and 2 after valid prefix samples: route F32 overflow and finite height outside the display cap.

[Native scenarios](../poc/Stardust.Core.SmokeTests/TerrainFamilyHeightPatchScenarios.cs) perform 64 ownership/atomicity/surface assertions, including unchanged prior results after every failure, source/export mutation, immutable wrappers, retained sample identity, fresh repeated calls, a separate calling thread and five known-height surface queries. The fresh frozen 203-case family-route regression also passes. Actual Core-reference builds report zero warnings/errors. There is no timing-based claim about the precise cancellation checkpoint reached by the pre-cancelled fixture.

Reproduce with [Invoke-Patch.ps1](../poc/round52-tests/Rendering/Invoke-Patch.ps1), explicit cached Python/.NET/package paths, read-only EXE/asset/config paths and a fresh ignored output root. The runner rejects existing, nonprivate or linked output ancestors before creating output. Source/generated original-derived bytes remain private; no new canonical registry or coverage denominator is introduced.

**Confirmed:** the tested modern rectangle, aggregate preflight, complete computed route handoff, immutable ownership and atomic rejection. **Strongly inferred:** finite conditional component compatibility inherited from the executable-backed frozen route. **Unknown:** original traversal, active controls, cache/streaming/reload policy, optional curves/sine, normal/material generation and whole-terrain parity. Next action: lead review and connection of this height result to the existing surface/Godot adapter with explicitly supplied normal/material/seam policy, then independently verify reflection, winding and disposal.

## Round53 opt-in sampling policy

[BuildSingleSampler](TERRAIN_SINGLE_SAMPLER_HEIGHT_PATCH_CONTRACT.md) adds one
fresh sampler per complete rectangle, truthful full-shape unique-cell receipts
and separate shared/cell accounting. The historical Build boundary and all151
cases/2211 heights above remain unchanged. The new156-case suite preserves
retained samples and atomic publication; persistent cache and original traversal
remain separate. Lead Version5 original-rule headless/GPU joins pass.
