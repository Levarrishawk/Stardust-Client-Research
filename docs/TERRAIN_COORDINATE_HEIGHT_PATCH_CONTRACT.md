# Padded coordinate-to-supplied-height patch contract

TerrainCoordinateHeightPatch (external historical reference) connects the accepted [coordinate producer](TERRAIN_SAMPLE_COORDINATE_GRID_CONTRACT.md) to [nine-sample patch assembly](TERRAIN_PATCH_ASSEMBLY_CONTRACT.md). Inputs are an immutable coordinate result and complete supplied row-major raw F32 heights, raw normals and resolved ARGB words. There is no height evaluator, complete TGEN/layer/filter generation, material program, normal/color generation, original terrain occurrence or GPU work.

## Closed active-cell/padding relation

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are VAs; the static verifier (external historical reference) hash-gates before decoding, then checks77 instruction anchors, the render-block vtable slot and nine offset records.

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| `0084A57D -> 00B6E520` reads signed PTAT member+2C; `0084A58A` saves it at frame-2C. `0084A64A/65C` copy it to frame-80, render block-90+10. | Confirmed active cell count source is the supplied tile value. | Cells per side are `coordinates.Request.Tiles`; not the full sample count or a guessed LOD crop. |
| `0084A68F/69E` copy appearance member+58 into render block+28; `00861912/15` transfer it to ClientChunk+68. | Confirmed sample-border source. | Border is the supplied margin in the coordinate request; preserve it exactly. |
| `0084A761/776` put full count into generator parameters; `0084A79E` invokes generator. `0084A7C7/CD/D0` later pass the render block to `008618D0`. | Confirmed synchronous generator-to-render assembly chain. | Full coordinate count and active render cell count are distinct. |
| `00861927/30` save render block+10 as loop limit. `00861C18/1E/24` use border+2*column and border+2*row. `00861FC3/C4/C9`, `00861FD2/D3/D8` advance inner column and outer row to that limit. | Confirmed complete cell rectangle and iteration order. | Emit row-major tiles-squared patches and gather all nine offsets. |
| `00861D91/94/97` use position descriptor+60 as row stride; normals use+6C at `00861DC8/CE`, colors use+C at `00861DF3/F6`. | Confirmed separate original channel strides. | This API explicitly normalizes all supplied channels to the coordinate count as a common stride. It does not assert all original descriptors always have identical dimensions. |
| `00861EFE/F07/F15` traverse nine records; `00861FAA` constructs geometry; `00861FBB` selects seams using saved active tiles. | Confirmed sample/geometry/seam handoff. | Reuse accepted assembly/topology and preserve selected index order. |

For full width `W = 2*(margin+tiles)`, the complete active sample interval is inclusive `[margin, margin+2*tiles]`. It has `2*tiles+1` samples. Complete coverage needs `margin+2*tiles+1 <= W`, hence `margin>=1` within this positive tile domain. Leading padding has margin samples; trailing padding has margin-1. Thus a supplied fresh-margin candidate3 gives leading3/trailing2, while margin1 is exact-fit leading1/trailing0. Nothing is averaged, resampled, shifted, or dropped to make an insufficient buffer fit.

The accepted fresh constructor writes member+58=3 at `00B5D7F1`; this does not establish current margin or absence of later changes. Margin stays explicit. Raw producer acceptance of margin0/negative or duplicate axes does not imply display eligibility. Those cases reject here before snapshots.

## Call, ownership and legacy error boundaries

`008618FD` calls render-block slot+4, whose baseline pointer is `008490C0`. It jumps through `00B5CFC0/C3` to no-op `00B62EC0`; this checked slot does not resize or validate channels. Position regeneration is conditional on dirty flag at `00B65376..89`; `00B65381` calls `00B65730` and clears it. Source position assembly reads generated position vectors rather than recomputing boundary coordinates.

The builder's two temporary parameter blocks are borrowed across synchronous calls. After assembly, `0084A808` destroys the generator block, `0084A817` destroys the render block, and `0084A826` destroys the timing helper before returning at `0084A83D`. Assembly skips its cell loop for nonpositive tile count at `00861C05`, drains its temporary helper at `00861FF1`, and ultimately returns at `00862605`. Allocation branch `00861CD2/D8/EA` tolerates a null constructor result locally; later behavior is not an established clean rejection policy. No original bounds-check, checked overflow, allocation/trap or exception cleanup equivalence is claimed. The modern owned all-or-nothing policy below is independent.

## API and coordinate separation

