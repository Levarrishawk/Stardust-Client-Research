# Experience Monitor Runtime

Date: 2026-09-22

## Result

`SwgCuiExpMonitor` is a read-only configurable projection of the existing
`PLAY:8` experience map and shared skill catalog. It introduces no packet and
performs no XP mutation.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00EA0410` | `swg_cui_exp_monitor_construct_VA_00EA0410` | binds skill selector, XP text and progress page; resolves complete/default colors | Confirmed |
| `0x00EA0920` | `swg_cui_exp_monitor_selection_changed_VA_00EA0920` | refreshes when the selected skill changes | Confirmed |
| `0x00EA0EA0` | `swg_cui_exp_monitor_refresh_VA_00EA0EA0` | resolves selected skill and XP type, reads current/cap values, updates bar/text and complete/default visual state | Confirmed |

RTTI confirms subscriptions to `CreatureObject::SkillsChanged`,
`PlayerObject::ExperienceChanged`, `CreatureObject::PlayerSetup` and
`CuiSkillManager::ExpMonitorSkillChanged`.

The refresh path resolves the selected skill through the shared skill manager,
obtains its XP definition/type, reads the current replicated value and catalog
cap, clamps/presents progress, and switches to a completion color when current
XP reaches the cap. Null or missing definitions are diagnosed and suppressed.

## Replacement-client requirement

Treat this as a view model over stable skill handles and replicated U32 XP
values. Use the effective Stardust `skills.iff`/`xp_limits.iff` overlay for XP
type and cap. Persist selected monitor slots by skill name through the existing
ten-slot `CuiSkillManager` compatibility model, while allowing a modern UI to
render any number of monitors internally.

## Residual uncertainty

Exact formatting punctuation and transition timing are visual fidelity only.
Inputs, cap computation, completion state and event lifecycle are closed.
