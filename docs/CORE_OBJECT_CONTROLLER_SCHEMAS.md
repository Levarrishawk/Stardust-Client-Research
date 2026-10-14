# Core Server-to-Client Controller Schemas

These are current Stardust-3 constructor layouts for high-frequency controller messages whose type registration was independently found in Stardust.exe. Fields listed here begin at outer offset `0x16`; the first U32 is the standard scalar written by `ObjectControllerMessage` unless noted.

| Type | Wire payload from `0x16` | Intended client effect | Confidence |
|---:|---|---|---|
| `0xCC` | U32 scalar, U32 animation CRC, U64 attacker, U64 weapon, U8 attacker posture, U8 trails, U8 unknown, then defender records introduced by U16 ordinal/count; client stores 0x18-byte decoded records; basic server record `{U64 defender,U8 posture,U8 hit,U8 effect}`, multi-target form appends U8 hit-location and U8 initial-damage | client submits zero- or one-defender combat presentation; if decoded defender count exceeds one it logs and discards the entire action | Confirmed constructor bytes, client routing and legacy single-defender limit; exact client record-member names incomplete |
| `0xDD` | U32 scalar, U64 NPC, U8 starter, ASCII16 text/key, U32 mobile CRC | open NPC conversation | Confirmed |
| `0xDE` | U32 scalar, U64 NPC | close NPC conversation | Confirmed |
| `0xDF` | U32 scalar, either Unicode32 text, or U32 encoded-size + U16 unknown + serialized `StringIdChatParameter` | display NPC conversation text | Confirmed variants; nested string-id structure tracked separately |
| `0xE0` | U32 scalar, U8 option count, repeated Unicode32 option | populate conversation response list | Confirmed |
| `0xF2` | U32 scalar, ASCII16 animation name | active creature controller resolves and plays the name across controlled-object appearance animation targets | Confirmed server layout, client codec and effect |
| `0xF4` | U32 envelope scalar, U64 sender, U64 chat target, Unicode32 text, U16 volume/type/mood, U8 flags/language, Unicode32 OOB and Unicode32 source name; current literal/@localized producers additionally leave eight zero tail bytes | render spatial chat with mood/language/range behavior | Confirmed original reader/writer and current constructors; [bounded source adapter](NGE_PRECU_SPATIAL_CHAT_CONTRACT.md), nested prose consumer fidelity remains separate |
| `0x116` | U32 scalar, U32 action count, U32 command CRC, U64 target, empty Unicode32 arguments | server-originated enqueue command | Confirmed |
| `0x117` | U32 scalar, U32 action count, F32 timer, U32 table value 1, U32 table value 2 | remove/update command queue entry | Confirmed layout; table semantics Unknown |
| `0x12E` | U32 scalar, U64 sender, U64 emote target, U32 emote ID, U8 flags (`bit0` animation, `bit1` text) | display/play emote | Confirmed |
| `0x131` | U32 scalar, U8 posture, U8 force/update flag (server writes `1`) | prepare dependent appearance/controller state, then apply posture and notify creature state machinery | Confirmed; executable factory and effect route `0x006392E0 -> 0x00469410 -> 0x004328D0` |
| `0x134` | U32 scalar, U64 attacker, U64 defender, U64 item, U32 damage, ASCII16 STF file, U32 zero, ASCII16 STF key, U8 color, Unicode32 custom text | display localized or custom combat-spam line | Confirmed |
| `0x13B` | U32 scalar, U64 cell/parent, F32 X, F32 Y, F32 Z | seat object at parent-relative location | Confirmed server order; coordinate convention needs client transform correlation |
| `0x140` | U32 scalar, U64 weapon, F32 ideal range, F32 max range, S32 point-blank accuracy, S32 ideal accuracy, S32 max-range accuracy | resolve the weapon and directly replace its five client combat-range/accuracy properties | Confirmed server layout and executable state writes |
| `0x166` | U32 scalar, S32 flourish ID | play entertainer flourish | Confirmed |
| `0x167` | U32 scalar, U32 action count, U32 command CRC | add command queue marker | Confirmed |
| `0x1BD` | U32 scalar, U64 target, ASCII16 STF file, U32 zero, ASCII16 STF entry, F32 scale, U8 red, U8 green, U8 blue, U8 flags (server `5`) | show colored fly text above target | Confirmed |
| `0x1DB` | U32 scalar, U64 described player, Unicode32 biography | display biography | Confirmed |
| `0x1E7` | U32 scalar, U32 player count, repeated player records containing U32 bitmask marker, four U32 bitmask words, Unicode32 name, U32 species, ASCII16 region, ASCII16 zone, ASCII16 guild, ASCII16 title | populate “players near you” results | Confirmed for records with a PlayerObject; null-ghost record behavior is structurally suspicious |
| `0x1FC` | U32 scalar, U32 location count, repeated `{ASCII16 location, ASCII16 planet, F32 x, F32 y, ASCII16 cell, ASCII16 image, ASCII16 description, U8 enabled}` | populate character-creation starting-location list | Confirmed current server serializer; server stores additional Z/radius/heading data but does not put it on this wire record |
| `0x229` | U32 scalar, U32 buff CRC, F32 duration seconds | add timed buff icon/state | Confirmed |
| `0x22A` | U32 scalar, U32 buff CRC | remove buff icon/state | Confirmed |
| `0x441` | Server: U32 scalar, ASCII16 quest name, U32 maximum, Unicode32 STF label, U32 zero, U32 current. Client registration: timer-shaped message with only ASCII string, U32, Unicode string, U32 | server intends quest-task counter; client decodes it as timer | Confirmed mismatch. Server constructor parameter `taskId` is not serialized |
| `0x444` | U32 scalar, ASCII16 quest name, U32 task ID, Unicode32 STF label, U32 seconds | server intends quest-task timer | Confirmed server construction; no Stardust.exe registration recovered |

Stardust.exe's actual quest counter is type `0x440` with the five-field counter-shaped payload. A replacement may intentionally accept the server's `0x441`/`0x444` profile, but must not describe those IDs as original-client-compatible.

Additional confirmed response layouts:

| Type | Wire payload from `0x16` | Intended client effect |
|---:|---|---|
| `0xEE` | U32 scalar, U32 referenced message type, U8 status, U8 correlation | acknowledge a controller request such as harvester hopper operation |
| `0x147` | U32 scalar, U64 target, U64 player, U32 option count, repeated `{U8 index,U8 parent,U8 radial,U8 callback,Unicode32 text}`, U8 request counter | install/show the context radial menu and correlate it with the request |

## Replacement design consequence

The decoder should expose typed, direction-specific DTOs and then translate them into Godot-facing events. Conversation, combat, and spatial-chat wire variants should remain protocol-layer variants rather than leaking packed/legacy optional layouts into UI or animation code.

Malformed list/count records must fail before state mutation. In particular, combat defender records and “players near you” entries should be parsed with remaining-length checks because current server constructors contain conditional record shapes.
