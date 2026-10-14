# UI Mediator Class Inventory

## Scope

Static RTTI and vtable analysis of the `Cui` / `SwgCui` application layer in `Stardust.exe`, followed by a focused inspection of `CuiMediator`, `SwgCuiInventory`, and the crafting-screen family. This block inventories architecture; it does **not** claim that all 225 classes have complete behavioral reconstructions.

## Confirmed findings

### Concrete class breadth

The executable contains 225 unique non-template RTTI names beginning with `Cui` or `SwgCui`. Compiler-generated transceiver/template types and anonymous source-path RTTI were excluded. The reproducible list is `ui_concrete_class_inventory.tsv`.

The heuristic functional grouping is:

| Group | Classes |
|---|---:|
| character/progression | 16 |
| crafting | 7 |
| framework | 24 |
| HUD/gameplay | 20 |
| inventory/economy | 32 |
| login/session | 12 |
| missions/quests/maps | 11 |
| options/support | 8 |
| social/communications | 20 |
| space | 26 |
| other/specialized | 49 |

The grouping is a navigation aid, not a recovered original taxonomy.

### Framework shape

`CuiMediator` has RTTI and a primary vtable at `0x015E8EE4`. Its virtual family includes destruction, activation/deactivation/update-related behavior, input/UI callbacks, and shared mediator services. Derived screens use a repeated multiple-inheritance layout: one table for their callback/listener face and another beginning with the `CuiMediator` base slots.

`SwgCuiInventory` has primary tables at `0x015F033C` and `0x015F03C0`. Constructor `0x00C874E0`:

- installs both vtables;
- binds UI objects named `ContainerPage`, `EqPage`, `TogglePaperdollButton`, and `ToggleExamineButton`;
- creates the inventory container view and optional equipment/paper-doll view;
- requires a `CodeData` object named `info` and fails loudly if it is absent;
- registers a callback node and marks itself update-capable.

Its update routine `0x00C88520` delegates to the base update then maintains distance/availability closure behavior for an externally targeted container. Its activation and deactivation paths `0x00C88A10` and `0x00C88B80` attach/detach container state, persist UI settings, and toggle the associated equipment/paper-doll state. This confirms UI mediators are stateful controllers over declarative pages, not merely visual layouts.

### Crafting is an explicit UI state machine

Seven concrete crafting UI classes are present:

- `SwgCuiCraftDraft`
- `SwgCuiCraftAssembly`
- `SwgCuiCraftAssemblyDialogOption`
- `SwgCuiCraftExperiment`
- `SwgCuiCraftCustomize`
- `SwgCuiCraftOption`
- `SwgCuiCraftSummary`

Their RTTI callback types expose the workflow edges directly:

```text
DraftSchematicsReceived / DraftSlotsReceived
                  |
                  v
             CraftDraft
                  |
                  v
            CraftAssembly
       SlotTransferStatusChanged
       Assembling / DoneAssembling
                  |
                  v
           CraftExperiment
          Experiment / Customize
                  |
                  v
           CraftCustomize
               Complete
                  |
                  v
            CraftSummary

Any stage ---- SessionEnded ----> teardown
```

`SwgCuiCraftAssembly` also subscribes to schematic change, object-attribute change, complexity-change, and container-selection events. `SwgCuiCraftExperiment` subscribes to complexity change and confirmation-message closure. This is strong evidence that `CuiCraftManager` owns network/session state while the mediators subscribe to typed local transceivers.

Constructor evidence supports the roles:

- Assembly `0x00CFDFA0`: `containerInventory`, `containerHopper`, `volumeSlots`, requirement colors, schematic/complexity text, unload pages, quality bar.
- Draft `0x00D1F980`: schematic `treeView`, `checkShowAll`, tabs and navigation.
- Experiment `0x00D2B210`: attribute sample/composite, risk display, current points and complexity, experiment button.
- Summary `0x00D35FA0`: result table, success text and sample bar.

## Architectural consequence

The replacement should preserve the separation, not the original C++ inheritance:

1. protocol messages update a crafting/session domain controller;
2. typed domain events update a current workflow state;
3. a screen controller binds that state to prototype host or another eventual UI implementation;
4. declarative SWG UI assets may be translated or adapted, but gameplay state must not live inside widgets.

The same model applies to inventory: a durable container/equipment model plus a disposable screen controller.

## Confidence

- **Confirmed:** 225 filtered concrete RTTI class names, named vtables, constructor widget bindings, and listed callback/transceiver payload names.
- **Strongly inferred:** the workflow ordering shown above and manager/controller ownership boundary.
- **Tentative:** exact source-level name of each `CuiMediator` virtual slot; function-level naming still requires per-slot caller analysis.
- **Unknown:** full behavior of every specialized screen and exact fidelity requirements of layout scripts/events.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `cui_rtti_raw.txt`
- `ui_concrete_class_inventory.tsv`
- `cui_vtable_inventory.tsv` (raw scanner output; requires RTTI validation)
- `decompiled-ui-mediators/`
- vtables `0x015E8EE4`, `0x015F033C`, `0x015F03C0`, `0x015F1004` through `0x015F15A0`