`Build(coordinates, heightBits, normals, argbColors, seamMask=0, uvScale=1, limits=null)` retains the immutable coordinate object. `Coordinates.Layer` remains available with its independently selected profile. Geometry uses `Coordinates.Position` exclusively, with no layer/position averaging or forced equality. All four profiles remain explicit in the coordinate receipt, and active original arithmetic state remains Unknown.

Native position Y is the supplied height plus the positive-zero vertical origin, with instruction rounding under the position profile and final F32 storage. `00B657D4/DD` observe that add/store; the implementation reuses frozen internal dyadic arithmetic within Core. `CopyInputHeightBits()` preserves the raw input, including negative zero. `CopyPositionHeightBits()` records stored Y; nearest-even `-0 + +0` becomes positive zero. Copying raw heights directly into Y would omit an observed operation.

`GridWidth`, `CellsPerSide`, `SampleBorder`, `LeadingPadding` and `TrailingPadding` are explicit receipts. `Patches` is a read-only row-major collection of accepted `TerrainPatchAssembly` values. Each assembly retains its exact source sample indices and owned geometry. Raw input height/normal/ARGB copies are available independently. ARGB is opaque U32 data: all patterns, including zero alpha and NaN-shaped words, are valid; only channel shape/caps constrain color input.

## Modern display eligibility, budgets and atomicity

Default limits:65536 complete grid samples,64 patches,576 selected sample occurrences. Hard maxima:1048576,1024,9216. Limits must be positive and within those maxima. Tiles1..32, valid complete padded extent, full Count-squared channel lengths and every aggregate cap are checked before channel snapshots/typed output arrays. Selected charge is nine occurrences per cell, counting shared vertices again; it is not a count of distinct grid samples or bytes. Geometry has at most24 indices per patch through accepted topology.

Require strictly increasing stored **position** axes across the full grid; layer axes may differ. Full position axes/heights must be finite and within one million. Supplied normals, including padding, must be finite, have components within4096 and nonzero eligible squared length at least1e-12. UV scale is positive through16, seam mask0..15, and no auxiliary UV set is produced. These are modern inspection restrictions. Unused padding is validated because the API owns complete display channels; this is stricter than the original selected gathers.

After snapshots, build all stored Y values and common-stride positions, then gather cells through the frozen assembly. Coherent winding/3D degeneracy checks apply. A separate frozen `TerrainPatchSurfaceQuery` constructor check enforces its default projected-area/position eligibility before publication; interpolation at arbitrary future points still follows its existing supplied-normal policy. This produces geometry suitable for the accepted prototype host adapter and frozen surface-query constructor without claiming original collision parity. No prototype host object is allocated here.

A failed height/normal check, later cell winding failure or surface-query gate returns no object and leaves all inputs and earlier results intact. Copies are independent; aliased height/color spans are snapshot safely. The retained coordinate input is immutable; there is no borrowed mutable channel data, callback or external write. Concurrent caller mutation during a call is unsupported.

Logical storage/work caps include full snapshots/positions/stored-Y and tiles-squared fixed gathers/geometry/query-constructor checks. Retained arrays are raw heights, stored heights, normals and colors plus the bounded geometries/source-index receipts. Caller-retained objects/copies and CLR reclamation/heap/RSS are outside logical per-call accounting. The temporary query checks copy nine vertices and bounded indices/areas; they have no engine resources.

## Independent verification and resume

Public smoke scenarios (external historical reference) reference the actual Core assembly. The independent oracle (external historical reference) reuses accepted Fraction coordinate functions, independently computes stored Y, padding, source indices, topology rings and winding/projected eligibility, and compares all raw heights/colors/selected positions/normals/layer pairs. Coverage includes81 profiles, all masks/all nine patterns, signed work values/scales/margins, adjacent chunk coordinates, exact/one-less budgets, full channel shapes, raw invalid heights/normals, duplicate axes, late geometry failure, aliases and retained copies. Synthetic fixtures contain no original asset bytes.

Invoke-PaddedPatch.ps1 (external historical reference) uses cached tools and private package/artifact roots, cleared feeds, disabled shared compilation/apphost, explicit CPU DLL invocation, actual separate Core/SmokeTests build and wrong-identity/truncation negatives. No GPU, corpus sweep, original execution or downloads are required. Exact results, pins, failed attempts and the next probe are recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_PADDED_COORDINATE_PATCH.md).

Next bounded milestone: join this supplied padded grid to one recovered height/boundary/layer stage while preserving separate layer/position profiles and current-request provenance. Original normal/color/material generation and complete procedural terrain remain separate.
