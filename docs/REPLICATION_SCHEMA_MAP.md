# Replication Schema Map

This document tracks the SWG object replication boundary recovered from `Stardust.exe` and correlated with the local Stardust server source. It distinguishes the common executable dispatcher from server-defined per-object field schemas.

## Common envelopes

The [received-only CREO4 scalar kernel](CREATURE_VIEW4_PARTIAL_STATE_CONTRACT.md)
preserves explicit field availability/source provenance without a fabricated
network baseline. The [constructor/template investigation](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md)
establishes conditional initialization inputs separately. The
[literal observer join](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R44_OBSERVER_STATE_AND_TERRAIN_PREREQUISITES.md)
confirms the current seeded-finalized world gap before end-baselines;
current-incarnation partial storage/admission remains open. No packet registry,
opcode, source-matrix denominator or gameplay claim changes.

Both message types use operand count 5 and little-endian application fields.

| Message | Opcode | Envelope after opcode | Executable path | Confidence |
|---|---:|---|---|---|
| `BaselinesMessage` | `0x68A75F0C` | U64 object ID, U32 object tag, U8 view, U32 payload size, U16 variable count, sequential field payload | `0x0051C6BE` -> `0x00ADBC80` -> object lookup -> `0x00554CF0` | Confirmed |
| `DeltasMessage` | `0x12862153` | U64 object ID, U32 object tag, U8 view, U32 payload size, U16 update count, repeated U16 field index plus encoded field value | `0x0051C76D` -> `0x00ADB2D0` -> object lookup -> `0x00554E90` | Confirmed |

The executable parsers register the same four outer members: ID, tag, view and variable payload. The baseline dispatcher selects one of nine object view members and invokes its snapshot unpack operation. The delta dispatcher selects the same member, invokes its indexed delta operation, and then calls the member's post-update/commit hook. Therefore a replacement client should use one `(object tag, view)` schema registry with separate `decodeSnapshot` and `applyDelta` entry points.

The owning client-object constructor at `0x00554240` creates six `Archive::AutoDeltaByteStream` members using `0x0118A650`. The view switch maps them exactly as follows:

| View | Client-object storage | Behavior |
|---:|---:|---|
| 1 | `+0xA4` | fixed registered byte stream |
| 2 | none | ignored/no fixed stream |
| 3 | `+0x124` | fixed registered byte stream |
| 4 | `+0xC4` | fixed registered byte stream |
| 5 | none | ignored/no fixed stream |
| 6 | `+0x144` | fixed registered byte stream |
| 7 | `+0x164` pointer | dynamically supplied UI view package |
| 8 | `+0xE4` | fixed registered byte stream |
| 9 | `+0x104` | fixed registered byte stream |

All six fixed members have RTTI `Archive::AutoDeltaByteStream` and vtable `0x015FE388`; they are not separate C++ types. Their actual schemas are lists of registered per-field decoder objects. Dynamic view 7 uses a wrapper whose constructor `0x006F24D0` stores a view/package key and embeds another `AutoDeltaByteStream` at wrapper `+0x08`. This is **Confirmed executable layout and dispatch**, not a proposed x64 layout.

The payload size excludes the server builders' 27-byte framed header: four SOE opcode/sequence bytes plus the 23-byte application envelope. After transport removal, application offsets are count0, opcode2, object ID6, raw tag14, view18 and payload size19; the inner U16 count begins at23. The size includes that inner count and excludes all23 application-envelope bytes. Server setSize writes at framed offset23 (application offset19). This distinction was checked during Round6 implementation; no four-byte padding belongs in an application codec. Baseline payloads begin with a U16 declared variable count and delta payloads begin with a U16 update count; every delta update then begins with a U16 schema index. Client behavior adds a compatibility-critical qualification: `autodelta_byte_stream_snapshot_decode_VA_0118B740` consumes the baseline count but iterates the complete locally registered decoder list rather than using the declared value as its loop bound. `autodelta_byte_stream_delta_decode_VA_0118AA00` likewise consumes the declared update count but loops while the bounded payload has bytes remaining; it reads each U16 index and directly selects the registered decoder pointer by `index * 4`. No explicit index bound check is visible. A replacement must preserve characterized count tolerance while adding strict delta-index and enclosing-payload bounds.

Count tolerance is not truncation tolerance. A baseline still attempts to decode every registered field, so a payload that ends before the schema does is malformed regardless of its declared count. A delta with any nonzero remainder attempts to read another U16 index, so a one-byte tail is malformed. The replacement parser should accept characterized surplus tails only where this executable demonstrably does, reject missing field bytes and partial index/value encodings, and never commit a partially validated mutation plan.

## Object tags and views in the initial player stream

Four-character tags below are written by the server as 32-bit multi-character constants. They must remain raw U32 wire values internally; display spelling is diagnostic metadata, not an instruction to byte-swap the packet.

| Tag | Object family | Views sent by current server | Visibility |
|---|---|---|---|
| `CREO` (`0x4352454F`) | creature/player creature | 1, 3, 4, 6 | view 1 and 4 only to the owning creature; 3 and 6 to observers |
| `PLAY` (`0x504C4159`) | player ghost/intangible companion | 3, 6, 8, 9 | 3 and 6 generally sent; 8 and 9 only to its parent/owner |
| `TANO` (`0x54414E4F`) | tangible base | 3, 6 | observer-visible according to object send scope |
| `ITNO` (`0x4F4E5449`) | intangible base | 3, 6 | object-specific |

Additional server packet directories define `SCLT`, `BUIO`, `STAO`, `RCNO`, `SHIP`, `GRUP`, `GILD`, `MISO`, and other families. They are inventoried but not yet field-mapped here.

Engine-critical additions are now mapped at server-wire level. `SCLT` view 3 carries five logical fields culminating in a U32 cell number; view 6 carries the constant/flags value `0x95` plus an empty U64-like collection snapshot (count/counter). Building view 6 carries constant `0x42` plus an empty U64 collection snapshot. The checked-in Building view-3 class is disabled/commented, so building objects rely on their tangible/object inheritance path in current code; this is a **server-source fact**, not a general protocol guarantee.

## Confirmed server field schemas

The following names and index positions come from the Stardust server constructors/delta builders. Executable acceptance of views 1-9 is confirmed, but every individual semantic field name has not yet been independently recovered from client code.

### TANO view 3: 11 fields

| Index | Field | Encoding |
|---:|---|---|
| 0 | complexity | F32 |
| 1 | localized object name | ASCII16 file, U32 zero spacer, ASCII16 string ID |
| 2 | custom name | Unicode32 |
| 3 | volume | U32 |
| 4 | customization string | ASCII16 |
| 5 | visible components | delta-container snapshot |
| 6 | options bitmask | U32 |
| 7 | use/countdown count | U32 |
| 8 | condition damage | U32 |
| 9 | maximum condition | U32 |
| 10 | visible flag | U8 |

`CREO` view 3 inherits these indices and changes the total to 18 fields:

| Index | Creature extension | Encoding |
|---:|---|---|
| 11 | posture | U8 |
| 12 | faction rank | U8 |
| 13 | creature link / mount ID | U64 |
| 14 | height | F32 |
| 15 | shock wounds / battle fatigue | U32 |
| 16 | state bitmask | U64 |
| 17 | wounds | delta-vector snapshot |

### TANO and CREO view 6

`TANO` view 6 begins with index 0 constant `0x76`, followed at index 1 by the defenders delta-vector. `CREO` view 6 expands the total to 22 fields:

| Index | Creature field | Encoding |
|---:|---|---|
| 2 | level | U16 |
| 3 | performance animation | ASCII16 |
| 4 | mood string | ASCII16 |
| 5 | weapon ID | U64 |
| 6 | group ID | U64 |
| 7 | group inviter ID plus invite counter | two U64 values in one schema field |
| 8 | guild ID | U32 |
| 9 | target ID | U64 |
| 10 | mood ID | U8 |
| 11 | performance start time | U32 |
| 12 | performance type | U32 |
| 13 | current HAM | delta-vector snapshot |
| 14 | maximum HAM | delta-vector snapshot |
| 15 | equipment/wearables | delta-vector snapshot |
| 16 | alternate appearance | ASCII16 |
| 17 | frozen flag | U8 |

The server constructor declares 22 logical fields but emits indices 0-17. The executable resolves the interoperability question: its generic baseline stream ignores the declared count after consuming it and invokes the 18 registered field decoders. There are no client fields at indices 18-21 for this class/view. The complete registration list is confirmed later in this document.

### CREO private views

View 1 has four fields: bank credits U32, cash credits U32, base HAM delta-vector, and skill-list delta container. View 4 has 14 logical fields: acceleration base/mod, encumbrances, skill mods, speed base/mod, listen target U64, run speed, slope angle/percent, turn scale, walk speed, water modifier, and a space-mission map snapshot.

### SHIP views required by space/world architecture

Current server constructors define four principal views. View 3 has 22 fields: tangible identity/customization/visibility fields 0-10, followed by chassis slip/current/max health, chassis CRC, five component armor/hitpoint/options maps, and maximum front/rear shields. View 6 has 23 fields: flags, defenders, U16 unique ID, acceleration/deceleration, pitch/yaw/roll acceleration and rates, max speed, target U64/slot, targetable bitfield, component map, wing/type/difficulty/faction strings, current shields and guild ID.

Private/owner view 1 contains 38 fields: start/end markers, fifteen component statistic/name/creator/ammo maps, chassis mass capacity, shield/capacitor values, engine acceleration/rate/max-speed values, reactor/booster values, droid command speed and droid U64 ID. View 4 contains current installed mass, the chassis speed scalar, current capacitor/booster energy and the component refire-efficiency map. These are **Confirmed server wire schemas** and demonstrate that space flight needs a specialized ship-state component, not a widened CreatureObject structure. The view-4 speed field is not instantaneous velocity; transform traffic updates a separate transient current-speed member.

View 1 is now field-closed against the executable. After its two inherited U32
zero placeholders, offsets `+0x598/+0x5D0/+0x608/+0x640` register four
U32→F32 maps; `+0x760` is U32→Unicode; `+0x798` is U32→U64; six more U32→F32
maps occupy `+0x7D0` through `+0x8E8`; `+0x958/+0x990` are U32→S32 ammunition
maps and `+0x9C8` is the U32→U32 ammunition-class map. Scalars 17-36 are F32
and index 37 is U64. All maps are `DeltaAutoPackedMap` full replacements even
inside a delta. The component-creator field has no current setter call and is
dormant. The droid-ID decoder at `0x00545050/0x005450E0` publishes
`DroidPcdChanged`.

