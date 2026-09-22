# InputMap/0006 command and binding codec closure

Date: 2026-09-20

## Scope

This block completes the binary record layouts left open by `ANALYSIS_CHECKPOINT_2026-09-12_INPUT_RUNTIME_AND_MAP.md`: external command definitions and joystick/POV bindings.

## Command-file indirection

The main `IMAP/0006` file contains `FORM CMDS / NAME`, whose NUL-terminated value is another logical asset such as `input/groundinputmap_cmds.iff`. `input_map_load_command_file_VA_00AB3280` opens that resource through the ordinary VFS and requires:

```text
FORM CMDS
  FORM 0006
    repeated FORM CATE
      CHUNK NAME       // category string
      repeated CHUNK "CMD "
```

Failure to open or a version other than `0006` is diagnosed and causes command loading to fail. Categories and commands are loaded before the main map resolves bindings.

## Exact CMD record framing

`input_map_command_load_VA_00AB29A0` consumes each `CMD ` chunk as:

```text
cstring commandName
U32 allowedDeviceFamilyMask
U8 commandFlag0
U8 commandFlag1
CommandAction action0
CommandAction action1
CommandAction action2
CommandAction action3

CommandAction := U32 value0, U32 value1, cstring payload
```

The corresponding writer `input_map_command_write_VA_00AB2AE0` emits the same order. The in-memory command is 0x70 bytes: name at `+0x00`, category at `+0x0c`, shift mask at `+0x18`, four 0x14-byte action records at `+0x1c/+0x30/+0x44/+0x58`, and flags at `+0x6c/+0x6d`.

The four records are independently dispatched for activation/press, release,
held/repeat update, and forced reset/cancel respectively. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_ACTION_TRANSITIONS.md`.

The category name is not repeated inside each `CMD ` chunk. Loader `input_map_command_categories_load_VA_00AB2E50` assigns the enclosing CATE name to each constructed command. Empty categories and duplicate command names are diagnosed.

## Shift states

`FORM SHFT` contains optional `FORM "KEY "` and repeated `DATA` records:

```text
U32 physicalKey
U32 shiftStateBit
U8 flag
```

The effective ground map uses six such nine-byte records. `InputMap` maintains a 32-entry reference/count array and derives the active shift-state bitmask from nonzero entries before selecting a `MAP` with matching `INFO` state.

## Exact MAP binding grammar

Each `FORM "MAP "` begins with:

```text
CHUNK INFO: U32 shiftStateMask, U8 mapFlag
```

Every binding family has the same DATA payload shape:

```text
U32 physicalInputIndex
cstring commandName
```

The supported forms and loaders are:

| Form | Family | Loader |
|---|---|---:|
| `KEYS` | keyboard key | `0x00AB2130` |
| `MOSB` | mouse button | `0x00AB2130` |
| `JOYB` | joystick button | `0x00AB2130` |
| `JOYX` | joystick axis | `0x00AB22A0` |
| `JOYS` | joystick slider | `0x00AB2410` |
| `POV ` | joystick point-of-view | `0x00AB2580` |

This corrects the earlier provisional spelling `JOYP`: the executable reads/writes the four-byte tag `POV ` (`0x504F5620`). The three specialized loaders differ in destination table/limit, not wire record width.

All loaders resolve `commandName` against the command collection and verify that
the command's `allowedDeviceFamilyMask` intersects the loader-family constant:
`0x1` for key/mouse/button, `0x2` for POV, `0x4` for joystick axis, and `0x8`
for joystick slider. Missing commands and disallowed device families are
diagnosed. MAP `INFO` is the separate shift-state selector. The runtime stores
`(physical index, command pointer)` pairs; pointer width is internal only and
must never enter a replacement file decoder.

## Replacement-client implications

- Original IMAP/0006 assets can be consumed directly; no binding rediscovery is required.
- Decode all integers into fixed-width values and resolve names to stable command IDs/handles after validation.
- Preserve four independent command actions and two flags even before every semantic label is known.
- Treat unknown physical indices as unbound with diagnostics, not as array indices without range checks.
- Modern physical events can be translated into the same six binding families, allowing Godot/Raw Input/XInput acquisition while retaining original maps.

## Confidence

- CMDS/CATE/CMD framing and every field width/order: **Confirmed by paired executable reader/writer and effective assets**.
- MAP/INFO and all six family record layouts: **Confirmed by paired executable reader/writer**.
- Four action-slot meanings and MAP inheritance flag: **Confirmed**. Command
  flag1 selects an alternate repeat-delay policy but its historical label is
  unknown; flag0 is zero throughout the effective corpus and remains opaque.
