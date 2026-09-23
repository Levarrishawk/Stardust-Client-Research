# Combat selector predicates and server animation coverage (2026-09-23)

## Outcome

The installed `combat/combat_manager.iff` is a recursive, asset-driven decision tree. `Stardust.exe` supports seven selector node kinds; the effective asset uses five. A static comparison of the current Stardust server's Lua combat-command animations and default generator against its 406 keys found **no missing key** in the enumerated set. This is source/asset coverage, not a live-server test.

## Selector-node runtime map

The factory at `0x00466D10`, constructor-assigned vtables and their virtual `+8` methods give the following direct map. Provisional names reflect observed data and behavior; the decompiler is not original source.

| IFF node | Installed count | Parser | Runtime virtual `+8` | Observed choice rule | Confidence |
|---|---:|---|---|---|---|
| `SNGL` | 2,641 | `0x00466F40` | `0x00467370` | Fetch one named `.pst`, create a playback instance, apply bindings. | Confirmed |
| `RAND` | 40 | `0x00467710` | `0x00467C00` | Read per-entry `PROB` bytes, normalize cumulative weights, sample RNG, delegate to one child. | Confirmed broad policy; exact RNG scaling not independently tested |
| `ATYP` | 0 | `0x004680C0` | `0x00468220` | Classify attacker appearance through `0x00469DF0` (`0` skeletal, `-1` other); select indexed child, then configured fallback. | Confirmed |
| `DTYP` | 0 | `0x00468360` | `0x004684C0` | Same appearance-type selection for defender; fallback when defender absent. | Confirmed |
| `AEPS` | 47 | `0x00468960` | `0x00468AC0` | Select by attacker-side action posture field `+0x08`, or configured fallback when no attacker; indexed-child miss falls back. | Confirmed field/branch; posture label strongly inferred from `POST` asset tag and surrounding code |
| `DEPS` | 1,221 | `0x00468BF0` | `0x00468D50` | Select by defender-side action posture field `+0x1C`, or configured fallback when no defender; indexed-child miss falls back. | Confirmed field/branch; posture label strongly inferred |
| `DDSP` | 331 | `0x00468E80` | `0x00469200` | Select by action field `+0x20` using `DISP` slots; if absent, use configured fallback; if both absent, warn and return null. | Confirmed index/fallback; exact disposition enum meanings not fully decoded |

Parser details: `ATYP`/`DTYP` children are keyed by 16-bit `TYPE`; `AEPS`/`DEPS` by 16-bit `POST`; `DDSP` by 16-bit `DISP`. The latter two parsers convert a negative index to the first empty slot (`0x0046A0F0`). The posture array size is 15 (`0x0069E990`). `RAND` reads byte `PROB` weights; `SNGL` reads `NAME` plus optional `VARS`. `0x00467F80` supplies type-node fallback, `0x00468810` posture-node fallback (including a special posture-14 to slot-13 attempt), and `0x00469200` disposition-node fallback. These details matter for original-asset consumption; they are not network fields.

The count is from recursive traversal of the effective 474,358-byte `CBTM/0002` file in `mtg_patch_011_files_01.tre` (priority 40): 406 top-level keys, 2,641 `SNGL`, 1,221 `DEPS`, 331 `DDSP`, 47 `AEPS`, 40 `RAND`, and no `ATYP`/`DTYP` nodes. Counts include nested nodes across every key, not distinct scripts. The manager references 24 distinct `.pst` paths.

## Stardust server cross-check

`CombatManager::broadcastCombatAction` hashes `combatCommand->getAnimation(...)` into `CombatAction` (`0xCC`). `CombatQueueCommand::getAnimation` either uses a configured animation string (`GENERATE_NONE`, `GENERATE_RANGED`, `GENERATE_INTENSITY`) or generates a default name when the string is empty. `CommandConfigManager` loads `animation` and `animType` from Lua. Existing `src/tests/CommandLuaTest.cpp::CheckAnimationCRCS` explicitly compares those names, including generated suffixes, against `combat/combat_manager.iff`; this test was **inspected, not run**.

A read-only static inventory of `MMOCoreORB/bin/scripts/commands/**/*.lua` found 233 files with a literal nonempty `animation` assignment: 62 no-suffix/default-mode files, 47 ranged-suffix files, and 124 intensity-suffix files. Expanding their possible suffixes produces **165 unique names, all 165 present** among the asset keys. Four additional files assign an empty animation. Exhaustively enumerating the default generator's naming grammar (`droid_attack`, `creature_attack`, `creature_attack_ranged`, three ranged fire prefixes, nine hit-location prefixes, two intensities and four sequence digits as applicable) produces **90 theoretical names, all 90 present**. Union of these sets is **237 keys**; **169 of 406** are outside this particular inventory. They must **not** be called dead: special command overrides and other producers were not enumerated, and not every Lua file is necessarily registered or reachable.

The server's `String::hashCode` implementation is the Engine3 CRC-table routine (`utils/engine3/MMOEngine/src/system/lang/String.h`). The executable's `0x00AA4810` obtains a CRC via `TemporaryCrcString`. Existing server `CommandLuaTest` is intended to check hash agreement against the asset. At the time of this checkpoint, no compiled test or specific numeric client/server fixture had been run. A subsequent block compared all 256 executable/server table entries and produced numeric fixtures, **confirming equality for this asset's lowercase, separator-free ASCII keys**; see `ANALYSIS_CHECKPOINT_2026-09-23_COMBAT_SPECIAL_EMISSIONS_AND_CRC.md`. Client filename normalization means arbitrary input strings need a separate check.

## Secondary caller

The other direct caller of playback action `0x00469550` is `0x008C7FC0`, which constructs a local combat presentation action through `0x0069E9A0` and hashes a configured string before applying it. Thus not every playback launch requires an inbound `0xCC` packet. The precise user-event/UI semantics of that path remain open.

## Replacement-client implications and next questions

- Load `CBTM/0002` into typed recursive selectors. Preserve tag IDs, slot/default rules and original `.pst` references; use explicit 32-bit CRC keys and fixed-width wire fields.
- Keep the network codec separate from selector evaluation. A server action hash chooses local presentation data; it is not an instruction to execute arbitrary server-supplied script paths.
- The headless first networking proof of concept still needs no playback implementation.
- Next: enumerate special-command animation overrides and registration reachability, then create numeric CRC fixtures and selector decision fixtures for representative `RAND`/`DDSP`/`DEPS` trees. Do not claim all 406 keys are server-emitted until those paths are checked.

Scratch scripts and decompilation for this block remain under ignored `local-private/session-2026-09-23-combat-selection/`. Neither original executable nor TRE content was changed.
