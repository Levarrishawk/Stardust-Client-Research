# 2026-09-30 consolidated interior scene content

## Question, identity and source boundary

Can actor/floor/door and selected appearance/material loading share one bounded source lifetime and identity record? Stop at original exact source union, one external read per asset, malformed/resource negatives and native/host regression. Reuse [selected scene pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md). Tools: existing independent portal/material verifier, original scene verifier, .NET smoke suite and Godot bounded launcher.

## Collision input observation

Before inventing a collision decoder, the selected cells 5/6/7 were inspected: all CELL bounds are NULL; all three room MESH/APPR collision slots are NULL. Their loaded FLOR files contain 76 triangles: 56 upward planar and 20 sloped, with no vertical or downward-facing triangles. Those source aggregates are Confirmed; they do not establish absent world collision in the original runtime. Full wall/ceiling behavior cannot be claimed from this loaded floor data. No visual-mesh collision policy was introduced.

## Confirmed implementation/evidence

[Combined reader](../../docs/INTERIOR_SCENE_CONTENT_CONTRACT.md) keeps engine-neutral scene/room/material results behind one owned source cache and global source budget. It retains the actor specification's normalized layout path and rejects substitution with an unrelated loaded asset even when the substituted hash is correct. Material options without appearances and malformed pinned specifications reject before lookup; count/byte budget negatives pass.

`verify_portal_scene.py --appearance-cells 5,6,7 --materials-only --source-manifest PRIVATE_JSON` independently checks 53 appearance/material sources without unrelated full-layout floor/path loading. `verify_original_interior_scene.py --content-bundle --appearance-source-manifest PRIVATE_JSON` reconciles their exact identity/priority/size union with independent door/floor sources: **61 unique assets / 9,262,168 bytes**, all external resolutions once. Original independent 2,880 ticks / 24 transfers / 172 blocked / zero steps remain unchanged. The manifest is regenerable private evidence, not a curated corpus.

Native smoke/build and original headless/GPU host pass selected geometry/textures, no-pass omission, both owner poses, mouse/view/visibility, timed physical input and guarded closing. Capture inspected. Confidence: Confirmed owned source composition and bounded replacement behavior. Decoded-pixel, source and GPU budgets are separate. Original collision architecture and physical OS cursor grabbing remain unresolved.

## Next action

Share immutable leaf geometry/material/GPU resources across independent door instances and rooms while retaining separate transforms, camera LOD and controller ownership.
