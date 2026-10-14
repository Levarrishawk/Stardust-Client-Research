# User Interface Architecture

## Installed declarative UI corpus

The TRE corpus contains 511 `.inc` records and nine `.ui` record copies. `ui/ui_root.ui` is readable declarative text rather than compiled widget bytecode. It defines a root `Page`, cursor/style/palette paths, packing and numerous includes. Referenced screens include login, cluster and avatar selection/creation, loading, ground and space HUDs, maps, crafting, skills, inventory/PDA-style pages, auction, missions, conversations, options and scripted UI.

Representative include files define nested widgets such as `Page` and `Data`, named paths, fixed/reference sizes, visibility, palette/style properties, effectors and event expressions such as `OnEnable`/`OnDisable`. `ui/ui_avatar_creation.inc` retains named data paths consumed by mediator/controller code.

These structures are **Confirmed by read-only extraction from Stardust TRE archives**.

## Runtime model

Client RTTI and callback evidence indicate a mediator/controller layer above the declarative widget tree. Classes such as `SwgCuiQuestJournal`, `SwgCuiWaypointMonitorBox`, profession/stat mediators and ground/space group pages subscribe to typed gameplay events and update named UI objects. The UI resource provides hierarchy, styles and identifiers; C++ mediator classes provide behavior and game-state binding.

`cui_manager_install_VA_008802F0` constructs the central UI manager/canvas, installs callback/service adapters, creates input/message listeners and loads/registers UI resources before later game mediators activate. `CuiManager::remove` is registered through the process ExitChain, while scene changes are separately published by `game_set_scene_VA_00424220`. This confirms process-lifetime UI services with scene-sensitive mediators rather than destruction of the entire UI system on every zone transition.

Although `qt-mt305.dll` is present beside the executable, Stardust.exe neither imports Qt nor retains a demonstrated Qt loader path in the analyzed UI installation. Qt is therefore **not evidence for the in-game UI** and should be excluded from replacement requirements unless a future direct call path proves otherwise.

Conceptually:

```text
replicated/domain event
  -> typed transceiver
  -> CUI mediator/controller
  -> named widget/data path in declarative UI tree
  -> layout/style/effect/render
```

## Replacement options

Behavioral compatibility does not require reproducing the original widget implementation. Three approaches remain viable for later evaluation:

1. parse a useful subset of legacy `.ui`/`.inc` into an engine-neutral UI description;
2. convert legacy layouts offline into the selected engine/UI toolkit;
3. redesign screens while retaining mediator-level behavior, data flow and protocol semantics.

The best approach may differ by milestone. Login/selection can be redesigned, while complex HUD and scripted UI may benefit from extracting named paths and layout metadata.

## Engine-evaluation implications

Prototype host has to be evaluated against the actual mediator/layout problem: support large declarative hierarchies, named-path lookup, data binding, effectors/animations, scaling from legacy 1024x768 reference layouts, cursor/style palettes, dynamic scripted pages and independent ground/space HUD composition. A small proof should convert or recreate one nontrivial include and connect it to a typed domain event.

## Remaining analysis

1. Map exact mediator activation/deactivation and named-widget lookup contracts.
2. Finish root include-resolution error/fallback behavior.
3. Identify scripted UI message formats and server-provided page generation.
4. Inventory widget/style/effect property vocabulary.
