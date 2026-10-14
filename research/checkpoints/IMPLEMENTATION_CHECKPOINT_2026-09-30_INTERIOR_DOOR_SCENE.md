# 2026-09-30 persistent multi-door scene

## Question and synthetic evidence

Can one engine-neutral actor persist through three loaded cells and two owned physical leaves without duplicate controllers, stale retention or inconsistent permissions? Close after repeated ownership and failure/lifecycle gates and independent tick comparison.

Inputs are explicitly synthetic: three flat rectangles, portal planes at X=0 and X=4, two normal leaves using the existing synthetic style, one actor with half bounds .25/.9/.25. No original executable or proprietary asset is required. Tools: native core smoke and [independent verifier](../../../SWG-Godot-Client/tools/python/verify_interior_door_scene.py); detailed generated records stay private.

## Confirmed results

Native gates pass 80 repeated transfers over two owner poses, then denied closed-leaf ownership/retreat, denied open-leaf and unknown directional barriers, stale retention after leaving incident cells, permission-callback failure before mutation, rejected reentrant reset, complete reset/dispose, missing binding and duplicate physical doorway rejection. Fixture debugging corrected an invalid starting triangle and explicitly marked its interior tessellation edges passable; no runtime tolerance was relaxed.

Independent reference matches **1,440 ticks / 24 transfers / 128 blocked ticks** over two owner poses, with exact owner/status/contact/transfer/controller-event/retained-count comparison, 8e-5 m position and 5e-5 parameter budgets. The oracle independently computes synthetic proximity, pulses, easing, oriented-box intervals and three-cell plane ownership; no native controller or trace is consumed.

Confidence: Confirmed modern bounded scene. Original global update order and broad world obstacle collision remain Unknown. Construction and failure semantics are consolidated in the [durable contract](../../docs/INTERIOR_DOOR_SCENE_CONTRACT.md). No new executable names, packet rows or research scores.

## Next probe

Use the previously validated Mustafar mining POB route 5/1 → 6/0, then 6/1 → 7/0. This supplies a flat shared corridor and two explicitly pinned leaves from one already studied POB. Verify the route independently before host adaptation; no broader POB survey is needed.
