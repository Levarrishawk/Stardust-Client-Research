# Community UI composition

Date: 2026-09-22

## Result

`SwgCuiCommunity` is a six-page composition root, not a monolithic social
protocol handler. Its pages are Personal, History, Character, Search, Friend and
Ignore. The root constructs each child mediator, registers them as workspace
sub-mediators, synchronizes them to one tab control, and delegates all substantive
state to the appropriate manager/replication path.

## Root mediator

| Address | Provisional name | Role |
|---|---|---|
| `0x00F6FD50` | `swg_cui_community_construct_VA_00F6FD50` | Resolves six pages, constructs six child mediators, binds Done and tabs, and loads `datatables/matchmaking/matchmaking.iff`. |
| `0x00F70260` | `swg_cui_community_destroy_VA_00F70260` | Releases all child mediators and the parsed matchmaking definition map. |
| `0x00F703D0` | `swg_cui_community_activate_VA_00F703D0` | Activates the selected page and modal/input state. |
| `0x00F70400` | `swg_cui_community_deactivate_VA_00F70400` | Restores input and deactivates all six children. |
| `0x00F70450` | `swg_cui_community_done_button_VA_00F70450` | Closes the root on Done. |
| `0x00F70470` | `swg_cui_community_factory_VA_00F70470` | Loads `/Community.main` and constructs the mediator. |
| `0x00F70510` | `swg_cui_community_lookup_matchmaking_definition_VA_00F70510` | Looks up a parsed matchmaking definition by key. |
| `0x00F705A0` | `swg_cui_community_tab_changed_VA_00F705A0` | Deactivates non-selected pages and activates the chosen one. |

The constructor's concrete child constructors are:

- Personal `0x0105D660`
- History `0x0105A8D0`
- Character `0x01058360`
- Search `0x01054690`
- Friend `0x0104CEE0`
- Ignore `0x01043450`

The root parses the original matchmaking table into a keyed definition map used
by children for labels/categories and selection semantics. This is original
asset-driven behavior and should be loaded from the shipped IFF rather than
hard-coded.

## State and protocol ownership

RTTI-owned transceivers and the already closed packet/replication paths establish
the page boundaries:

- Friend subscribes to `CommunityManager::FriendListChanged` and
  `FriendOnlineStatusChanged`; chat friend-list/status messages feed that manager.
- Ignore subscribes to `CommunityManager::IgnoreListChanged`; chat ignore-list and
  mutation results feed that manager.
- Search subscribes to player-controller `CharacterMatchRetrieved`; the request is
  `requestCharacterMatch` and results arrive as inner type `0x1E7`.
- Character subscribes to biography retrieval, `WhoStatusChanged`, spoken-language
  changes and player setup/state.
- History subscribes to `MatchMakingManager::MatchFoundResult`.
- Personal edits the replicated `MatchMakingId` preference model, but the current
  Stardust `setMatchMakingPersonalId`/`setMatchMakingCharacterId` commands are
  validation-only stubs and do not persist changes.

Thus the root introduces no new wire format. It composes previously mapped chat,
controller and replication services.

## Replacement-client contract

Keep a social/community feature shell with independently testable page
view-models. Preserve original `matchmaking.iff` loading and unknown bit positions
in the data model. Treat current-server matchmaking preference writes as dormant
capability; do not fabricate persistence. Friend/ignore, search and biography
must remain separate services even if a modern UI presents them together.

## Confidence

- Six-page construction, table load, tab switching and lifecycle: **Confirmed**.
- Manager/transceiver ownership: **Confirmed by RTTI and mapped handlers**.
- Current server matchmaking write stubs: **Confirmed by server source**.
- Historical semantics of every matchmaking-table bit: **Not claimed; preserve from assets**.