View 4 is also field-closed. Registrar `0x00555440` adds F32 fields at legacy
offsets `+0x44C`, `+0x460`, `+0xA64`, and `+0xB68`, followed by the U32→F32
packed refire-efficiency map at `+0x920`. The map is a full replacement in
deltas. Current mass/capacitor/booster values have active simulation producers;
the chassis scalar is template-derived and feeds maximum-speed calculation.
Current Stardust replicates refire efficiency but does not read it in server
simulation, so its final client timing application remains Strong rather than
fully address-confirmed.

View 6 is now field-closed. After inherited flags and defenders, registrar
`0x005554C0` adds U16 unique ID at `+0x3D0`; nine flight F32s at `+0x474`
through `+0x514`; target U64/U32 at `+0xC20/+0xC58`; targetability at `+0xC74`;
component CRC map at `+0x678`; four ASCII fields at `+0xCA8`, `+0xCCC`,
`+0xCF0`, and `+0xD14`; shield F32s at `+0xA00/+0xA28`; and guild U32 at
`+0xD38`.
Targetability is a full byte-vector snapshot, whereas the component CRC map is
a true incremental ADD/DROP/SET `DeltaAutoMap`. Wing name has no producer;
baseline guild ID is owner-derived while its delta setter is dormant.

Executable constructor `ship_object_replication_ctor_VA_00536670` independently confirms those cardinalities and their registration order. It derives from `TangibleObject`, then adds 11 fields to view 3 (for 22 composed fields), 36 fields to view 1 (plus the two inherited start/end fields), 21 fields to view 6 (plus inherited flags/defenders), and five fields to view 4. Its primary RTTI vtable is `0x015DDE6C`; the legacy object extends to at least `0xDB4` bytes, which is evidence about the old ABI only and must not become the replacement layout.

Three SHIP:6[15] component-CRC map mutation callbacks at `0x0053C3E0`, `0x0053C130`, and `0x0053BE80` all call `ship_component_attachment_refresh_VA_00539B70` for the affected component slot and then publish `ShipObject::Messages::ComponentsChanged`. The refresh path obtains the installed component CRC, validates both chassis and attachment CRC, and updates the ship attachment/appearance; it emits dedicated warnings for a null chassis or invalid attachment CRC. The SHIP:3 armor/hitpoint/option maps are correlated condition state but do not own this attachment callback.

View 3 is now field- and container-closed. The executable registers its eleven
ship additions at legacy offsets `+0x3E8`, `+0x3FC`, `+0x410`, `+0x424`,
`+0x528`, `+0x560`, `+0x6AC`, `+0x6E4`, `+0x71C`, `+0xA14`, and `+0xA3C`,
matching server indices 11-21 exactly. Indices 15-18 are U32-to-F32 packed
maps; index 19 is U32-to-U32. Their baseline and delta-field form is U32 count,
U32 counter, then ADD entries. `DeltaAutoPackedMap` accepts ADD/DROP/SET for
local mutation but serializes the complete resulting map, so DROP and SET are
not sent through these setters. `ShipDeltaVector` classifies view 3 as public
and coalesces it before broadcast. SHIP inherits the confirmed TANO
view-3 index-5 incompatibility: the old client codec consumes eight bytes per
visible-component entry while current Stardust emits a four-byte integer.

Specialized view-6 codecs are also address-backed. Target NetworkId uses snapshot/delta `0x00545290/0x005453C0` and invokes `0x0053D040`, which updates local targeting/controller state and publishes `LookAtTargetChanged`. Target slot uses `0x00545550/0x005455A0` and invokes `0x00696760`, which publishes `LookAtTargetSlotChanged` and reconciles slot validity. The targetable bitfield uses `0x00545860/0x005458F0`; callback `0x006969F0` refreshes selection when the changed ship is the current player's root ship. Owner-view droid ID uses `0x00545050/0x005450E0` and publishes `DroidPcdChanged`, showing that the client interprets the server's droid object ID as its persistent-control-device relationship.

### GRUP views and event boundary

`GroupObject` RTTI resolves to primary vtable `0x015E0ED4`; constructor `group_object_replication_ctor_VA_0067AFC0` derives from the static/common client object and registers seven additional view-6 fields. With the inherited flags/unknown U32, this exactly matches the server's eight-field `GRUP:6`: member list, member-ship list, group name, level, formation CRC, master-looter NetworkId, and loot rule.

The member list vtable `0x015E1024` uses snapshot/delta decoders `0x0067E500/0x0067E750`. Each element is `NetworkId` plus extended-capable ASCII length (`U16`, or `0xFFFF` then U32) and bytes; the legacy runtime stride is `0x18`. The member-ship vtable `0x015E0FE4` uses `0x0067FA90/0x0067FCA0`; each element is `NetworkId + U32`, runtime stride `0x10`. Both snapshots begin with U32 element count and U32 update counter.

Mutation hooks publish both aggregate and granular events: `MembersChanged` (`0x0067B500`), `MemberAdded` (`0x0067B790`), `MemberRemoved` (`0x0067BA80`), `MemberShipsChanged` (`0x0067BD70`), `MemberShipAdded` (`0x0067C000`), and `MemberShipRemoved` (`0x0067C260`). The granular RTTI types preserve payload semantics as `(GroupObject*, (NetworkId, string))` for membership and `(GroupObject*, (NetworkId, int))` for ship assignment. Local-player membership changes additionally enter a client notification/UI construction path. The replacement should therefore expose a transactional group-state change set plus post-commit granular events rather than binding UI directly to raw delta commands.

### GILD views and guild-directory compatibility issue

`GuildObject` RTTI resolves to primary vtable `0x015E2714`. Constructor `guild_object_replication_ctor_VA_0070B5F0` derives from the static/common client object and adds one view-3 field, producing the server's five-field `GILD:3` when the four inherited identity/name fields are included. View 6 contains only the inherited common U32, matching the server's one-field `GILD:6`. The added view-3 field is a string set with snapshot/delta decoders `guild_list_snapshot_decode_VA_0070C520` and `guild_list_delta_decode_VA_0070C720` (container vtable `0x015E27B4`). A snapshot is U32 count, U32 update counter, then extended-capable ASCII strings.

Post-change callback `guild_list_rebuild_lookup_VA_0070B8C0` clears and rebuilds two lookup structures. Each canonical string is split at the first `:`; the prefix is parsed as an unsigned integer guild ID and the suffix becomes the guild abbreviation. Current server `GuildObject::getGuildKey()` and `GuildObjectDeltaMessage3` independently emit exactly `guildID:guildAbbrev`, so this mapping is **Confirmed**.

The executable delta decoder accepts command `0` remove-string, command `1` add-string, and command `2` clear-all. Current `GuildObjectDeltaMessage3::removeAllGuilds()` instead emits command `3`. The client decoder has no command-3 branch and silently leaves its set unchanged. This is a **Confirmed current server/client incompatibility**: a replacement aiming at intended protocol behavior should accept command 2 and may deliberately tolerate command 3 as a Stardust compatibility alias, while the server should eventually be corrected to emit 2. This observation is documentation only; no server change was made.

### PLAY view 3: 11 fields

`PLAY` view 3 inherits five `ITNO` fields: complexity F32, localized name, Unicode custom name, volume/data size U32, and status U32. It then appends:

| Index | Player field | Encoding |
|---:|---|---|
| 5 | four player bitmasks | U32 count `4`, then four U32 values |
| 6 | four profile settings | U32 count `4`, then four U32 values |
| 7 | skill title | ASCII16 |
| 8 | birth date | U32 |
| 9 | total played time | U32 |
| 10 | unresolved triple | U32 `0x6C2`, U32 `0xDC62`, U32 `0x23` |

View 6 declares three logical fields but visibly writes a U32 zero and U8 privilege flag. As with `CREO:6`, the executable consumes the declared count but decodes its locally registered schema, so no fabricated third value should be added. Views 8 and 9 contain private experience, waypoint, quest, ability, crafting, language, stomach and Jedi-state data. Their container element encodings require separate mapping before implementation.

## Delta index evidence

Server delta builders directly corroborate schema indices. Examples include `CREO:3` posture 11, height 14 and state bitmask 16; `CREO:4` acceleration modifier 1, speed modifier 5 and turn scale 10; `CREO:6` weapon 5, group 6, target 9 and alternate appearance 16; `PLAY:3` title 7; `PLAY:6` privilege 1; `PLAY:8` force current/max 2/3; and `PLAY:9` language 9, food/drink 10-13 and Jedi state 17.

These mappings are suitable for protocol fixtures, but delta-container internals still need independent schemas for vector/map operations, update counters, element indices and add/remove opcodes.

## Replacement-client implications

- Preserve object IDs, tags, view numbers, field indices, counts and sizes at their historical wire widths. None become pointer-sized in an x64 client.
- Keep object construction separate from replication. A create packet registers identity/template/transform; baseline views populate state; `SceneEndBaselines` commits object readiness.
- Treat server `SceneObjectCloseMessage` as the C++ producer name for wire message `SceneEndBaselines` (`0x2C436037`). Its body is exactly one LE64 object ID. It terminates the initial baseline transaction for that object; it must never be modeled as destruction, stream closure, or a request to close UI.
- The minimum lifecycle state machine is `absent -> created/pending baselines -> baseline views and containment applied -> finalized/ready`. `SceneDestroyObject` alone drives removal. A robust x64 implementation should diagnose an end-baselines ID that is absent without confusing it with a destroy event.
- Store unknown tags, views and delta indices as bounded diagnostic blobs rather than desynchronizing the stream.
- Make ownership visibility explicit. Private `CREO`/`PLAY` views must not be assumed present for remote objects.
- Validate declared payload size before parsing containers, cap list counts and string lengths, and require exact consumption or an explicitly documented trailing-field exception.

## AutoDelta container wire formats

### Common snapshot prefix

The principal container snapshots begin with:

`U32 elementCount | U32 updateCounter | element data...`

This is confirmed by the server `DeltaVector`, `DeltaVectorMap`, `DeltaSet`, `AutoDeltaSet`, and specialized list serializers. It is also confirmed in Stardust.exe for representative `Archive::AutoDeltaVector<int, CreatureObject>` snapshot decoder `0x0043B050`, which clears the current vector, reads both U32 values, then reads exactly `elementCount` elements.

Container element shapes differ:

| Container | Snapshot element encoding | Source confidence |
|---|---|---|
| `DeltaVector<E>` / client `AutoDeltaVector<E>` | repeated `E`; no per-element command byte | Confirmed both sides for integer specialization |
| `DeltaVectorMap<K,V>` | U8 command `0` (ADD), then `K`, `V` for every snapshot element | Confirmed server; representative client map serializer shows command plus key/value |
| `DeltaSet<K,V>` / `AutoDeltaSet<K>` basic snapshot | repeated keys only | Confirmed server |
| `DeltaSet` key/value snapshot variant | U8 add command `1`, then `K`, `V` | Confirmed server variant |
| `DeltaBitArray` | `DeltaVector<U8>`; count is stored bytes, not bits | Confirmed server inheritance |

