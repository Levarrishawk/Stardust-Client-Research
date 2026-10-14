# Supplied-circle height patch contract

TerrainCircleHeightPatch (external historical reference) composes the accepted [circle evaluator](TERRAIN_BOUNDARY_CIRCLE_INFLUENCE_CONTRACT.md), [constant-height evaluator](TERRAIN_HEIGHT_CONSTANT_OPERATION_CONTRACT.md), and [patch assembly](TERRAIN_PATCH_ASSEMBLY_CONTRACT.md) over supplied synthetic coordinates. This is one boundary and one affector. Full TGEN, boundary maxima/inversion, ancestor activation, filters, original coordinate production, materials, collision and terrain streaming remain outside the slice.

## Explicit compatibility and modern boundaries

The circle loading/cache, circle query/shaping and height arithmetic precisions are three mandatory independent `TerrainHeightArithmeticPrecision` inputs. Each is Binary24, Binary53 or Binary64 with nearest-even rounding. The accepted arithmetic graphs and F32 spill points are preserved by calling their public APIs. The shaped circle influence is already F32 before being supplied to AHCN. This direct single-boundary handoff is an explicit synthetic composition policy; it does not establish the original layer's intervening products or activation rules. Original active precision, directed rounding and exception/trap behavior remain Unknown; see [runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md).

The local display convention is explicitly **modern**: X is the first supplied circle coordinate, Y is the resulting height, and Z is the second supplied coordinate. No original world-axis/unit mapping is claimed. First/second axes are supplied raw F32 words; they are not generated with host arithmetic. Axis values must be finite, strictly increasing and within one million in magnitude.

A disabled circle returns the accepted skipped circle receipt with null influence. The composition preserves the raw base height and records no AHCN invocation (`Height=null`). An enabled circle passes its exact influence word to AHCN, including positive zero from an invalid selector. Disabled AHCN and nonpositive influence retain the accepted height behavior. Every grid output must additionally be finite and within one million for display geometry, even if the arithmetic kernel skipped. Thus lossless disabled kernel receipts do not imply that nonfinite display geometry can be rendered.

Modern inspection normals are constant `(0,1,0)` and modern colors are constant opaque ARGB `FF4080C0`. These are unshaded inspection choices; they do not reproduce terrain slope normals, recovered terrain lighting, or original color generation. Strictly increasing X/Z axes yield upward source triangle orientation. Existing assembly/visual eligibility rejects degenerate or numerically tiny triangles. No normal smoothing or original material parity is claimed.

## API, order, limits and ownership

`Build(cellsPerSide, firstAxisBits, secondAxisBits, inputHeightBits, circle, height, cachePrecision, queryPrecision, heightPrecision, seamMask=0, uvScale=1, limits=null)` takes immutable decoded rule objects and borrowed spans. An N by N patch region has width `2*N+1`, equally sized first/second axes and exactly width-squared base heights in row-major order. There is no padding border. Seam mask0..15 and UV scale greater than zero through16 feed the accepted assembly without alteration.

Configuration, hard limits, declared counts, shapes and decoded-circle eligibility are checked before snapshots. Default caps are64 patches and289 grid samples, permitting N=8; hard caps are1024 patches and4225 samples, permitting N=32. Positive N, positive patch cap and sample cap at least9 are required. Counts and work are bounded before allocating. A smaller explicit cap rejects before computation. Arbitrary supplied rectangle grids and zero-size outputs are unsupported.

The builder snapshots all three spans, validates both axes, then evaluates row-major samples. Each immutable sample receipt holds supplied coordinate/base bits, the circle receipt, optional AHCN result and final height bits. All samples precede patch assembly. Patches are assembled in row-major cell order using the accepted nine sample offsets and seam branch priority. The complete immutable result is returned only after every cell passes geometry/winding checks. Failure changes no input, earlier returned result, engine object or external state.

