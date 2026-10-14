# Analysis checkpoint: terrain flora runtime

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`

## Outcome

RTTI, constructor labels and concrete vtables establish three client terrain-flora managers with separate data callbacks and runtime strategies: static non-collidable flora, dynamic-near flora and dynamic-far flora. Static collidable flora remains in the shared terrain/object path rather than these three client-only manager instances.

## Class and ownership evidence

| Class | Vtable | Terrain field/category |
|---|---:|---|
| `ClientStaticRadialFloraManager` | `0x015E64C8` | `+0x1C4`, `FloraStaticNonCollidable` |
| `ClientDynamicRadialFloraManager` | `0x015E6FFC` | `+0x1BC`, `FloraDynamicNear`; `+0x1C0`, `FloraDynamicFar` |
| `ClientRadialFloraManager` | `0x015E704C` | shared base behavior |
| `FloraManager` | `0x015ED0E4` | common abstract/base family |

The constructor passes the exact callback for each generated grid and records every manager in a common vector at appearance `+0x1B8`, allowing lifecycle/update iteration without erasing domain-specific behavior.

## Static non-collidable flora

The static manager follows the active camera, maintains a collection of spawned flora objects, and applies a smooth distance-band fade rather than a binary cutoff. Placement resolves a flora family/template from the ClientChunk sample, creates an appearance/object, applies orientation/scale transformation, terrain-derived color tint where available, and attaches it to world/render ownership.

Its query methods also transform segment/collision requests into each flora object's local space and can return transformed hit information. This does not make the category authoritative server collision: the manager label is explicitly `FloraStaticNonCollidable`, and the collision helpers serve client picking/effects or local queries.

## Dynamic near/far flora

Both dynamic domains use `ClientDynamicRadialFloraManager` with different callbacks and configured distance bands. The per-frame virtual advances a double-precision accumulated time using frame delta, then runs shared radial-manager maintenance.

Dynamic nodes retain generated transforms/color and optional animation parameters. Rendering groups node output into local shader primitives. A batching routine caps one submitted primitive at 500 entries and emits additional primitives as needed. A shared prebuilt index resource contains 500 quads (3,000 16-bit indices). This is a legacy draw batching policy, not an asset-format constraint.

Recovered node preparation applies:

- camera-relative distance and radial-tile residency;
- generated transform/orientation;
- 0..255 color normalization;
- optional sinusoidal motion driven by accumulated time and family parameters;
- shader/material bucketing before sorter submission.

The near/far split therefore controls density/distance/render cost while sharing the same weighted-family sampling format.

## Lifecycle and replacement architecture

Manager update paths read the current camera position, advance radial coverage, retire stale nodes and prepare visible entries. Destruction clears nodes, render buckets and owned appearances before invoking shared base cleanup.

A replacement should model:

```text
PTAT/TGEN flora family data
  -> deterministic tile/seed sampler
  -> category-specific residency policy
  -> engine-neutral instance records
  -> material/mesh grouped GPU instancing
```

Prototype host MultiMesh or an equivalent explicit renderer instance buffer is conceptually appropriate. Compatibility depends on family selection, seed/tile determinism, transforms, color and distance-band behavior—not on 500-quad batches, legacy memory pools or raw appearance pointers.

Static collidable flora must remain a different object/collision policy from decorative static and dynamic flora. The client may render decorative flora locally, while the server remains authoritative for movement and tangible world objects.

## Confidence and unknowns

Confirmed: class identities, three manager instances, exact category labels/callbacks, separate near/far grids, camera-relative update, accumulated time, 500-entry batching and 3,000-index shared quad buffer.

Strongly inferred: packed three-byte value is terrain-derived flora color; sinusoidal branch is wind/sway animation; static-manager collision helpers support local picking/effects.

Unknown: exact random-seed formula, radial tile eviction thresholds, every family record field, static collidable flora object construction, wind parameter labels, and sorter phase used by every flora material.

