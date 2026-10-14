# Retained filter-only family height field

`TerrainFamilyFilteredHeightField.Build` is an immutable engine-neutral composition of the accepted selected FFRA receipt, retained filter consumer, layer influence product and per-cell height field. The caller opts into one modern policy: no supplied boundaries, fallback local influence one, one enabled selected filter, no inversion. Original layer admission, ancestry and traversal are not implied.

## Boundary and identity

[Core implementation](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainFamilyFilteredHeightField.cs) accepts an immutable family set, an accepted [operational selected FFRA receipt](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md), raw enabled AHFR bytes, the [existing field request](TERRAIN_FAMILY_HEIGHT_FIELD_CONTRACT.md), complete prior-height and incoming-weight arrays, limits and cancellation. Controls explicitly specify table/construction/noise-query/filter-query/combine/product/height precision, each 24/53/64. The first four height-route controls are reused inside `Sampling`; filter query, combine and product are separate controls.

Dimensions are 2..128, default maximum 256 and hard maximum 1,024 cells. Both arrays must have exactly W*R row-major finite binary32 words. Aliased caller arrays are allowed and copied separately. Origin and spacing use the unchanged frozen axis helper: finite coordinates, positive spacing, finite strictly increasing stored axes under binary24 and the existing bounded display policy.

The selected filter must have `EnabledOperationalSubset` policy and enabled/operational state, exact group hash/byte length and the identical family object returned by the supplied set's exact signed lookup. An independently decoded equal-hash set is deliberately rejected: it is not the accepted binding's family identity. FFRA version/decoded fields, family key, finite range/amount/scale, modes0..5, curves off, octave count1..16, seed guard and finite used raw fields are revalidated. The selected owned rule length/hash is rechecked. The selected receipt's immutable constructor already binds source generator/group/rule extents; no new generator or original parent activation is inferred.

Strict AHFR `Prepare` runs before filter sampling even if every incoming weight is zero/negative. It is reused without edits. Enabled/typed operation0..3, finite scale, exact signed family and supported used raw state must all pass. The downstream field later performs its own strict preparation; counts distinguish the one prevalidation from its one decode/lookup. No existing production file is edited.

## Actual retained composition

One fresh full-rectangle FFRA sampler queries every unique row-major cell once, regardless of incoming sign. Every result must be an actual `ConditionalComputedMiss`. It supplies its retained sample directly to `TerrainFilterFractalInfluence.Query`, then `FromRetainedFractalFilter`, then the one-filter `TerrainLayerInfluence` composition. Incoming weights are the owned actual cell values. The query performs its mandatory scaled binary32 store; the retained query value reaches shaping/combination without a premature inspection projection. The stored final products feed the unchanged `TerrainFamilyHeightField.Build`.

Cells retain actual computed result, filter query, carrier, influence receipt and the identical downstream height cell. Query input is the selected receipt's Input; sample identity survives through query/carrier, and carrier identity survives in the influence sample. Downstream influence words equal product words exactly. Each receipt retains full rectangle index/column/row and actual incoming weight. Public arrays/cells are read-only wrappers over inaccessible owned backing storage; copy methods return detached exports. The immutable selected receipt and resulting HeightField are retained directly.

Local influence and incoming-product gates are distinct. Filter queries still execute when incoming is +0, -0 or negative. Reversed/equal finite thresholds and unknown selectors can yield local zero; finite negative/large feather amounts are clamped only in the consumer. Nonpositive final products produce explicit downstream skips with exact prior-height bits and null Applied. If every product is nonpositive, there is still one FFRA sampler and zero HEIGHT samplers. Filter normalization/query failures cannot be hidden by zero incoming values; eligible height normalization/query failures can be skipped when products are all nonpositive. Separate filter and height families test this distinction.

The filter sampler is disposed before downstream height construction. Height sampling is independently fresh and local to the frozen field. The wrapper returns only after complete filter queries/products, complete field construction, complete cell assembly and final cancellation check. Any filter query/consumer/product/height/cancellation failure publishes no partial field or counters and cannot mutate a prior result. Using scopes establish cleanup on exceptions; tests verify prior-result preservation and retained/reference-sampler usability/disposal. Precise timing of mid-call cancellation and instrumented failed-wrapper disposal counts are not claimed.

