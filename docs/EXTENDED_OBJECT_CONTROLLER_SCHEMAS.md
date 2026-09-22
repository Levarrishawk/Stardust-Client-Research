# Extended Object-Controller Schemas

Baseline: current local Stardust-3 source correlated with `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`) on 2026-09-11.

This catalog covers lower-frequency server-to-client `ObjectControllerMessage` payloads not expanded in `CORE_OBJECT_CONTROLLER_SCHEMAS.md`. Offsets begin after the common controller envelope and include its leading `U32` scalar. Integers and floats are little-endian; `ASCII` and `Unicode` use the engine's length-prefixed encodings. These schemas come from current server producers. A matching executable factory confirms recognition of the numeric type, but not by itself the client-side semantic label.

## Harvesting

### `0xEA` — harvester resource data

`U32 scalar`, `U64 harvesterId`, `U32 resourceCount`, followed by `resourceCount` records: `U64 resourceId`, `ASCII resourceName`, `ASCII resourceType`, `U8 densityPercent`.

The density is a byte-scale percentage, not a float. **Schema: confirmed. Client effect: strongly inferred as harvester resource-list population.**

### `0x22B` — harvester shared-network setup

`U32 scalar` only; the current producer has no type-specific body. **Confirmed.** It is a client-side setup trigger rather than a data-bearing message.

## Space/JTL

### `0x41D` — ship-list response

`U32 scalar`, `U32 entryCount`, `U64 terminalId`, `ASCII currentTravelPoint`, then zero or more records: `U64 shipId`, `ASCII cityOrTravelPoint`.

The server writes `entryCount = shipMap.size() + 1`; the leading terminal/travel-point pair therefore appears to occupy the extra logical entry despite its different shape. **Bytes: confirmed. Count interpretation: strongly inferred.** Preserve this heterogeneous first-entry convention until client parsing is independently recovered.

### `0x3FE` — begin hyperspace

`U32 scalar`, `ASCII destinationZone`, `F32 x`, `F32 z`, `F32 y`. The nonstandard coordinate order is exactly what the server emits. **Confirmed.**

### `0x42D` — orient for hyperspace

`U32 scalar`, `U16 9`, `U32 0xCA2A548B`, `ASCII destinationZone`, `F32 x`, `F32 z`, `F32 y`.

The constant resembles a nested message discriminator; its exact client contract is not proven and it must not be discarded as padding. **Bytes: confirmed. Meaning of constants: unknown.**

### `0x432` — ship hit

`U32 scalar`, `F32 localHitX`, `F32 localHitY`, `F32 localHitZ`, `S32 shipHitType`, `F32 newPercent`, `F32 oldPercent`.

The producer clamps each local hit coordinate before serialization. The last two values are post-hit then pre-hit. **Confirmed.**

### `0x437` / `0x439` — group-member space invite / response

- `0x437`: `U32 scalar`, `U64 pilotId`, `U32 0`, `U64 invitedMemberId`.
- `0x439`: `U32 scalar`, `U64 memberId`, `U8 decision`, `U64 pilotId`.

**Confirmed.**

## Loot lottery

### `0x43C` / `0x43D` — open / close lottery window

Both use `U32 scalar`, `U64 containerOrInventoryId`. **Confirmed.**

## Image design

- `0x23A` begin: `U32 scalar`, `U64 designerId`, `U64 targetId`, `U64 tentId`, `ASCII holoEmote`.
- `0x239` reject: `U32 scalar`, `U64 designerId`, `U64 targetId`, `U64 tentId`, `U8 rejectionType`.
- `0x238` server-to-client change: `U32 scalar`, `U64 designerId`, `U64 targetId`, `U64 tentId`, `U8 changeType`.

All are **confirmed**. `0x238` is directionally asymmetric: the client-to-server form appends the much larger `ImageDesignData` record documented in `ACTIVE_OBJECT_CONTROLLER_SCHEMAS.md`. A codec keyed only by numeric type will be wrong.

## Crafting

### `0x102` — available schematic list

`U32 scalar`, `U64 craftingToolId`, `U64 craftingStationId` (zero when absent), `U32 schematicCount`, then `schematicCount` records: `U32 schematicCrc`, `U32 repeatedSchematicCrc`, `U32 toolTab`.

The duplicated CRC is intentional in the current producer. **Confirmed.**

### `0x103` — selected schematic and ingredient slots

`U32 scalar`, `U64 craftingToolId`, `U64 manufactureSchematicId`, `U64 prototypeId`, `U32 constant2`, `U8 allowFactory`, `U32 slotCount`, followed by `slotCount` variable records, then `U16 0`.

Each slot record contains:

1. `ASCII stringIdFile`, `U32 0`, `ASCII stringIdName`, `U8 optional`, `U32 1`;
2. the same `ASCII file`, `U32 0`, `ASCII name` triplet again;
3. `Unicode resourceType`, `U8 slotEncoding`, `U32 quantity`;
4. when `slotEncoding == 2`, an additional `U16 0`.

Observed encodings are `2` identical component, `4` resource, and `5` mixed component. Optionality is separate. **Confirmed from `DraftSlot::insertToMessage`.**

### `0x1BF` — draft schematic slot definition

`U32 scalar`, `U32 schematicCrc`, `U32 repeatedSchematicCrc`, `U32 complexity`, `U32 size`, `U8 constant2`, followed by the same `U32 slotCount`, variable slot records, and trailing `U16 0` defined for `0x103`. **Confirmed.**

### `0x207` — resource weight definitions

`U32 scalar`, `U32 schematicCrc`, `U32 repeatedSchematicCrc`, `U8 batchRowCount`, then `batchRowCount` packed rows, `U8 normalRowCount`, then `normalRowCount` packed rows.

Each row begins with `U8 propertyCount`, followed by that many packed bytes. A byte is `(propertyType << 4) | weightNibble`. Batch rows force the low nibble to `1`; normal rows use configured weight modulo 16. A filler row is encoded as a zero count byte. Known high-nibble property codes: `0` PO/unknown, `1` CR, `2` CD, `3` DR, `4` HR, `5` FL, `6` MA, `7` PE, `8` OQ, `9` SR, `A` UT, `B` BK. **Confirmed.**

### Status/result envelopes: `0x10C`, `0x113`, `0x1BE`, `0x1C2`

- `0x10C`: `U32 scalar`, `U32 subcode`, `U32 resultOrFlag`, `U8 clientCounter`. Confirmed subcodes include `0x107` slot error, `0x108` ingredient removal, `0x10A` hopper/update state, `0x10B` manufacture-schematic transition, `0x10F` start failure, and `0x15A` completion transition.
- `0x113`: `U32 scalar`, `U32 0x105`, `U32 experimentationResult`, `U8 clientCounter`.
- `0x1BE`: `U32 scalar`, `U32 0x109`, `U32 assemblyOrStageResult`, `U8 clientCounter`.
- `0x1C2`: `U32 scalar`, `U8 clientCounter`; this closes the crafting window in the current server.

Fields are **confirmed from producers**. Human meanings of every result enumeration remain dependent on server enums and client UI behavior.

## Replacement-client implications

1. Dispatch ObjectController on `(direction, type)`, then allow a second discriminator such as crafting `subcode`.
2. Bound every count and string before allocation; several formats contain heterogeneous or variable records.
3. Wire widths remain fixed in an x64 client. Never serialize native pointers, `size_t`, enum storage, or Godot `Variant` representations.
4. Preserve actual coordinate order even if internal math uses conventional `(x,y,z)` vectors.
5. Repeated CRCs, constants, and trailing zeros remain protocol fields until executable parsing proves otherwise.