`Samples` and `Patches` are read-only wrappers over inaccessible owned arrays. Geometry and source-index inspection return copies; `CopyHeightBits` returns a fresh array. Rule inputs are immutable borrowed values; circle query receipts remain immutable. Aliased axes/heights are safe because all snapshots precede evaluation. Concurrent caller mutation during a call is outside this synchronous API contract. Retained results and caller-requested copies are not included in per-call logical caps.

Work is bounded by width-squared accepted circle/AHCN operations and N-squared fixed nine-sample assemblies. Temporary storage includes three input copies and full sample/position/normal/color grids. Retained storage includes one receipt per grid sample, one assembly per patch and bounded geometry/index copies. These are logical caps, not measured CLR heap/RSS limits.

## Evidence and confidence

| Observation | Interpretation / confidence | Implementation consequence |
| --- | --- | --- |
| Accepted36 `00B72430` query and `00B63D20` selector graph, fresh radius-cache stores, confirmed46 static anchors. | Confirmed conditional circle behavior; active original precision Unknown. | Use the accepted evaluator and two independent supplied policies. |
| Accepted34 `00B76540` arithmetic order and caller's F32 supplied-influence store;21 process/five caller anchors. | Confirmed conditional AHCN graph. | Pass the already stored circle word, with a third explicit profile. |
| Accepted33 `008618D0` nine offsets and `00865FF0` seam priority; accepted32 topology and prototype host reflection. | Confirmed supplied assembly/adapter behavior. | Reuse the public assembly and visual; preserve index order under Z reflection. |
| This builder's grid coordinates, one-circle handoff, up normals, fixed color and finite display caps. | Modern authored policy, not executable research. | Keep these choices labeled synthetic; no original terrain claim or coverage increase. |

Baseline executable SHA-256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The independent verifier (external historical reference) replays accepted SHA-first circle and AHCN static checks, then uses exact Fraction/isqrt models for influences and height words. It independently models sample selection, seam priority and literal topology rings. Synthetic IFF fixtures use no original asset bytes.

## Replacement preview and validation

TerrainCircleHeightPreview (external historical reference) uploads the built cells through the accepted `TerrainPatchVisual`, preserving its Z reflection, original indices, back culling and separate UV/COLOR channels. Its scene (external historical reference) expects an explicit synthetic fixture file. The independent standalone harness (external historical reference) avoids the shared prototype host build.

CPU verification covers all27 precision triples, circle selectors0..3 and invalid selectors, seven AHCN operations, all seam masks, grids1/2/3, all serialized circle versions, disabled rules, signed-zero/negative/zero radius, feather clamp and nonfinite/overflow rejection. Exactly1199 cases pass the independent comparison:1192 accepted and7 rejected,29928 accepted grid samples and4800 exact patches. Ownership/cap tests include exact default and hard maxima, aliases, late failure, retained results and read-only/copy boundaries.

Actual Core/project-reference CPU and combined Core/SmokeTests builds verify assembly visibility with no internal arithmetic access. Headless readback verifies four patches/25 samples and4 allocations/disposals. Hidden GPU verifies the same four patches plus one probe, four exact front UV/color samples and four culled-back samples,5 allocations/disposals, and a private640x480 capture. Only the known pre-state certificate-store diagnostic is allowed; post-state warnings/errors reject. The fixed shader intentionally contains one black front quadrant, which the pixel oracle checks exactly.

Reproduce with Invoke-Patch.ps1 (external historical reference), explicit cached historical implementation path (omitted) paths, the original executable for static evidence only, cached static packages, cached dependency packages and cached prototype host SDK packages. The runner uses cleared offline feeds, task-private APPDATA/package copies/intermediates, disabled shared compilation/apphost, explicit CPU DLL invocation, hidden45-second prototype host launches and a fresh private output root. No original executable execution, live contact, download or shared prototype host build is needed.

The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_CIRCLE_HEIGHT_PATCH.md) records exact results and the resume point. Next bounded milestone: recover one original coordinate producer or one intervening layer influence product, with explicit storage/rounding points, before claiming original generated terrain.