Specialized classes such as wearables, abilities, skills, waypoints and schematics override only the element codec or update details; they retain the count/counter concept.

### Vector delta operations

Representative client specialization: RTTI `Archive::AutoDeltaVector<int, CreatureObject>`, vtable `0x015DD07C`, snapshot decoder `0x0043B050`, delta decoder `0x0043B1A0`.

The field payload passed to the delta decoder begins:

`U32 operationCount | U32 newUpdateCounter | operation...`

For this four-byte signed-integer specialization, Stardust.exe recognizes:

| Command | Wire body | Client action | Validation observed |
|---:|---|---|---|
| 0 | U16 index | remove element | requires `index < size`; invalid request is ignored |
| 1 | U16 index, E value | insert/add at index | zero-fills to index before inserting |
| 2 | U16 index, E value | set/replace at index | equal existing value is a no-op; otherwise zero-fills to `index + 1` |
| 3 | U16 count, repeated E | replace/assign from counted sequence | allocates temporary sequence from count, then assigns it |
| 4 | no body | clear all | empties vector and fires callbacks |

Commands 0, 1, 2 and 4 match the current server `DeltaVector` methods directly. Command 3 is accepted by Stardust.exe even though the generic checked-in server class does not emit it; older or specialized server code may use it.

### Update-counter reconciliation

The representative integer-vector decoder `0x0043B1A0` computes unsigned `R = (N - C + L) mod 2^32`, then skips to cursor threshold `min(R,N)`. N is the raw operation-loop bound, C incoming counter and L local counter. Ordinary commands consume one cursor unit; assign of k values consumes k+1, including a skipped assignment and final overshoot. Fully parsed skipped commands preserve archive alignment. See the instruction-backed [bounded legacy kernel](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md).

The arithmetic/cursor rules and mutation-dependent local counter are **Confirmed by direct pinned instructions**. This specialization does not force L to C or max(L,C). Invalid remove, equal replace and empty clear do not advance L; insert/changed replace/remove advance once, assign advances k+1. Bounds and atomic commit remain modern policy. These vector rules must not be generalized to other map/set specializations without direct evidence.

### Map and set commands

Server definitions establish two command vocabularies:

- `DeltaVectorMap`: `0=ADD`, `1=DROP`, `2=SET`, followed by key and value. DROP includes the prior value on the wire.
- `DeltaSet` / `AutoDeltaSet`: `0=drop`, `1=add`, `2=clear`; basic key-only variants serialize a key for add/drop, while key/value variants serialize both.

Representative client RTTI and vtables are now address-backed:

- `Archive::AutoDeltaSet<SkillObject const*, CreatureObject>` -> vtable `0x015DCF30`.
- `Archive::AutoDeltaMap<int,float,...>` -> vtable `0x015DE068`.
- `Archive::AutoDeltaVector<CachedNetworkId,...>` -> vtable `0x015E0614`.
- `Archive::AutoDeltaMap<string,int,PlayerObject>` -> vtable `0x015E0C9C`.
- `Archive::AutoDeltaMap<NetworkId,Waypoint,PlayerObject>` -> vtable `0x015E0BA0`.

Their serializers/decoders confirm the common count/counter framing and command-driven update design. Exact element codecs and every callback are still being mapped.

### Executable-confirmed set decoder

Representative specialization `Archive::AutoDeltaSet<SkillObject const*, CreatureObject>` uses vtable `0x015DCF30`, snapshot decoder `0x0043D820`, and delta decoder `0x0043D9A0`.

The snapshot reads U32 element count, U32 update counter, then that many keys into a newly cleared set. The delta decoder reconciles update counters and recognizes the following commands. Do not generalize its arithmetic from the recovered integer-vector kernel; the integer-set specialization below records its own exact gate:

| Command | Body | Behavior |
|---:|---|---|
| 0 | key | remove only if present |
| 1 | key | add only if absent |
| 2 | none | clear entire set |

Skipped stale operations still consume their key for commands other than clear. After applying the suffix, the local counter is raised to at least the incoming counter. These mechanics are **Confirmed by Stardust.exe** and match the server `AutoDeltaSet` vocabulary.

For creature skills, the current server's specialized `SkillList` uses ASCII16 skill names as keys: baseline `U32 count | U32 counter | ASCII16[count]`; deltas use command 1 plus ASCII16 to add and command 0 plus ASCII16 to remove. Client RTTI shows the in-memory set stores `SkillObject const*`, proving that wire strings are resolved into client skill-definition objects rather than retained as server pointers.

### Executable-confirmed map decoder

Representative specialization `Archive::AutoDeltaMap<int,float,...>` uses vtable `0x015DE068`. Its baseline decoder `0x00540E30` clears the map and reads U32 count, U32 counter, then repeated U8 command, U32 key and F32 value. Baseline builders emit command 0 for every entry.

Its delta decoder `0x00540F60` reconciles update counters and recognizes the server `DeltaMapCommands` values:

| Command | Body | Behavior |
|---:|---|---|
| 0 (`ADD`) | key, value | insert when absent; duplicate-key behavior is container-policy dependent |
| 1 (`DROP`) | key, prior value | remove matching key; prior value remains available for callbacks/validation |
| 2 (`SET`) | key, value | insert if absent or replace if the value changed |

All three commands consume both key and value. The decoder ignores a `SET` whose current value already equals the incoming value. After processing, it advances the local counter to at least the incoming counter. These mechanics are **Confirmed for the integer/float specialization**; complex specializations reuse the control framing but substitute their key/value codecs.

### High-priority specialized element codecs

| Replicated field | Container/control form | Element encoding from current server | Client-side identity/effect |
|---|---|---|---|
| `CREO:1` skill list | specialized set-like list | ASCII16 skill name | resolves to `SkillObject const*`; add/remove changes available skills |
| `PLAY:9` ability list | specialized indexed vector | ASCII16 ability name in baseline; add uses command 1, U16 index, serialized ability name | resolves against client ability/command definitions |
| `CREO:6` equipment/wearables | indexed vector | ASCII16 customization, U32 containment/arrangement, U64 object ID, U32 template CRC | client RTTI `AutoDeltaVector<WearableEntry,...>`; caches equipment metadata, while actual attachment is containment-driven |
| `PLAY:8` experience | map | ASCII16 experience name, U32 value, with map command byte | client RTTI `AutoDeltaMap<string,int,PlayerObject>`; callback `ExperienceChanged` |
| `PLAY:8` waypoints | specialized map | U8 command, U64 key; value = U32 cell ID, F32 X/Z/Y, U64 unknown, U32 planet CRC, Unicode32 name, U64 object ID, U8 color, U8 active | client RTTI `AutoDeltaMap<NetworkId,Waypoint,PlayerObject>` |
| completed/active quests | byte vector / bit array | U32 byte count, U32 counter, raw U8 bytes; generic vector delta commands | client callback RTTI exposes completed/active quest bit-array changes |

The wearable wire entry is especially important for architecture: its U64 object ID is a network identifier and its U32 CRC selects an asset/template. Neither is a native pointer, and the entry must be decoded into an x64-owned value object before attachment resolution.

### Executable-confirmed replication callbacks

MSVC RTTI and callback-specialization vtables show that decoded replicated values can trigger typed owner notifications. The decoder pattern is consistent: retain the old value, read the fixed-width replacement, run the container's post-read hook, compare old and new values, then invoke the callback only when the value changed. These are behavioral requirements; the replacement should not reproduce the original callback object layout.

| Owner / replicated state | RTTI callback specialization | Vtable | Decode/apply method | Observed effect | Confidence |
|---|---|---:|---:|---|---|
| `CreatureObject` visual posture | `VisualPostureChanged` | `0x015DD104` | `0x0043A3B0` | reads one byte and emits a typed change notification when different | Confirmed |
| `CreatureObject` group ID | `GroupChanged` with `CachedNetworkId` | `0x015DCE4C` | `0x0043F960` | updates a cached network identifier and conditionally notifies group-dependent state | Confirmed decode/callback; downstream UI behavior Unknown |
| `CreatureObject` performance type | `PerformanceTypeChanged` | `0x015DCE04` | `0x00440160` | reads U32 and invokes owner callback on change | Confirmed |
| `CreatureObject` rank | `RankChanged` | `0x015DCDBC` | `0x004403B0` | reads one byte; this specialization does not expose the same conditional callback tail | Confirmed decode; notification semantics Tentative |
| `CreatureObject` alternate appearance template | `AlternateAppearanceSharedObjectTemplateNameChanged` | `0x015DCD10` | `0x00442080` | decodes a string-like value and routes a changed template name into appearance state | Strong; full string codec/callee remains unmapped |
| `PlayerObject` experience value | `ExperienceChanged` | `0x015E0C78` | `0x00664940` | reads U32 and invokes typed player callback on change | Confirmed |
| `PlayerObject` spoken language | `SpokenLanguageChanged` | `0x015E0BC4` | `0x006652D0` | reads U32 and invokes language callback on change | Confirmed |
| `PlayerObject` completed quests | `CompletedQuestsChanged` with `BitArray` | `0x015E0B58` | `0x00665580` | decodes a bit array, compares old/new state, and notifies only if changed | Confirmed |
| `PlayerObject` active quests | `ActiveQuestsChanged` with `BitArray` | `0x015E0D44` | `0x00665820` | decodes a bit array, compares old/new state, and notifies only if changed | Confirmed |
| `TangibleObject` maximum hit points | `MaxHitPointsModified` | `0x015E0654` | `0x00641430` | reads U32 and calls owner virtual slot `+0xB0` with old/new values | Confirmed |
| `TangibleObject` condition flags | `ConditionModified` | `0x015E05BC` | `0x00643360` | reads U32 and calls owner virtual slot `+0xAC` with old/new values | Confirmed |

The executable also retains message/transceiver RTTI for `CreatureObject::DamageTaken` and `PlayerObject::JediStateChanged`. Those establish event identities but are not, by themselves, proof of a particular baseline/delta field or decoder, so they remain outside the confirmed table above.

This callback evidence changes the replacement model: each schema field needs both a codec and an optional semantic change hook. Applying an entire baseline/delta should be transactional where possible, with notifications emitted after validation and state commit. That avoids reproducing the legacy risk of partially mutated state if a later field is malformed.

### Confirmed callback consumers and specialized containers

RTTI for `MessageDispatch::OwnedTransceiver` identifies concrete subscribers without requiring guessed call semantics:

