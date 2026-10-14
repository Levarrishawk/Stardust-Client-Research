# 2026-09-30 original occupied close and host

## Question and identity

Does the optional closing sweep preserve occupied original-door clearance, deny entry and allow retreat in both native and rendered hosts? Close after independent guarded/default traces and original headless/GPU controls. The unchanged [scene pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md) select Mustafar 6/1 ↔ 7/0 with the same two-leaf scene specification SHA-256 `12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA`.

Tools: core original closing producer, [independent original closing verifier](../../../SWG-Godot-Client/tools/python/verify_original_closing_scene.py), Godot 4.7.2 .NET x64 and bounded preview launcher. Detailed original geometry/records and GPU capture remain private.

## Confirmed observations

Actor approaches an already open second leaf, stays on its supported source floor just outside the permission barrier, then entry is denied while closure advances. The independent original plane/easing/slab/controller reference matches **692 ticks over two owner poses and both policy modes**. Guarded: 346 ticks, 156 closing holds, zero physical-contact stops; unguarded prototype: 346 ticks, 314 physical overlap stops. Guarded retreat releases closure and completes it. Both retain source ownership with zero transfers. This comparison describes replacement policies; original game obstruction handling remains Unknown.

The host exposes `-HoldClosingDoors` only with an explicit scene specification. Headless/GPU guarded runs pass two-pose occupied hold, actual denied-entry barrier, retreat and completed release, plus the existing 16 manual transfers, physical timestamped keys/diagonal round trips, typed four-transfer route, stall/pause/focus and all-leaf placement. The inspected capture shows second fraction 0.600 waiting, entry denied, actor owned by connecting_hall and first leaf closed. No controller pulse opens a denied door.

Confidence: Confirmed bounded modern policy on a representative original leaf. Narrow-phase original mesh collision, opening sweeps, rotating leaves and original global ordering remain Unknown. Core default remains unchanged.

## Next action

Use existing cell appearance/geometry loading to render the selected original rooms in the persistent scene. Verify owner alignment and visibility without introducing another POB survey or claiming world obstacle collision.
