# Game Loop and Frame Architecture

## Confirmed entry and loop

`app_entry_VA_00401050` performs configuration and subsystem installation, then registers `game_loop_VA_00423780` through the platform/application framework. `0x00423780` initializes loop state and repeatedly calls `frame_tick_VA_004237C0` until `quit_requested_VA_00423750` becomes true.

## Per-frame sequence

```text
frame_tick_VA_004237C0
|-- diagnostics, frame delta and allocation history
|-- pump OS/application events; return early if platform says no frame
|-- active-window input update
|-- platform window update
|-- periodic network dispatch/service
|-- ordered simulation/subsystem dt updates
|-- scene/session notification when selected graphics state changes
|-- optional CUI update
|-- scene/camera and texture-renderer updates
|-- if not quitting:
|   |-- set graphics-in-progress guard
|   |-- renderer per-frame constants/counters
|   |-- IDirect3DDevice9::BeginScene
|   |-- frame/sorter counter hook
|   |-- IoWin stack top render virtual
|   |-- clear graphics-in-progress guard
|   |-- IDirect3DDevice9::EndScene
|   `-- Present(default destination or supplied rectangle/window)
|-- resource/async service
|-- platform/input tail
`-- increment global frame count and accumulated time
```

This outer order is Confirmed. The individual simulation callees preceding rendering remain only partly named.

## Raster API frame slots

`s207_r.dll!GetApi` returns table base `0x62B835B8`. This fixes the executable wrapper mapping:

| API offset | Main wrapper | s207 entry | Confirmed behavior |
|---:|---:|---:|---|
| `+0x80` | `0x00755700` | `0x62A03D70` | Advances frame time/count and uploads a four-float constant under tag `0x30`. |
| `+0x84` | `0x00755730` | `0x62A03DC0` | Device-state check followed by `IDirect3DDevice9::BeginScene`; resets per-frame backend caches. |
| `+0x88` | `0x00755740` | `0x62A03E30` | `IDirect3DDevice9::EndScene`. |
| `+0x8C` | `0x00755800` | `0x62A04010` | Default presentation parameters forwarded to common presenter. |
| `+0x90` | `0x00755810` | `0x62A04040` | Caller-supplied presentation destination forwarded to common presenter. |

Common presenter `0x62A03E60` calls `IDirect3DDevice9::Present`. On device-lost/not-reset results it tests cooperative level, calls the pre-reset release path, resets the device with retained presentation parameters (including bounded retry for a still-lost device), and calls the post-reset reconstruction path. This directly connects the prior resource-lifecycle findings to the end of every rendered frame.

## IoWin composition boundary

The frame does not call `GroundScene` or CUI directly. `0x00AB54F0` invokes virtual slot `+8` on the head of an `IoWinManager` linked stack (`DAT_01945934`). `0x00AB5500` pushes a window; `0x00AB55A0` removes one; `0x00AB5310` routes queued input/events down the same linked stack until consumed. Confirmed stack participants include `GroundScene`, `CuiManager`, `HyperspaceIoWin`, `SpaceDeath`, and other transition/debug windows.

The GroundScene vtable is `0x015DD988`; slot `+8` is `0x0051B770`. Its render method configures cameras/view state, clear color/depth behavior, runs the terrain/world appearance path and restores multi-camera state.

The normal nested composition is now bounded. `CuiIoWin` render `0x0093B2B0` begins bloom capture, invokes `0x00AB58E0` to render its next/lower IoWin, resolves bloom, runs GroundScene-only post rendering `0x0051BCE0`, then calls the root CUI render stage `0x00881210`. An optional full-screen overlay follows. Thus bloom affects the lower scene but not the UI. This order is Confirmed; several individual GroundScene post-pass component names remain open.

## ShaderPrimitiveSorter phases

Graphics installation creates twelve ordered phases and maps tags:

| Index | Tag | Semantic label |
|---:|---|---|
| 0 | `ENV ` | environment |
| 1 | `TERR` | terrain |
| 2 | `MAIN` | main opaque geometry |
| 3 | `BLOB` | blob/shadow-like geometry |
| 4 | `PREA` | pre-alpha |
| 5 | `ALPH`, `BLND` | alpha/blended |
| 6 | `WBLW` | below water |
| 7 | `WATR` | water |
| 8 | `WABV` | above water |
| 9 | `PSTA` | post-alpha |
| 10 | `SHDW` | shadow |
| 11 | `LAST` | final |

`0x00773B10` begins a sorter/camera environment: it pushes environment texture and camera state, updates the dynamic `ENVM` binding, initializes each phase, and resets the two primitive queues (precalculated-vertex-lighting versus normal). `0x00773DE0` flushes/closes each phase, restores prior environment/camera state and invalidates queued primitive phase assignments. Phase records are 0x24 bytes. Exact draw order inside each record and the meaning of its two configured small integers remain partly open.

## Replacement implications

- Retain deterministic simulation-before-render and presentation-after-composition ordering.
- Replace the mutable D3D9 state machine with an explicit frame graph, but preserve the semantic phase constraints required by assets and water/alpha ordering.
- Represent transient overlays/transitions as composable render layers rather than copying the IoWin linked-list implementation.
- Device loss must suspend submission, release/recreate backend resources through explicit owners, and resume only after reconstruction completes.
- Preserve world -> bloom -> GroundScene post overlays -> CUI -> final fade/overlay ordering. Individual subpasses can remain engine-native while this observable layering is retained.
