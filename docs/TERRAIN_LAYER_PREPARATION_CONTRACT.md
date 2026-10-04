# Layer preparation, bounds and work gates

## Evidence and scope

This closes enabled-count and bounds preparation used before the general
[influence path](TERRAIN_LAYER_INFLUENCE_CONTRACT.md). It does not implement
recursive generation, capture original cache state or establish real filter
semantics. Executable SHA256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
is checked before imports/VA use by [the verifier](../tools/python/verify_terrain_layer_preparation.py).
[Checkpoint and reproduction](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R41_LAYER_PREPARATION.md).

## Enabled-count preparation: Confirmed

Layer vtable `015ED1A4` slot+04 is `00B63C50`. It resets four S32 counts,
walks each complete pointer array in source order, calls enabled getter
`00B62F20` (byte+08), increments only for nonzero enabled, then invokes that
object's virtual+04 preparation. Child-layer preparation therefore recurses.
Disabled entries are not prepared. Counts are computed rather than copied
from serialized ADTA or inferred from raw array lengths.

| Collection | Pointer/count | Prepared enabled count | Evidence |
|---|---|---|---|
| boundaries | +14/+18 | +40 | 00B63C5E/75/7C |
| filters | +20/+24 | +44 | 00B63C8E/A5/AC |
| affectors | +2C/+30 | +48 | 00B63CBE/D5/DC |
| child layers | +38/+3C | +4C | 00B63CEA/D02/D09 |

These are local enabled predicates. This does not establish every external
caller, edit invalidation, ancestor activation or concurrent mutation policy.
The replacement must distinguish complete list length, prepared enabled
count and per-entry enabled state; a stale count is not a valid fresh cache.

## Bounding preparation: Confirmed finite graph

`00B64730` always clears byte+52. If boundary inversion byte+50 is nonzero,
it returns immediately; prior bound words remain and enabled children are
not recursively updated by this call. Otherwise it sets minima+54/+58 to
`7F7FFFFF` and maxima+5C/+60 to `FF7FFFFF`. Each enabled boundary sets+52
and receives the four-word accumulator through virtual+28. Child layers are
then independently visited through their local enabled getter and recursively
prepared; their rectangles are not unioned into the parent rectangle here.

Byte+52 means this preparation found an enabled local boundary, not that every
boundary is finite/nonempty or that the rectangle is numerically valid. Specific
boundary union behavior is separate. [ADTA byte+68](TERRAIN_LAYER_ADTA_RUNTIME_CONTRACT.md)
is a distinct unknown-role field. No arithmetic precision profile is identified
by this flag mapping.

General entry `00B63D20` returns without local/child work when both prepared
affector count+48 and child count+4C are zero. Thus boundary/filter-only layers
can have nonempty lists while producing no work in this path. If+52 is set,
four finite comparisons reject disjoint rectangles against request words
+574/+578/+57C/+580. Touching edges survive; the request words' complete
producer/units remain outside this probe. Signed comparisons and unordered
status behavior must not be replaced by guessed NaN support.

The no-local-work fast predicate requires counts+40/+44/+48 all zero.
Enabled children alone then receive the incoming weight grid. Otherwise the
general sample graph runs and positive child count uses the local product
cache. A supplied combination API can evaluate a weight without this full
runtime work eligibility; it must not claim to reproduce traversal from that
weight receipt alone.

## Weight storage and independent callers

With enabled children and local work, `00B63E16..50` checks the request's
sample count against retained dimensions+70/+74, frees+78 when growth needs
replacement, allocates count squared times four bytes, and saves the new
dimensions/pointer. A smaller requested count can reuse larger dimensions;
the original row stride+70 matters when indexing the product cache. Allocation
overflow/failure, all resets and threading are **Unknown**; no replacement
allocation policy is derived from unchecked legacy arithmetic.

`00B65F20` walks top-level generator layers, checks local enabled and calls
the bounds preparation. Existing verified dirty-region caller `00612B00`
also invokes it before and after terrain-modifier placement changes and tests
byte+52 before taking a rectangle. This correlates with the prior
[dirty-region lifecycle](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_DIRTY_REGION_CALLERS.md)
rather than repeating or reopening that closed notification family.

Layer serializer `00B64DA0` emits version0003, normalizes runtime bytes+50,
+51,+68 and writes three four-byte ADTA words. This confirms byte+68 survives
serialization; its generator meaning remains **Unknown**. No evidence here
justifies assigning it culling or cache eligibility.

Next: trace one actual filter's query and cache predicates, then connect only
the proved preparation/traversal transitions to a bounded runtime layer graph.
