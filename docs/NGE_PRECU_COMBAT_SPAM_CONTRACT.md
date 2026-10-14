# Source-client Pre-CU combat-spam adapter

Date: October14,2026. A confirmed late world-session codec mismatch, separate from containment admission and flytext.

## Evidence and confidence

Source build64EA4A066857D40C59ABA639B2509863C73F361557808012D9D1690F5BE2C674 enters the textured world and opens the original Character sheet. Later, a first-throw code stack records Archive::get → MessageQueueCombatSpamArchive::get → ControllerMessageFactory::unpack → ObjControllerMessage → GameNetwork::Listener. The bounded failure receipt records outer opcode0x80CE5E46 and80 application bytes. No IDs, payload bytes or text are logged. **Confirmed current decoder identity.** Attribution of the older generic Unicode-rethrow fault to this same codec is **Strongly inferred**, not independently recovered from that older dump.

The original executable's SHA256E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is reverified before the static probes. Registered0x134 reader wrapper00AEED70 calls00B1B720. Calls at00B1B776/780/78A read the three NetworkIds;00B1B79D..7A9 consumes four damage bytes;00B1B7B1 reads StringId;00B1B7C4..7D6 consumes one color byte. Its StringId-invalid branch reads Unicode at00B1B81F and constructs the literal message through00AEEF00. The valid StringId branch constructs through00AEED90. No NGE discriminator, coordinates, armor or damage-detail block precedes those fields. **Confirmed original structure.**

Current Core3 correlation: Stardust-3/MMOCoreORB/src/server/zone/packets/object/CombatSpam.h, both constructors, outer flags0x1B/type0x134. The producer always includes a U32 UTF16-unit count and literal tail, including zero for localized messages. The original valid-StringId reader leaves that optional producer tail unread; this adapter consumes the complete pinned producer shape. Strict complete consumption is an explicit modern policy, not original malformed-input equivalence.

## Supported wire and owned model

The inner body is U64 attacker, U64 defender, U64 item, four damage bytes, ASCII16 table, U32 StringId index, ASCII16 name, U8 color, then U32 UTF16-unit count and those units. Damage is retained as signed32 bits, matching the original local integer substitution; the server's unsigned producer preserves identical bits. StringId index is retained separately rather than lost through the NGE helper's index reset.

PrecuCombatSpamProfile is engine-independent. It owns IDs/strings/literal values, bounds all reads and rejects extra body bytes. MessageQueueCombatSpam owns that DTO with an explicit profile flag; its existing NGE constructors/copy/decoder remain available. Cross-profile packing rejects before writing, rather than inventing NGE-only data or silently converting an incompatible message.

The source UI formats the localized StringId, or the literal when no table/name exists, through the existing grammar substitutions for attacker/defender/item and damage. It publishes the existing CombatSpamReceived event. No coordinates or attack-detail outcomes are invented.

## Color classification

Original formatter008C94D0 resolves the three objects at008C94F8/9504/9510. Its color jump table008C97D0 contains0,1,4,4,4,4,4,4,4,4,2,3; targets are008C9709,008C96E5,008C972D,008C9734 and008C9739. Colors0/1 select miss/hit categories according to attacker-local, then defender-local, then other/other. Colors10/11 select old text categories1/2; other bytes select generic0. **Confirmed branch/classification structure.** The adapter maps those semantic categories to the source's NGE text enum symbols, whose numeric values differ. It does not reinterpret a legacy color as a newer CombatResult.

## Verification and remaining gate

Native suite10841 checks/2997 malformed-input rejections plus five isolated boundary cases passes. New synthetic fixtures cover localized/literal messages, nonzero reserved StringId index, high ID/damage bits, owned copy, every prefix, extra tails, cross-profile rejection, all256 color bytes/four local-role combinations, the actual registered controller route and all five NGE discriminator/success/failure paths. An independently built80-byte application rejects under the old NGE interpretation and passes/round-trips under the Pre-CU route; it matches the live message length, not captured live content.

The independent character-sheet refinements use an explicit Core3-correlated modern three-pool encumbrance projection: primary rows zero, each pair of secondary rows receives its corresponding pool encumbrance. Nine-value vectors retain raw indexing; unsupported shapes remain unavailable. This is not newly proved original UI arithmetic. Its numeric fixtures are included in the stated suite.

Next: verify naturally received0x134 text and continued stationary rendering, then normal disconnect/re-entry. Full combat semantics, localized fallback/color visual fidelity, endurance and every other controller family remain separate gates. [Daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).
