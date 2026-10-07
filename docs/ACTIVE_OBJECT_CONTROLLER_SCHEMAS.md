# Active Object-Controller Schemas

Round89 clarification of the historical0x13F row: “occupies scalar position” describes the current server callback cursor, not a recovered client packer overlay. The executable typed serializer emits four counter bytes and the actual sender queues a separate scalar0. [Lifecycle evidence](REPLACEMENT_TELEPORT_ACK_LIFECYCLE.md) preserves that discrepancy and the unclosed final wire extent. Fresh server scene-ready also clears teleporting; it is distinct from the full-transform-triggered ACK.

Baseline: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; correlated against the local Stardust-3 server tree on 2026-09-11.

## Scope and cursor convention

This document describes the 24 inner controller types currently registered by `ZonePacketHandler::registerObjectControllerMessages()`. The server's top-level `ObjectControllerMessageCallback::parse` consumes priority, type, and object ID, then invokes the selected callback with the cursor at outer offset `0x16`. Thus an inner callback's initial `parseInt()` or `shiftOffset(4)` consumes the four-byte scalar at `0x16`; subsequent fields begin at `0x1A` unless stated otherwise.

All integer fields are little-endian. `ASCII16` means U16 byte length followed by bytes; `Unicode32` means U32 UTF-16 code-unit count followed by little-endian UTF-16 units. Count fields must be bounded before allocation or iteration in a replacement client.

## Client-to-server schemas and effects

| Type | Callback | Payload from outer `0x16` | Server result | Confidence |
|---:|---|---|---|---|
| `0x71` | `DataTransformCallback` | movement payload documented in `OBJECT_CONTROLLER_PROTOCOL.md` and `PACKET_HANDLER_MAP.md` | validates and applies world-relative creature movement | Confirmed |
| `0xED` | `EmptyHopperCallback` | 12 skipped bytes (scalar plus passed-player/unknown area), U64 harvester, U64 resource, U32 quantity, U8 mode (`0` retrieve, `1` discard), U8 correlation/check byte | validates installation/admin/range/resource and retrieves or discards hopper contents; replies `GenericResponse(0xED,1,correlation)` | Confirmed fields/effect; skipped eight bytes and correlation label Unknown |
| `0xF1` | `DataTransformWithParentCallback` | parent-relative movement payload documented in `OBJECT_CONTROLLER_PROTOCOL.md` and `PACKET_HANDLER_MAP.md` | validates and applies cell/parent-relative creature movement | Confirmed |
| `0xF5` | `MissionListRequestCallback` | U32 scalar, U8 flags, U8 sequence, U64 terminal ID | validates mission terminal and requests a mission list keyed by sequence | Confirmed |
| `0xF9` | `MissionAcceptCallback` | U32 scalar, U64 mission ID, U64 terminal ID, U8 terminal index | validates terminal/mission, accepts mission, replies with controller type `0xFA` carrying mission ID, success byte, terminal index | Confirmed |
| `0x106` | `CraftingExperimentCallback` | U32 scalar, U8 client counter, U32 row count, repeated `{U32 row, U32 points}` | applies requested experiment points to active crafting session | Confirmed |
| `0x107` | `CraftingAddIngredientCallback` | U32 scalar, U64 ingredient ID, U32 slot, U32 unknown, U8 counter | adds ingredient to slot in active crafting session | Confirmed fields/effect; unknown U32 Unknown |
| `0x108` | `CraftingRemoveIngredientCallback` | U32 scalar, U32 slot, U64 ingredient ID, U8 counter | removes ingredient from active pre-assembly crafting session | Confirmed |
| `0x115` | `ItemDropTradeCallback` | U32 scalar, then `MessageQueueSecureTrade {U32 operation=0,U64 invalid/source=0,U64 target player}` for C→S request; server skips scalar + operation + first ID and consumes target | establishes reciprocal trade targeting; creates paired sessions and sends `BeginTradeMessage` when both players target each other | Confirmed |
| `0x116` | `CommandQueueEnqueueCallback` | U32 scalar/action timer, U32 action count, U32 command CRC, U64 target ID, Unicode32 arguments | resolves command priority and enqueues command after rate checks | Confirmed |
| `0x117` | `CommandQueueRemoveCallback` | U32 scalar, then client codec `{U32 action count, F32 timer, U32 tab1, U32 tab2}`; current client cancellation fills the final three with zero, while the server parser consumes action count and the zero timer bits as an unused command CRC and leaves two trailing U32s | removes/cancels queued action | Confirmed executable C→S bytes and server partial consumption; S→C uses all four typed fields |
| `0x126` | `TargetUpdateCallback` | U32 scalar, U64 target ID | validates target and updates player or piloted-ship target | Confirmed |
| `0x13F` | `TeleportAckCallback` | U32 movement counter (occupies scalar position) | clears the player's teleporting flag | Confirmed field width/effect; relationship of counter to movement ordering not used by current server |
| `0x142` | `MissionAbortCallback` | U32 scalar, U64 mission ID | aborts mission and replies with controller type `0x142` carrying the mission ID | Confirmed |
| `0x146` | `ObjectMenuRequestCallback` | U32 scalar/unknown size, U64 object ID, U64 player ID, U32 radial count, repeated `{U8 index, U8 parent, U8 radial type, U8 callback, Unicode32 command}`, U8 counter | validates object/request and builds server radial-menu response/actions | Confirmed layout; scalar meaning Unknown |
| `0x15A` | `CraftingCustomizationCallback` | U32 scalar, Unicode32 name, U8 template choice, U32 schematic count, U8 pair count, repeated `{U32 count, U32 value}` | applies name/template/customization selections in active crafting session | Confirmed fields/effect; pair labels inherit ambiguous server names |
| `0x238` | `ImageDesignChangeMessageCallback` | U32 scalar, U64 designer, U64 target, U64 tent, U8 type, encoded `ImageDesignData` (below) | updates active image-design session | Confirmed |
| `0x239` | `ImageDesignRejectMessageCallback` | U32 scalar, U64 designer, U64 target, U64 tent, U8 type, encoded `ImageDesignData` (below) | cancels active image-design session | Confirmed |
| `0x3FA` | `UpdateShipTargetSlotCallback` | U32 scalar/size, U32 target component slot | validates piloted ship and selects its targeted component | Confirmed target slot/effect; scalar label Unknown |
| `0x41C` | `JtlShipListRequestCallback` | U32 scalar, U64 terminal ID | range-validates terminal and sends ship list response | Confirmed |
| `0x435` | `DroidCommandProgrammingCallback` | U32 scalar, U32 command count, repeated ASCII16 command, U32 add-count, repeated U64 module ID, U32 drop-count, repeated U64 module ID, U64 control-device ID | programs commands/modules on the specified droid control device | Confirmed |
| `0x436` | `GroupMemberSendSpaceInviteCallback` | U32 scalar, U64 group-member ID | validates same parent/range and forwards a space invite to member | Confirmed |
| `0x438` | `GroupMemberSpaceInviteResponseCallback` | U32 scalar, U64 pilot ID, U8 decision | validates same parent/range and forwards decision to pilot | Confirmed |
| `0x43F` | `LotteryWindowCallback` | U32 scalar, U64 container ID, U32 item count, repeated U64 item ID | records/handles loot-lottery item selection for container/session | Confirmed layout; exact higher-level lottery state transition Strong |

