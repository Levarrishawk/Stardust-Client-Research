# Terrain layer influence: general per-sample path

## Scope and input identity

This is a static specification of the general sample loop in `0x00B63D20`,
not a complete terrain evaluator. The executable pin and coordinate producer
are recorded in [the coordinate contract](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md).
Run `tools/python/verify_terrain_layer_influence.py` with explicit executable,
static-package and private receipt paths to verify the pin, instruction
anchors, two selector tables, constants and status-branch interpretation.
Original execution and original generated-height comparison have not occurred.

## Observations and confirmed finite behavior

The incoming row-major weight is read at `0x00B63F49` and stored to an F32
local at `0x00B63F53`. The local accumulator starts at positive zero.
Layer boundary gate `+0x40`, boundary array `+0x14`/count `+0x18`, local-enabled
call `0x00B62F20`, and boundary query vtable slot `+0x24` select candidates.
No enabled boundary, or a nonpositive boundary gate/count, produces one at
`0x00B64026`. This distinguishes no boundary from an enabled boundary that
returns zero.

The two literal tables `0x00B642A0` and `0x00B642B0` map selectors 0/1/2/3
to identity/square/square-root/cubic `q*q*(3-2*q)` graphs. Larger unsigned
selectors discard the query and load positive zero. Graph instruction order
and arithmetic policy must remain explicit; active original precision and
exception policy remain **Unknown**.

At `0x00B63FE0`, each shaped boundary candidate is stored to F32 **without
popping** its arithmetic result. The old F32 accumulator is compared against
that unspilled result. A strictly smaller old value selects the candidate's
stored F32 word; other finite comparisons retain the old word, including ties.
The selected value is stored to F32. The boundary loop exits early only when
that selected value compares **equal to one** (`0x00B64000..0x00B6400B`). It is
not an observed greater-than-or-equal clamp. For candidates in `[0,1]` this is
an ordered maximum; arbitrary finite values require the actual early-exit
graph. In particular, a later value above one need not be examined.

Layer byte `+0x50` optionally replaces this accumulator with an F32 store of
`1-accumulator`. If the result is nonpositive, filter and affector work for
this sample is skipped. The comparison also skips an unordered result;
this does not establish a supported NaN/exception contract.

Filters use gate `+0x44`, array `+0x20`/count `+0x24`, the same enabled call,
query vtable slot `+0x14`, and the second selector table. Each shaped query
is stored to F32 while its unspilled value is compared against the existing
F32 accumulator (`0x00B640E9..0x00B640FC`). Strictly smaller query results
select the stored candidate; other finite comparisons retain the old word.
The selected F32 value exits the filter loop only when **equal to zero**.
For `[0,1]` this selects a minimum; it is not an arbitrary-value clamp.

Layer byte `+0x51` then optionally applies `1-accumulator` to the **combined
boundary/filter accumulator**, with an F32 store. This happens even when no
filter was queried. A second nonpositive/unordered gate skips affectors.
It is not independently inverting each filter or a separate filter aggregate.

Enabled affectors are visited in source order through array `+0x2C`/count
`+0x30`. Before each vtable `+0x10` invocation, the local accumulator is
multiplied by the saved incoming F32 weight and stored to an F32 argument
(`0x00B6416F..0x00B6418C`). The incoming weight does not participate in either
earlier positive gate. Affector-specific handling of a nonpositive product
must therefore be preserved separately. Later height/filter queries may see
state changed by preceding affectors; precomputing all layers is unproved.

When layer `+0x4C` is positive, the same product is stored into its row-major
weight cache at `0x00B641D3..0x00B641E3`, including samples that skipped
filter/affector work. Child traversal at `0x00B64220..0x00B64249` passes the
incoming grid in the no-local-work fast case, otherwise the layer's weight
cache (`0x00B64239`). This is a real recursive call to `0x00B63D20`.

## Interpretation, confidence and implementation consequences

**Confirmed:** the instruction/storage/selection/gating/product sequence
above. Conventional maximum/minimum descriptions apply only to the stated
bounded domain and explicit early exits. Finite equal signed-zero candidates
retain the old stored word. A rounded candidate must not replace the
unspilled operand in a comparison merely because both often yield the same
numeric result.

**Strongly inferred:** the incoming/cache product carries ancestor influence
to child layers. Array/call structure establishes propagation, while complete
parser-to-runtime field ownership and all cache activation transitions remain
outside this checkpoint.

**Unknown:** active original arithmetic profiles; all filter query semantics;
transformed boundary instances; complete culling/fast-path/cache lifecycle;
floral/shader/environment/normal generation; generated original terrain parity.
The current implementation may accept supplied query values under explicit
profiles as a bounded graph, but must not call that a full generator.

The next probe is a conditional owned per-sample combination receipt accepting
ordered supplied shaped/query results and incoming weight, with independent
precision/storage/early-exit fixtures. It must keep query callbacks and full
TGEN execution outside that first implementation.
## Round40 layer and CREO4 composition

[The supplied implementation](TERRAIN_LAYER_INFLUENCE_IMPLEMENTATION_CONTRACT.md) preserves retained queries, unspilled comparisons, F32 selected words, gates/inversions and separate incoming products. [ADTA mapping](TERRAIN_LAYER_ADTA_RUNTIME_CONTRACT.md) connects fresh serialized inversion words to runtime bytes; real filter queries and complete traversal remain unknown.
## Round41 supplied height execution and CREO4 fields

[Preparation](TERRAIN_LAYER_PREPARATION_CONTRACT.md) now establishes enabled counts, local bounds and retained cache stride. [Supplied ordered AHCN execution](TERRAIN_SUPPLIED_LAYER_HEIGHT_CONTRACT.md) and [original-actor grounding](SCENE_WORLD_LAYER_TERRAIN_PREVIEW_CONTRACT.md) are independently validated. Real filters/full recursive generation remain open.
## Round42 complete private state and supplied terrain sequence

[FHGT](TERRAIN_FILTER_HEIGHT_CONTRACT.md) directly reads query-time current height before this layer's affectors. [The supplied flat sequence](TERRAIN_SUPPLIED_LAYER_SEQUENCE_CONTRACT.md) threads height through prebound queries; it does not imply that real height filters may be precomputed for later layers. A retained filter adapter and dynamic query-time binding remain next.