| Change event | Confirmed subscriber identities retained in Stardust.exe | Compatibility implication |
|---|---|---|
| `PlayerObject::ExperienceChanged` | `CuiSkillManagerNamespace::MyCallback`, `SwgCuiExpMonitor`, `SwgCuiSkills2Profession`, `SwgCuiSkills2MyStats`, `SwgCuiSkills2ProfessionGraph` | experience updates drive both skill/profession model refresh and several independent UI views |
| `CreatureObject::GroupChanged` | `SwgCuiSpaceAssignPlayerFormation`, `SwgCuiShipView`, `SwgCuiSpaceGroupLaunch`, `SwgCuiSpaceGroup`, `SwgCuiGroup` | group membership is consumed by ground and space UI systems; it cannot be treated as display-only text |
| `PlayerObject::WaypointsChanged` | `SwgCuiWaypointMonitorBox`, `SwgCuiSpaceZoneMap` | waypoint map mutation refreshes both monitor and space-map presentation |
| `PlayerObject::CompletedQuestsChanged` | `SwgCuiQuestJournal` | completed-quest bit-array mutation refreshes quest-journal state |
| `PlayerObject::ActiveQuestsChanged` | `SwgCuiQuestJournal` | active-quest bit-array mutation refreshes quest-journal state |
| `CreatureObject::VisualPostureChanged` | `SwgCuiStatusGround` | posture affects status presentation in addition to likely animation/state behavior |
| `PlayerObject::JediStateChanged` | `SwgCuiSkills2Profession`, `SwgCuiSkills2ProfessionList`, `CuiCommandTableParser` | Jedi state affects profession presentation and command availability/parsing |

These are **Confirmed subscriber class/event pairings from RTTI**. Exact callback method bodies and redraw granularity remain Unknown.

Two complex container implementations are now address-backed:

- `Archive::AutoDeltaVector<WearableEntry,CreatureObject>` uses vtable `0x015DCD34`, snapshot decoder `0x00441740`, and delta decoder `0x00441920`. Its in-memory vector stride is `0x20` bytes. The delta decoder implements the same U32 operation-count/counter reconciliation and U8 command/U16 index control framing as the generic vector. The 32-byte runtime entry must not be confused with the variable-length wire representation or copied into the new x64 ABI.
- `Archive::AutoDeltaMap<NetworkId,Waypoint,PlayerObject>` uses vtable `0x015E0BA0`, snapshot decoder `0x00669F40`, and delta decoder `0x0066A1D0`. The snapshot clears the existing map before reading count/counter and entries. The delta decoder performs counter reconciliation before command application. RTTI separately exposes `PlayerObject::WaypointsChanged`, `ClientWaypointObject`, and its `ActivatedChanged`, `LocationChanged`, and `ColorChanged` transceivers.

This supports a two-layer waypoint model: a replicated player-owned waypoint value map feeds client waypoint presentation objects, which then publish narrower activation/location/color events. The class/event identities are **Confirmed**; the exact conversion/ownership sequence is **Strongly inferred** and still needs direct call-path tracing.

The generic wearable-vector implementation supports two notification tiers. Its mutation helpers can synchronously invoke remove/insert/replace callbacks and later enqueue one coalesced aggregate callback. For this concrete `CreatureObject` field, however, constructor `0x0042ECF0` leaves all four callback slots at object offsets `+0x944/+0x948/+0x94C/+0x950` null and does not install replacements. Thus the decoder mutates only the metadata vector. Actual mesh attachment is driven by the separate containment path: `UpdateContainmentMessage` -> `0x00555020` -> CreatureObject add/remove callbacks `0x00436D90`/`0x00437050` -> `0x0067A060`/`0x0067A140` -> skeletal wear/stop-wearing. This is **Confirmed** and corrects the earlier assumption that replicated-vector callbacks directly owned appearance attachment.

### Specialized PLAY codecs

Three remaining container codecs are now executable-confirmed:

| Field | Snapshot decoder | Delta decoder | Element grammar |
|---|---:|---:|---|
| `PLAY:9[0]` abilities | `0x00661E20` | `0x00662030` | variable-length ASCII bytes prefixed by U16 length, or `0xFFFF` followed by U32 length; in memory each string occupies a 12-byte legacy `std::string` object |
| `PLAY:9[4]` draft schematics | `0x006633A0` | `0x00663530` | two U32 values per element, matching current server schematic ID/object CRC pair; legacy runtime stride is 8 bytes |
| `PLAY:8[6]` quest data | `0x00665C40` | `0x00666430` | U32 quest CRC key plus `PlayerQuestData`: U64 owner ID, U16 active-step mask, U16 completed-step mask, U8 completed flag, U32 quest counter |

The ability and draft-schematic deltas use the five-command indexed-vector grammar: remove `0`, insert `1`, set `2`, replace-all `3`, clear `4`, with U16 indices. Both apply update-counter stale-prefix reconciliation. The quest field is a packed map; its executable value decoder `0x01233560` exactly matches the current server `PlayerQuestData::toBinaryStream` widths and order.

PLAY:9 index 2 is no longer generically “crafting state”: RTTI fixes it as `Crafting::CraftingStage`. Current server delta methods fix index 1 as experimentation-enabled state, index 3 as nearest crafting-station NetworkId and index 5 as experimentation points. Index 6 is still semantically ambiguous: the current baseline comments call it species data, while client RTTI attaches `PlayerObject::Messages::ExperienceChanged` to the U32. That conflict is preserved as an Unknown rather than resolved by naming guesswork.

### Malformed payload boundary

`archive_read_fixed_checked_VA_00439910` delegates to `byte_stream_read_checked_VA_0118B030`, which verifies `offset + requestedBytes <= streamSize` and throws an Archive exception with diagnostic `Archive::ByteStream::read beyond end of buffer` on failure. A one-byte remainder at the view-delta level is therefore not silently ignored: `0x0118AA00` attempts another U16 field index and the checked read throws.

The field index itself is not validated. `0x0118AA00` computes `registry[index]`, dereferences it and calls virtual slot `+0x18` directly. An out-of-range U16 can therefore access outside the registered-field array and may fault or dispatch through invalid memory. Earlier field/container mutations are applied in place; if a later read throws, the normal outer commit is skipped, so state can be partially changed without the coalesced field notifications. These mechanics are **Confirmed by code**. The replacement must decode to temporary state, bounds-check every index and length, and commit atomically.

### Security and x64 requirements

### Client view registry and dispatch

The persistent Ghidra pass confirms that the common client object embeds six 32-byte `AutoDeltaByteStream` view registries. The constructor at `0x00554240` creates them in this exact order and the shared dispatchers select them as follows:

| Wire view | ClientObject offset | Field-registration helper | Snapshot path | Delta path |
|---:|---:|---:|---|---|
| 1 | `+0xA4` | `client_view1_register_field_VA_00555420` | vtable `+0x0C` | vtable `+0x18`, then `+0x1C` commit |
| 3 | `+0x124` | `client_view3_register_field_VA_005554A0` | same | same |
| 4 | `+0xC4` | `client_view4_register_field_VA_00555440` | same | same |
| 6 | `+0x144` | `client_view6_register_field_VA_005554C0` | same | same |
| 7 | optional pointer `+0x164` | dynamic UI-package path | `0x006F2540` | `0x006F25B0`, then `0x006F2620` |
| 8 | `+0xE4` | `client_view8_register_field_VA_00555460` | same | same |
| 9 | `+0x104` | `client_view9_register_field_VA_00555480` | same | same |

Views 2 and 5 have no branch in either shared dispatcher. The embedded stream vtable is `0x015FE388`; its confirmed methods are snapshot decode `0x0118B740`, field registration `0x0118A840`, delta decode `0x0118AA00`, and deferred callback commit `0x0118A970`.

Registration assigns each field a U16 index equal to its current position and stores a back-pointer to the owning view. Snapshot decode reads but does not use the wire U16 declared-field count, then visits every client-registered field in order. Delta decode reads but does not use the wire U16 update count, then consumes U16 field-index/value pairs until the bounded archive iterator reaches its end. The commit pass invokes queued field callbacks and clears the pending set. These mechanics are **Confirmed by executable code** and explain why current-server baseline count declarations and trailing legacy placeholders need not equal the number of client field objects.

Changed fields enter the pending callback collection through `0x0118A7C0`, which implements ordered-set insertion and suppresses duplicate keys. Commit `0x0118A970` walks that tree in key order, invokes each field callback once, then destroys all nodes and resets the count. Multiple updates to one field within a single delta therefore coalesce to one post-apply callback, and legacy callback order follows field identity/address rather than packet order. This is **Confirmed**. A replacement should preserve coalescing and use an explicit deterministic order, preferably schema index. It should not emulate x86 pointer ordering unless a later trace proves an observable dependency on that incidental layout.

Constructor-derived registration additions are now mechanically bounded: common `ClientObject` adds view 1×2, view 3×4 and view 6×1; `IntangibleObject` adds view 3×1; `TangibleObject` adds view 3×7 and view 6×1; `CreatureObject` adds view 1×2, view 3×7, view 4×14 and view 6×16; `PlayerObject` adds view 3×5, view 6×1, view 8×7 and view 9×18. Counts compose through inheritance. All common, Tangible, and Creature fields are now ordered; remaining semantic gaps are concentrated in historical PlayerObject fields and specialized element codecs.

### Waypoint replication to presentation objects

`PlayerObject` owns two `AutoDeltaMap<NetworkId,Waypoint,...>` instances at `+0x538` (registered in view 8) and `+0x53C` (registered in view 9). Local-player setup `player_replication_callback_install_VA_0065C3B0` installs three callbacks on each map. The view-8 callbacks at `0x0065C640`, `0x0065C920`, and `0x0065CC40` resolve or cast affected objects to `ClientWaypointObject`, perform add/remove/update lifecycle work through the client object/world managers, and publish `PlayerObject::Messages::WaypointsChanged`. The view-9 callbacks at `0x0065CF50`, `0x0065D010`, and `0x0065D110` implement a parallel, map-membership-guarded path.

The destination `ClientWaypointObject` constructor `0x005252B0` is now independently closed at the type level. It inherits `ITNO:3` and adds seven fields in order: `Vector` with callback, bool with callback, `NetworkId`, ASCII string with callback, Unicode string, bool, and ASCII string with color-change callback. Owner callbacks publish `LocationChanged` at `0x00526E20` and `ColorChanged` at `0x005270D0`; the first bool callback updates the global active-waypoint collection/visibility path, and the other ASCII callback applies a presentation value through the current appearance. Exact historical names for the two bools and first ASCII field remain **Tentative** until the Waypoint-to-object assignment call is fully labeled; the types, order, and event boundaries are **Confirmed**.

