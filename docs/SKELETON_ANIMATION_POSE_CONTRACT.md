# Skeleton animation local poses

## Original local composition evidence

The verified baseline's skeleton update007E67E0 fetches RPRE/RPST/BPTR/BPRO through0082B8D0/E0/F0/900. Their direct instruction getters read template offsets+28/+2C/+30/+34. Instructions007E6917..007E6995 call the aggregate sampled rotation/translation query, multiply sampled rotation by BPRO, then RPRE, then left-multiply RPST before conversion. Result: `RPST * (sampleRotation * BPRO * RPRE)` in scalar-first quaternion algebra. Translation instructions007E6931..007E695A add sampled components to BPTR. Parent/attachment world composition follows007E69CC..007E69FD. These are Confirmed bounded operations; modifiers, aggregate controller mapping and inverse-palette cache ownership remain separate.

Constructor00822FC0 iterates requested skeleton transform names and queries00825310 for each. Extra clip records are not automatically mapped to a skeleton joint. A missing match selects separate static fallback evaluators. Loop direction and fallback branch are Confirmed; this implementation's explicit bind-pose fallback is a declared modern policy rather than a claim about every original controller fallback.

## Implementation boundary

`SkeletonAnimationPoseBuilder.Create` applies owned sampled values by explicit ASCII-insensitive names. Unsupported/duplicate names, invalid sample rotations/translations and topology reject. Missing skeleton transforms reject by default; callers may explicitly select BindPose, which retains the exact existing local bind and reports the missing names. Extra animation names are retained in the unused diagnostic. No synthetic joint or guessed attachment is inserted.

Matched joint quaternion follows the recovered order and is normalized for modern finite publication; translation is BPTR plus sampled translation. `Apply` patches exactly one selected skeleton in an existing attached binding, validating every source index/name and complete coverage; other skeletons retain their local binds. World hierarchy and inverse-bind skin palette use the previously verified engine-neutral boundaries. Facial bind joints therefore follow the animated head without receiving an unsupported facial animation graph.

## Verification

Native noncommuting hand fixture includes four90-degree rotations about distinct axes, additive translation, explicit unused `rifle`/missing-child reports and rejection of ambiguous/nonunit/nonfinite samples. `--skeletal-animation-pose` adds original local/world and position/normal diagnostics to the same pinned15-source object-root chain.

Original body has37 selected matching names; unused clip `rifle`; missing skeleton `hold_l`. This is Confirmed source correspondence, with explicit BindPose policy for hold_l. At frames0,30,59.5,90,120 the independent raw-source/double oracle verifies265 attached joint poses and8,950 shader vertices. Maximum matrix error9.958999709347438e-7; maximum vertex/normal error1.3300641358293674e-6, both below2e-5. All complete authored weights are used. Source bytes remain unchanged; no original executable runs.

`verify_skeletal_descriptors.py` validates source identity and independent key samples, then calls `verify_skeleton_animation_pose.py` for hierarchy/deformation checks. [Sampling/reproduction](COMPRESSED_ANIMATION_SAMPLING_CONTRACT.md), [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETON_ANIMATION_POSE.md). Next: explicit sampled original pose upload and GPU readback; timed animation/controller/root-motion semantics remain separate.
