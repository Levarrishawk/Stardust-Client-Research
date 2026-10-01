# 2026-09-30 portal room visibility

## Question and inputs

Can a bounded replacement query select the current scene's rooms through authored portal triangles while preserving actor ownership, transforms and conservative fallback? Stop at native synthetic geometry/negative cases, independent representative original rays and headless/GPU node selection. Reuse the exact [selected-scene layout/configuration/specification pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md). No executable VA claims or new corpus survey.

## Evidence and interpretation

Confirmed modern implementation: [query contract](../../docs/PORTAL_VISIBILITY_CONTRACT.md) clips triangle apertures and propagates plane sets along simple cell paths. It preserves alternative paths and conservatively returns all selected cells on bounded exhaustion. Native tests pass aligned/offset openings, behind/side/reverse views, portal-plane eye, cycles, selection and geometry mutation isolation, state/plane fallback and malformed views/indices.

`verify_portal_visibility.py` independently reads the pinned POB and uses ray/triangle intersections with barycentric portal samples. Six views (forward/reverse from each of cells 5, 6 and 7) agree on visible-cell unions across 384 sampled rays. Detailed native records remain private. This is bounded sample evidence, not an exhaustive independent proof of clipping under every camera pose.

Original Godot headless/GPU controls pass with `-PortalVisibility`. Both owner poses check three visible rooms from initial cell5 and only cell7 from the forward-looking endpoint. Camera and visibility toggles preserve actor/controller state. Original typed scheduling, physical keys, denied entry, retreat and guarded closing remain passing. The inspected capture shows two visible rooms from cell6 with denied second entry and fraction 0.600 held; first-room geometry is excluded. Texture counts remain 42 rendered / two no-pass omitted surfaces, zero material diagnostics.

Confidence: Confirmed bounded replacement query and representative source/host behavior. Legacy DPVS semantics, door occlusion, solid/winding flag visibility semantics, exterior traversal and world mesh collision remain Unknown or unimplemented. Visibility does not change logical cells or asset lifetime. Research metrics unchanged.

## Next probe

Add bounded view orientation and mouse-look event handling so movement and visibility use the current camera heading rather than a fixed route heading. Retain timestamped input, owner conversion and focus/pause cleanup.
