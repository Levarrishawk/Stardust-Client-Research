# Analysis Checkpoint: Player Directory Requests

Three player-service request rows are closed.

| Request / opcode | Client route | Body | Resulting server behavior |
|---|---|---|---|
| `FactionRequestMessage` / `0xC1B03B81` | caller `0x00F9FBC0` → constructor `0x013F6660` → reliable send | none | serialize the session player's faction-standing list in `FactionResponseMessage` |
| `GuildRequestMessage` / `0x81EB4EF7` | callers `0x00E730B0` and `0x00FA1280` → constructor `0x013F4A10` → reliable send | U64 creature object ID | resolve the creature and return its guild data in `GuildResponseMessage` |
| `PlanetTravelPointListRequest` / `0x96405D4D` | ticket activation `0x00D84500` → constructor `0x013F1BF0` → reliable send | U64 player ID; ASCII planet/zone | ignore redundant transmitted player ID, use session identity, and return enabled travel points for the zone |

The travel request was already fully documented and is reconciled here. The faction and guild classes are independently named in the executable, and their constructors/callers align with the server callback schemas.

## Confidence

Identities, opcodes, field contracts, construction routes, and server actions: **Confirmed**.

