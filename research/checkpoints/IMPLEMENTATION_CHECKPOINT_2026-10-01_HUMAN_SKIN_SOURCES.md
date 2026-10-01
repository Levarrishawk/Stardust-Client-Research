# October 1, 2026 — Original human MGN skin-source prefix

## Question and stop

Can the unchanged object-root detail0 body/head MGN sources decode all position/transform-weight arrays with owned bounds and independently matching values? Stop before primitive generation, skinning or rendering.

## Evidence

Read existing MGN texture-renderer/corpus specifications and curated loader rows. Exact prefix layout was insufficient in durable docs, so ran existing read-only Ghidra project against verified baseline loader00800EF0. No analysis changes or executable execution. Its INFO reads nine4-byte then four2-byte values; subsequent SKTM/XFNM lists use the third/fourth words, POSN uses the fifth with twelve-byte records, TWHD one4-byte record per position, TWDT the sixth word with two4-byte fields per pair, and optional NORM the seventh with twelve-byte vectors. Shader/blend counts are retained; extra leading words/short-tail meanings are not invented.

Original inputs remain the pinned48-archive effective overlay. New priority53 sources:

|Resource|Bytes|SHA-256|
|---|---:|---|
|appearance/mesh/hum_m_body_l0.mgn|161,413|93378D74AA75B1C6673C9008914FFB51CF7DC380FF18459DBBFC26EF016A59B0|
|appearance/mesh/hum_m_head_l0.mgn|148,489|84DFF58013C8374D80D691549A67E009A84344649010C9A4365552C9FCE48F19|

Twelve source union329,296 bytes. Body884 positions/1,404 pairs/884 normals/35 transform names; head570/766/634/17. Every per-position group consumes its declared consecutive pairs, sums within1e-4 of1 and references an in-range transform index. Every transform name uniquely matches a referenced original skeleton under supported folding. Data-driven influence semantics are Strongly inferred; exact raw structure is Confirmed. Full generation consumer remains a named next boundary.

## Implementation and verification

Owned bounded SKMG0004 prefix and optional bundle detail selection preserve all original values. Later forms/chunks remain owned metadata, including body/head blend shapes, hardpoints, occlusion and TRTS. Retention is not semantic validation or texture generation. Full native smoke passes mutation/truncation/invalid count/index/sum/nonfinite/negative weight/version gates. Original masked selected MGN rejects. Independent descriptor verifier matches all twelve source identities, exact prefix-array canonical hashes, group invariants and named-joint correspondence. Original inputs are unchanged; synthetic fixtures are separately labeled.

Run `--skeletal-skin-sources` with prior pinned object/SAT/configuration options. Existing [descriptor verifier](../../tools/python/verify_skeletal_descriptors.py) audits the output; [durable prefix contract](../../docs/SKELETAL_MESH_SKIN_SOURCE_CONTRACT.md) specifies limits. Raw/decompiler/generated evidence stays ignored/private. No research-score or semantic-name count changes.

Next: trace PSDT reader007FCB50 and decode vertex maps/triangle records/UVs, retaining occlusion and generated-texture bindings before any neutral bind-pose preview claim.
