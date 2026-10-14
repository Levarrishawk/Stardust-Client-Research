# Original character-sheet source adapter

Date: October14,2026. Bounded read-only local-player presentation in SD-Src-Client, using the installed original UI and existing response research.

## Evidence and confidence

Original [sheet runtime](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CHARACTER_SHEET_RUNTIME.md) establishes constructor00F9F290, activation00F9FBC0, deactivation00FA0000 and response listener00FA0100. The [response contracts](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_CHARACTER_SHEET_RESPONSES.md) establish the five message layouts and identity guards. These findings use the hash-verified original executable; this continuation does not execute it or add provisional names.

The installed effective ui/ui_pda_char_sheet.inc comes from stardust_3_01.tre priority53,77318 bytes, SHA2563444F7A245E27B6BA8F7A38D40068F1B8EA54AA959F594816B5B396C53652D45. Its CodeData binds pageAttributes, pageExperience, original consumption/force widgets, identity/location/faction/badge fields and stat migration. The installed attribute table has six rows: health, strength, constitution, action, quickness, stamina. Mind/focus/willpower are absent from this Stardust page, consistent with the user's display requirement. Extracted content remains private.

Computer-use opening Character in source build3237A2E4D1E27E3B5A8D8BD91D80E14B343426DF12B7A654420278DA3934818E causes an access violation in SwgCuiCharacterSheetNamespace::loadSkillModListingDatatable. Source dereferences the absent NGE datatables/expertise/skill_mod_listing.iff before binding a different control set. **Confirmed source/runtime blocker.**

## Response adapter

Engine-independent PrecuCharacterSheetProfile.h owns decoded values and consumes complete application messages through bounded Archive reads. It does not alter the NGE classes or transport. The explicit original-layout UI branch selects it.

| Message | Preserved original distinction |
|---|---|
| CharacterSheetResponseMessage | Two U32 reserved values, three Vector3/ASCII16 location pairs, UTF16 spouse with U32 code-unit count, U32 lots remaining, U32 faction and U32 status. No NGE citizenship string. |
| FactionResponseMessage | Leading ASCII16 rank, three S32 standings, independently counted ASCII16 names/F32 values. |
| GuildResponseMessage | U64 recipient, ASCII16 guild name, retained U16 reserved value; no NGE member-title string. |
| BadgesResponseMessage | U64 recipient, U32 terminal mask index, index+1 U32 masks. Widen before adding one and bound against remaining bytes before allocation. |
| PlayerMoneyResponse | Existing U32 bank/cash decoder, parsed and discarded; replicated balances remain authoritative. |

The adapter requires the pinned Core3 application metadata13/7/4/3, expected opcodes and complete consumption. This strict malformed-input policy is a **modern compatibility choice**, not a new claim about the original decoder's tolerance. Strings retain existing Archive bounds. Structurally valid unequal faction arrays are decoded; the UI retains its prior detailed map while refreshing headline standings. Each DTO is fully decoded before presentation changes.

Server correlation is Stardust-3/MMOCoreORB/src/server/zone/packets/player/CharacterSheetResponseMessage.h, FactionResponseMessage.h, GuildResponseMessage.h and BadgesResponseMessage.h constructors. RequestCharacterSheetInfoCommand.h and RequestBadgesCommand.h under objects/creature/commands return read-only details. They corroborate the existing original contract; the running Debian binary's source identity is not newly established.

## Presentation and ownership

SwgCuiPrecuCharacterSheet is a separate workspace mediator selected by ClientGame.usePrecuHud. It subscribes on activation, releases subscriptions/pointer demand on deactivation, and refreshes from the current local creature/player rather than retaining unsafe raw owner pointers. Remote-player examine remains unsupported by this initial adapter. The NGE route is preserved.

The raw-nine CreatureObject accessor reads current/max/base/wounds at the same original index and refuses unavailable values. Encumbrance availability is independent: nine values retain raw indices; Core3's three-pool form projects each pool onto its two affected secondary rows, with primary-row zero. Unsupported shapes stay unavailable. This modern projection is correlated with CreatureObjectImplementation.cpp's three-element initialization and PlayerManagerImplementation.cpp::applyEncumbrancies, not newly proved original UI arithmetic. It does not remap the six source semantic enum positions. Existing authored table rows receive their raw values; bars use bounded current/maximum and (maximum-wounds)/maximum fractions. The displayed buff column is the signed maximum-minus-base difference, a documented modern projection pending complete original arithmetic fidelity. Food/drink use actual bounded replicated ratios. Identity/title, species, dates/time, shock wounds and detail/faction responses come from their respective replicated or decoded sources. Guild/badge recipient IDs must match the current local creature.

The initial badge display reports earned-mask bit count; localized badge names/unearned visibility are still required. Biography, guild abbreviation, original XP presentation, full force visibility, stat migration and other-player examine remain open. Stat migration is hidden until its independent workflow is restored; it is not executed during this bounded test. This increment does not enable collections or the deferred buff builder.

## Validation and next gate

Synthetic production decoder fixtures pass9074 checks/2350 malformed-input rejections plus five isolated CRC/ACST cases. They cover nonempty locations/spouse/standings/guild/masks, reserved-value retention, every truncated prefix of the four messages, wrong opcode/metadata, extra tails, inclusive-count overflow, unequal faction arrays and unchanged NGE guild/faction/character-sheet decoding. The installed UI and real model/lifecycle are not instantiated by those fixtures.

Next: deploy matching EXE/symbols, use authorized computer-use login, open the original Character window, inspect its tabs and normal closure while stationary. A successful window does not establish all named remaining presentation/workflow boundaries. [Daytime receipt](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md).


## Live sheet receipt

Build3D472FD3606C3FDBF63AAEF8E5B91FD54EF77C47BEF2562D0D0F4B077DBED992 opens the original Status tab with all six authored numeric rows and no Mind/force display. Earlier blank rows are corrected by separating encumbrance availability; bar widgets bind directly to the authored page rather than an unresolved relative data-cell path. Count-only receipts show faction names/values0/0, detail applied1 and16 badge masks/3 set bits. The earlier Personal tab shows decoded lots/location defaults and badge count. This validates bounded local presentation and read-only query/response routing, not nonzero wounds/armor/force or full badge/XP/biography/examine fidelity.

Force visibility is also supported by a fresh original-code probe: update00FA1090 calls00FA1140;00FA116C..1177 checks the player's maximum field,00FA1210..121A shows the force widget, and00FA1258..1264 hides it when nonpositive. The adapter sizes its actual image from current/max and hides its containing page when max is zero.
