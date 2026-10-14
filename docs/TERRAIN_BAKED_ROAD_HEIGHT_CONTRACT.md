# Bounded original baked road height

Exact AROA0005 with HDTA0001 now supports a strict baked road height profile.
The input binding (external historical reference) and
height operation (external historical reference)
implement target observations under declared modern binary64 arithmetic and
recovered F32 storage points. Conservative
[nonintersection admission](TERRAIN_ROAD_NONINTERSECTION_CONTRACT.md) remains
the first path. Possible intersections with unsupported baked data still fail.
Shader choice/RNG, flora clearing, editor regeneration and original x87 parity
remain outside this height-only slice.

## Identity and confidence

Rechecked target SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
RTTI018B8E84/vtable015EDD70 identify road process00B87E30. The original
terrain/TGEN identities and AROA0005 field order are pinned in the
[nonintersection contract](TERRAIN_ROAD_NONINTERSECTION_CONTRACT.md).
Statements below about target instructions/bytes are **Confirmed** unless
explicitly marked modern policy or Unknown. NGE revision
949451032647e45e42c3aaef3f41b132c8af36e3 AffectorRoad/Affector/HeightData
symbols corroborate roles only. No original executable was executed.

## Baked segment association and lookup

HDTA loader00B7CC50 selects0001 reader00B7CD40. For each framed child,
00B7CD68..78 appends a segment through00B7CE90, then calls00B7C7B0.
The append stores at the prior end and advances it by4 bytes. Segment loader
enters SGMT at00B7C7C1, obtains payload length/12 at00B7C7CD..D1, reads
F32 XYZ vectors via00A95810 and appends them in source order. No count prefix
is inside SGMT. Runtime lookup00B7D020 selects the segment by index and
calls00B7C3D0; segment geometry/index association is therefore explicit.

Road finder00B7BFF0 runs a complete vertex pass before its open-segment pass.
A strictly closer road vertex i takes baked segment i's first sample Y through
00B7C066..74; the final road vertex takes the preceding segment's final sample
Y through00B7C078..95. For a strictly closer open-segment projection, index i
and projected F32 X/Z are retained at00B7C176..19D. The final helper call at
00B7C1E7 uses that index/point to obtain height. Ties do not replace the earlier
winner; vertices therefore win exact endpoint ties and earlier entries win
equal-distance ties. Do not replace these two ordered passes with a generic
nearest-segment clamp.

Segment query00B7C3D0 independently clamps query X/Z to the first/last sample
rectangle at00B7C3E6..48B. It retains dx, spills dz to F32 at00B7C496 and
chooses X when abs(dx)>=abs(dz), otherwise Z. Positive-axis arrays scan forward;
negative-axis arrays scan in reverse, using strict sample-axis<query until the
first coordinate>=query. Each branch computes:

```text
height = F32(previous.Y
             + ((query - previous.axis)/(current.axis - previous.axis))
               * (current.Y - previous.Y))
```

Exact branch graphs: X forward00B7C4C0..4F9, X reverse00B7C501..53E,
Z forward00B7C558..595 and Z reverse00B7C59F..5DC. The fraction remains
retained; only final height stores F32. With an empty outer height list,
00B7D04D returnsfalse and caller00B7C1F0..200 supplies zero height. Nonempty
out-of-range indexes, empty segments or collapsed first-coordinate straddles
have no safe bounds handling in the observed code. The modern profile rejects
these structures instead of emulating unsafe/nonfinite behavior.
This refusal also applies to an otherwise valid road queried at its minimum
dominant-axis endpoint when the line wins after stored-distance rounding.
For the synthetic horizontal road0..20, q=(0,.1f), squared vertex distance
rounds upward at its F32 store. The identical endpoint line candidate then
strictly improves that stored best, queries the first baked coordinate and
reaches0/0. Target00B7C4C0..DB has previous=current=begin and no endpoint
guard. The helper rejects this specific collapsed straddle; the independent
oracle verifies its rounding premise. It does not invent endpoint recovery.

## Finder stores and final height write

