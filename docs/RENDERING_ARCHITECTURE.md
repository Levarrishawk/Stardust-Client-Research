# Rendering Architecture

## Particle visual sampling boundary (2026-09-13)

PTCL is sampled using normalized particle age: CLRR yields RGB, waveforms yield alpha and velocity modulation, and PTCL/0003 supplies three rotation axes. PTQD adds rotation and two dimensions; its lock flag reuses primary size for the secondary axis. PTEX selects a sprite-sheet frame and UV rectangle. A modern renderer should accept sampled `{transform, rgb, alpha, sizeX, sizeY, rotation, uvRect, resourceId}` records and must not own waveform evaluation, motion or lifetime policy.

The legacy quad path at `0x007A2660` CPU-expands every particle into six dynamic triangle-list vertices containing position, packed color and UV. It disables culling for submission. This is not a required internal architecture: a modern backend may instance one record per particle and synthesize corners in shaders, provided age sampling, PTEX animation, local-space following, two-sided rendering and transparent ordering remain behaviorally compatible.

PTMH uses the same PTCL base but instantiates a normal `.apt` appearance through the appearance-template resource system. Its four specialization curves are uniform scale and three rotations. The simulation updates the child appearance transform and scale; a modern backend may instance or pool it but must preserve `.apt` indirection rather than assuming the resource is a single mesh.

PATT attachments also instantiate ordinary appearance templates. Birth/death/timed/collision policy belongs to particle simulation, as does kill-with-parent cleanup. The renderer only sees the resulting child appearance instances; recursive `.prt`, `.swh`, `.apt`, `.msh` and `.sat` paths must all pass through the shared appearance factory.

PTEX drives quad UV selection and shader binding. A disabled descriptor intentionally creates no shader primitive. Enabled descriptors resolve through the shared shader manager. Frame selection supports normalized-lifetime and explicit-FPS modes over inclusive ranges in 1×1/2×2/4×4 atlases. Modern GPU batching may replace per-effect local primitives but must group on resolved material plus relevant render state, not merely texture filename.

## Confirmed raster-module boundary

Stardust.exe does not import D3D9 directly. `graphics_install_VA_007548A0` reads a configured raster major, rejects version 4/DX8, accepts majors 5 through 7, formats `.\\s2%02d_r.dll`, loads it with `LoadLibraryA`, resolves exported `GetApi`, calls the returned interface's validation and install methods, and retains the API table in globals. This is a clean legacy abstraction boundary between engine rendering concepts and the concrete rasterizer.

For configured major 5, installed `s205_r.dll` is 32-bit, exports only `GetApi`, and imports `Direct3DCreate9` plus D3D performance markers from `d3d9.dll`. It also retains one DirectDraw compatibility import. Therefore D3D9 device creation and resource implementation live in the raster DLL, while Stardust.exe owns higher-level appearances, shader/effect templates, sorting and render phases. This strongly supports rebuilding behavioral renderer interfaces instead of translating D3D9 calls from the main executable.

The configured major-7 module `s207_r.dll` has the same single `GetApi` export and D3D9 import boundary. `GetApi` returns a two-entry bootstrap interface: a verifier and installer. Its installer at `0x62A01BE0` consumes platform/config/report callbacks, then publishes a raster API table containing well over one hundred function pointers for frame/device state, resource creation, render targets, buffers, textures, shader programs and draw/state operations. Diagnostics confirm adapter enumeration, display modes, shader capability selection, create/reset/present, lost/reset recovery, depth/stencil and render-target surfaces, static/dynamic vertex/index buffers, sampler/render state and shader constant upload. This bounds the legacy backend as a comprehensive immediate-mode graphics service; reproducing its slot ABI is unnecessary for a clean renderer.

The visible-light boundary is also recovered. `Light` instances register into a 32-bit sorter mask; the selected pointer vector crosses raster API slot `+0xE8` (`s207_r.dll` `0x62A04F60 -> 0x62A09EF0`). Backend consumer `0x62A0A160` classifies the lights, and point-light constant builder `0x62A0A3C0` packs world position, color and the three object floats at `+0x100,+0x104,+0x108`. Those floats are the ordered attenuation coefficient vector; conventional constant/linear/quadratic labels are strongly inferred from the `(1,0,0)` default and D3D convention. A replacement renderer should model this semantic record directly, not reproduce the mask or API-table ABI.

The local `d3d9.dll` is a separate 32-bit proxy/wrapper exposing the four functions consumed by `s205_r.dll`; its strings describe device creation, reset/presentation recovery and wrapper effects. Those wrapper-specific behaviors are deployment additions and are not treated as original client architecture.

## Installed-client settings

