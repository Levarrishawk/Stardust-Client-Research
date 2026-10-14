# Server-to-Client Object-Controller Emissions

Baseline: current local Stardust-3 source correlated with `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`) on 2026-09-11.

## Static inventory result

`tools/server_object_controller_usage.py` found 71 direct construction sites representing **51 distinct constant controller types**. Of those, **50/51 (98.04%)** have a recovered Stardust.exe factory registration. This is a lower-bound inventory: the generic `ObjCtrlMessageQueueString(type)` wrapper and any dynamically selected types are deliberately not guessed.

| Type | Server producer/purpose | Priority | Client inbound factory | Status |
|---:|---|---:|---:|---|
| `0x71` | `DataTransform` | `0x1B` | `0x00AD4860` | Registered |
| `0xCC` | `CombatAction` | `0x1B` | `0x0119C330` | Registered |
| `0xDD` | start NPC conversation | `0x0B` | `0x00B12F20` | Registered |
| `0xDE` | stop NPC conversation | `0x0B` | `0x00B12BD0` | Registered |
| `0xDF` | NPC conversation message | `0x0B` | `0x00B14FF0` | Registered |
| `0xE0` | conversation string list | `0x0B` | `0x00B123A0` | Registered |
| `0xEA` | harvester resource data | `0x0B` | `0x00B17F00` | Registered |
| `0xEE` | generic response | `0x0B` | `0x00B191F0` | Registered |
| `0xF1` | parent-relative transform | `0x1B` | `0x00AD43B0` | Registered |
| `0xF2` | animation | `0x1B` | `0x00B12880` | Registered |
| `0xF4` | spatial chat | `0x0B` | `0x00AEF2C0` | Registered |
| `0xFA` | mission accept response | `0x0B` | `0x00B165D0` | Registered |
| `0x102` | crafting session response | `0x0B` | `0x00AF6D00` | Registered |
| `0x103` | crafting session response | `0x1B` | `0x00AF7280` | Registered |
| `0x10C` | crafting status/step | `0x1B` | `0x00B193B0` | Registered |
| `0x113` | crafting status/step | `0x1B` | `0x00B193B0` | Registered |
| `0x116` | command queue enqueue | `0x0B` | `0x00AE3EF0` | Registered |
| `0x117` | command queue remove | `0x1B` | `0x00AE3850` | Registered |
| `0x12E` | emote | `0x0B` | `0x00AF3E70` | Registered |
| `0x131` | posture | `0x1B` | `0x00B11F50` | Registered |
| `0x134` | combat spam | `0x1B` | `0x00AEED70` | Registered |
| `0x13B` | sit on object | `0x1B` | `0x00ACE300` | Registered |
| `0x140` | weapon ranges | `0x1B` | `0x00B11BB0` | Registered |
| `0x142` | mission abort response | `0x0B` | `0x00ADE520` | Registered |
| `0x147` | object-menu response | `0x0B` | `0x00AF35F0` | Registered |
| `0x166` | flourish | `0x1B` | `0x01194F20` | Registered |
| `0x167` | command queue add | `0x0B` | `0x01195040` | Registered |
| `0x1BD` | fly text | `0x1B` | `0x00B13280` | Registered |
| `0x1BE` | crafting status/step | `0x1B` | `0x00B193B0` | Registered |
| `0x1BF` | draft schematic response | `0x0B` | `0x00B19CB0` | Registered |
| `0x1C2` | crafting status/step | `0x1B` | `0x011951C0` | Registered |
| `0x1DB` | biography | `0x1B` | `0x01196180` | Registered |
| `0x1E7` | players near you | `0x0B` | `0x01196340` | Registered |
| `0x1FC` | starting location list | `0x1B` | `0x011967B0` | Registered |
| `0x207` | draft schematic response | `0x0B` | `0x00B13C30` | Registered |
| `0x229` | add buff | `0x1B` | `0x00AC81C0` | Registered |
| `0x22A` | remove buff | `0x1B` | `0x00AC80C0` | Registered |
| `0x22B` | harvester setup | `0x1B` | `0x00AC8310` | Registered |
| `0x238` | image-design change | `0x1B` | `0x00B0B990` | Registered |
| `0x239` | image-design rejection | `0x1B` | `0x00B0B990` | Registered |
| `0x23A` | image-design start | `0x1B` | `0x00B11260` | Registered |
| `0x3FE` | begin hyperspace | `0x1B` | `0x01195B30` | Registered |
| `0x41D` | JTL ship-list response | `0x1B` | `0x01196A60` | Registered |
| `0x42D` | orient for hyperspace | `0x1B` | `0x01195B30` | Registered |
| `0x432` | ship-hit event | `0x1B` | `0x00B107A0` | Registered |
| `0x437` | group-member space invite | `0x1B` | `0x01195890` | Registered |
| `0x439` | group-member invite response | `0x1B` | `0x01196EE0` | Registered |
| `0x43C` | open loot-lottery window | `0x0B` | `0x01195890` | Registered |
| `0x43D` | close loot-lottery window | `0x0B` | `0x01195890` | Registered |
| `0x441` | server labels quest-task counter | `0x0B` | `0x00B0F0D0` | **Registered to the client's timer class, not counter** |
| `0x444` | server labels quest-task timer | `0x0B` | — | **No factory registration recovered** |

