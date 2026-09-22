# Analysis checkpoint: server SUI asset footprint

Date: 2026-09-22  
Target assets: configured `C:\Stardust-DEV\swgemu_live.cfg` TRE overlay  
Server correlation: current native and Lua SUI producers

## Result

Every concrete root page currently named by Stardust server SUI code was found in the effective original UI overlay. This closes an important feasibility question: current server-authored dialogs can be driven from original assets without recreating their layouts by hand, provided the replacement implements the legacy `.ui`/`.inc` loader, namespace lookup, widgets, properties and events.

The effective files are supplied by `stardust_3_01.tre` at search-tree priority 53:

- `ui/ui_root.ui` includes `ui_scripted.inc` and `ui_pda.inc`.
- `ui/ui_script.ui` also includes `ui_scripted.inc` for the script-oriented UI root.
- `ui/ui_scripted.inc` is 530,223 bytes and defines the `Script` page tree.
- `ui/ui_pda.inc` is 273,589 bytes and defines the `PDA` page tree.

## Server root-page inventory

| Server path | Effective asset definition | Producer/use | Status |
|---|---|---|---|
| `Script.listBox` | `ui_scripted.inc`, `Script/listBox` | native list/character-builder; Lua list template | Confirmed present |
| `Script.messageBox` | `Script/messageBox` | native and Lua message boxes | Confirmed present |
| `Script.inputBox` | `Script/inputBox` | native and Lua text input | Confirmed present |
| `Script.transfer` | `Script/transfer` | native bank/transfer and Lua transfer | Confirmed present |
| `Script.keyPad` / `Script.Keypad` | `Script/keyPad` | native keypad and Lua keypad | Confirmed present; lookup is case-insensitive in practice |
| `Script.ColorPicker` | `Script/colorPicker` | native color box | Confirmed present; case differs |
| `/Script.calibration.game1` | `Script/calibration/game1` | Lua array puzzle | Confirmed present |
| `/Script.calibration.game2` | `Script/calibration/game2` | Lua amplifier puzzle | Confirmed present |
| `/Script.calibration.game3` | `Script/calibration/game3` | Lua receiver puzzle | Confirmed present |
| `/Script.calibration.game4` | `Script/calibration/game4` | Lua processor puzzle | Confirmed present |
| `/PDA.Questpopup` | `PDA/Questpopup` | Lua radiation-sensor popup | Confirmed present |

The leading slash and dot-separated namespace syntax both resolve into the UI hierarchy. Asset names preserve mixed case, while live server sources use case variants (`Keypad`/`keyPad`, `ColorPicker`/`colorPicker`). A replacement should normalize lookup consistently with the original client but retain source spelling for diagnostics.

## Usage breadth

There are 119 current screenplay call sites for the principal Lua SUI templates (`SuiMessageBox`, `SuiListBox`, `SuiInputBox`, `SuiTransferBox`, and the quest popup query used for this inventory). The four calibration pages and keypad are also used by live dungeon/custom puzzle scripts. Consequently, SUI asset support is an early compatibility feature, not optional late-stage UI polish.

## Replacement implication

The minimum compatible UI resource layer must support:

1. configured TRE overlay precedence and effective-record selection;
2. `.ui` and `.inc` text parsing with recursive includes;
3. hierarchical `Page`/`Namespace` name resolution with original path semantics;
4. the widget/data-source/property subset already inventoried in the UI corpus;
5. cloning/instantiating a named page for `CuiDataDrivenPage`;
6. case-compatible lookup and useful missing-path diagnostics;
7. runtime property writes, data-source mutation and event subscriptions.

This does **not** require the first networking proof of concept to render SUI. It does make SUI resource loading a mandatory milestone before gameplay scripts can be considered compatible.

## Evidence

- Effective archive selection from `swgemu_live.cfg` and direct TRE extraction.
- `ui_root.ui` / `ui_script.ui` include chains.
- Direct page names in extracted `ui_scripted.inc` and `ui_pda.inc`.
- Current native constructors and Lua `SuiTemplate.new(...)` calls.

