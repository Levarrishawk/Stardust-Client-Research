# Resource Manager and Harvester Workflow

## Scope and conclusion

`CuiResourceManager` is the client-side projection for a selected installation's harvestable resource pools and hopper-empty/discard operations. Its authoritative inputs are an ObjectController resource-data message and a generic correlated response. It then publishes renderer-independent UI events `PoolsChanged` and `EmptyCompleted`.

## Resource-pool snapshot

The manager installation function `cui_resource_manager_install_VA_0099D0B0` subscribes an owned callback to `MessageQueueHarvesterResourceData const&` from `PlayerCreatureController`.

Current Stardust constructs that controller message with inner type `0xEA` and payload:

```text
U64 harvester_object_id
U32 resource_count
repeat resource_count:
  U64 resource_spawn_id
  ASCII resource_name
  ASCII resource_type
  U8 density_percent
```

`cui_resource_manager_apply_harvester_data_VA_0099D470` first verifies that the message's harvester ID matches the currently selected installation. It clears the prior pool tree, converts each 0x28-byte legacy message element into an owned UI resource record, derives localized/type presentation data, inserts the record into the manager collection, and publishes `CuiResourceManager::Messages::PoolsChanged`.

The server computes density as `getDensityAt(zone, harvester X, harvester Y) * 100` and truncates it to U8. The replacement should preserve the incoming U8 as protocol data and convert to display percentage only above the protocol layer.

## Empty or discard hopper request

`cui_resource_manager_begin_empty_VA_0099DB90` owns the confirmation and request workflow. Its authoritative request payload is:

```text
ObjectController inner 0xED (C→Z)
U64 harvester_object_id
U64 resource_spawn_id
U32 quantity
U8 mode        // 0 retrieve to inventory, 1 discard
U8 correlation_token
```

The manager increments/retains an 8-bit token for the in-flight operation, opens confirmation/progress message boxes as appropriate, sends the controller request, and prevents stale dialogs from completing a newer operation.

Current Stardust's `EmptyHopperCallback` validates the installation, admin rights, 20-metre range, resource/container identity and quantity. Retrieve mode clamps to `ResourceContainer::MAXSIZE` and moves material to player inventory if space exists. Discard mode subtracts the requested amount with bounds. The server then broadcasts the updated installation hopper delta.

## Correlated completion

Stardust responds with `GenericResponse(player, 0xED, 1, token)`. `cui_resource_manager_apply_empty_response_VA_0099DF50` accepts only response type `0xED`, compares the returned byte at legacy payload `+9` with the current 8-bit token, and discards stale responses with the embedded `discarding stale empty hopper response` diagnostic. It closes the progress state and publishes `CuiResourceManager::Messages::EmptyCompleted` carrying the result Boolean.

This is a real transaction correlation contract, not cosmetic state. The token width remains U8 in an x64 client.

## Replacement-client model

```text
HarvesterResourceData 0xEA
  -> validate selected installation ID
  -> replace ResourcePoolSnapshot
  -> PoolsChanged(snapshot revision)

Empty/Discard request 0xED + U8 token
  -> PendingHopperOperation
  -> GenericResponse 0xED + same token
  -> reject stale token or publish EmptyCompleted
  -> authoritative hopper delta refreshes quantities
```

Recommended types:

- `HarvesterId` and `ResourceSpawnId`: explicit U64/NetworkId values;
- `densityPercent`: U8 on wire, numeric display value internally;
- `quantity`: U32 on wire, range-checked before UI conversion;
- `mode`: explicit enum backed by U8;
- `correlationToken`: wrapping U8, compared before state mutation.

Do not store installation/resource object pointers in the protocol or pending transaction. UI confirmation objects should be independently cancellable and must not own authoritative world objects.

## Confidence

- `0xEA` resource snapshot fields and server density behavior: **Confirmed by current server constructor and client callback**.
- `0xED` request and `GenericResponse` acknowledgement fields, mode meanings, token correlation: **Confirmed by both sides**.
- Manager collection replacement and typed UI events: **Confirmed**.
- Some legacy tree-node/helper layouts and localized presentation derivations: **Not individually named; not protocol blockers**.
