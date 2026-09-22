# Analysis checkpoint: DoorObject live state machine

Date: 2026-09-12

## Scope

The live update, proximity-trigger, paired-door, appearance, effect, collision and portal-barrier behavior of the client `DoorObject` constructed from portal-layout data.

## Confirmed findings

### The door is a composite runtime object

The constructor at `0x0128FF40` initializes:

- two independent interpolator/controller records at `DoorObject +0xA0` and `+0xB8`;
- the data-table motion vector at `+0xD0`;
- a portal-record/template link at `+0xDC`;
- an optional paired door at `+0xE0`;
- open/close timing and smoothing values at `+0xE4/+0xE8`;
- a primary appearance object at `+0xF0`, optional mirrored/secondary appearance at `+0xF4`, and portal barrier at `+0xF8`;
- four effect-name handles at `+0x10C..+0x118`, matching the table's open-begin, open-end, close-begin and close-end columns;
- a trigger radius squared at `+0x120`;
- a small retained trigger-object list at `+0x124..+0x12C`.

It then creates appearances, the dedicated collision property and the portal barrier. These are not independent decorative components: the per-frame update coordinates them.

### Open/close motion is interpolated and applied symmetrically

`door_object_alter_VA_01290760` is the DoorObject frame update. It advances both interpolators using frame delta, derives a normalized/curved displacement using the configured timing and springiness/smoothness values, and applies the motion vector to the primary appearance. When a second leaf exists, it applies the negated displacement to that leaf.

The update also propagates the resulting door fraction/state to attached appearance/customization-facing properties and returns an alter scheduling result through the base object machinery.

### Effects are transition-indexed

The update asks each interpolator for a transition index, selects the corresponding effect handle from the four-entry effect array, and starts the effect on the frame, primary leaf, secondary leaf and/or barrier appearance when present. This structurally matches the data-table distinction between begin/end effects for opening and closing.

### Portal blocking follows actual transition state

When the portal template allows a runtime door, the update derives an enabled/blocked value from the current interpolator state and calls the portal-record state setter at `0x00B53940`. That setter writes a byte only when it changes and then invokes a registered change callback. The separate portal barrier object is paired with this state.

For the force-field style path, `+0x11C` selects specialized appearance/effect handling. The portal-record disabled flag at `+0x11D` can force the portal state rather than allowing ordinary interpolation to control it.

### Opening is driven locally by collision/proximity objects

The DoorCollisionProperty callback at `0x01290BB0` validates that a candidate object's start/end cell owners correspond to the door or its paired leaf. It adds the candidate to the DoorObject trigger list through `0x01290D50` and starts the relevant interpolator direction.

The trigger list is capped at 25 entries. `door_object_prune_trigger_objects_VA_01290EC0`, called every frame, removes destroyed/null objects and objects whose squared distance exceeds the configured trigger-radius squared. This establishes a local proximity/collision-driven automatic-door model. No distinct door-open network message was found in this path.

The client still consults live portal/cell permission through the linked portal record (`0x01290CF0` -> `0x00B53920`). Thus replicated cell permission can suppress traversal/open behavior without rebuilding the door resource.

### Paired doors coordinate

`0x01290CB0` installs the paired-door pointer and forwards the corresponding portal barrier association. `0x01290D10` reports a combined eligibility/state condition across the door and its optional pair. This accounts for layouts where two door objects represent opposite sides/leaves of the same passage.

## Replacement requirements

1. Preserve the door-style table as authored data rather than baking styles into code.
2. Implement door motion as a deterministic state controller with the original timing, smoothing, two-leaf sign inversion and transition effects.
3. Keep visual openness, collision shape and portal-graph blocking synchronized.
4. Maintain trigger membership by stable object handles, not raw pointers; cap or otherwise bound the set.
5. Apply replicated cell/portal permission dynamically on top of local proximity behavior.
6. Treat force fields and paired leaves as explicit variants.

## Confidence and remaining unknowns

| Claim | Confidence |
|---|---|
| two controllers drive door transition state | Confirmed structurally |
| primary/secondary leaves receive opposite movement | Confirmed |
| four effect entries map to transition begin/end events | Confirmed by executable/table correlation |
| trigger radius is stored squared and used to prune occupants | Confirmed |
| trigger list hard cap is 25 | Confirmed |
| portal state changes invoke a registered callback | Confirmed |
| no normal door-open packet is required | Strongly inferred from this path and server-source absence; not a proof of every special door |
| exact historical controller class and state-enum labels | Unknown |

