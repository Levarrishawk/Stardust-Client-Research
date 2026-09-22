# Analysis checkpoint: ASHT link and action policy

Date: 2026-09-12

## Scope

Static reconstruction of `AnimationStateLink`, state-link selection order, movement-aware actions, `useAddTrack`, and the replace/queue behavior passed from `StateHierarchyAnimationController` into `PhysicalAnimationTrack`. Evidence is from `C:\Stardust-DEV\Stardust.exe` with SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Correction to the prior working model

The earlier checkpoint described link applicability as virtual predicates and left “link predicate subclasses” as an expected family. RTTI and vtable enumeration do not support that model. This executable exposes abstract `AnimationStateLink` and one concrete `EditableAnimationStateLink`; no concrete predicate-subclass family appears in RTTI. The virtual calls used by the controller are link accessors (`hasTransitionAnimation` and `getTransitionLogicalAnimationName`), not predicate evaluation.

This is a material correction: transition selection is path matching in the owning `AnimationState`, followed by optional link-animation playback.

## Link object and serialization

MSVC RTTI resolves:

| Type | Vtable | Key routines |
|---|---:|---|
| `AnimationStateLink` | `0x015E6110` | destructor `0x00834D60`; abstract accessors |
| `EditableAnimationStateLink` | `0x015E60F8` | destructor `0x00834D90`; accessors `0x00834FC0..0x00835010` |

The concrete object is 16 bytes:

- `+0x04`: owning hierarchy pointer;
- `+0x08`: owned `AnimationStatePath` pointer;
- `+0x0C`: optional owned logical-animation-name object.

`0x00834FC0` returns the target path. `0x00834FD0` returns true only when the optional logical-animation name exists and is non-empty. `0x00835000` returns that name or the shared empty value.

The binary `LINK` form stores a signed-16 path-element count followed by that many state-name strings, then a one-byte “has transition animation” flag. Versions 0/1 (`0x00835340`) store a signed-16 legacy animation-name/index reference when the flag is set; version 2 (`0x008354E0`) stores the logical animation name directly as a bounded string. XML output uses `<statePath relative='false'>` and an optional transition-animation value. Relative state paths remain unsupported in this client generation.

## Link lookup and deterministic order

`0x00832C60` is the state-level link lookup used by `0x007CF760`. It iterates the state's link collection in stored order, obtains each link path through virtual slot `+0x04`, compares that path against the requested path/depth via `0x007CEE00`, and returns the **first** matching link. There is no runtime predicate chain in this lookup.

During a hierarchical path change, `0x007CF760` searches links while walking outward from the old branch and inward toward the new branch around their common prefix. For each selected link:

1. virtual slot `+0x08` tests whether the link carries a transition animation;
2. virtual slot `+0x0C` returns the logical animation name;
3. non-empty names are submitted to the semantic `action` track.

Traversal order is therefore behavioral: stored link order decides the match within a state, while old/new hierarchy traversal decides the sequence of selected links.

## First-link replacement and subsequent queuing

The boolean passed from `0x007CF760` to the common play bridge is true only while the number of already submitted link animations is zero. At `0x0083E9B0`, that boolean becomes the physical-track change mode:

- mode 1: clear pending changes and replace the current animation immediately;
- mode 0: if a current animation or queued work exists, append a 0x14-byte queued change record; if the track is empty, apply immediately.

Consequently the first qualifying transition-link animation replaces the current `action` track animation; later qualifying link animations are queued in traversal order. All link animations request the observed 0.15-second transition duration. Link playback does not use the `default` logical-animation fallback.

## Basic and movement-aware actions

RTTI shows exactly two concrete ASHT action implementations in this executable:

| Type | Vtable | Runtime role |
|---|---:|---|
| `EditableBasicAnimationAction` | `0x015E5760` | named action with logical animation and flags |
| `EditableMovementAnimationAction` | `0x015E56FC` | selects between two child actions by locomotion state |

`EditableBasicAnimationAction` contains its action name at `+0x08`, logical animation name at `+0x18`, and boolean fields at `+0x28/+0x29`. XML writer `0x007F7D60` proves `+0x29` is `useAddTrack`. Follow-up corpus and reference analysis closes `+0x28` as a dormant, binary-only legacy compatibility byte: XML omits it, no runtime path calls its getter, and all 16,617 ACTN records across configured archive copies store false. Its historical source name is absent, but its complete observable contract is known.

`0x007CFCE0` searches the current state and then ancestors for the first matching action. It obtains the selected logical-animation name and `useAddTrack`; `0x007CFAB0` routes false to semantic track `action` and true to `add`.

Every direct action submission uses physical change mode 1, so it replaces existing work on its selected semantic track rather than joining that track's queue. It requests the same 0.15-second transition duration and does not use the `default` fallback. Since `action` and `add` are separately mapped semantic tracks, replacement is per selected track; whether a particular installed controller template maps them to shared physical machinery remains template-dependent.

`EditableMovementAnimationAction` owns two child actions labeled by embedded/XML strings `noMovementAction` and `movementAction`. Selector `0x007F7150` reads the animation-environment vector `loco_vel` and chooses the movement child if **any** of its three float components compares nonzero; only an exact zero vector selects `noMovementAction`. It delegates name, logical-animation and flag access to the chosen child, so movement choice occurs at action resolution time.

## Sequence queries and cancellation

The hierarchy controller preserves action/add separation for request management:

- `0x007CFB10` queries the current sequence on the selected track and clears that track only if the caller's sequence matches;
- `0x007CFB60` compares a caller sequence against the latest issued sequence for the selected track;
- `0x007CFB90`-family wrappers query action-track active state, compare physical mapping, and fetch current skeletal-animation timing.

At the physical layer, `0x0083EC00` releases the current animation, clears queued changes, and resets track state. Sequence numbers come from a monotonically incremented counter and are returned before increment for each accepted replace or queued request.

## Replacement requirements

1. Preserve ordered state paths and ordered link lists.
2. Match the first applicable link by path; do not invent predicate subclasses.
3. Submit the first link animation as replace and later link animations as queued, all in hierarchy traversal order.
4. Preserve per-track action replacement and stable sequence handles.
5. Support `useAddTrack` independently of the action's logical animation name.
6. Resolve movement-aware actions from the exact-zero/nonzero `loco_vel` vector at request time.
7. Decode legacy link references and v2 direct logical names into one engine-neutral representation.

## Confidence

- **Confirmed by code:** RTTI/vtables, concrete link layout, path-first lookup, first stored match, v0/v1 versus v2 link payload distinction, first-link replace/subsequent queue behavior, 0.15-second transitions, direct-action replacement, `useAddTrack`, and exact `loco_vel` selection test.
- **Strongly inferred:** source-level names `hasTransitionAnimation` and `getTransitionLogicalAnimationName`; behavior is confirmed but original method names are not present.
- **Unknown:** the exact source enum names for physical change modes 0/1 and controller-template cases that might map semantic `action` and `add` tracks onto shared physical evaluators. The BasicAnimationAction `+0x28` byte is closed as a dormant binary compatibility field; its missing historical source label is not a behavioral unknown.

## Stable resume point

The ASHT link/action runtime is now closed enough for implementation. Next animation work should inventory installed ASHT/LATT assets and controller templates for semantic-track mappings, or move to the category-13 voice/audio reachability contradiction.
