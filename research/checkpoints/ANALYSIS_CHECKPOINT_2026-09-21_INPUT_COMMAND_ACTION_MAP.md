# Input Command / Numeric Action Corpus

Date: 2026-09-21

## Scope

This block exhaustively inventories the effective external `CMDS/0006` assets
used by the configured client. It closes the earlier content-enumeration gap for
numeric input actions without assigning unsupported historical enum names.

Reproduction:

```text
input_command_inventory.py C:\Stardust-DEV input_command_actions.csv input_numeric_action_map.csv
```

The extractor applies `swgemu_live.cfg` search-tree precedence, including
compressed TRE entries and tombstones, then parses the confirmed
`CMDS/CATE/CMD` record grammar.

## Effective corpus

| Command asset | Commands | Populated action records | Distinct numeric action IDs |
|---|---:|---:|---:|
| `input/groundinputmap_cmds.iff` | 169 | 179 | 40 |
| `input/spaceinputmap_cmds.iff` | 235 | 275 | 80 |
| `input/ui_inputmap_cmds.iff` | 32 | 71 | 32 |
| **Total** | **436** | **525** | **120** |

Every source is `CMDS/0006`. The 436 commands occupy 1,744 serialized action
slots. Populated slots are distributed as follows:

| Slot | Populated records |
|---:|---:|
| 0 | 410 |
| 1 | 81 |
| 2 | 34 |
| 3 | 0 |

Slot 3 must still be decoded, retained and written: its absence in the current
corpus is not permission to shorten the fixed four-record format. The runtime
later closes the four slots as press, release, held/repeat, and forced
reset/cancel; see `ANALYSIS_CHECKPOINT_2026-09-21_INPUT_ACTION_TRANSITIONS.md`.

## Action IDs are interpreter operations, not command identities

There are only 120 distinct nonzero `value0` values for 436 named commands.
The most common value, `324`, occurs 311 times and always carries a string
payload such as `/kneel`, `/jump`, or `/ui action targetSelf`. It is therefore a
generic string/semantic-command dispatch operation. The user-facing command is
the containing `CMD_*` record plus its payload, not numeric ID 324 alone.

Other action records use `value1` as raw 32-bit parameter bits. Common values
include `0x3f800000` and `0xbf800000` (+1.0 and -1.0 as IEEE-754), consistent
with signed axis/action magnitudes. A replacement decoder must preserve the raw
U32 and let the selected action interpreter apply the expected type; globally
coercing `value1` to integer or float would be incorrect.

The authoritative many-to-many evidence is in:

- `input_command_actions.csv`: one row per serialized slot, with source,
  category, command, allowed device mask, flags, action ID, parameter bits and payload;
- `input_numeric_action_map.csv`: every nonzero numeric action ID and every
  command/slot reference using it.

## Categories and flags

Ground categories contain camera 9, character 15, chat 26, internal 22, target
26 and UI 71 commands. Space contains camera 12, chat 26, internal 22, ship 55,
target 45 and UI 75. The dedicated UI file contains 32 commands in category
`all`.

`flag0` is zero throughout this effective corpus. `flag1` is set on 30 command
definitions: cursor/edit/navigation keys and several space weapon-group cycling
or fire commands. That population is compatible with a repeat/continuous-input
policy, but executable behavior has not yet proven its historical name. It is
retained as an opaque Boolean.

## Replacement contract

1. Load the external command asset named by the main IMAP before resolving
   physical bindings.
2. Preserve command name, category, allowed device-family mask, both flags and exactly
   four action records.
3. Dispatch each populated record as `(numeric action ID, raw U32 parameter,
   ASCII payload)` at the appropriate input transition.
4. Use `input_command_actions.csv` as the completeness fixture. No hard-coded
   replacement enum may silently discard an observed ID.
5. Keep the string-command interpreter behind a bounded semantic command
   interface; do not execute payload text as host shell or script input.

## Confidence

- **Confirmed by effective assets:** source/version counts, all command names,
  categories, flags, shift masks, four slots and all action tuples.
- **Confirmed by corpus structure:** numeric IDs are many-to-many operation
  selectors rather than unique user command IDs; ID 324 is payload-driven.
- **Strongly inferred:** common `value1` bit patterns are signed scalar
  magnitudes.
- **Unknown but preserved:** historical names for command flags and
  implementation semantics of numeric IDs not yet tied to a consumer. Slot
  transition roles are confirmed.
