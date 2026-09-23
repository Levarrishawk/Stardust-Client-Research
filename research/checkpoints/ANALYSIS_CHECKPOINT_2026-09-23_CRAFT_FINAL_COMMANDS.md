# Craft completion command identities and state boundary

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`; current server source read-only.

## Result

The stage-5 branch of `CuiCraftManager` function `0x0098C570` queues one of
two commands rather than sending ObjectController `0x15A` again. Static
initializers establish the exact command identities:

| Client command hash slot | Literal and initializer | Client branch | Stardust callback |
|---|---|---|---|
| `0x01932B04` | `createPrototype` at `0x018A2D20`, hashed at `0x00988A50` | Output type `1` | `CreatePrototypeCommand` parses counter and optional create-item boolean; calls `CraftingSession::createPrototype` |
| `0x01932B00` | `createManfSchematic` at `0x018A2D30`, hashed at `0x00988A80` | Output type `2` | `CreateManfSchematicCommand` parses counter; calls `CraftingSession::createManufactureSchematic` |

`0x0098C570` decimal-formats a client counter and appends another `0`/`1`
digit on the prototype branch, then uses command queue `0x0046E5F0`.
The exact choice semantics should be taken from the server parser and session
method, not from the decompiler's temporary variable names.

## Compatibility caution

The executable branch checks replicated client-visible crafting state `5`
before queuing these commands, while `CreatePrototypeCommand` requires the
server's private session state `6`. The intervening `nextCraftingStage`
transition and `finishStage1` explain the difference; see
`ANALYSIS_CHECKPOINT_2026-09-23_CRAFT_FINAL_STATE_TIMELINE.md`.

Current server registers both commands in `CommandConfigManager2.cpp` and
aborts active trade before running the crafting session callback. A replacement
must preserve these command strings, argument grammar, response sequencing,
and session ownership; it need not reproduce the old dialog layout.

## Evidence/confidence

Exact literals, hash initializers, client command queue, server registration,
parsing, and the 5/6 state-publication distinction are **confirmed by code**.

Scratch: `local-private/session-2026-09-23-marathon/xrefs_block38.tsv`,
`block38_init_disasm.tsv`, and `0098c570` decompile.
