# Verified BC7 DDS extension — 2026-09-29

## Question and stopping condition

Can typed DX10 BC7 (DXGI 98/99) produce bounded owned RGBA8 mips, preserve color/alpha metadata and run inside the Godot host? Stop after all eight modes, partition/rotation cases, edges/mips, negative envelopes and independent pixel comparisons. This is a standard modern-format extension, not a claim that the original executable accepts BC7.

## Provenance and implementation

Primary standards: [Microsoft BC7 mode reference](https://learn.microsoft.com/en-us/windows/win32/direct3d11/bc7-format-mode-reference) and [DXGI IDs](https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format). BC7 stores 16 pixels in a 16-byte block and offers eight valid modes. The no-mode prefix is reserved with zero output. Confidence: Confirmed standard contract.

The core pins BCnEncoder.Net 2.3.0 (MIT OR Unlicense), whose package records source commit `74eda8a330759d81e6307f19aefc547e40687825`. Archive SHA-256: `0D4DECB032058890326AF2CDD23EFEF9372FA4609461A00687CE24C9CEBDE07E`. Its selected netstandard2.1 dependency is CommunityToolkit.HighPerformance 8.4.0 (MIT), archive SHA-256 `ABD45966F2E5BE4D6C5EE22BE70741C724DFF68D06DA6936DF1B4D1837FABC6B`. Lock files preserve resolved versions/content hashes. See [notices](../../../SWG-Godot-Client/poc/THIRD_PARTY_NOTICES.md). Managed dependency archives stay ignored; no original or downloaded source is promoted. Initial sandbox network restore failed; authorized automatic review allowed the scoped private-cache restore. No direct user intervention was needed.

`Bc7Pixels` passes exact validated raw blocks to the codec, reuses a 4×4 pixel buffer, clips partial edges and writes owned RGBA. DDS surface/mip/source/output guards remain ours and precede output allocation. DXGI 98 is Linear, 99 Srgb; alpha metadata is preserved without gamma or premultiplication changes. Reserved prefixes are handled explicitly as zero RGBA. BC6H remains unsupported pending HDR output semantics.

## Independent fixtures and observed discrepancy

`tools/python/verify_bc7_fixtures.py` generates standard bit patterns without the codec's encoder: deterministic seed 20260930, all eight mode prefixes, all available partition IDs, rotation/index-selector cases, UNORM/SRGB pairs, complete nine-level non-square mip chains, clipped 3×5 pixels, a 1×1 constant, a gradient and a reserved block. Twenty fixtures cover 148 mip comparisons: 147 valid-mode mips match Pillow 12.3.0 byte-for-byte; the reserved mip follows the format specification.

Observation: Pillow's reserved-block hash is `20E65596FE7C15642996B35EF3054F8C93A772FC0E91F66B3174AE8FF26A2921` (opaque black). Native/specification zero RGBA hash is `F5A5FD42D16A20302798EF6ED309979B43003D2320D9F0E8EA9831A92759FB4B`. Both are retained in verifier output; this mismatch is not silently declared agreement. Confidence: Confirmed observations; normative output selected explicitly.

Native hand-expected mode-6 fixtures independently check constant RGBA (128,0,64,254), weighted interpolation, reserved output, clipped pixels, small-mip minimum and decode budget. Every truncated typed-header/payload prefix, unsupported surfaces/typeless IDs and invalid metadata still fail through our parser. Existing legacy/modern DDS and transport/host tests pass.

## Godot verification and reproduction

Debug Godot build passes with zero warnings/errors. A real OpenGL GPU run loads the managed codec inside Godot, decodes mode-7 SRGB with nine mips, and reads all uploaded bytes back identically. Fixture SHA-256: `4C1FBEA33AD7838E03456BEA8039447963A17B104E9A4BDD3CA1344DBB9AAD20`. The host UV/front-face fixture also passes. This verifies RGBA uploads, not hardware-compressed BC7 sampling or photometric fidelity. Existing certificate diagnostic persists with exit 0.

Run `verify_bc7_fixtures.py OUTPUT_DIRECTORY CORE_SMOKE_PROJECT`, keeping generated fixtures private. Godot accepts `--dds-self-test FILE --dds-sha256 HASH` for the owned DDS upload gate; run hidden and bounded for GPU checks, or headless for resource checks. Use locked restore then `--no-restore` builds. No original executable, sockets, commits or publication occurred.

## Next probe

Next modern texture milestone: explicit HDR pixel representation and BC6H unsigned/signed fixtures, or signed BC4/BC5 channel semantics. Next appearance milestone: CMPA composition. Do not route HDR values through RGBA8 or label inspection color-space handling as faithful original lighting.
