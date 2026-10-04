# Terrain chunk request and fresh margin

## Scope and input identity

This static probe closes fresh-constructor provenance and the worker record's
builder argument order. It does not establish settings after construction or
capture an actual request. All VAs refer to executable SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The [coordinate producer](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md)
defines the subsequent arithmetic and storage graph.

## Evidence and consequences

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Client constructor calls base constructor `00B5D7C0` at `0084937C`. Base stores literal3 to member+58 at `00B5D7F1`, and literal2 to+5C at `00B5D7F8`. | Confirmed fresh defaults. The producer uses+58 as its margin factor; the meaning of+5C remains Unknown. | Fresh margin3 is evidence-backed. Keep the current margin explicit because later mutation has not been audited. Do not interpret+5C as the sample border. |
| `00B5D970/973/976` loads+58, adds the signed constructor argument and doubles the result. | Confirmed dimension calculation consistent with the builder's count graph. | A bounded replacement checks its count and allocation before construction; this does not prove legacy overflow rejection. |
| `0085EECA/CD/D2/D8` copies five words from node payload+14 into a local record. `0085EF28/32/36` copies five words into completion storage and advances its end by20bytes. | Confirmed 20-byte transferred record. Node header and record payload are distinct. | Copy request values into owned modern work; do not retain a borrowed node pointer. |
| `0085EF5A/5D/60` reads record+8/+4/+0; `0085EF64/65/66` pushes them before call `0085EF69` to builder `0084A530`. | Confirmed first three builder argument sources. First/second signed coordinates and signed scale are strongly inferred from the independently checked builder consumers. | Preserve ordered first, second, scale words. This supplies provenance, not an actual observed request value. |
| After building, `0085EF84` writes the completion pointer into the last copied word. | Confirmed result-slot write to record+10. Scheduling meaning of record+0C is not closed here. | Modern ownership/generation tokens may differ internally; stale completion rejection remains required. |
| `0084A782/798` copies member+5C into temporary parameter+8. | Confirmed structural propagation only. | Retain this as an unassigned parameter; no width, LOD or border semantics are inferred from literal2. |

The worker's fourth builder argument and the scheduling units are outside this
probe. Earlier [terrain lifecycle evidence](WORLD_TERRAIN_ARCHITECTURE.md)
remains the source for queue synchronization, cancellation and teardown. No new
claim about every caller or constructor failure path is made.

## Corpus and arithmetic limits

The pinned Tatooine PTAT0014 supplies chunk width8.0 and tile count2. Combining
those original metadata words with fresh margin3 and a **synthetic** scale1
would give count10, derived spacing4, step2 and margin6 under finite nearest
policies. This is a conditional illustration, not an original work-record or
coordinate comparison. Actual phase precision/control words remain Unknown.
No additional asset sweep or original executable execution occurred.

## Reproduction and next probe

[Static verifier](../tools/python/verify_terrain_chunk_sample_requests.py)
SHA-gates the original, rechecks58 coordinate anchors and the half constant,
then checks23 request/default anchors. Its receipt belongs under ignored private
storage. Instructions and call order are Confirmed; joined coordinate/scale
names are Strongly inferred; later settings and record scheduling semantics
remain Unknown.

```powershell
python tools/python/verify_terrain_chunk_sample_requests.py --exe C:/Stardust-DEV/Stardust.exe --packages local-private/toolchain/python-static-analysis-packages --receipt local-private/parallel-development/round38/chunk-request-static.json
```

Next bounded probe: connect member+58 through the generated ClientChunk sample
border to the accepted nine-sample assembly, and inspect setters/configuration
before selecting a production default. A complete layer generator remains a
separate implementation milestone.
