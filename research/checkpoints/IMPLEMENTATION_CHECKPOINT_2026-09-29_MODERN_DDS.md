# Modern DDS extension checkpoint

Date: 2026-09-29. User steering: add newer DDS support rather than limiting the
decoder to installed legacy formats. Question: can the same bounded mip/pixel
contract accept typed DX10 surfaces and BC4/BC5 without losing encoding metadata?
Stop at independent synthetic byte/pixel agreement and explicit unsupported gates.

## Evidence and implementation consequence

[Microsoft's DX10 header](https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dds-header-dxt10)
defines the extra 20 bytes, dimension/array/cube fields and alpha modes;
[DXGI constants](https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format)
identify typed UNORM/sRGB formats. This extends replacement functionality; it is
not a claim that the dated original executable accepts DX10 or these formats.

**Confirmed implemented:** typed BC1–BC5, RGBA/BGRA32, R8 and RG8 2D DX10;
ATI1/BC4U and ATI2/BC5U legacy aliases. Color-space and alpha metadata are
retained without applying gamma/unpremultiplication. BC4 remains a red channel;
BC5 remains red/green. Normal-Z reconstruction is material policy, not a decoder
side effect. Details and numeric IDs are in the [durable contract](../../docs/DDS_DECODER_CONTRACT.md).

One 2D surface is the current resource boundary. Arrays, cubes, volumes, typeless
formats, reserved alpha bits, signed BC4/BC5, BC6H and BC7 fail explicitly.
BC6H requires a float/HDR result; BC7 can later extend the RGBA8 dispatch and
shared mip validation. Neither codec is represented as implemented.

## Validation

`verify_modern_dds_fixtures.py` independently writes 16 synthetic DX10/ATI
fixtures and requires every native mip hash to match Pillow (or the independent
raw RG8 channel reference). BGRA/RGBA patterns are asymmetric; BC4/5 contain
interpolated endpoints and selectors. The tool refuses to replace differing
existing fixtures. Files remain ignored and contain no original assets.

```powershell
python tools/python/verify_modern_dds_fixtures.py local-private/generated-evidence/textured-mesh-hour/modern-dds poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
```

Core tests additionally cover all supported sRGB aliases, premultiplied-alpha
metadata preservation, every truncated modern fixture prefix, unsupported DXGI
IDs, array sizes 0/2, cube/3D declarations and reserved alpha values. All pass.
No installed modern DDS corpus or BC6H/BC7 pixels were tested.

Next modern-format action: introduce independently checked BC7 mode/partition
fixtures and an RGBA decoder; keep BC6H and signed-channel output contracts
separate. The [prototype host preview](IMPLEMENTATION_CHECKPOINT_2026-09-29_ORIGINAL_PROTOTYPE_PREVIEW.md)
already distinguishes explicit Linear versus sRGB/unspecified color textures.
