# Analysis session — 2026-09-25 to 16:00 EDT

Target: verified `C:\Stardust-DEV\Stardust.exe`, configured original assets, `s207_r.dll`, and current Stardust source where relevant. No replacement code, execution of the client, commit, or push occurred.

## Substantive blocks completed

1. **A02 — exact-end CKAT messages: closed.** Standard completion, loop/reset, and replacement paths never perform a separate endpoint query; the half-open message window excludes 1,517 exact-end records. Preserve the records but do not emit them in compatibility playback. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_CKAT_END_FRAME_DELIVERY_CLOSURE.md).
2. **A01 — CKAT nonfinite quaternion path: closed to replacement depth.** Negative AROT radicands produce masked x87 NaNs on the synchronous main-thread pose-evaluation path and propagate into the pose cache without repair. The replacement should diagnose, clamp the radicand and normalize rather than reproduce renderer corruption. Four SROT records retain a narrower construction-thread residual. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_CKAT_NAN_PROPAGATION_AND_THREAD.md).
3. **A04 — non-prefix animation messages: closed for ordinary gameplay.** The shared listener has only the six-prefix gameplay callback and an intro-scene callback; there is no generic ordinary fallback interpreter. Preserve unknown names without inventing effects. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_NONPREFIX_ANIMATION_MESSAGE_SCOPE.md).
4. **A07 — Light scalar semantics: closed to replacement depth.** Correctly mapping raster API base `0x62B835B8` traces typed selected `Light` pointers into s207 point-light shader packing. Fields `+0x100,+0x104,+0x108` are the ordered attenuation coefficient vector; constant/linear/quadratic names are strongly inferred. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_LIGHT_ATTENUATION_CONSUMER.md).
5. **A03 — combined-root clip reachability: narrowed.** Neither of the two installed QCHN+LOCT clips has a literal full-path or basename reference in decompressed configured TRE payloads, the executable, or current server source. They are strongly inferred dormant/orphaned, not proven unreachable through synthesized or CRC paths. Import support remains mandatory. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_CKAT_COMBINED_CLIP_REACHABILITY.md).
6. **A08 — missing-floor police POB: closed for the current server/configuration.** Current templates select the distinct working `mun_all_...` POB at six sites; the broken `mun_impl_...` layout has environment metadata but no spawn/template chain. Safe diagnostics remain required for future/admin/CRC-only selection. See [checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_POLICE_POB_CURRENT_SERVER_CLOSURE.md).

## Corrections and discipline

The first A07 backend lookup accidentally treated `0x62B835C0` as the API base and landed on a vertex-format constructor. That interpretation failed type continuity. Rechecking the already established `GetApi` base (`0x62B835B8`) corrected slot `+0xE8` to `0x62A04F60`, which copies the typed visible-light vector; its sole consumer reads the disputed fields directly. The false lead is documented rather than hidden.

## Remaining boundary

A03 still lacks the historical name for the external actor/basis convention applied after the exact combined-root delta calculation. The controller accessor is virtual (`TrackAnimationController` slot corresponding to `0x0082F990`) and copies from physical track `0x0083EC70`. A final recheck of `SkeletalAppearance2::alter` (`0x007C8AD0 -> 0x007D7790 -> controller alter`) confirms that this frame path advances and caches locomotion but does **not** itself apply the delta to the owning object's world transform. The broader indirect accessor caller must be typed before assigning a coordinate-space label. Given the zero-reference result and rarity, exact arithmetic is the compatibility requirement if the historic label remains unavailable.

## Progress accounting

Six substantive blocks were completed. Five observable contracts closed and one was materially narrowed. The historical **96% implementation-grade / 90% comprehensive** planning estimates remain suspended pending a reproducible current-evidence rescore; no percentage was advanced merely from block count. Function/address accounting remains separately uncalibrated.
