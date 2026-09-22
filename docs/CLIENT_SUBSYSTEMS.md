# Client Subsystems

## Server-driven UI (SUI) protocol

The client contains a retained server-page manager rather than a collection of packet-specific dialog handlers. `SuiCreatePageMessage` creates a page from an ASCII root-page asset key and an ordered command stream; `SuiUpdatePageMessage` applies later mutations to that retained page; `SuiForceClosePage` closes it by U32 page ID. The seven supported command operations clear data sources, add child widgets, set properties, add data items, subscribe to events, add data-source containers, and clear containers. A client UI event is matched to its subscription, selected widget values are encoded as Unicode strings, and `SuiEventNotification` is sent reliably to the server.

This boundary is suitable for a replacement architecture: keep the wire parser and SUI command interpreter engine-neutral, then provide an adapter over the chosen UI engine. The adapter must resolve original `rootPage` names and widget paths, so consuming the original UI template/assets remains part of the non-negotiable asset-compatibility work. Page updates must not reinstall event subscriptions; Stardust.exe explicitly suppresses type 5 during updates.

Detailed evidence and schemas are in `ANALYSIS_CHECKPOINT_2026-09-12_SUI_PAGE_PROTOCOL.md`.

| Subsystem | Evidence | Current model | Confidence | Priority |
|---|---|---|---|---|
| Lifecycle/platform | WinMain call chain, USER32 message APIs, install/remove strings | central app installs modules, enters run loop, reverses teardown | Strong | High |
| Configuration/preferences | `ConfigFile` exports; layered `.cfg`; registry imports; machine-options IFF | layered text config plus generated options and some registry state | Strong | High |
| Networking | Winsock imports; GameNetwork/Connection/NetworkHandler strings; server source | transport connection + named message layer + dispatch bus | Strong | Highest |
| Object model | ClientObject/CreatureObject/PlayerObject RTTI; templates | template-created polymorphic objects keyed by 64-bit network IDs | Strong | Highest after network |
| Replication | Baselines/Deltas/AutoDelta RTTI and server constructors | view-indexed baseline initialization and indexed delta mutation | Strong | Highest after network |
| Scene/world | Scene/NetworkScene/GroundScene, CmdStartScene strings | session switches scene using terrain path and player template | Strong | Medium |
| Renderer | graphics exports, recovered `GetApi` raster loader, `s205_r.dll` D3D9 imports, DPVS | higher-level render/resource model calls versioned raster API; D3D9 device lives in module | Confirmed boundary; API slots incomplete | Medium |
| Terrain | procedural-terrain RTTI, `.trn`, scene errors | terrain appearance/template drives ground scene | Strong | Later |
| Animation | animation TRE and CreatureObject animation/mood callbacks | skeletal animation driven by asset graphs and replicated state | Strong concept | Later |
| UI | CUI RTTI/vtables, declarative UI files, typed preferences, mediator/message callbacks | retained mediator hierarchy over declarative widgets; typed live-preview settings with transactional scalar rollback | Strong/Confirmed major boundaries | Later |
| Input | DirectInput8 import, keyboard/mouse/joystick/FF RTTI, action-map diagnostics | raw devices feed binding/action and ground/space mouse-mode layers | Confirmed raw layer; Strong action layer | Later |
| Camera | DPVS camera, ground-scene/camera strings | camera supplies cell/frustum and drives visibility | Strong concept | Later |
| Audio | Miles imports and detailed startup diagnostics | streamed 2D/3D audio with occlusion/reverb/listener | Confirmed | Later |
| Resources | TreeFile export, TRE priority config, CRC strings | layered virtual filesystem feeding typed managers | Confirmed | High |
| Memory | MemoryManager/MemoryBlockManager exports; named per-message pools | global/custom allocator plus fixed-size class pools | Confirmed | Redesign |
| Threading/jobs | Win32 thread/sync/TLS/IOCP imports; file-streamer and terrain-thread types | main owner plus bounded specialist workers: file streamer has three strict-priority FIFO queues and 128 KiB slices; terrain has ordered requests, completion vector and main-thread commit | Confirmed specialist topology; no general job system proven | Medium |
| Diagnostics | Report/Fatal/Warning/profiler/crash exports, PDB path, dbghelp | assertion/reporting/profiling/crash-report chain | Confirmed | Rebuild early |

