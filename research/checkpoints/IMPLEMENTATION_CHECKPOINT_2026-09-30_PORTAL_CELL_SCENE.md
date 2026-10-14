# Portal cell scene checkpoint — 2026-09-30

## Question and stopping condition

Can one pinned original PRTO load its authored cell appearances under a shared aggregate source budget, independently agree on geometry, and expose manual cell selection in the offline prototype host host? Closed when native negative/ownership tests, independent original geometry agreement and host selection/upload gates pass.

## Inputs and evidence

Baseline executable SHA-256 remains `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before static vertex-loader inspection. Configuration SHA-256 remains `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` and mounts 48 TREE0005 archives. Original root `appearance/yt1300.pob`, priority 52, 9,455 bytes, SHA-256 `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`.

`vertex_array_load_v0003_VA_00784D90` reads position at 00784EA0, normal at 00784EF2, then declaration bit 3 at 00784F49 causes an exact four-byte read before texture coordinates at 00784F97. Effective interior primitives use declaration `0x110D` and 36-byte vertices. **Confirmed structure:** position/normal six floats, packed U32, UV two floats. **Interpretation:** primary packed color, strongly inferred from prior vertex-format research; channel interpretation and faithful material use remain open. The replacement preserves the U32 without pretending it is a float or changing UV offsets.

## Implementation and agreement

`AppearanceGeometryGraphReader.LoadMany` shares templates/sources across ordered roots and rechecks explicit pins even on cached roots. Tolerant missing/unsupported cell roots retain diagnostics; aggregate resource failures remain fatal. `PortalSceneBundleReader` includes the POB in the same aggregate resource/byte budget, owns resolver buffers, and composes loaded cell roots at identity transforms in authored CELS order. The synthetic composite is adapter input, not a recovered serialized appearance family.

Independent `tools/python/verify_portal_scene.py` agrees with native source identities, raw vertex/index geometry hashes and sorted-index hashes: **eight cells, 14 sources, 11 mesh templates, 66,617 vertices, 55,916 triangles, 310 sorted-index variants, zero omissions**. Counts include all loaded exterior LODs, not simultaneous visible geometry.

Prototype host `--asset-interior` reuses immutable GPU meshes with independent cell nodes, a named-cell selector, camera reframing and optional camera/PIVT LOD. All eight selections are asserted. Neutral preview preserves geometry; packed color material interpretation remains deferred. Original portal culling, floor behavior, doors and lighting are not implemented by this inspector.

## Validation and limitations

Native smoke passes owned source mutation, sharing, wrong root/repeated-root pins, missing-root isolation and fatal aggregate count/byte budgets. Colored vertices pass asymmetric U32/UV checks and every truncated fixture prefix. .NET builds pass without warnings. Original independent agreement passes. Headless host geometry/selection checks and hidden OpenGL GPU coordinate/readback/capture checks pass. Capture was visually inspected and remains private. Existing nonfatal prototype host certificate-store startup diagnostic persists.

Canonical [scene contract](../../docs/PORTAL_CELL_SCENE_CONTRACT.md). Next: bounded FLOR geometry reader, independently checked against effective versions, then modern floor queries with explicit behavior limits.
