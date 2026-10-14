# Input Runtime Dispatch

## Layering

The runtime path is confirmed as four separable layers:

```text
DirectInput devices
  -> IoWinManager normalized events
     -> stacked scene/UI IoWin consumers
        -> InputMap command/action queue
           -> camera, controller, UI, console and gameplay consumers
```

This is important for replacement: original key codes and DirectInput object layouts do not belong in gameplay systems. The stable concept is a normalized action stream with context-sensitive consumers.

## Device installation

`DirectInput::install` at `0x00420650` calls `DirectInput8Create` version `0x0800`, then installs keyboard, mouse and joystick devices. Creation failure is fatal during startup. It configures mouse/axis normalization and iterates up to eight joystick slots, applying per-axis normalization, dead-zone/range/saturation-style settings and force-feedback related setup.

`SetupClientDirectInput::install` at `0x00421490` wraps platform/window setup, installs DirectInput, registers a platform callback, and performs final device setup. This confirms input depends on the native window handle but the rest of the action stack is engine-owned.

## `IoWinManager`

`SetupSharedIoWin::install` (`0x00AB5030`) initializes the shared IoWin layer. `IoWinManager` install `0x00AB5130`:

- registers reverse-order removal;
- creates a shared fixed-size pool of 20-byte input-event blocks;
- registers platform input callbacks;
- initializes two arrays of 30 state slots.

IoWin is a stack/chain of input windows. Scene, UI, hyperspace and test consumers have distinct RTTI classes. Events can therefore be consumed or transformed by the active top-level context before reaching lower gameplay layers.

## Input maps and schemes

The byte-level IMAP/0006 grammar is already documented separately. Runtime scheme initialization `0x006E0EF0` constructs the available profiles and persists separate last-used ground and space scheme types:

| Profile asset | Embedded scheme value |
|---|---:|
| `input/groundinputmap_swg.iff` | 0 |
| `input/groundinputmap_iso.iff` | 2 |
| `input/groundinputmap_mmorpg.iff` | 15 |
| `input/groundinputmap_fps.iff` | 5 |
| `input/groundinputmap_ja101.iff` | 0 |
| `input/groundinputmap_mmorpg2.iff` | 31 |
| `input/groundinputmap_swg2.iff` | 54 |
| `input/spaceinputmap_default.iff` | 1 |
| `input/spaceinputmap_ja101.iff` | 1 |

Those integers are confirmed stored values; their bit/enum interpretation is not asserted. Preferences are `ClientGame.lastInputSchemeType` and `lastInputSchemeTypeSpace`.

The ground scene initializes multiple cameras and action consumers, then installs the selected mapping. Camera update routines drain the normalized command queue by numeric action ID and value, proving mapping resolution occurs before camera/controller logic.

## UI input context

`CuiIoWin` constructor `0x0093ADB0` loads `input/ui_inputmap.iff` into a dedicated `InputMap`. It resolves `CuiM_Enter` / `ENTER` and `CuiM_Escape` / `ESCAPE`, centers cursor state from the current UI root size, and initializes reticle/dead-zone and shift-state support. UI handling is therefore its own context, not hard-coded interception in the ground controller.

## Replacement design consequence

- Translate physical keyboard/mouse/gamepad input into logical action events.
- Preserve the original IMAP files and command names through an adapter; do not require rebinding all legacy content.
- Maintain explicit contexts: global, login/UI, ground, space, placement/debug.
- Allow UI to consume navigation/text-entry actions while gameplay is suppressed, without losing device state.
- Use modern raw input/XInput or prototype host input APIs internally; DirectInput compatibility is not required.
- Preserve separate ground/space profiles and import/export of bindings.
- Treat numeric physical-device identifiers in legacy files as 32-bit asset fields, translating them to modern device descriptors.

## Confidence

- **Confirmed:** DirectInput install/device families, eight joystick slots, IoWin event-block pool, stack/context architecture, profile paths/values/preferences, dedicated UI map and Enter/Escape actions.
- **Strongly inferred:** exact meaning of several device calibration arguments and scheme-value bit fields.
- **Closed by corpus inventory:** every effective numeric action ID is mapped to
  its command, slot, raw parameter and payload in `input_command_actions.csv`
  and `input_numeric_action_map.csv`. Historical enum labels remain unknown but
  are not needed to preserve the asset contract. See
  `ANALYSIS_CHECKPOINT_2026-09-21_INPUT_COMMAND_ACTION_MAP.md`.

## Evidence

- `decompiled-input-runtime/`
- `input_runtime_strings.txt`
- `ANALYSIS_CHECKPOINT_2026-09-20_INPUTMAP_0006_CODECS.md`
