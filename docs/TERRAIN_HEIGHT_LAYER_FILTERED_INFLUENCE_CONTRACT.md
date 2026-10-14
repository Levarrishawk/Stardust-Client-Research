# Boundary-admitted height-layer FFRA influence

Round 57, 2026-10-04. [TerrainHeightLayerFilteredInfluence](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainHeightLayerFilteredInfluence.cs) composes the actual [round 56 boundary admission](TERRAIN_HEIGHT_LAYER_BOUNDARY_ADMISSION_CONTRACT.md) with one owned [FFRA rule](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md). It produces local influence and a separate incoming product for a complete grid. AHFR stays unevaluated; this is not ancestor traversal or full terrain generation.

See [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainHeightLayerFilteredInfluenceScenarios.cs), [independent Fraction oracle](../../SWG-Godot-Client/poc/round57-tests/Assets/peer.py), [isolated runner](../../SWG-Godot-Client/poc/round57-tests/Assets/Invoke-SelectedRule.ps1) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R57_HEIGHT_LAYER_FILTERED_INFLUENCE.md).

## Exact input identity and eligibility

`Evaluate(boundary, slice, grid, incomingWeightBits, controls, limits, token)` requires the same actual immutable slice and grid instances retained by `boundary`; separately decoded byte-identical substitutes reject. The slice must be locally enabled TypedEvidence. The consumer obtains its filter exclusively from `slice.Filter` and retains the exact slice/filter/family/child objects. No caller-supplied replacement rule, raw data, family or sampler is accepted.

Before copying or sampling, checks compare the FFRA source child ordinal 3, global rule index, FFRA/0005 tag/version, offset, length and hash with the accepted rule receipt. Generator hash/length, layer index, group hash/extent, signed family ID and absolute family offset must match the slice. The accepted round 55 factory already established owned root/group/family identity; this consumer does not export source bytes again.

The FFRA receipt's policy remains TypedEvidence, as selected inside the evidence slice. Its independent **operational eligibility** must be true: enabled typed FFRA/0005, finite thresholds, serialized amount and scale; a supported mode 0..5 family, both curves off, signed octave count 1..16, seed other than int.MaxValue and finite used family parameters. This uses the frozen round 54 eligibility boundary without changing its policy or decoder. Unknown feather selectors and reversed/equal thresholds are accepted and retain their established consumer behavior. Amount clamping occurs in the existing query.

AHFR support is not required. A different height family with authored curves, or the pinned original layer 3's unsupported AHFR, remains valid typed evidence here. Disabled/nonfinite/ineligible FFRA rejects even when every boundary cell is blocked. Eligibility does not guarantee a usable normalization or every numeric query. An eligible zero-sum normalization is never constructed when no cell is admitted; an admitted query triggers the frozen sampler's rejection.

Incoming weights must contain exactly N finite F32 values, including blocked cells. Negative values and both signed zeros are accepted; they do not suppress an admitted FFRA query. The consumer copies weights into its own storage and retains a scalar weight in each immutable cell.

## Admission, retained queries and recombination

Sampling controls explicitly select table construction, sampler construction and sampler query precision; FFRA query, combine and product precision are separate 24/53/64 choices. Combine precision must equal the actual boundary probe's combine precision. Changing it requires a new boundary result, since the first gate depends on shaping and stored comparisons. Product precision may differ independently. Original active arithmetic controls remain Unknown.

Every row-major cell is retained. Only the actual `BoundaryPositive` cells become sampler requests, with their original unique column/row identities and existing Layer-coordinate bits. Equal coordinate values in different cells do not merge those cells.

A zero-admitted grid makes zero sampler constructions, query batches, computed samples, FFRA queries, FFRA carriers and disposals. Otherwise the operation constructs **one fresh complete-shape frozen no-curve sampler**, makes one admitted-subset query batch, requires a computed miss for each unique cell, and disposes that sampler once. Cache dimensions remain the full grid dimensions; the admitted subset is not repacked into another shape.

Each admitted cell retains the actual frozen sampler Result/Sample, `TerrainFilterFractalInfluence.Query` result and `FromRetainedFractalFilter` carrier. The query uses the retained sample for its mandatory scaled F32 store. Exact sample/query/input/carrier reference identities are checked. After disposal, retained samples and queries remain usable independently of the sampler's cache/kernel lifetime.

