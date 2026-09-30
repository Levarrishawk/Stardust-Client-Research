# Textured portal scene — 2026-09-30

## Bounded question and identity

Can existing shared cell geometry gain bounded material/texture bundles with independent source/pixel agreement and honest unsupported branches? Closed for SSHT and baseline IMPL0004/0005, PASS0008/0009; CSHD customization is the next separate probe.

Original executable SHA rechecked: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 TREE0005 mounts. Pinned `appearance/yt1300.pob` SHA `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`, priority52/9,455 bytes. Only static original reads and owned offline native/Godot execution occurred.

## Evidence and conclusions

**Observation, confirmed:** older effects `effect/c_emismap.eft` SHA `448C0D0F2689312FD3A09541C5FD0E172356F9B7B1F6C09DE0DE42319FA09C5C` and `effect/c_specmap.eft` SHA `747DE4EC25DC5E65C2EC102F902A2EC3A791561090FFCCFD45231E7AED16D079` carry IMPL0004 DATA5/PASS0008 DATA51. Direct dispatcher 0075CD00 selects version 4 loader 0075D730, which reads count/phase and computes runtime flags after load. PASS0008 loader 007613C0 omits offsets4D/64/68/6C/70 compared to version 9 loader007619D0. **Consequence:** native versions and absent fields remain explicit; capability/default/runtime-state selection is deferred.

**Observation, confirmed:** independent raw TRE names, source hashes, geometry/floor/graph checks, effect fields and Pillow pixel hashes agree on the selected complete bundle. Eight cells/11 meshes/66,617 vertices/55,916 triangles/310 sorted variants remain intact. The textured bundle has 77 present sources, seven successful material meshes, 23 textures/221 mips and four CSHD exterior omissions. A failed material bundle can contribute attempted sources without becoming renderable.

**Observation, confirmed:** actual GPU MAIN inspection verifies 23 shared texture uploads/all mip bytes and yields seven textured/four neutral templates, 78 textured/ten neutral surfaces. The hall capture was inspected visually. Floors, routes and every cell selector remain active. Strict headless inspection deliberately retains neutral surfaces for precalculated lighting; the opt-in unlit MAIN mode omits that lighting. This is an inspection adapter, not recovered original material rendering.

## Validation and closure

Native ownership, repeated source lookup, aggregate encoded/decoded budgets, missing texture isolation and complete truncation fixtures pass. Asymmetric PASS0008/0009 fixtures prove field offsets. `verify_portal_scene.py --textures` independently agrees on all sources, 221 mips and 18 implementation/pass records plus seven floors/eight graphs. Godot builds without warnings/errors; bounded strict headless and hidden GPU MAIN capture pass. The existing certificate-store startup error remains nonfatal and unrelated to asset loading; process exits0. All original-derived captures/output stay ignored/private. Workspace link/registry validation and focused diff checks follow this checkpoint.

Canonical [contract](../../docs/TEXTURED_PORTAL_SCENE_CONTRACT.md). Scores, packet counts and 2,590-name registry are unchanged. Next exact probe: CSHD0001 inline SSHT and TXTR/TFAC structure, original default/customization application and an explicitly limited default inspection path.
