# Terrain patch sample assembly and seam selection

## Result and scope

TerrainPatchAssembly (external historical reference)
closes the integer sample-to-vertex and boundary-to-pattern seam left by
[accepted topology](TERRAIN_PATCH_TOPOLOGY_CONTRACT.md) and
[supplied geometry](TERRAIN_PATCH_GEOMETRY_CONTRACT.md). It produces an owned
`TerrainPatchGeometry` and an immutable receipt with copied source sample indices.

Inputs are resolved, common-stride row-major position/normal/ARGB grids,
integer cell metadata, UV scale and auxiliary selectors. There is no height
evaluator, procedural rule execution, material evaluation, baked-color
modification, compass/world mapping, prototype host dependency or renderer publication.

## Exact sample order

Baseline executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Addresses here are VAs. Builder `008618D0` uses nine 16-byte records at
`015E71C8..015E7257`. The first two S32 words are offsets; the next pair are
base UV floats. The sample order is **Confirmed** by those bytes and its loop:

| Local vertex | Column offset | Row offset |
| ---: | ---: | ---: |
| 0 | 1 | 1 |
| 1 | 0 | 1 |
| 2 | 0 | 2 |
| 3 | 1 | 2 |
| 4 | 2 | 2 |
| 5 | 2 | 1 |
| 6 | 2 | 0 |
| 7 | 1 | 0 |
| 8 | 0 | 0 |

For cell `(column,row)`:
`originColumn = sampleBorder + 2*column`,
`originRow = sampleBorder + 2*row`, then
`sourceIndex = (originRow + rowOffset)*gridWidth + originColumn + columnOffset`.

Observed chain: `00861C18/00861C1E/00861C24` derive both origins using the
same chunk `+68h` value; `00861D6E` loads the table; `00861D81..00861DF9`
add offsets and perform row-stride multiplication for positions, normals and
RGB source. `00861EFE/00861F07` advance the table by 16 and stop after nine
records. `00861FAA` passes the nine position/normal/resolved-color arrays to
`00865DA0`. The latter stores cell column/row at primitive `+04/+08`.

The original uses separate source-channel strides and converts/modifies RGB
before producing ARGB words. This API deliberately requires already resolved
ARGB and a common stride for all supplied channels. It does not assert original
buffer widths are equal or reproduce the intervening color/material operations.
A future generator must normalize those inputs explicitly.

## Exact seam selector

`00861FBB` calls `00865FF0` with the chunk seam byte and cell count.
The selector reads primitive column/row and compares them to zero and
`cellsPerSide-1`. Four meaningful low bits are:

| Bit | Applicable boundary | Single-edge pattern | Omitted midpoint |
| ---: | --- | ---: | ---: |
| 1 | row maximum | 1 | vertex 3 |
| 2 | column minimum | 3 | vertex 1 |
| 4 | row minimum | 5 | vertex 7 |
| 8 | column maximum | 7 | vertex 5 |

Adjacent combinations select 2 (column minimum + row maximum),
4 (column minimum + row minimum), 6 (column maximum + row minimum),
and 8 (column maximum + row maximum). Otherwise pattern 0 applies.

**Confirmed branch priority:** row minimum/bit4, then column minimum/bit2,
then row maximum/bit1, then column maximum/bit8. Within the first branch,
column-minimum/bit2 takes priority over column-maximum/bit8. This matters when
`cellsPerSide=1`, where minima/maxima coincide: mask15 selects pattern4.
For larger chunks each normal corner has only two relevant edges. Interior
cells ignore the seam mask. No compass names are assigned.

The selector rebuilds triangle planes and stores the chosen index-list pointer
at `00866160`; it does not regenerate positions. This helper preserves selected
index order and counts using the accepted nine vectors, and leaves original
collision receiver objects outside the engine-neutral boundary.

## API, ownership and modern validation

`TerrainPatchCell` holds count, column, row, sample border and seam mask.
`SelectPattern(count,column,row,mask)` performs constant-work selection for
positive int counts and coordinates in range. Masks outside 0..15 reject;
this rejection is modern policy rather than recovered malformed-input behavior.

`Assemble(width,height,positions,normals,argb,cell,scale,...)` requires exact
`width*height` lengths for all three common-stride grids. It checks positive
dimensions, nonnegative border, checked dimension/origin arithmetic, and enough
space for the entire active cell grid: each dimension must be at least
`sampleBorder+2*cellsPerSide+1`. Default maximum declared grid samples is
1,048,576. Exact cap acceptance and lower-cap rejection are tested.

Only nine selected samples are read/copied; unused grid elements are not
scanned or retained. Selected finite/normal/UV/selector eligibility comes from
the accepted geometry constructor. Work remains nine gathers plus bounded
geometry construction and at most eight triangle checks, independent of the
declared grid size. The result adds a nine-int receipt to the geometry's bounded
owned payload. Caller-owned full grids and retained old results are separate
from that accounting.

Default modern winding policy rejects degenerate triangles or cross products
in disagreement with the summed supplied normals. It can be explicitly disabled
through `RequireCoherentWinding=false`; this does not prove original malformed
surface acceptance. Normals/positions themselves remain raw CPU values.
A failed call publishes no result and changes no previous assembly or input.
All inspection arrays are copies; no engine or mutable global state is retained.

## Evidence and verification

The the separate implementation record
SHA-gates before PE/VA use, checks nine table records and 27 instruction sites
in five ranges. A bounded interpreter evaluates only decoded equality/test
branches with synthetic integers; it never executes the original client.
Its results match an independent literal boundary specification.

The native oracle passes 480 assemblies (all 16 masks for every cell in
counts1..4), 288 larger-count selections (7 and int.MaxValue), 5306 checks and
46 rejection cases. Every selected source index, position/normal/raw color,
base/auxiliary UV, selector and topology index matches the independent oracle.
Synthetic winding checks and ownership/failure-retention checks also pass.

Original generated heights, source padding defaults, material selection,
resolved-color generation, world-axis orientation and scene integration remain
outside this slice. Reproduction and exact next action are in the
[checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R33_TERRAIN_PATCH_ASSEMBLY.md).
No prototype host/GPU probe was run in this round.