Each cell then gets a fresh frozen layer receipt using its actual prior circle carrier, raw BCIR selector, boundary gate 1, ADTA boundary inversion and its own incoming weight. An admitted cell additionally supplies the actual FFRA carrier and raw FFRA selector. A blocked cell supplies an **empty filter list**; an enabled entry with a null query would violate the frozen sample contract even if the first gate would skip it.

Filter gate is 1 and ADTA filter inversion is preserved. The frozen combiner applies it to the combined boundary/filter local accumulator only after a positive first gate. It cannot admit a blocked cell or cause an FFRA query there. Recombined `BoundaryPositive` must equal the actual admission result. Local influence and incoming product remain separate stored fields, with final local/product positivity reflecting their distinct operands. No height application follows.

## Immutable receipts and logical budgets

Output retains the exact boundary result, slice, grid and selected filter/source metadata. Cells hold the actual boundary cell, incoming weight, nullable computed/query/carrier chain and final receipt. Blocked chains are null. Read-only cell and incoming arrays prevent mutation; exported weight/local/product arrays are detached copies. Repeated and concurrent calls construct independent local samplers and preserve prior results.

The frozen sampler supports at most 1,024 full-cache cells. Square complete grids therefore have axis count 2..32. Default/hard cell and filter-query caps are 1,024; axis hard cap is 32. Every limit is validated before allocation, and cancellation brackets input validation, requests, sampler/query work, recombination and publication.

For N full cells, reserve worst-case all-admitted work **before any weight copy, query array or sampler construction**, including when the actual admitted count A is zero:

| Charge | Worst-case reservation | Actual logical charge |
|---|---:|---:|
| Consumer array/storage bytes | 512*N | 512*N |
| Copied arrays/entries | 68*N | 28*N + 40*A |
| Sampler storage | 9,648 + 16*N | same if A>0; zero otherwise |
| Sampler construction work | 20,256 | 20,000 + 16*octaves if A>0; zero otherwise |
| Sampler query storage | 1,456*N | 16*N + A*(160 + 80*octaves) if A>0; zero otherwise |
| Sampler query work | 8,257*N | N + A*(64 + 512*octaves) if A>0; zero otherwise |
| Consumer recombination/bookkeeping work | 96*N | 96*N |

`ReservedRouteBytes` is consumer storage plus reserved sampler/query storage; `AccountedRouteBytes` uses actual A and octaves. Work follows the analogous sums. `CopiedInputBytes` is 4*N for incoming weights; `CopiedEncodedInputBytes` is zero. Copied-array charges include the incoming array, sampler request copy and copied boundary/filter entries. They are a separate copy-volume measure, not added a second time to storage. These are deterministic conservative policy charges, not measured heap use, CPU instructions or elapsed time. Future caller exports and retained prior input storage are excluded.

Default copy/array/route/work caps are 131,072/1,048,576/8,388,608/16,777,216. Hard maxima are 1,048,576/8,388,608/33,554,432/33,554,432. Sampler byte/work caps have hard maximum 1,048,576 each; query byte/work hard maxima are 2,097,152/16,777,216. Full N must fit each cap, including the filter-query cap, even when fewer cells are admitted. Zero, negative, above-hard and exact-one-less cases are explicit caller policy tests.

Actual counts distinguish retained boundary probes, new recombinations, sampler constructions/batches/disposals, computed cells, FFRA queries/carriers and octave queries. Output publication is atomic: sampler/query failure, late nonfinite stores or cancellation publish no partial result and preserve prior output. Disposal follows the frozen sampler's owner-thread/using lifetime.

## Evidence and remaining boundary

**Confirmed:** native and independent Fraction agreement for mixed/zero/all-admitted grids, raw selectors, both ADTA inversions, incoming signed/zero/negative weights, all frozen modes, explicit profiles, retained-vs-F32 scale-store witnesses, exact identity/ownership, caps and rejection paths. One pinned original layer 3 BCIR+FFRA+ADTA occurrence is exercised with explicitly synthetic grid/profile inputs. Its height is never numerically evaluated.

**Unknown:** active original arithmetic controls and original-run parity; original recursive layer traversal, ancestor activation, bounds culling, curves-enabled AHFR sampling and original sampler pointer/cache lifetime. Existing static evidence establishes the selected codecs, first gate and FFRA/store/combiner sequence, not a recovered full original terrain execution.

Next bounded action: apply a supported synthetic AHFR through the actual positive influence products, preserving the distinct first gate, local influence and incoming product. Original layer 3 AHFR remains blocked on its authored curves; no broader original-layer search is implied.
