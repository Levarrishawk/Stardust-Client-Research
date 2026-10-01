# 2026-09-30 precise triangle contact along floor paths

## Question and boundary

Can the triangle kernel consume existing precise feet segments, including slopes and zero-request-progress vertical lift, without rounding to float or losing global contact progress? Stop at full-path validation and independent synthetic segment/time comparison; actor locator adoption and original obstacle authority remain separate.

## Confirmed observations

The [extended contract](../../docs/TRIANGLE_BOX_SWEEP_CONTRACT.md) defines `FloorPathTriangleBoxSweep`, complete input validation and segment/triangle work limits. Native gates pass two-segment wall contact at global parameter 0.34375; sloped center Y1.75; ceiling contact during zero-parameter lift; support tangency; and a contact near coordinate1,000,000 that would collapse into initial overlap if rounded to float. Invalid later segments, continuity breaks and oversized query work reject before returning contact.

The independent LP verifier's `--paths` mode generates 512 synthetic two-segment paths, using the same seed 20260930 and float32 triangle/box geometry with double feet coordinates. Fixture SHA-256 is `9DA86EC6B5CA265173B512F189C1A423D2CBAA56C1BC24DA62C310484A9EF31D`. All 357 clear, 145 moving and10 initial contacts match, including segment index and global parameter. Maximum parameter difference 2.498001805406602e-16. Original straight-sweep512-case oracle also passes after the precise shared-kernel refactor. Native smoke passes; generated fixtures/output remain ignored.

Confidence: Confirmed bounded modern query adapter, not original physics fidelity. No supported floor locator is adopted, no sliding response implemented and no authored visual mesh promoted to collision. Metrics and registries remain unchanged.

## Exact next action

Implement supported-floor collision adoption/rollback using authoritative synthetic path fixtures, with repeat/retreat and ceiling-blocked step negatives. Only then select an explicit, evidence-backed original obstacle source; the selected Mustafar collision-bound/FLOR audit remains a constraint.
