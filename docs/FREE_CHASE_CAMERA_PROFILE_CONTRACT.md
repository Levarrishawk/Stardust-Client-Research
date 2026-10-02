# Free-chase camera profile contract

`FreeChaseCameraProfileDecoder` owns FCHS/0000 with ordered ZOOM/FRST/DEFH/PSHM/PSTM chunks. ZOOM is repeated F32LE; FRST and DEFH one F32LE each; PSHM repeated S32LE posture/S32LE state/F32LE height multiplier; PSTM repeated S32LE posture/F32LE multiplier. Original grammar/precedence evidence: [camera family](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_CAMERA_FAMILY_AND_FREE_CHASE.md) and [profile semantics](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_FREE_CHASE_PROFILE_SEMANTICS.md), constructor0x006D5EF0 and target-height consumer0x006D6660.

## Owned limits and evaluation

Require <=64 KiB, exact version/order/chunk widths,1..64 increasing nonnegative zoom values, finite absolute values<=100, nonnegative first-person threshold, posture0..14/state0..33 and unique map keys. Original loader skips invalid indices and PSTM overwrites duplicate keys; this strict parser deliberately rejects those cases. Unknown PSHM duplicate handling is not reproduced. Preserve raw list order and F32 values. No asset defaults are invented.

TargetHeight takes explicit posture, active-state index set and positive bounded appearance metric. Select posture/state override before posture-only value, then DEFH; scale selected multiplier by metric. For multiple active matches, the prototype picks smallest state index. The deployed two overrides belong to different postures, so original multiple-match behavior remains untested. Target-height smoothing, mount offsets and original appearance-metric derivation are outside this reader.

SelectSetting range-checks a persisted index and falls back to integer middle setting as observed in the constructor. ZoomDistance requires an explicit positive multiplier. Direct0x00503400 reads F32 global0x01911180; configuration loader0x0050407E..0x00504092 reads ClientGame/freeChaseCameraMaximumZoom with F32 default1 and stores that global. Constructor multiplies each authored zoom by the getter result. This refines the earlier unit-conversion wording: it is a configurable zoom scale, not proven physical-unit conversion. Effective runtime preference/config overrides are not inferred from the asset or a single config file.

## Original profile

Effective `camera/freechasecamera.iff`: priority40,212 bytes,SHA256 01A19A5B18E81C9366B86B5F313C31D8F0C1961F8230DB4535AA5C50E95C543D. ZOOM=[0,.5,.75,1.25,2,3,4,5,6,7.25,8.5,9.75,11,12.5,14], FRST=.5, DEFH=1.7. PSHM=(8,19,1.1)/(0,27,.7). PSTM=1:1.2,2:.35,7:.35,8:.7,12:.35,13:.35,14:.35. Values are authored F32, with their normal decimal representation shown here.

Native `--free-chase-profile <owned-file> <SHA256>` emits profile and135 explicit posture/state/metric height queries. Independent `verify_free_chase_profile.py <archive-root> <configuration> <native-output> --configuration-sha256 <pin>` resolves effective original bytes and checks all fields and F32 results. Ownership/truncation/version/width/count/index/float/duplicate/precedence/fallback fixtures and full native/build pass.

[Retained original interior integration and obstruction-aware camera](INTERIOR_CHASE_CAMERA_CONTRACT.md) are implemented and verified. Next: camera-room ownership before third-person portal visibility. Legacy camera smoothing/collision/mount/input policy is not implemented by this data reader.