## Quest counter/timer compatibility finding

The executable's registrations and RTTI associations are:

| Client type | Client typed class | Outbound factory | Inbound factory | Registration site |
|---:|---|---:|---:|---:|
| `0x440` | `MessageQueueQuestTaskCounterMessage` | `0x00B0F630` | `0x00B0F6F0` | `0x00B0F3B6` |
| `0x441` | `MessageQueueQuestTaskTimerMessage` | `0x00B0F020` | `0x00B0F0D0` | `0x00B0EDA6` |

This mapping is confirmed by the class-specific memory-manager strings, RTTI descriptors/vtables, factory construction calls, and the immediate type passed to `obj_controller_factory_register_VA_00B0CFC0`. No direct `0x444` factory registration exists in the recovered 139-entry registry.

The server emits its quest counter as `0x441` with five fields and exposes a timer as `0x444` with four fields. The client expects its four-field timer at `0x441` and its five-field counter at `0x440`. Therefore the IDs are not merely missing; they are shifted/mismatched:

- **Confirmed:** a server `0x441` counter will be decoded through the client's timer codec, which consumes only the timer-shaped prefix and cannot represent the trailing current-count field.
- **Strong:** server `0x444` reaches the unknown-inner-type path because no registration was recovered.
- **Not currently demonstrated:** timer traffic in ordinary gameplay, because no screenplay calls `Journal.timer()` beyond its wrapper definition. Counter traffic is available through `Journal.counter()` and should be audited separately for active calls.

Treat this as a confirmed protocol-definition mismatch in optional quest-journal functionality and a requirement for the replacement client's Stardust-compatibility profile, not as evidence that login or normal scene entry is blocked.

## Implications

The 98.04% numeric intersection is strong evidence that Stardust.exe and this server tree share the intended controller vocabulary. It does not measure field-schema completeness. Each registered type still needs directional field decoding and an effect contract before feature-complete replacement.

For a narrow login/network proof of concept, none of these 51 inner messages is required before authentication and the first known response. For scene-entry proof, `0x71`, `0xF1`, `0x116`, `0x117`, `0x131`, and common social/UI messages become high-priority after object creation/baselines are in place.

Field-level layouts for common combat, conversation, command, posture, social, and quest types are in `CORE_OBJECT_CONTROLLER_SCHEMAS.md`. Harvester, JTL, loot, image-design, and crafting layouts are in `EXTENDED_OBJECT_CONTROLLER_SCHEMAS.md`.

## Round64 implementation boundary

[Directional full transform codec](OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md) implements the documented0x71/0xF1 emissions and current-server request grammar. Server constructors still emit zero outer word; Transform.h request parsers consume U32 timestamp bits. Existing intersection/packet coverage is unchanged. No live forwarding or world effect is established by offline bytes.

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

## Flytext payload closure, October14

