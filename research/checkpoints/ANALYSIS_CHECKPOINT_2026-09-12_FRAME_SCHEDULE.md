# Frame schedule, IoWin composition, and raster presentation

Date: 2026-09-12

Scope: static analysis of `C:\Stardust-DEV\Stardust.exe` and configured `s207_r.dll` only.

## Findings

### Confirmed outer frame order

`frame_tick_VA_004237C0` completes platform/event, network, simulation, scene and texture-renderer updates before entering a quit-guarded graphics section. That section sets a graphics-in-progress flag, advances renderer frame state, begins the D3D scene, updates a frame/sorter counter, invokes the active IoWin render method, clears the guard, ends the D3D scene, and presents. Resource/async and platform/input tail service occur after presentation.

### Confirmed raster slot mapping

`GetApi` returns base `0x62B835B8`, resolving a previous possible off-by-eight ambiguity. Main API offsets `+0x80/+0x84/+0x88/+0x8C/+0x90` map respectively to s207 `0x62A03D70`, label entry `0x62A03DC0`, label entry `0x62A03E30`, label entry `0x62A04010`, and `0x62A04040`.

- `0x62A03DC0` calls `IDirect3DDevice9::BeginScene` and resets backend per-frame caches.
- `0x62A03E30` calls `EndScene`.
- `0x62A03E60`, reached by both presentation wrappers, calls `Present` and owns cooperative-level/device-reset recovery.
- Recovery invokes `0x62A03520` before reset and `0x62A03560` after reset, connecting the full end-of-frame path to the resource lifecycle documented previously.

### Confirmed composition owner

The outer frame renders through `IoWinManager`, not a hardcoded scene/UI call. Its head is a linked stack; virtual slot `+4` handles events and slot `+8` renders. Confirmed pushed window families include GroundScene, CuiManager, HyperspaceIoWin and SpaceDeath. This architecture allows scene, UI, transitions and modal overlays to replace or layer frame behavior.

GroundScene's concrete render virtual is `0x0051B770`. It configures camera and clear state, renders the world path, and handles alternate/multiple cameras. Exact nested world/UI/post-processing call labels remain incomplete.

### Confirmed semantic render phases

The ShaderPrimitiveSorter installs 12 ordered phase records and tag mappings: ENV, TERR, MAIN, BLOB, PREA, ALPH/BLND, WBLW, WATR, WABV, PSTA, SHDW and LAST. Begin-environment `0x00773B10` pushes camera/environment state and initializes phases; end-environment `0x00773DE0` closes/flushed phases and restores the previous state. Dynamic `ENVM` is rebound at the boundary.

## Strongly inferred

- The phase index order is the intended coarse render order, supported by installation sequence, tag semantics and begin/end iteration.
- The two configured per-phase small integers select sorting/queue policy. Their exact enum names and algorithms are not yet assigned.
- IoWin supplies the top-level composition policy while GroundScene and CUI own nested world/UI ordering; it should become explicit layers in a replacement.

## Unknown

- Exact UI draw placement relative to post-alpha/final phases.
- Exact bloom/post-processing invocation point inside GroundScene or subordinate renderers.
- Complete primitive comparison keys and forward/reverse rules for all 12 phases.
- Meaning of renderer frame constant tag `0x30` beyond its first component being accumulated raster time.
- Complete behavior of alternate present destination parameters.

## Replacement model

```text
update platform/network/simulation/scene
  -> acquire frame
  -> prepare camera + dynamic environment resources
  -> world semantic phases
  -> water/alpha/shadow/final phases
  -> post-processing (placement pending)
  -> UI/overlay layers (placement pending)
  -> submit/present
  -> asynchronous/resource tail service
```

Do not reproduce the IoWin linked list or D3D9 BeginScene state machine literally. Preserve deterministic ordering, semantic phase constraints, transient overlay behavior, and explicit loss/recreation ownership.

## Progress

- Comprehensive whole-client ledger: approximately **53.3%**, reported as **53%**.
- Replacement-relevant research: approximately **74%**.
- Renderer workstream: **50%**.

The next renderer block should descend into GroundScene `0x0051B770` callees to place world, post-processing and CUI precisely within this confirmed outer envelope.

