# Analysis checkpoint: track animation controller runtime

Date: 2026-09-12

## Scope

The runtime bridge from the previously mapped ASHT/LATT/keyframe resources to per-frame skeletal poses, locomotion deltas and animation notifications.

## Confirmed findings

RTTI resolves `TrackAnimationController` to vtable `0x015E5EFC`. The concrete constructor ends at `0x0082F600` and installs that vtable. It receives three external owners/inputs, constructs three mapping arrays, creates one 0x4C-byte physical-track runtime for each globally installed track template, and initializes distinguished logical tracks from global identifiers.

The controller separates three concepts:

- global/install-time track templates and names;
- per-controller logical-to-physical track maps;
- per-physical-track animation state and cached pose output.

The mapping builders at `0x00830060` and `0x008300C0` resolve template-defined indices into controller-local arrays. Lookups at `0x00830130`/`0x00830150` then map a logical track identifier through that table to the actual physical-track object. This is not a single active clip attached directly to the skeleton.

Embedded registration strings identify the installed track vocabulary `logical_root`, `logical_primary`, `add_combiner` and `action_combiner`. This confirms two distinguished logical tracks and two combiner roles; these names are engine contracts and should be preserved in a compatibility layer rather than replaced with anonymous numeric slots.

### Per-frame control surface

The TrackAnimationController vtable exposes these confirmed behaviors:

| Slot function | Observed role |
|---:|---|
| `0x0082F940` | forwards elapsed time to the distinguished/root physical track (`0x0083E630`) |
| `0x0082F960` | evaluates one transform/channel through the selected physical track (`0x0083E890`) |
| `0x0082F990` | copies accumulated locomotion rotation/translation from a selected physical track (`0x0083EC70`) |
| `0x00832860` | returns no optional resolver/object |
| `0x00832870` | no-op setter hook |
| `0x00832880` | registers an indexed callback/listener record |
| `0x008329D0` | removes an indexed callback/listener record |
| `0x00832A60`, `0x00832A70` | false/default optional capabilities |

The physical-track update at `0x0083E630` rejects non-positive elapsed time, advances the active skeletal animation through its virtual interface, collects locomotion rotation and translation, handles completion/remaining-time conditions, and processes queued state-change records. Transform evaluation uses the live animation when present and otherwise returns cached transform and translation arrays.

### Physical-track state

The constructor at `0x0083E290` establishes:

- owner controller at `+0x00`;
- track template at `+0x04`;
- retained active SkeletalAnimation at `+0x08`;
- queue/status fields at `+0x0C..+0x1C`;
- accumulated locomotion translation at `+0x20` (three floats);
- accumulated locomotion rotation at `+0x2C` (four floats);
- cached per-transform rotations at `+0x3C` (16 bytes each);
- cached per-transform translations at `+0x40` (12 bytes each);
- current animation sequence/id at `+0x44`;
- monotonically increasing request/sequence counter at `+0x48`.

Cached arrays are sized from the skeleton transform count obtained through the controller's transform-name map. Destruction releases the active animation, queued change records, and both pose caches.

### Animation changes are sequenced, queueable and reference-counted

`0x0083E9B0` is the central physical-track animation-change operation reached from `0x0082F7B0`. It accepts an animation/template plus mode and transition parameters, obtains a new sequence number from `+0x48`, and either applies immediately or allocates a 0x14-byte queued change record. Existing active animations are explicitly detached/released through skeletal-animation reference APIs. The returned sequence number can later be queried/cancelled through the controller facade.

The controller also forwards animation messages to an external callback owner (`0x007E4760`) and contains a notification fan-out path (`0x00830170`) that enumerates messages/events produced by the active skeletal animation. Animation events are therefore a required output of evaluation, not decorative metadata.

## Strongly inferred findings

- `logical_root` is the root/locomotion-producing logical track and `logical_primary` is the ordinary primary animation track. The association is strong but the exact global-index assignment still needs a direct initialization trace.
- The four-float locomotion value is a quaternion and the three-float value is translation, based on their widths, identity initialization and skeletal-animation consumers.
- `+0x44` is the last/current issued animation sequence handle; public methods return `value - 1` or report active-state plus the current value.

## Replacement-client implications

A compatible animation layer needs logical named tracks mapped to physical evaluators, not merely an engine AnimationPlayer with one clip. Each physical track must preserve active animation ownership, queued transitions, stable request handles, per-transform cached fallback poses, root-motion accumulation, and emitted animation messages. The engine adapter may use native blend graphs internally, but these observable semantics belong above the engine-specific layer.

Pose and root-motion caches should use native 64-bit containers and aligned math types. Asset indices and controller-visible sequence values remain explicitly sized compatibility values; no file or protocol field should grow merely because pointers do.

## Unknowns

- exact global indices and priority rules for the four confirmed installed track/combiner names;
- all state-change mode enum labels and blend-time parameter meanings;
- complete ASHT state/link decision semantics feeding `0x0083E9B0`;
- ordering between controller message fan-out and appearance/world movement application;
- animation target/look-at modifier composition after base pose evaluation.

## Confidence

- Vtable identity, array topology, physical-track sizes, update/evaluate/root-motion split, reference ownership and queue existence: **confirmed by code**.
- Quaternion/translation labels and broad logical-track roles: **strongly inferred**.
- Exact transition enum names and track names: **unknown**.

## Resume point

Recover the installed global track-template table around `0x019249xx`, label distinguished track globals from their initialization strings, and trace the ASHT state-change action factories into `0x0082F7B0`/`0x0083E9B0`.
