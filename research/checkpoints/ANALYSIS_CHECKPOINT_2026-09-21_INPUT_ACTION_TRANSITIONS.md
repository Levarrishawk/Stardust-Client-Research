# Input Command Action-Transition Semantics

Date: 2026-09-21

## Closed slot meanings

| Slot | Offset | Runtime transition |
|---:|---:|---|
| 0 | `+0x1C` | activation / press |
| 1 | `+0x30` | deactivation / release |
| 2 | `+0x44` | held/repeat update |
| 3 | `+0x58` | forced reset/cancel |

`0x00AADFD0` starts an active-input record and immediately dispatches slot 0.
`0x00AAE150` finds the matching record, dispatches slot 1 and removes it.
`0x00AAE5A0` advances records from repeat-delay state 1 to active state 2 and
dispatches slot 2 during updates. `0x00AAE470` dispatches slot 3 for live
commands while clearing input state during reload/reset.

Slot 3 is empty in the effective corpus but is an active executable contract
and must remain available for custom/user command maps.

`commandFlag1` participates in initialization of the active record's repeat
delay. Its exact historical label is unavailable, but its consequence is
bounded: it selects an alternate repeat-delay policy before slot 2 begins. This
agrees with its use on editing/navigation and repeated space-weapon commands.
`commandFlag0` remains zero in the effective corpus and is preserved.

## Replacement contract

- Preserve all four slots even when empty in default assets.
- Generate explicit `Pressed`, `Released`, `HeldRepeat`, and `Cancelled`
  transitions after physical-to-command resolution.
- Keep repeat timing policy in the input adapter, not gameplay systems.
- On profile reload, device loss or context reset, issue cancellation for live
  bindings before destroying their command references.

## Confidence

- Slot 0/1/2/3 transition roles: **Confirmed** by direct command-offset dispatch
  sites and active-record lifecycle.
- Flag1 as alternate repeat-delay policy: **Strongly inferred** from its field
  read, timer state and corpus distribution; historical enum/name unknown.

## Evidence

- `input_module_disasm.tsv` around `0x00AADFD0`, `0x00AAE150`,
  `0x00AAE470`, and `0x00AAE5A0`
- `decompiled-input-policy-runtime/`
- `input_command_actions.csv`