This promotes the two-layer model from inference to **Confirmed** for PLAY:8: replicated waypoint values drive client waypoint presentation objects, then publish a broad player-waypoints-changed event consumed by waypoint UI. Direct decompilation of insert/remove/replace callbacks confirms materialization through `0x00525B20`, world/object removal and release, and publication after mutation. The view-9 callbacks first search the primary view-8 map by NetworkId and perform their create/update/remove action only when that key is absent from view 8. The presentation layer therefore exposes a union of the two maps with view 8 taking precedence. View 9 is **Confirmed behaviorally as a lower-priority fallback/legacy waypoint source**; its original product-level use case remains Unknown. The current server fills PLAY:8 and sends an empty placeholder shape in PLAY:9. See `ANALYSIS_CHECKPOINT_2026-09-22_WAYPOINT_REPLICATION_RUNTIME.md`.

### Cell, static, and building inheritance boundary

The object constructors now establish the client inheritance/registration side of three early-world object types:

| Wire tag | Client constructor | Base constructor | Added registered fields | Resulting view shape | Confidence |
|---|---:|---:|---|---|---|
| `SCLT` (`CellObject`) | `0x006E58A0` | `ClientObject` `0x00554240` | one view-3 scalar at object `+0x288`, initialized to `-1` | view 3 = four common fields plus cell number; view 6 = one common field | Confirmed |
| `STAO` (`StaticObject`) | `0x00709FE0` | `ClientObject` `0x00554240` | none | only common client-object fields | Confirmed |
| `BUIO` (`BuildingObject`) | `0x0070DBB0` | `TangibleObject` `0x0063EE10` | none in the building constructor | tangible view 3 = 11 fields; tangible view 6 = two fields | Confirmed |

`CellObjectMessage3` in the current server emits the four common object fields followed by the U32 cell number, exactly matching the five client registrations. `StaticObjectMessage3` emits four fields, exactly matching the common view-3 registry. A building is a tangible object on the client; the disabled standalone `BuildingObjectMessage3` in the server also declares 11 fields, matching the composed common-plus-tangible registry. These correlations are **Confirmed across executable constructors and current server packet source**.

The common view-6 field at client object offset `+0x230` uses vtable `0x015DE08C`. MSVC RTTI names only the generic `Archive::AutoDeltaVariable<unsigned long>` type. Its snapshot/delta methods (`0x00542DA0` / `0x00542DC0`) deserialize one fixed U32; direct constructor inspection shows its owner/callback slots remain null, so it has no semantic ClientObject notification in this build. `TangibleObject` adds a second view-6 field, the defender delta vector, so `BuildingObjectMessage6`'s U32 constant followed by an empty vector count/counter is a complete two-field baseline. `CellObjectMessage6` and `StaticObjectMessage6` declare two operands but the client has only the common U32 field; their remaining legacy bytes are not consumed by a registered field. This is direct evidence that the outer baseline path tolerates unused payload bytes in these cases. The historical semantic name remains **Unknown**, but behavior is closed as passive byte-preserved state. See `ANALYSIS_CHECKPOINT_2026-09-22_COMMON_VIEW6_OPAQUE_FIELD.md`.

For a minimal replacement world snapshot, the required behavior is therefore inheritance-driven: create the concrete object first, attach the appropriate ordered schema assembled from its base classes, apply registered fields, and tolerate only explicitly characterized legacy tails. The replacement should not generally accept arbitrary trailing bytes; allowances should be keyed by tag/view and covered by fixtures.

### Complete CREO:6 registration order

The active server builder `CreatureObjectDeltaMessage6` is now explicitly closed against this registry: it uses generic opcode `0x12862153`, tag `CREO`, view 6 and repeated U16 field indices. Its scalar helper indices 2-12 and 16 match the table below; container-owned HAM/max-HAM/wearables/frozen updates occupy 13-15 and 17. Client route `0x0051C76D -> 0x00ADB2D0 -> 0x00554E90 -> 0x0118AA00 -> 0x0118A970` applies and commits the delta.

Constructor order, RTTI-specialized field types, and current server write order now agree on all 18 client fields in `CREO:6`:

| Index | Owner | Server value | Client field evidence |
|---:|---|---|---|
| 0 | ClientObject | legacy/common U32 | field vtable `0x015DE08C`; U32 decoder |
| 1 | TangibleObject | defenders | tangible view-6 delta vector |
| 2 | CreatureObject | level | `AutoDeltaVariable<unsigned short>` at `+0x90C` |
| 3 | CreatureObject | performance animation | `AnimatingSkillData` string callback at `+0x8E0` |
| 4 | CreatureObject | mood string | `AnimationMoodChanged` string callback at `+0x558` |
| 5 | CreatureObject | weapon ID | `CachedNetworkId` at `+0x5C8` |
| 6 | CreatureObject | group ID | `GroupChanged<CachedNetworkId>` at `+0x7A8` |
| 7 | CreatureObject | group inviter ID + invite counter | one callback field holding a pair of NetworkIds at `+0x7E0` |
| 8 | CreatureObject | guild ID | U32/int field at `+0x868` |
| 9 | CreatureObject | target ID | `LookAtTargetChanged<CachedNetworkId>` at `+0x588` |
| 10 | CreatureObject | mood ID | byte callback at `+0x530` |
| 11 | CreatureObject | performance start time | U32/int field at `+0x834` |
| 12 | CreatureObject | performance type | `PerformanceTypeChanged<int>` at `+0x818` |
| 13 | CreatureObject | HAM | `AutoDeltaVector<int>` at `+0x404` |
| 14 | CreatureObject | maximum HAM | `AutoDeltaVector<int>` at `+0x43C` |
| 15 | CreatureObject | equipment/wearables | `AutoDeltaVector<WearableEntry>` at `+0x91C` |
| 16 | CreatureObject | alternate appearance | typed string callback at `+0x954` |
| 17 | CreatureObject | frozen | `AutoDeltaVariable<bool>` at `+0x9A4` |

The server declares 22 operands but emits these 18 logical fields. In particular, group inviter ID and invite counter are two U64 wire values decoded by one pair-valued field object; they are not two schema indices. Because the client snapshot visits its 18 registered fields and ignores the declared count, the mismatch is compatible. This table is **Confirmed** by client constructor registration order, MSVC RTTI, decoder types, and current `CreatureObjectMessage6` ordering.

### PLAY:8 and PLAY:9 registration order

The seven PLAY:8 registrations exactly match the seven current server values:

| Index | Value |
|---:|---|
| 0 | experience map (`string -> int`) |
| 1 | waypoint map (`NetworkId -> Waypoint`) |
| 2 | current force power |
| 3 | maximum force power |
| 4 | completed-quest bit array |
| 5 | active-quest bit array |
| 6 | player quest-data map |

The two U32 zeros written after those fields are therefore a characterized PLAY:8 legacy tail, not additional registered fields.

PLAY:9 has 18 client registrations in this order: ability list; two crafting-state fields; nearest crafting-station NetworkId; draft-schematic vector; crafting field; species field; friends vector; ignore vector; spoken language; current/max food; current/max drink; two unknown U32 fields; secondary waypoint map; Jedi state. Current `PlayerObjectMessage9` writes wire-compatible empty container shapes for disabled friends/ignore/waypoint state and declares 19 operands, but the client registry contains 18. Indices 14 and 15 are confirmed generic U32 AutoVariables at legacy member/value offsets `+0x5C0/+0x5CC` and `+0x5D4/+0x5E0`; their only discovered accessors are `0x00660A20` and `0x00660A30`, and neither accessor has a static caller in this executable. Current server construction emits zero for both. Their original product semantics therefore remain **Unknown**, but they are confirmed dormant with respect to direct client presentation code in this build.

### Complete CREO:3 and bounded PLAY:3/6 ordering

`CREO:3` contains the four common ClientObject fields, seven TangibleObject fields, then seven CreatureObject fields. The derived indices are exactly:

| Index | Value | Client RTTI/type evidence |
|---:|---|---|
| 11 | posture | `ServerPostureChanged<byte>` |
| 12 | faction rank | `RankChanged<byte>` |
| 13 | creature-link/mount ID | `AutoDeltaVariable<NetworkId>` |
| 14 | height/scale | `ScaleFactorChanged<float>` |
| 15 | shock wounds | integer AutoDelta field |
| 16 | state bitmask | `StatesChanged<uint64>` |
| 17 | wounds | indexed integer vector |

This exactly matches the server's declared count of 18 and is **Confirmed** by constructor registration order, RTTI, and `CreatureObjectMessage3`.

`PLAY:3` composes four common ClientObject fields, one IntangibleObject field, and five PlayerObject fields: player bitmask structure, profile-settings structure, skill title, birth date, and total played time. The server declares 11 fields but the client registers 10. The trailing three U32 constants (`0x6C2`, `0xDC62`, `0x23`) are therefore a characterized legacy tail, while the preceding counted bitmask/profile sequences each belong to one complex registered field.

Both leading PLAY:3 structures use the client RTTI type `MatchMakingId`. Codec `0x01223E10` reads a U32 element count followed by that many U32 values into a dynamic vector; it does not require the server's current count of four. The current server labels the first instance player bitmasks and the second profile settings. This establishes their exact wire grammar while leaving the historical client-side `MatchMakingId` naming/semantics partly opaque. Like the generic AutoDelta containers, the legacy codec exposes no schema-specific maximum before count-driven growth; the x64 replacement must bound it.

`PLAY:6` composes the common view-6 U32 and one player privilege byte callback. The server declares three fields but emits these two registered values. No third client field is registered. These conclusions are **Confirmed for count, order, and wire width**; the historical meaning of the two PLAY:3 complex structures beyond their server labels remains **Strongly inferred**.

### Common and Tangible view-3 closure

RTTI makes the four common view-3 registrations exact and reusable across object types: complexity (`float` with `ComplexityChange` callback), `StringId`, Unicode custom name (`NameChanged` callback), and U32 volume. Registration order matches the current server base-line constructors.

`TANO:3` then appends seven fields in this exact order:

| Index | Server value | Client field type/callback |
|---:|---|---|
| 4 | customization string | string `AppearanceData` callback |
| 5 | visible components | specialized component container |
| 6 | options bitmask | U32 `ConditionModified` callback |
| 7 | use count | U32/int AutoDelta value |
| 8 | condition damage | U32 `DamageTaken` callback |
| 9 | maximum condition | U32 `MaxHitPointsModified` callback |
| 10 | object visible | byte AutoDelta value |

`TANO:6` is the common unsigned-U32 field followed by the defender delta vector. This closes the ordered common/Tangible schema inherited by buildings and creatures. The `ConditionModified` and `MaxHitPointsModified` class names reflect the client's generic tangible health model even though current server accessors call them options, condition damage, and maximum condition. The order and widths are **Confirmed**; the semantic meaning of the common view-6 U32 remains Unknown.

