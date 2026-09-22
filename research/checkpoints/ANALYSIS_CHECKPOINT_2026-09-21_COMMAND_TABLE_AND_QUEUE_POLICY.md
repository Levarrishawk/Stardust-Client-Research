# Command Table and Queue Policy

## Scope

This block closes the data-driven command-definition layer that sits between typed UI/chat commands and the previously mapped ObjectController `0x116` command queue. It inventories the effective Stardust assets, client overlay order, row semantics, and the boundary between local C++ hooks and server-authoritative commands.

## Effective table graph

`client_command_table_install_VA_0052D830` always reads these two list tables first:

1. `datatables/command/command_tables_shared.iff`
2. `datatables/command/command_tables_client.iff`

It then selects ground or space by the current game mode and appends the matching shared and client lists. Every effective list contains a `tableFilename` column and currently names exactly one real table. All twelve list/table assets resolve from `mtg_patch_013_configurable_02.tre` at configured priority 42.

The real-table overlay order is therefore:

```text
shared base -> client base -> shared ground/space -> client ground/space
```

`command_table_list_load_VA_0052D640` ignores missing/invalid referenced tables after warning. `command_table_overlay_load_VA_011BBFC0` visits rows in list order. `command_table_row_apply_VA_011BC4D0` computes the lowercase command CRC/name key, creates or updates the retained definition, and treats `disabled=true` as deletion of an earlier definition rather than as a callable disabled row.

## Corpus measurements

| Table | Rows | Disabled | C++ hook | Script hook | Combat queue |
|---|---:|---:|---:|---:|---:|
| shared base `command_table.iff` | 771 | 4 | 192 | 590 | 320 |
| client base `client_command_table.iff` | 70 | 0 | 70 | 0 | 0 |
| shared ground | 5 | 0 | 2 | 3 | 0 |
| client ground | 2 | 0 | 2 | 0 | 0 |
| shared space | 48 | 0 | 6 | 42 | 0 |
| client space | 13 | 0 | 13 | 0 | 0 |

After case-insensitive overlay, ground has 848 source rows, 11 replacements, 837 distinct keys and 833 enabled retained commands. Space has 902 source rows, 12 replacements, 890 distinct keys and 886 enabled retained commands.

## Row contract

The effective tables contain 69, 74 or 75 physical columns because older client-space variants omit later state/queue columns. The loader accesses columns by name and supplies defaults, making this an evolvable named-column schema rather than a packed fixed record.

Replacement-relevant fields are:

- `commandName`, converted to the command CRC/key used by queue lookup and the server.
- `defaultPriority`: `immediate=0`, `front=1`, `normal=2`; invalid values become an internal default value 3.
- `scriptHook`, `failScriptHook`, `cppHook`, `failCppHook`, `defaultTime`, and `characterAbility`.
- 22 locomotion eligibility bits (`L:*`) and 34 state eligibility bits (`S:*`), stored as bitsets with missing columns taking table defaults.
- `tempScript`, `targetType` (`none=0`, `required=1`, `optional=2`), `callOnTarget`, and `maxRangeToTarget`. The client stores squared maximum range for comparisons.
- `stringId`, `visible`, `commandGroup`, `godLevel`, `displayGroup`, and optional `addToCombatQueue`.
- `disabled`, which removes the keyed definition immediately.

The row decoder resolves C++ hook strings through the registered client-function table. Script hooks remain definitions for the command engine/server route; a modern clean implementation should dispatch semantic handlers explicitly rather than reproduce the original raw function-pointer registry.

## Runtime boundary

The command table decides local eligibility, target requirements, priority, presentation metadata and whether a registered client C++ hook handles the command. Commands that reach the authoritative route use the already confirmed path:

```text
command name/CRC + target + Unicode args
  -> client_command_queue_enqueue_VA_0046D7A0
  -> command_queue_enqueue_send_VA_0062CB50
  -> ObjectController 0x116
  -> Stardust CommandQueueEnqueueCallback
  -> QueueCommand lookup/execution
```

The wire body remains `{U32 actionCounter, U32 commandCRC, U64 targetId, Unicode arguments}`. Server result/removal returns through controller `0x117`. Thus command table metadata is client-side policy and discoverability, while the server command implementation is authoritative.

## Replacement requirements

- Load original named-column DTI tables through the TRE overlay and preserve the four-layer environment-specific order.
- Compute command keys with the established SWG lowercase CRC algorithm; do not use host-language hashes.
- Preserve fixed-width wire CRC/action fields and U64 object IDs on x64.
- Implement disabled rows as removal and retain absent-column defaults.
- Separate local semantic commands from network commands without exposing arbitrary native function lookup.
- Keep eligibility/target/range checks for responsive UI, but never treat them as security boundaries; Stardust must validate again.

## Confidence and bounded unknowns

- **Confirmed:** asset filenames and precedence, row counts/columns, overlay ordering, disabled deletion, priority and target enums, locomotion/state bit loops, squared range, C++ hook resolution, queue boundary, and network DTO.
- **Strongly inferred:** command-name key generation is the same established normalized CRC used by Stardust; the called helper and all downstream map/server behavior agree.
- **Unknown/non-blocking:** historical labels for the internal priority value 3 and every private flag within the retained definition object.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`: `0x0052D640`, `0x0052D830`, `0x011BBFC0`, `0x011BC4D0`, `0x0046D7A0`, and existing controller `0x116/0x117` codec chain.
- Effective DTI assets extracted with `swgemu_live.cfg` overlay precedence.
- Stardust `CommandQueueEnqueueCallback`, `QueueCommand` registry and command implementations previously correlated in `OBJECT_CONTROLLER_PROTOCOL.md`.
- Reproducible corpus summary: `summarize_command_tables.py`.

