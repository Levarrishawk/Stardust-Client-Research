# Logical animation tables

## Owned outer grammar

LogicalAnimationTableDecoder reads LATT/0000 with INFO: NUL ASCII associated hierarchy path, then little-endian signedI16 entry count. Each counted ANIM contains INFO NUL logical name and one nested typed animation-template FORM. The complete child IFF tree/payload is owned and retained; retaining PXAT/TSCL/DRAT/SPAT/SSAT/PBAT/AGAT does not implement their evaluators. Associated ASHT and referenced ANS files are not loaded in this milestone.

Limits:2MiB/100,000 IFF nodes/depth32/4,096 entries/2,047-character printable ASCII strings. Negative counts reject (original loader reports WarningStrictFatal); widths/version/order/tails/truncation/nonform children reject. Serialized names remain unchanged; normalized lookup names remove trailing ASCII spaces only, matching direct loader trim. Case folding for the modern index is explicit ASCII-insensitive policy, not an assertion of original CRC sorting.

## Defaults and duplicate names

Original loader007E1250 compares the trimmed literal against its distinguished name before constructing/sorting CrcString entries. Initializer007DFDB0 sets01924244 from string018765E0, confirmed bytes default. When absent, it appends a default with null template. The owned description preserves serialized order/count, tracks the authored or synthesized default and exposes a separately sorted modern index. Missing lookup returns DefaultEntry. Duplicate folded names remain serialized; ambiguous Find rejects. This deliberately does not invent the original duplicate-key winner.

Original all_m.lat contains908 entries/four duplicated names and no authored default; lookup count909. hum_m_face.lat has138 entries/no duplicate names and an authored default; lookup count138. Body top-level child families:PXAT426/TSCL400/DRAT28/SPAT18/SSAT24/PBAT11/AGAT1; facePXAT137/SSAT1. Both associated paths are appearance/ash/all_b.ash. Typed subtree hashes agree independently, without claiming their semantics.

## Object-root loading and evidence

Optional loadLogicalTables requires owned skeleton loading and validates LATX skeleton references before resolving each distinct table once. Original fourteen-source710,463-byte union includes the prior twelve-source geometry plus unchanged tables: all_m priority51/361,386 bytes, SHA8AFC6F77B4D769EFA1EA6764D7D4E80FF236F4B917DD50DDAFD45253DC29F494; face priority53/19,781 bytes, SHACD2488A8EEC959C3AE38D8BD968D8076CEAF7F4930AC3FC5B60B8E1A2A4B85C3. Full native owned/default/duplicate/negative/dependency/masked-source gates and independent effective-overlay source/outer grammar/subtree hash audit pass. Original executable remains static.

Native --skeletal-logical-tables uses existing object/SAT/configuration pins; verify_skeletal_descriptors.py audits table rows automatically. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LOGICAL_ANIMATION_TABLES.md). Next: select unambiguous logical locomotion entry, decode its proxy/scale envelope and load only the selected original clip. Animation evaluation and ASHT state selection remain open.