`C:\Stardust-DEV\options.cfg` selects a 1920×1200 windowed client, hardware mouse cursor, `useSafeRenderer=0`, `rasterMajor=7`, and a Direct3d9 fullscreen refresh rate of 60 Hz. `user.cfg` caps the client at 60 frames per second and sets `cameraFarPlaneSpace=16384`. These are **confirmed configuration inputs**. Their exact consumers inside `Stardust.exe` still require string/data xrefs.

## Findings

The main executable exports and RTTI reveal explicit `Material`, `Texture`, `StaticShader`, shader implementation/pass, pixel- and vertex-shader program, static/dynamic index buffer, static/dynamic vertex buffer, and graphics-data adapter types. This confirms a resource/object abstraction above the device API.

Configuration uses `[ClientGraphics]`, `rasterMajor=7`, and `[Direct3d9]`. The executable directory contains versioned rasterizer DLL families (`gl05_r.dll`, `gl06_r.dll`, `gl07_r.dll`, `s205_r.dll`, `s206_r.dll`, `s207_r.dll`, `em05_u.dll`, etc.). The direct xref and call chain at `0x007548A0` makes dynamic raster-module selection **Confirmed**.

The local `d3d9.dll` is a newer x86 proxy whose four exports are `Direct3DCreate9` and three D3DPERF marker functions. Coupled with `d3d9-postfx.ini`/`.log`, this strongly identifies it as a deployment post-processing shim, not the original renderer module. Its behavior must be kept separate from reconstruction of the 2005 client.

DPVS is a confirmed visibility layer: cells, portals, camera frusta, scissor rectangles, object bounds/models, minimum coverage, and visibility parents are imported. It is architectural, not merely an optimization toggle, because scene cells/portals map naturally to SWG interiors.

## Confirmed render-phase registration

`client_graphics_application_install_VA_0075A220` registers semantic render phases and assigns group/order values before installing later client graphics systems. Retained/source-style tags correspond to environment, terrain, main geometry, blob/shadow-like content, pre-alpha, alpha, blended, below-water, water, above-water, post-alpha, shadow and final/last phases. Exact expansion of two abbreviated tags remains provisional, but registration and ordering are **Confirmed**.

The per-frame function `frame_tick_VA_004237C0` updates simulation/resource systems first, then enters a quit-guarded graphics block, runs preparation/world-render calls, exits the guard, and chooses one of two final present paths. See `GAME_LOOP_ARCHITECTURE.md`.

This is a strong mapping target for a modern renderer or engine: preserve semantic queue ordering and water/transparency dependencies while replacing D3D9 state-machine implementation.

## Effect and shader pipeline evidence

`shader/defaultshader.sht` supplies material, texture-map and texture-coordinate-set forms and references `effect/a_simple.eft`. The installed effect contains multiple implementation/pass branches: VS/PS 2.0 programs, VS/PS 1.1 programs, and a fixed-function fallback. Other installed program paths explicitly cover terrain blending/DOT3, water passes, UI/radar, skyboxes, environment/specular/emissive maps, procedural clouds, screen shaders and texture-renderer compositing.

This confirms the translation unit should be the semantic SHT/EFT material graph plus selected pass, not an individual legacy shader program. Capability fallbacks can become offline/import-time modern shader variants. Exact render-state fields and constant bindings remain to be decoded.

## Rendering concepts likely tied to assets

- `.sht` shader templates and referenced vertex/pixel programs.
- `.dds` textures.
- static and skeletal mesh TREs.
- appearances (`.apt` and related IFF forms), LOD/composite appearance structures.
- terrain `.trn`, portal/cell data, lights, particle/client effects.
- DPVS proxy/bounds data embedded in appearance/world assets.

Exact format/version coverage remains unknown and is a major workstream.

## Engine and renderer choice is provisional

No implementation choice is final during analysis. Once all client analysis needed to constrain a build is complete, perform the pre-build engine decision gate specified in `64BIT_PORT_PLAN.md`. That gate explicitly includes Godot, a custom DX12 client, and a custom Vulkan client. An engine must be judged against recovered SWG protocol, resource, scene/cell, terrain, animation, material and lifecycle requirements—not only its headline rendering API.

## DX12 versus Vulkan for a custom renderer

Provisional custom-renderer recommendation: **DirectX 12 for a Windows-first replacement**, behind a deliberately narrow renderer interface. This is not yet a decision to build a custom renderer.

Why DX12 currently wins:

- The deployed client and operational workflow are Windows-specific.
- PIX, Visual Studio Graphics Diagnostics, DRED, and Windows GPU-driver tooling reduce bring-up risk.
- HLSL and DXC provide a direct modern shader toolchain; old shader concepts can be translated into generated HLSL/PSOs.
- Windowing, fullscreen/HDR, input coexistence, crash diagnostics, and installer integration are simpler for a Windows-only first target.
- Vulkan does not make legacy D3D9 semantics automatically easier; it adds instance/device/extension and cross-vendor validation complexity before portability has demonstrated value.

