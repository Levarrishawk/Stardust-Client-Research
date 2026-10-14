# Round76: scalar lookup storage

Date:2026-10-06. Question: what storage does007D3790 return to CKAT constructor00822FC0, under which key/miss conditions, and does that establish pointer lifetime? Stop: one262-byte lookup plus necessary tiny accessor, at most12 explicit metadata candidates; no constructor/consumer/actor reanalysis.

## Inputs and identity

Trusted round76 sync passed4469 frozen accepted75 files and verified own75 sealed hashes, preserving worker HEAD/index/private data. Frozen manifest873d99dd4dcefb51300e68942dbf31e5bd71d24c593fbd9e2ceb6f3ba497fa97; archive2887d1c027dff45fcb834e72228359219f52528985e0631470b9734f7926a845. Read START/latest75/overnight, applicable repository instructions, scale-source contract, current status/ledger/unknown and prior checkpoint/canonical metadata. Cached path discovery found no separately named007D3790 decompilation; original bytes were used. An initial Windows rg glob-as-path search failed; corrected --glob directory search found only accepted75 references. No semantic evidence rests on the failed search.

Original C:/Stardust-DEV/Stardust.exe SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 was confirmed before VAs. Cached Python/Capstone/pefile and existing bounded RawPe/output-path helper were reused; no downloads/execution/toolchain mutation. Exact inventory supplied explicit helper sizes and comparator label, as correlation only.

## Findings and confidence

Confirmed: lookup object+10 holds lazy container pointer; search returns node; sentinel/comparator tests choose hit versus miss. Both exits calculate result node+20. Scalar interpretation comes from accepted73 consumer, not a new float load here. Miss prepares a caller-key temporary at EBP-2C and zero bits at EBP-1C, invokes007D40C0 and returns its result node+20. Effective+0 float insertion is Strongly inferred, not Confirmed, because backend copying/transformation was not expanded. No default1 evidence.

Lookup007D3790..007D3895:262 bytes/89 instructions, SHA38fbd29cbcccf7cd4d55d2db94a80de7b7ff4bac923a185f601d172d1bdbdbc4. Sole tiny accessor007D3C40..007D3C4F:16 bytes/eight instructions, SHAab3ff97c89ef583e501b6ef4b4a9ef626b38883a4dc75de8e0eb1b475b770146, copies one pointer-sized word and returns this. It closes hint forwarding without proving node ownership. Nine calls/five branches and bothret4 exits mapped. No other bodies expanded.

Strongly inferred: ordered associative node storage, key comparison and insert-with-hint roles, borrowed interior pointer. Unknown: original lookup type/key type/name/encoding/units, allocator ownership, mutation/erasure/destruction and pointer stability.01923F7C remains virtual-only data; no runtime name can be obtained from file absence. Local allocation-null branch proceeds with zero container; delegates/exceptions are unexpanded, so no defined graceful-failure claim. No local malformed-pointer/range validation, no executed failure behavior.

Ten explicit candidates of12: lookup and its eight distinct direct delegates plus key metadata. Seven delegate bodies remain unexpanded; only tiny16-byte word-copy opened. No fresh global census, RTTI/type guessing, constructor/actor/evaluator bodies, assets or active runtime instance proof. September25 actor closure remains accepted at original confidence; this adds no actor coverage.

[Durable storage contract](../../docs/COMPRESSED_ROOT_SCALE_STORAGE_CONTRACT.md) contains exact role anchors, confidence, ownership consequences and next action. Only own new contract/checkpoint and portable test directory are changed; existing scale-source/history/shared canonical artifacts are reserved for lead integration.

## Validation and resume

Portable runner (external historical reference) uses explicit paths and fresh ignored output. Independent raw/pefile mapping agrees;89+8 instruction coverage,21+4 anchors, nine calls/five internal aligned branches and19 synthetic rejection guards pass. These reject verifier identity/truncation/mutation/bounds/anchor/key-file assumptions, not legacy CPU cases. No native production build applies to this static documentation milestone.

Next precise probe: separately authorize1484-byte007D40C0 insertion writer, establish temporary zero-bit transfer to node+20 and identify immediate node allocator/erase/destructor for lifetime. A typed01923F7C key producer remains a separate unresolved boundary. Stop and idle after sealed scope/validation; no self-dispatch.
