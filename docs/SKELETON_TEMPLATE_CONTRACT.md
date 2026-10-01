# Owned skeleton templates

`SkeletonTemplateDecoder` accepts deployed SKTM/0002 and SLOD/0000 with owned strict IFF framing. One input is bounded to1 MiB/8,192 nodes/depth8. SKTM count is U32, followed by exact ordered NAME/PRNT/RPRE/RPST/BPTR/BPRO/JROR arrays. NAME contains count NUL ASCII strings; parent is I32, each quaternion four F32 in scalar-first WXYZ order, BPTR three F32, JROR U32. Authored names and all fields are preserved; JROR is not discarded when nonzero.

Modern acceptance requires at most2,048 joints per basic skeleton, exact array widths, unique nonempty authored names, parent-1 or earlier index, one root for nonempty skeleton, finite translations within1e6 and finite quaternion squared norm within1e-4 of1. Zero-joint skeletons are valid. These are bounded corpus-oriented restrictions, not a claim about arbitrary historical inputs.

SLOD/0000 INFO is I16, negative clamps to zero; exact count inline SKTM children follows. At most64 levels and8,192 aggregate joints are accepted. Unsupported inline types/versions reject. All serialized levels remain owned; exposure takes at most four, with skip-L0 removing the first only when multiple exist. Basic SKTM exposes one level. No bind matrix or animation evaluation is supplied by this decoder.

`FindTransform` is an explicit bounded ASCII case-insensitive adapter lookup; absent returns-1 and ambiguous folded names reject. Raw names remain unchanged. Original loader0082B320 constructs each name using00AA4050 with normalization enabled, reaching00AA4250/00AA2D10/0131D6E8. The default ASCII branch folds A-Z by+0x20; a locale branch exists and is outside this importer. The source human SAT's head attachment and SKTM Head joint thus match under the supported policy. General filename normalization and original CRC collision semantics are not reproduced by this simple adapter lookup.

`CreatureAppearanceDescriptorBundleReader` optionally loads skeletons through the existing shared source cache. The original union is ten unchanged sources/19,394 bytes. Body SLOD contains38/30/27/21 joints; face basic SKTM contains15. Head attachment matches exactly one body transform under ASCII folding at all four exposed levels. Source identities and canonical named-array hashes agree independently. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_SKELETON_DESCRIPTORS.md) records tests, pins and the corrected case assumption.

Grammar/invariants follow [field order](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_SKTM_V0002_FIELD_ORDER.md), [hierarchy audit](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_SKTM_HIERARCHY_INVARIANTS.md) and [SLOD runtime](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_SLOD_RUNTIME.md). Disk structures are Confirmed. Next is recovered bind-transform construction with independently computed original matrices; renderer skinning remains separate.


[Recovered bind construction](SKELETON_BIND_TRANSFORM_CONTRACT.md) is now implemented and independently checked on176 original body/face matrices. Renderer skinning/animation remains separate.
