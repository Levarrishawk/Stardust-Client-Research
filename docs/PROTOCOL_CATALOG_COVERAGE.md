# Protocol Catalog Cross-Coverage

Date: 2026-09-11  
Executable: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Mechanical lower bound

`tools/protocol_catalog.py` compares message-like ASCII identifiers embedded in the executable with packet class names declared under the current Stardust server `MMOCoreORB/src` tree.

| Inventory | Count |
|---|---:|
| Distinct current-server class names ending in `Message` or `Msg` | 114 |
| Distinct executable ASCII identifiers ending in `Message` or `Msg` | 297 |
| Exact name overlap | 77 |
| Exact overlap as a fraction of this server class inventory | 67.54% |

This is a **name-evidence lower bound**, not handler coverage. It proves that at least 77 current server packet-class names have an exact semantic anchor in `Stardust.exe`. It does not prove each string is registered, reachable, or compatible field-for-field. Conversely, a missing exact match is not evidence of incompatibility: server convenience names differ from original wire names (`BaseLineMessage` versus `BaselinesMessage`, `DeltaMessage` versus `DeltasMessage`, `UpdateCellPermissionsMessage` versus singular client name), and many callbacks build opcodes without a class ending in `Message`.

## Exact-overlap families

The 77 exact matches include:

- session/permissions/error and zone connection responses;
- scene containment, time, transforms and client effects;
- trade and auction lifecycle;
- chat room/system/instant/persistent messaging;
- SUI, ticket, holocron and browser/UI messages;
- resource survey and mission-browser messages;
- structure placement and cell permissions;
- space projectile, missile, nebula, component and ship-transform messages.

This breadth strongly supports protocol recovery from binary plus source without rediscovering most message identities through packet interception. The remaining work is principally `name/opcode -> parser -> handler -> state effect`, not blind discovery of all names.

## Server-only exact-name misses

There are 37 names in this bounded server-class inventory without an exact executable string. They fall into three categories:

1. **Known naming aliases:** `BaseLineMessage`, `DeltaMessage`, `SceneObjectCreateMessage`, `SceneObjectDestroyMessage`, `ClientIdMessage`, `UpdateCellPermissionsMessage`, and `PostureMessage` have already-correlated client names or generic envelopes.
2. **Likely internal envelope/controller names:** `ObjectControllerMessage`, `StandaloneObjectControllerMessage`, light transforms, and connection-test helpers may be decoded by numeric/controller dispatch rather than a literal class name.
3. **Genuine follow-up candidates:** buffs, hyperspace phase messages, harvester data, group space invitations, quest task counters/timers, nearby-player and PvP updates.

These 37 must be checked by opcode and construction layout before being called absent from the client.

`SERVER_NAME_MISS_RESOLUTION.md` now accounts for all 37: 31 are correlated to an executable alias, route, ObjectController identity, or executable class alias, while 6 retain only a known server wire schema. This gives a 108/114 (94.74%) **identity-resolved lower bound**, which must not be confused with schema or behavior completeness.

## Executable-only message-like strings

There are 220 executable names not represented by an exact server packet-class name in the scanned tree. This set includes original client messages unused by current Stardust, internal `MessageDispatch` event names, diagnostic/static variable labels, and server features expressed through callbacks rather than identically named classes. It is therefore a research queue, not 220 required protocol implementations.

## Coverage interpretation

- **Confirmed:** counts and exact string overlap for the specified executable and checked-out source tree.
- **Strong inference:** the server source can directly name or structurally correlate a clear majority of replacement-relevant protocol families.
- **Unknown:** reachable handler count across all listener/controller classes and the deployed use frequency of long-tail messages.

Do not roll the 67.54% figure into “percent of client reverse engineered.” It measures only exact-name intersection against one mechanically defined server packet-class catalog.

## Next analysis

1. Resolve the 37 server-only names by opcode aliases and controller dispatch.
2. Inventory named-message checks in login, chat, UI and controller listeners.
3. Join exact names to literal opcodes and server constructor layouts.
4. Promote rows to `PACKET_HANDLER_MAP.md` only after a direct client route is established.
