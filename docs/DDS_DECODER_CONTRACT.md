# Native DDS decoder contract

Updated 2026-09-29. `Stardust.Core.Assets.DdsDecoder` is engine-neutral and returns
owned, top-to-bottom RGBA8 mip pixels. This contract describes the implemented
slice, separately from original-client format acceptance and full asset coverage.

## Accepted formats

| Container | Encoded format | Native description |
|---|---|---|
| Legacy FourCC | DXT1 / DXT3 / DXT5 | BC1 / BC2 / BC3 |
| Legacy FourCC | ATI1 or BC4U / ATI2 or BC5U | BC4 / BC5 unsigned channels |
| Legacy RGB | 32 bits, BGRA masks, alpha present | BGRA32, tightly packed rows |
| DX10 | DXGI 71/72, 74/75, 77/78 | BC1/2/3 UNORM or UNORM_SRGB |
| DX10 | DXGI 80 / 83 | BC4 / BC5 UNORM |
| DX10 | DXGI 28/29 / 87/91 | RGBA32 / BGRA32 UNORM or UNORM_SRGB |
| DX10 | DXGI 61 / 49 | R8 / RG8 UNORM |
| DX10 | DXGI 98/99 | BC7 UNORM / UNORM_SRGB |

The DX10 extension must describe one 2D surface: resource dimension 3,
misc flags 0, array size 1. Cubes, arrays, volumes, typeless and signed formats
are rejected explicitly by the RGBA8 interface. Typed BC6H uses the separate
HDR output contract below rather than an RGBA8 conversion.
BC7 now uses a pinned managed codec behind our validated block boundary. Legacy DXT2/DXT4,
BGR24, packed 16-bit/luminance formats and padded linear rows remain open.

`DdsImage` retains the supported DXGI ID, color space and alpha mode. Legacy
color space is Unspecified; DX10 UNORM is Linear and UNORM_SRGB (including BC7 ID 99) is Srgb.
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

All eight BC7 modes are supported through pinned BCnEncoder.Net 2.3.0. Our DDS parser owns sizes, surface restrictions, metadata, allocation limits and clipping; the library receives only exact 16-byte blocks. The reserved no-mode prefix produces zero RGBA per the format specification. Pillow returns opaque black for that case, so it is tested against the specification separately. See the [BC7 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_BC7_DECODER.md) and [dependency notices](../poc/THIRD_PARTY_NOTICES.md). Valid synthetic mode/partition/rotation/mip output matches Pillow, and a Godot GPU upload/readback gate passes.

## Separate BC6H HDR output

`DdsHdrDecoder.Decode` returns `DdsHdrImage` with owned little-endian binary16 RGB mip bytes. Typed DXGI 95/96 preserve unsigned/signed BC6H respectively. The format has no alpha channel; header alpha metadata is retained without changing pixels. There is no tone mapping, gamma transform or clipping to RGBA8. Six bytes per decoded pixel count against the same default aggregate limit. Surface, mip, input, trailing-data and dimension gates remain explicit; typeless ID94 is rejected.

Only validated sixteen-byte raw blocks enter the existing pinned dependency. Reserved mode prefixes 19/23/27/31 produce zero RGB. All fourteen modes and signed/unsigned output match an independent bcdec decoder in Godot 4.7.2 for forty standards-only fixtures/264 mips. Actual RGBH GPU readback preserves their complete half bytes. See the [HDR checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md). Static material bundles now retain separate RGBA8/RGB-half images under the [HDR material contract](BC6H_MATERIAL_BUNDLE_CONTRACT.md); scene tone mapping remains open. This extension is not evidence of original executable DX10 acceptance.
