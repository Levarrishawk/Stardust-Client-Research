# Permission-list workflow

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The structure/POB-ship permission-list feature is now closed beyond its already
mapped response packet. `SwgCuiPermissionList` displays the authoritative primary
member vector, uses the secondary vector as optional add-name suggestions, and
submits exact `permissionListModify` command strings for add/remove. Stardust
resolves players or `guild:<abbreviation>`, validates authority and list policy,
mutates the relevant structure/ship list, then updates cell access where needed.

## Stable functions

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00FABC70` | `swg_cui_permission_list_construct_VA_00FABC70` | binds title, member list, add/remove/close, add page and combo/input | Confirmed |
| `0x00FAC1C0` | `swg_cui_permission_list_button_handler_VA_00FAC1C0` | opens/closes add page and queues exact add/remove commands | Confirmed |
| `0x00FAC7A0` | `swg_cui_permission_list_factory_VA_00FAC7A0` | creates `/PDA.PermissionList` mediator | Confirmed |
| `0x00FAC810` | `swg_cui_permission_list_apply_VA_00FAC810` | replaces members, suggestions and localized list title from response | Confirmed |

## Protocol and action mapping

`PermissionListCreateMessage` (`0x52F364B8`) contains:

```
U32 memberCount
repeat memberCount: Unicode memberName
U32 suggestionCount
repeat suggestionCount: Unicode suggestedName
Unicode listName
```

The first vector populates the visible member list. The second populates the add
combo's data source; Stardust currently sends zero suggestions, so manual entry is
normal and must remain enabled.

Add queues:

```
permissionListModify <entered-name> <list-name> add
```

Remove queues:

```
permissionListModify <selected-name> <list-name> remove
```

The server additionally supports `toggle`, although this mediator uses explicit
add/remove. List names are normalized uppercase server-side and actions lowercase.

## Server policy

- The caller must have administration rights to an in-range/targeted structure
  or POB ship.
- Names longer than 40 characters are rejected.
- Player names resolve through `PlayerManager`; guild principals use
  `guild:<abbreviation>` and resolve through `GuildManager`.
- Valid structure lists include the lists configured on that object, commonly
  `ADMIN`, `ENTRY`, `BAN`, `HOPPER` and `VENDOR`.
- Owners cannot be removed from `ADMIN` or banned; callers cannot remove
  themselves from required administration; capacity and conflicting-list rules
  are enforced.
- Adding to `BAN` revokes the target's other permissions first.
- Successful changes update affected building/ship cell permissions, including
  nearby guild members for guild principals.
- Response member names are regenerated from stable U64 principal IDs; guilds are
  serialized as `guild:<abbreviation>`. Invalid stale IDs are cleaned from the
  authoritative list after the response is sent.

## Replacement-client contract

Treat list contents as a replace-on-response snapshot keyed by the Unicode list
name. Never serialize internal x64 pointers or local UI row objects. Preserve both
counted Unicode vectors, manual input, the exact command spelling and `guild:`
principal convention. Server messages and ordinary system-message responses are
authoritative; optimistic local add/remove should not become permanent without a
new snapshot.

## Unknowns

The historical purpose of server-populated suggestion values beyond add-input
completion is not demonstrated because current Stardust emits none. This does not
leave member semantics, command construction, authority or mutation behavior
unknown.

