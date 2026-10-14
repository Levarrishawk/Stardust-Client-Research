# Native DDS decoder contract

Updated 2026-10-02. `Stardust.Core.Assets.DdsDecoder` is engine-neutral and returns
owned, top-to-bottom RGBA8 mip pixels. This contract describes the implemented
slice, separately from original-client format acceptance and full asset coverage.

## Accepted formats

| Container | Encoded format | Native description |
|---|---|---|
| Legacy FourCC | DXT1 / DXT3 / DXT5 | BC1 / BC2 / BC3 |
| Legacy FourCC | ATI1 or BC4U / ATI2 or BC5U | BC4 / BC5 unsigned channels |
| Legacy RGB | 32 bits, BGRA masks, alpha present | BGRA32, tightly packed rows |
| Legacy RGB | 24 bits, BGR masks, no alpha | BGR24, tightly packed rows |
| Legacy RGB | 32 bits, BGR masks, no alpha | BGRX32, tightly packed rows |
| DX10 | DXGI 71/72, 74/75, 77/78 | BC1/2/3 UNORM or UNORM_SRGB |
| DX10 | DXGI 80 / 83 | BC4 / BC5 UNORM |
| DX10 | DXGI 28/29 / 87/91 | RGBA32 / BGRA32 UNORM or UNORM_SRGB |
| DX10 | DXGI 88/93 | BGRX32 UNORM / UNORM_SRGB |
| DX10 | DXGI 61 / 49 | R8 / RG8 UNORM |
| DX10 | DXGI 98/99 | BC7 UNORM / UNORM_SRGB |

The DX10 extension must describe one 2D surface: resource dimension 3,
misc flags 0, array size 1. Cubes, arrays, volumes, typeless and signed formats
are rejected explicitly by the RGBA8 interface. Typed BC6H uses the separate
HDR output contract below rather than an RGBA8 conversion.
BC7 now uses a pinned managed codec behind our validated block boundary. Legacy DXT2/DXT4,
packed 16-bit/luminance formats and padded linear rows remain open.

`DdsImage` retains the supported DXGI ID, color space and alpha mode. Legacy
color space is Unspecified; DX10 UNORM is Linear and UNORM_SRGB (including BGRX ID93 and BC7 ID99) is Srgb.
Alpha modes 0–4 are preserved as Unknown, Straight, Premultiplied, Opaque,
Custom; reserved bits/values fail. No gamma transform or alpha unpremultiplication
is performed. Encoded channel values remain byte-exact.

BC4/R8 becomes `(R,0,0,255)`; BC5/RG8 becomes `(R,G,0,255)`. The decoder does
not infer luminance or reconstruct a normal's blue channel. That is a material
interpretation. BGRA is reordered into RGBA. BC1 preserves its transparent
three-color mode; BC2/3 always use the four-color RGB palette. RGB565 expands
by bit replication, palette interpolation uses integer division, and BC2/3 alpha
follows its explicit/interpolated representation.

## Bounds and ownership

Defaults: dimension ≤8,192; input ≤64 MiB; total decoded pixels ≤128 MiB.
One complete legacy header is 128 bytes; DX10 adds 20. Mip count 0 means one;
declared positive counts may be incomplete but cannot exceed the dimension-derived
chain. Each mip halves each dimension with a floor of one. BC byte size uses
ceil(width/4) × ceil(height/4) × block bytes, including the 4×4 minimum for
small mips. Partial edge blocks are clipped to logical dimensions.

All mip extents and the aggregate output budget are validated before output
allocation. Truncated headers/payloads and trailing bytes fail. Source spans are
borrowed only during decoding; returned descriptions own their pixel arrays.
ReadOnlyMemory exposes a read-only view, not a cryptographic immutability promise.

The [CPU asset bundle](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_MATERIAL_GRAPH.md)
adds 128 MiB aggregate source/decode budgets and explicit dependency identity.
The resolver owns extraction allocations before these bundle checks.

## Evidence and reproduction

- [Original DDS checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_DDS_PIXELS.md):
  all 80 original mesh texture mips match Pillow, plus separate BC2/BC3 samples.
- [Modern DDS checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_MODERN_DDS.md):
  16 independent synthetic fixtures and malformed header/surface tests.
