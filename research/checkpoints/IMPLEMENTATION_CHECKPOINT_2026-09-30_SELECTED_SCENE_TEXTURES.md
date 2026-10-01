# 2026-09-30 selected scene MAIN textures

## Question and inputs

Can the existing texture/material pipeline consume the same three [selected Mustafar rooms](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md), with preserved effect records and explicit renderer fallbacks? Reuse those exact layout/specification/configuration pins. Stop at independent geometry/effect/DDS comparisons and original headless/GPU host gates. No new POB survey.

## Findings and evidence

The first integration attempt correctly fell back all three meshes because `c_emismap_bloom.eft` uses inline IMPL/0006 and `c_specmap_emismap_bloom.eft` uses IMPL/0007. Targeted effective-source inspection confirms seven-byte DATA and existing SCAP/optional OPTN/DATA/counted PASS grammar. Their PASS records use already-supported 0009. Prior [shader-version boundary](ANALYSIS_CHECKPOINT_2026-09-12_SHADER_VERSION_BOUNDARY.md) also records the seven-byte 0007 envelope and legacy dispatcher ceiling. No external-reference implementation was required.

`StaticEffectDecoder` now preserves inline 0006/0007 versions, ordered candidates, phase, both raw flag bytes and existing pass fields. Native asymmetric fixtures and every truncation reject; 0008/0009 implementation and PASS/0010 remain rejected. This establishes raw structure, without asserting equal flag semantics or selecting the original hardware branch.

`TexturedPortalSceneReader` forwards the selected appearance set into the existing loader. Empty selection performs no material reads; selected synthetic sharing/read-count gates pass. `verify_portal_scene.py --appearance-cells 5,6,7 --textures` independently matches three textured mesh bundles, 26 distinct DDS textures / 251 mip levels and 25 effect records, plus unchanged 72,607 vertices / 42,986 triangles. The verifier additionally loads the full layout's 31 floors / 32 path graphs; selection still applies only to appearances. Its total 84 sources differs from the host's 53 because the host does not reload those floor/path resources through the appearance pipeline.

`-SceneTextures` requires `-SceneGeometry` and uses explicit MAIN-only inspection. Original headless/GPU gates pass selected room owner placement, camera view state preservation, existing multi-door input/timing and optional guarded close. Upload readback remains enabled through the established adapter. The inspected image shows textured corridor surfaces. Host: three textured templates, 42 textured surfaces, two neutral fallbacks, 26 shared GPU textures. Both fallbacks are `mining_outpost_collide.sht`, whose effect has zero passes and no MAIN texture.

Confidence: Confirmed decoder structure, original source comparisons and replacement upload/rendering. MAIN inspection omits specular/emissive programs, lighting and original capability selection. Room geometry supplies no obstacle collision. No research-score change.

## Next action

Define zero-pass rendering policy from the decoded effect rather than shader names; verify zero, mixed and ordinary pass cases and omit collision-only surfaces without losing original primitive identity in upload verification.
