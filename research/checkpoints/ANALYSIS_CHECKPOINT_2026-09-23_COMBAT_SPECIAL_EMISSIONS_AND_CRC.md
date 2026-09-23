# Stardust combat special emissions and CRC equivalence (2026-09-23)

## Result

The static server-to-client combat-animation coverage audit now includes the principal special-command overrides and direct `CombatAction` emitters. Across the bounded, enumerated source paths, **297 distinct candidate animation strings are all present** among the installed `combat/combat_manager.iff`'s 406 keys. The other **109 keys are outside this inventory**, not proven unreachable. This remains a source-and-asset audit: no server, client, Lua registry, or live combat test was run.

The client and server CRC calculations are now **confirmed equivalent for the observed lowercase, separator-free ASCII animation keys** by code and table comparison. `Stardust.exe` calls `0x00AA4810` when loading manager `KEY ` strings; it constructs a `TemporaryCrcString`, which normalizes through `0x00AA2D10` and reaches `swg_filename_crc32_VA_00AA4790`. That routine starts at `0xFFFFFFFF`, updates `(crc << 8) ^ table[((crc >> 24) ^ byte) & 0xFF]`, and returns the complement. The server's `String::hashCode` in `utils/engine3/MMOEngine/src/system/lang/String.h` uses the same CRC update **without** that filename normalization. All **256 32-bit table entries** in the executable at `0x015EA5D0` equal the server source table. Every installed combat-manager key is lowercase and contains no path separators, so the normalization is inert for this corpus. Example results, computed from the verified table: `fire_single` = `0x7DF498A1`; `fire_3_single_light` = `0xDF8100CE`; `throw_grenade_near_proton` = `0xA8785862`; `force_healing_1` = `0x800505BF`; `dodge` = `0xE430FF04`. These are code-derived fixtures, not a live wire capture. Uppercase, separator-containing and non-ASCII names must not be assumed equivalent across the two implementations.

## Registration and emission evidence

`CommandConfigManager::runSlashCommandsFile` loads paths under `scripts/commands/`; `commands.lua` lists **804** `RunSlashCommandsFile` entries. All **233** command Lua files with literal nonempty animation strings from the previous block appear in that manifest. This supports configuration reachability, but a manifest entry alone does not prove that an individual command executes during gameplay. `CommandConfigManager2/3/4.cpp` factory registrations and `commands.lua` entries confirm the examined `FireHeavyWeaponCommand`, `ThrowGrenadeCommand`, `SaberSlash1/2Command`, `DrainForceCommand`, `TransferForceCommand`, `HealDamageCommand`, and `PetThrowCommand` paths are configured.

| Source path | Enumerated candidate names | Manager matches | Construction and caution |
|---|---:|---:|---|
| Heavy-weapon `getAnimation` override | 8 | 8 | Four `animationType` values from heavy-weapon Lua templates × light/medium; missing weapon template returns empty. |
| Grenade `getAnimation` override | 22 | 22 | Seven grenade-template `animationType` values × near/medium/far, plus empty-type `throw_grenade`; template definitions bound this type set. |
| Saber Slash 1/2 overrides | 3 | 3 | One-/two-handed/polearm knockdown names; invalid weapon returns empty. |
| Dot-pack direct `CombatAction` | 12 | 12 | Near/medium/far × poison/disease × area/non-area. |
| Heal-damage direct `CombatAction` | 3 | 3 | Near/medium/far healing-longrange. |
| Scout trap-template direct `CombatAction` | 11 | 11 | Literal `animation` values from eleven current-source trap-template Lua definitions; pet-trap path consumes the same template property. |
| Explicit C++ direct emissions | 7 | 7 | Includes `force_healing_1`, `dodge`, `force_intimidate_chain`, `tumble`, `tumble_facing`, and two reaction names. |

These sets overlap one another and the prior Lua/default-generator set. Their **union**, not the sum of row counts, is 297 distinct names, with no absent manager key. The earlier 237-name audit remains correct for its narrower scope. The 109 manager keys outside this combined set are not a defect finding. Some may be client-only, alternate versions, data-driven through sources not enumerated, or simply dormant; no usage conclusion is drawn.

The first cross-product of all `animationType` values with both overrides produced apparent missing names. That was a **false positive**: grenade-only types were combined with the heavy-weapon override and heavy-only types with grenades. The final audit scopes types to `/heavy/` and `/grenade/` template directories, respectively, matching the source's weapon-template type use. No missing candidate remains after that correction.

## Limits and follow-up

- The source inventory covers the identified `getAnimation` overrides and direct `CombatAction` constructors found in the current server tree. Dynamic external data, altered runtime scripts, and unobserved executable paths could produce other hashes.
- `commands.lua` reachability is static. A load or gameplay test should validate the Lua registry and the server's existing `CommandLuaTest::CheckAnimationCRCS` against the deployed TRE set.
- Representative selector fixtures (`DDSP`, `DEPS`, weighted `RAND`) should be built from original IFF bytes without changing them. Their recursive behavior is mapped, but exact output selection under all contexts has not been tested.
- The 109-key residual should be classified by naming family and source search, not labeled dead by subtraction.

Read-only audit scripts and decompiler output remain under ignored `local-private/session-2026-09-23-combat-selection/`. No executable, TRE, server code, commit, or build was changed.
