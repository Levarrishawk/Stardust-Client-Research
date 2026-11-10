# Original timed modifiers in the source client

October 14, 2026. This is a compatibility specification and implementation receipt, not a claim that the research repository contains the client implementation. NGE buff-builder remains separately deferred. Original timed buffs are required by the [conversion plan](NGE_PRECU_CONVERSION_PLAN.md).

## Evidence and confidence

The executable SHA-256 was reverified as E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VA-based probes. The [attribute-modifier investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_ATTRIBUTE_MODIFIER_MANAGER.md), [controller effects](CLIENT_CONTROLLER_EFFECTS.md) and [schemas](CORE_OBJECT_CONTROLLER_SCHEMAS.md) remain the starting authority.

Observation: creature dispatcher006392E0 forwards0x229/0x22A to manager add00943670/remove00943700. Clear009437D0 and typed add/remove/all events provide lifecycle notifications. Tick00943300 subtracts frame delta and removes entries at remaining duration <=0. It has neither the later NGE -1 infinite sentinel nor its five-second negative grace. **Confirmed original behavior.** Invalid/nonfinite frame-delta rejection is an explicit modern defensive policy.

Observation: loader00943370 uses Crc::calculate on the modifier name and stores display/description StringIds and icon style; dotted names also form a tree. Effective datatables/modifiers/modifiers.iff is16260 bytes, SHA-256 B1BB7F8A22861462E3D56041FBA37DF9A756BA74ABCF661E1D937F3181BF42BC, priority42 mtg_patch_013_configurable_02.tre. Four string columns are ModifierName, DisplayName, DisplayDescription and IconImageStyle. Native corpus validation finds148 rows, unique names/CRCs,148 populated icon paths and148 display IDs. This proves catalog structure, not successful rendering of every style. **Confirmed bytes and decoded corpus.**

Observation: original SwgCuiAttributeModifiers constructor00EFE910 binds VolumePage and sampleIcon, with add/remove/all subscriptions at00EFEADB..00EFEB99. Effective ui/ui_ground_hud.inc, priority53 stardust_3_01.tre, is64909 bytes, SHA-25611048CAC8FF60B3EE2E09B7309B1310FB21974C7F11C8953778C7D4B55A80999. Its embedded AttribMod CodeData maps VolumePage and volumestates to buffs and sampleIcon to sampleIcon. The timed template is24x24; sampleStateIcon is a different20x20 template. The authored buffs volume has21x21 cells and198x63 extent. Preserve that authored packing rather than imposing NGE buff preferences. **Confirmed effective asset.**

The similarly named standalone ui_ground_hud_attribmod.inc is23832 bytes, SHA-256378A901ADF00BA77B1BF4A022C11C5026F03B3572E61FF46E69836C3D766CDA1. Its geometry differs; it is not the instantiated embedded GroundHUD page. No original assets are promoted into Git.

## Wire and translation boundary

Local Core3 source correlation: Stardust-3/MMOCoreORB/src/server/zone/packets/object/Buffs.h, AddBuff and RemoveBuff; objects/creature/buffs/BuffImplementation.cpp::sendTo. These are server source observations, not proof of the deployed revision. Standard controller envelope carries flags0x1B, type, U64 controlled object and U32 scalar. Inner add0x229 is U32 CRC then F32 remaining seconds; remove0x22A is U32 CRC. All fixed scalars are little endian. No NGE buff-slot or stacking fields occur here.

NGE CM_modData553 and CM_cancelMod554 already have these numeric IDs. Preserve the enums. The source adapter validates exact tail consumption and finite duration before publishing a typed message. All finite signed durations remain wire values; zero and negative values expire on the next valid update. Unknown CRCs remain timed entries and can still be removed. Rejecting nonfinite values and extensions is a deliberately stricter policy, not recovered original malformed-input equivalence.

The conversion restores CuiModifierManager query access and revision notifications, preserves its typed events and definition tree, and clears scene state during GroundScene teardown with an installed-manager guard. Pre-CU HUD presentation uses the original local-player sampleIcon and volume, localized name/description/time-left tooltip, and once-per-second refresh between revisions. Duplicate icons are owned through existing Attach/RemoveChild/Detach conventions. Unknown definitions have a diagnostic tooltip; missing styles do not invent a replacement asset. Definition validation prepares the whole catalog before replacing prior output.

NGE removeBuff context commands are not emitted for this volume. Original cancellation request semantics remain unrecovered; this is an explicit unresolved function, not cancellation acceptance. Other state icons and NGE buff-builder negotiation are separate systems. The conversion uses the existing source HUD mediator; it does not establish the original factory owner or change the curated SwgCuiAttributeModifiers workflow row.

## Validation and remaining gates

Native production-code fixtures cover actual registered add/remove controller applications, every truncated prefix, extra tails, U32 CRC width, signed finite extremes, nonfinite read/write rejection, incorrect pack type, replacement/expiry, invalid elapsed time, malformed catalog preservation and the148-row effective corpus. The combined subsequent combat-action build passes13,126 checks/4,471 malformed rejections plus five isolated CRC/customization cases. False-profile NGE codec branches remain covered separately.

Release build D97C177C70B1249F18EFD259089728CFF6EE309F8BF0814C59D1B3F5EF1FE92F and matching symbols are deployed only to Experimental. Stationary login and world startup succeed with the corrected sampleIcon binding. No active timed buff has yet been observed; receipt, icon art, live tooltip/countdown, refresh/removal and scene cleanup with a nonempty map remain live gates. Empty-world startup cannot close them.

Next probe: observe a naturally received known0x229, inspect its original icon and timer, then receive removal/expiry and re-enter without stale state. Deliberate gameplay or server mutation requires its own test scope; no injected live buff or admin command was used here. See the [bounded checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_TIMED_MODIFIERS_AND_COMBAT_ACTION.md).

## October 16 source-client live extension

Under the user's subsequent bounded gameplay authorization, the source client
receives Center of Being add/remove events through the existing controller path:
CRC `5997AC66`, 30 seconds, followed by removal. The installed 148-row catalog has
no Center of Being definition. Local Core3
`objects/creature/commands/CenterOfBeingCommand.h` uses
`STRING_HASHCODE("centerofbeing")`; `BuffImplementation.cpp::sendTo` and
`packets/object/Buffs.h` provide the matching emission boundary. The live CRC also
matches native `Crc::normalizeAndCalculate("centerOfBeing")`. **Confirmed current
source/wire correlation**, with deployed server revision still unidentified.

Source-client build `4AC8E2BB` adds a deliberately narrow missing-definition
presentation bridge for that CRC only. Existing installed definitions win. The
bridge reuses the installed command icon and localized command name/description;
it does not add an original catalog row or establish that the original executable
used this bridge. Unknown other CRCs retain fallback behavior. This is a
**current Core3 presentation adaptation**, separate from the original manager
contract above, with no new executable-VA claim or semantic-registry change.

Two natural activations show add/remove at `929066.495/929096.526` and
`929139.223/929169.212`. Live acceptance covers the icon, localized tooltip,
one `Time Left: 8s` sample and removal. Sampled countdown decrement, active
replacement/reconnect, populated teardown and broader original definitions remain
open. The actual-library suite passes 15,457 checks / 6,608 malformed-input
rejections plus five isolated CRC/customization cases; these are fixture counts,
not feature coverage. No NGE buff-builder activation, injected buff or server
change was used.