## Lifecycle risks to recover

- Exact install order and dependency prerequisites.
- Main-loop phase order (OS events, network dispatch, simulation, scene alter, render, audio).
- Shutdown behavior on connection close, scene change, fatal error, and normal quit.
- Cross-thread ownership and whether message dispatch marshals to the main thread.
- Scheduled/asynchronous asset callbacks during scene teardown.

## Object/replication details already exposed

Creature RTTI includes callback-bearing delta variables for posture, visual posture, mood, animation mood, look-at target, skills, group, rank, state bitset, scale, wearable entries, mission-critical objects, and other state. These names provide a concrete starting point for mapping baseline view indices to runtime fields without packet interception. Exact ordering and serialization types still need recovery from class constructors and server baseline classes.

## Replication-to-subsystem event fan-out

The client uses typed message transceivers to decouple replicated state from consumers. RTTI confirms one replicated change may feed several independent systems: experience feeds the skill manager, experience monitor, profession page, player-stat page and profession graph; group changes feed ground/space group and ship/formation UI; waypoint changes feed the waypoint monitor and space-zone map; active/completed quest bit arrays feed the quest journal; visual posture feeds ground status UI; and Jedi state feeds profession UI plus the command-table parser.

For the replacement, keep wire decoding independent from consumers. A suitable conceptual flow is:

`validated baseline/delta -> transactional object-state commit -> typed domain event -> zero or more UI/animation/gameplay subscribers`

This preserves observable fan-out while avoiding the original global transceiver storage and legacy object layouts. Event delivery order, main-thread affinity and re-entrancy remain to be recovered before implementation.

## Appearance/animation dependency breadth

A single installed human-player template fans into a skeletal appearance template, body/head LOD mesh generators, body/face skeletons, logical animation tables, client-data effects and multiple sound templates. RTTI confirms graph-based animation selectors/blends/priorities and typed customization variables. The replacement asset manager therefore needs cross-resource dependency tracking and partial invalidation; a flat mesh/material loader is insufficient. See `APPEARANCE_ANIMATION_ARCHITECTURE.md`.

## UI resource model

The installed root UI and hundreds of include fragments are declarative text. They define hierarchical named widgets, styles/palettes, packing, visibility, event expressions and mediator-consumed data paths. Combined with typed CUI subscriber classes, this upgrades the UI model from Tentative to **Strong architecture / Confirmed resource form**. See `UI_ARCHITECTURE.md`.

## Options/preferences transaction

`SwgCuiOpt` owns eleven child mediators (AFK, Alarm, Chat, Chat Colors, Combat,
Controls, Graphics, Misc, Sound, Terrain and UI). The ordinary pages register
typed integer-slider, float-slider, Boolean and selection bindings containing a
current getter, setter, default getter, bounds and optional dependent callback.
Changes preview through setters immediately; opening the parent snapshots all
scalar values, OK retains them, Cancel restores them, and deactivation writes
the typed `OPTN/0002` machine-options store. Defaults is separately
confirmation-gated.

