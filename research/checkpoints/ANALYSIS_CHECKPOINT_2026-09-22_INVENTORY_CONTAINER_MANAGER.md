# Inventory and Container Manager Workflow

## Scope

This block reconstructs `CuiInventoryManager` above the already known object
containment system: authoritative open acknowledgement, UI open/close events,
multi-window reference tracking, local containment helpers, resource splitting,
and destruction requests.

## Open/close protocol

`inventory_request_open_VA_008D9AF0` assigns a nonzero incrementing U32 request
token. In connected play it submits the ordinary `openContainer` command; the
current Stardust command validates range, line of sight, lock state and OPEN
permission, then emits opcode `0xDCA57409` (`OpenedContainerMessage`) followed
by the container's contained objects.

The server message body is:

- U64 container NetworkId;
- U32 value (current Stardust emits zero);
- U16 mode (`0` open, `2` close in the server constructor).

The listener at `0x008D8DB0` decodes the message, resolves the NetworkId to a
`ClientObject`, warns on an unresolved object, and routes a valid response into
`inventory_publish_open_request_VA_008D9CA0`. The manager publishes either
`ItemOpenRequest` or `ItemOpenRequestNewWindow`, both carrying
`pair<ClientObject*, string>`; the HUD window manager owns the concrete UI.

Open state is reference-counted per container. Closing one view decrements the
count. `inventory_notify_item_closed_VA_008D9E70` submits `closeContainer` only
when the last local view closes, and publishes `ItemCloseAllInstances` for
forced closure. Current Stardust's ordinary close command does not send a close
acknowledgement (`closeContainerTo(..., false)`), so client-side close state is
intentionally optimistic.

## Containment and capacity helpers

The manager does not invent a separate inventory database. It resolves live
`ClientObject`/container state and uses containment operations shared with
replication:

- `inventory_find_best_destination_VA_008D9200` enumerates candidate containers
  and chooses the least occupied option, breaking ties by contained-object
  count; an empty single-slot candidate ends the search early.
- `inventory_remove_from_inventory_VA_008D94F0` resolves the player's named
  inventory slot and removes the requested object through the container API.
- `inventory_transfer_equipped_to_inventory_VA_008D9680` removes nested
  equipped/attached children as needed, transfers them to inventory or world
  placement, and then inserts the requested object; failure paths are explicit
  warnings rather than silent success.

These calls mutate the client projection for responsiveness, but authoritative
containment remains the server's scene-object/container update stream. A new
client should support prediction with reconciliation, never treat the UI model
as authoritative.

## Resource splitting and object destruction

`inventory_request_resource_split_VA_008DA580` accepts only a
`ResourceContainerObject` or `FactoryObject` with a valid containing context,
activates the `ResourceSplitter` mediator, and publishes `SplitContainer` with
two NetworkIds. `SwgCuiResourceSplitter` is the concrete typed subscriber.

`inventory_request_server_destroy_VA_008DA850` rejects null/self targets. In
connected mode it submits `serverDestroyObject`; an offline/development path
may invoke local destruction directly. A separate confirmation-dialog path is
used when the object's policy requires confirmation.

## Replacement-client model

Keep four layers distinct:

1. authoritative replicated containment graph;
2. pending open/close/move/destroy commands keyed by IDs/tokens;
3. short-lived predicted UI projection with reconciliation;
4. windows subscribed to typed open/close/split events.

Do not retain raw object pointers in UI state. Use NetworkIds and resolve them
against the current object registry so destruction, zone transition, or a late
open response cannot leave a dangling reference.

## Confidence

- **Confirmed:** server validation and message layout, client listener route,
  open token, per-container view count, last-close command, concrete event
  payload types, resource/factory split gate, and destroy command name.
- **Strongly inferred:** friendly roles of the capacity and equipment-transfer
  helpers from their container calls and diagnostics.
- **Unknown:** historical semantic name of the server's always-zero U32 open
  field and whether any non-Stardust server uses U16 modes other than 0/2.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- listener/vtable `0x015E8268`, listener `0x008D8DB0`
- `0x008D9200`, `0x008D94F0`, `0x008D9680`, `0x008D9AF0`,
  `0x008D9CA0`, `0x008D9E70`, `0x008DA580`, `0x008DA850`
- `MMOCoreORB/src/server/zone/packets/scene/ClientOpenContainerMessage.h`
- current `OpenContainerCommand`, `CloseContainerCommand`, and
  `SceneObjectImplementation::openContainerTo/closeContainerTo`
- inventory-manager RTTI transceivers and owned UI subscribers