Original executable identity remains SHA256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150, checked before static VA use. Existing tools/pe_x86_probe.py with the already installed private pefile/Capstone packages was reused; no original executable ran. Registered0x1BD unpack wrapper00B13280 calls00B1DE60. Reads at00B1DE9F/00B1DEA9 consume NetworkId and StringId; checked fixed reads at00B1DEBC/00B1DED6/00B1DEEF/00B1DF08/00B1DF21 consume4/1/1/1/1 bytes. There is no intervening Unicode read. Constructor00B132A0 owns the NetworkId/StringId/scalars and initializes the64-byte legacy queue object; its destructor releases StringId state. Registered pack wrapper00B13260 calls00B1E0A0:00B1E0DD/00B1E0E7 write NetworkId/StringId,00B1E0F2 writes4 bytes, then00B1E105/00B1E118/00B1E12B/00B1E13E write one byte each. Wrong-data-type packing reports and returns without a normal payload. Read failures route through the characterized Archive bounds exception path. No new provisional semantic names are assigned here.

**Confirmed grammar:** little-endian U64 emitter ID; StringId as ASCII byte-string table/U32 index/ASCII byte-string key; F32 scale; U8 red/green/blue; U8 flags. String byte-length conventions inherit the established StringId codec. Flags are preserved raw; no new gameplay meaning is assigned to their bits. Current Core3 MMOCoreORB/src/server/zone/packets/object/ShowFlyText.h::ShowFlyText independently emits precisely these fields under outer0x80CE5E46/type1BD/priority1B with zero outer scalar. No original asset corpus occurrence is required for the numeric/string-field grammar; actual source-client receipt below reaches this active server-emitted path. Literal Unicode flytext is absent from this legacy codec, rather than an optional tail.

NGE source MessageQueueShowFlyTextArchive instead reads a Unicode string between StringId and scale and uses U32 flags. The explicit source Pre-CU profile now omits Unicode, promotes U8 flags and rejects trailing bytes. Unsupported outbound literal Unicode rejects before bytes are appended; NGE localized/literal paths remain. UnicodeArchive independently checks count against remaining bytes/UTF16 width before assignment, avoiding unchecked large allocation/overflow. This is a modern malformed-input guard, not original failure equivalence. Synthetic real-production tests cover localized Pre-CU/NGE round trips, NGE literal text, rejected Pre-CU literal text, required-prefix truncations and excess tails; Unicode tests preserve embedded zero/unpaired units and reject excessive lengths before target mutation. Latest source suite8568 checks/1990 malformed rejections plus five isolated CRC/ACST failures passes.

Existing packet-handler/capability inventory and345-row server gap matrix are unchanged: ShowFlyText was already documented/registered. This closes its payload observation; it does not change the24 client-originated active-controller count or function/research/readiness denominators. [Runtime continuation and remaining hang](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_ZONE_ENTRY.md).


## Combat-spam payload closure, October14

[Source-client codec contract](NGE_PRECU_COMBAT_SPAM_CONTRACT.md) directly confirms the0x134 reader and original color jump table against the same hash-verified executable. The new first-throw receipt identifies the NGE combat-spam decoder on an80-byte object-controller application. Original wire begins three U64 IDs, damage, StringId and U8 color, with current Core3's Unicode tail; it has no NGE discriminator/position/attack-detail prefix. Native fixtures exercise the actual registered controller route and retained NGE branches. Existing0x134 emission inventory and345 packet rows are unchanged. Live correction is pending; historical generic fault attribution remains strongly inferred.


## October14 spatial-chat codec refinement

[Spatial-chat contract](NGE_PRECU_SPATIAL_CHAT_CONTRACT.md) closes original0xF4 flags width/order against direct reader/writer probes and resolves a new172-byte application failure in the source NGE codec. The explicit Pre-CU adapter preserves owned OOB/text and bounds only the known eight-zero current-producer tail. Native11439/3538 plus five isolated cases pass; naturally received structured spatial chat is decoded in the rendered world. Nested presentation/outgoing acceptance remain open. The existing SpatialChat matrix row remains documented;345 source rows and all coverage denominators remain unchanged.