## Directional asymmetry

Controller type is not a sufficient schema key: direction also matters. At minimum:

- `0x117` uses one four-field client codec in both directions: `{actionCount, F32 timer, U32 tab1, U32 tab2}` after the scalar. The client cancellation sender emits `{actionCount, 0.0, 0, 0}`. Current server C→S parsing consumes only `actionCount` and the zero timer bits under its unused `actionCRC` member, leaving the two trailing zero U32s; its S→C constructor supplies meaningful timer/tab values that the client uses.
- `0x142` is accepted C→S as a mission abort request and also emitted S→C as an abort response containing a mission ID.
- `0x238`/`0x239` use the same client factory pair in Stardust.exe but are different session actions on the server.

The replacement codec registry must therefore use at least `(direction, innerType)`, not `innerType` alone.

## Nested `ImageDesignData`

The server parser and serializer are symmetric. Immediately after the `0x238`/`0x239` fixed prefix, the nested payload is:

| Order | Wire field |
|---:|---|
| 1 | ASCII16 hair template |
| 2 | ASCII16 hair-customization data string |
| 3–6 | U32 session ID, timestamp, required payment, offered payment |
| 7 | U8 designer accepted |
| 8 | U32 target accepted (not U8) |
| 9 | U8 stat-migration requested |
| 10–13 | U32 body, face, markings, and hair skill modifiers |
| 14 | U32 body-attribute count, repeated `{ASCII16 attribute, F32 value}` |
| 15 | U32 color-attribute count, repeated `{ASCII16 attribute, U32 value}` |
| 16 | ASCII16 holo-emote |

This layout includes an unusual asymmetric pair (`designerAccepted` U8 versus `targetAccepted` U32); it must be preserved on the wire rather than normalized to native booleans.

## Validation requirements for a replacement

Before invoking domain logic, validate:

1. the top-level object ID equals the controlled player or permitted parent, matching current server enforcement;
2. the exact minimum payload length for the directional type;
3. all list counts against remaining bytes and explicit implementation limits;
4. every referenced object ID against type, ownership, range, and current session state;
5. crafting, trade, image-design, and mission sequence/counter values without relying on native object layouts.

## Remaining bounded questions

