# Modern height-patch composition contract

ROUND60, 2026-10-04. This is a bounded engine-neutral compatibility prototype with an explicitly modern normal policy. It establishes no original normal, material, control, or numerical parity and introduces no operational rendering path.

## Inputs and retained result

[TerrainModernHeightPatch.Build](../poc/Stardust.Core/Geometry/TerrainModernHeightPatch.cs) accepts an actual immutable `TerrainCoordinateHeightPatch`, mandatory `TerrainModernNormalPolicy.ModernBinary64CentralDifference`, optional composition/child limits, and cancellation token. The result retains the exact `Source`, actual frozen `TerrainModernHeightNormals` as `NormalField`, and actual frozen `TerrainCoordinateHeightPatch` as `Patch`. Both parity properties are false. Every successful receipt reports one normal build and one final patch build.

The normal child reads the source's stored position X/Z axes and stored position heights. The final child receives `Source.CopyInputHeightBits()`, the full row-major ARGB snapshot, actual derived normal snapshot, same coordinate grid, seam mask, and UV scale. Passing stored heights to that final builder would add the vertical origin twice. Raw heights and stored heights remain distinct and are preserved bit for bit. Final selected normals are the actual normal-field values at the selected source indices. Positions, triangle indices, source indices, seam patterns, UVs, full colors, and selected RGBA conversions retain the frozen source-builder behavior.

[The frozen normal implementation](../poc/Stardust.Core/Geometry/TerrainModernHeightNormals.cs) uses binary64 centered secants on the stored axes and one-sided secants at the full-grid edges, normalizes `(-gx,1,-gz)`, and exports binary32 vectors. Composition changes no algorithm or engine adapter. A final winding failure rejects the candidate rather than returning a partial result.

## Bounds and charges

All count, option, limit-range, child-count, child-resource, and aggregate-resource checks precede source snapshot exports and either child build. Width W is 2 through 32, N is W squared and at most 1024. The source must contain its complete padded active region. P is tiles squared; selected samples are 9P. Default parent patch/selected caps are 64/576; hard caps are 256/2304. Source mask is 0 through 15 and UV scale finite, positive, and at most 16. Stored position axes must be finite, within magnitude 1,000,000, and strictly increasing. Raw height magnitude is bounded by 1,000,000; stored heights must be finite, with the frozen normal child's eligibility checks retained.

Successful logical charges follow this table. They describe bounded accounting, not measured managed heap sizes or recovered legacy allocation costs.

| Stage | Copy bytes | Array bytes | Work units |
|---|---:|---:|---:|
| Composition-owned | 36N | 256 + 36N | 128 + 4W + 16N + 4P |
| Actual normal child | 8W + 4N | 256 + 8W + 16N | 128 + 4W + 80N |
| Final child conservative reservation | 20N + 1024P | 256 + 36N + 2048P | 256 + 96N + 4096P |
| Aggregate | 60N + 8W + 1024P | 768 + 8W + 88N + 2048P | 512 + 8W + 192N + 4100P |

The owned copy charge includes source raw/stored/color snapshots, the normal export, and final raw/stored/color preservation exports. Parent default copy/array/work caps are 1,048,576 / 2,097,152 / 4,194,304; hard caps are 4,194,304 / 16,777,216 / 33,554,432. Normal child limits remain independently enforced. The frozen final builder supports grid/patch/selected count caps only; it has no byte/work caps or cancellation hook. Parent reservations and pre/post cancellation checks bound and contain that synchronous stage, but cannot interrupt it. `FinalBuilderSupportsCancellation` is false.

Borrowed immutable axis metadata is inspected before exports. All public copy helpers return detached arrays. No source mutation, cache publication, engine object, GPU object, adapter call, or partial candidate escapes on failure. Existing immutable source and prior receipts remain valid.

## Reproduction and evidence

[The isolated runner](../poc/round60-tests/Rendering/Invoke-Patch.ps1) requires explicit Python, .NET, executable, cached-package, and fresh ignored-output paths. It verifies the static executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` before model imports, output allocation, or managed builds. It rejects public/existing/linked/dangling-linked/non-directory-ancestor output paths, clears package feeds, and builds actual Core from source with cached dependencies. AssetRoot and Config parameters retain the runner interface; this composition oracle uses authored fixtures and does not consume assets or configuration.

[Independent fixtures and verification](../poc/round60-tests/Rendering/verify_patch.py) reuse frozen Fraction coordinate/topology evidence and independently compute 100-digit Decimal normals. Expected normal values are not supplied to the native runner. [Native scenarios](../poc/Stardust.Core.SmokeTests/TerrainModernHeightPatchScenarios.cs) construct actual frozen sources and both actual child results.

Four fresh replays pass; the final replay contains 335 cases: 287 accepted, 48 expected rejections, 9,296 full-grid normal samples, 536 patches, and 4,824 selected samples. Raw/stored heights, colors, positions, indices, source selections, pattern, RGBA, UVs, and charges compare exactly; independent normal components use absolute tolerance 3e-6. Native selected normals compare bit for bit to actual derived exports. Coverage includes flat/sloped/wave fields, nonuniform axes, independent coordinate precision phases, signed zero, all 16 seam masks, caps, repeated and concurrent calls, precancellation, and late source-height rejection. The final replay adds five explicit adjacent active padded-seam comparisons. It executes 592 ownership checks and 18 double-origin witnesses.

Nonzero-origin fixtures are explicitly synthetic supplied metadata: the frozen public coordinate producer emits zero vertical origin. Test-only reflection constructs the actual frozen grid with the generated axes/policies and authored origin, then uses the actual frozen source builder. This does not establish an original occurrence or inject normal values. A deliberately wrong stored-as-input reference demonstrates changed heights for each of the 18 nonzero numeric origins; the correct composition preserves both channels.

## Confidence and next boundary

Confirmed: bounded composition behavior, actual child/source identity, ownership, preflight accounting, bit-preserved geometry/color channels, and tested failure containment. Strong evidence: modern numerical output against an independent high-precision reference within the stated tolerance. Unknown: original normal/material/control behavior, portable binary64 bit identity, and cancellation inside the frozen final builder. No GPU or lit-preview result is claimed.

Next probe: a separately authorized engine consumer may retain this receipt and provenance while displaying the exact final geometry. Original normal admission requires separate executable evidence and authoritative fixtures; modern tolerance agreement cannot close that gap.
