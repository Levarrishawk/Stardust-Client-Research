# Analysis checkpoint: animation state-hierarchy asset model

Date: 2026-09-12

## Scope

Binary `ASHT` loading, version compatibility, action-group ownership and root-state construction in `Stardust.exe`. This checkpoint deliberately does not claim the TrackAnimationController execution loop is complete.

## Confirmed findings

### The binary ASH format is `ASHT`, not an inferred extension tag

The executable directly enters `ASHT` and dispatches versions `0000`, `0001` and `0002` in `animation_state_hierarchy_ctor_load_VA_007EDFA0`. Version-specific readers are:

- `0000` -> `0x007EE390`;
- `0001` -> `0x007EE7C0`;
- `0002` -> `0x007EEBF0`.

The same subsystem has optional XML import/export support, gated by `disableAshXmlSupport`; binary IFF remains the deployed runtime path.

### Version 2 owns action groups plus one root state tree

`ASHT/0002` optionally enters `AGPS`. Its `INFO` chunk contains a signed 16-bit action-group count. The loader allocates that many action-group objects and constructs each with the enclosing hierarchy and format version. It then constructs one 0x50-byte root `AnimationState` object recursively from the remaining version payload.

The hierarchy object stores:

- root state pointer at `+0x18`;
- owned action-group vector at `+0x1C`;
- reference/name state inherited from the base animation-state-hierarchy template.

If no root state was loaded, the constructor creates a default root state instead of leaving the hierarchy unusable.

### Action/state objects are explicit, version-aware graph nodes

RTTI and constructor calls establish separate runtime types for:

- AnimationState and editable state;
- AnimationAction and editable action;
- AnimationStateLink and editable link;
- AnimationActionGroup;
- basic and movement animation actions;
- animation-message action templates.

The state loader receives the parent hierarchy and serialization version, so nested paths, links and actions are resolved with hierarchy context rather than being free-standing clip references. Diagnostics explicitly reject unsupported versions and note that relative animation-state paths were not implemented in this client generation.

### Save path confirms canonical v2 layout

The binary writer emits `ASHT/0002`, serializes optional `AGPS/INFO` with the action-group count and each group, then delegates the root state tree to its own writer. This independently corroborates the v2 reader ordering.

## Replacement requirements

1. Decode `ASHT/0000` through `0002`; use v2 as the canonical internal form.
2. Preserve action groups, nested states, state links and action/message payloads as a graph, not a flattened clip list.
3. Resolve state paths deterministically and reject unsupported relative paths with actionable diagnostics.
4. Supply an explicit default root state when the source omits one, matching client behavior.
5. Keep the parsed graph engine-neutral so a Godot AnimationTree adapter or custom runtime can consume the same model.

## Confidence and remaining unknowns

| Claim | Confidence |
|---|---|
| ASHT versions 0000..0002 | Confirmed |
| v2 AGPS signed-16 count and owned action groups | Confirmed |
| one recursively loaded root state | Confirmed |
| default root synthesis | Confirmed |
| detailed per-version state/link/action disk fields | Partial |
| TrackAnimationController transition evaluation and blend scheduling | Unknown; next animation runtime block |
| precise mapping from replicated posture/mood to state paths | Partial from earlier handler work |

