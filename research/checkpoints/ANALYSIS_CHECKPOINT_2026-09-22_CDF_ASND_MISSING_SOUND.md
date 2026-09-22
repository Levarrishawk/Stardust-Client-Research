# CDF `ASND` missing-child boundary

Date: 2026-09-22. Target: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Decompiled functions are reconstructed pseudocode.

## Bounded question

Do the three newly recovered `CLDF/0000/ASND` records whose `.snd` child is absent cause CDF parsing to fail? Stop at file load and prefetch/cache behavior; full object-level playback is a separate question.

The `ASND` branch of `0x00606250` constructs a single CDF field at `+0x38`, reads/copies the NUL-terminated path string into its string storage, and closes the chunk. This branch **does not fetch the `.snd` template** and does not test file existence before preserving the CDF record. `0x00604110` subsequently traverses CDF dependencies, including `+0x38`, and calls `0x0060AA70` to prefetch sound resources. `0x0060AA70` calls `0x00417320`; on a null sound-template result it constructs a path/CRC-string object and inserts that into its lookup state, rather than dereferencing a null sound template. **Confirmed by code.**

Thus the absent `sound/mus_figrin_dan_song_1.snd`, `sound/amb_seashore_outdoors_lp.snd`, and `sound/amb_snow_outside_lp.snd` from the 84 newly recovered CDFs do **not** invalidate the CLDF parser, and the traced prefetch path is null-tolerant. The missing files remain real asset-completeness gaps: no audible output can be promised from a path without a valid template. We have **not** yet identified every object-level caller that uses the `ASND` field or proved the eventual playback failure policy. Do not conflate this path with the `PSND` inside a `CLEF` effect, whose runtime has been traced separately.

Evidence: `0x00606250` `ASND` branch; `0x00604110` CDF dependency traversal; `0x0060AA70` prefetch/map insertion; `0x00417320` allow-missing sound-template fetch; corpus pairs listed in `ANALYSIS_CHECKPOINT_2026-09-22_RECOVERED_CDF_DEPENDENCIES.md`. No server packet or renderer claim is made. Two stable names added. Planning estimates remain 96% implementation-grade / 90% comprehensive.
