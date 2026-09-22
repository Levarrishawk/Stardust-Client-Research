# Analysis session: multi-block terrain, interiors, animation, audio and input

Date: 2026-09-12

## Scope and baseline

This session continued static analysis of `C:\Stardust-DEV\Stardust.exe` only (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`). No replacement-client code was written, no executable was modified, and nothing was published to GitHub.

Completed bounded blocks:

1. terrain auxiliary grids and flora runtime;
2. terrain lighting and static-collidable flora;
3. interior floor, portal and door runtime;
4. skeleton, logical animation, keyframe and ASHT formats;
5. TrackAnimationController runtime;
6. SD2D/SD3D sound-template schema;
7. DirectInput, normalized events, IoWin dispatch and IMAP/0006 bindings.
8. yaw animation selection and skeletal look-at/target pose modifiers.
9. StateHierarchyAnimationController state/link/action flow into physical tracks.
10. Sound2d/Sound3d runtime, dynamic volume/pitch envelopes and Miles backend bridge.

## Material findings

### Terrain and interiors

The procedural terrain model now covers asynchronous chunk requests, generation/commit state, exact LOD and seam topology, layered material construction, collision queries, typed auxiliary grids, radial flora managers, lighting-mask correction, and deterministic static-collidable flora. Interior analysis covers FLOR 0000..0006, stored path acceleration, cross-floor movement, PRTO/CELL/PRTL versions, portal crossing, and the proximity-driven paired-door state machine synchronized with collision and portal blocking.

### Character animation

The asset chain is a typed composition system rather than a conventional skeleton-plus-clip pair. `SLOD` selects capped skeleton detail levels; `SKTM` supplies skeleton data; `LATT` maps logical names to typed animation templates; `CKAT` and `KFAT` provide compressed/uncompressed channels; and `ASHT/0002` supplies action groups plus a recursive state root.

At runtime, named logical tracks (`logical_root`, `logical_primary`) map to physical evaluators through installed combiner roles. Physical tracks retain active animations, queue transitions, issue stable sequence handles, cache poses, accumulate locomotion rotation/translation, and emit animation messages. These semantics must sit above any future engine animation API.

### Audio

`SD2D` accepts versions 0000..0003. Deployed v3 holds a 32-bit sample count, retained sample strings, and 28 ordered 32-bit scalar fields. Its writer confirms the parser order. `SD3D` wraps the common payload and accepts 0000/0001 tails. Post-load mode/radius normalization is behavioral and must be preserved. Individual labels for anonymous scalar fields remain deliberately unclaimed pending Miles-call correlation.

### Input

Keyboard, mouse and up to eight joysticks are acquired through DirectInput8 and translate 20-byte buffered records into normalized events. Keyboard mapping uses a configured 256-entry translation table. Events enter a five-dword FIFO, near-identical analog events can be coalesced, and the service pass sends events down the IoWin stack until consumed or a window is removed.

`IMAP/0006` is the effective input-map schema. It stores named commands, shift/modifier states, and separate sparse families for keys, mouse buttons, joystick buttons, axes, sliders and POVs. Ground, space and UI maps are separate assets.

### Skeletal pose continuation

The typed yaw-animation runtime reads the signed `yaw_dir` animation-environment value, chooses left, right or neutral nested animation, and cross-fades only when that selection changes. This closes the path from the yaw asset template to live evaluation.

Look-at and target-pitch transform modifiers compute clamped local-space angles after controller pose output. Look-at yaw is rate-limited; target pitch tracks a distinct valid target. Skeletal appearance setup attaches pitch at `spine3` and a linked two-modifier look-at chain across `neck` and `head`, tolerating skeletons without the expected names.

The live state-hierarchy controller preserves ordered hierarchical paths, walks outgoing/incoming state links around their common prefix, resolves link and destination-loop logical animation names through LATT, and routes them to named `action` and `loop` tracks. Action lookup walks state ancestors and selects `action` or `add`. A shared bridge instantiates the typed animation, optionally retries `default`, selects a transition duration, and calls the previously mapped TrackAnimationController/PhysicalAnimationTrack change path.

### Audio playback continuation

Concrete Sound2d/Sound3d runtime objects preserve per-instance sample-selection order, volume and pitch target generators, lifetime envelopes, attenuation and smoothed obstruction/occlusion. The client adapts one logical voice to Miles 2D sample, 3D sample or stream handles. Template offsets `+0x70..+0x80` are now constrained as volume mode, target interval, target range and interpolation time; the `+0x5C..+0x6C` family drives pitch/playback-rate variation. Effective values reach the exact Miles volume/rate/obstruction/occlusion APIs.

Template `+0x2C/+0x30` select the per-instance loop count, while loop-block markers come from sample resource metadata rather than another `.snd` scalar pair. The three backend types receive their corresponding Miles loop-count calls; resident 2D and stream voices additionally receive loop blocks. Template `+0x8C` is confirmed as an input to the Miles 3D distance pair.

The configured 48-archive overlay contains 7,242 effective `.snd` winners: 4,043 explicit SD3D wrappers and 3,199 SD2D roots. All use common SD2D/0003; all explicit SD3D wrappers use an empty tail 0001. A 528-file SD2D subset has an additional empty sibling `0001` chunk that the legacy loader accepts and a strict replacement parser must investigate rather than discard by assumption.

The final streaming-policy trace distinguishes two independent configuration limits. `ClientAudio/maxSampleCount` is clamped to 16..64. `ClientAudio/maxCached2dSampleSize` defaults to 64 KiB and is clamped to 64..512 KiB at the audio boundary. For stream-capable 2D sounds, a sample strictly larger than the latter threshold reaches `AIL_open_stream`; a positive sample at or below it reaches resident `AIL_allocate_sample_handle`/`AIL_init_sample`. Spatial 3D voices follow their own allocation branch. This is an implementation-grade compatibility rule, not a general memory-cache budget. A final validation also corrected the category gain table width to 13 entries (indices 0..12); configured overrides are sound effects at 1, UI at 4, background music at 8 and player music at 9, with other indices initially 1.0.

The scalar corpus pass decoded all 28 common words across all 7,242 winners. It confirms four-value volume and pitch mode enums, the dominant 1/-1 loop policies, an invariant-zero `+0x4C`, and realistic deployed 3D-distance values. It also found a genuine unresolved hazard: 558 descriptors carry category value 13 even though the observed gain initialization covers only indices 0..12 and the backend accessor appears to index directly. Per-file inventory identifies those 558 primarily as voice/dialogue assets (`vo`, C-3PO, Han, Darth and related families), making a specialized route plausible but not yet proven. This is recorded as a contradiction requiring reachability analysis, not papered over as a safe array access.

## Evidence quality

- **Confirmed by code:** recorded addresses, tags, versions, read/write orders, object widths, vtable routes, queue shapes and ownership statements.
- **Strongly inferred:** quaternion/translation labels for root motion, broad meaning of sound mode/radius fields, and root/primary role association of named logical tracks.
- **Unknown:** exact labels for the remaining sound-mode scalars, full ASHT predicate/interruption semantics, all animation composition variants, specialized joystick record widths, all IoWin flag names, and numerous gameplay/UI message handlers.

## Reproducibility and integrity

- The sound overlay result is generated by `sound_overlay_inventory.py` from the 48 configured `searchTree_00_*` archives, not from filename sampling.
- `sound_overlay_winners.csv` contains exactly 7,242 data rows, all live, with 4,043 SD3D and 3,199 SD2D roots. Its checkpoint SHA-256 is `612EDECBD1C52E403961923612C3201B5947C4C83935CA0D5C80686F8CEF9768`.
- `sound_scalar_distributions.csv` and `sound_scalar_values.csv`, generated by `sound_scalar_inventory.py`, preserve every observed value/count and the corresponding per-descriptor rows rather than only summarized common values.
- All conclusions in this session use `Stardust.exe` with the identity stated above. `SWGEmu.exe` was neither present nor used.
- The private workspace and dated staging copy were hash-compared after publication. No repository-tracked files were changed.

## Progress

- Comprehensive weighted whole-client analysis: **67%**.
- Implementation-grade replacement-readiness research: **87%**.
- Raw function-name coverage remains a separate, much smaller number and is not represented as completion.

These estimates increased because broad subsystem summaries now have concrete schemas and live runtime paths. They do not imply that every function, handler, mediator or asset variant is documented.

## Session timing

The bounded session began at approximately 12:32 Eastern and closed after the final audio resource-policy block at approximately 14:32 Eastern. The last block was completed rather than abandoned at the time boundary.

## Recommended next block

Enumerate ASHT link predicate subclasses and action interruption policies, then inventory effective installed `.snd` scalar distributions to constrain the remaining lifetime-envelope, category and pitch modes. Continue specialized transform modifiers after those bounded blocks.
