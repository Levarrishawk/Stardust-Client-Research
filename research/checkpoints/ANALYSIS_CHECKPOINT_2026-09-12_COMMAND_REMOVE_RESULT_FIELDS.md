# CommandQueueRemove result-field closure

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The two historically called `tab1`/`tab2` U32 fields in ObjectController inner
type `0x117` are now behaviorally named:

- field 1 is the client command-result/error category;
- field 2 is its category-specific detail enum.

The complete typed payload remains:

```text
U32 actionCounter
F32 commandTimerSeconds
U32 resultCategory
U32 resultDetail
```

## Executable evidence

`client_command_queue_remove_apply_VA_0046E720` finds the queued action, publishes
the Removing event, and, when `resultCategory != 0`, calls localization helper
`0x0046E9D0` with the command identity and both result fields.

`0x011BB8E0(resultCategory)` indexes a seven-entry StringId table in `cmd_err`:

| Value | StringId |
|---:|---|
| 0 | `success_prose` |
| 1 | `locomotion_prose` |
| 2 | `ability_prose` |
| 3 | `target_type_prose` |
| 4 | `target_range_prose` |
| 5 | `state_prose` |
| 6 | `god_level_prose` |

For category 1, `resultDetail` is converted through the 22-entry locomotion enum
table. For category 5 it is converted through the 34-entry state enum table. The
selected detail becomes a localized parameter in the command-error prose. Other
categories do not consume the detail in this helper.

## Server correlation

Current Stardust source independently supplies the exact intended values:

- locomotion failure sends `{category=1, detail=creature locomotion}`;
- state failure sends `{category=5, detail=lowest active invalid-state bit index}`;
- invalid target sends `{3,0}`;
- too far sends `{4,0}`;
- successful completion sends category/detail zero and a nonzero command duration.

The server names the fields `tab1` and `tab2`, but those are placeholder source
names, not evidence of their semantics.

## Replacement requirements

Decode fixed-width wire fields unchanged. Internally expose a typed
`CommandCompletion { actionCounter, duration, category, detail }`. Preserve the
seven category values and locomotion/state detail tables for original string-table
compatibility. Unknown categories/details should log and fall back to numeric text,
not index legacy arrays out of bounds.
