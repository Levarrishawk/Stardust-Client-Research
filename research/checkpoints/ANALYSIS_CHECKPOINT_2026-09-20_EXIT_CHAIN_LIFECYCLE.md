# Exit-chain and top-level lifecycle closure

Date: 2026-09-20

## Scope

This block resolves the previously generic `app_entry_VA_00401050` / `ExitChain` portion of the process lifecycle. It does not claim that every subsystem-specific `install()` body is understood.

## Confirmed control flow

```text
CRT entry 0x0131DC7A
  -> WinMain wrapper 0x00401810
    -> app entry 0x00401050
       -> foundation/platform configuration and install
       -> Game::install 0x00422E80
       -> register game loop callback 0x00423780
       -> platform run boundary
       -> registered Game::cleanupScene callback 0x00423780? [see correction below]
       -> drain ExitChain 0x00A9FB60 -> 0x00AA22C0
       -> foundation/platform removal
       -> close process handle acquired during startup
```

Correction to the compact diagram: `0x00423780` is the game loop, while the callback passed at `0x004015CC` is `0x00423780` and is invoked by `0x00A9FB50`; scene cleanup itself is `0x00423700`, registered inside `Game::install` with the diagnostic name `Game::cleanupScene` and callback `0x00423700`. The two must not be conflated.

## ExitChain record and ordering

`exit_chain_register_VA_00AA21E0(callback, diagnosticName, priority, suppressDuringForcedExit)` allocates a 20-byte node:

| Offset | Meaning | Evidence |
|---:|---|---|
| `+0x00` | next node | linked-list traversal |
| `+0x04` | callback | indirect call in drain and callback-key removal |
| `+0x08` | diagnostic-name pointer | registration call sites |
| `+0x0c` | signed priority/order key | sorted insertion comparison |
| `+0x10` | Boolean execution flag | tested by drain before callback |

Insertion is in ascending priority order. Equal-priority registrations retain registration order because insertion skips nodes whose priority is less than or equal to the new node. `exit_chain_drain_VA_00AA22C0` marks the chain as draining, unlinks the head before invocation, conditionally calls the callback, and frees the node. Callbacks registered while draining can therefore be consumed in the same drain. `exit_chain_force_drain_VA_00AA2320` sets a forced-exit flag and enters the same drain.

This is **not inherently a reverse-destructor stack**. Replacement code must preserve explicit dependency order rather than assuming all legacy removals were LIFO.

## Game lifecycle registrations

`game_install_VA_00422E80` registers the following confirmed callbacks with diagnostic names:

- `Game::remove` -> `0x00423690`
- `MoodManagerClient::remove` -> `0x00500B60`
- `ObjectAttributeManager::remove` -> `0x004FA0E0`
- `DraftSchematicManager::remove` -> `0x004EC0A0`
- `AuctionManagerClient::remove` -> `0x004CBE20`
- `PlanetMapManagerClient::remove` -> `0x004B8910`
- `ResourceIconManager::remove` -> `0x004B5280`
- `ClientRegionManager::remove` -> `0x004AD3D0`
- `QuestJournalManager::remove` -> `0x0049B240`
- `Game::cleanupScene` -> `0x00423700`
- `CuiManager::remove` -> `0x00880DC0`

The install routine also selects optional diagnostic I/O windows by configuration (`ShadowVolumeTest`, `DynamicNormalMapTest`, `TimeOfDayTest`, `JgrillsTest`, `LodShaderTest`, `SetTextureTest`, `WaterTest`, `SplitMeshTest`) before installing ordinary game managers.

`game_remove_VA_00423690` unregisters configuration variables/commands, removes animation-side state at `0x00401A40`, destroys the small polymorphic owner stored at `0x01908850`, and clears that global. `game_cleanup_scene_VA_00423700` calls the active scene's virtual cleanup operation and then the scene-release boundary `0x00424220(0)`.

## Failure and cleanup behavior

- If the graphics/window installation test at `0x004014E5` fails, app entry skips the game-loop registration/run region and still performs its outer cleanup.
- ExitChain drain is explicitly called after the platform run callback returns.
- The received-message malformed-archive exception path remains process-fatal; no lifecycle catch in app entry converts it into session recovery.
- The original chain permits registration during teardown. A replacement should prohibit or tightly define this behavior because it complicates ownership reasoning.

## Replacement-client implication

Use typed RAII owners and an explicit phase graph (`platform -> diagnostics/config -> VFS/assets -> protocol -> game/session -> UI/scene`) rather than recreating the global callback list. Preserve observed external ordering requirements, but do not preserve callback registration as the ownership mechanism. Scene/session cleanup must run before resources that its destructors may touch are released.

## Confidence

- ExitChain node layout, sorted insertion, equal-priority stability and drain behavior: **Confirmed by code**.
- Named Game registrations and callback addresses: **Confirmed by code and embedded diagnostic strings**.
- Full semantic name of every platform/foundation call in `app_entry`: **Unknown**; deliberately not invented.

