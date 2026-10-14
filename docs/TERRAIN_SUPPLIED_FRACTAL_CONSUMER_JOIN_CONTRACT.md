# Conditional fractal consumer composition

Date: 2026-10-04. The actual Core runner (external historical reference)
joins accepted AHFR/0003, FFRA/0005 and FHGT inputs in a synthetic single-cell
sequence. It does not add an original sampler, family map, grid cache, normals
or recursive terrain driver.

## Order and arithmetic

The root AHFR consumes a supplied F32 sample and stores its height. A supplied
boundary gates FFRA; the mandatory FFRA sample-scale F32 store precedes threshold
testing. Its retained query enters the ordered minimum filter combination. If
local influence remains positive, FHGT queries the root's stored height and
enters that combination with its retained value. Local influence is multiplied
by the explicit incoming weight, with the selected precision and F32 product
store. Positive local influence admits the child AHFR; that consumer's own
product gate decides whether supplied child data is used.

This last distinction is deliberate: zero incoming product does not suppress
otherwise eligible filter queries. Disabled root and nonpositive child-product
paths skip unused sample validation. No complete original traversal or cache
invalidation behavior follows from this isolated composition.

All arithmetic profiles are explicit 24/53/64 choices. Serialized fields and
supplied operands are independently authored synthetic bytes. The original
sampler may return a wider retained x87 value on a miss; these accepted supplied
F32 APIs remain narrower. Original controls and noise are unresolved.

## Evidence and confidence

The Python peer (external historical reference) generates
the fixture using accepted independent Fraction/integer oracles and raw IFF
encoding before native execution. It imports no native outputs. The actual
Core reference build has zero warnings/errors; 903 native receipts match.
411 cases witness stale-height behavior, and40 witness prematurely projected
query behavior. Three explicit unused-NaN cases exercise boundary, product and
disabled gates. Receipts include both heights, query projections/current-height,
scaled sample, local/product stores, filter visits and child outcome.

Observation: independently calculated receipts equal actual native receipts.
Interpretation: the accepted supplied consumer and retained carrier boundaries
compose correctly under the enumerated synthetic profiles. Confidence:
Confirmed for this bounded composition. Consequence: a future terrain driver
must preserve these stores and gates; this does not authorize original noise or
terrain-parity claims.

Reproduce: generate a private fixture with `fractal_join_peer.py generate`,
restore/build FractalJoin.csproj with the sources-cleared accepted NuGet.Config,
run FractalJoin.dll with fixture/output paths, and run the peer's `verify` command
on them. No asset input or original executable execution is required.

Next action: consume the independently reviewed family-map and sampler findings
before designing a complete original-valued fractal terrain boundary.
