# Object Attribute Manager

## Scope

This block closes the normal object-inspection attribute request/response path, cache ownership, invalidation, formatting boundary, and current Stardust server producer. It is distinct from the auction-details packet that happens to use a server class with the same source-side name.

## Active protocol path

| Direction | Identity | Schema | Client path | Result |
|---|---|---|---|---|
| C→Z | queued command `getAttributesBatch` | space-delimited decimal object IDs in the ordinary command-queue argument | `object_attribute_queue_request_VA_004FA170` → pending ordered set → `object_attribute_flush_batch_request_VA_004FCA30` → command queue | asks for one or more object attribute lists |
| Z→C | `AttributeListMessage` / `0xF3F12F2A` | U64 object ID; U32 count; repeated ASCII attribute key + Unicode value | listener `object_attribute_listener_receive_VA_004FA000` → parser `attribute_list_message_parse_VA_00AD1D70` → `object_attribute_apply_response_VA_004FA380` | replace/cache list, timestamp it, publish `AttributesChanged(objectId)` |

`AttributeListMessage` serializes its attribute vector with U32 count. Each key uses the Archive narrow-string length convention: U16 length, or sentinel `0xFFFF` followed by U32 length, then bytes. Each value uses the established Archive Unicode codec. The server normally supplies textual Unicode values even for numeric attributes.

The current Stardust producer is `MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h`. `SceneObjectImplementation::sendAttributeListTo()` supplies ordinary lists. `GetAttributesBatchCommand` accepts at most 1000 token iterations (`MAXIMUM_BATCH = 999` with the post-increment guard), rejects inaccessible creature-owned subobjects for non-privileged users with an empty list, and emits empty lists for missing IDs.

## Request batching

The client does not immediately send one packet per tooltip request. It retains requested `NetworkId` values in an ordered pending set. `object_attribute_flush_batch_request_VA_004FCA30` converts every ID to decimal text, joins them using a delimiter consistent with the server tokenizer, submits one `getAttributesBatch` command through the normal command queue, then clears the pending set.

This means `getAttributesBatch` is not a standalone named `GameNetworkMessage`; it is a gameplay command whose responses are standalone `AttributeListMessage` packets.

## Cache and invalidation

The manager maintains an ordered cache keyed by 64-bit `NetworkId`. A cached entry contains:

- the vector of 24-byte logical attribute pairs (narrow key plus UTF-16 value);
- a freshness/timestamp field;
- bookkeeping used by request suppression/invalidation.

`object_attribute_get_cached_VA_004FA800` can optionally force a request, otherwise returns the cached vector and timestamp. `object_attribute_invalidate_and_request_VA_004FCBD0` clears freshness and requeues the object. An additional set suppresses requests for IDs currently considered unavailable/invalid. Scene/object lifecycle hooks clear entries so stale attributes do not survive object destruction or relevant state changes.

The listener registers only `AttributeListMessage`, parses the archive into an object ID plus attribute vector, updates the cache, and publishes the typed `ObjectAttributeManager::Messages::AttributesChanged` event. RTTI confirms subscribers in `SwgCuiInventoryInfo` and `SwgCuiCraftAssembly`.

## Presentation boundary

The manager also contains extensive formatting helpers. They convert raw key/value pairs into localized, color-marked UI text and can append debug-only template, appearance, hit-point, and internal-name information. These formatters are presentation policy; the compatibility contract is the raw ordered `(key, Unicode value)` list and its change notification.

The replacement should cache structured pairs and let UI format them. It should not preserve the legacy inline color-markup construction as the authoritative data representation.

## Collision with auction naming

Current Stardust also uses the C++ class name `AttributeListMessage` for bazaar/vendor details but changes the wire identity to `0xFE0E644B`, whose executable name is `GetAuctionDetailsResponse`. That schema adds description, template path, and customization fields and belongs to AuctionManagerClient. Opcode and validated schema—not the server header class name—must select the decoder.

## Replacement requirements

- Preserve `NetworkId` as a 64-bit wire value independent of native pointer width.
- Preserve the response order and raw string keys; localization/label grouping happens above the parser.
- Batch requests and cap the number of IDs per command at or below the server's limit.
- Deduplicate in-flight IDs and bound total cached records/string sizes.
- Treat an empty list as a valid negative/missing/inaccessible response, not a parse failure.
- Invalidate cache entries on object destruction and explicit dirty notifications.
- Publish typed `AttributesChanged(objectId)` after atomic replacement of the cached list.

## Confidence

- **Confirmed:** command name, pending batch construction, response opcode/schema, executable listener/parser, cache keyed by U64 ID, vector element shape, freshness field, invalidation/re-request behavior, event type/subscribers, and current server validation/producer.
- **Strongly inferred:** exact scheduling cadence of the batch flush and some auxiliary suppression-set semantics.
- **Unknown:** original source member names, historical meaning of every formatting category, and exact intended cache lifetime in seconds.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `object_attribute_addresses.txt`
- `decompiled-object-attributes/`
- `MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h`
- `MMOCoreORB/src/server/zone/objects/creature/commands/GetAttributesBatchCommand.h`

