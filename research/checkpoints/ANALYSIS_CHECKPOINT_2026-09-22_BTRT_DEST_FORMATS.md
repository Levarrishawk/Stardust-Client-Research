# Blueprint texture renderer DEST format contract

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The BTRT `DEST` chunk contains output width, output height, and an ordered list
of file-format identifiers. The loader maps each on-disk ID through a fixed
executable table and stores the mapped values in the template. They are output
format choices, not shader channels, variables, or render commands.

For the eight surveyed production assets, the authored IDs are
`[4, 4, 3, 5, 6]`; the mapping yields `[4, 4, 1, 3, 0]`. Duplicate entries are
retained, so a replacement must not deduplicate or reorder this list.

## Evidence

- RTTI identifies vtables `0x015E7C48` and `0x015E7C84` as
  `BlueprintTextureRendererTemplate` and `BlueprintTextureRenderer`.
- `0x0086EEB0` reads two U32 dimensions, a U32 count, and that many U32 format
  IDs, mapping them through `0x015E7A58`.
- The fixed file-ID -> internal-ID map is: `0->6`, `1->5`, `2->2`, `3->1`,
  `4->4`, `5->3`, `6->0`, `7->16`, `8->16`, `9->16`, `10->7`, `11->8`.
- `0x00869A90` is reached from both BTRT version loaders after list parsing.
- `0x00869CD0` consumes the dimensions and passes the first mapped format plus
  the full format count into `0x007650E0`, the Texture factory.
- `0x008705B0` constructs a 0x20-byte runtime through `0x00872FA0`.
- `0x00872FA0` separately allocates revision-value slots for customization
  variable factories. These slots are not DEST outputs.
- Template virtuals `0x0086E300`/`0x0086E310`/`0x0086E350`/`0x0086E330`
  enumerate customization-variable factories, a separate collection.

## Replacement-client rule

Parse dimensions and the complete ordered format-ID list exactly. Preserve the
mapping at the asset boundary, including duplicates. Treat the list as an
ordered acceptable-format set supplied to texture creation, not as multiple
render outputs. Modern GPU formats may differ internally, but selection must
preserve the original priority/order contract.

## Confidence and remaining unknown

- **Confirmed:** field order, element width, executable mapping, duplicate
  preservation, and downstream delivery to texture creation.
- **Strongly inferred:** entries are an ordered acceptable destination
  texture/pixel-format set.
- **Confirmed by the installed s207 backend:** graphics API slot `+0x114` is
  `0x62A04630`; `0x62A12510` walks candidates in order, rejects unsupported
  formats through `TextureFormatInfo`, and stops after the first successful
  `CreateTexture`/`CreateCubeTexture`.

## Exact format map

The executable's 14 named `TextureFormat` values and s207 D3D mappings are:

| Internal ID | Embedded name | D3DFORMAT | Modern semantic |
|---:|---|---|---|
| 0 | `TF_ARGB_8888` | 21 / `A8R8G8B8` | BGRA8 with alpha |
| 1 | `TF_ARGB_4444` | 26 / `A4R4G4B4` | packed 4-bit BGRA |
| 2 | `TF_ARGB_1555` | 25 / `A1R5G5B5` | packed 1-bit alpha |
| 3 | `TF_XRGB_8888` | 22 / `X8R8G8B8` | BGRA8, alpha ignored |
| 4 | `TF_RGB_888` | 20 / `R8G8B8` | 24-bit BGR source |
| 5 | `TF_RGB_565` | 20 / `R8G8B8` | 565 source expanded by backend |
| 6 | `TF_RGB_555` | 20 / `R8G8B8` | 555 source expanded by backend |
| 7 | `TF_DXT1` | FourCC `DXT1` | BC1 |
| 8 | `TF_DXT2` | FourCC `DXT2` | premultiplied BC2-family |
| 9 | `TF_DXT3` | FourCC `DXT3` | BC2 |
| 10 | `TF_DXT4` | FourCC `DXT4` | premultiplied BC3-family |
| 11 | `TF_DXT5` | FourCC `DXT5` | BC3 |
| 12 | `TF_A_8` | 28 / `A8` | alpha-only 8-bit |
| 13 | `TF_P_8` | 41 / `P8` | palette index 8-bit |

s207 additionally contains backend table entries 14=`A16B16G16R16F` and
15=`A32B32G32R32F`, but the client `TextureFormatInfo` entries 14/15 are empty
and unnamed. They are therefore backend capabilities, not supported client
asset enum values in this build. BTRT file IDs 7-9 map to internal sentinel 16;
none occurs in the surveyed effective assets and it must be rejected safely.

For the effective BTRT list, `[4,4,3,5,6]` therefore means
`[RGB_888, RGB_888, XRGB_8888, RGB_565, RGB_555]`. On s207, the first supported
choice is normally RGB_888 backed by D3D9 `R8G8B8`.

## Remaining unknown

The source-code spelling for the invalid/sentinel value 16 is unavailable.
This no longer blocks texture negotiation or modern-format translation.

## 2026-10-01 selected-template correction

The selected human face's raw DEST is width256,height256,count4,IDs `[4,3,5,6]`. The earlier `[4,4,3,5,6]` included the count. Direct baseline table015E7A58 maps the corrected IDs to internal `[4,1,3,0]`: RGB888,ARGB4444,XRGB8888,ARGB8888. The earlier final sentence also incorrectly treated file IDs as internal enums; superseded for this sample. Other seven historical templates were not rescanned. Scratch ARGB8 rendering precedes destination conversion. [Reproducible execution checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_EXECUTION_BOUNDARY.md).