- [Microsoft DDS layout](https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dds-file-layout-for-textures),
  [block compression](https://learn.microsoft.com/en-us/windows/win32/direct3d10/d3d10-graphics-programming-guide-resources-block-compression),
  [DX10 header](https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dds-header-dxt10),
  [DXGI IDs](https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format).
  These define standard formats; original-client acceptance is established separately.

Run the core smoke suite, `tools/python/verify_dds_pixels.py`,
`tools/python/verify_modern_dds_fixtures.py` and
`tools/python/verify_textured_mesh_bundle.py` as documented in the checkpoints.
No original pixels or synthetic generated files need to enter Git.

## BC7 codec and reserved blocks

All eight BC7 modes are supported through pinned BCnEncoder.Net 2.3.0. Our DDS parser owns sizes, surface restrictions, metadata, allocation limits and clipping; the library receives only exact 16-byte blocks. The reserved no-mode prefix produces zero RGBA per the format specification. Pillow returns opaque black for that case, so it is tested against the specification separately. See the [BC7 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_BC7_DECODER.md) and [dependency notices](../../SWG-Godot-Client/poc/THIRD_PARTY_NOTICES.md). Valid synthetic mode/partition/rotation/mip output matches Pillow, and a Godot GPU upload/readback gate passes.

## Separate BC6H HDR output

`DdsHdrDecoder.Decode` returns `DdsHdrImage` with owned little-endian binary16 RGB mip bytes. Typed DXGI 95/96 preserve unsigned/signed BC6H respectively. The format has no alpha channel; header alpha metadata is retained without changing pixels. There is no tone mapping, gamma transform or clipping to RGBA8. Six bytes per decoded pixel count against the same default aggregate limit. Surface, mip, input, trailing-data and dimension gates remain explicit; typeless ID94 is rejected.

Only validated sixteen-byte raw blocks enter the existing pinned dependency. Reserved mode prefixes 19/23/27/31 produce zero RGB. All fourteen modes and signed/unsigned output match an independent bcdec decoder in Godot 4.7.2 for forty standards-only fixtures/264 mips. Actual RGBH GPU readback preserves their complete half bytes. See the [HDR checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md). Static material bundles now retain separate RGBA8/RGB-half images under the [HDR material contract](BC6H_MATERIAL_BUNDLE_CONTRACT.md); scene tone mapping remains open. This extension is not evidence of original executable DX10 acceptance.

## 2026-10-01 face dependency closure

The optional object-root face resource closure owns two shaders,32 DDS choices/251 declared mips and three palettes:50 sources/2,283,397 bytes including preceding geometry. Independent pixel/palette/source-union agreement passes. Seven original64×32 BC2 eyebrow files retain one16-byte terminal block via explicit `AllowLegacyTerminalBlockTail`; default DDS trailing-data rejection remains. The extra block is owned metadata, not an added mip. Preparation/rendering remains pending. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_FACE_RESOURCE_CLOSURE.md).

## 2026-10-02 tightly packed opaque colors

`DdsFormat.Bgr24=10` and `Bgrx32=11` are appended; existing values0–9 are unchanged. Legacy opaque formats require pixel flags exactly `0x40` (RGB), zero FourCC, bit count24 or32, masks R=`00FF0000`, G=`0000FF00`, B=`000000FF`, A=0. Legacy BGRA retains flags`0x41`/32bits/A=`FF000000` and now explicitly requires zero FourCC. DX10 BGRX accepts typed IDs88/93 with pixel flags exactly4 and zero legacy bits/masks. Typeless IDs89/92 reject. These are bounded modern decoder restrictions, not a recovered original rejection policy.

BGR bytes become RGBA; alpha is always255. BGRX ignores every X byte. Legacy color space/alpha metadata remain Unspecified/Unknown. DX10 IDs88/93 retain Linear/Srgb and header alpha modes0–4 even though the encoded format has no alpha channel; metadata does not change opaque output. No gamma conversion or alpha processing occurs.

All declared mips are tightly packed, with three or four input bytes per pixel and four output bytes. Opaque headers accept only required CAPS/HEIGHT/WIDTH/PIXELFORMAT flags plus optional PITCH, MIPMAPCOUNT or LINEARSIZE. PITCH and LINEARSIZE together reject. PITCH requires top-row `width × bytesPerPixel`; LINEARSIZE requires top-mip `width × height × bytesPerPixel`. Unflagged pitch/size values do not describe padding. Lower mip sizes derive from dimensions without row alignment. Unsupported flags, masks, counts, surfaces, truncation, trailing data and aggregate budgets reject before pixel allocation. The existing opt-in BC2 terminal-tail exception never admits opaque tails.

Fresh effective source: `texture/nebula_dant_back.dds`, priority35, `mtg_patch_006_texture_02.tre`,16,511bytes, SHA256 `A2627501B233EE3A795BEB5B42446BF2F9B2A60854AD3781366720B5631FDC9D`. Header64×64/7mips, flags`000A1007`, RGB`0x40`/24bits/exact masks above, LINEARSIZE12,288bytes. Encoded mip bytes12,288/3,072/768/192/48/12/3 total16,383; with128-byte header this accounts for the full resource. Seven owned RGBA mips total21,844bytes. All seven independently match Pillow DDS/raw decoding and a separate byte oracle. This proves one original asset's decoder acceptance, not original executable behavior or every catalog/corpus format.

The prior ignored DDS inventory's29,906 rows include113 BGR24 records (SHA256 `DCEDE8198E48FF68CCD65FDF2C618E50703A13F1F08BF97CA34A1D0E5665D471`). Round12 checked that existing CSV identity/count, then fetched only the specified effective sample; it did not repeat a full corpus census. Historical valid-resource/tombstone numbers in the dated research report remain separate.

[DdsOpaqueColorScenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/DdsOpaqueColorScenarios.cs) is registered by the existing DDS smoke scenarios.36 independently authored fixtures cover72 mips: legacy BGR24/BGRX and typed88/93; absent pitch, exact pitch and exact top linear size; all five DX10 alpha metadata modes; distinct B/G/R/X bytes; non-power-of-two rows and terminal1×1 mips.781 negative checks comprise657 truncated prefixes and124 explicit malformed/size/budget/tail cases. Source mutation leaves output unchanged, mip lists reject mutation and the original archive scope disposes before owned output checks. Existing BC1–5/BGRA/RGBA/R/RG/BC7 and BC6H HDR regressions pass unchanged.

[Independent verifier](../../SWG-Godot-Client/tools/python/verify_dds_opaque_colors.py) takes explicit original root, configuration and native receipt paths, with optional `--prior-inventory` to verify the existing CSV pin/counts. It verifies source priority/configuration/SHA, both pixel oracles, header/size gates and every negative receipt. Native ownership/disposal observations are checked as receipts, not independently executed by Python. No original images are published. [Round12 checkpoint and commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R12_DDS_OPAQUE_COLORS.md).