Vulkan becomes preferable if Linux-native client support is an actual near-term requirement, or if a portability layer is a primary product constraint. Keep asset decoding, material descriptions, visibility results, and draw-packet construction API-neutral so a Vulkan backend remains feasible.

### Godot evaluation questions

The later evidence matrix should test whether Godot can cleanly support the recovered requirements, especially custom binary resource loaders/importers for TRE/IFF-derived assets, stable 64-bit network/object identifiers, transactional baseline/delta application, zone and cell/portal streaming, SWG skeletal customization, terrain scale, translated legacy material states, and controlled threading/lifecycle order. Prototype scoring should use one representative asset and one replicated-object update path, not a broad gameplay build.

Engine-provided rendering, windowing, input, audio, UI and tooling could materially reduce work. Conversely, any need to fork core engine behavior, duplicate its scene graph, or bypass its resource system would reduce that advantage. These are Unknown until the asset/world analysis supplies concrete test cases.

## Do not emulate D3D9 calls

Reconstruct material and frame behavior, then map to immutable pipeline descriptions, descriptor layouts, modern resource barriers, and explicit lifetime management. Avoid a line-for-line D3D9 compatibility wrapper as the replacement architecture. It would preserve the old state-machine coupling and obscure opportunities for batching, streaming, and multithreaded preparation.

## Next static-analysis targets

1. Recover the `GetApi` table slots and renderer install/remove/device-create/reset contract.
2. Recover texture/vertex format enums from raster-module switch tables.
3. Map `.sht` parsing to render states, texture stages, shader constants and fallback selection.

## Confirmed mesh draw and material-selection boundary

The SPS topology enum is completely recovered: IDs 0-5 are non-indexed point list, line list, line strip, triangle list, triangle strip and triangle fan; IDs 6-11 are their indexed equivalents. The table is initialized at `0x0076F890` with twelve wrappers into consecutive graphics-interface slots `0x120` through `0x150`. Primitive statistics independently confirm triangle-list `/3` and strip/fan `-2` arithmetic.

Sorted index data is view-dependent rather than an alternate LOD. `static_shader_primitive_prepare_VA_007707D0` transforms the active camera displacement to primitive-local space, compares it against each SIDX direction, selects the minimum dot product, and binds that record's index buffer. The portable mesh representation therefore needs optional view-sorted index variants alongside its default indices.

SHT and EFT loading are cached and fault-tolerant through default shader/effect resources. EFCT 0000/0001 contain ordered implementation candidates; the original client selects the first candidate supported by current graphics capabilities. EFCT/0001 also carries `containsPrecalculatedVertexLighting`, exposed through `ShaderTemplate` virtual slot 7 and ultimately by the exported `StaticShader::containsPrecalculatedVertexLighting()`. IMPL 0000-0007 adds capability/option gates over time and owns counted PASS records. PASS 0000-0009 is the next detailed state-decoding boundary. For a modern renderer, preserve the semantic candidate/pass graph and the precalculated-lighting classification at import time, translate viable legacy branches into modern pipeline variants, and choose based on required features rather than emulating D3D9 capability levels literally.

## PASS representation boundary

PASS 0009 is the executable-supported superset of a versioned 0x80-byte pass description. The loader explicitly reads 24 U8 fields and eight U32 fields from DATA, then loads one of two program paths:

```text
PASS
  DATA: child count + render-state scalar/tag fields
  PFFP 0000/0001 fixed-function state | PVSH 0000 vertex-program name
  PPSH pixel program | repeated fixed-function texture-stage descriptions
```

Older PASS versions populate progressively smaller subsets and rely on constructor defaults. This makes the correct modern boundary a validated logical description, not the legacy object's byte layout. Program-backed passes can translate into modern shader/pipeline variants. Fixed-function passes require a small compatibility shader generator driven by decoded PFFP and texture-stage operations.

PFFP is specifically the fixed-function **vertex-lighting** half, not the texture combiner. Loaders `0x00762360/0x00762440` decode three enable bytes and four U8 material-source selectors; v0000 also decodes a material tag. A fresh configured-overlay scan found 704 v0001 records (11 distinct payloads) and one v0000 record. The old record's tag is `MAIN`, confirming the migrated material-tag role. The seven remaining fields fit the D3D9 lighting/specular/color-vertex plus four material-source contract. A modern importer should retain their raw order, map the source selectors to material constant versus vertex color channels, and generate the vertex-lighting shader variant; no native D3D9 state-object layout should cross this boundary.

The installed raw corpus is overwhelmingly PASS 0008/0009, so these should be implemented first after the exact field labels are closed. Later patches also contain PASS 0010 and IMPL 0008/0009 beyond the main executable dispatchers. The replacement's asset-compatibility requirement makes these a required investigation even if current Stardust.exe never reaches them.

