# 2026-09-30 selected original room geometry

## Question, inputs and stopping condition

Can the established room appearance loader render only the current three-cell route, preserve authored ordinals and apply the shared owner pose alongside persistent doors and floor ownership? Stop after native selection negatives, independent geometry hashes and original headless/GPU placement and view controls.

Reuse the [Mustafar scene pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md) and two-door specification SHA-256 `12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA`. Selected CELS indices: 5, 6, 7. No new executable addresses or behavior claims. Tools: .NET smoke suite, `verify_portal_scene.py --appearance-cells 5,6,7`, Godot 4.7.2 .NET and bounded preview launcher.

## Evidence and interpretation

Confirmed: the independent source/mesh verifier matches all three selected appearance templates: 72,607 vertices / 42,986 triangles, four sources including the pinned POB, zero sorted variants. The POB still contains 31 cells; selection changes appearance loading only. Native tests check preserved authored child ordinal, resolver mutation isolation, empty selection and invalid duplicate/out-of-range sets. Null selection preserves the full-scene behavior; floor/path flags remain independent.

Confirmed replacement behavior: `-SceneGeometry` loads the bundle's floor-cell appearances with the existing neutral geometry adapter. Room root receives the same owner transform as the floor root. Both authored and rotated/translated poses pass the original multi-door headless/GPU controls and reflected room placement. Interior view hides the actor inspection box, while the floor inspection overlay is separately selectable. Switching views preserves actor/controller state. The inspected GPU capture shows original corridor geometry and the guarded second leaf at fraction 0.600 while entry remains denied.

Confidence: Confirmed selected geometry composition and bounded host behavior. Colors are neutral inspection materials. Appearance meshes supply rendering only; wall/ceiling collision, portal visibility, lighting and original camera behavior remain Unknown or unimplemented. No corpus-wide coverage increase follows from this representative route.

## Compatibility consequence and next probe

The reusable loader can select rooms without renumbering CELS or loading every appearance. Engine-neutral decoded geometry remains separate from Godot placement. Next: connect the established MAIN texture/material pipeline to this selected scene, with explicit diagnostics and resource limits.
