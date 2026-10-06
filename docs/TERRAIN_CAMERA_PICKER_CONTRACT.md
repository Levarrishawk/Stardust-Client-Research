# Owned terrain camera picker

[TerrainCameraPicker](../poc/Stardust.Godot/TerrainCameraPicker.cs) adapts an actual live active Godot Camera3D to an actual frozen [retained terrain ray query](TERRAIN_MODERN_PATCH_SET_RAY_QUERY_CONTRACT.md). It borrows the exact camera, same-viewport live Node3D parent and query. It builds no terrain/query geometry and owns only one lazy diagnostic marker. This is modern replacement picking, with `OriginalPickingParity == false`.

## Stable API

```csharp
TerrainCameraPicker(Camera3D camera, Node3D parent,
    TerrainModernPatchSetRayQuery query,
    int maximumAttempts = 256, ulong revisionSeed = 0)
TerrainCameraPickReceipt Pick(Vector2 pixel, double maximumT,
    CancellationToken cancellation = default)
bool HandleInput(InputEvent input, double maximumT,
    out TerrainCameraPickReceipt? receipt,
    CancellationToken cancellation = default)
bool IsCurrent(TerrainCameraPickReceipt? receipt)
void Dispose()
```

`Query`, `Current`, `SelectedHit`, `Attempts`, `Revision`, `Marker`, `MarkerBuilds`, `OwnedResourceCount` and `IsDisposed` expose explicit state. Constructor, picks, input handling, currency checks and disposal require the constructing managed thread and Godot main thread. Inspection and use of exposed Godot objects also belong on that thread. `Marker` is an inspection reference to owned state; external destruction/reparenting invalidates admission/currency.

## Camera and coordinate policy

Only active perspective and orthographic cameras in the exact parent's viewport are supported. Freed, queued, detached, inactive or unsupported-projection cameras/parents reject before query. Each pick requires a finite pixel inside the actual visible rectangle: `0 <= X < width`, `0 <= Y < height`. The configured window size is not substituted; headless and GPU viewports can differ. `maximumT` is finite in [0,1e9], additionally subject to the actual retained query's limits.

`ProjectRayOrigin(pixel)` and `ProjectRayNormal(pixel)` run on the borrowed actual camera. Origin/direction must be finite and direction nonzero. X/Y are copied and Z is reflected **once** into native space. The returned Godot direction is not renormalized by the adapter. The retained query receives the explicit closed interval [0, maximumT], preserving its binary64 nearest-T and computed-tie policy. This queries supplied terrain only; it does not establish occlusion against other objects, engine physics masks, UI hit testing or original collision/picking parity.

The immutable receipt captures exact picker/camera/query references, camera instance ID, pixel, actual viewport size, actual engine origin/direction, native ray, actual nullable hit, successful revision and charged attempt. Hit source/chunk/patch/geometry/child-query references are preserved. A marker position reflects the native hit Z back once into engine-world space; the marker uses `TopLevel` so the borrowed parent's transform does not apply a second transform. Query geometry is assumed to describe that native world coordinate space; arbitrary terrain placement is outside this adapter.

## Currency, limits and publication

`IsCurrent` accepts only the exact currently published receipt from this picker and actual camera/query. It returns false safely for null, disposal, invalidated camera/parent, changed active camera/viewport, unsupported projection or invalidated marker ownership. Reinstating the same live active camera can restore currency of the retained receipt. Currency means current selection publication; it does not claim the captured historical ray is a fresh projection after camera pose/viewport changes. A later successful hit **or miss** replaces the receipt and stales the previous one.

The constructor cap is 1..256. The cap and checked next U64 revision precede the query and receipt/marker allocation. Invalid input, invalid borrowed state, revision overflow and precancellation do not charge an attempt. Immediately before the actual query, an attempt is charged; later query failure/cancellation retains that charge, preventing repeated failed work from bypassing the cap. Revision/receipt change only on successful publication.

A hit creates at most one lazy MeshInstance3D/SphereMesh/StandardMaterial3D trio. The modern marker is an unshaded magenta sphere of radius0.08 and height0.16. Repeated hits reuse those exact objects. A miss publishes a nullable-hit receipt, clears `SelectedHit` and hides an existing marker without rebuilding or freeing it. Input/native rejection and observed cancellation before marker mutation preserve the previous receipt, revision, marker position and visibility. Callback-driven rejection during a reused-marker update can leave a physical marker mutation prefix, as described below. A successful left-button event performs one pick; right/wheel/released/motion/other events return false with null output and no query/attempt or selection change. The caller owns event delivery and marking input handled.

Cancellation is checked before projection/query, by the frozen query between triangles, after query, and before marker publication. Newly staged unpublished marker resources are released if staging fails or cancellation is observed. Godot AddChild and property setters can synchronously emit user callbacks. A busy/reentry guard rejects nested Pick/Dispose and records even caught nested exceptions. Borrowed state, caught reentry and exact staged-node/resource ownership are checked after AddChild and property callbacks before transfer/publication. The real ChildEnteredTree test attempts both nested operations, catches them and confirms that the outer pick rejects with all staged resources drained. A rejected reused-marker property update can have changed position/visibility before its callback; Current/revision remain unchanged, but that physical prefix is not rolled back. Catastrophic engine/allocation/property failures are also not proven rollback transactions. Cancellation arriving after the final token check can produce a successful result under cooperative cancellation semantics.

`Dispose` is owner-thread-only and idempotent. It detaches/frees only the owned marker node and releases its mesh/material, clears selection and retains borrowed camera/parent/query ownership. It also drains retained resources if a borrowed parent was already freed. Disposal becomes terminal before releasing resources, rejects nested mutation callbacks, and remains closed after a reported caught disposal reentry. All three owned resource counts become zero; historical receipts remain data but cannot be current after disposal.

## Evidence and portable replay

The [standalone verification node](../poc/Stardust.Godot/TerrainCameraPickerVerification.cs) and [own runner](../poc/round65-tests/Rendering/Invoke-Picker.ps1) stage/build a private project with frozen Core/terrain-visual dependencies and cached Godot4.7.2 packages. No shared project or host source is edited. [Independent verification](../poc/round65-tests/Rendering/verify_picker.py) constructs synthetic geometry/normals before execution, then uses exact Fraction plane/3D Gram barycentric intersection on captured actual camera rays. Separate mathematical projection and viewport/Z-reflection checks use captured camera basis/FOV/size/near values; native hits never seed expectations.

Both headless and actual GPU tests cover perspective/orthographic center/corners, oblique/sloped hits, explicit miss, exact shared-edge earlier-chunk selection, repeat/reuse, real Godot mouse event objects, invalid input, caps/revision overflow, main-thread rejection, active-camera switching/restoration, freed camera/parent, precancellation, timed charged late cancellation and borrowed survival/cleanup. GPU readback verifies a magenta marker at the selected center and saves a private inspected capture. Full results and exact next probe are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R65_TERRAIN_CAMERA_PICKER.md). Timed cancellation establishes observed state preservation after charge, not a deterministic exact cancellation instruction. No original executable is run.

## Round66 non-mutating admission

`ValidateAdmission(Vector2 pixel, double maximumT, CancellationToken cancellation = default)` shares actual picker preflight without querying, charging attempts, allocating a marker or publishing a receipt. It checks live owners, pixel/range, remaining attempt/revision budget and cancellation through the existing reentry gate. The [input adapter](TERRAIN_POINT_TARGET_INPUT_CONTRACT.md) composes it with current controller admission. Frozen65 picker headless/GPU regression passes; two borrowed mutations still have explicit partial-effect boundaries.
