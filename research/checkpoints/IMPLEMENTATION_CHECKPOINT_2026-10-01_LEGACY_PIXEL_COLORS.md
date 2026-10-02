# Legacy pixel COLOR input saturation — October1,2026

Question: does the modern character/room boundary preserve the original PS1.x/PS2.0 color-register range? Stop at explicit input saturation and rendered HDR/negative cases.

Evidence: original selected compiled pixel programs read COLOR v0/v1; primary Microsoft Direct3D/HLSL documentation establishes their0..1 input range. Existing native/interpreter tests operated on supplied register values, so they did not establish this raster boundary. The [durable contract](../../docs/LEGACY_PIXEL_COLOR_INPUT_CONTRACT.md) distinguishes instruction arithmetic from input preparation and hardware interpolation precision.

Changes: engine-neutral finite saturation helper; normal-mapped and mouth GPU shaders saturate only vertex color inputs before recovered pixel equations. Directional pixel terms and final specular sums remain HDR. Added eight normal/four mouth negative/HDR rendered fixtures; existing32 cell-light/12 normal-material/eight mouth and original route/live-control/resource tests remain passing. Native suite/build pass. GPU logdc55c6805ee6431ab7e09917b255d334 passes. No new source/name/packet/coverage counts.

Confidence: Confirmed API range/application to recovered COLOR declarations; Unknown exact original interpolation precision/clamp staging and runtime LIT behavior. Modern fragment-input clamp is explicit.

Next: apply the same boundary to retained room material variants while preserving baked COLOR0 and excluding global ambient duplication.