`ITNO:3` is also closed: the four common fields followed by one U32 status field registered by `IntangibleObject` at object `+0x288`. `ITNO:6` has only the common U32 registered on this client. The current server then writes ASCII item identifier plus Unicode detailed description as a legacy tail; neither has a corresponding IntangibleObject view-6 field in this executable. This is **Confirmed for the analyzed build/current server pair** and should be a narrowly keyed compatibility case, not a general rule that descriptions are irrelevant to other messages or UI paths.

### CREO:1 and CREO:4 closure

The active builder `CreatureObjectDeltaMessage4` is now explicitly closed against the fourteen-field view-4 registry and generic delta route. Scalar indices 0, 1 and 4-12 match the executable order. Index 2 is the S32 encumbrance vector; index 3 is the ASCII16-to-two-S32 skill-mod map with commands ADD 0, DROP 1 and SET 2; index 13 is the U64 owner-to-U64 mission-object set-map. Whole mission-map updates use command 1 and clear uses command 2. Recipient-specific zero-to-0.1 movement substitutions occur in the server producer and are not decoder rules.

Common view 1 contains bank credits followed by cash credits, confirmed by `CashBalance`/`BankBalance` callback RTTI and server order. CreatureObject appends base HAM and the skill set, producing the complete four-field `CREO:1` schema. The skill field's client type is a set of `SkillObject const*`, while its wire keys are ASCII skill names.

CreatureObject is the first class here to register view 4, with exactly 14 fields. Constructor order matches `CreatureObjectMessage4`: acceleration base/modifier; encumbrance vector; skill-mod list; speed multiplier base/modifier; listen-to NetworkId; run speed; slope angle; slope percent; turn scale; walk speed; water modifier; and the space-mission object set/map. The view is therefore cardinality- and order-complete. Individual skill-mod and space-mission element codecs remain documented as specialized containers rather than native x86 layouts.

### Server delta-index audit

The current server delta helper classes agree with the recovered client indices for all directly implemented CREO and PLAY scalar updates:

- `CREO:1` bank/cash = indices 0/1.
- `CREO:3` customization = 4; posture through states = 11 through 16.
- `CREO:4` acceleration/speed/listen/run/slope/turn/walk/water positions match indices 0-12, with container updates occupying the omitted 2, 3, and 13 slots.
- `CREO:6` level through performance type = 2-12 and alternate appearance = 16; HAM/max-HAM/wearables/frozen occupy the remaining registered slots.
- `PLAY:3` player bits/profile/title/birth/play time = 5-9.
- `PLAY:6` privilege byte = 1.
- `PLAY:8` force current/max = 2/3; experience and waypoints use 0/1 container updates.
- `PLAY:9` language = 9, food/drink values = 10-13, experimentation/crafting/station fields = 1/2/3/5, and Jedi state = 17.

One naming hazard is explicit in the server: `CreatureObjectDeltaMessage3::updateCreatureBitmask()` writes inherited TANO index 6, which client RTTI calls `ConditionModified`, and `updateIncapacitationRecoveryTime()` writes inherited index 7, corresponding to the general integer/use-count slot. The replacement protocol schema must treat `(tag, view, index, codec)` as authoritative and keep server/client semantic aliases as metadata; it must not infer wire meaning solely from one source method name.

The original integer-vector snapshot decoder uses the untrusted U32 element count to drive repeated reads and vector growth; no explicit semantic maximum is visible in `0x0043B050`. Archive reads provide a lower-level boundary, and allocation failure may terminate parsing, but that is insufficient for a new network-facing client.

A replacement must:

1. bound every container to the enclosing baseline/delta payload size before allocation;
2. impose schema-specific count and decoded-byte limits;
3. use checked arithmetic for `count * elementSize`, `index + 1`, and container growth;
4. keep counters and indices at U32/U16 wire widths even on x64;
5. decode into temporary state and commit only after the full container validates;
6. consume or reject unknown commands without applying partial state;
7. never use native pointer or `size_t` layouts as wire representations.

### Weapon, resource-container, and installation derived schemas

`WeaponObject` constructor `0x00709D00` calls the TangibleObject replication constructor and registers no additional replicated fields. It adds only five zero-initialized local DWORDs before installing the `WeaponObject` vtables. Current server `WeaponObjectMessage3` leaves all weapon-specific writes commented out and view 6 is likewise the TangibleObject view. The executable therefore expects only inherited `TANO` fields for this build. This is **Confirmed** and is another example of declared operand counts not defining the actual client schema.

`ResourceContainerObject` constructor `0x0070A230` adds two view-3 fields after the eleven inherited `TANO:3` fields:

| RCNO:3 index | Value | Client type |
|---:|---|---|
| 11 | stack quantity | U32/S32 AutoDelta scalar |
| 12 | resource spawn ID | U64 `NetworkId` callback field |

It adds three fields after inherited common/Tangible view-6 state:

| RCNO:6 index | Value | Client type |
|---:|---|---|
| 2 | maximum stack size | U32/S32 |
| 3 | resource type | ASCII string |
| 4 | resource display name | Unicode string |

Current server `ResourceContainerObjectMessage6` makes seven `insert*()` calls but declares five operands. Its leading `ASCII16("")`, U32 zero, `ASCII16("")`, and empty Unicode value form twelve zero bytes. The inherited client common-U32 decoder consumes the first four; the inherited defender-vector decoder consumes the next eight as zero count and zero counter. The final U32, ASCII, and Unicode values then align with RCNO indices 2-4. This byte-level compatibility is **Confirmed across constructor order and server serialization**; source-level insert-call count is misleading.

The server delta helper's quantity index `0x0B` is correct. Its commented resource-ID index `0x0E` is outside the client's thirteen-field view-3 registry; its view-6 setters use indices `0x05` and `0x06`, outside the client's five-field registry. This directly explains the source comment that those view-6 deltas cause a client crash. Correct indices are 12 for resource ID, 3 for resource type, and 4 for resource name. Do not enable the existing helpers unchanged.

The resource-ID callback `0x0070A9E0` marks object `+0x460` dirty. Class virtual `0x0070A570` later derives a texture from the replicated resource type/name, applies it to the object's appearance under tag `REP0`, and clears the dirty flag. Thus resource-container presentation depends on a runtime resource-to-texture mapping, not merely its object template.

`InstallationObject` constructor `0x0070AD70` adds three view-3 fields after `TANO:3`: index 11 active U8 (`InstallationActive` RTTI), index 12 surplus power F32, and index 13 base power rate F32. Current server `InstallationObjectMessage3` writes exactly those bytes despite declaring five operands, and its active delta index `0x0B` matches the client. No InstallationObject-specific view-6 fields are registered in this class.

### Manufacture schematic and factory-object schemas

`ManufactureSchematicObject` constructor `0x00701500` inherits `IntangibleObject` and appends four view-3 fields. RTTI and registration order give the complete composition:

| MSCO:3 index | Value | Client type |
|---:|---|---|
| 0-3 | common ClientObject state | complexity, StringId, Unicode name, volume |
| 4 | intangible status; used by this server as manufacture limit | U32 |
| 5 | crafting values | `AutoDeltaMap<StringId,float>` |
| 6 | creator/player name | Unicode string |
| 7 | integer scalar | S32; server sends 25 |
| 8 | float scalar | F32; server sends 8.0 |

The map snapshot codec at `0x00703190/0x007032C0` is U32 element count, U32 update counter, then per entry U8 operation/state, `StringId`, and F32 value. This matches the server's one `crafting/complexity` entry. `updateCraftingValues()` correctly targets index 5. However, `updateManufactureLimit()` also targets index 5 and writes only an integer, while the baseline places the manufacture limit in inherited index 4. Repository-wide source search found no call to that helper, so the defect is presently dormant in the inspected tree; it still must not be copied or activated unchanged.

The same constructor appends five view-6 fields after the common U32:

| MSCO:6 index | Client type | Current server baseline interpretation |
|---:|---|---|
| 0 | common U32 | constant `0x76` |
| 1 | ASCII string with `AppearanceData` callback | empty |
| 2 | ASCII string with `Appearance` callback | empty |
| 3 | U32 callback field | schematic CRC |
| 4 | bool | active crafting flag |
| 5 | U8 callback field | resource-slot index/state, initially zero |

The server's single U32 zero encodes two consecutive empty ASCII16 values, and its final U16 encodes the bool plus the initial U8. This is another valid byte-level packing shortcut hidden by source-level `insert*()` calls. `insertToResourceSlot()` correctly targets view-6 index 5.

The callback path is also recovered. A change to index 3 resolves the schematic CRC through the client object-template registry, instantiates/fetches the associated draft-schematic appearance, and publishes `ManufactureSchematicObject::Messages::ChangeSignal`; invalid CRC or appearance paths emit explicit warnings. Index 1's `AppearanceData` callback validates a `SharedTangibleObjectTemplate`, constructs a custom appearance, and republishes change. Index 2 applies appearance/customization data to the live appearance when possible. Index 5 publishes the same change signal. These are presentation reactions after field mutation, not part of the wire codec, and belong behind a replacement replication-to-presentation adapter.

The executable RTTI class named `FactoryObject` at constructor `0x0070D940` calls `TangibleObject` directly and adds no fields. It is therefore a tangible factory-related object/class in this build, not evidence that all server installation factories inherit this exact client class layout.

### Client mission object schema

`ClientMissionObject` constructor `0x006C34B0` inherits the five `ITNO:3` fields and appends twelve, producing the exact 17-field `MISO:3` registry declared by the current server:

| MISO:3 index | Value | Client type / callback |
|---:|---|---|
| 5 | difficulty | S32 |
| 6 | destination | `Location` |
| 7 | creator name | Unicode string |
| 8 | reward credits | S32 |
| 9 | start location | `Location` |
| 10 | target template CRC | U32 |
| 11 | description | `StringId` |
| 12 | title | `StringId` |
| 13 | refresh counter | S32 callback |
| 14 | mission type CRC | U32 callback |
| 15 | target name | ASCII string |
| 16 | waypoint | `Waypoint` callback |

Current `MissionObjectDeltaMessage3` uses exactly these indices. A `Location` is serialized by the server as F32 x/z/y, U64 object ID, and U32 planet CRC. The waypoint serializer contains its own position, target/object IDs, planet/name, color, and active state; its full byte order remains defined by `WaypointObject::insertToMessage` and the client `Waypoint` codec rather than by native layout.

The three callback fields have distinct effects. Refresh-counter change `0x006C3B00` publishes `ClientMissionObject::Messages::EnteredMissionBag` and then refreshes mission presentation. Mission-type change `0x006C3F30` selects a type-specific appearance/template from a fixed client mapping and updates presentation. Waypoint change `0x006C3D90` resolves or creates a `ClientWaypointObject`, associates it with the mission/mission bag when valid, and refreshes the mission. This gives an end-to-end server delta -> field codec -> client callback -> mission UI/presentation chain at indices 13, 14, and 16.

