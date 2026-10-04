# Round41 layer preparation and bounds gates

October3,2026. Question: what populates the layer's count/work/bounds gates,
and is the bound-presence flag the serialized unknown ADTA flag? Stop after
exact preparation, entry and consumer evidence with a reproducible verifier;
no full generator or unrelated dirty-region reinvestigation.

## Inputs and tools

Original executable SHA256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
is freshly checked before VA use. Accepted40 frozen input3174 files; manifest
`74cc0552f0715e52b782c9aec68dc003584fabbdd8987f55cc6684c971238ea8`;
ZIP `c788a27f07bb5d5b5c4613c6b54a015ecf673288e3518fadff2cad79dd94c746`.
START/latest40, living status/ledger/UNKNOWN, layer/ADTA contracts, direct
curated00612B00 row and prior checkpoint were read. Existing disassembly and
call-xref tools are reused; detailed mapped instructions/xrefs stay ignored.
New compact verifier uses cached Python/PE/Capstone; no original process or
native generated-terrain query is executed.

## Observations and interpretation

**Confirmed:** vtable015ED1A4+04 ->00B63C50 resets+40/+44/+48/+4C and derives
them from locally enabled boundary/filter/affector/child pointer lists. The
enabled getter reads byte+08; each admitted object receives virtual+04. This
closes the earlier supplied signed-gate fields' preparation identity.

**Confirmed:**00B64730 clears+52, returns early for inverted boundaries+50,
otherwise accumulates enabled local boundary rectangles into+54..60 and
recurses independently into enabled children. The child rectangles are not
folded into this parent's rectangle. Original finite entry comparisons accept
touching rectangles and reject disjoint ones; work eligibility depends on
prepared affectors or children, separately from boundary/filter lists.

**Confirmed:** general fast path requires no prepared local boundary/filter/
affector. Local work with enabled children uses owned legacy product storage;
sample-count growth replaces its buffer and stored dimensions affect row stride.
Complete invalidation/allocation failure/thread behavior remains **Unknown**.

**Confirmed:** generator00B65F20 and existing dirty-region00612B00 consume bounds
preparation; the latter's prior lifecycle remains authoritative. Serializer
00B64DA0 writes version0003 with normalized+50/+51/+68 ADTA words. Unknown-role
+68 is distinct from bound-presence+52. No new canonical semantic function
name or aggregate coverage percentage is assigned.

[The durable contract](../../docs/TERRAIN_LAYER_PREPARATION_CONTRACT.md) contains
the exact field/array/caller table, finite branch interpretation and compatibility
consequences. A replacement must prepare counts and bounds explicitly, preserve
inversion's recursion suppression, keep weight evaluation separate from actual
work eligibility and avoid claiming complete cache lifecycle from this probe.

## Reproduction and limits

```powershell
python tools/python/verify_terrain_layer_preparation.py --exe C:/Stardust-DEV/Stardust.exe --packages local-private/toolchain/python-static-analysis-packages --receipt local-private/round41/preparation.json
```

Use the installed bundled interpreter explicitly. The verifier passes88 exact
new instruction anchors,52 ADTA prerequisites,two getter instructions,five
layer vtable entries and nine independently interpreted finite status cases.
Direct-call scanning finds recursive00B6479F, generator00B65F3F and both
00612B00 bounds calls; these exact callers are separately decoded. Such linear
xref discovery is not a proof that no additional indirect callers exist.
One bounded contextual range crossed the selector jump-table data; those
decoded data bytes are excluded from every anchor/conclusion. A broad discovery
query returned excessive context; the exact offset/function reads replaced it.
No static assertion failed. There is no original runtime cache occurrence to
compare; raw ADTA occurrence evidence remains the prior40 result.

Final link/scope/whitespace gates belong to41 acceptance. No worker source or
shared registry is modified by this static probe. Unknown+68 semantics, real
filter dependencies and full cache activation are retained. Next exact probe:
one height/slope filter's query and cache-predicate behavior, with mutations and
call order checked before implementing full recursive traversal.
