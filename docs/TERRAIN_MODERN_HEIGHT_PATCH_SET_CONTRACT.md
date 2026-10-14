# Modern supplied height-patch set contract

ROUND61, 2026-10-05 Eastern. [TerrainModernHeightPatchSet](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainModernHeightPatchSet.cs) admits a supplied list of actual frozen [modern height-patch receipts](TERRAIN_MODERN_HEIGHT_PATCH_CONTRACT.md). It retains each exact receipt and caller order without rebuilding normals or geometry. This is modern bounded admission; original terrain, normal, material, and arithmetic-control parity remain unclaimed.

## API and policies

`Build(ReadOnlySpan<TerrainModernHeightPatch> supplied, TerrainModernPatchEdgePolicy edgePolicy, TerrainModernPatchNormalPolicy normalPolicy, TerrainModernHeightPatchSetLimits? limits=null, CancellationToken cancellation=default)` returns an immutable owned list. `Patches` is read-only and `CopyPatches()` returns a detached reference array. The actual candidate objects, Source, NormalField, and final Patch identities remain intact. `NormalBuilds` and `GeometryBuilds` are zero; neither child factory is called.

The caller chooses both policies explicitly:

| Edge policy | Normal policy | Admission |
|---|---|---|
| IndependentSuppliedPatches | IgnoreSharedNormals | Validate identity, bounds, budgets, unique requests, and nonoverlapping interiors; no shared-channel claim |
| RequireBitIdenticalActiveSharedEdges | IgnoreSharedNormals | Require matching shared positions, stored height words, and complete overlap sample spacing |
| RequireBitIdenticalActiveSharedEdges | RequireBitIdenticalSharedNormals | Additionally compare each actual derived normal's three F32 words |

Independent mode with required normals and unknown policy values reject. There is no implicit tolerance, snapping, averaging, fallback policy, normal modification, or input repair. Normal comparison is optional because padded stencils can differ even when active heights agree.

A unique chunk-request key includes all six frozen request fields: width word, tiles, margin, First, Second, and Scale. Profiles, mask, and UV are not additional key fields. Repeated keys reject in both modes. Physically overlapping active rectangle interiors also reject in both modes, even if request keys differ.

## Physical boundaries and exact words

Each active axis spans `SampleBorder` through `SampleBorder + 2*CellsPerSide`, inclusive, in the actual source's stored position axes. Physical adjacency uses numeric F32 coordinates, independent of caller ordering or request index order. All finite stored axes must be strictly increasing. Reversed list order and mixed precision profiles are supported when their actual boundaries satisfy the chosen policy.

For a shared edge with positive overlap length, strict mode requires both overlap endpoints and the identical ordered set of sample coordinate words across the entire overlap. Partial overlaps are supported when both grids contain every matching overlap sample. Different spacing, missing overlap endpoints, or extra unmatched samples reject. For a corner-only contact, strict mode compares that corner's position, height, and optional normal words. `SharedEdgePairs` and `SharedCornerPairs` are separate; `SharedSampleComparisons` counts comparisons per pair, including edge endpoints and corner contacts. A central point can therefore contribute to several pair comparisons.

Height comparison uses the final patch's stored position-height raw F32 words, not raw input heights or an unrounded calculation. Positive and negative zero height words are distinct; `SignedZeroHeightWordsAreDistinct` is true. Strict position and normal comparisons also distinguish their signed-zero words. Numeric coordinate equality only identifies contact; it does not waive the subsequent word comparison. Disconnected supplied chunks and missing neighbors are allowed: the API has no requested region or implicit completeness requirement. The empty list is valid when its policies and budgets are valid.

## Capture, identity, and failure

Initial borrowed-input preflight validates limits, actual Source/NormalField/final-grid identities, shape, mask/UV consistency, axes, totals, reservations, request uniqueness, and interior overlap before the reference-list copy. The captured owned list then undergoes the complete preflight again, before height snapshots or shared-channel inspection. Captured reservations must equal the initial deterministic plan. Changed or invalid captured candidates reject. Capture is not an atomic operation against arbitrary concurrent caller mutation; the guarantee concerns the actual admitted owned list. Every published list obeys the stated caps and pair constraints.

Strict mode copies each captured final patch's full stored-height channel for comparison. Normals and axes are borrowed through frozen read-only children. Copies of the result list cannot alter membership; source receipts remain untouched. Precancellation, validation failure, shared mismatch, and later cancellation return no set and preserve prior sets. Cancellation checkpoints occur in each pass, pair, export, and shared point, and before publication. No child builds execute, so no cancellation behavior inside existing child factories is claimed.

