# 2026-09-30 zero-pass surface omission

## Question and scope

Should a decoded effect containing no passes produce a neutral fallback surface? Stop at native empty/mixed classification, original visible primitive upload identity and all-zero-pass synthetic host tests. Reuse [selected textured scene inputs](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_TEXTURES.md); no original mesh/effect bytes are modified.

## Observations and policy

Confirmed source structure: `shader/mining_outpost_collide.sht` has no named MAIN texture; `effect/e_invisible_collidable.eft` contains one IMPL/0005 with zero passes. Shader names are correlation only. `StaticEffectDescription.HasRenderPasses` examines candidate pass counts. Empty effects and effects whose every candidate is empty return false; mixed empty/renderable candidates return true. Native decoded zero-pass, ordinary, empty and mixed fixtures pass.

Confirmed replacement behavior: the adapter omits primitives whose effect has no render passes, before MAIN material requirements. Other unsupported materials retain diagnosed fallback behavior. Each uploaded surface retains its original primitive index; readback compares that indexed primitive's positions, normals, UV and reflected winding. Mapping requires ascending unique in-range indices. Geometry graph verification uses the adapter's mapped check rather than rechecking reduced surfaces against a full primitive count.

Original headless/GPU host gates pass. Three selected room templates now contain 42 textured surfaces, two omitted surfaces, zero neutral fallbacks, 26 shared GPU textures and zero material diagnostics. The inspected capture retains the textured corridor. A clearly synthetic variant of the first original decoded mesh assigns zero passes to every effect candidate: no surfaces upload, all original primitives are counted omitted, no neutral fallbacks appear and readback remains valid. Native smoke suite and Godot build pass.

Confidence: Confirmed raw effect structure and bounded rendering policy. This is not proof of legacy collision selection, visibility traversal or capability choice. No obstacle collision is implemented by hiding these surfaces; decoded original geometry remains intact and available to other consumers.

## Consequence and next action

Rendering no longer exposes no-pass collision helpers as visible fallback geometry. Next: add bounded portal-cell visibility selection for the current scene, using existing portal topology while preserving room/door ownership and rendering diagnostics.
