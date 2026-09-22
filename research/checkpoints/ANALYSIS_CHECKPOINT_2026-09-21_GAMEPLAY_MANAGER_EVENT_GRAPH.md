# Gameplay Manager Event Graph

## Scope and method

MSVC RTTI retains the fully instantiated types for `MessageDispatch::Transceiver` and `OwnedTransceiver`. Their template arguments expose publisher manager, event type, payload type, and—when owned—the concrete subscriber. This block extracts those types into `manager_event_graph.tsv`.

This is architectural evidence, not a claim that RTTI lists every dynamically registered callback.

## Inventory result

- 26 named manager publishers
- 107 unique publisher/event pairs
- 121 concrete owned-subscriber edges
- 228 distinct rows when generic transceiver declarations and concrete subscribers are retained separately

The highest-fan-out publishers are:

| Publisher | Extracted rows |
|---|---:|
| `CuiChatRoomManager` | 42 |
| `CuiCraftManager` | 26 |
| `ClientSecureTradeManager` | 22 |
| `CommunityManager` | 13 |
| `CuiPersistentMessageManager` | 12 |
| `CuiMissionManager` | 11 |
| `ClientMacroManager` | 10 |
| `CuiLoginManager` | 10 |
| `CuiConversationManager` | 9 |
| `CustomerServiceManager` | 9 |

## Confirmed subsystem topology

Representative edges recovered directly from type names:

```text
CuiLoginManager
  -> ClusterConnection, ClusterListChanged, ClusterStatusChanged, AvatarListChanged
  -> avatar/cluster/login screens

CuiCraftManager
  -> DraftSchematicsReceived, DraftSlotsReceived, StageChange
  -> Assembling, DoneAssembling, Experiment, Customize, Complete, SessionEnded
  -> seven crafting mediators

ClientSecureTradeManager
  -> Requested / denied variants / BeginTrade
  -> AddItem / RemoveItem / GiveMoney / Accept / RequestVerify / Complete / AbortTrade
  -> HUD, trade mediator, trade container provider

CuiChatRoomManager
  -> room create/destroy/join/leave/member/moderator/invite/ban/status/message events
  -> chat window, room browser and who list

CuiPersistentMessageManager
  -> HeaderReceived, BodyReceived, MessageFailed, EmailListChanged
  -> AttachObject, StartComposing
  -> mail browser/composer, HUD and chat window

CuiInventoryManager
  -> ItemOpenRequest, ItemOpenRequestNewWindow, ItemCloseAllInstances, SplitContainer
  -> HUD/window manager and resource splitter
```

Other confirmed publishers include auction, combat spam, conversations, ship damage, instant messages, missions, modifiers, skills, socials, system messages, quest journal, attributes, draft schematics, matchmaking, customer service, weapon groups and ship-component attachments.

## Architectural interpretation

The original client consistently separates:

1. network/replication/controller handlers;
2. a subsystem manager that owns durable state and request sequencing;
3. typed local events;
4. one or more UI/presentation subscribers.

This explains why packet handlers frequently appear to do little beyond updating a manager and publishing a transceiver: the visible behavior is deliberately downstream.

For the replacement, preserve this dependency direction with ordinary typed signals/event buses. Do not reproduce template class names or raw pointer payloads. Events should carry stable IDs or immutable snapshots so a screen cannot retain a destroyed scene object.

## Analysis impact

The gameplay/UI long tail is now enumerable in two complementary dimensions:

- 225 concrete screen/controller classes in `ui_concrete_class_inventory.tsv`;
- 107 typed manager event contracts in `manager_event_graph.tsv`.

This does not make every behavior complete, but it prevents future sessions from rediscovering subsystem ownership and provides a finite checklist for manager-by-manager closure.

## Confidence

- **Confirmed:** all extracted publisher/event names and the 121 owned subscriber types are literal RTTI evidence.
- **Strongly inferred:** event direction and durable-state ownership pattern, supported by already-decompiled login, crafting, chat, inventory, mission and replication handlers.
- **Unknown:** callbacks registered through non-templated/dynamic paths and exact payload fields where template names expose only a generic pair/vector type.

## Evidence

- `manager_event_rtti_raw.txt`
- `manager_event_graph.tsv`
- `extract_manager_event_graph.py`
- `C:\Stardust-DEV\Stardust.exe`