## PASS-to-D3D9 compilation boundary

The configured `s207_r.dll` is now part of the address-backed analysis rather than merely an import inventory. Its SHA-256 is `D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D`, image base `0x62A00000`. Graphics API slot `+0xA0` (`0x62A046A0`) selects the current shader implementation and slot `+0xA4` (`0x62A046B0`) applies a selected pass. The latter reaches `0x62A0F4B0`; passes are compiled into fixed 0xA4-byte backend records and applied by `0x62A0EF30`. This is **Confirmed**.

The backend compilation path is:

```text
engine ShaderImplementation / PASS records
  -> raster implementation build 0x62A0F3C0
  -> one-pass compiler 0x62A0E9D0
  -> backend pass record (0xA4 bytes)
  -> pass selection 0x62A0F4B0
  -> state application 0x62A0EF30
```

`0x62A0B9B0` applies implementation-level render-state `(D3DRS,value)` pairs with `IDirect3DDevice9::SetRenderState`, then selects its pixel shader. `0x62A0E910` applies one compiled texture binding and its `(D3DSAMP,value)` pairs with `SetSamplerState`. Thus the asset descriptions are deliberately compiled once into backend-neutral engine state plus D3D9-specific cached records; a replacement should preserve that compile/cache boundary while emitting modern pipeline descriptors.

The one-pass compiler closes several previously anonymous PASS 0009 fields:

| PASS runtime offset | Meaning | Evidence/confidence |
|---:|---|---|
| `+0x30` | alpha-test enable | controls call to exported `StaticShader::getAlphaTestReferenceValue`; **Confirmed** |
| `+0x34` | alpha-reference tag or sentinel (`A255`, `A128`, `A001`, `A000`) | explicit tag comparisons and fallback lookup; **Confirmed** |
| `+0x40`, `+0x44` | texture-factor 1 and 2 tags | exported `getTextureFactor`; **Confirmed** |
| `+0x48` | texture-scroll tag | exported `getTextureScroll`; **Confirmed** |
| `+0x4C` | stencil-reference enable | gates exported stencil lookup; **Confirmed** |
| `+0x50` | stencil-reference tag | exported `getStencilReferenceValue`; **Confirmed** |
| `+0x7C` | material tag | exported `getMaterial`; **Confirmed** |
| `+0x14` | three-way fog override (`inherit`, force off, special/disable sentinel) | compiled to backend field `+0x94`, which drives D3DRS 34 (`FOGENABLE`); exact third-value policy **Strongly inferred** |

The compiled record also contains material constants, two normalized texture factors, four scroll values, alpha/stencil references, a packed vertex declaration/texcoord selection, a texture/sampler vector, and cached shader handles. This is enough to specify the modern material compiler shape without copying the 32-bit backend layout. Remaining work is to label the other PASS booleans/enums by tracing construction of the implementation-level D3DRS pair vector and to decode each fixed-function color/alpha operation.

## Exact core render-state map

Raster compiler `0x62A0B340` emits the `(D3DRS,value)` vector applied by `0x62A0B9B0`. Literal D3D9 identifiers confirm:

| PASS offset | D3DRS | Meaning |
|---:|---:|---|
| `+0x10` | 9 | SHADEMODE (translated enum) |
| `+0x18` | 26 | DITHERENABLE |
| `+0x19` | 7 | ZENABLE |
| `+0x1A` | 14 | ZWRITEENABLE |
| `+0x1C` | 23 | ZFUNC (translated enum) |
| `+0x24` | 171 | BLENDOP |
| `+0x28/+0x2C` | 19/20 | SRCBLEND/DESTBLEND |
| `+0x30/+0x38` | 15/25 | ALPHATESTENABLE/ALPHAFUNC |
| `+0x3C` | derived flags | four-channel color-write mask |
| `+0x4C/+0x54` | 52/56 | STENCILENABLE/STENCILFUNC |
| `+0x58/+0x5C/+0x60` | 55/54/53 | STENCILPASS/ZFAIL/FAIL |
| `+0x4D` | 185 | TWOSIDEDSTENCILMODE, when supported |
| `+0x64` | 189 | CCW_STENCILFUNC |
| `+0x68/+0x6C/+0x70` | 188/187/186 | CCW_STENCILPASS/ZFAIL/FAIL |
| `+0x74/+0x78` | 59/58 | STENCILWRITEMASK/STENCILMASK |

Together with the previously confirmed material, alpha/stencil reference, factor, scroll and fog fields, this closes the core PASS state roles. Translation-table element values still require enumeration.

