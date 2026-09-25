# CKAT negative-radicand propagation and evaluation-thread boundary

Date: 2026-09-25. Question A01 / W08+W02. Target: verified original `C:\Stardust-DEV\Stardust.exe`. This continues the F32 operand and x87-mask checkpoints without recounting the 248 records.

## Result

The 244 affected animated AROT keys are decoded during lazy per-frame pose evaluation, not during background asset parsing. On the ordinary scene/appearance path this occurs synchronously under `SkeletalAppearance2::alter`, after the main frame pump restores the configured x87 control word. Invalid-operation is therefore masked on the established path. The resulting non-finite fourth quaternion component is not checked or repaired by CKAT sampling, quaternion interpolation, the physical-track pose cache, or the aggregate pose cache.

The four affected static SROT values use the same packed decoder during compressed-animation runtime construction. This block does not prove that every possible construction site is on the main thread, so their thread/control-word boundary remains narrower than the AROT result.

## AROT path

```text
SkeletalAppearance2::alter 0x007C8AD0
  -> aggregate lazy transform evaluation 0x007D7510 / 0x007D6A10
  -> TrackAnimationController transform slot 0x0082F960
  -> physical-track evaluation 0x0083E890
  -> active CKAT transform slot 0x00822CD0
  -> dynamic rotation evaluator slot 0x00822480
  -> keyed quaternion sample 0x00824F00
  -> packed decode 0x00AC1380 / 0x00AC0D90
  -> quaternion interpolation 0x00ABA7C0
  -> physical-track and aggregate pose caches
```

`0x00822CD0` invokes the selected per-transform rotation and translation evaluators. `0x00822480` routes the dynamic rotation channel into `0x00824F00`, which decodes one or two adjacent packed values and interpolates them. Prior instruction evidence proves `0x00AC0D90` performs unclamped `fsqrt(1-x²-y²-z²)` after F32 component stores.

When a decoded quaternion contains a non-finite fourth component, interpolation `0x00ABA7C0` computes a non-finite dot product. Its ordered comparison used to choose the sign branch supplies no finite fallback; subsequent comparison/arccosine/sine or weighted-combination arithmetic propagates non-finite values. `0x0083E890` copies the returned quaternion verbatim into its per-transform cache, and `0x007D7510` copies it into the aggregate cache. No `isnan`, identity substitution, normalization, or component clamp appears on this route.

## Original and replacement policy

- **Original behavior:** ordinary AROT evaluation runs with masked invalid-operation and propagates a non-finite quaternion into the pose cache rather than throwing a synchronous exception or repairing it locally.
- **Exact NaN payload:** masked-invalid x87 `fsqrt` is expected to store an IEEE quiet indefinite NaN, but its exact payload/sign was not captured from a live process. Compatibility must not depend on that payload.
- **Replacement recommendation:** retain the packed input and issue a precise asset/channel diagnostic, but clamp the reconstruction radicand to zero and normalize before publishing the quaternion to renderer or physics transforms. This is an intentional safety divergence; propagating NaNs through modern transform matrices is not useful compatibility.

## Outcome

**A01 materially narrowed and closed for ordinary animated AROT evaluation.** Remaining scope is the four static SROT construction sites and exact live NaN payload; neither changes the safe replacement policy. Confidence is confirmed by code for the call/cache path and absence of local recovery, strong for masked-invalid non-finite propagation, and unknown for every SROT construction thread.
