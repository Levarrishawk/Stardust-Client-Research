# Bounded road nonintersection admission

The original Tatooine sparse-survey road rejection at chunk(-140,-160) does
not require a road-height evaluator: every padded sample misses each enabled
road. Target executable instructions independently establish the required
geometric no-hit gate. TerrainRoadInput (external historical reference)
decodes exact AROA0005 geometry; TerrainRoadExclusion (external historical reference)
admits only conservatively separated samples. Possible road influence passes
to the separate [strict baked height profile](TERRAIN_BAKED_ROAD_HEIGHT_CONTRACT.md);
unsupported hit profiles still reject the chunk.

## Target identity and observation

Static input hash rechecked October13:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
RTTI018B8E84 names AffectorRoad; vtable015EDD70 slot+10 points to00B87E30.
Constructor00B87CC0 supplies AROA and installs that vtable. Loader00B881F0
recognizes0000..0005;0005 uses00B888E0. These identities are **Confirmed**.

The0005 loader reads inherited IHDR via00B62FA0, enters FORM DATA, calls
HeightData loader00B7CC50, then reads DATA: LE signed32 point count, count
F32 X/Z pairs, F32 width, raw32 shader family, raw32 height feather function,
F32 height feather distance, raw32 shader feather function and F32 shader
feather distance.00B8893C..83 loops points,00B8898D..98 sets width, and
00B8899D..00B88A80 reads the remaining five values, clamping the two feather
distances to[0,1]. Fields/order and width are **Confirmed** from target reads.
HeightData optionally enters ROAD or HDTA at00B7CC5E..80 and recognizes
0000/0001;0001 calls segment loader00B7C7B0. This slice accepts only the
original HDTA0001 framing, retaining complete encoded bytes and its SGMT
payloads without interpreting height values.

Road process00B87E30 checks global boundary-poly enable and positive supplied
amount, then its own extent.00B87F8A..A1 stores F32(width*0.5),00B87FC9
calls boundary-poly finder00B7BFF0, and00B87FD0 skips all road writes onfalse.
Finder stores radius-squared at00B7C005..10, scans vertices with strict distance
comparisons00B7C020..4A, then open segments. Segment t is stored F32 at
00B7C114;0<=t<=1 is checked00B7C11B..139; projected X/Z stores occur at
00B7C144/150. Squared distance updates require strict improvement at
00B7C16C..174. Final00B7C1B1..1BD requires best distance strictly below
radius-squared or returnsfalse at00B7C210. **Confirmed consequence:** no
geometric hit means no height/material/flora write, independently of HDTA
height semantics. Later NGE AffectorRoad/AffectorBoundaryPoly source merely
corroborates these roles; it does not supply the target acceptance oracle.

## Original occurrence and separation

Terrain SHA-256:
`6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`;
TGEN SHA-256:
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
Layer167 (parent141) is enabled, has no filters, uninverted ADTA flags0/0/0,
and a rectangle boundary. Its three AROA0005 records are:

| TGEN offset | Enabled | Width | Point count | SGMT sample counts |
|---:|---:|---:|---:|---|
|96801|0|8|3|79,37|
|98384|1|8|2|19|
|98781|1|22|3|45,49|

Their hashes are respectively
`4FFC74A07656B7F02D643C492BB0A0DC60F7DB00F0DB8398CE698093237C7CB6`,
`2F1C7F0B70486BD87F0914F666B5AF5F20381A2F7105DED2DFDCA3AFFD191033`,
and `FA6338DFE6B42C7371886753E25AB100BDC5AFAF447217D371185EAB328272BD`.
Actual modern source request(40m,tiles4,margin3,-140,-160,scale1) builds14
samples per axis: X-5615..-5550, Z-6415..-6350, step5,196 pairs.
The enabled width8 road is at X-5052, at least498m away. Width22 road runs
(-5839,-6188)->(-5609,-6458)->(-5224,-6457). For its first segment,
rectangle minimum oriented cross product is8270, length-squared125800,
giving distance-squared683929/1258=543.663752, distance23.316598m.
The second segment is at least42m away vertically. The entire padded
rectangle therefore misses both enabled roads; **Confirmed** original geometry
and independently verified modern coordinate/separation proof. Exact original
x87 arithmetic remains unproved.

## Explicit modern admission policy

Decoder bounds:1MiB,16384 IFF nodes,depth8,2..8192 points, finite coordinates
within +/-32768, positive finite width<=65536 and no adjacent duplicate
points. Known inherited IHDR and exact geometry DATA extent are mandatory;
each opaque SGMT has at least two12-byte vectors. These are strict modern
eligibility guards. Unknown versions and malformed geometry reject; disabled
rule handling follows the [common admission contract](TERRAIN_HEIGHT_TERRACE_CONTRACT.md).

Exclusion compares every supplied sample to every open polyline segment,
including endpoint caps, using binary64 clamped projection. Every distance
must strictly exceed **full authored width plus1 metre**, conservatively wider
than the target halfwidth. Tangent, near and ambiguous points are retained as
possible hits. The coordinate bound and1m cushion retain numerical uncertainty;
this is an explicit modern exclusion policy, not an exact target finder or
original rounding emulation. Per-call caps:1..1048576 samples and8388608
sample/segment pairs. No sampled terrain height or HDTA value is read.

## Verification and next action

Standalone tests (external historical reference) cover center,
actual/cushion tangencies, near cases, endpoints, negative coordinates, one
hit among misses, degenerate/nonfinite/out-of-envelope inputs and every cut
of a synthetic record. Ownership tests retain opaque snapshots. The optional
original mode checks three records, every padded sample and actual successful
chunk construction without applying layer167. The
[independent rational oracle](../tools/python/verify_terrain_road_exclusion.py)
uses exact squared distances via dot products, avoiding C# projection code.

```powershell
dotnet run --project poc/terrain-road-exclusion-tests/TerrainRoadExclusionTests.csproj -- $OriginalTerrain $PrivateReceipt
python tools/python/verify_terrain_road_exclusion.py $PrivateReceipt
```

Pass only a receipt path for synthetic tests. The follow-up
[baked road height contract](TERRAIN_BAKED_ROAD_HEIGHT_CONTRACT.md) now admits
the original monotone endpoint-matching profile through00B7D020->00B7C3D0.
Missing/unsafe/stale profiles and original exceptional arithmetic still reject.
At a confirmed geometric hit, target core height overrides desired height;
outer height uses desired+(current-desired)*(distance/halfwidth), and supplied
amount is a positive gate rather than a blending coefficient. Those observed
branches00B88013..2A/00B880D0..F1 are not implemented by this slice. Do not
substitute NGE fixed-height0006 behavior, generic road omission, or material
completion. Research scores and canonical address labels are unchanged.