Per-texture compiler `0x62A0E570` compiles D3DSAMP 10 (maximum anisotropy), 1/2/3 (address U/V/W), and 7/6/5 (mip/min/mag filtering). TextureData supplies defaults; sentinel values select stage overrides. `0x62A0E0E0` binds the texture and `0x62A0E910` applies the sampler vector. Fixed-function color/alpha combiners remain pending.

## Effective newer-version assets

A corrected configuration-aware scan processed all 48 configured TRE archives in descending priority, honoring true zero-size masks. It resolved 23,393 winning `.sht/.eft` paths: 23,306 live and 87 masks, with zero parse errors. The prior 23,286/107 split misclassified 20 live uncompressed SHT records. Twenty-seven live winners contain forms beyond the traced dispatchers: 24 SHT records from priority-47 `mtg_patch_018.tre` and three EFT records from priority-40 `mtg_patch_011_files_01.tre`. Raw IMPL 0008 assets are shadowed, but IMPL 0009/PASS 0010 assets remain visible.

PASS 0010 and IMPL 0009 are therefore **Confirmed runtime-namespace winners**, not historical residue. Whether every asset is requested in a normal session remains Unknown. Full original-asset compatibility nonetheless requires decoding them.

## Newer shader schema boundary

The effective-corpus payload inventory proves `PASS 0010` is a one-byte extension of the serialized PASS state prefix, not a different child graph. All 108 records use a 57-byte DATA chunk and the same fixed-function or programmable children as PASS 0009. The added byte is before the first U32 tag; its exact insertion ordinal is not assigned without a loader or matched historical asset.

`IMPL 0009` retains SCAP/optional-OPTN/DATA/PASS ordering and the seven-byte DATA width. A tolerant engine-neutral decoder can preserve the additional PASS scalar as explicitly unknown and retain IMPL flags verbatim until semantic fixtures exist. It must not silently interpret 0009/0010 as 0007/0009.

## Generated fixed-function vertex path

`0x62A15070` is a vertex-program variant cache rather than the STAG color/alpha combiner. It caches by the packed mapping of up to eight texture-coordinate selections and calls `0x62A14740` on a miss. The latter emits macros such as `vTextureCoordinateSet*`, `textureCoordinateSet*`, `maxTextureCoordinate`, and `TARGET`; selects `vs_1_1` below shader capability 2.0 and `vs_2_0` otherwise; invokes the D3DX shader compiler; and creates an `IDirect3DVertexShader9` through the device vtable. One branch emits explicit TEXCOORD declarations/register bindings.

This closes how fixed-function-era texture-coordinate generation is represented on D3D9: dynamically compiled vertex shader source, even when the pixel side uses PFFP/STAG. A modern renderer should translate it into shader variants or a small parameterized shader family. This function does **not** decode STAG color/alpha operations; those remain open.

## Fixed-function STAG contract and backend reachability

The STAG v0000 loader's 33-byte order matches the classic D3D9 fixed-function state model: color operation plus three arguments and modifier flags; alpha operation plus three arguments and modifier flags; result argument; texture and coordinate-set tags; address U/V/W; three filter overrides; and one unresolved trailing enum. The effective corpus contains 951 records with 128 distinct payloads. Dominant operation 3 maps coherently to D3DTOP_MODULATE under a `+1` translation; values 1/2 map to SELECTARG1/SELECTARG2, 4 to MODULATE2X, 6 to ADD, and 12/13/15 to the corresponding alpha-blend operations. This mapping is **Strongly inferred** from loader order, D3D9 ordering, defaults and asset use—not claimed as a recovered active translation table.

The installed s207 backend initializes D3DTSS 1-6, 7-11, 22-24, 26-28 and 32 for every stage, and `0x62A0B230` can apply a cached `(D3DTSS,value)` vector through `IDirect3DDevice9::SetTextureStageState`. However, Ghidra finds no code/data reference to `0x62A0B230`, and a raw pointer search finds no stored vtable entry. The active implementation factory `0x62A04D40` constructs `0x62A0BAA0`, whose compiler `0x62A0B340` assumes a programmable pixel-shader reference. Therefore the deployed backend's directly recovered route is programmable; the D3DTSS applicator is an orphaned or inactive legacy path in this binary.

Replacement consequence: STAG must still be decoded because 951 effective assets contain it and fixed-function-only fallbacks may be needed, but it should be translated into a modern fragment-shader expression graph. Reproducing D3D9 texture-stage state calls or their inactive backend class hierarchy is unnecessary.

## Texture parameter resolution and binding

The active material path is now end-to-end:

```text
SSHT/TXMS/TXM DATA + optional NAME
  -> texture-data map keyed by four-character tag
  -> StaticShader::getTextureData(tag) at 0x0076AEF0
  -> STAG texture tag at runtime +0x3C
  -> s207 per-stage compiler 0x62A0E570
  -> cached texture pointer + D3DSAMP vector
  -> 0x62A0E910 / SetTexture + SetSamplerState
```

