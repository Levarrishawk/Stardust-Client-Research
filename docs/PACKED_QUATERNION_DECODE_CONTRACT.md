# Packed quaternion numerical boundary

`PackedQuaternionDecoder` consumes owned selector bytes and a packed U32 from CKAT descriptors. It supports the deployed selector range 128..254. Original zero/default selector records outside that range exist but are deliberately rejected by this bounded implementation; their numerical behavior is not implemented.

## Formula and evidence

The word partitions into 11/11/10 bits at shifts 21/10/0. High bits 0x400/0x400/0x200 encode the signs; lower bits encode magnitudes. Selector ranges start at 254/252/248/240/224/192/128 for levels 0..6. For level i and ordinal j, width is F32(2/(2^i+1)), base is F32((j+1)*width-1), and scale is F32(width*F32(1/1023)) or F32(width*F32(1/511)). Each component is F32(base ± magnitude*scale). The fourth component derives from the remaining norm.

This independently authored implementation follows [table construction](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_QUATERNION_QUANTIZER_TABLE_FORMULA.md), [selectors](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_CKAT_QUATERNION_QUANTIZER_SELECTORS.md) and [F32 stores](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_CKAT_F32_RADICAND_SIGN.md). Formula/bit partition are Confirmed. It models stored F32 components with double intermediate arithmetic, not a complete x87 emulator.

## Explicit publication policy

The result retains decoded X/Y/Z, the double radicand and a `Repaired` diagnostic. Reconstruction uses sqrt(max(0,radicand)); publication normalizes the resulting finite quaternion. A negative radicand therefore produces a visible diagnostic instead of silently flowing into transforms. `Reconstructed` names the modern clamped reconstruction, not the original NaN result.

Original ordinary animated evaluation propagates a non-finite value on the established path, according to the [propagation/thread analysis](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_CKAT_NAN_PROPAGATION_AND_THREAD.md). Clamp/normalization is an intentional modern safety divergence. Exact live NaN payload and every static construction thread remain outside this claim.

## Verification

Native fixtures verify identity, a previously documented original overshoot tuple (selectors fc/fc/e8, word 4192340735), all 127 deployed selectors with three varied sign/magnitude words, normalized finite publication and rejection of non-deployed selectors. These fixtures are synthetic inputs to the new implementation; no original executable was run.

The selected original ambient clip supplies 2,217 animated packed values and eight static values. `verify_skeletal_descriptors.py` verifies the executable SHA-256, reads its immutable range/reciprocal tables through the existing `ckat_float32_overshoot_probe.py`, independently decodes every original word and compares components, radicand, publication and repair flag. All 2,225 agree; maximum component/publication error 9.133205225442254e-8. No selected value requires repair. This sample does not establish all-corpus numerical equivalence.

No interpolation, animation state machine, clip-to-skeleton application or animation playback is claimed. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PACKED_QUATERNION_DECODE.md). Next: keyed rotation/translation evaluation with deterministic endpoint/interior fixtures and explicit unmatched-transform handling.
