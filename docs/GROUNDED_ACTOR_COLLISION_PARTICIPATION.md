# Grounded actor collision participation

## Evidence boundary

Baseline SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are baseline virtual addresses. This is static analysis; the original executable was not run. See the [bounded checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_ACTOR_EXTENT_PARTICIPATION.md).

| Observation | Interpretation and confidence | Implementation consequence |
|---|---|---|
| `012A09C0` gets object `+8C`, refreshes it through `01260E40` when byte `+AC` is set, then returns member `+B4`. | Confirmed transformed collision extent accessor. | Distinguish local owned extent and transformed query extent. |
| `012A0AB0` checks both collision properties' byte `+7E`, virtual `+14` candidate eligibility and an additional filter; it passes both `012A09C0` results plus displacement to `012A12D0`. | Confirmed object-pair extent query; full filter meanings remain Unknown. | Do not substitute shader eligibility for actor candidate eligibility. |
| `012A12D0` dispatches on each extent's integer `+4`: paired type9 uses `012A1870`; source types4/3 and destination types4/3 use recursive branches. Unsupported/null pairs return a cleared result with no retained extent pointers. | Confirmed dispatch, not an exhaustive supported-type taxonomy. | Preserve explicit unsupported outcomes and composite traversal. |
| `012A1870` delegates type9 value intersection to `0126F880`, clips entry/exit against global bounds and retains both extent pointers on a hit. `0126F880` dispatches further on value member `+4`. | Confirmed nested shape dispatch. A type9 extent is not itself a single sphere type. | Shape labels require value-level evidence. |
| Constructor `01293CB0` initializes outer type9, copies17 words and installs vtable `01600104`. Its complete-object locator `01639448` references type descriptor `018E4F60`, named `.?AVSimpleExtent@@`. | Confirmed RTTI and layout. | Legacy pointer layout need not be reproduced. |
| `01260B90` converts source extent kinds0/1/2 to type9 simple values, recursively converts3/4 composites, clones5 and copies9; unsupported kinds return null. | Confirmed conversion branches. Semantic names for all kinds remain open. | Appearance extent loading and actor extent conversion are separate boundaries. |
| `01260E40` refreshes owned local `+B0` and transformed `+B4` extents, destroys both when owner scale changes, tries an owner-template source and another owner source, and has a conditional sphere-derived fallback. It clones and transforms the local extent, then clears the dirty byte. | Confirmed lifecycle. Exact source getter ownership and full source precedence semantics remain open. | Transform/scale changes require deterministic invalidation and rebuild. Do not retain stale collider data. |
| `01267A90` requires the actor's extent to dynamically cast to `SimpleExtent` and its contained kind to be0. It queries candidate objects through `012A0AB0`, ranks contacts by entry then exit, and separately queries floor through `012647B0`; when both produce usable results it compares squared distances. | Confirmed inspected contact-selection path. Reachability from every grounded actor controller is not established. | Body and floor ordering must be traced at the movement caller before claiming original actor equivalence. |
| `01266960` has a conditional sphere query through `01281C30` before floor continuation through `01267380`. `01281C30` filters candidate properties, constructs transformed query values and calls `012A0DF0`/`012A0D00`/`012A12D0`. | Confirmed extent-based preflight in the inspected floor path. | Floor adjacency alone does not establish complete actor collision. |

No direct `MeshAppearance` collision virtual or shader-primitive query is present in the inspected body helper chains. This establishes what these functions consume; it does not prove all actor paths avoid mesh intersection. Direct caller search found query selector setters at `00843EF1` and `008A5833`; their inspected surrounding functions perform segment-style visibility/occlusion queries. These sites do not establish a grounded actor selector.

## Current replacement policy

The [static mesh selection rule](STATIC_MESH_COLLISION_SELECTION.md) describes original segment-query eligibility. The [triangle box guard](TRIANGLE_BOX_SWEEP_CONTRACT.md) is an independently authored conservative volume policy. The original Mustafar probe exposes rendered0.1 m rises over flat FLOR. That result remains valid for the modern box policy; it is not evidence that the original grounded actor stops there.

Do not increase feet clearance to conceal those rises or infer that every shader-eligible triangle is an original actor obstacle. Keep the full-triangle probe available, but defer default persistent-host activation until collision-extent source selection and the movement caller are reconciled. Existing floor/door movement remains a bounded modern implementation.

## Next bounded question

Trace the owner-template source feeding `01260B90` in `01260E40`, plus the original APPR/CELL collision extent loaders. Identify whether the selected original room/door sources supply NULL, primitive or composite extents. Then follow the grounded movement caller to establish the active body shape and floor/body ordering. Stop at a pinned source-to-query chain or an explicit unsupported/missing source; preserve unknown controller policies.


## Authored source follow-up

The [secondary extent decoder](APPEARANCE_EXTENT_DISTANCE_CONTRACT.md) and independent six-source probe confirm NULL APPR secondaries in selected room/door assets. Selected CELL bounds are also NULL. Loader00B2CD10 stores secondary template+10; primary00B2CCF0 stores+0C. Alternate owner source and synthesized fallback remain open. Authored NULL does not prove absent runtime body collision.

## Creature fallback and source precedence

The [fallback checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md) closes the inspected extent refresh source order: appearance secondary, layout cell0 extent, then conditional creature sphere. Constructor `01260110` installs RTTI CollisionProperty and initializes fallback flag `+78` false. `012604D0` enables it on a SharedCreatureObjectTemplate branch. It is not enabled merely by a NULL secondary.

Confirmed field chain: `stepHeight` → getter `011AF1B0` → property `+9C`; `collisionRadius` → `011AF8D0` → `+A0`; `collisionOffsetX/Z` → `011B05B0/011B0940` → `+A4/+A8`; `collisionLength` → `011B0CD0` → `+E8`. Loader `011B13F0` binds those names and offsets. `collisionHeight` is separate from step height.

With ordinary flag `+7B` false and positive scale, fallback local center is `(offsetX * scale, (stepHeight + radius) * scale, offsetZ * scale)` and radius is `radius * scale`. Its lowest point is step height above local origin. Special flag `+7B` true substitutes radius0.5 and step height1.0 before scaling. Runtime flag meanings, active original template values and controller-wide participation remain open. This does not justify increasing the existing box's clearance: it motivates a distinct evidence-backed actor shape.

Template inheritance, random modes and relative parameter operations must be handled or rejected explicitly before adopting an original profile. Next probe resolves the original human template's deterministic collision values and appearance chain.


## Original human profile follow-up

The [object-root profile](OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) independently resolves step height0.5 and radius0.5 from five original inherited templates. At scale1 the recovered local fallback sphere is center(0,1,0), radius0.5. Active controller participation remains open; the preview actor has not been changed.
