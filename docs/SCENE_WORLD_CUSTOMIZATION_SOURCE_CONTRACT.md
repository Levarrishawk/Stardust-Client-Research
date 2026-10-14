# Ordered raw journal to supplied appearance source

[SceneWorldCreatureCustomizationSourceAdapter](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldCreatureCustomizationSourceAdapter.cs) is an engine-neutral single-consumer adapter implementing `ISceneWorldCreatureCustomizationSource` and `IDisposable`. It borrows the world, frozen raw journal, and explicit seed/projector callbacks. It does not load assets, decode CIDM names/ranges, construct linked-variable graphs, compose faces, or own a GPU visual. The next presentation boundary is the accepted [Round20 wrapper](SCENE_WORLD_CREATURE_CUSTOMIZATION_VISUAL_CONTRACT.md).

```csharp
new SceneWorldCreatureCustomizationSourceAdapter(
    world, rawSource,
    Func<SceneWorldObjectSnapshot, IReadOnlyDictionary<ShaderCustomizationKey,int>> seed,
    Func<PackedCustomizationSnapshot, IReadOnlyDictionary<ShaderCustomizationKey,int>,
        IReadOnlyDictionary<ShaderCustomizationKey,int>> projector,
    maxObjects: 64);
SceneWorldCreatureCustomizationSnapshot? Find(SceneWorldObjectSnapshot snapshot);
void Dispose();
```

`LastFailure` reports the most recent transaction failure, clearing at the next owner-thread `Find`. `CachedObjects` exposes the bounded actor-entry count. Construction, `Find`, `Dispose`, and cache diagnostics require the creating serialized owner thread. Construction checks world owner access without invoking seed/projector/raw callbacks. All borrowed references are released by disposal; the world and raw source are never disposed. Repeated disposal is harmless. Calls to `Find` after disposal throw.

## Staged application and acknowledgment

Each current finalized actor has at most one cache entry, keyed by object ID with exact incarnation, acknowledged raw through-revision, copied current bytes/extended-length flag, and the last committed full supplied snapshot. Entries are pruned against current world objects before lookup/capacity admission and after callback failure. Destroy, recreation, selection generation changes, and world close remove stale incarnations. Default capacity is 64; configurable range is 1..4096. Capacity failure occurs before source/seed/projector callbacks and returns null with `LastFailure`.

World freshness includes object, placement, envelope identities, and the current immutable underlying replication state. `Snapshots()` creates fresh outer replication wrappers, so outer wrapper reference equality is deliberately not used. `world.IsCurrent` and the underlying immutable state identity establish typed-state freshness.

The adapter reads every ordered unacknowledged update, using the accepted [packed codec](PACKED_CUSTOMIZATION_CONTRACT.md). It never projects only the last raw bytes. A nonempty packed record set obtains the explicitly supplied seed if no appearance values have committed yet; later sets use the previously staged full values. Every seed/projector result is copied into read-only owned values. Seeds must contain 1..4096 valid keys; names have 1..256 printable non-space ASCII characters. Every projection must return exactly the same full key set, including namespace/private flags. The adapter does not invent defaults or infer ranges. A projector must preserve record order and duplicate semantics; in particular, an out-of-range attempt can retain the old local value produced by an earlier valid write.

Both outer-empty and version1/count0 skip seed/projection. An all-empty transaction acknowledges raw progress while retaining the same supplied snapshot/revision (or null initially). A transaction containing any nonempty record set publishes a new full snapshot at the batch's `ThroughRevision`, even if values compare equal. Trailing empty updates do not reset staged values. An already-acknowledged empty journal returns the last supplied snapshot. A cold adapter seeing only already-acknowledged last bytes returns null; it cannot reconstruct missing ordered history from those bytes.

The batch's final update must match current bytes **and** `UsesExtendedLength`, and its revision must equal `ThroughRevision`. Update revisions must be beyond this consumer's acknowledged revision. Rewinds, same-revision altered bytes/flags, replayed updates, and unexplained externally acknowledged newer revisions fail. Encoded current bytes and each packed update use the codec's 1536-byte default bound; the frozen journal bounds updates to 4096 and codec records to 255.

All resulting values and the new entry are staged before acknowledgment. After callbacks, world freshness is rechecked. Before acknowledgment, another raw-source lookup must return the exact same batch object. `Acknowledge` must succeed for that exact batch. Afterward, the world and an empty acknowledged journal with identical incarnation/through/current bytes/flags must still agree before cache commit. Failed exact acknowledgment publishes no snapshot or cache change. Source callbacks that try reentry poison the transaction even if they catch the nested exception themselves; reentry propagates as `InvalidOperationException` without acknowledgment.

The raw source is a trusted serialized **single-consumer** journal and must implement atomic exact acknowledgment. The adapter cannot undo external mutations or a journal that falsely claims successful acknowledgment; a detected post-ack source/world mutation prevents cache publication and reports failure. Host policy must not silently continue after such a contract violation.

## Failure policy

Strict packed decoding and whole-journal atomic failure are explicit modern replacement policy. Structural malformed data or projector/seed failure rolls back even valid earlier updates within that transaction, leaves the journal unacknowledged, and retains prior committed appearance values. This differs from the recovered original sequential-setter/tolerated-tail behavior. A valid packed record containing an out-of-range attempt is a projector decision, not a structural codec failure; the ordered fixture preserves the earlier accepted value.

`Find` returns the previous supplied snapshot when the actor remains current after a transaction failure, and null when no prior values exist or the world became stale. It records the exception in `LastFailure`. No failed staging values or acknowledged revision enter the cache. Re-reading an unacknowledged batch attempts the bounded transaction again; there is no internal background retry or queue. The production host can choose fail-closed behavior by checking `LastFailure` after `Find` and throwing. Thread/reentry/disposed/argument misuse errors propagate rather than becoming appearance values.

## Native evidence and reproduction

[SceneWorldCustomizationSourceScenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldCustomizationSourceScenarios.cs) uses a real accepted typed world with a fake raw journal and a synthetic two-key projector. Its literal packed bytes are independently authored. The fake projector accepts values in `[0,10)` and retains old values on invalid attempts; this is test policy, not an implementation of actual CIDM/range projection.

The gate passes 97 assertions: initial empty/count0 bypass, full seed ownership, same-value notifications, ordered duplicates, valid packet followed by invalid range attempt, missing source retention, rewind/mutation/order/current/extended metadata failures, exact ack failure, source/world callback races, malformed atomic rollback, bad full-key projections, invalid seeds, caught reentry/disposal, cross-thread guards, generation reset/recreation/pruning, capacity recovery and world-close cleanup. Round20 shared selection and single-composer queue regressions pass. The engine-neutral default smoke suite also passes. No original asset reads or GPU runs occur in this worker milestone.

```powershell
./poc/round21-tests/Rendering/Launch.ps1 -PythonPath $taskPython -PackageCache $taskPackageCache
```

The [launcher](../../SWG-Godot-Client/poc/round21-tests/Rendering/Launch.ps1) uses task-private APPDATA, the existing package cache and a no-sources [NuGet config](../../SWG-Godot-Client/poc/round21-tests/Rendering/NuGet.Config), generates the baseline synthetic peer, and runs the own [native project](../../SWG-Godot-Client/poc/round21-tests/Rendering/Rendering.csproj). `$taskPython` is the existing bundled Python executable and `$taskPackageCache` the already populated private NuGet cache; no downloads are required. It prints an ignored/private JSON receipt path and restores caller environment settings. Shared Program registration and actual raw-store/CIDM/GPU integration remain lead-owned. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_CUSTOMIZATION_SOURCE.md).
