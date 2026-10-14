# October 1, 2026 — Original human skeleton descriptors

## Question and stop

Can the object-root descriptor bundle retain every original body/face skeleton field and validate its attachment names without inventing pose math? Stop at owned structure, original named-array agreement and bounded name matching.

## Evidence and implementation

Read prior SKTM field-order/hierarchy/bind-route and SLOD runtime checkpoints; inspected stable registry rows and existing skeleton tools/private evidence. Rechecked executable SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VA work. Implemented owned SKTM0002/SLOD0000 structures, all serialized levels, zero-joint acceptance, raw JROR retention and declared topology/unit-quaternion/resource restrictions. Optional bundle skeleton loading uses one shared source cache.

Ten original sources total19,394 bytes: prior eight descriptors plus two priority53 skeletons:

| Resource |Bytes|SHA-256|
|---|---:|---|
|appearance/skeleton/all_b.skt|9,091|7F3EB762EB97B27C2170014BE66BFCA0C232E35B2F8CDBEFEA912736FB681CC3|
|appearance/skeleton/hum_m_face.skt|1,223|E6929997EC3646329905DD8B1C831DF9899302437973F5F112D77D88D0301F75|

Body inline levels38/30/27/21 joints; face15. Five levels/131 joints in total. Every field participates in a deterministic canonical hash (I32 count then each raw ASCII name/NUL, parentI32, RPRE/RPST/BPTR/BPRO F32 arrays and JRORU32). Independent original parser agrees on all hashes and source identities.

An initial exact-case attachment assertion failed: SAT says head, SKTM says Head. Inspection proves Head exists in all four body levels. Existing0082B320 decompilation constructs names using00AA4050(name,1); new direct instructions trace00AA4050→00AA4320→00AA4250, whose enabled branch calls00AA2D10. That normalizer calls0131D6E8, whose default branch maps ASCII0x41..0x5A by+0x20. A locale branch remains; default ASCII behavior is Confirmed by instructions. Added explicit ASCII-insensitive adapter lookup with absent-1/ambiguous rejection, preserving raw spelling. No general CRC map or locale-equivalence claim follows.

## Validation and limits

Full native smoke and original `--skeletal-skeletons` pass after correcting the assumption. Tests cover owned-input mutation, zero joints, nonzero synthetic JROR, cap/skip/negative counts, every truncated prefix, duplicate names, self/forward/out-of-range/multiple roots, nonfinite/nonunit rotations/translations, budgets and wrong version. Original skeleton masking rejects. Independent descriptor verifier now rechecks ten sources, all named-array hashes and unique folded head attachment across exposed body levels. No pose, skinning, animation or rendered character is claimed.

Reproduce with the earlier descriptor pins/options and `--skeletal-skeletons`; [verify_skeletal_descriptors.py](../../../SWG-Godot-Client/tools/python/verify_skeletal_descriptors.py) audits the resulting output using the configuration pin. Static name folding reproduces with [disasm_range.py](../../tools/python/disasm_range.py) at the addresses above against the verified baseline. Derived sources/decompilation remain ignored/private. No semantic registry count or research score change.

[Durable contract](../../docs/SKELETON_TEMPLATE_CONTRACT.md). Next: independently derived affine bind transforms using confirmed scalar-first RPST*BPRO*RPRE, rigid inverse and localInverse*parent/external order; then MGN geometry.
