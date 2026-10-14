# Finite shader-family selection and three-root surface preview

`TerrainShaderConstantInput` owns strict ASCN0001/IHDR/DATA12 bytes and preserves the family key, override word and raw clamp bits. It supports the established IHDR0000/0001 layouts and rejects other rule versions, malformed framing/order/extents, excessive source/name/node/depth sizes and pre-cancellation. Raw nonfinite clamp words remain inspectable; operational selection requires a finite effective clamp.

`TerrainShaderConstantFamilyRule.Bind(input, families)` selects the first matching serialized, nonempty family. Its effective clamp is the override word when override is nonzero, otherwise the family's second raw word. `Evaluate(influenceBits, priorFamilyKeyBits)` requires finite influence and a present nonempty prior family. An enabled rule assigns when influence is positive and at least the clamp; equality assigns. Otherwise the previous family is retained. Results carry full family key and actual serialized family-array index. These are engine-neutral labels, not the original three-byte packed map. Missing/empty families and nonfinite operational inputs are modern strict-profile rejections, not recovered original error handling.

## Direct target evidence

Executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` is checked before PE/VA interpretation. Constructor00B872C0 pushes ASCN and installs vtable015EDD30; COL01626714 links type descriptor018B8E38, `AffectorShaderConstant`. Its process slot+10 is00B87410. Loader0001 body00B87640 reads three four-byte DATA words; setters00B873C0/D0/E0 store family+20, normalized nonzero override+24 and raw clamp+28. These identity/layout observations are **Confirmed**.

Process00B87410 rejects nonpositive finite amount at00B87435/3A. Byte+24 selects override+28 or cached clamp+1C at00B87481..90. Comparison00B87493 and testAH1/branch00B87499/9C skip a smaller amount, while finite equality assigns. SGRP0006 second raw word read00B6AC34 passes to00B6B420 and family+0C; getter00B6B410 and keyed wrapper00B6B430 feed the cached clamp. These finite selection semantics are **Confirmed target evidence**, independent of NGE.

The target writes request-state+20's descriptor with stride+0/data+8 at byte offset `3*(row*stride+column)`. The three-byte record contains family-array index, the low byte of requested family ID, then a child-choice byte. RNG call00B59EF0 through state+60 occurs after successful assignment;00B69140 writes only the choice byte. Original RNG state/conversion, child texture choice and shader/blend execution remain **Unknown**. The label helper intentionally omits them.

The existing [layer preparation](TERRAIN_LAYER_PREPARATION_CONTRACT.md), [influence graph](TERRAIN_LAYER_INFLUENCE_CONTRACT.md) and [ordered traversal](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md) show generic enabled-affector/child gates, source-ordered affectors and recursion. They contain no height-only exclusion for material affectors. This justifies material-only root admission; it does not make a three-root sampler complete traversal.

## Explicit pinned partial preview

`OriginalTerrainSurfacePreviewSampler.Bind(generator)` requires original Tatooine TGEN SHA256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. It binds the actual SGRP group and validates enabled root layers0/78/93, LAYR0003, zero ADTA flags, no local filters, exactly one enabled ASCN0001, expected keys5/8/3, clamp1 and the expected zero/three/one BCIR0002 boundaries. Input strings/assets remain in their existing owned readers.

`Sample(grid, precision, cancellation)` consumes the actual layer-coordinate grid, capped at32 samples per axis. It uses existing retained circle queries and the layer-influence combination graph, with incoming root influence1. The explicit default precision isBinary64;24/53/64 may be supplied. Active original controls and original numeric parity remain **Unknown**. The admitted unbounded root establishes key5 for every sample;78 and93 conditionally replace it in order. Each result retains actual grid/sampler identity, detached/read-only keys, original family indices, last assigning layers and assignment counts.

The policy is `OriginalRoot0Junland78ArchValley93PartialFamilyLabels`. Its receipts expose all51 omitted shader-owning layers, deferred Junland slope-shader children79/80/81 and the original filter-kind listFDIR/FFRA/FHGT/FSHD/FSLP. These filters and other shader-owning roots are not evaluated; the sampler rejects modified input identity rather than expanding the selection. Layer93 children contain no ASCN. `CompleteMaterialTraversal`, original numeric/child-choice/blend parity are explicitly false. The parent host may select the first actual child as a separate modern rendering policy; neither this sampler nor the selection helper chooses textures or GPU blends.

## Extended zero-ADTA boundary families

The original default and its labels remain selectable through `Bind(generator, cancellation)`. Explicit `Bind(generator, OriginalTerrainSurfacePreviewPolicy.ExtendedBoundaryFamilies, cancellation)` selects instance `PolicyName` `OriginalZeroAdtaEightShaderLayersBoundaryFamiliesPartialLabels`; each sample's `Policy` forwards the actual selected name. This adds five ASCN rules while preserving the three-root policy. It does not expand any filter or unknown-flag support.

| Added layer | Complete ancestor path | Family | Boundaries | Original first-child shader |
|---|---|---|---|---|
|87 Bens Mesa|87|3|seven BCIR0002|`shader/sand_dune_yellow.sht`|
|105 Sarlacc inner layer|104→105|5|parent/child BCIR0002|`shader/tatt_sand_bumpy.sht`|
|129 Fort Tusken flats|128→129|3|parent BCIR and two local BCIR|`shader/sand_dune_yellow.sht`|
|135 Fort Tusken path|128→135|9|parent BCIR, BPLN0001, BREC0003|`shader/rock_tato_road.sht`|
|138 Fort Tusken cave porch|128→138|6|parent BCIR, two BCIR, BREC0003|`shader/rock_cliff_anza.sht`|

The selected shader-layer list is0/78/87/93/105/129/135/138, covering five distinct keys3/5/6/8/9. `AdmittedInfluenceLayers` additionally includes parents104/128. Every selected node and complete ancestor chain must be locally enabled, LAYR0003, all-zero ADTA and free of local filters. Boundary-kind counts and enabled finite fields are checked against the pin. Rectangle extension flags must be zero; the accepted fresh rectangle contract leaves its runtime transform disabled. The selected polyline has six original ordered points, width6 and zero feather. Layer105 uses its actual override clamp bits3F6C0831; the other rules use family clamp1.

The sampler visits admitted nodes in original order, computes the existing retained boundary combination, multiplies it by the parent's stored influence, and forwards that actual product to ASCN selection. Boundary-only labels do not depend on ignored height writers in these filter-free nodes. Forty-six other shader layers remain deferred; slope-shader descendants79/80/81/88/89/133 are explicit. In particular, city-road/square chains through root141's ADTA third word1 remain excluded: [byte+68 semantics are Unknown](TERRAIN_LAYER_ADTA_RUNTIME_CONTRACT.md). FHGT must consume ordered query-time height and FSLP needs its recovered normal/cache prerequisites; neither is replaced with final generated heights or modern render normals.

`TerrainLayerInfluenceQuery.FromPolyline` now retains the existing `TerrainPolylineQuery.Value` for caller shaping and exposes its actual query identity. The source enum appends Polyline, preserving prior values. Its60 independent Fraction/integer fixtures include eight cases where reloading the F32 projection before shaping changes the answer. No BPLN arithmetic or raw decoder changes are made. The existing [rectangle](TERRAIN_BOUNDARY_RECTANGLE_INFLUENCE_CONTRACT.md) and [polyline](TERRAIN_BOUNDARY_POLYLINE_INFLUENCE_CONTRACT.md) contracts remain authoritative.

The extended original test covers24 grids and4704 labels, including all16 width40 chunks of request(-101,154,4,4): legacy X[-4040,-3880], Z[6160,6320]. This contains the original road rectangle and small fixed cave-porch patches. Core receipts establish label behavior; GPU acceptance is a separate host boundary. The verifier exports `build_classification(generatorBytes, extended=True)` and `classify(spec, x, z)`, returning family key, original family index and last assigning layer from original bytes and independent boundary models. Parent render verification can use these functions without reading production labels. The default six grids/1176 labels still compare independently under the old policy.

Focused acceptance: synthetic-only native746 assertions/131 rejections,486 independent ASCN selections and60 independent polyline-adapter cases; eight retained-value witnesses distinguish correct shaping from an early F32 reload. Original native897 assertions/149 rejections join the1176 default and4704 extended labels. Seven additional tampered extended receipts reject. Parent separately reports actual DX12 Fort Tusken acceptance:708 grounded movement updates and256 independently classified patch associations (97 dune,2 fixed porch rock,67 base sand,90 road), with all five original shader/texture bindings compared. That uses modern patch-center/first-child rendering; original slope, random choice and blend parity remain deferred. The consolidated [terrain checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_TATOOINE_REGIONS.md) owns host acceptance and retained receipts.

## Validation and continuation

Focused native tests (external historical reference) cover486 finite byte fixtures, truncation and malformed DATA/header/version/framing, nonfinite raw versus operational behavior, absent/empty family, first duplicate match, detached ownership, disabled rules, signed zeros, threshold ties and adjacent F32 values, cancellation and repeat/parallel original sampling. Default and extended policies have separate original grids. The independent verifier (external historical reference) parses synthetic ASCN bytes separately, reuses independent SGRP/BREC/BPLN oracles, compares circle labels with a100-decimal reference, and verifies target body hashes/anchors and existing preparation/order proofs. The high-precision reference is classification evidence for these fixtures, not x87 arithmetic parity.

Build the test project with its source-free NuGet.Config, an existing offline package cache and isolated artifacts. Its default DLL execution requires no private input; retain stdout as a synthetic native receipt and run the Python verifier with `--native` alone for the486 independent selection checks. Optional DLL execution with one original TRN path adds the six original grids; retain that stdout privately and run the verifier with `--native`, `--trn`, `--exe` and `--packages`. The three original-mode paths must be supplied together; synthetic-only verification refuses an original receipt. Original mode retains every target/body/identity/coordinate/label check. No original process or network is executed.

Next: admit further ordered material layers only after closing their unknown flags or query-time filter dependencies. Preserve the partial-surface label and independent render association checks. Original child choice and four-layer GPU blending remain separate questions.
## Subsequent ordered slope opt-in

The default policies described here remain available. The separately admitted
[ordered slope policy](TERRAIN_ORDERED_SLOPE_PREVIEW_CONTRACT.md) joins query-time
height/normal/filter evaluation for the six former deferred descendants, with
14 shader layers/seven families and explicit modern numeric policies. It passes
the [21-check frozen DX12 gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_ORDERED_SLOPE_TEXTURING.md).
Forty other ASCN layers remain deferred; complete appearance and original numeric
or blending parity are not established.
