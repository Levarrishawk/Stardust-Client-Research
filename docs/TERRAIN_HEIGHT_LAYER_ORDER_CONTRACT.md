# Original height-layer order: one pinned FHGT path

The selected real Tatooine FHGT reads the shared current height after the root layer and earlier siblings have completed. It does **not** form a real FHGT/AHCN pair: its layer, ancestor and examined earlier-sibling prefix contain no AHCN. Actual prior height writes use AHFR. This closes the ordering and fresh-preparation prerequisite for the [supplied current-height contract](TERRAIN_SUPPLIED_HEIGHT_FILTERED_SEQUENCE_CONTRACT.md); original numeric terrain parity remains Unknown.

The [identity-gated verifier](../../SWG-Godot-Client/tools/python/verify_terrain_height_layer_order.py), [offline runner](../../SWG-Godot-Client/poc/round44-tests/Rendering/Invoke-HeightLayerOrder.ps1) and [Round 44 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R44_HEIGHT_LAYER_ORDER.md) reproduce the bounded result. All production terrain APIs are frozen in this research round.

## Input identity and bounded occurrence

Addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Config SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Effective priority-42 `terrain/tatooine.trn` is 1188446 bytes, SHA-256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`. Its TGEN begins at source offset 210, occupies 139608 bytes and hashes to `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.

One original asset supplies one selected FHGT occurrence. A bounded independently authored frame walker examines the first five depth-first layers and stops after the selected layer; later layer semantics are not decoded. Offsets below are relative to TGEN. Names and original payloads are retained privately. There is no whole-corpus absence claim.

| Layer index / parent | Offset | Local enabled / ADTA words | Own rule order |
| --- | ---: | --- | --- |
| 0 / none | 12247 | 1 / 0,0,0 | AHFR, ASCN, ACRF, AENV |
| 1 / 0 | 12810 | 1 / 0,0,0 | ACRF |
| 2 / 0 | 13059 | 1 / 0,0,0 | FSLP, AFDN |
| 3 / 0 | 13364 | 1 / 1,0,0 | BCIR, FFRA, AHFR |
| 4 / 0 | 13786 | 1 / 1,0,0 | BCIR, BCIR, FFRA, FHGT, AHFR |

All five LAYR versions are 0003 and every examined rule header has enabled U32 1. Root 0 has 23 direct child layers; this is a count of immediate framed children in this one input, not a census of their semantics. Selected layer 4 hashes to `CB2F058575268B31C0CD3D80401CA9EE5991FB31D340F745D6779EC50F8D5D7A`. Its FHGT is version 0002, serialized child ordinal 5 (including IHDR/ADTA positions 0/1), offset 14204, 97 bytes, SHA-256 `61773ADF9E1765EEAB0D87844C0E9779578FDAAA22D276103426D4EBAC20B99C`.

**Observation / Confirmed:** no AHCN appears among the root's own rules, the earlier direct siblings 1..3 or the selected layer's own rules. **Consequence:** the synthetic FHGT/AHCN sequence remains a valid bounded implementation test, but cannot be labeled an original selected-occurrence replay. Later layers and other terrain assets were not searched for a preferable pair.

## Load order and fresh preparation

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Version dispatcher `00B64836` calls LAYR/0003 loader `00B64BB0`; its repeated call at `00B64C76` invokes rule classifier `00B79D00`. Classifier calls appenders `00B63830`, `00B63990`, `00B63AF0`, `00B64550` for boundary/filter/affector/child respectively. Each appender writes at the old count, then increments it. | Separate family arrays preserve relative source order within each family, Confirmed. | Do not execute arbitrary serialized interleaving as a single mixed list. This occurrence's FFRA precedes FHGT in the filter array. |
| TGEN loader invokes each layer's +10 loader at `00B65D77`, then appends through `00B65130` at `00B65D7D`; `00B651A2..AE` appends to generator +4C/+50. | Top-level layer order is retained, Confirmed. | Source root 0 is visited before later top-level layers. |
| Wrapper `00B65C7B/80` recognizes 0000, calls TGEN loader at `00B65C97`, then calls `00B653C0` at `00B65CAC`. Preparation resets generator byte+60, invokes bounds preparation at `00B653C8`, then locally enabled top layers' virtual+04 at `00B653E7`. | Fresh load connects to bounds and enabled-count preparation, Confirmed. | Prepared counts are derived from local enabled getters, not raw list lengths or ADTA. |
| Accepted layer preparation `00B63C50` recurses through enabled boundaries, filters, affectors and children. Bounds preparation `00B64730` clears byte+52 and returns early when inversion byte+50 is set. | Recursive local enabled checks and independent local bounds, Confirmed. | Selected layer 4 and sibling 3 have boundary inversion; fresh bounds byte+52 is clear, suppressing their local rectangle rejection. Root has no local boundaries; child rectangles are not unioned into the root rectangle. |

