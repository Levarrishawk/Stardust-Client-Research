# Analysis Checkpoint: Attribute Modifier Manager

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block closes the client-side timed modifier/buff path from current Stardust controller messages through the runtime collection and the `SwgCuiAttributeModifiers` presentation boundary. It also recovers the original asset table on which the UI depends. No executable or service was run.

## Confirmed executable path

The creature-controller dispatcher `creature_controller_message_dispatch_VA_006392E0` handles two server-only inner controller types:

| Inner type | Payload consumed | Client path | Result |
|---|---|---|---|
| `0x229` | U32 modifier CRC at payload `+4`; F32 seconds at `+8` | `modifier_manager_add_or_refresh_VA_00943670` | insert/find CRC-keyed active entry, set remaining seconds, publish `CuiModifierManager::Messages::ModifierAdded` with the CRC |
| `0x22A` | U32 modifier CRC at payload `+4` | `modifier_manager_remove_VA_00943700` | find and erase the matching active entry, publish `ModifierRemoved` with the CRC |

The server definitions in `MMOCoreORB/src/server/zone/packets/object/Buffs.h` are byte-compatible: `AddBuffMessage` emits U32 CRC followed by F32 duration and `RemoveBuffMessage` emits U32 CRC. Both use controller flags `0x1B` and inner types `0x229/0x22A`.

`modifier_manager_tick_VA_00943300` subtracts frame elapsed time from every active entry and removes entries whose remaining time compares at or below the zero constant. `modifier_manager_clear_all_VA_009437D0` empties the collection and publishes `AllModifiersRemoved`. Thus the client collection is a timed UI projection; authoritative gameplay effects remain on the server.

## Original asset contract

Manager installation at `0x009430B0` loads `datatables/modifiers/modifiers.iff` through the common data-table service. The effective configured-overlay copy was extracted from `mtg_patch_013_configurable_02.tre` (search-tree priority 42), is 16,260 bytes, and declares DataTable-IFF version `0001`, four string columns and 148 rows:

1. `ModifierName`
2. `DisplayName`
3. `DisplayDescription`
4. `IconImageStyle`

Loader `modifier_definition_table_load_VA_00943370` computes the canonical SWG filename CRC of `ModifierName`, parses `DisplayName` and `DisplayDescription` into two `StringId` values, retains the icon style path, and also retains the original modifier name. Its 0x54-byte legacy definition record is therefore:

```text
+0x00 U32 modifierNameCrc
+0x04 StringId displayName          (legacy 0x1c bytes)
+0x20 StringId displayDescription   (legacy 0x1c bytes)
+0x3c narrow string iconImageStyle  (legacy 0x0c bytes)
+0x48 narrow string modifierName    (legacy 0x0c bytes)
```

Definitions are indexed by CRC and also inserted into a hierarchy derived from dot-separated modifier names. The active collection is a distinct CRC-to-remaining-seconds tree. This separation explains why network traffic carries only the CRC and duration: text, icon and grouping are resolved locally from original assets.

Examples in the installed table include `food.dessert_bofa_treat`, localized `@food_name:` / `@food_detail:` identifiers, and `/Styles.Icon.state.*` UI styles. Original modifier data and referenced STF/UI-style content are therefore mandatory compatibility assets.

## Replacement contract

- Preserve `0x229 = {U32 CRC,F32 seconds}` and `0x22A = {U32 CRC}` exactly on the wire.
- Resolve CRCs against the effective original `modifiers.iff`; retain unknown CRCs as bounded diagnostic entries rather than crashing.
- Keep authoritative buff mechanics server-side. The client timer is presentation state and may be corrected/refreshed by later packets.
- Publish typed add/remove/clear events to UI consumers. Do not reproduce the legacy red-black-tree nodes, 0x54-byte in-memory record, raw pointer ownership, or MSVC transceiver implementation.
- Preserve dot hierarchy, localized name/description and icon-style identity at the compatibility boundary; a modern UI may render them differently.

## Confidence

- Controller IDs, byte layouts, dispatcher calls, active timer behavior, event identities and four table columns: **Confirmed by code/assets**.
- Dot-separated names forming the definition hierarchy: **Strongly inferred** from recursive delimiter parsing and deployed names.
- Exact original member names and whether zero/negative durations have additional design semantics: **Unknown, non-blocking**.

