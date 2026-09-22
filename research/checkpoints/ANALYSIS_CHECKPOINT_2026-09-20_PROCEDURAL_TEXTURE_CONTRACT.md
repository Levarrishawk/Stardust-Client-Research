# Procedural texture constructor contract and use families

Date: 2026-09-20

## Scope

This block closes the positional ABI and observed call families of `texture_create_procedural_VA_007650E0`. It intentionally leaves the two leading byte flags and final backend flag neutrally named where their original enums are absent.

## Exact constructor ABI

`texture_create_procedural_VA_007650E0` allocates a 0x30-byte `Texture`, forwards seven arguments to `texture_construct_procedural_VA_00768EB0`, and returns a fetched reference. The constructor stores:

```text
Texture createProcedural(
    U8  renderTarget,      // stored Texture+0x14
    U8  cubeTexture,       // stored Texture+0x15
    U32 width,             // stored Texture+0x18
    U32 height,            // stored Texture+0x1c
    U32 mipCount,          // stored Texture+0x20
    TextureFormat *formats,// ordered candidate-format array
    U32 formatCount        // number of candidate formats
)
```

The runtime initializes `Texture+0x16=0`, backend pointer `+0x24`, status byte `+0x28=0`, and auxiliary pointer `+0x2c=0`. It then calls graphics backend slot `+0x114` with `(Texture*, formats, formatCount)`.

Thus width, height, mip count, texture kind, usage/pool policy, and format fallback are confirmed rather than merely inferred. The D3D9 backend walks the format candidates in order and stops at the first supported format it can create.

`d3d9_texture_data_construct_VA_62A12510` maps the flags exactly:

- `renderTarget == 0`: `Usage=0`, `Pool=D3DPOOL_MANAGED`;
- `renderTarget != 0`: `Usage=D3DUSAGE_RENDERTARGET`, `Pool=D3DPOOL_DEFAULT`;
- `cubeTexture == 0`: `IDirect3DDevice9::CreateTexture`;
- `cubeTexture != 0`: `IDirect3DDevice9::CreateCubeTexture` (height is not passed to D3D because cube faces are square).

## Confirmed call families

Ten direct call sites exist in the executable:

| Caller | Dimensions/mips | Broad subsystem evidence |
|---|---|---|
| `0x00507880` (two calls) | 128x128 with computed mip chain; 32x32x1 | radar/UI-like render texture pair; first registered to `MAIN` |
| `0x0064B1E0` (four calls) | current display quarter-size or display-derived, one mip | post-processing render targets associated with four named shader/template resources |
| `0x00845CC0` (two calls) | 32x32x1; 1x1x1 | ground-environment support textures |
| `0x008608F0` | square caller-selected size, one mip | lazily rebuilt terrain texture, registered to `MAIN` |
| `0x00869CD0` | BTRT-selected width/height/mips | blueprint texture renderer destination texture |

Every direct user observed here supplies an explicit candidate-format array (usually one entry) and count. No call site allocates a procedural texture without specifying dimensions, mip count, format candidates, and 2D/cube plus render-target policy.

## Replacement-client consequence

Original assets and systems expect runtime-generated textures to participate in the same reference-counted `Texture` abstraction and shader phase registration as file-backed DDS textures. In a modern renderer, this should become a resource descriptor with explicit usage flags and views, while preserving:

- caller-selected dimensions and complete/explicit mip counts;
- effective-format negotiation/fallback;
- phase/material registration behavior;
- resize/recreate behavior for display- and terrain-dependent resources.

The confirmed flags map naturally to modern image dimension and usage descriptors, but their D3D9 pool consequence must be redesigned as heap selection, resource states, descriptors, and explicit synchronization.

## Confidence

- Argument count/order, field offsets, dimensions, mip count, candidate-format array/count, render-target/default-pool policy, 2D/cube selection, backend forwarding, and all ten call sites: **Confirmed by code**.
- Broad subsystem labels for the call families: **Strongly inferred** from owning functions/resources.
- Exact original source identifier spellings remain unavailable, but their behavior is closed.

## Evidence

- wrapper: `0x007650E0..0x00765153`
- constructor: `0x00768EB0..0x00768F2E`
- D3D9 backend creator: `0x62A12510`
- direct callers: `0x0050799A`, `0x005079D2`, `0x0064B225`, `0x0064B266`, `0x0064B2A7`, `0x0064B2BF`, `0x008466B5`, `0x008466CF`, `0x00860961`, `0x00869CEE`
