# Analysis Checkpoint: Skill and Progression Manager

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope completed

This block connects the shared skill-definition assets, replicated creature/player progression state, `CuiSkillManager`, profession/skill selection, experience-monitor persistence and skill surrender. It deliberately separates authoritative server rules from client-side projection and validation.

## Definition assets

`skill_manager_install_VA_012AAC70` constructs the global definition manager and opens two original DataTable-IFF assets through the VFS:

| Asset | Effective overlay | Size | Shape |
|---|---|---:|---|
| `datatables/skill/skills.iff` | `stardust_3_01.tre`, priority 53 | 278,975 bytes | version 0001; 1,089 rows; 27 columns |
| `datatables/skill/xp_limits.iff` | `mtg_patch_013_configurable_02.tre`, priority 42 | 1,205 bytes | version 0001; 48 rows; `NAME`, `LIMIT` |

The skill columns are confirmed from the installed asset:

```text
NAME, PARENT, GRAPH_TYPE, GOD_ONLY, IS_TITLE, IS_PROFESSION, IS_HIDDEN,
MONEY_REQUIRED, POINTS_REQUIRED, SKILLS_REQUIRED_COUNT, SKILLS_REQUIRED,
PRECLUSION_SKILLS, XP_TYPE, XP_COST, XP_CAP, MISSIONS_REQUIRED,
APPRENTICESHIPS_REQUIRED, STATS_REQUIRED, SPECIES_REQUIRED,
JEDI_STATE_REQUIRED, SKILL_ABILITY, COMMANDS, SKILL_MODS,
SCHEMATICS_GRANTED, SCHEMATICS_REVOKED, SEARCHABLE, ENDER
```

`skill_definition_lookup_or_load_VA_012AAF20` lazily creates a `SkillObject` from the skill table, inserts it in the name map, and rejects/rolls back a failed load. `skill_xp_limits_initialize_VA_012AB360` builds the name-to-integer cap map from the second table. The current Stardust server independently loads the same two paths in `SkillManager.cpp`; its `Skill::parseDataTableRow` supplies the authoritative gameplay interpretation.

This is a material compatibility rule: a replacement must mount the same effective overlay order. A base-game `skills.iff` would not describe Stardust's 1,089-row progression graph.

## Live replicated state

The definition catalog is combined with two previously confirmed replication channels:

- `CREO:1` skill list: baseline `U32 count | U32 counter | ASCII16 skillName[count]`; add/remove deltas resolve each string into a client `SkillObject const*` and publish `CreatureObject::Messages::SkillsChanged`.
- `PLAY:8` experience map: ASCII16 XP type to U32 amount, with map delta commands; changes publish `PlayerObject::Messages::ExperienceChanged`.

`cui_skill_manager_install_VA_008AFD90` subscribes its callback object to `SkillsChanged`, `ExperienceChanged`, `PlayerSetup`, and delete-skill confirmation. The manager does not own progression truth. It recomputes client presentation from replicated state plus the shared asset catalog.

## UI-facing state and events

The manager retains:

- one validated selected skill name;
- one validated selected profession name (the candidate must exist and carry the profession flag);
- ten persisted experience-monitor skill slots named `expMonitorSkill_%d` under the `CuiSkillManager` settings namespace.

Changes publish three typed contracts recovered from RTTI and code:

| Event | Payload | Consumers/effect |
|---|---|---|
| `SelectedSkillChanged` | `const std::string &` | profession page changes selected skill/details |
| `SelectedProfessionChanged` | `const std::string &` | profession page switches graph/profession |
| `ExpMonitorSkillChanged` | pair of monitor index and skill name | experience monitor retargets one of ten slots |

Selection setters reject unknown skills and reject non-profession entries rather than manufacturing placeholders.

## Surrender flow

`cui_skill_request_surrender_VA_008B1270` resolves the requested skill definition, obtains the local creature, walks owned skills to find dependants that require it, and formats a dependent-skill warning when necessary. If surrender is permitted it opens `CuiDeleteSkillConfirmation`; the confirmed path sends the internal `surrenderSkill` command with the canonical skill name. The current server registers `SurrenderSkillCommand` and executes authoritative validation/removal in `SkillManager::surrenderSkill`.

The client dependency check is therefore advisory UX. The replacement must not treat it as security or authority; the server remains decisive.

## Replacement contract

- Load `skills.iff` and `xp_limits.iff` from the original ordered VFS before resolving replicated names.
- Keep wire strings and U32 XP amounts unchanged even in a native x64 object model.
- Represent definitions with stable IDs/handles, not pointers encoded in replicated state.
- Recompute profession graphs, eligibility displays, command/ability lists, modifiers and schematic presentation from the shared catalog.
- Keep client validation advisory and submit surrender through the established command protocol; surface server rejection.
- Persist monitor choices by skill name. The historical ten-slot UI limit may remain as a compatibility default but should not determine internal container width.

## Confidence

- Asset paths, overlay provenance, row/column contracts, replication inputs, subscriptions, selected-name validation, ten monitor slots and surrender command handoff: **Confirmed by code/assets/server source**.
- Exact historical names for every helper and every UI formatting branch: **Unknown but bounded**.
- Full semantic interpretation of all 27 columns is best sourced jointly from `SkillObject` and Stardust's `Skill::parseDataTableRow`; this block confirms the architecture and content boundary, not every leaf rule.