## Next unknowns

### Tangible view-3 baseline/delta closure (2026-09-15)

The base `TANO:3` schema is now closed as eleven ordered fields: common complexity, `StringId`, Unicode custom name and U32 volume, followed by ASCII customization data, visible-component container, U32 options, U32 use count, U32 condition damage, U32 maximum condition and U8 object visibility. `TangibleObjectDeltaMessage3` actively emits indices 0, 1, 2, 4, 6, 7, 8 and 9; collection producers emit index 5 through `AutoDeltaSet`. Volume and visibility have no direct methods in that builder.

The executable's customization codec `0x00641030/0x00641180` confirms extended ASCII decoding and appearance-data notification. Correction: TANO:3 index 5 is object `+0x328`, RTTI `AutoDeltaSet<int>`, whose `0x00642B80/0x00642F00` snapshot and `0x00642BE0/0x00643050` delta codecs use four-byte keys exactly like server `AutoDeltaSet<int>`. The eight-byte `0x006421A0/0x006423E0` codecs belong to the separate `AutoDeltaVector<CachedNetworkId>` at `+0x2F0`, registered into another view. There is no visible-component width defect.

Derived object tags reuse this view by schema composition. A replacement must preserve these fixed wire widths independently of x64 native layout and must invoke presentation reactions only after atomic replication commit.

### Tangible view-6 defender closure (2026-09-15)

`TANO:6` consists of common U32 index 0 and an ordered defender vector at index 1. The baseline writes `0x76`, then vector count/counter and repeated U64 object IDs. `TangibleObjectDeltaMessage6` has no methods of its own; active `TangibleObjectImplementation` code calls `startUpdate(1)` and writes directly through `DeltaVector<ManagedReference<SceneObject*>>`.

Commands are 0 remove plus U16 index, 1 add plus U16/U64, 2 replace plus U16/U64, and 4 clear. `setDefender` uses two replacements to move the selected defender to index zero, proving order and the primary-defender position are semantic. Client offset `+0x2F0` is registered into view 6 separately from the view-3 component container. Replacement state must retain U64 IDs and resolve lifecycle-safe handles only after atomic commit.

1. Recover the remaining historical semantics in PLAY:3/8/9; ability, draft-schematic and quest-data codecs are closed, while PLAY:9 indices 14-15 have no direct static consumers found so far.
2. Determine the historical producer/use case of the secondary PLAY:9 fallback waypoint map; precedence behavior is resolved.
3. Historical naming of the passive common view-6 U32 remains optional; its exact codec, null callback ownership, family constants and keyed legacy-tail policy are closed.
4. Archive failure ownership is closed through the received-message queue: checked reads throw, while field/view dispatch, typed parser, GroundScene/event fanout, connection dispatch and queue drain contain cleanup unwinding but no local catch/recovery. See `ANALYSIS_CHECKPOINT_2026-09-22_REPLICATION_DECODE_FAILURE_BOUNDARY.md`.
5. Continue representative derived-object schemas and active ObjectController payloads; MSCO is now closed, including one unsafe current-server delta helper.

## Runtime ship transform stream (2026-09-15)

