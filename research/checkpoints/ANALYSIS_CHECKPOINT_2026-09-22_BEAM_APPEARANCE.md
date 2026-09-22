# Analysis checkpoint — BEAM appearance

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The registered BEAM appearance family is now specified to the limit of available static evidence. Stardust.exe supports `FORM BEAM/FORM 0000`, although the effective configured archive overlay contains no BEAM asset.

| Stage | Address | Finding | Confidence |
|---|---:|---|---|
| install | `0x00798240` | registers tag `BEAM` and factory `0x00798280` | Confirmed by code |
| dispatch | `0x00798460` | accepts only version `0000`; diagnoses other versions | Confirmed by code |
| decode | `0x00798540` | reads two F32 values, Vector4, shader-template path | Confirmed by loader plus runtime use |
| instantiate | `0x007983F0` | asks shader template for an instance using the Vector4, then constructs runtime appearance | Confirmed by code |
| runtime | `0x00792EC0`, `0x007928D0` | owns a four-vertex shader primitive, regenerates geometry and bounds | Confirmed by code |

## Serialized v0000 contract

All numeric payload values are little-endian inside the big-endian IFF envelope:

1. F32 dimension 0;
2. F32 dimension 1;
3. four F32 values passed as a Vector4 to shader-instance creation;
4. zero-terminated shader-template path.

The two dimensions are used directly as primitive extents, and the runtime derives a bound from `sqrt(dimension0^2 + dimension1^2)`. The exact historical names for the dimensions and Vector4 remain unknown. Calling the Vector4 a color would be plausible but is not justified without a corpus sample or source corroboration.

## Corpus status and implementation consequence

No effective appearance-overlay record has outer tag BEAM, and no `.beam` namespace winner exists in the configured archives. This means the loader is dormant compatibility code in this installation, not proof that BEAM can be omitted. A modern client should implement the compact decoder and native four-vertex primitive, while treating the Vector4 as an opaque shader-instance parameter until stronger evidence appears.

This block closes the executable format/runtime contract. It does not claim corpus validation of value ranges.
