# CKAT packed rotation: F32 store and x87 radicand sign

Date: 2026-09-24. Gated question A01. Static original executable `C:\Stardust-DEV\Stardust.exe` and read-only effective original `.ans` overlay. This narrows a numerical edge, but it is **not** a differential runtime fixture and does not establish exception/NaN policy.

## Prior result and new question

The previous full-corpus Python probe reported 244 animated keys and four static rotations with reconstructed three-component norm above 1.00001 across 16 clips. Its arithmetic did not model the executable's F32 stores between quantizer lookup and square root. The question here is whether those overshoots disappear after the actual 32-bit component rounding, not whether the corpus needs to be recounted for its own sake. See `ANALYSIS_CHECKPOINT_2026-09-23_CKAT_FULL_QUATERNION_NORM_AUDIT.md`.

## Instruction and corpus evidence

`0x00AC0E20` loads F32 range width and F32 constants, computes selector table bases/scales with x87, and stores each to F32 (`fstp dword`). `0x00AC0D90` calls the 11-bit and 10-bit decoders, stores each resulting component to F32 at the caller's output addresses (`0x00AC0DBC`, `0x00AC0DD9`, `0x00AC0DE8`), reloads those F32 components, sums their squares, subtracts from F32 `1.0`, and executes `fsqrt` at `0x00AC0E07`. There is no comparison or clamp between the subtraction and square root in this function. Both static and animated routes use this decoder according to the prior vtable/caller traces.

The new focused probe reads the executable's F32 range descriptors/constants, rounds table bases/scales and each decoded component to F32, then sums squared stored components. Across the same **12,059,807 AROT keys and 127,315 SROT records** it again finds **244 AROT + four SROT** sums above 1.00001 in **16 clips**. Their smallest sum above that threshold is **1.000010142530148** and largest **1.000297157005014**. Example: `appearance/animation/probe_droid_std_normal_to_combat_idle.ans`, AROT key 19, packed `4192340735`, selectors `fc fc e8`, sum about `1.00022683147`. The sign of `1 - sum` for these records is not a Python-double rounding artifact; the margin is orders of magnitude above rounding differences in the three stored F32 values and x87 accumulation.

## Conclusion and remaining uncertainty

**Confirmed:** for the 248 identified records, the reconstructed fourth-component square-root operand is negative under the observed F32-store path, and the decoder has no local clamp. The earlier numerical warning survives executable-matched F32 storage. **Not confirmed:** whether original playback traps, propagates an indefinite/NaN value, skips the bad sample, or recovers in a caller or exception boundary; process/thread x87 control-word state and actual live evaluation of each asset are not proved. Do not silently clamp or declare a crash in a compatible importer based only on this static result.

Next closure evidence: select one of these exact packed records in a bounded original-client playback/differential harness or prove the active x87 control word and caller error path. Until then, preserve raw packed values and attach diagnostics to the 16 clips. The script `tools/python/ckat_float32_overshoot_probe.py` reproduces the read-only result. No packet mapping or whole-client percentage changes.
