# Skills and Profession UI Family

Date: 2026-09-22

## Result

This block closes the concrete UI family layered over the previously recovered
`CuiSkillManager`, `skills.iff`, `xp_limits.iff`, `CREO:1` skill set and
`PLAY:8` experience map. The family adds no independent wire protocol. It is a
set of projections, selectors and one advisory surrender action.

## Mediator composition

`SwgCuiSkills2` at `0x00D831A0` is a shell that constructs three child
mediators from the original declarative UI page:

- `SwgCuiSkills2ProfessionList` at `0x00EC5E70`;
- `SwgCuiSkills2MyStats` at `0x00EC40A0`;
- `SwgCuiSkills2Profession` at `0x00EC0660`.

The profession mediator in turn constructs five
`SwgCuiSkills2ProfessionGraph` instances (`0x00FF2B60`) corresponding to the
five graph pages embedded in the shipped UI. The graph binds novice/master,
discipline, next/previous and acquired/unavailable visual templates.

## Inputs and presentation

| Mediator | Authoritative/projected inputs | Principal output |
|---|---|---|
| profession list | shared skill catalog; local owned skills; Jedi state; selected profession | filtered profession selection with current-profession icon |
| profession detail | selected skill/profession; owned skills; XP map; Jedi state | profession/skill name, modifiers, commands, schematic grant/revoke icons, costs, recovery/current state, points and XP progress |
| profession graph | skill dependency graph and experience changes | five-page novice/discipline/master graph with acquired and unavailable styles |
| my stats | creature skill modifiers; player XP; commands/certifications; ten-slot monitor manager | tables for modifiers, experience and certifications plus XP-monitor selection |

RTTI confirms subscriptions to `SkillsChanged`, `SkillModsChanged`,
`ExperienceChanged`, `CommandsChanged`, `JediStateChanged`,
`SelectedSkillChanged` and `SelectedProfessionChanged`. All are typed domain
events produced by replication or `CuiSkillManager`; none are packet decoders
inside these mediators.

## Mutation boundary

The only material mutation exposed by this family is skill surrender. The
detail page invokes the already-mapped `CuiSkillManager` dependency check and
confirmation flow, which queues `surrenderSkill <canonical skill name>`.
Current Stardust's `SurrenderSkillCommand` delegates authoritative validation
and removal to `SkillManager::surrenderSkill`. Skill acquisition remains an
authoritative trainer/conversation/server workflow; the apparent acquire/cost
labels in the profession page are explanatory presentation, not a direct
client-side grant operation.

## Original-asset dependency

This family depends materially on:

- the effective Stardust `datatables/skill/skills.iff` and
  `datatables/skill/xp_limits.iff` overlays;
- the declarative `SwgCuiSkills2` page hierarchy and icon/style paths;
- localized skill, command, modifier and schematic labels.

A replacement may redesign layout and navigation, but it must preserve the
same catalog semantics, dependency graph, hidden/Jedi filters and localization
resolution. The server remains authoritative for ownership and XP.

## Replacement architecture

Implement a read-only progression view model fed by typed skill/XP/modifier/
command events. Keep graph layout and widgets outside the network/object model.
Resolve replicated skill-name strings to stable catalog handles, never native
pointers. Preserve `surrenderSkill` as a command transaction and surface the
subsequent replicated state as the final result.

## Confidence and residuals

Mediator composition, controls, graph count, event subscriptions and authority
boundary are **Confirmed**. Exact pixel layout, graph-edge routing and every
localized explanatory branch are fidelity work, not an unidentified gameplay
or network subsystem.
