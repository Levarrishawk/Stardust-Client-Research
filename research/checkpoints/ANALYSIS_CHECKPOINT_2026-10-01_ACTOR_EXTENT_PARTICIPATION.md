# October 1, 2026 — Actor extent participation

## Question and stopping condition

Does the inspected original grounded floor/body path directly consume the shader-selected mesh triangles used by the new conservative box guard? Stop after resolving its immediate query inputs, shape dispatch and floor/contact combination; do not infer controller-wide equivalence.

## Inputs and method

Original executable SHA-256 rechecked as `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Existing Ghidra project processed read-only with no analysis or original execution. Reused `DecompileAddressList.java`, `find_call_xrefs.py`, `disasm_range.py` and `pe_x86_probe.py`. Detailed decompilation, instruction checks and RTTI bytes remain ignored private evidence. Public descriptions are independently authored.

Prior [full original geometry probe](IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPLETE_ROOM_TRIANGLES.md) exposed rendered0.1 m ramps above flat FLOR. That concrete discrepancy motivated this static question; no additional POB survey occurred.

## Findings and confidence

The [durable specification](../../docs/GROUNDED_ACTOR_COLLISION_PARTICIPATION.md) records exact addresses, observations and consequences. Confirmed: `012A09C0` returns refreshed property `+B4`; `012A0AB0` supplies paired transformed extents to `012A12D0`; outer type9 is RTTI `SimpleExtent`, containing its own shape discriminator. `01260E40` owns local/transformed extent lifecycle and invalidates on scale change. `01267A90` combines separately obtained body and floor results. `01266960` has conditional extent-based preflight before floor movement.

The inspected chain contains no direct shader-primitive intersection call. This is a local control-flow fact, not a claim that every original actor path avoids triangle collision. Shape-source getter semantics, controller reachability, all candidate flags and global ordering remain Unknown. Selector1 setters found in other inspected segment queries do not establish grounded actor authority.

## Compatibility consequence

Retain all42,986 original triangles and the validated conservative probe, but do not enable it by default as an original actor reconstruction. Floor navigation, object collision extents and appearance segment intersection are distinct inputs. No clearance increase or arbitrary floor-triangle filtering is introduced.

## Validation and next probe

Direct instruction checks confirm accessor fields, pair dispatch and floor-query call/selection branches. RTTI bytes confirm the type9 name. Workspace validator reports859 Markdown files, zero broken/private links,2,599 canonical names/zero duplicates and345 packet rows; `git diff --check` passes before this documentation addition. Re-run the documentation gate after promotion. No new names or coverage score increase.

Next: resolve extent source selection in `01260E40` and APPR/CELL loaders against the pinned room/door assets, then establish the movement caller's active shape and ordering.