- Verify the now-expanded nested `ImageDesignData` fields against the Stardust.exe codec rather than server symmetry alone.
- Verify `0x115` operation-0 forwarding against a controlled server integration test; the static payload construction and server parser now agree.
- Inventory every S→C controller type actually constructed by Stardust-3 and intersect that set with the 139 client factories.
- Decode client-side factories/handlers for server-emitted types required by login and the smallest playable scene.

## Round64 root/parent movement word clarification

For0x71/0xF1, the current server consumes outer0x16 as U32 timestamp via `Transform::parseDataTransform`/`parseDataTransformWithParent`, independently of client F32 outer storage. The [directional implementation](OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md) preserves all timestamp bits CtoS, and retains a finite F32 scalar StoC before the common counter/tail. This clarifies interpretation without changing bytes or registry coverage. Original sender timestamp conversion and units remain unproved here.

## Round65 modern full-root application

The [explicit-direction application router](SCENE_WORLD_FULL_TRANSFORM_APPLICATION_CONTRACT.md) now consumes the existing full0x71 schema into the current typed authoritative motion store under explicit modern immediate-correction/zero-quaternion policy. Parent0xF1, contained, stale or ineligible targets decline; unsigned counters preserve prior issued rows on duplicates. Independent pose/serial/lifecycle tests and direct offline host correction pass. This adds no recovered schema, original interpolation/queued-ingress proof or live contact. Canonical packet-source345-row matrix and coverage counts remain unchanged.

## Round66 modern queued motion ingress

The [combined router](SCENE_WORLD_COMBINED_MOTION_INGRESS_CONTRACT.md) selects accepted compact opcodes, explicit inbound full-root/parent codecs or existing world fallback. Recognized malformed full packets propagate errors; unsigned duplicate/wrap behavior remains in existing children. Actual queued host correction and duplicate are processed before subsequent grounded steering; malformed61-byte root drains the actual world/presentation/controller prefix. No wire schema, original interpolation claim, live contact or345-row packet-matrix change follows.

## Round67 modern outbound local root construction

[Current-local builder](SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md) constructs the existing62-byte ClientToServer root grammar from exact current store-issued local typed motion. Flags, counter and timestamp bits are explicit caller inputs; native matrix conversion preserves XYZ and wire X/Z/Y without engine reflection. Nine cases/93 checks/eight exact whole-frame fixtures and sign-equivalent near180 basis checks pass. No queue/send/cadence or original sender policy is established. Wire schema and345-row matrix remain unchanged; malformed numerical guards unreachable through store admission are source-inspected rather than claimed as dynamically exercised.

## Round68 modern root FIFO ownership

[Offline queue](SCENE_WORLD_CLIENT_ROOT_MOTION_QUEUE_CONTRACT.md) owns builder-produced root arrays and essential generation/incarnation metadata. Capacity is checked before build; eligible take drops stale prefixes then transfers a same-incarnation historical frame. Typed/motion/containment refresh does not authorize current send or stale already-built bytes. Three exact frames/70 checks/six lifecycle gates and nine joined FIFO frames pass. No transport send, schema or345-row matrix change; sender cadence/units/gameplay policy remain Unknown.

## Round69 offline root queue-to-SOE admission

[Transport bridge](SCENE_WORLD_ROOT_MOTION_TRANSPORT_BRIDGE_CONTRACT.md) adopts historical root FIFO bytes and lets existing SOE copy to reliable channel0. Whole-transport pending backpressure is checked before take. Bridge owns no socket/pump/send and preserves borrowed queue/transport on disposal. Three exact application frames/five exact datagrams/five ACKs/80 component checks and nine joined transfers/ACK releases pass. Layered joined framing uses independently validated application fields/basis; rare post-take fault is source-inspected. No schema/345-row matrix change or original sender cadence/timestamp/gameplay inference.

## Round70 offline SOE application ingress

[Ingress contract](SCENE_WORLD_SOE_APPLICATION_INGRESS_CONTRACT.md) borrows current world/pump/transport, copies transport-owned applications to pump and transfers caller-owned unsent controls. Reliability may be consumed before count/byte/capacity failure; four actual failure prefixes are explicit. Five exact controls/73 component checks, two joined correction/duplicate messages and20-update later malformed-application cleanup pass. No schema/345-row matrix change, socket/send or live zone legality inference.

## Round71 peer-disconnect lifecycle proof

[Disconnect ownership](SCENE_WORLD_PEER_DISCONNECT_CONTRACT.md) uses actual frozen codec/ingress/bridge/FIFO/pump. Matching ID closes transport without closing borrowed scene; caller drains pending scene/root owners and canceled resources.39 component checks and109-update joined closure/wrong-ID failure pass. Reason bytes remain uninterpreted; no schema or345-row matrix change, socket/send/live phase claim.