## Complete reservations before snapshots

Let L be AHFR bytes, F selected FFRA bytes, A=W+R, N=W*R and O the filter octave count. The frozen field's all-positive reservations are:

- HC = L+4*A+100*N+256 copy/output bytes.
- HB = 8*L+65536+9512+136+32*N+1696*N route bytes.
- HW = L+32*A+24*N+L+20256+N+8304*N work.
- QB = 1456*N query bytes; QW = 8257*N query work.

Outer shared reserves S=8*L+65536+9512+136+32*N bytes and T=L+20256+N work cover the independent AHFR prevalidation and FFRA setup/cache/request retention. Wrapper caps must accept:

| Aggregate | Reservation |
| --- | --- |
| Copy/output bytes | HC+L+F+8*N+4*A+512*N+256 |
| Route bytes | HB+S+1760*N |
| Work | HW+T+8272*N+32*A+F |
| Filter query bytes/work | QB/QW |

Every outer cap and every downstream field cap is checked before any rule or operand snapshot, selected rule export, axis/query allocation or sampler creation. Both sides reserve every cell positive at16 octaves. Owned operands are then validated and used exclusively; later borrowed mutation cannot expand a preflight reservation. This does not promise a coherent simultaneous snapshot across independently copied arrays.

Default outer caps: 256 cells,65,536 rule bytes,4 MiB copy bytes,128 MiB route bytes,64 Mi work units,2 MiB filter query bytes and16 Mi query work. Hard outer maxima:1,024 cells,65,536 rule bytes,8 MiB copy,512 MiB route,128 Mi work,2 MiB/16 Mi query. Nested field caps/hard maxima are unchanged. All-zero products also require the worst-case caps. The wrapper implements a deliberately conservative logical policy, not measured CLR heap usage or elapsed time.

Actual route charge equals `HeightField.AccountedRouteBytes + S + N*(480+80*O)`; actual work equals `HeightField.AccountedWork + T + N*(80+512*O) + 32*A + F`. Thus actual per-cell filter query costs use the actual octave count, while downstream costs use its selected positive count. LogicalCopyBytes remains the conservative complete reservation. Counts report one height prevalidation, one filter construction/batch/disposal, N computed filter cells/queries/products, N*O kernel octave queries and the actual nested field counts, with explicit HeightPositiveCells/HeightSkippedCells.

## Verification and compatibility limits

[Independent Fraction oracle](../../SWG-Godot-Client/poc/round55-tests/Rendering/verify_filtered_field.py) matched [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainFamilyFilteredHeightFieldScenarios.cs):144 cases/62 expected rejects;1,818 accepted cells/1,582 applied heights;18 retained-vs-binary32 witnesses;153 ownership/atomicity/cleanup assertions. All82 accepted fields match frozen54 explicit filter-to-field composition. Eighteen uniform-positive fields also match the frozen fresh-per-cell patch. Six modes/three profiles, cross query/combine/product controls, zero/negative/mixed inputs, unusual finite range/selector/amount, normalization/query/late height failures, exact and one-below outer/nested caps, 256/1,024-cell shapes, aliases, borrowed/export mutation, read-only views, pre-cancel, repeat and foreign-thread isolation are covered.

One already pinned original FFRA/family selection is reused; its coordinates/prior/incoming/controls and AHFR are explicit synthetic inputs. Original-derived bytes/names and generated fixtures remain private. No new corpus survey or executable execution occurred. Confirmed: bounded modern behavior and independently matched receipts/identity. Strongly inferred: component compatibility from frozen executable-backed contracts. Unknown: original active arithmetic controls, ordering, ancestry/filter admission, original cache lifetime or generated-terrain parity. Godot/GPU integration is a separate lead gate.

Replay [Invoke-FilteredField.ps1](../../SWG-Godot-Client/poc/round55-tests/Rendering/Invoke-FilteredField.ps1) with explicit bundled Python, original EXE, asset/config roots, cached packages and dotnet. A fresh ignored private output root is required; linked/nonprivate/existing roots reject before creation. The producer verifies executable identity and the closed pinned selection. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R55_FILTER_TO_FIELD_PIPELINE.md) records the frozen baseline and exact next probe.
