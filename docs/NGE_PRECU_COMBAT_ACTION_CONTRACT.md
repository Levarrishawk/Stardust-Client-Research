# Pre-CU combat-action boundary

October14,2026. A stationary-world first-throw trace identifies MessageQueueCombatAction::unpack +194, followed by ControllerMessageFactory, ObjControllerMessage and the connection listener. It ends in an unhandled dispatch exception and exit3 on the previous DEFF032F build. No command was deliberately executed. This is direct decoder identity evidence; it does not establish whose combat action was broadcast or its exact payload bytes.

## Original client and current server

The original Stardust.exe SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 was reverified. Registered reader0119C330 reads U32 animation at0119C39B, U64 attacker/weapon through0119C3B9/3C3, then single-byte attacker posture/trails/effect at0119C3D6/409/431. U16 defender count is read at0119C459. Each loop0119C473..4E8 consumes U64 defender and three single bytes for posture, defense and effect, then calls0119C050. **Confirmed widths/order.** The raw three bytes are preserved; additional names are correlated with source constructor and consumer roles.

All fixed scalars are little endian. After the standard controller scalar, the basic body is25+11*N bytes: U32 animation, two U64 IDs, three U8 attacker fields, U16 count, then N eleven-byte defenders. Zero defenders is valid. No actionNameCrc, location discriminator/coordinates/cell, hit-location or U16 damage exists in this original reader. NGE adds those fields, causing the observed overread on the shorter target boundary. **Confirmed layout mismatch; causal attribution strongly supported by the fault route and exact source difference.**

Server correlation: Stardust-3/MMOCoreORB/src/server/zone/packets/object/CombatAction.h constructors emit flags0x1B/type0xCC. Basic constructors and addDefender emit the original form. A distinct list constructor emits repeated U16 ordinal followed by thirteen-byte records: the eleven original bytes plus U8 hit-location and U8 initial-damage. It caps at20; it does not emit a conventional count plus homogeneous thirteen-byte array. Null list entries can skip ordinals. This is **confirmed local server construction**, not original-client parser acceptance or deployed emission proof.

## Conversion policy

PrecuCombatActionProfile is an engine-neutral DTO codec. It accepts an exactly sized basic counted array, or an explicitly distinguished extended form beginning ordinal1 with consecutive complete records up to20. It rejects unexplained tails, impossible counts, incomplete records and skipped/reordered ordinals before constructing the controller message. Basic counts are bounded by available bytes and U16 capacity. Extended ordinal gaps are unsupported rather than guessed. IDs preserve all64 bits; animation/byte values are not widened into a new wire format.

The adapter supplies zero for absent NGE action-name/location and basic hit-location/damage fields. Extended values retain their U8 width. A private format flag preserves extended packing; NGE-only pack data is rejected before writing. False-profile NGE layout remains unchanged. Registered controller capability remains server-to-client only.

Original common application006E4950 permits zero or one defender and ignores the whole action for more than one, as established in [controller effects](CLIENT_CONTROLLER_EFFECTS.md). The source presentation adapter restores that limit, lets zero-defender actions reach playback with a null defender, and avoids NGE command-table animation suppression requiring an absent actionNameCrc. Resolving unknown/missing objects remains the existing consumer's responsibility. This codec does not make authoritative damage, HAM, targeting, command timing or combat gameplay accepted.

## Fixtures and live limits

Native fixtures use independently authored basic0/1/2 and extended1/2/20 bodies, the actual registered0xCC factory/application envelope, exact round trips, high-bit64-bit IDs, all truncated prefixes, bad ordinals/counts/tails, >20 extended rejection, absent NGE defaults and retained NGE location/damage round trip. A prefix that is itself a complete basic/extended packet must remain valid: for example an extended one-record packet shortened by its final two bytes is indistinguishable from a valid basic one-record packet. Framing owns packet boundaries; the codec cannot invent a discriminator to reject this ambiguity.

Combined native acceptance:13,126 checks/4,471 malformed rejections plus five isolated CRC/customization cases. Release EXE D97C177C70B1249F18EFD259089728CFF6EE309F8BF0814C59D1B3F5EF1FE92F builds and enters the stationary world. A capped opt-in receipt records body length, defender count and extended flag only, without IDs/names/payloads/endpoints. Natural corrected0xCC receipt and animation fidelity are still pending; idle survival alone does not prove the failing producer has been seen again.

Next: observe natural receipt and continued rendering, then normal disconnect/re-entry. No intentional combat, remote packet capture, injected packet or server mutation was used. The [bounded checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_TIMED_MODIFIERS_AND_COMBAT_ACTION.md) carries the resume point. Existing packet inventory rows and research metrics are unchanged.