The target finder stores F32 halfwidth-squared, each accepted best squared
distance, line t, projected X/Z and sqrt(best). A line t outside[0,1] is
discarded; exact corridor tangency requires strict best<halfwidth-squared and
therefore misses. Numerator uses stored F32 deltas while the denominator uses
retained coordinate differences at00B7C0E0..119. The implementation preserves
those F32 stores with explicit modern binary64 working arithmetic.

Road process first gates global boundary-poly enable and supplied amount>0,
then checks its point AABB expanded by full width at target F32 stores. The
global getter00B7BFA0 reads018B8B34, initially byte1 in the image. The helper
requires an explicit global-enable argument; traversal supplies true as a
modern operating choice. Later global toggles are not recovered here.

For a confirmed hit, distance is F32 and the core threshold remains retained:

```text
coreLimit = halfwidth * (1 - clampedHeightFeatherDistance)
if distance <= coreLimit:
    result = desiredHeight
else:
    result = F32(desiredHeight
                 + (currentHeight - desiredHeight) * (distance/halfwidth))
```

The inclusive core branch00B87FE1..00B8802A directly overwrites the height
cell. The outer branch00B880D0..F1 uses retained division/interpolation with
one final F32 store. Supplied amount is a positive gate; it never scales this
blend. The serialized height feather function is unused in this writer; no
NGE0006 fixed-height ramp or generalized feather shaping is substituted.
The target additionally clears flora in the core and chooses a shader under
its independent shader feather gate. These side effects remain unimplemented.

## Strict supported profile and original witness

The binding requires exactly one baked segment per road edge,2..8192 finite
XYZ samples per segment,65536 aggregate samples, coordinate bounds inherited
from the geometry decoder, bit-identical endpoint X/Z matching each ordered
road edge, bit-identical shared junction Y, a nondegenerate dominant axis and
strict monotonicity along that axis. Finite feather distance is clamped[0,1]
as the target loader does. Missing/mismatched/stale/degenerate/nonmonotone
profiles reject if conservative geometry retains a possible hit. Recovered
original exceptional arithmetic/control word remains Unknown; finite outputs
and valid positive straddles are mandatory modern eligibility guards.
Traversal reserves64+32*roadPointCount+8*maximumSegmentSamples work units per
query and caps total road-height work at8388608 before its sample loop.
Binding, exclusion, query loops and interpolation check cancellation, with
checks at least every64 vector/point iterations and each traversal sample.

Original pad road at TGEN offset98781 has45 and49 baked samples, decreasing Z
then increasing X, matching all edge endpoints. Its shared junction Y bits
represent49.185935974121094. It is enabled, width22, height feather distance
bits3EC41893 (approximately.383). The adjacent disabled road's baked endpoints
are stale relative to its current polyline; disabled handling avoids reading it.

## Verification and continuation

Standalone tests (external historical reference) cover all four
axis/direction branches, equal dominant-axis selection, vertex/segment ties,
negative heights/coordinates, core edges, strict corridor tangent, amount
gate/extrapolation independence, global disable, loader clamp and malformed
baked profiles. An [independent rational/Decimal oracle](../tools/python/verify_terrain_road_height.py)
checks the target graph's stores, winner identity, projection, desired height,
distance and final result. Original XYZ vectors remain only in private receipts.
The focused gate passes3054 assertions and2050 oracle samples across eight
profiles, including pre-cancelled binding/query and collapsed endpoint refusal.

Original chunk(-140,-161) has33 geometric hit cells (17 core,16 feather) among
196 padded cells. Tests repeat these under three positive amounts. These are
mostly padding hits; a2x2 region from(-140,-162) covers the visible road.
An overlapping independently generated1x2 region from(-139,-162) matches all
padded heights and normals; the joined patch set checks38 shared samples.
This proves bounded implementation consistency, not original numerical parity.

```powershell
dotnet run --project poc/terrain-road-height-tests/TerrainRoadHeightTests.csproj -- $OriginalTerrain $PrivateReceipt
python tools/python/verify_terrain_road_height.py $PrivateReceipt
```

Pass only a receipt path for synthetic checks. Next: parent independently
replays the oracle and whole selected-region/sparse/GPU gates. Full material
ownership, flora, unsupported original road profiles and original runtime
numeric controls remain separate. No research or address-label metric changes.
