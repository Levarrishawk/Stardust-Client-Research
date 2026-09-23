# Craft output-option mediator

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`.

## Result

`SwgCuiCraftOption` presents three branch actions before customization or
experimentation. Its button callback `0x00D35230` sets a `CuiCraftManager`
output-mode value and either advances the stage or opens the experiment
mediator:

| Action branch | Manager mode call | Subsequent action | Interpretation |
|---|---:|---|---|
| First button | `0x0098DB60(2)` | `0x0098CBE0` stage advance, subject to a resource/capacity gate | Manufacture schematic path; strongly inferred from later mode-2 `createManfSchematic` command |
| Second button | `0x0098DB60(1)` | `0x0098CBE0` stage advance | Prototype/item path; strongly inferred from later mode-1 `createPrototype` command |
| Third button | `0x0098DB60(0)` | Opens `WS_CraftExperiment`; only if replicated crafting state is `3` | Return to experimentation; confirmed control flow |

The first branch consults an object/capacity measure and shows localized UI
feedback when insufficient. The exact gameplay quantity represented by that
measure is not yet confidently labeled. Do not implement it from the
decompiler's temporary variable names.

Activation `0x00D35030` subscribes to `CuiCraftManager::Customize` and
`SessionEnded`; deactivation `0x00D35100` removes both. On a successful
customize-stage event, `0x00D35430` opens `WS_CraftCustomize` only when the
replicated crafting state is `4`; otherwise it warns. This confirms the UI
transition is response/state-driven rather than an immediate button swap.

## Evidence and confidence

Executable RTTI/vtables `0x015F1434/0x015F14B8`, button callback
`0x00D35230`, activation/deactivation `0x00D35030/0x00D35100`, customize
event callback `0x00D35430`, manager `0x0098CBE0`, and final commands
documented in the preceding checkpoints. Branch calls and stage gates are
**confirmed by code**; names of first/second buttons are **strong inference**
from their later server command modes.

## Open

Exact capacity/cost check and localized string semantics on the first branch
need further tracing. Full button widget hierarchy/presentation is not yet
required for protocol compatibility.

Scratch: `local-private/session-2026-09-23-marathon/block40_option.txt`
and matching decompiles.
