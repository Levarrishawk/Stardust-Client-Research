# Supplied current-height filters in the scene preview

Date: 2026-10-04. This synthetic version-3 preview joins the accepted
[current-height sequence](TERRAIN_SUPPLIED_HEIGHT_FILTERED_SEQUENCE_CONTRACT.md)
to the actual prototype host terrain and motion host. It is a conditional compatibility
slice, not an implementation of the original recursive terrain generator.

## Boundary and publication

ScenePumpedHeightFilteredTerrain (external historical reference)
decodes bounded supplied BCIR/BREC, two FHGT and six AHCN envelopes. Four chunks
each supply 100 initial height words. The first layer stores its result before
the second FHGT queries that mutable height. Query precisions are explicitly
53 and 64 bits; coordinate, combination, product and affector precision are
explicitly selected. No original floating control state is inferred.

The handler evaluates the complete 400-sample batch and constructs all padded
patch candidates before allocating terrain visuals. Late nonfinite inputs
reject the preview before publication. Sixteen patches contain 128 triangles.
Exported receipts preserve initial/intermediate/final bits and both queries,
local products and affector visits. Heights supply triangle-based grounding to
the accepted motion driver, with zero foot offset for this fixture. Normals
and colors are supplied diagnostics, not recovered original terrain data.

Version 3 is an explicit private fixture mode. Existing version-1/2 paths remain
separate. The version-3 camera frames the whole character and terrain. The
original terminal resource is retained but its mesh can be occluded by the
synthetic hill; resource retention is established independently of visibility.

## Independent execution evidence

The Python peer (external historical reference)
uses independently authored Fraction arithmetic and generates inputs before
the host runs. Actual historical implementation path (omitted) headless and GPU receipts match 400 samples,
16 patches, 128 triangles and nine timed motion states. Ninety samples witness
the second query observing a preceding height write. Terrain cleanup balances
all sixteen allocations/disposals. The joined original human/terminal loaders
retain three calls with three closes; full CREO4, full CREO6, TANO6, customization
and clock/material regression oracles pass on the same host evidence.

A NaN placed at the last sample of the fourth chunk rejects before any terrain
chunk/ready/visual marker. Failure cleanup reports zero actors and no unmatched
loader calls. These are confirmed replacement-host behaviors, not original
malformed-input or terrain parity claims. The screenshot was inspected: whole
human and terrain are visible with diagnostic colors. GPU/system certificate
diagnostics already covered by the prior host harness remain recorded privately.

The first peer used an incorrect reset timestamp; checking the accepted earlier
motion peer established the canonical 1300006 value, after which the saved
actual log passed. The first camera cropped the human head; a new camera and
fresh GPU capture corrected it. A scalar PowerShell log-selection error in the
negative wrapper was corrected using an array; the retained actual failure log
then passed without rerunning the malformed host. No native result was used to
generate expected terrain arithmetic.

## Limits and next action

Original AHFR, FFRA, fractal sampling, normals, cache lifecycle and recursive
terrain admission are outside this host slice. Newly accepted supplied-sample
AHFR/FFRA APIs are documented separately and are not used by version 3 here.
The next scene increment is an explicit received-only creature world/router
policy; a future fractal terrain host must first preserve the original sampler
return/store distinction and family lookup evidence.

## Round46 accepted compatibility increment

The same400-sample/16-patch/nine-grounded-state fixture now passes in both full4 and explicit received-only partial4 modes. The terrain oracle accepts either exact selected mode marker. Its geometry, height, clock, original-resource and exact-cleanup assertions remain active; the [partial preview](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_PREVIEW_CONTRACT.md) separately verifies33 received events without default seeds.

## Round49 shared-host regression

The shared entry now additionally supports the [planar sourced-resource mode](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md). A separate unchanged supplied-terrain run passes all400 samples/16 patches/128 triangles/nine grounded states/90 preceding-height witnesses and partial/full6/clock/customization gates. Source mode itself does not claim terrain grounding.
