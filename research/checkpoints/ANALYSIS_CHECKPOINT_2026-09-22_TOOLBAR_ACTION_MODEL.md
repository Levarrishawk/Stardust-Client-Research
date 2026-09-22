# Toolbar Action and Persistence Model Checkpoint

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Finding

`SwgCuiToolbar` is a local, persistent action-binding model that can reference
commands, macros, inventory objects and separators. It resolves a selected slot
into an action object and delegates execution into the already-mapped command,
macro or object-use systems. It is not itself a new network protocol.

## Evidence

RTTI identifies `SwgCuiToolbar`, its action helper, scene/player-setup,
player-command-added/removed and macro changed/reset subscribers, plus a
keybinding-change callback receiver.

Constructor `0x00F64830` creates the action model; binds `volumePage`, sample
item page/button, separator sample, `volumeKeyBindings` and text pane; loads
`PalTarget`, icon margin and flash-color properties; subscribes to scene change
and tutorial events; and retains multi-pane/multi-slot selection state.

The input/context routine `0x00F669D0` distinguishes item use, action perform,
remove and describe operations and supports drag/reorder placement. Dispatcher
`0x00F673A0` handles `toolbarItemRemove`, `toolbarItemUse`,
`toolbarActionPerform`, `toolbarItemInfo`, separator insert/remove,
`toolbarItemBind` and `toolbarCommandDescribe`.

`0x00F67990` resolves the selected slot, marks/animates it, invokes the retained
action object's execution method and publishes success/failure feedback. The
action object determines whether execution becomes a local macro/UI action,
inventory object use or command-queue request.

## Persistence

`0x00F68900` persists a rectangular pane/slot model through the common mediator
settings store. It records pane count, per-pane slot count and for each slot a
typed action identifier plus optional command/macro text, object identity and
retained action data. Empty slots receive explicit defaults. It validates equal
slot counts across panes and logs partial-save failures.

`0x00F696D0` rebuilds key labels by querying input bindings named
`CMD_uiToolbarSlot%02d`. This complements the decoded `.uis` container: `.uis`
is the generic per-character envelope; these routines define toolbar-specific
content written into it.

## Replacement design

Use an explicit tagged slot variant:

```text
Empty | Command{name,args} | Macro{id,name} | Object{networkId} | Separator
```

Keep pane/slot indices and wire-visible object IDs fixed-width. Resolve current
key labels through the new input layer and validate action availability at use
time. Preserve/import original settings and icons, but replace raw-pointer
action objects and implicit 32-bit layouts.

## Confidence and residuals

Class/event structure, UI action vocabulary, keybinding naming and persistent
pane/slot iteration are **confirmed**. Exact historical labels for every stored
subkey are **strongly inferred** and should be checked against populated user
`.uis` fixtures. Network behavior delegates to already-bounded systems.