The [preparation contract](TERRAIN_LAYER_PREPARATION_CONTRACT.md) and [ADTA contract](TERRAIN_LAYER_ADTA_RUNTIME_CONTRACT.md) retain exact count/bounds behavior. Serialized third ADTA word maps to byte+68 and is zero here; its broader role remains Unknown.

## Three distinct grids and the actual height writer

The generator entry `00B65400` checks generator byte+60. On its first guarded pass it sets that byte and calls `00B75AE0` on generator +40 with the requested dimensions. This is a map/cache preparation boundary; its full numerical behavior is outside this result. Independently of that guard, `00B65426..43` loads request state +18, multiplies its descriptor dimensions and clears its data to raw +0. Thus this entry initializes the height grid for the request; it does not preserve old generated heights merely because byte+60 is already set.

It later calls `00B65280` at `00B6571A`. That driver owns a separate generator influence descriptor +54/+58/+5C, grows/reuses its storage as needed and fills the retained descriptor with literal F32 one at `00B652EE`. It calls enabled top-level layers at `00B65366`, passing this influence descriptor and the same request-state pointer. The [sample-coordinate contract](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md) supplies the accepted per-cell coordinate boundary.

A third buffer, each layer's +70/+74/+78 product descriptor, carries local influence multiplied by incoming influence to children. `00B641D0..E3` stores that product using the retained row stride. It is not the height grid at state +18. Reusing capacity must preserve stride; allocation overflow/failure and thread coordination are not established by these unchecked legacy operations.

AHFR's constructor pushes literal tag `41484652` at `00B767E3`, installs vtable `015ED698` at `00B76808`, and initializes cached family sentinel +18 to `FFFFFFFF` and cached pointer +14 to zero. COL `016262C0` points to type descriptor `018B8A30`, `.?AVAffectorHeightFractal@@`; process slot +10 is `00B768A0`. **Identity is Confirmed from tag, constructor, RTTI and vtable**, rather than from a neighboring string alone.

Its process rejects nonpositive finite supplied product before work at `00B768B1/B4`. When eligible, `00B768F1..FC` obtains the same state's +18 descriptor, data+08 and row stride+00. `00B7690A` loads the current cell. After the fractal/operation branch, `00B7694D..5F` reloads that descriptor and stores a final F32 value into the same row/column cell. **Confirmed shared-cell writer.** A store can preserve the prior value; no claim that this occurrence necessarily changes it numerically follows without the unresolved fractal/operation controls.

AHFR also compares desired lookup key +1C with cached key +18 (family identity is Strongly inferred from the fractal-map boundary); when different, it updates the family marker and resolves through state +58 / `00B75B70` before storing the returned pointer at +14. Its use through `013E7C00` is Confirmed. Full lookup validity, cache lifetime, fractal-map numerical behavior and active floating controls remain Unknown. These are prerequisites for value reproduction, not grounds to substitute AHCN.

## Recursive timing, parent gates and cache demand

`00B63D20` is the recursive layer driver. It receives incoming influence as its first stack argument and the shared request state as its second; state is retained in EBX. It returns immediately if prepared affector count+48 and child count+4C are both zero, or when enabled local bounds reject the request. A no-local-work fast flag requires prepared boundary/filter/affector counts all zero; that path can forward incoming influence directly to children.

Otherwise it runs the complete row/column pass. In each cell: boundary query/combination and inversion precede the positive gate; enabled filters run in family order; filter inversion precedes local positivity and the affector loop. FHGT is therefore conditional on the post-inversion boundary gate and on earlier FFRA not producing an exact-zero exit. Its state pointer is pushed at `00B640A6`, virtual+14 queried at `00B640B8`. The later affector call receives the same EBX at `00B6417A/8C`. FHGT query `00B77739/3C` then reads state +18's current row-major cell, as established in the [FHGT contract](TERRAIN_FILTER_HEIGHT_CONTRACT.md).

