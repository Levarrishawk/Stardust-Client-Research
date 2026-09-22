# Analysis Checkpoint: Packet-Ledger Reconciliation

Three matrix gaps were stale bookkeeping rather than missing analysis:

- `DataTransform` is already closed end-to-end as ObjectController inner `0x71`, including client pack/unpack, authoritative server validation, correction handling, and remote interpolation targets.
- `ShipObjectMessage3` is already closed as the 22-field public `SHIP:3` baseline, including inherited TANO fields and ship component maps.
- `ShipObjectMessage4` is already closed as the private `SHIP:4` baseline for mass, chassis speed scalar, capacitor/booster resources, and refire efficiency.

The existing packet map and dedicated ship checkpoint reports are the evidence of record. Their server inventory rows are now marked documented. `GenericResponse` remains open because one analyzed use (`EmptyHopper`) does not establish every generic-response consumer or a single universal client handler.

Four more replication rows were already closed by tag/view analysis: `CellObjectDeltaMessage3` (`SCLT:3` field 5, cell number), `CreatureObjectMessage1` and `CreatureObjectDeltaMessage1` (`CREO:1` bank/cash plus baseline HAM and skill set), and `GuildObjectMessage6` (the one-field `GILD:6` common view). These source-class rows now point to the existing tag/view evidence. `FactoryCrateObjectDeltaMessage3` remains open pending resolution of its declared update index against the unusual `FCYT:3` field count.

The secure-trade callback rows for abort, add item, give money, accept, and unaccept were also fully closed in `TRADE_PROTOCOL.md`, including executable constructors, reliable send, exact opcodes, server callbacks, and state transitions. `SitOnObject` is closed in `CLIENT_CONTROLLER_EFFECTS.md` through controller ID `0x13B`, target resolution, seated-state scheduling, and fallback posture. Those six inventory rows are reconciled as documented.

Four derived replication rows were already closed in `REPLICATION_SCHEMA_MAP.md`: `PLAY:6` privilege-byte delta, `RCNO:3` quantity/resource fields, `RCNO:6` max-stack/type/name fields and its server padding compatibility, and `WeaponObjectMessage6` as inherited TANO-only state with no weapon-specific client registrations. Their matrix rows are reconciled below.

Five controller/support rows were also stale: command-queue add `0x167`, generic response `0xEE`, JTL ship-list response `0x41D`, ObjectMenuSelect `0x7CA18726`, and the signed-byte packed quaternion used by ship transforms are already schema-mapped in the controller, radial-menu, and ship-transform reports. Their rows now reference that existing evidence.
