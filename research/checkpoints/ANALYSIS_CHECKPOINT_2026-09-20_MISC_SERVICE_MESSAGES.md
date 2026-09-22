# Analysis Checkpoint: Miscellaneous Service Messages

## Commodities item-type catalog

`CommoditiesItemTypeListResponse` (`0xD4E937FC`) enters the auction listener at branch `0x006A85BC`. The client validates/deserializes the response's catalog identity and item-type container, then applies it through `0x004CF5D0`. Current Stardust returns ASCII `Core3.0` and U32 zero, an empty catalog.

## Holocron ticket submission

`CreateTicketMessage` (`0x40E64DAC`) is constructed at `0x00ADFA90` from UI routes `0x005CAF30` and `0x013B8F20` and sent reliably. Stardust consumes the compatible prefix: ASCII player name, U32 category ID, U32 subcategory ID, and Unicode ticket body. The legacy message registers additional metadata after that prefix; current Stardust ignores it and submits only the body when tickets are enabled.

## Galaxy loop timing

`RequestGalaxyLoopTimes` has no body. `GalaxyLoopTimesResponse` (`0x4E428088`) is recognized by the network-status listener at `0x00C7D1E0` and parsed at `0x013F0D80` as two U32 values. The server wrapper writes three zero U32 values, so its trailing value is ignored. Current Stardust deliberately does not send it because doing so causes repeated requests. This dormant diagnostic transaction is not needed for the networking proof of concept.

## Confidence

Identities, opcodes, client branches/parsers, and current server behavior: **Confirmed**. Loop-value meanings and legacy ticket fields after the consumed prefix remain **Unknown/bounded follow-up**.

