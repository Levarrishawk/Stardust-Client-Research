# Static packed vertex-color boundary

## Executable evidence

The pinned `s207_r.dll` SHA256D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D builds vertex declarations at0x62A13720. At0x62A13876..0x62A138B3, format bit3 emits declaration type4, method0, usage10, usage index0. The descriptor-provided byte offset is retained. The array is passed to device vtable+0x158 at0x62A139E7; failure string identifies CreateVertexDeclaration. Prior original loader00784D90 consumes the matching packed word before UVs.

Type4 is D3DDECLTYPE_D3DCOLOR, normalized packed bytes expanded to RGBA, and usage10/index0 is diffuse COLOR0. These numeric API meanings are documented by [Microsoft declaration types](https://learn.microsoft.com/en-us/windows/win32/direct3d9/d3ddecltype) and [declaration usage](https://learn.microsoft.com/en-us/windows/win32/direct3d9/d3ddeclusage). The direct backend observations establish their application here.

`PackedVertexColor.ToRgba` maps U32 AARRGGBB to `(R,G,B,A)/255` with no gamma operation. Absence remains explicit. A primitive with mixed optional presence is rejected at GPU upload. Prototype host uploads COLOR alongside reflected positions/normals and unchanged authored indices; source alpha is retained even when the inspection shader is opaque.

## Original scene occurrence

The same selected original POB→cell appearances→mesh/material chain contains five static meshes/46 primitives/72,717 vertices.72,495 vertices have COLOR0:34,417 in cell5,3,202 in cell6 and34,876 in cell7. Both door LODs are uncolored. Independent raw VTXA walks agree on every raw-word and normalized RGBA hash. GPU readback checks every uploaded colored vertex; three asymmetric channel/display fixtures agree at max0.001296 within2/255.

## Material distinction

The selected original effect graphs mark `c_specmap`, `c_emismap_bloom` and `c_specmap_emismap_bloom` as precalculated, while `a_specmap_cbmp` is false. This flag classifies submission; it alone does not prove that dynamic lighting must be disabled.

Original ASCII vertex assets provide more specific evidence:

| Vertex asset | Observed color use | Confidence |
|---|---|---|
| c_simple_vs11 | Final diffuse assignment replaces computed light terms with input COLOR0 | Confirmed source behavior; compiled runtime identity unverified |
| c_spec_pp_vs20 | Diffuse adds COLOR0 to computed non-primary light contribution; specular remains dynamic | Confirmed source behavior; original selection/LIT unverified |
| c_spec_vs20_nodot3 | Diffuse adds COLOR0 to computed full light contribution; specular remains dynamic | Confirmed source behavior; original selection/LIT unverified |
| tfcsl_2uv | Includes c_ambient and diffuse_specular modules; exact expanded behavior follows separately | Source structure confirmed; module arithmetic pending |

These are original asset sources, not third-party analogues. The runtime compiles vertex programs; no original execution or shader compilation was performed. Preserve this confidence boundary. Do not replace all precalculated materials with one dynamic or unlit formula.

## Next implementation

Select and validate original pixel programs and texture bindings per family, then preserve baked COLOR0 and the appropriate dynamic contribution. Static room/door rendering remains the prior MAIN-only inspection variant until that opt-in boundary is validated. See [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_STATIC_VERTEX_COLORS.md).
