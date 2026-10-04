# Terrain sample coordinate producer

This bounded static contract connects the supplied pair consumed by terrain
boundaries to the chunk builder and generated position grid. It adds no original
terrain execution, complete generator implementation or default precision policy.
VAs refer to executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Observation, interpretation and confidence

| Observation | Interpretation and confidence | Implementation consequence |
| --- | --- | --- |
| PTAT0013 `00B6EDE2/EA/FA` and0014 `00B6F100/108/118` load signed integer member+2C, divide F32+28 by it and store F32+94; getter `00B6E6B0` returns+94. | Confirmed derived spacing operation; chunk-width/tiles names are strongly inferred in the [input contract](PROCEDURAL_TERRAIN_INPUT_CONTRACT.md). | Preserve signed division and the loading-phase F32 store; raw input and derived runtime values remain distinct. |
| Builder `0084A530` reads+28/+2C/+94 through getters, loads signed third argument at `0084A5A0`, multiplies it by derived spacing and literal half `3F000000`, then stores F32 at `0084A5C7`. | Confirmed spacing graph. Third argument is scale in the existing chunk/LOD chain, strongly inferred. | Generation spacing is conditional on the supplied scale and loading result; no shared phase precision is implied. |
| At `0084A5CC..EA`, builder scales signed member+58 by stored spacing into an F32 margin, then subtracts it from each signed first/second argument times stored+28. | Confirmed pair of origins. The [Round38 request probe](TERRAIN_CHUNK_SAMPLE_REQUEST_CONTRACT.md) confirms fresh constructor value3; later configuration remains unclosed. | Preserve multiply/subtract/store order and supply the current margin explicitly. |
| Builder writes count, spacing, first origin, zero vertical origin and second origin into its temporary parameter block at `0084A75B..8B`, then passes the block at `0084A79E`. | Confirmed parameter ownership and offsets. Count is twice(member+58 plus+2C), from `0084A5A3..B0`. | Parameter offsets0/4/C/10/14 mean count/spacing/first/vertical/second for this path. No checked-overflow legacy guarantee is established. |
| Layer `00B63D20` iterates count squared, converts row to F32 at `00B63F2F/32`; column uses signed `fild` directly at `00B63F5A`. Both multiply spacing, add their respective origins, and store F32 at `00B63F63/6F`. | Confirmed coordinate producer, with different index storage points. | Do not replace both branches with a reassociated origin-plus-index expression without matching stores. |
| `00B63FA1..AF` pushes second then first stored coordinate and invokes boundary virtual+24. | Confirmed pair order, including the accepted BCIR query. | First argument comes from column/parameter+C, second from row/parameter+14. |
| Position builder `00B65730` uses the same spacing/origins; `00B657D2/E3/E9` writes first, height+vertical-origin, second into consecutive vector components. | Confirmed component correspondence. X/Y/Z naming agrees with established native position/collision consumers; strongly inferred for this joined chain. | Boundary first corresponds to native X, second to native Z, height to Y in this producer. This does not establish every asset or protocol field's coordinate convention. |

## Conditional arithmetic graph

`R_l`, `R_b`, `R_q` denote the respective arithmetic-instruction rounding policies
for loading, building and querying. `S` denotes each observed F32 storage
operation. Active original policies, rounding modes and exception masks remain
Unknown, as explained in the [runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md).
These symbols state the storage graph; they do not assert nearest-even operation
under the actual runtime. Signed integer loads are exact x87 integer loads before
the next arithmetic instruction, except the explicit row F32 spill.

```text
derived = S(R_l(chunkWord28 / signed(tileWord2C)))
step = S(R_b(R_b(signed(scaleArgument) * derived) * 0.5))
margin = S(R_b(signed(member58) * step))
firstOrigin  = S(R_b(R_b(signed(firstArgument)  * chunkWord28) - margin))
secondOrigin = S(R_b(R_b(signed(secondArgument) * chunkWord28) - margin))
count = 2 * (member58 + tileWord2C)           # legacy integer operations
rowFloat = S(signed(row))
first  = S(R_q(R_q(signed(column) * step) + firstOrigin))
second = S(R_q(R_q(rowFloat       * step) + secondOrigin))
```

The position-grid routine converts row and column with integer loads and stores
their completed coordinates. Its row branch does **not** have the layer's
intermediate row-index F32 spill. For ordinary small counts the integer values
are exactly representable; no universal equivalence for larger counts or altered
phase arithmetic is claimed. Generated Y is the stored height-grid value plus
parameter+10, with a final F32 store. This builder supplies positive-zero+10.

Generator `00B65280` derives a rectangle from origins and `(count-1)*step`, storing
minima at parameter+574/+578 and maxima+57C/+580. Layer bounds can reject before
per-sample work. Count<=0 skips the sample loops; that is not a modern valid-input
promise. Layer/rule enable and dirty-grid state still determine whether this
producer or position regeneration runs. The same worker and synchronous builder
routes established earlier remain distinct; this probe does not establish every
reachable caller or its active control word.

## Ownership, corpus boundary and error policy

The builder owns its temporary parameter block for the synchronous generator call
and conversion into `ClientChunk`; generator/layer code borrows it and writes
mutable output grids. Existing [lifecycle evidence](WORLD_TERRAIN_ARCHITECTURE.md)
covers worker request/result transfer and teardown. No borrowed legacy pointer is
introduced into the modern implementation by this research-only contract.

The previously pinned Tatooine PTAT0014 has chunk word8.0 (41000000) and tile word2; those
raw words support a conditional derived spacing of4.0 under ordinary finite
nearest policies. They do not reveal an actual work request, margin setting,
control word or generated coordinate occurrence. No new corpus sweep or original
coordinate comparison was performed. Integer overflow, allocation failure,
nonfinite generation and masked/trapped exceptional behavior remain unresolved;
a replacement must document bounded modern rejection before adopting the graph.

## Reproduction and next action

[Verifier](../tools/python/verify_terrain_sample_coordinates.py) SHA-gates the
original before decoding and checks58 exact instruction anchors plus the half
constant. Receipts stay private; no disassembly corpus is published.

```powershell
python tools/python/verify_terrain_sample_coordinates.py --exe C:/Stardust-DEV/Stardust.exe --packages local-private/toolchain/python-static-analysis-packages --receipt local-private/parallel-development/round37/sample-coordinate-static.json
```

Confidence is Confirmed for the checked instructions/storage/call order and
Strongly inferred for joined semantic axis/scale names. The
[Round38 follow-up](TERRAIN_CHUNK_SAMPLE_REQUEST_CONTRACT.md) closes fresh+58
initialization and the worker record's first three argument sources. Later
configuration and actual request values remain open. Next probe: join the
sample-border construction to assembly before original procedural generation
parity. Boundary/filter/layer combination is still a separate risk.
