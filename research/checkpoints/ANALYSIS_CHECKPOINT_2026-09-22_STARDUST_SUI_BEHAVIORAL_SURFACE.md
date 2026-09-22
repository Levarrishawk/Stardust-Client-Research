# Analysis checkpoint: Stardust SUI behavioral surface

Date: 2026-09-22  
Scope: current native/Lua SUI producers correlated with `CuiDataDrivenPage` and effective UI assets

## Result

Stardust's live server-authored SUI surface is narrower than the full protocol enum but deeper than simple labels and buttons. Current scripts rely on terminal close events and button events, property extraction, mutable data sources, sliders, text inputs, geometry changes, the legacy UI expression engine, and an object-viewer property bridge.

## Events actually required

Current active Lua SUI code uses:

- event 1 (`onButton`) for calibration/processor puzzle actions;
- event 9 (`onClosedOk`) for accepted list, input, transfer and keypad values;
- event 10 (`onClosedCancel`) for list/keypad cancellation state.

No active current script subscribes directly to event 7 (`onTextbox`). Text is obtained as a subscribed property when a terminal event is emitted. This explains why the executable can interoperate with the server despite having no recovered specialized textbox listener override.

## Property and widget capabilities required

| Capability | Current evidence | Replacement need |
|---|---|---|
| localized/plain text | `Text`, `LocalText` on labels and text boxes | Unicode property conversion and localization-token rendering |
| visibility/input state | `Visible`, `Enabled`, `GetsInput` | live layout/input-state mutation |
| button semantics | `IsDefaultButton`, `IsCancelButton`, `ButtonPressed` | default/cancel routing and readable event properties |
| list selection/data | `List.dataList`, `SelectedRow`, `otherPressed` | data items, list model and selected-index serialization |
| transfer control | `ConversionRatioFrom/To`, two input texts | custom transfer widget behavior already reconstructed |
| slider/bar values | `Value` on slider and bar paths | numeric property conversion and slider interaction |
| geometry | `Location`, `Size`, `ScrollExtent`, `UserResizable` | runtime layout mutation with legacy coordinate semantics |
| color | `Color` on calibration controls | legacy color parser/property setter |
| client UI scripts | `OnRunScript`, followed by empty `RunScript` trigger | legacy expression subset (`Value`, parent size, location/size arithmetic) |
| object viewer | `setObject` on `PDA.Questpopup.comp.vP.viewer` | U64/object-ID-to-renderer bridge |

The calibration games make `OnRunScript`/`RunScript` functional rather than decorative: scripts dynamically recompute bar size and position from `Value`. A replacement that parses pages but ignores the expression subset will render these live Stardust puzzles incorrectly.

## Update-subscription compatibility quirk

Several puzzle completion paths place `subscribeToEvent(..., "noCallback")` into an update batch. The executable's confirmed `0x00A7C480` rule skips **all** type-5 subscription commands after initial creation. Therefore those attempted callback replacements do not take effect in the original client; existing subscriptions remain installed.

This should be recorded as observed compatibility behavior. A replacement should initially reproduce the skip rule. If Stardust later wants true runtime subscription replacement, that is a deliberate protocol/server-and-client extension—not something to silently "fix" during reimplementation.

## Asset path validation

The concrete paths referenced by templates and puzzle scripts correspond to nodes present beneath the effective root pages, including:

- common caption/prompt/OK/cancel/other/revert controls;
- list data source and list selection;
- keypad result and enter/slice/keycard buttons;
- transfer transaction inputs and labels;
- calibration slider/bar hierarchies;
- PDA quest-popup text and object viewer.

Dynamic indexed paths require hierarchical lookup at runtime; they cannot all be pre-bound as fixed members.

## Architectural consequence

The original UI asset adapter should expose a typed property service rather than allow gameplay code to know Godot/DX12 widget internals:

```text
SUI command interpreter
    -> legacy path resolver
    -> typed property adapter
       -> text/layout/input/data-source widget
       -> expression evaluator
       -> object-viewer renderer bridge
```

This preserves wire and asset behavior while allowing the modern UI implementation to differ internally.