`0x76026FB9` is not a baseline/delta member, but it is the live motion stream
that consumes SHIP:6's U16 unique-ID lookup and effective flight parameters.
Its fixed payload is U16 ID, I8x4 quaternion, I16 X/Z/Y position, I16 speed,
U16 packed direction, I8 yaw/pitch/roll rates and U32 stamp. The original local
sender writes ID zero; the server selects the ship from the pilot session and
broadcasts using the actual recipient-visible ID. Local controllers reconcile;
remote controllers interpolate. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_TRANSFORM_CONTROL.md`.


## Bounded implementation update — October 2, 2026

The replacement now decodes the complete eighteen-field [CREO:3 baseline](CREATURE_VIEW3_BASELINE_CONTRACT.md) and ordered [scalar deltas at indices11–16](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md). It preserves raw byte strings/UTF16 units, collection entries/counters, scalar bits and declared-count tolerance; malformed tails and budgets reject before state publication. Raw component-entry preservation does not establish original duplicate-set semantics. [Scalar state](SCENE_CREATURE_SCALAR_STORE_CONTRACT.md) requires an explicit caller seed and exact registry revision. The offline world still records opaque envelope history rather than automatically applying these codecs. These are implementation gates for previously documented schemas; the345-row packet matrix and research denominators do not change.

## Round9 implementation boundary

[Stable-incarnation binding](SCENE_CREATURE_REPLICATION_STORE_CONTRACT.md) now preserves complete CREO3 baselines and current scalar overlays across containment/finalization. [Wounds field17](CREATURE_VIEW3_WOUNDS_DELTA_CONTRACT.md) decodes all five established vector commands with atomic bounded application. Its strict modulo-interval/index policy is modern; original counter overflow/growth/ignore details remain open. These implementations change no schema/name/packet denominator and are not yet automatically applied by the world coordinator.

## 2026-10-02: exact CREO3 components integer-set kernel

[Components contract](CREATURE_VIEW3_COMPONENTS_DELTA_CONTRACT.md) closes field5 at object+0x328 from pinned snapshot00642F00/delta00643050 and signed tree/serializer helpers. Snapshot raw S32 order/duplicates remain evidence; original runtime deduplicates and serializes signed ascending members while copying the raw counter unchanged. Confidence: Confirmed direct instructions plus27 anchors/69 independent fixtures. Replacement raw snapshot encoding deliberately preserves original input entries separately from normalized runtime state.

For N commands, C incoming and L local U32 counter: skip0 if unsigned (L+N) mod2^32 <= C; otherwise skip=min_unsigned((L-C+N) mod2^32,N). Apply remaining drop/add/clear in order; increment L moduloU32 only for actual membership changes. Finish each field with unsigned max(L,C), even for zero commands. Future-gap and wrap cases discriminate this kernel from the wounds vector, which has no initial sum gate or final forced counter. This conclusion is specific to the directly checked integer set; no uninspected skill/map specialization is silently assigned its exact arithmetic. Unknown commands reject in the modern bounded codec; original installed callbacks/outer partial effects remain unresolved. Components-world integration is deferred.


## Round13 bounded implementation composition

The [composite CREO3 codec](CREATURE_VIEW3_COMPOSITE_DELTA_CONTRACT.md) and [optional Mode3 world](SCENE_WORLD_REPLICATION_CONTRACT.md) now compose recovered field5 set membership and fields11..17 scalar/vector updates into one incarnation-bound result/acknowledged observation. Raw full baseline order/duplicates/counter remain separate from normalized current membership; distinct set/vector arithmetic is unchanged. Synthetic18 codec cases,69 recovered-set runtime cases and original-resource framed integration agree. This adds implementation composition, no new wire keys, executable semantic names, notification callback or gameplay claims.

## Round14 bounded mixed-world routing

The [generic coordinator](SCENE_MIXED_WORLD_CONTRACT.md) resolves explicit catalog recipe during ordered replay before applying owned replication envelopes. CREO typed modes remain creature-only; static TANO is bounded opaque history and has no inferred field schema. Static CREO or creature TANO rejects before state/history publication and retires the modern world generation. Legacy early typed decoding remains unchanged. This is implementation policy over accepted envelope bytes; no wire keys, server packet rows, executable names or protocol coverage counts change. Original typed static families/views remain separate work.

## Round19 appearance customization ingress

[Field4 ingress](CREATURE_APPEARANCE_CUSTOMIZATION_INGRESS.md) connects the known
U16/extended-U32 byte string to original nonempty appearance callback, unescape/
version1 records, installed CIDM name lookup and typed variable setter. Outer
CREO baseline and opaque envelopes preserve owned bytes, including noncanonical
extended lengths; current typed CREO field4 delta application is still absent.
Six independent exact cases and947 negative probes pass. Original inner reader
can mutate earlier setters before later failure and does not test final decoded
tail on success; strict atomic replacement parsing must be labeled modern policy.
Typed setter conversion/clamping and actual player values remain open. No schema,
packet or provisional-name denominator changes.

## Round20 customization boundary

[Packed version1 contract](PACKED_CUSTOMIZATION_CONTRACT.md) and [CIDM dictionary](CUSTOMIZATION_ID_DICTIONARY_CONTRACT.md) now have owned codecs/original byte fixtures. Confirmed U8/S16 and range rejection apply to the two concrete classes traced there. Supplied presentation is implemented separately; current-incarnation rawfield4 delta/state and live variable mounting remain next. Existing345 packet-source rows do not imply345 executable handlers and are unchanged.

## Round21 integrated boundary

Round21 implements CREO:3 field4-only deltas: ordered immutable strings, raw declared count, U16/FFFF+U32 lengths and preserved extended-small spelling. Store journals every update with exact-current acknowledgment; projection owns full explicit values. Mixed4+scalar/component/wounds and inherited TANO routing remain deferred. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_WIRE_CUSTOMIZATION_INTEGRATION.md).

## Round22 integrated boundary

Round22 ordered CREO:3 union now accepts4 plus11..16; raw/scalar order/count/extended spelling preserved. Native rejection preserves all state candidates. Indices5/17 with4 and inherited TANO remain deferred. [Evidence/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_INTEGRATION.md).

## Round23 integrated boundary

The supported CREO:3 union4/5/11–17 now stages raw customization, scalars, components, wounds and pending world state together. Original selected ACST/CIDM context supplies exact ranges and authored defaults through five already owned palettes. Main independent native, headless/GPU, motion and held-container gates pass. Broader fields/views, live mounts/player seeds and graphical attachment/wound behavior remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_FULL_CREATURE_INTEGRATION.md).

## Round24 integrated boundary

Supported CREO3 fixed common fields0/3/6–10 join the existing union. Current common state survives older ingress; visibility uses that overlay while hidden appearance work commits. Actual delta headless/GPU/motion and separate held-baseline visibility proofs pass. Names1/2, TANO, gameplay meanings and live interoperability remain open. STF variant1 exact owned lookup passes two original tables; locale/cache/fallback is deferred. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_CREATURE_INTEGRATION.md).

## Round25 named creature increment

The supported ordered CREO3 union now covers indices0–17, including raw StringId/custom names. Current name overlays and variable charge survive older ingress. Bounded original STF1 resolution drives an optional caption; actual delta headless/GPU and separate held-baseline name proofs pass. Explicit modern name precedence/directory/display policies do not establish original UI/locale behavior. Other families/views and live interoperability remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_NAMED_CREATURE_INTEGRATION.md).

## Round26 tangible stages

Complete owned TANO3 baseline and ordered0–10 delta codecs, standalone current tangible store and bounded original STF preload pass independent native oracles. Static supplied-projection caption/visibility passes original terminal GPU and lifecycle checks. Lead codec-store-preload join passes; atomic typed SceneWorld publication is next. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R26_TANGIBLE_STAGE_INTEGRATION.md).

## Round27 typed tangible publication

Optional complete TANO3 world state commits with its journal after private preparation. Static typed token gates actual original terminal caption/raw visibility beside supported CREO ingress; five headless/GPU states, retained resources and teardown pass independent oracles. Default opaque behavior remains. Authored template names and local SWTS selector semantics are separately documented; outer clock and live interoperability remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TANGIBLE_WORLD_INTEGRATION.md).

## Round28 exact TANO6 codec boundary

[Owned TANO6 codec](TANGIBLE_VIEW6_CODEC_CONTRACT.md) closes two-field passiveU32/orderedU64-ID consumption and specialized assignment overshoot with pinned static evidence and deterministic exact/malformed/cap fixtures. Raw counters are preserved, no defender-state/world application yet. Existing packet-source inventory is unchanged; no gap-matrix row changes are warranted by this implementation stage.

[Round29 TANO6 state policy](TANGIBLE_VIEW6_STATE_POLICY.md) confirms specialized counter arithmetic, zero-filled growth and mutation/no-op increments. Codec exists; state/world integration remains pending. Modern whole-message atomicity must be distinguished from original partial snapshot effects.

Round30 [owned TANO6 state](TANGIBLE_VIEW6_STATE_CONTRACT.md) applies the accepted specialized codec/policy with immutable modern candidate publication and events. World current-incarnation join remains pending; raw modulo32 counter and whole-assignment rules are preserved.

## Round31 view6 inheritance and publication

[CREO6 prefix contract](CREATURE_VIEW6_PREFIX_CONTRACT.md) consolidates directly verified shared TANO6 field objects0–1 and creature fields2–17, including raw strings with FFFF/U32 lengths and a two-U64 field. Shared schema does not mean contiguous delta byte prefix or permission to accept CREO in the TANO-only codec. Existing packet rows/denominators remain unchanged.

[Owned TANO6 store](SCENE_TANGIBLE_VIEW6_STORE_CONTRACT.md) separately publishes modern immutable state/journal against exact registry currentness. Historical event acknowledgment is not target delivery. Coordinator/router integration remains pending.

## Round32 accepted world and terrain inputs

[Complete CREO6 baseline codec](CREATURE_VIEW6_CODEC_CONTRACT.md) consumes all eighteen registered fields, preserves declared-count metadata and raw strings/bits/counters, and supports ordered deltas0,2–12,16–17. Field1 and13–15 delta bodies are explicitly unsupported by this codec. [Optional TANO6 world integration](SCENE_WORLD_TANGIBLE_VIEW6_CONTRACT.md) seeds the static end barrier, publishes immutable current-incarnation state and exposes guarded event receipts. World budgets remain separate partitions; no weak target resolution or live delivery is established. Packet inventory and source-gap rows are unchanged.

## Round33 terrain structure and creature state

Owned TGEN inputs expose 257 layers and 87 AHCN records in the same pinned Tatooine file while retaining unsupported rules. Exact nine-sample terrain assembly and seam branch priority pass independent static/native checks. Complete current CREO6 scalar/string/pair models now have bounded incarnation-owned publication and a prepared commit seam. Separately, the exact HAM/max-HAM vector codec and mutation kernel pass independent wire/state/counter/work oracles. These remain separate partitions, not a composed full CREO6 world update. Original mixed-world headless and combined Core regressions pass. Height evaluation, group semantics, vector composition, original owner side effects and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain unchanged. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R33_TERRAIN_AND_CREATURE_STATE.md).

## Round34 creature world and terrain arithmetic

[Supported CREO6 world partition](SCENE_WORLD_CREATURE_VIEW6_CONTRACT.md) requires its complete baseline before the creature end barrier and guards current event delivery. [Ordered composite union](CREATURE_VIEW6_COMPOSITE_CONTRACT.md) adds13/14 HAM to scalar/string/pair updates, preserving interleaved order under one immutable result. Defender1/wearable15 deltas, composite world routing and original owner effects remain open; packet rows unchanged. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R34_CREATURE_AND_TERRAIN_KERNELS.md).

## Round35 terrain and composite publication prerequisites

[Wearable field15](CREATURE_WEARABLES_VIEW6_CONTRACT.md) uses object-ID-only set equality: changed customization/arrangement/CRC alone do not replace an existing equal-ID entry. Sparse defaults are empty customization, arrangementFFFFFFFF, ID0, CRC0. This narrow projection is separate from scalar/HAM composition and world publication; defender1 remains open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R35_TERRAIN_AND_COMPOSITE_PREREQUISITES.md).

## Round36 composite world and terrain boundaries

[Defender1](CREATURE_DEFENDERS_VIEW6_CONTRACT.md) directly inherits the established CachedNetworkId specialization: full U64 equality, zero sparse defaults and exact counters/ordered notification descriptions. [Scalar/HAM world](SCENE_WORLD_CREATURE_VIEW6_COMPOSITE_CONTRACT.md) accepts the frozen ordered union only; defender1/wearable15 remain separate and rejected by that union until combined accounting is accepted. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R36_COMPOSITE_WORLD_AND_TERRAIN_BOUNDARIES.md).

## Round37 complete creature kernel and supplied terrain

[Complete CREO6 delta composition](CREATURE_VIEW6_FULL_CONTRACT.md) supports indices0..17, retaining raw baseline declared22 metadata and all vector/string/counter semantics. Full event descriptions charge48 logical bytes; inherited construction journals remain separately bounded. Frozen scalar/HAM world/store partitions still reject1/15. Owner private views and gameplay/resource callbacks remain separate. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_COMPLETE_CREATURE_AND_SUPPLIED_TERRAIN.md).

## Round38 full creature publication and terrain inputs

[CREO4 complete baseline/scalars](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md) directly verifies fourteen registrations, all snapshot specializations and eleven scalar delta indices. Client mission field13 is a set of complete NetworkId pairs; owner-keyed server map correlation does not establish client mutation. Collections2/3/13 delta semantics remain separately closed. Full CREO6 world publication is now accepted, with48-byte event accounting. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_FULL_CREATURE_PUBLICATION_AND_TERRAIN_INPUTS.md).

## Round39 supplied terrain grounding and CREO4 prerequisites

The separate [CREO4 field2 encumbrance kernel](CREATURE_ENCUMBRANCES_VIEW4_CONTRACT.md) closes all five exact vector commands. Field3/13 deltas and complete view4 composition remain next. Existing packet-source inventory boundaries and345 rows are unchanged; this is an internal view-field closure. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_TERRAIN_GROUNDING_AND_PRIVATE_VIEW.md).
## Round40 layer and CREO4 composition

[Restricted scalar/encumbrance composition](CREATURE_VIEW4_SCALAR_ENCUMBRANCE_CONTRACT.md) and the separate [skill-map kernel](CREATURE_SKILL_MODS_VIEW4_CONTRACT.md) now pass independent ordered wire/state/budget oracles. Field3 supports opcodes0..2; unknown-opcode framing is context-dependent and excluded. Field13 and complete CREO4 publication remain next. No outer packet-source boundary or345-row inventory change is claimed.
## Round41 supplied height execution and CREO4 fields

[The supported CREO4 union](CREATURE_VIEW4_SUPPORTED_CONTRACT.md) now includes skill maps alongside eleven scalars and encumbrances. [Mission-set13](CREATURE_MISSION_OBJECTS_VIEW4_CONTRACT.md) is independently implemented with complete signed64 pair ordering. Full union/publication is next; packet-source inventory is unchanged.
## Round42 complete private state and supplied terrain sequence

[Full14-field CREO4 union](CREATURE_VIEW4_FULL_CONTRACT.md) and [complete prepared storage](SCENE_CREATURE_VIEW4_FULL_STORE_CONTRACT.md) now pass independent wire/model/event/identity/global-cap oracles. Full world ingress and original owner effects remain separate. Packet-source inventory is unchanged.

## Round43 private world and current-height filters

Optional complete CREO4 world publication and original-resource headless/GPU joins pass independent full-model, ordered-event, variable-journal and cleanup checks. Query-time FHGT supplied execution and its two-layer geometry/query join pass independently. FSLP raw structure is implemented; initializer timing/x87 sine preprocessing remains unresolved. Server observer CREO4 deltas can arrive before end-baselines without a full view4 baseline; original default/template initialization and modern admission for that path remain the next compatibility milestone. The current seeded finalized private route does not yet cover it. Original recursive terrain, gameplay and live interoperability remain open. Research89%/readiness94%,2626 names/zero duplicates and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R43_PRIVATE_WORLD_AND_HEIGHT_FILTERS.md).

## Round45 accepted compatibility increment

The [Round45 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_45_2026-10-04.md) records independently reviewed/replayed supplied
AHFR/FFRA consumers, an incarnation-bound received-only partial CREO4 store,
one human movement-template input chain and the actual current-height FHGT/AHCN
Godot preview. The store accepts pending/finalized objects without default seeds
but has no world/router admission yet. Original fractal generation/cache lifetime,
floating controls, complete terrain and later creature writers remain unknown.
Research scores and canonical name/packet denominators remain unchanged.

## Round46 accepted compatibility increment

The [Round46 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_46_2026-10-04.md) records received-only CREO4 world/router admission and its original-resource headless/GPU preview, an owned authored movement profile, and static family-map/sampler prerequisites. Partial/full4 policies are mutually exclusive; no default seed is fabricated. Fractal cache misses can return retained wide values while hits reload F32, so supplied-F32 consumer acceptance is not original sampler parity. Research scores,2626 canonical names and345 packet-source rows are unchanged.

## Round47 compatibility increment

The [received movement reader](SCENE_WORLD_RECEIVED_CREATURE_MOVEMENT_INPUT_CONTRACT.md) classifies the ten established CREO4 F32 slots as missing/finite/nonfinite and binds exact current world/phase provenance. It changes no wire grammar or packet-source row. [Authored and received selection](EXPLICIT_AUTHORED_RECEIVED_MOVEMENT_JOIN_CONTRACT.md) remain distinct; no defaults are merged.

## Round48 compatibility increment

[Original movement writers](CREATURE_MOVEMENT_WRITER_CONTRACT.md) show constructor and received run/walk updates feed the same mutable current slots, with alternate-source/modifier branches in consumers. Modern received-only state remains distinct from authored provenance; explicit admission does not implement original full arbitration. Packet registries/counts are unchanged.