The second STAG tag is independent: `0x62A0E9D0` passes pixel-program coordinate tags to `StaticShader::getTextureCoordinateSet` at `0x0076AF80`, packs up to eight three-bit coordinate-set indices, and selects the generated vertex-shader variant. This confirms the two STAG tags as texture-data key and texture-coordinate-set key respectively.

`TextureData` is a 36-byte legacy runtime value containing a dirty/special byte, address U/V/W, mip/min/mag filters, maximum anisotropy and a `Texture*`. The shader compiler copies values into a 0x14-byte backend binding record; neither pointer-bearing layout is suitable as a 64-bit compatibility structure.

Named textures are loaded from a `NAME` chunk through `TemporaryCrcString` and the central texture cache. Records marked special, plus `ENVM`, omit initial named-resource resolution. In s207, tag `ENVM` and every tag beginning with `_` use a global dynamic-texture registry (`0x62A12380`, lookup `0x62A12340`, update `0x62A123B0`). Registry replacement fetches the new texture and releases the old one, so ownership is explicit and mutable at runtime.

Corpus inspection found 41,741 effective TXM records and 45 distinct tags. The overwhelming sampler defaults are address WRAP and LINEAR filtering. The modern IR should therefore model `{textureTag, source, sampler, coordinateSetTag}` separately: named resource references, per-material substitutions and global dynamic render targets are three distinct source classes.

## DDS creation and upload

`TextureList` initializes a CRC-string-keyed cache at `0x00764930`. Fetch at `0x00764DA0` holds the texture-list mutex, reuses an existing `Texture`, or allocates a 0x30-byte object and constructs it at `0x00768BB0`; the returned object receives an additional reference. `Texture::fetch` is a plain increment and `Texture::release` a plain decrement, with zero removing the cache entry, destroying graphics data and freeing the object. The reference counter itself is not atomic.

Filename-backed construction at `0x00769170` directly reads the standard DDS magic/header from `TreeFile`. It recognizes uncompressed BGRA/BGR/A1R5G5B5/A4R4G4B4/R5G6B5/luminance layouts, DXT1-DXT5 and PNM1, mapping them into the engine's 14-value `TextureFormat` domain. Unsupported or malformed formats close the file and recursively substitute the configured default texture.

The loader applies `ClientGraphics.discardHighestMipMapLevels`; filenames containing `_n.dds` use the greater of that value and `discardHighestNormalMipMapLevels`. Discard is clamped below the final mip, and block-compressed dimensions are not reduced below 4x4. If the hardware/backend cannot support mipmaps, only the base retained level is created and remaining bytes are skipped.

Graphics API slot `+0x114` creates `Direct3d9_TextureData`. Backend constructor `0x62A12510` tries a prioritized format list until `CreateTexture` or `CreateCubeTexture` succeeds. Ordinary file textures use D3DPOOL_MANAGED and usage zero. The engine then iterates each retained 2D mip or all six cubemap faces, computes linear or block-compressed row sizes from `TextureFormatInfo`, locks the destination, copies rows from the abstract file, and unlocks it. Conversion paths use an offscreen surface plus `D3DXLoadSurfaceFromSurface` when source and selected GPU formats differ.

The installed `s207_r.dll` closes the exact 14-value map: IDs 0-13 are
ARGB8888, ARGB4444, ARGB1555, XRGB8888, RGB888, RGB565, RGB555, DXT1-DXT5,
A8 and P8. Backend D3DFORMAT values are respectively 21, 26, 25, 22, 20,
20, 20, DXT1-DXT5 FourCCs, 28 and 41. Thus RGB565/RGB555 sources are expanded
to the backend's `R8G8B8` texture. Backend-only float entries 14/15 are not
named/supported by this client's `TextureFormatInfo` table. See
`ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md`.

Static file textures have no separate lost-device reconstruction callback because the D3D9 managed pool owns reset restoration. A modern explicit API cannot inherit that behavior: the replacement resource manager must retain a logical asset key or CPU upload source and recreate GPU images/descriptors after device loss. It should not preserve the original 32-bit `Texture*`, graphics-data pointer or non-atomic reference layout.

Effective DDS inventory: 29,903 valid resources, dominated by BC3/DXT5 (21,673) and BC1/DXT1 (4,874), with 174 cubemaps. Three `.dds` paths actually contain `FORM SSHT`, so dispatch must validate content magic and fail through the normal fallback path rather than trust extensions.

## Procedural textures and render-target copy service