Chat Colors, Alarm and AFK bypass the scalar rollback layer. Chat highlight and
alarm add/remove actions mutate their global manager collections immediately,
and AFK commits custom state during page deactivation, so these changes survive
Cancel in the legacy client. A replacement should implement a toolkit-neutral
settings registry and may improve this asymmetry deliberately, but it should not
mistake it for unknown behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md`.

## World/environment clock

Scene start and `ServerTimeMessage` supply an authoritative elapsed-Zone time in seconds. GroundEnvironment reduces it modulo the terrain PTAT cycle duration, then advances cycle seconds by frame delta locally and wraps at duration. Its normalized phase drives interpolated environment records and is exposed to lighting and adaptive-music decisions. The effective Tatooine terrain specifies an 11,280-second/188-minute cycle. This subsystem should become a small engine-neutral clock service with explicit correction policy, feeding renderer environment parameters and audio events without inheriting GroundScene memory layout.

Environment descriptor selection combines the terrain chunk's PTAT family ID with the server weather index. The exact row falls back to the same family at weather zero and only then to an empty default descriptor. Descriptor rows jointly define sky, two cloud layers, time-varying lighting ramp, shadow/fog policy, day/night environment maps, a camera-attached atmospheric effect, wind scale and adaptive audio. Changes retain current/previous descriptors and blend visual state for five seconds. A replacement should keep this selection/transition service renderer-neutral and publish immutable descriptor snapshots to renderer and audio consumers.

## Structure placement session

`EnterStructurePlacementModeMessage` (`0xE8A54DC1`) carries a deed ID and original client object-template path into `SwgCuiStructurePlacement`. The mediator owns a cancelable session containing the copied path, fetched SharedTangibleObjectTemplate, derived footprint/appearance state, temporary preview scene object and StructurePlacementCamera. The camera supports four quarter-turn orientations, terrain-conforming footprint cells, green/red/yellow overlay shaders and WMAP-aware water feedback. Completion emits the ordinary `placeStructure` command as `deedId X Z quarterTurn`; Stardust independently recomputes height and enforces all authoritative validation. This should remain a gameplay tool/session above the generic renderer, terrain-query and command-queue layers.

## Particle effect timing, hierarchy and allocation

PEFT/0002 initial/transition/sustaining time-scale fields determine the delta delivered to emitter groups, while its fourth float controls overall spatial effect scale. `ParticleEffectAppearance` recursively multiplies both forms of scale through nested parent effects. Its alter path subdivides larger advances before invoking each group, accumulates effect elapsed time, then rebuilds aggregate bounds and liveness. These rules belong in a portable particle-instance layer rather than the renderer.

The legacy particle allocator uses global fixed-size 0xA8-byte records and block/pool thresholds. EMTR/0014 `+0x229` selects admission into a larger reserve threshold. The replacement should preserve observable priority/drop policy but replace record sizes, raw pointers and global allocator internals with native containers or data-oriented arenas.
# Ticket purchase/travel destination service (2026-09-12)

The legacy `SwgCuiTicketPurchase` mediator combines three separable responsibilities: asynchronous destination discovery, presentation/map construction, and final command submission. On activation it issues one `PlanetTravelPointListRequest` per known planet and tracks replies with `m_requestsOutstanding`. Each `PlanetTravelPointListResponse` carries parallel point-name, coordinate, tax, and interplanetary arrays, which the mediator joins by index into destination records. The final purchase is not a special packet: it is the ordinary `purchaseTicket` command with four names and a `1|0` round-trip flag. A replacement should retain the asynchronous protocol service and server-authoritative transaction while decoupling both from the UI framework.

The same mediator supports space-terminal travel. It keeps the terminal ID, selected ShipControlDevice ID, and a shared list of selected launch-party member IDs separately. It submits `launchIntoSpace` with the terminal as the command target and the device/member/destination values as arguments. This should become a session/service request in a modern client; the server remains responsible for ship ownership, certification, component, party-member and destination validation.

# Object radial-menu service (2026-09-12)

Radial menus are an asynchronous merge between client/default actions and server-authoritative additions. The client sends inner controller type `0x146` with target/player IDs, flat indexed menu records and an 8-bit cache generation; Stardust echoes/merges the menu and returns type `0x147` with the same schema and counter. The client rejects stale generations, reconstructs hierarchy from item/parent indices and retries timed-out requests. Server callback selections are then sent as `ObjectMenuSelectMessage` containing only target ID and semantic radial ID. A replacement should model this as a protocol-backed menu service, not embed packet handling in UI widgets.
