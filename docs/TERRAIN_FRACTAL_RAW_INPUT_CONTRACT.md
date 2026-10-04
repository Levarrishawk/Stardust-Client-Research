# MFRC/0001 raw input contract

Date:2026-10-04. This bounded engine-neutral decoder preserves the known raw MFRC/0001 words. It does not instantiate a sampler, normalize runtime state, generate noise or admit original terrain samples. The [accepted seed/schema research](TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md) supplies the evidence; [implementation checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R48_FRACTAL_RAW_INPUT.md) supplies reproduction and limits.

## Exact raw envelope

TerrainFractalRawInput.Decode accepts exactly84 bytes: FORM MFRC (big-endian size76), containing FORM 0001 (size64), containing one DATA chunk (size52). DATA starts at byte32 and contains13 little-endian four-byte words. No extra forms/chunks, padding, trailing bytes or alternative extent is admitted.0000 is explicitly unsupported despite the executable dispatcher recognizing it; no grammar is guessed. Unknown versions reject.

| Word | Raw field | Interpretation / evidence |
| --- | --- | --- |
| 0 | SeedBits | Signed seed inspection, destination+0/013E6B80 |
| 1 | FirstCurveFlagBits | Raw U32 flag; nonzero enable inspection, byte+24/013E6C50 |
| 2 | FirstCurveParameterBits | F32 raw word, +28/013E6C50 |
| 3 | SecondCurveFlagBits | Raw U32 flag; nonzero enable inspection, byte+2C/013E6C70 |
| 4 | SecondCurveParameterBits | F32 raw word, +30/013E6C70 |
| 5 | OctaveCountBits | Signed octave inspection, +14/013E6BF0 |
| 6 | FrequencyMultiplierBits | F32 raw word, +18/013E6C10 |
| 7 | AmplitudeMultiplierBits | F32 raw word, +1C/013E6C30 |
| 8 | FirstCoordinateScaleBits | F32 raw word, +4/013E6BB0 |
| 9 | SecondCoordinateScaleBits | F32 raw word, +8/013E6BB0 |
| 10 | FirstOffsetBits | F32 raw word, +0C/013E6BD0 |
| 11 | SecondOffsetBits | F32 raw word, +10/013E6BD0 |
| 12 | ModeBits | Raw discriminator, +38/013E6CB0 |

Observation:13 loader/writer words and destination order, confirmed by289 static anchors for executable SHA256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. Confidence: Confirmed structure. The sine flag and derived normalization are not serialized here. Optional curve naming remains descriptive; raw decoding does not resolve their arithmetic.

All raw words survive unchanged, including NaN payloads, infinities, arbitrary flag words and signed integer bit patterns. HasFiniteParameters is an inspection of the eight F32 fields, not runtime eligibility or authorization to evaluate a sampler. SignedSeed/SignedOctaveCount are unchecked signed32 interpretations; flag enable inspection does not replace raw flags. No F32 conversion or runtime setter is invoked.

## Ownership and limits

The input owns its84-byte encoded snapshot and13-word array. CopyEncodedBytes and CopyDataWordBits return independent copies. Fields is an immutable record; changing a separate record cannot change the input. No proprietary payload is retained in public fixtures.

TerrainFractalRawInputLimits defaults: MaximumInputBytes65536, MaximumInputs65536, MaximumAggregateInputBytes5505024, MaximumOwnedCopyBytes8912896. Hard bounds are respectively1048576,1048576,88080384,142606336. Negative or oversized policies reject. Logical buffer-copy cost is136 bytes per input (84 encoded+52 word bytes); this is not a CLR heap-size promise and excludes record/object overhead.

Decode checks policy, aggregate single-input caps and envelope before copying. DecodeBatch checks count and prospective copy cost first, then every source length, accumulated input bytes and strict envelope before any payload snapshot or result-array allocation. It revalidates each borrowed source and its owned snapshot before decoding words. Batch return is atomic; a late invalid envelope/cap or changed malformed source yields no partial batch. Callers remain responsible for avoiding concurrent mutation of borrowed lists/buffers; no original concurrency guarantee follows.

## Selected original and verification

One necessary first-family occurrence: priority42 terrain/tatooine.trn, family key1, MFRC at TGEN offset6674,84 bytes, SHA25685A84EFBDF4EE507C0011C9E4D4ED130EBF4719735A521FD491ABD286EBD723E. Source/config/TGEN/family identities remain those in the accepted research contract. Its parameter values and seed are decoded dynamically into ignored private fixtures/receipts, never a public payload array or original parameter literal.

The fresh independent47 inspector SHA-gates EXE and source identities and agrees with the new native raw decoder on every selected word. Independent synthetic encoding/struct inspection tests arbitrary raw/nonfinite words, tags, sizes,0000/unknown versions, every truncation and trailing bytes. Native copied-array, exact/late aggregate caps and prior-result preservation checks pass. Full counts and regressions are recorded in the checkpoint.

Next: consume this raw representation through a separately bounded runtime eligibility policy with evidenced controls and optional-state provenance. No production noise, full MFAM map or actual original floating controls are implemented here.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