The executable exposes three 0x30-byte Texture factories: `0x00765000` creates and fills a solid-color texture, `0x00765070` uploads supplied CPU pixels while honoring backend row pitch, and `0x007650E0` creates an empty procedural/renderable texture. Direct callers prove use by bloom/downsample/blur, dynamic-normal-map testing, terrain/environment setup and UI/radar canvases. Bloom registers render textures under four-character shader tags including `MAIN` and `SMAL`.

The raster helper at `0x62A0AD10` is separate from ordinary texture creation. It saves render target 0 and the depth-stencil surface, evicts managed resources, and lazily allocates fixed 512x512 A8R8G8B8 render-target/default-pool and system-memory textures. `0x62A0AF40` selects a texture subresource as render target, tracks its mip dimensions and chooses a sufficiently large scratch mip. `0x62A0B120` uses `GetRenderTargetData` and `D3DXLoadSurfaceFromSurface` for GPU readback/copy, releases transient surfaces, and restores the prior target/depth binding through `0x62A0AED0`.

Pre-reset `0x62A03520` calls registered callbacks and releases this and other default-pool resource families. Post-reset `0x62A03560` recreates them and invokes reconstruction callbacks. This is direct evidence that a replacement backend needs explicit lifetime phases for renderable images, subresource selection, synchronization/readback and pass ordering. The fixed global 512x512 scratch implementation is a legacy mechanism, not a compatibility requirement.

## Outer frame and presentation schedule

The raster `GetApi` base is exactly `0x62B835B8`. Main wrappers at API offsets `+0x80`, `+0x84`, `+0x88`, `+0x8C` and `+0x90` therefore map to raster frame-state update `0x62A03D70`, `BeginScene` entry `0x62A03DC0`, `EndScene` entry `0x62A03E30`, default-present entry `0x62A04010` and explicit-destination present `0x62A04040`. Common presenter `0x62A03E60` handles lost/not-reset returns, tests cooperative level, executes pre-reset release, resets with retained parameters, and runs post-reset reconstruction.

The main frame calls these around a single `IoWinManager` render virtual. IoWin is a linked stack shared by input routing and rendering; GroundScene, CuiManager, hyperspace and death/transition windows are confirmed participants. GroundScene vtable slot `+8` is `0x0051B770`, which establishes camera/clear state and invokes nested world rendering.

## GroundScene, bloom and CUI layering

`CuiIoWin` render at `0x0093B2B0` closes the normal-game composition order. It calls bloom capture `0x0064B500`, renders its next/lower IoWin through `0x00AB58E0`, resolves bloom at `0x0064B560`, conditionally runs GroundScene post rendering `0x0051BCE0`, renders the root CUI through `0x00881210`, and may finish with a screen-space fade/overlay rectangle. The UI is deliberately outside the bloom chain.

Bloom install `0x0064B040` reads the ClientGame/Bloom controls. Resource creation loads `2d_downsample_4x4.sht`, `2d_blur.sht` and `2d_bloom.sht`. Resolve performs downsample, horizontal and vertical Gaussian blur, then backbuffer composite; `0x0064B3D0` generates seventeen weights/offsets.

GroundScene render obtains the confirmed `TerrainObject` singleton. Its appearance virtual `+0x128` is now confirmed as the environment/clear-color getter, not geometry rendering. A camera-position query against terrain/water height controls the below-water branch. D3D9 Clear and SetViewport are confirmed through the s207 API callees.

Concrete procedural terrain geometry is callback-driven. `0x00561060` installs terrain callbacks around each camera operation in `0x00561180`. `ClientProceduralTerrainAppearance` method `0x0084AAA0` traverses visible terrain structures; `ClientTerrainSorter` batches primitives at `0x0084EDB0`, submits them through `0x0084F110 -> 0x00773AE0`, and recycles temporary structures through `0x0084EBA0`. The sorter has five category buckets; exact mapping to terrain/water phase tags remains open.

Both water managers explicitly resolve and register to `WATR`. Global water owns one large static or dynamic planar primitive; local water owns and frustum-culls a vector of bounded primitives. Prepare methods set camera-side/time constants and bind shader/vertex/index resources, followed by indexed geometry draws. No mirrored camera or reflection/refraction render target is visible in these paths.

```text
scene target: GroundScene / other lower IoWin
       -> optional bloom downsample + H/V blur + composite
       -> GroundScene-only post-world stage
       -> root CUI pages and UI overlays
       -> final transition/fade overlay
       -> EndScene / Present
```

ShaderPrimitiveSorter begins a camera/environment scope at `0x00773B10`, updates dynamic `ENVM`, initializes 12 phase records, and separates primitives with and without precalculated vertex lighting. `0x00773DE0` closes/flushed phases and restores prior camera/environment state. The registered coarse order is ENV, TERR, MAIN, BLOB, PREA, ALPH/BLND, WBLW, WATR, WABV, PSTA, SHDW, LAST.

## Terrain mesh/material boundary

