# Space `onPointerToggled` cursor synchronization

Date: 2026-09-24. Scope W10. Original target `C:\Stardust-DEV\Stardust.exe`; Ghidra output is reconstructed pseudocode.

`SwgCuiHudActionSpace` registers `onPointerToggled` at `0x0196CB98`. Its `0x00E211D0` action branch tests `0x00881920`; this reads a count at input/UI owner `+0x2C` and returns true when positive. When the count is **not** positive, the branch gets the owner through `0x00881620`, obtains a cursor position from `0x0093D820`, and applies it through `0x0093D1F0`. The position getter chooses stored coordinates at `+0x4C/+0x50` under one mode, otherwise half the current screen width/height. The apply helper calls a cursor-position routine (`0x0078FDB0`) and publishes an input/UI event with code `0x12` through `0x0087FF60`/`0x010E77C0`.

This is local pointer/cursor reconciliation on a space-action notification. There is no direct network send or command in the inspected branch. The semantic name of the count and the exact event-code interpretation remain unproven; do not label it a modal-dialog count without a typed owner or event definition. It is not a protocol requirement for the first x64 networking proof of concept.

Confidence: **Confirmed by executable** for action name, count gate, stored-versus-screen-center coordinate choice, cursor/event calls and no direct send; **Unknown** exact mode and event labels. No new opcode or whole-client percentage change.
