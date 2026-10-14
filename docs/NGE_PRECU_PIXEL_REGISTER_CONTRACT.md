# Installed legacy pixel-register bridge

Date: October 14, 2026. This is the NGE source-client renderer adapter for the installed Stardust overlay, separate from the clean x64/DX12 rendering work.

## Evidence and confidence

The effective TRE lookup resolves the pixel include from stardust_3_00.tre, priority52,784 bytes. Its declarations own c0/c1/c2 for DOT3 direction/diffuse/specular, c3/c4 for two texture factors, c5 for material specular color, c6 for its power, c7 for alpha/bloom settings and c8 onward for user values. **Confirmed installed asset declaration**, not a claim that every historical Pre-CU overlay uses the same extension. The vertex include resolves from mtg_patch_011_files_01.tre, priority40,3339 bytes; its primary c0..c59 assignments and light cardinalities match the source definitions inspected. Proprietary include bytes remain private. Pixel include SHA25636B679E349F22E95569E9E68CBDAC0FDA689015AD203FA901E624568D0997D67; vertex include SHA256C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9.

Source correlation: SD-Src-Client src/engine/client/application/Direct3d9/src/win32/Direct3d9_PixelShaderConstantRegisters.h assigns NGE c3/c4 to hemispheric light differences, c5/c6 to factors and c7 to material specular. Direct3d9_LightManager.cpp uploads full five-register lighting data or four-register alpha updates. Direct3d9_StaticShaderData.cpp uploads the two factors and material; StateCache::setSpecularPower previously only retains a CPU value. **Confirmed source producer/declaration mismatch.** This supplies a falsifiable explanation for black character materials; its live visual consequence remains unverified at preparation time.

## Adapter contract

Direct3d9.usePrecuShaderConstants defaults false. When enabled, the engine-independent PrecuPixelShaderConstants helper plans the following producer writes through the existing raster sink:

| Existing producer span | Installed-program upload |
|---|---|
| DOT3 c0..c4 | Keep c0..c2; construct alpha holder at c7; discard hemispheric fields |
| Alpha/DOT3 update c1..c4 | Keep c1/c2; update holder c7; keep direction |
| Direction-only c0 | Preserve c0 and other registers |
| Factors c5/c6 | Upload c3/c4 |
| Material color c7 | Upload c5 |
| Material power | Upload power in c6.x with deterministic unused lanes |
| User c8 onward | Preserve starting register and contents |

The c7 holder gets enable from diffuse alpha, bloom from the first hemispheric record's alpha, zero in its unused third lane, and opacity from specular alpha. Unsupported built-in producer spans reject before invoking the sink. No IFF/shader source/texture assets are modified; hardware/API acceptance remains the renderer's existing responsibility. False retains the NGE upload path. The main vertex-constant producer is unchanged.

## Validation and next gate

Release Direct3d9 builds; native suite passes8747 checks/2068 malformed rejections plus five isolated CRC/ACST cases. New independent register fixtures verify retained c3..c6 through full and partial light updates, c7 field placement, direction-only preservation, seventeen user registers and unsupported spans without partial mutation. These are CPU production-helper fixtures, not GPU fidelity or all-material corpus coverage.

Next: close the unchanged observation client normally, deploy the matching newly built gl05_r.dll and enable only this explicit profile key in the private runtime. Repeat login/selection/world and compare stationary materials. Record exact DLL identity and observed result before accepting the visual correction. [Daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).

## Observed visual correction

Installed gl05_r.dll SHA256A9F4EC54965AF63849E530E08EDE66E978320934842B521C884125E14519E78B uses the unchanged main EXE0E1D600D42B75914DFC56112DD177B7611E6DC294FEED73FBC4B433DC93D7B2A and unchanged TREs. Computer-use login shows a textured, opaque selection character after its normal fade; world entry shows textured character and visible radar imagery. **Confirmed for this stationary character/profile**, not all shaders, all hardware or historical overlay equivalence. Retained old DLL permits rollback; user d3d9 wrapper remains uninvolved.
