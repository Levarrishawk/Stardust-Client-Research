# Skeletal mesh skin-source prefix

## Implemented boundary

`SkeletalMeshSkinSourceDecoder` accepts SKMG/0004 with owned strict IFF framing and exact INFO/SKTM/XFNM/POSN/TWHD/TWDT prefix order. This is a skin-source reader, not a complete mesh-generator renderer. Optional NORM is decoded; later chunks/forms remain owned uninterpreted metadata, including DOT3, HPTS, BLTS, occlusion families, PSDT and TRTS. Their presence is retained and reported; semantic validity is not claimed by this prefix reader.

INFO is44 bytes: nine little-endian I32 words followed by four I16 words. Words0/1 and all short words are preserved without new semantic labels. Words2..8 supply skeleton-reference, transform-name, position, weight-pair, normal, shader and blend counts respectively. SKTM has count NUL paths; XFNM has count NUL transform names. POSN/NORM are count three-F32 records. TWHD has one I32 per position; TWDT has count eight-byte index/F32 pairs. Loader00800EF0 confirms widths, order and separate vectors. Grouping TWHD as consecutive per-position influence counts and TWDT as named-transform index/F32 weight is Confirmed by generator008031A0, receiver00836C00 and weighted evaluator00809A40. Each shader vertex uses PIDX to select influences and NIDX to select its authored normal, with the same complete weights applied to normal directions. The inspected normal-output helper does not normalize. See [consumer closure](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_SKELETAL_INFLUENCE_CONSUMERS.md).

Modern limits:64 skeleton references,2,048 transform names,262,144 positions/normals,1,048,576 pairs,4,096 shaders/blends,64 positive influences per position; finite vectors within1e6, finite weights0..1 and per-position sums within1e-4 of1. Exact array widths and complete consumption reject malformed inputs. Names must be nonempty and unambiguous under ASCII case folding. Weights are preserved, never renormalized or truncated.

## Object-root source closure

Optional `loadSkinSources` requires owned skeleton loading and explicit logical detail0..3 before source lookup. It selects the same exposed MLOD index for each SAT generator, requires the selected child and checks that referenced skeleton paths exist in the appearance. Missing/masked sources or budgets reject. Direct standalone SKMG generators remain outside the earlier MLOD-only bundle interface.

Original detail0 body/head retain884/570 positions,1,404/766 weights and884/634 normals;35/17 transform names each match exactly one joint in their referenced original skeletons under ASCII folding. The twelve-file union is329,296 bytes, each source externally resolved once. Independent original raw-array hashes agree; [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_SKIN_SOURCES.md).

Next is PSDT vertex/index/UV mapping and declared neutral bind-pose drawing. Blend customization, occlusion activation, generated face texture and animation are not implemented by retaining their metadata.
