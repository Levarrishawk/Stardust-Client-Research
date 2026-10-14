# Explicit skeletal animation bank contract

`CreatureAppearanceDescriptorBundle.AdditionalAnimations` optionally holds up to eight named `PreparedSkeletalAnimation` objects alongside the existing pinned selected idle. All use the same owned appearance skeleton and source cache; preparation finishes on the CPU before archive scope closes. A differing skeleton mapping fails. This executes finite joint sampling, not animation graph state or root motion.

## Explicit original routes

`LogicalAnimationClipSelection.ExplicitChildRoute` is an optional ordered list of up to eight child indices. Route traversal accepts SSAT/0000 counted ANMS children or SPAT/0000 U8-counted children, then the existing PXAT/TSCL decoder. A route is an explicit selection: SSAT value mappings/defaults and SPAT speed blending are retained raw and are not executed. Invalid versions/counts/forms/indices/depth fail; there is no fallback route.

`SkeletalAnimationBankProfile.Load` reads version1, exact top-level fields version/animations; each entry has exact key/table/name/sha256/route fields. Enforce unique keys, normalized appearance/lat paths,64-hex pins and bounded nonnegative route indices. The authored [human locomotion profile](../../SWG-Godot-Client/poc/preview-profiles/human-locomotion.json) explicitly selects loop_standing male branch child0 then walk1/run2. It is a preview selection, not a reconstructed default character graph.

Prepared additional clips may opt into validated [MSGS/translation-only LOCT](COMPRESSED_LOCOMOTION_CONTRACT.md). Root rotation still fails. `Locomotion` exposes owned records; no actor movement or callbacks occur. Existing prepared idle continues to reject opaque suffixes by default. Clip path/hash, frame rate/end, time scale and binding diagnostics remain available.

## Original verification

Pinned human object IFF -> inherited appearance -> SAT -> owned skeletons/MLOD/MGN and original LATT -> explicit SSAT/SPAT -> PXAT -> CKAT. Original bank adds only walk7509/run5843 source bytes to the selected-geometry/idle closure:17 sources/739,486 bytes, one external lookup per source. Both movement clips have39 transforms; polearm1_polearm/l0 are unused and hold_l retains bind pose. The composed skeleton binding has53 joints.

Native `--skeletal-locomotion-bank` uses the existing pinned object/SAT probe syntax. All222 quarter-frame prepared poses match direct sampled/joint-bound poses; this is a consistency check using core sampling, separate from the independent original222-position audit. Missing/masked/wrong-pin movement clips fail. The descriptor Python verifier independently audits effective source union/hashes and raw table child routes, clip fields and binding diagnostics. Build/full native/ownership/profile/route negatives pass.

Next: retained GPU clip switches and an explicit movement-linked phase policy. Original SSAT runtime state, speed blending, event callbacks and gameplay root authority remain unimplemented.