ClientChunk rendering is CPU-batched rather than one persistent GPU object per cell. `0x008618D0` creates pooled nine-vertex cell primitives grouped under 16-byte ShaderSets. A 3x3 terrain-classification neighborhood selects and caches a one-to-four-layer shader assembled from `terrain_[dot3_]blend0..3.sht`; the material contract uses `TERR`, `LVL0..3`, `NRM0..3` and `ALP1..3` tags.

Mixed LOD is handled by nine immutable 16-bit index patterns selected at `0x00865FF0`. The normal eight-triangle center fan and its seven-/six-triangle collapsed-edge variants change topology without changing positions. During visibility submission, `0x008668E0` appends nine positions and the selected indices into a per-shader CPU batch. Buffer allocation and upload therefore occur after the compatibility-facing terrain generation boundary.

For a modern renderer, terrain generation should publish renderer-neutral positions, normals, material weights/identities and seam relations. Larger engine-native chunk meshes and a different crack-control technique are acceptable only after matching original material transitions and geometry. Legacy object sizes, pointer-rich ShaderSets, pools and D3D9 buffer behavior are implementation artifacts, not compatibility requirements.

## Particle appearance instantiation boundary

The `PEFT` appearance factory now has a confirmed description-to-runtime bridge. A template owns timing data, four version-2 scalar controls and an ordered list of emitter-group descriptions. Creating `ParticleEffectAppearance` materializes one runtime `ParticleEmitterGroup` for each description and delegates per-frame behavior/render preparation to those groups and their emitters. Quad and mesh particles ultimately use the same shader/resource abstractions as other appearances, while `ParticleEmitter::LocalShaderPrimitive` is a renderer-facing transient primitive.

The modern renderer should receive particle draw batches from a simulation-owned particle system. It may replace legacy memory pools, watchers, pointer vectors and D3D9 local primitives, but must preserve authored group order, timing/wave functions, particle-description choice, referenced shaders/meshes and attachment transforms. Top-level version support is complete for every live effective `.prt`; detailed `EMTR` version/field semantics remain the next particle slice.

Emitter-group construction confirms a second ownership tier. Each runtime group retains its immutable `EMGP` description and parent effect, initializes common PTIM-driven timing state, then creates one runtime emitter for every ordered `EMTR` description. Effect → group → emitter lifetime and order are therefore compatibility semantics; legacy 0xD8/0x364 allocation sizes and vector implementations are not. The deployed version census proves a modern decoder needs EMTR 0000, 0006 and 0009-0014 immediately, while implementing the executable's complete 0000-0014 dispatch is inexpensive defensive compatibility.

Modern EMTR field analysis reinforces the simulation/render split. Terrain collision sampling, surface-height offset, surface-frame alignment, generation accumulation and randomized direction changes occur before rendering. DX12/Godot/Vulkan particle submission must therefore consume simulation results rather than reinterpret authored EMTR data directly. Shape selection is a six-value closed enum, while rendered particle payload is a quad-or-mesh tagged union; the optional sound template routes independently through audio/resource management.

Waveform tracing makes this boundary concrete: emitter translation/rotation, shape extent, direction spread, generation rate, initial velocity, lifetime and gravity are simulation inputs. Particle age divided by authored lifetime becomes normalized age for the nested quad/mesh visual description. A modern renderer receives the resulting transform and visual sample; it must not own emission timing, terrain queries, gravity integration or particle death.

## Outdoor environment descriptors

The renderer-facing outdoor state is selected by `(PTAT environmentFamilyId << 16) | serverWeatherIndex`. Each 25-column descriptor supplies gradient-sky texture, independent bottom/top cloud shaders with size and speed, a 256×8 time-indexed lighting ramp, shadow and fog policy, optional camera-attached weather appearance, day/night environment maps and wind scale. GroundEnvironment retains previous/current descriptors and blends their ramp-derived parameters for exactly five seconds. ENVM/0000 separately defines shared star, sun, moon and repeated celestial resources. A DX12/Godot/Vulkan implementation can redesign GPU representation, but it must preserve these asset inputs, family/weather fallback and transition behavior.

All eight lighting-ramp consumer roles are now closed. Rows drive, in order: ambient color/repeated-CELS opacity; main-light primary RGB/intensity; main-light secondary RGB/intensity; fill RGB/intensity; bounce RGB/intensity; fallback sky RGB/SUN-MOON visibility; fog RGB/sky-family opacity; and shadow-volume RGBA. The legacy renderer has distinct generic-celestial, principal sun/moon, and ShadowVolume alpha paths plus two main-light banks; collapsing them would discard authored behavior. When no sky source resource exists, GroundEnvironment builds a solid six-face `_SKY` cube texture from row 5.
4. Trace resource-loss/recreation policy independently of the deployment post-processing proxy.