After all parent cells and affectors have completed, the driver checks the accumulated work flag at `00B64208/0D`, prepared child count at `00B6420F/14`, and each child's local enabled getter at `00B64226/2D`. It recurses in child-array order at `00B6423E`; `00B6423C/3D` passes the same state and either incoming influence (fast path) or local product descriptor (general path). General child admission requires at least one sample's positive post-filter local influence, recorded at `00B64150`; it is not a blanket per-cell incoming-product-positive gate. Nonpositive child products may still suppress a particular affector inside its own process.

**Confirmed consequence under prepared valid finite request state and positive sample count:** root 0 has no boundaries/filters, so fallback local influence is one. Root influence input is one; its enabled affectors complete before child 1, then 2, then 3, then 4. Root AHFR and an eligible sibling-3 AHFR store can therefore precede layer-4 FHGT's read of the same cell. Layer-4's own AHFR runs after its filters. **Unknown:** whether FFRA gates, actual fractal values or later operation branches permit/change each individual sample. No original generated cell was executed or observed.

Before sampling, enabled filters' virtual+18/+1C demand predicates can trigger derived-cache rebuild calls `00B65730` / `00B65AF0` when request flags+570/+571 are dirty, then clear the respective flag. After affectors, virtual+14/+18 predicates can set those flags. FHGT vtable+18/+1C resolve to `00B63420/30`, both literal false. AHFR virtual+14 is `00B76980`, literal true, so its post-call predicate marks +570 dirty; its other predicate and all rebuild math/lifetimes are not assigned new semantics here. These are derived-cache invalidation boundaries, not cached FHGT query results. The accepted [FSLP contract](TERRAIN_FILTER_SLOPE_CONTRACT.md) identifies its virtual+18 at `00B79020` as literal true and +1C as literal false, rechecked here. Thus the examined enabled sibling-2 FSLP can demand the first derived-cache rebuild before its query when +570 is dirty. AHFR marks that flag after its call even if its internal product gate returns without changing a height. FSLP/AFDN numerical behavior and the cache rebuild results remain separate prerequisites.

## Verification, limits and next prerequisite

Final research replay verifies 150 new exact instruction anchors, two RTTI objects and 18 vtable entries. It also reruns the frozen preparation verifier (88 anchors, 52 ADTA prerequisite anchors, getter and nine finite status checks) and FHGT verifier (90 anchors, five shared-height/caller anchors, 18 finite status checks). These counts are separate evidence inventories and are not added to a coverage denominator.

The single selected-source walk reads 171 framed records within fixed caps of 512 records, 64 visited layers and depth 16. Identity negatives cover mutated/truncated EXE and config before optional decoder loading/archive lookup/output, plus four synthetic wrong-byte/wrong-size source/generator identity gates. No original process, generated-height evaluation, live server, packet capture, GPU, downloaded toolchain or production API change occurs. Original malformed-input handling, allocation failure, undefined/nonfinite arithmetic and concurrency remain Unknown.

The closed prerequisite is the **real selected topology, fresh-load preparation, shared-height identity and recursive write-before-read ordering**. The exact next question is whether selected root/sibling AHFR and preceding FFRA can be reproduced from the pinned fractal family/map state and `013E7C00`/`00B75B70` boundaries under explicitly identified original controls. Until then, keep the modern FHGT/AHCN slice synthetic and exclude full real terrain admission.

## Round45 accepted compatibility increment

Follow-up: [AHFR conditional consumer](TERRAIN_HEIGHT_FRACTAL_CONTRACT.md) and [FFRA conditional consumer](TERRAIN_FILTER_FRACTAL_CONTRACT.md). The original unresolved boundaries above remain explicit.

## Round46 accepted compatibility increment

The [family lookup](TERRAIN_FRACTAL_FAMILY_LOOKUP_CONTRACT.md) and [sampler](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md) establish static prerequisites. A miss may retain wide ST0 after writing the F32 cache, whereas a hit reloads F32. Existing supplied-F32 APIs therefore do not establish original sampling parity. The [903-case supplied consumer join](TERRAIN_SUPPLIED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) independently verifies mutable height, query timing and retained influence composition without generating noise. Exact seed/table/interpolation, optional postprocessing, effective controls and reload quiescence remain open.
## October 13 derived-normal follow-up

The [slope normal producer contract](TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md)
now closes the first derived-cache rebuild at 00B65730: current-height positions,
triangle-normal accumulation, normalization and state+3C normal Y consumption.
It also confirms rebuilding before the demanding layer's cell loop. This extends
the earlier structural timing result; original arithmetic/control parity and
the second cache rebuild remain outside that closure.
