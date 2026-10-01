# 2026-09-30 thin target portal landing

## Question and evidence

Why does light Yavin enclave cell 4/portal 0 fail reverse target adoption only in the identity pose? The original persistent trace first fails on reverse tick 23, retaining source cell 3/triangle 93. Target cell 4/triangle 0 has a slanted portal edge and a thin interior. Rounded edge projection lies just outside that triangle; the former 10-micrometre centroid correction can round tangentially and remain outside.

POB SHA-256 `4C86C20F37FDC22E21A17BF55D005D24A155B18E6FC6C330DAFFDCFCE3BB2862`; leaf APT `82E69268BA600B0E0CE3292C7D8DACE45896F352436F11CC26FAFBCC2588A617`; target floor `32854773279E217C05D0EF11DE278366A2C6AD79CC7104808A1E5A7882C71859`. Configuration and table identities remain those in the [precise actor contract](../../docs/PRECISE_DOOR_ACTOR_CONTRACT.md). Detailed original geometry remains private. Confidence: Confirmed raw geometry, native reproduction and independent persistent reference.

## Bounded fix

If the existing centroid bridge still fails, reuse inward-edge canonicalization against the original projected landing. Local correction stays within **1e-5 metres**; total source-contact/world-landing distance stays within the existing **1e-4 metres** paired-edge budget. Height is reprojected at rounded XZ. Global external-start acceptance is unchanged; unrepresentable landings still stop explicitly. This is modern numerical compatibility policy, not recovered original tolerance.

## Verification

Core smoke passes, including 1,000 independently authored slanted thin target triangles with reusable successful transfers. The pinned original Yavin trace matches **948 independent ticks / 44 transfers / 64 blocked ticks** across two poses. Its screened 60-tick round trip now returns exactly to the initial owner/position. The complete same-input screen yields **496 exact short round trips / 533 instantiated / 782 authored candidates**, with 37 remaining Accepted non-round-trips and no LandingFailed. Existing YT-1300 14-link/56-probe and 2,400 precise portal-box references pass unchanged.

## Next probe

Classify the 37 non-round-trips by first forward stopping boundary. Current endpoint summary groups: 20 FloorStopped without transfers, six MissingTargetFloor without transfers, six FloorStopped after a forward transfer and eventual reverse adoption, four PortalBlocked, one Accepted without a transfer. Trace the connected floor and encountered portal; inspection endpoint height can select a different stacked sheet. Do not force passage or relax unsupported-surface policy from a screen result alone.
