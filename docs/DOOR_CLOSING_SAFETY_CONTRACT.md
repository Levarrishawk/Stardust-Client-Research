# Explicit modern closing obstruction policy

`SingleLeafDoorRuntime.Advance(..., closingActors)` optionally checks a supplied snapshot of incident actor bodies before a closing advance. `InteriorDoorActorScene(..., holdClosingLeaves: true)` supplies its owned actor body to every leaf. The historical default supplies no bodies and retains previous behavior. This policy is modern safety behavior, not proof of original door obstruction handling.

## Sweep and controller semantics

The controller previews its next state without consuming a frame. If fraction decreases, derive the candidate eased leaf translation. Authored leaf orientation is fixed and the easing is monotonic, so the complete translation can be tested as relative actor motion through the previous oriented leaf box. Actor bodies use finite positive world-axis half bounds and feet plus half-height centers. The existing separating-face rule applies. Any blocked/initial-overlap sweep holds the previous fraction and pose for that entire tick.

A held tick consumes its frame, reports `ClosingHeld`, leaves opening false and emits no close transition/effect. It does not request opening or grant permission. Once clear, closure resumes with normal events. This conservatively holds the whole proposed close instead of advancing partway to contact. It does not depenetrate an existing overlap or solve opening-leaf collisions.

At most 32 unique nonzero actor handles are accepted; cells must be nonnegative, vectors finite and positive half bounds at most 100 m. Only accepted incident cells participate. Body records are copied before retained-position callbacks. Invalid snapshots, failed callbacks or relative sweep overflow reject before controller/contact mutation. Reset/dispose clear the hold flag and contacts.

## Verification and limits

[Closing gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DOOR_CLOSING_SAFETY.md) covers transformed actor/leaf frames, full-close obstruction, repeated holds without drift, release/events, unrelated owner filtering, snapshot mutation/failure and historical-default behavior. Independent slab/controller comparison matches 240 synthetic ticks with 52 holds. Scene denial stays blocked while closure is held; retreat releases occupancy and closes normally. A source already genuinely inside a permission barrier retains the existing initial-overlap policy; this option does not repair such a spawn.

No narrow-phase leaf mesh, crushing/push response, arbitrary rotating leaf or original global collision order is claimed. The original multi-door default trace remains unchanged.

## Original and host gate

[Mustafar occupied close](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_DOOR_CLOSING_SAFETY.md) independently matches 692 ticks: guarded 346/156 holds/no physical stops, unguarded replacement 346/314 physical stops. Guarded retreat releases closure while source ownership remains unchanged. `-HoldClosingDoors` is an explicit scene-only preview option. Headless/GPU checks verify hold, denied entry, retreat/release and existing timing/input/placement; private capture inspected. This is replacement policy comparison, not observed original game obstruction behavior.
