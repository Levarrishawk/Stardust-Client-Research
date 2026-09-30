# Authored customization texture inspection

## Explicit state policy

`CustomizableShaderTextureQuery.Select` uses an exact-case `(VariableName, Private)` key, where Private is raw flag!=0. Supplied integer values are snapshotted; missing keys use each record's **serialized authored default**. Per-operation values clamp to0..ChoiceCount-1 before indexing FirstChoice+value. Returned immutable records retain authored index, tag, requested/clamped values, selected path and whether an authored default was used. This is explicit inspection state, not a claim that original instance construction immediately applies authored defaults. Original instance zero initialization and later customization callbacks remain distinct.

Different ranges for the same variable remain valid and clamp independently. Original texture application0077BEE0 confirms this local clamping/indexing. Unknown override keys do not affect selection; namespaces and case never merge. Inputs and tables cap at 4,096 records. Out-of-range defaults remain intact in the decoder and clamp only during this query.

## Material attachment

`CustomizableShaderInspectionMode` defaults to Reject. Explicit AuthoredTextureDefaults retains the entire owned CSHD description, derives named static bindings by applying selections in authored order and loads only selected texture dependencies. Absent target tags do not create new bindings, following original target-membership checking. Inline effect/material/coordinate metadata remain owned and unchanged. The derived bindings are inspection state; they do not replace serialized metadata or encode a new original shader.

The same per-load source/decode caches and fatal aggregate budgets apply. Missing selected textures fail the material bundle with explicit neutral geometry fallback. Unselected texture choices and referenced palette assets are not eagerly loaded. Decoder-level palette controls remain intact; palette shading, material customization and dynamic variable updates are unimplemented.

Godot requires explicit MAIN-only inspection for customizable materials. The launcher flag `-AuthoredTextureDefaults` requires `-InteriorTextures -MainTextureOnly`; the footer identifies authored texture defaults and omitted palette/original lighting. Default strict behavior is unchanged.

## Independent and host verification

All5,173 shader descriptions still agree independently. Every856 authored texture selection is checked at its serialized default, Int32.MinValue and Int32.MaxValue (2,568 selected-operation cases), including exact path/index/state-provenance hashes. Synthetic exact-case, distinct private/shared namespaces, supplied-value ownership, negative default, clamping, strict rejection, missing selected texture and nonloading of unused choices pass. Two original shared variables have different per-operation ranges; neither is collapsed.

The original eight-cell YT-1300 now has **87 shared present sources,11 complete material mesh bundles,33 textures/321 mip levels,39 effect implementation/pass records and zero material-bundle omissions**. Independent source, geometry, floor, graph, effect-field and Pillow mip agreement passes. Actual GPU upload readback agrees for all 33 textures/mips; display has11 textured templates,85 textured surfaces and three neutral surfaces (one collision material/two nonopaque grates). All geometry/selection/floor/route gates remain valid. A hidden exterior capture was visually inspected and its labels fit. Headless MAIN-only gates pass separately.

## Reproduction and next action

Run `verify_customizable_shaders.py ROOT CONFIG PRIVATE_OUTPUT --core-dll DLL` and `verify_portal_scene.py ROOT CONFIG appearance/yt1300.pob SHA DLL --authored-defaults`. Native optional flag is `--portal-textures-defaults`. Build Godot and invoke the bounded launcher with Interior/InteriorTextures/MainTextureOnly/AuthoredTextureDefaults, selecting Cell0 for the exterior or Cell1 for the hall. Use a fresh ignored capture path for GPU verification.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_AUTHORED_TEXTURE_INSPECTION.md). Next: integrate separately verified typed BC6H RGB-half images into material bundles without coercing HDR pixels into RGBA8.
