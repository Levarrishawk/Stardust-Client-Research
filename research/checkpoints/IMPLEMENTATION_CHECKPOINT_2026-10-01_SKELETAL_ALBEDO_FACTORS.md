# Original skeletal albedo-factor slice — 2026-10-01

## Question, inputs and stopping condition

Which original sampled channels and factors color the body/head/mouth, and can the unlit preview preserve those semantics? Stop at compiled/source/pass/selected-color agreement, deterministic core fixtures and GPU post-sampling/display verification. Document full lighting and joint customization separately.

Input executable/DLL/config/object/SAT/idle/face identities are unchanged and reverified in this run's orientation. Original programs priority40: pixel_program/h_color2_specmap_ps11.psh,1399 bytes,SHA25695E44154F4107866CF23FD5C77E082573211135BE17E1CE679243A2AEB85EB92; pixel_program/h_specmap_ps11.psh,1124 bytes,SHA2564A3155359640270C2274495AFD1E8E68E6139CCBC358DDA14B66D50BAF8B340C. Static original program/IFF inspection, native owned codecs, independent Python and prototype host4.7.2 x64 GPU probes. No original code execution/server contact.

## Evidence and confidence

Observation: two-color PEXE contains TEX t0/t1, diffuse multiply, LRP r1.rgb,t0.a,c4,c3, specular multiply/add and opacity copy. Single-color uses diffuse×c3 plus specular instead of LRP. PSRC independently states the same hue equation. PASS+40/+44 are MAIN/HUEB or MAIN/zero. Existing62A0E9D0/62A0EF30 factor normalization/upload trace establishes constants3/4. Confirmed RGB factor/mask contract; full lighting terms are retained program data, not translated.

Observation: original TFNS white defaults plus five authored CSHD palette factors produce skin for body/head, eye for mouth and HUEB white; face retains white factors. Independent raw instructions/source/pass/factor/color/source-union audit agrees for four materials/two programs. Confirmed selected albedo values. Exact original gamma, capability branch, lighting/bloom/fade remain outside this proof.

Observation: first GPU fixture rejected an extra sRGB conversion in the adapter. OUTPUT_IS_SRGB selection corrected it; the remaining dark-channel discrepancy matched Compatibility's documented approximate transfer pair. The test preserves its2/255 bound and independently references that adapter display step rather than widening tolerance. Three GPU cases pass, maximum normalized error0.001545. Official prototype host sources are cited in the [durable contract](../../docs/SKELETAL_ALBEDO_FACTOR_CONTRACT.md); local GPU results establish the actual host observation.

## Implementation, ownership and verification

Optional validated `SkeletalAlbedoFactorBundle` shares source ownership; source closure74/5,927,038 bytes. Shared TFNS decoder replaces prior face-only duplicate parsing. Engine-neutral evaluator implements post-sampling RGB/mask algebra. Prototype host retains original texture mips, applies validated factor vectors/mask and verifies parameter/texture/material identities. Existing modes remain explicit.

Native smoke and original optional probe pass both instruction/comment patterns, truncation/altered-token/ownership, static-map duplicates/extents, sample bounds, hand-calculated endpoint/midpoint/single-factor tests, invalid-option-before-lookup and original missing/masked program gates. Independent producer audit passes complete source union and previous face/geometry/mip/pose checks. Prototype host build zero warnings/errors. Headless worker, GPU480-update/two-loop/control and first-lookup cancellation fixtures pass. Capture visually inspected, now colored skin with generated face. Original/private outputs remain ignored; no staging/publication.

## Remaining uncertainty and exact next action

Bring explicit generated face and albedo-factor owner into the interior character path and retain route/collision/animation proof. Then reconcile explicit face/body/eye customization selections before interactive color editing. Full normal/specular/lighting, occlusion/clothing, original gamma/hardware precision remain independent gaps. Research metrics/semantic-name counts unchanged.
