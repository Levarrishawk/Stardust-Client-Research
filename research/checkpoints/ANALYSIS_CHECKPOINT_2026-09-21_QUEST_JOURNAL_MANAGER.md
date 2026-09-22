# Quest Journal Manager

## Scope

This block closes the static quest-definition catalog, its relationship to replicated player quest state and dynamic counter/timer overlays, and carries forward the confirmed Stardust controller-ID incompatibility as an explicit pre-build server/client decision.

## Static quest catalog

`quest_journal_manager_install_VA_0049B0C0` loads `datatables/player/quests.iff` through the shared DataTable/VFS layer and immediately builds an in-memory quest catalog.

The executable resolves seven named columns, allocates one 0x50-byte definition per row, and retains:

- numeric row/index identity;
- narrow quest/internal strings;
- Unicode/localizable display strings;
- a visibility/availability boolean;
- an ordering/category integer;
- parent/child hierarchy links.

After all rows are created, it resolves parent references by name. A missing parent produces the exact diagnostic `QuestJournalManager: quest '%s' has invalid parent '%s'`; the definition remains diagnosable rather than silently attaching to the wrong node. Root entries and per-parent children are retained in separate ordered collections.

The precise historical names of all seven columns are not embedded as ordinary strings at the call sites (they are initialized globals), so this block does not invent them. Their value types and hierarchy consumers are confirmed.

## Runtime state composition

The journal shown to the player is a composition of three sources:

1. definitions and hierarchy from `datatables/player/quests.iff`;
2. active/completed quest and task state from `PlayerObject` replication and quest-library state;
3. transient counter/timer records received through object-controller messages.

Query functions filter visible definitions against active/completed/task predicates, include library-provided quests, merge children, and sort by stored display order. `SwgCuiQuestJournal` separately subscribes to `PlayerObject::QuestsChanged`, `CompletedQuestsChanged`, `ActiveQuestsChanged`, and player setup, while `QuestJournalManager` publishes `CountersChanged` and `TimersChanged` for its transient overlays.

Dynamic records use the quest identity plus task/label identity and replace an existing matching record rather than appending duplicates. Counter records retain current and maximum values; timer records retain a duration/deadline value. The manager publishes the corresponding no-payload typed event after mutation, causing the journal to requery.

## Confirmed protocol incompatibility

The executable's object-controller registry expects:

| Client controller ID | Client class | Payload after scalar |
|---:|---|---|
| `0x440` | `MessageQueueQuestTaskCounterMessage` | ASCII quest; U32; Unicode label; U32; U32 |
| `0x441` | `MessageQueueQuestTaskTimerMessage` | ASCII quest; U32 task/selector; Unicode label; U32 duration |

Current Stardust emits:

| Server controller ID | Server class | Payload |
|---:|---|---|
| `0x441` | `QuestTaskCounterMessage` | ASCII quest; U32 max; Unicode label; U32 zero; U32 current |
| `0x444` | `QuestTaskTimerMessage` | ASCII quest; U32 task ID; Unicode label; U32 seconds |

Therefore the current counter is delivered under the ID the executable assigns to a shorter timer codec, and the server timer uses an ID for which this executable has no recovered factory. This is not merely a naming discrepancy: the `0x441` body lengths differ, making the current server counter unsafe to decode as the client's timer.

The replacement client can deliberately support Stardust's current IDs, but that is a compatibility profile decision. If exact Stardust.exe interoperability remains required, the server definitions should be corrected or version-negotiated rather than silently overloading one ID with two schemas.

## Asset dependency

The quest journal cannot be reconstructed from network traffic alone. The hierarchy, visibility/order metadata, static journal strings, and many task relationships originate in `quests.iff` and quest-library assets. Original asset consumption is therefore mandatory for faithful journal behavior.

## Replacement requirements

- Load and validate the original `quests.iff` table through VFS before exposing journal definitions.
- Build parent links only after all definitions exist; diagnose missing parents and break cycles safely.
- Keep definition data immutable and player progress/transient overlays separate.
- Key counter/timer overlays by structured quest/task identity and replace atomically.
- Publish typed counter/timer changes, while replicated quest bit/packed state uses the PlayerObject event path.
- Implement controller IDs through a versioned compatibility table. Never select a parser only by server-side class name.
- Resolve the `0x440/0x441/0x444` discrepancy before calling quest counter/timer interoperability complete.

## Confidence

- **Confirmed:** table path, seven-column load, row object size, parent-link second pass, invalid-parent diagnostic, active/completed filtering topology, local timer/counter maps and events, UI subscriptions, executable controller registrations/codecs, and current Stardust mismatch.
- **Strongly inferred:** detailed business labels for some table members and exact timer deadline convention.
- **Unknown:** original column names for all seven fields and whether Stardust operators currently exercise the mismatched Lua helpers in production content.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `quest_journal_addresses.txt`
- `decompiled-quest-journal/`
- `MMOCoreORB/src/server/zone/packets/object/QuestTaskCounterMessage.h`
- `MMOCoreORB/src/server/zone/packets/object/QuestTaskTimerMessage.h`
- `MMOCoreORB/src/server/zone/objects/player/LuaPlayerObject.cpp`

