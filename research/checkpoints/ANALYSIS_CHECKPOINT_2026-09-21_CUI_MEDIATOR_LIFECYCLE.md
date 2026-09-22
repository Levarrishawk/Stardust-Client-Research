# `CuiMediator` Lifecycle Contract

## Confirmed virtual layout

The primary mediator vtable at `0x015E8EE4` has twelve executable entries. Comparison with `SwgCuiInventory`'s corresponding table at `0x015F03C0` identifies the central slots:

| Slot | Base function | Role | Evidence |
|---:|---:|---|---|
| `+0x08` | `0x009C1010` | close/deactivate request | clears pending state, may remove from workspace |
| `+0x10` | `0x009C1040` | per-frame update | derived inventory calls it with elapsed time |
| `+0x14` | `0x009C1CC0` | activation hook | derived inventory calls it first in `0x00C88A10` |
| `+0x18` | `0x009C1D30` | deactivation hook | derived inventory calls it first in `0x00C88B80` |

The other slots cover geometry/event callbacks and screen-specific behavior; source-level names remain provisional.

## Construction and ownership

Base constructor `0x009BFF00` takes a mediator name and root UI page. It:

- initializes the mediator's flags, callback holders, rectangle/state cache and event object;
- installs vtable `0x015E8EE4`;
- resolves the root page's `CodeData` object;
- optionally binds standardized controls `ButtonClose`, `ButtonMaximize`, `ButtonMinimize`, `checkPopupHelp`, and `WidgetToFocus`;
- reads declarative properties including `StickyVisible`, `StartSize`, `FocusGlow`, and `PopupHelp`;
- registers the mediator in a global collection;
- retains/binds referenced UI objects.

`0x009C1480` increments an ownership/retention counter at `+0x40`; related release logic exists in the same family. A new client must make page/control ownership explicit rather than emulate this raw counter layout.

## Activation/deactivation

Base activation `0x009C1CC0` restores persisted mediator state through the settings layer and applies two geometry-related flags to the root page. Derived mediators then connect domain callbacks and attach model objects.

Base deactivation `0x009C1D30` persists state, propagates the same geometry flags, and updates internal visibility/pending-state flags. Derived mediators then disconnect domain callbacks and detach their model objects.

The repeated ordering in `SwgCuiInventory` is therefore:

```text
activate:
  base mediator restore/activate
  attach domain model + callbacks
  restore screen-specific settings

deactivate:
  base mediator persist/deactivate
  disconnect domain model + callbacks
  persist screen-specific settings
```

## Update and automatic closure

Base update `0x009C1040` implements an optional range/target validity policy. On a throttled interval it obtains the player and target, measures world-space distance adjusted by object radius, and invokes the close path when the target is absent or outside the configured range. This explains why inventory/container and conversation-style mediators can close automatically as the player moves away.

`0x009C2620` marks a mediator active/update-enabled and registers it with the update machinery. `0x009C20A0` clears pending state, handles focus/geometry bookkeeping, and invokes a virtual callback when a flag is set.

## Declarative binding contract

`0x009C21D0` resolves an object/property from `CodeData`, checks the requested runtime type, and emits fatal or warning diagnostics according to whether the binding is required. `0x009C2DB0` wraps it and stores the typed object pointer. Therefore the original UI files are effectively dependency-injection manifests: widget names and types are constructor contracts.

For future original-asset compatibility, a UI adapter must support at least:

- named `CodeData` lookup;
- runtime widget-type checks;
- required versus optional bindings;
- named properties and saved settings;
- event subscription and deterministic release;
- per-frame update registration;
- workspace activation/deactivation and focus.

## Confidence

- **Confirmed:** vtable slot addresses, constructor property/control names, binding/type-check behavior, derived call ordering, target-range update logic.
- **Strongly inferred:** human-readable slot roles `activate`, `deactivate`, `update`, and close request from derived-call behavior.
- **Unknown:** original source names of all twelve virtuals and every internal flag bit.

## Evidence

- `decompiled-cui-mediator-core/`
- `decompiled-ui-mediators/`
- vtables `0x015E8EE4` and `0x015F03C0`

