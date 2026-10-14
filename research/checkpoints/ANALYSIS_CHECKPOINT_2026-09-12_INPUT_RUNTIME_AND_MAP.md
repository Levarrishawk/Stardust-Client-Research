# Analysis checkpoint: DirectInput runtime and InputMap/0006

Date: 2026-09-12

## Scope

The path from Windows DirectInput devices through normalized engine events and the versioned input-map asset that binds those events to client commands.

## Confirmed findings

### Device installation and ownership

`direct_input_install_VA_00420650` creates the DirectInput8 interface, registers removal, installs keyboard, mouse and joystick devices, then applies default joystick dead-zone/saturation ranges. Device pointers are held in global slots; all installed devices are also appended to a common device vector used for service/removal.

- `install_keyboard_device_VA_00420180` allocates a 0x130-byte KeyboardDevice, uses the standard keyboard data format, acquires it and loads the configured translation table.
- `install_mouse_device_VA_00420210` is conditional on `useMouse`, allocates a 0x34-byte MouseDevice and uses the standard mouse data format.
- `install_joystick_devices_VA_00420420` is conditional on `useJoystick`; DirectInput enumeration is capped at eight joystick objects.

The configuration reader at `0x00421510` loads `ClientDirectInput` keys including `useJoysticks`, `useJoystick`, `useMouse`, and `keyboardTranslationTable`. The apparent singular/plural joystick keys are both real compatibility inputs, not documentation spelling variants.

### Buffered raw records become a small normalized event vocabulary

Each device inherits a common buffered-device base. Device polling reacquires on DirectInput input-lost/not-acquired results. Buffered records are 20-byte `DIDEVICEOBJECTDATA`-shaped entries, and device-specific update functions translate them into the engine event sink at `0x00AB5670`.

Keyboard update `keyboard_device_translate_event_VA_0041EA40`:

- maps the DirectInput scan-code offset through a 256-byte configured translation table at device `+0x30`;
- uses bit 7 of `dwData` for press/release;
- emits normalized event type 7 for key down and 8 for key up;
- contains special callback handling for configured modifier/system keys before ordinary emission.

Mouse update `mouse_device_translate_event_VA_0041EC90`:

- maps relative X/Y/Z offsets to normalized axis event type 14 with axis indices 0..2;
- carries fractional remainders across polls after applying per-axis scale;
- maps button offsets to event types 15/16 for down/up.

Joystick update `joystick_device_translate_event_VA_0041F700`:

- normalizes six axes using queried min/max/range and per-axis sensitivity/inversion;
- emits axis type 9 with indices 0..5;
- converts POV data to type 12 and a normalized angle, using -1 for centered;
- emits joystick button down/up as types 10/11;
- handles slider-like controls through additional normalized event types.

This event layer is the correct compatibility seam. DirectInput COM objects are implementation history; normalized event identities, axis directions and subsequent bindings are behavioral contracts.

### IoWin routing is a queued, top-down, consumable dispatch

`normalized_input_event_enqueue_VA_00AB5670` allocates a five-dword event node containing next pointer, type, two integer parameters and one raw 32-bit value/float. It appends the node to a FIFO headed at `0x01945828` and tailed at `0x01945824`. Repeated joystick axis events of types 9 and 13 may be coalesced by device/axis while within configured tolerance; this prevents redundant high-rate analog events from flooding the queue.

`iowin_dispatch_queued_events_VA_00AB5310` detaches the complete queue once per service pass, adds a synthetic frame/input event, and processes each node in order. It first invokes an optional global event observer, then walks the IoWin stack from its head through the saved next link. Each IoWin receives virtual slot `+4`; a return flag can remove that window and can terminate propagation. Event nodes are returned to the pool after dispatch. Input focus is therefore not a single hard-coded target: it is ordered stack propagation with explicit consumption/removal semantics.

### InputMap has one effective binary schema

`input_map_load_VA_00AADA30` accepts outer form `IMAP`. Version `0005` is explicitly skipped as old. Version `0006` is the only parsed schema; other versions produce an unsupported-version diagnostic.

The canonical writer at `input_map_write_0006_VA_00AAD930` emits:

```text
FORM IMAP
  FORM 0006
    FORM CMDS
      CHUNK NAME   // map name
      command records
    FORM SHFT      // shift/modifier definitions
    FORM MAP       // one sparse map per shift state
```

The v6 reader first loads command definitions and resolves them into a name-indexed command collection, then loads shift states, then binding maps. A binding references a command by name during load; missing commands are diagnosed and left unresolved rather than silently assigned a numeric command.

### Sparse maps preserve device-specific binding families

Each `MAP` begins with `INFO`: a 32-bit shift-state identifier plus a one-byte flag. Optional child forms then hold distinct binding families:

| Form | Binding family |
|---|---|
| `KEYS` | keyboard keys |
| `MOSB` | mouse buttons |
| `JOYB` | joystick buttons |
| `JOYX` | joystick axes |
| `JOYS` | joystick sliders |
| `JOYP` | joystick POVs |

Each family consists of repeated `DATA` chunks. For key/mouse/button bindings the record begins with a 32-bit physical input identifier followed by a command name. The loader resolves the name to a command object and checks its allowed device-family mask (later closed as bits 1/2/4/8 for key-like, POV, axis and slider inputs). Axis/slider/POV families use specialized readers but retain the same physical-binding-to-command principle.

Ground, space and UI use separate map assets. Confirmed embedded defaults include `input/groundinputmap_swg.iff`, several alternative ground schemes, `input/spaceinputmap_default.iff`, `input/spaceinputmap_ja101.iff`, and `input/ui_inputmap.iff`. The action string `toggle_mouse_mode` is one concrete command contract.

## Replacement-client implications

Preserve the `IMAP/0006` parser, command names, shift-state masks, separate ground/space/UI maps, and normalized input semantics. Replace acquisition with prototype host input events or a native Raw Input/XInput layer. An adapter should translate modern physical events into a device-neutral compatibility event, then apply the original InputMap binding logic. This also makes rebinding independent of the renderer and window layer.

Joystick normalization must reproduce center/range, dead-zone, sensitivity, inversion and POV-centered behavior. Keyboard mapping must preserve the configurable 256-entry translation table where legacy scan-code identity matters.

## Unknowns

- exact names of all normalized event-type enum values;
- binary layout of command-definition records inside `CMDS`;
- exact `JOYX`/`JOYS`/`JOYP` DATA record widths beyond the confirmed resolution behavior;
- modifier callback identities in KeyboardDevice;
- semantic names of every IoWin return-flag bit and the exact CUI/GroundScene stack order during each game state.

## Confidence

- DirectInput device types, configuration, reacquisition, event values, IMAP version envelope and top-level grammar: **confirmed by code**.
- FIFO event allocation, analog coalescing and top-down consumable IoWin dispatch: **confirmed by code**.
- Replacement seam and requirement to preserve command names/shift states: **confirmed architectural consequence**.
- Human-readable names for internal numeric event types: **tentative until enum/string correlation**.

## Resume point

Decode the `CMDS` record and the three specialized joystick binding readers, then label IoWin return bits and confirm concrete CUI/GroundScene ordering in each scene state.