## Limits and accounting

Hard/default count bounds are 16 chunks, axis width 32, 4,096 total full-grid cells, 256 total patches, and 2,304 selected samples. Each receipt retains the frozen complete padded extent and axis eligibility. Count caps may be reduced to zero. Every limit value is validated even for an empty list.

Let K be chunks, N the sum of full-grid cells, S the sum of axis widths, W the maximum width (zero for empty), Q=K(K-1)/2, and H=4N in strict mode or zero in independent mode. Successful logical reservations are:

| Owned admission charge | Formula |
|---|---:|
| Copy bytes | 8K + H |
| Array bytes | 256 + 128K + H |
| Work units | 128 + 128K + 16S + H + Q(256 + 64W) |

Array charges conservatively reserve reference/jagged-array overhead. Work includes both metadata/pair preflight passes and a worst-case active-edge scan with optional normal comparisons. Complexity is O(K squared + sum of axis widths + inspected active-edge samples, including overlap endpoint searches), plus strict full-height snapshot copying. These are declared logical reservations, not measured heap sizes or CPU instructions. Parent caps are checked before the initial list copy and again for the captured inputs before height exports.

Owned default copy/array/work caps are 262,144 / 1,048,576 / 2,097,152; hard maxima are 1,048,576 / 4,194,304 / 16,777,216. Three separate borrowed reservation properties sum the existing receipts' aggregate copy/array/work reservations. Their default caps are 4,194,304 / 8,388,608 / 16,777,216; hard maxima are 16,777,216 / 33,554,432 / 67,108,864. They describe retained child provenance and conservative historical reservations. They are neither new executed child work nor measured live memory, and are not added to owned admission work.

## Evidence and reproduction

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainModernHeightPatchSetScenarios.cs) construct actual frozen receipts, retain their identities, mutate caller/exported lists, verify read-only membership, exercise repeated/concurrent calls, cancellation, foreign-source identity, and late mismatch preservation. Test-only reflection authors signed-zero source metadata and a deliberately foreign receipt; production uses no reflection.

[The independent oracle](../../SWG-Godot-Client/poc/round61-tests/Rendering/verify_set.py) uses frozen Fraction coordinate/storage calculations, independently enumerated physical active point maps, and 100-digit Decimal normal references. Exact admission and accounting results agree for 272 cases: 218 accepted and 54 rejected, using 227 actual receipt fixtures. Normal reference components use absolute tolerance 3e-6 only for checking the frozen normal algorithm; the production admission policy always uses exact words. Cases include all 81 coordinate-phase profiles, all 16 seam masks, permutations, incompatible spacing, partial and missing endpoint samples, corner mismatch, differing padded normals, signed zero, empty/disconnected sets, count/resource equality and one-less/negative/hard limits. There are 998 ownership checks and 256 bounded concurrent caller-list churn attempts. Every accepted captured list satisfies independent cap/key/interior/identity checks; timing-dependent acceptance counts are not a coverage claim.

One SHA-pinned sealed four-chunk original-layer modern snapshot is rebuilt through the actual frozen height evaluation and normal/patch factories as test setup. All 400 height and 400 normal values match the accepted snapshot. Set admission then retains these four receipts with strict height and normal policies: four edges, two diagonal corners, and 22 exact shared comparisons. The snapshot includes synthetic warm priors and explicit modern first-curve arithmetic; this does not establish original seam or normal parity. No policy is relaxed to admit it.

[The offline runner](../../SWG-Godot-Client/poc/round61-tests/Rendering/Invoke-Set.ps1) requires explicit cached dependencies, SHA-pinned executable and sealed private fixture, and a fresh ignored output path. Both input identities are checked before output/model/build work. Nine negative output/input guards pass, including linked and dangling ancestors, prior receipt preservation, and wrong sealed fixture identity. Actual Core/native builds have zero warnings and errors. No engine, GPU, host, shared-file, registry, or sibling61 changes occur.

Confirmed: bounded retained-list identity, exact pair policies, accounting, owned capture validation, and tested failure preservation. Unknown: original terrain/normal/material/control parity and any global layout/connectivity requirement. Next probe: a separately authorized consumer may retain this set, preserving its explicit policies and original-parity limits.

## Round62 actual query consumer

A separate [surface query](TERRAIN_MODERN_PATCH_SET_SURFACE_QUERY_CONTRACT.md) retains the actual admitted set and constructs ordered child queries without geometry/normal rebuilding. The replacement host now displays and grounds through this consumer. Closed-edge first-supplied order is explicit; original collision semantics remain unclaimed.
