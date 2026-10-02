# Explicit original animation pose preview

The standalone creature inspector can select one original logical animation table/name and clip SHA-256, plus an explicit frame. Original object/SAT pins remain required. The shared source cache resolves all dependencies; only the selected clip is prepared. `PreparedSkeletalAnimation` maps the selected LATX relationship to exactly one owned skeleton, copies the selected L0 joints and prepares channels on the background worker. Missing/ambiguous mapping or unsupported event/root suffixes reject. Preparation is explicit; it retains unused/missing names and declares BindPose fallback. No animation graph, priority execution or gameplay root motion is enabled.

Worker archive/configuration scopes close before transfer. Frame validation completes before GPU publication; cancellation at the first lookup transfers no geometry. Main thread applies the prepared local pose through the existing53-joint hierarchy and complete weighted shader deformation. CPU arrays validate before surfaces mutate; existing meshes/materials/UVs/triangles remain. Normals are normalized only at Godot's packed-normal boundary. Tangents remain core-verified and are not uploaded by the neutral adapter.

## Verified sample

The pinned original human ambient idle retains15 effective sources/726,134 bytes. Frames0/30/60/90/120/0 pass complete position/normal/UV/index readback and a bind reset. Explicit captured frame30 renders the selected original channel pose, with aligned attached head/body under neutral materials. The unused `rifle`, missing-bind `hold_l` and zero repaired packed values are reported. This is sampled original animation data, not the earlier synthetic forearm pose. At this milestone it is a frozen frame, not timed playback.

Headless, cancellation, GPU upload/capture and the prepared-vs-explicit native pose comparison pass. The earlier independent265-joint/8,950-vertex audit is rerun with prepared poses and remains within2e-5. Exact original matrix-cache/thread/modifier semantics, character textures/customization/generated face and full animation state graphs remain separate.

## Launcher

Add these to the existing `-Creature` object/SAT-pinned launcher:

```powershell
-CreatureAnimationTable appearance/lat/all_m.lat `
-CreatureAnimationName emt_standing_idle_ambient_1_ag `
-CreatureAnimationSha256 798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B `
-CreatureAnimationFrame 30
```

Frame/name/table/pin are required together; diagnostic CreaturePose cannot be combined. `-Headless` tests upload preparation, `-CancelCreatureLoad -Headless` tests cancellation and a fresh ignored `-CapturePath` tests GPU rendering. Preparation is opt-in in the engine-neutral bundle reader and adds no external source read beyond the selected dependency closure.

[Pose contract](SKELETON_ANIMATION_POSE_CONTRACT.md), [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_ANIMATION_POSE_PREVIEW.md). Next: bounded preview clock, scrubbing and timed playback, with explicit loop/focus/elapsed-time policy.


Follow-up: [bounded timed playback and scrub/play controls](ANIMATION_PREVIEW_PLAYBACK_CONTRACT.md) now pass; the frozen pose remains available.
