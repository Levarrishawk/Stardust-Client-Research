# Analysis checkpoint: state-hierarchy animation runtime

Date: 2026-09-12

## Scope

This block connects the previously recovered `ASHT` hierarchy and `LATT` logical-animation assets to `TrackAnimationController` change requests. It focuses on the concrete `StateHierarchyAnimationController` rather than attempting to label every editor-side ASHT field.

## Confirmed architecture

MSVC RTTI resolves `StateHierarchyAnimationController` to vtable `0x015E5354`. Its constructor is `0x007CF440`. The object owns or retains:

- an animation-state-hierarchy template at `+0x14`;
- a logical-animation table template at `+0x18`;
- the current state path at `+0x1C`;
- a `TrackAnimationController` at `+0x20`;
- a pending animation identifier and remaining timer at `+0x24/+0x28`.

The current state path is an ordered vector of state-name identifiers. Path helpers can compare complete paths, find their common prefix, append/remove states, and render dotted names for diagnostics. The runtime therefore preserves hierarchical state identity; it does not flatten ASHT into a single enum.

### Installed tracks and fallback

Static installation at `0x007CF390` resolves and validates three track names against the TrackAnimationController registry:

| Embedded name | Track-id global | Observed use |
|---|---:|---|
| `loop` | `0x01923E0C` | persistent state-loop animation |
| `action` | `0x01923E08` | one-shot action/link animation |
| `add` | `0x01923E04` | additive/alternate action path |

The embedded logical-animation name `default` is retained at `0x01923E48` and is used as a fallback when a requested logical animation cannot be resolved and the caller permits fallback.

These state-controller track names coexist with the lower TrackAnimationController vocabulary (`logical_root`, `logical_primary`, `add_combiner`, `action_combiner`). The former are state-layer semantic tracks; the latter are controller/template mapping and combiner roles. They should not be collapsed into one anonymous set.

## State-path transition behavior

`0x007CF760` is the central hierarchical state-path change routine. It requires both ASHT and LATT inputs, ignores a path identical to the current path, resolves the requested state in the hierarchy, and then compares old and new paths around their common prefix.

The routine searches link records while walking out of old states and into new states. Subsequent RTTI/vtable analysis corrected the original “virtual predicate” hypothesis: the owning state selects the first stored link whose state path matches, and the link virtuals only expose whether it has a transition animation and that logical name. The first selected transition animation replaces the `action` track; later selected link animations are queued in traversal order. After link processing, the stored current path is replaced and the destination state's loop animation is resolved and started on the `loop` track. See `ANALYSIS_CHECKPOINT_2026-09-12_ASHT_LINK_ACTION_POLICY.md`.

Two caller flags select immediate versus ordinary transition behavior. One route obtains a controller-wide transition duration before starting the destination loop; another uses explicit play flags. Their broad effects are visible, but their source enum names remain unknown.

### Logical animation to physical-track bridge

`0x007CFD70` is the concrete LATT resolution/instantiation helper. It asks the retained logical-animation table for an entry by logical name and instantiates the typed `SkeletalAnimation` with the controller's environment/transform-name inputs. The template entry is released after construction.

`0x007CFC60` is the common state-controller play bridge:

1. instantiate the requested logical animation through `0x007CFD70`;
2. optionally retry with logical animation `default`;
3. choose zero or `0.15f` as the observed transition duration from a caller flag;
4. call `TrackAnimationController` change operation `0x0082F7B0` with the selected track id, animation and play/queue parameters;
5. return the issued animation sequence handle to the caller and release the temporary animation reference.

This provides the missing evidence chain:

```text
ASHT state path / state link / action
    -> logical animation name
    -> LATT lookup and typed SkeletalAnimation construction (0x007CFD70)
    -> semantic track selection: loop, action, or add
    -> StateHierarchy play bridge (0x007CFC60)
    -> TrackAnimationController::changeAnimation (0x0082F7B0)
    -> PhysicalAnimationTrack queue/change path (0x0083E9B0)
    -> pose, root motion, messages
```

### Actions

`0x007CFCE0` searches the resolved current state and its ancestors for an action definition. An action supplies a logical animation identifier plus an action-mode byte. Public action entry at `0x007CFAB0` uses that byte to select `action` versus `add`, then submits the animation through the same common play bridge. This confirms ancestor fallback for action lookup and a distinct additive action route.

Sequence-query/cancel methods at `0x007CFB10` onward forward to the underlying TrackAnimationController with the same semantic track ids. The hierarchy layer therefore retains the lower controller's stable animation-request handles.

## Replacement-client implications

The compatibility model needs four explicit layers:

1. hierarchical state path and link traversal;
2. logical animation/action selection with ancestor and `default` fallback;
3. semantic track routing (`loop`, `action`, `add`);
4. physical track scheduling, blending, pose/root-motion evaluation and messages.

A host engine state machine may implement the final graph, but flattening these layers would lose original-asset behavior. ASHT and LATT should first be decoded into an engine-neutral runtime model; engine animation nodes should be an adapter/output.

## Confidence

- **Confirmed by code:** RTTI/vtable, retained ASHT/LATT/current-path/controller members, track strings and ids, common-prefix path traversal, link/action logical-animation lookup, ancestor action search, `default` fallback, LATT instantiation, `0.15f` transition selection, and call into `0x0082F7B0`.
- **Strongly inferred:** semantic labels “persistent loop,” “one-shot action,” and “additive action” from installed names and call patterns.
- **Unknown:** the BasicAnimationAction `+0x28` flag, source-level names of physical change modes, and controller-template cases where semantic tracks share physical evaluators. Link priority and action interruption behavior are closed in `ANALYSIS_CHECKPOINT_2026-09-12_ASHT_LINK_ACTION_POLICY.md`.

## Stable resume point

The ASHT-to-physical-track chain and link/action policy are now connected. Resume with installed ASHT/LATT/controller-template mapping or specialized action consumers; do not repeat the disproven link-predicate-subclass search.
