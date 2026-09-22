# Draft Schematic Manager and Asset Bridge

## Scope

This block closes how the player's replicated draft-schematic identities become client-side schematic objects, how additions/removals propagate, and why the original draft/crafted object templates are mandatory inputs to crafting presentation.

## Replicated identity

`PLAY:9[4]` is an indexed delta vector whose element is exactly two U32 values. Current Stardust emits them in this order:

1. `DraftSchematic::objectCRC` — CRC of the shared draft-schematic object template;
2. `DraftSchematic::schematicID` — stable schematic identifier.

The baseline and delta codecs, including update counter and five indexed-vector operations, were closed previously. This block traces their consumer.

## Manager lifecycle and event path

`draft_schematic_manager_install_VA_004EBE60` owns three typed subscriptions:

- `PlayerObject::Messages::DraftSchematicAdded` carrying `(PlayerObject*, (U32,U32))`;
- `PlayerObject::Messages::DraftSchematicRemoved` with the same payload;
- `CreatureObject::Messages::PlayerSetup` carrying the local `PlayerObject`.

On player setup, `draft_schematic_manager_rebuild_VA_004EC3F0` discards the old derived list, obtains the player's replicated pair vector, constructs one 0x3c-byte `DraftSchematicInfo` per entry, and sorts the resulting pointer list by localized/display name. Add and remove callbacks update this derived collection and publish `DraftSchematicManager::Messages::Changed` with the local creature. `SwgCuiContainerProviderDraft` is the confirmed presentation subscriber.

The manager is therefore a derived asset-backed view of authoritative `PLAY:9[4]` state; it is not another network cache.

## DraftSchematicInfo resolution

`draft_schematic_info_resolve_VA_006B5670` lazily resolves the entry only when its client object is absent:

```text
replicated objectCRC
    -> client template registry lookup by CRC
    -> require SharedDraftSchematicObjectTemplate
    -> fetch its craftedSharedTemplate
    -> instantiate a client object from the crafted template
    -> end-baselines/initialize presentation object
    -> obtain appearance/customization and schematic metadata
```

If the draft template, crafted shared template, or object construction fails, the executable warns with the exact template identity and constructs a fallback placeholder object so UI code can remain operational. A resolved object can then be found by object pointer, replicated pair, or display ordering.

This is direct evidence that server replication is intentionally compact: the server supplies identities, while ingredient slots, schematic attributes, crafted-object appearance, localized name, customization, and much of crafting presentation come from original client assets.

## Original asset dependencies

The client requires at least:

- `SharedDraftSchematicObjectTemplate` files addressed by CRC;
- the referenced crafted shared object template;
- object-template inheritance/property decoding;
- localized object names through STF;
- appearance and customization data for the crafted preview;
- ingredient-slot and schematic-attribute arrays embedded in the draft template;
- resource weight/maximum data consumed by `DraftSchematicInfo`.

The diagnostic path explicitly compares schematic attribute count with resource-weight count and assembly weights with maximum-resource-weight count. A replacement loader must validate these parallel arrays before exposing the schematic.

## Current server correlation

`PlayerObjectMessage9` and `PlayerObjectDeltaMessage9` emit `objectCRC` then `schematicID`. The server's `SchematicMap`, `DraftSchematicTemplate`, and crafting session retain richer server-side rules, but they do not transmit a complete client presentation definition in `PLAY:9`. This makes the client template corpus non-optional even if protocol compatibility is perfect.

The later crafting-session controller messages (`0x1BF`, `0x207`, and related queue objects) operate on the selection/session after this player-known schematic catalog is available; they do not replace the asset-backed catalog.

## Replacement requirements

- Preserve both U32 identities exactly and key derived records by the pair, not by native pointer.
- Resolve object-template CRCs through the original VFS/template registry.
- Lazily construct preview/presentation objects; a headless protocol client may retain only parsed metadata.
- Make the derived list immutable per rebuild or update it atomically before publishing `Changed`.
- Preserve deterministic display-name sorting after localization.
- Surface a bounded placeholder plus diagnostics for missing assets rather than dereferencing null.
- Validate all parallel attribute/weight arrays and resource slot references.

## Confidence

- **Confirmed:** pair order and width, replication source, three manager subscriptions, rebuild/add/remove topology, derived info size, CRC template lookup, required draft-template type, crafted-template fetch, object instantiation, fallback behavior, sorting criterion, and UI subscriber.
- **Strongly inferred:** some detailed member labels within the 0x3c-byte info object and exact fallback template identity.
- **Unknown:** historical source names for individual cached members and all per-template content semantics outside the already decoded template families.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`
- `draft_schematic_addresses.txt`
- `decompiled-draft-schematic/`
- `MMOCoreORB/src/server/zone/packets/player/PlayerObjectMessage9.h`
- `MMOCoreORB/src/server/zone/packets/player/PlayerObjectDeltaMessage9.h`
- `MMOCoreORB/src/server/zone/objects/player/variables/SchematicList.*`

