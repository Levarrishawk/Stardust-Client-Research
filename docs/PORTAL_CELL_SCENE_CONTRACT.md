# Portal cell scene contract

The offline `PortalSceneBundleReader` consumes a pinned [portal layout](PORTAL_LAYOUT_DECODER_CONTRACT.md) and loads nonempty authored cell appearance paths through the shared engine-neutral geometry graph. CELS ordinal keys remain zero based. Cell appearance vertices are already in the layout coordinate frame; this composition adds identity transforms only.

## Ownership, budgets and omissions

One owned lookup cache includes POB, redirects and every attempted appearance dependency. Defaults are 256 attempted resources, 128 MiB encoded source bytes, graph depth 32 and 4,096 edges. Root identity mismatch is fatal. Missing/unsupported cell appearances are explicit omissions with underlying parse diagnostics. Aggregate failures are fatal. LoadMany also bounds ordered root requests by the configured resource count; repeated requests share immutable templates but separate placements. Buffers/caches may coexist during parsing: the encoded budget is not a peak-memory guarantee.

The synthetic composite root is adapter input and has no serialized family interpretation. Floors, door transforms, cell lighting, navigation, portal visibility and original creation lifecycle remain separate.

## Vertex extension and host boundary

Selected VTXA0003 declaration `0x110D` stores position/normal, an owned packed U32 at byte 24, and UV at byte 28 (36-byte stride). Bit-3 consumption is confirmed in original loader 00784D90; exact color channel/material interpretation is not claimed. Verification hashes include its raw word and preserve all prior declaration hashes. The neutral host does not translate that word into original lighting.

Godot `--asset-interior` supplies a named-cell selector and independent cell node visibility, with shared meshes, per-cell reframing and optional camera/PIVT LOD. Manual selection does not implement DPVS or playable interior traversal. Headless and GPU checks exercise every loaded cell. [Original eight-cell evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_CELL_SCENE.md).

## Reproduction

Build/run the native smoke project and run `verify_portal_scene.py ROOT CONFIG MEMBER SHA CORE_DLL`. The original YT-1300 identity is in the checkpoint. `Invoke-StardustAssetPreview.ps1 -Interior -Cell 1` selects its hall; use `-Headless` for the bounded upload/selection gate or a fresh ignored private capture path for GPU verification. No original executable or server is contacted.


Optional floor resources and inspection now follow the [surface query contract](FLOOR_SURFACE_QUERY_CONTRACT.md). They share the scene source budget; floor walking remains deferred.

Shared material bundles and explicit unlit MAIN inspection follow the [textured scene contract](TEXTURED_PORTAL_SCENE_CONTRACT.md).

## Selected appearance cells

`PortalSceneBundleReader.Load(..., selectedAppearanceCells: cells)` snapshots a unique nonnegative cell set before lookup, rejects invalid/out-of-range indices, and preserves authored CELS ordinals. Null selects all appearances; empty selects none. Floor/path loading flags remain independent and still apply to the full layout. The independent verifier accepts `--appearance-cells` for neutral geometry. [Selected-room acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md) covers mutation isolation, empty/invalid selections and original geometry hashes.

`TexturedPortalSceneReader` accepts the same selected appearance set. The independent verifier now supports selection with texture, floor and path flags; floor/path traversal remains across the full layout. [Selected texture acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_TEXTURES.md) records the different source-accounting boundaries.
