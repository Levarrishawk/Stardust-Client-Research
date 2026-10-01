# October 1, 2026 — Skeletal influence consumers

## Bounded question

Do original TWHD/TWDT records become complete per-position influences, and which influences deform each shader vertex normal? Stop at loader-to-generator-to-renderer transfer, receiver/accessors, evaluation and original occurrence. Static verified executable baseline E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150; existing read-only Ghidra project and disasm_range.py. No original execution.

## Direct evidence

008031A0 uses POSN at+40/+44, TWHD at+4C, TWDT at+58. Its loop at00803416–0080346F consumes each position count, walks eight-byte pairs, remaps pair word0 through the transform-name lookup result array01924648, and passes position index/remapped transform/word1 to00836C00. Receiver stores count/start per position and appends both words. Missing name lookup or negative returned index exits before completing generation.

007FD560 transfers shader PIDX position references through00837080 and NIDX normal references through008372C0. The latter sets offset4 of the24-byte vertex record; position is offset0. Accessors00837B20/B40 separately fetch position/normal vectors.00837C90 and00837CB0 obtain influence count and pair by the vertex position reference, not normal index.

008078A0 constructs renderer vtable015E5874 and calls008088E0. That builder makes36-byte per-vertex records: first transform/weight, remaining count, source position3F32, source normal3F32; remaining pairs are separately appended in authored order. No four-weight truncation or renormalization is present in this transfer.00807BC0 reaches008096C0; selected mode2 reaches00809A40. This evaluator weights transformed positions and transformed normal directions by the same complete position influence group.0080A030 includes translation,0080A090 excludes it.007899D0 writes the resulting three normal words directly to the vertex buffer; this helper does not normalize. DOT3 branch similarly transforms tangent directions and preserves fourth component; shader lighting/final normalization and alternate optimized modes remain separate.

## Confidence and consequence

Confirmed grouping and transform-index/F32 weight roles, based on loader widths, transfer instructions, receiver records and float evaluation. Confirmed shader normal influence association with PIDX, while NIDX selects authored normal vector. Original body/head have2,170 pairs/1,454 positions and1,790 shader vertices; all source and binding audits already pass. The exact animated matrix producer, blend/occlusion/customization and optimized modes are not closed here. Modern binding palette remains explicitly supplied rigid poses, not recovered animation.

Promote complete influence semantics from Strongly inferred to Confirmed; implement normal/tangent deformation per shader vertex without incorrectly treating NORM as a parallel position array. No new canonical function names or metric changes. Curated descriptions are independently authored; raw decompilation/disassembly retained ignored/private.

## Next probe

Implement the engine-neutral shader vertex deformation boundary and compare positions/directions against independent original-byte probes before GPU pose inspection. [Skin source contract](../../docs/SKELETAL_MESH_SKIN_SOURCE_CONTRACT.md); [binding contract](../../docs/SKELETAL_JOINT_BINDING_CONTRACT.md).
