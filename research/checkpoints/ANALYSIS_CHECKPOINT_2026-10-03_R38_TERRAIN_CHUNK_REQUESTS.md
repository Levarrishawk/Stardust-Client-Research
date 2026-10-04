# Round38 terrain chunk requests — 2026-10-03

## Question and stopping condition

Where does the coordinate producer's member+58 originate on fresh construction,
and which worker record words reach its first three builder arguments? Stop
after directly checking the constructor and record-to-call chain. No later-write
audit, scheduling-unit interpretation or runtime capture is included.

## Inputs, tools and evidence

Original executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
SHA-gated PE/Capstone static decoding; original executable was never executed.
The [durable contract](../../docs/TERRAIN_CHUNK_SAMPLE_REQUEST_CONTRACT.md)
records every material address, interpretation, confidence and consequence.
The [verifier](../../tools/python/verify_terrain_chunk_sample_requests.py)
passed58 inherited producer anchors, the half constant and23 new anchors.
Detailed disassembly and receipt remain private and regenerable.

## Findings versus interpretation

Confirmed: fresh+58=3, fresh+5C=2; constructor dimension is twice(+58 plus its
argument); worker copies20-byte payloads; record+0/+4/+8 become ordered first,
second and scale builder arguments; record+10 receives the completion pointer;
+5C propagates to parameter+8. Semantic coordinate/scale names are Strongly
inferred from the separately verified builder. Meaning of+5C, scheduling units,
later mutations and actual work values remain Unknown.

## Compatibility and unresolved edges

Fresh margin3 supports an explicit supplied default only when construction
provenance applies. Modern requests must retain explicit margin and phase
arithmetic policies, own transferred values, check dimensions, and reject stale
completions. The same pinned original metadata supplies width8/tile2; any
illustrative scale1 request is synthetic. No coordinate parity or full terrain
generation is claimed. Existing lifecycle evidence is retained; this probe does
not close every caller/error path.

## Exact next action

Trace+58 through ClientChunk sample-border construction to the existing
nine-sample assembly, then inspect setters/configuration. Reproduce with the
command in the durable contract before relying on these VAs.
