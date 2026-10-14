# Legacy pixel COLOR input range

PS1.x/PS2.0 COLOR inputs are saturated to0..1 before shader use. Primary [Microsoft HLSL documentation](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-writing-shaders-9) states this range; the [input register reference](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx9-graphics-reference-asm-ps-registers-input-color) also describes normalized unsigned color interpolation. Original selected PEXE declarations consume v0/v1 as COLOR inputs, closing applicability to the owned character/room programs.

`LegacyPixelColorInput.Saturate` represents this engine-neutral boundary. Prototype host normal/specular and vertex-lit mouth shaders clamp supplied/interpolated vertex diffuse/specular before recovered pixel arithmetic. Primary dot3 light constants and later pixel specular sums remain HDR; no final RGB clamp is introduced.

The helper rejects nonfinite input. It does not model hardware-specific color interpolation precision, the exact point of clamp within raster interpolation, or original vertex LIT power behavior. The adapter applies the defined range at fragment input; exact raster precision remains Unknown.

Low-level compiled-program interpreters/evaluators continue to accept supplied register values. Earlier diagnostic cases using HDR v0 values verify instruction arithmetic only; they bypass the input-range boundary explicitly and are not full pipeline equivalence claims.

GPU fixtures supply negative/HDR diffuse(-0.2,0.5,1.5) and specular(1.5,-0.2,0.5). Eight normal-material and four mouth cases check clamp followed by mask/bloom/fade/alpha equations. Existing original owner-cell route/resources/controls and previous shader gates remain passing. See [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LEGACY_PIXEL_COLORS.md).
