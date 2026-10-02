# Animation event playback contract

R2-RUNTIME-01 adds `AnimationEventPlayback`, an engine-neutral elapsed-seconds bridge to the [modern event cursor](ANIMATION_EVENT_CURSOR_CONTRACT.md). It uses existing [owned MSGS records and pinned clip evidence](COMPRESSED_LOCOMOTION_CONTRACT.md), without a new parser/scheduler, wall clock, preview-clock mutation, animation graph, Godot, root motion, or effect/audio delivery. Original scheduler semantics remain **UNKNOWN**.

## API and time policy

Construct with `AnimationEventPlayback(source, frameRate, looping = true, maximumCyclesPerAdvance = 64, maximumEventsPerAdvance = 4096)`. It starts playing and focused at phase0, active seconds0, generation1. FPS is finite, greater than0 and at most1000, matching the existing preview-clock time-base bound. `Advance(elapsedSeconds)` validates finite seconds in [0,60], then returns an owned read-only `AnimationEventPlaybackBatch` implementing `IReadOnlyList<AnimationEventOccurrence>`. No wall clock is sampled or injected.

`ActiveSeconds` accumulates accepted active tick durations using compensated summation. Compute absolute frame phase by multiplying the compensated total by FPS once. This avoids accumulating independently rounded frame increments. `UnwrappedPhase` is the cursor's absolute phase; `CurrentFrame` is phase modulo EndFrame for loops and the clamped phase for nonloops. `CompletedLoops` is floor(phase/EndFrame) for loops and0 for nonloops.

Explicit MODERN rounding policy: snap an absolute phase within eight machine epsilons times max(EndFrame,abs(phase)) to the nearest positive integer frame. This generalizes the preview clock's cycle-boundary snap to authored event frames. Keep raw active time and compensation after snapping; tiny increments are not discarded. A snapped phase never moves backward. This precision convention can deliver an event within that tolerance of its mathematical boundary; it is not original runtime rounding evidence or a claim of arbitrary floating-point partition equivalence.

The cursor exclusively enumerates events. Its (previous,target] interval, first-positive initial frame0, terminal-before-next-frame0 order, duplicate occurrence preservation and cycle/event budgets remain unchanged. Zero elapsed returns an empty current-generation batch without consuming initial events. Whole/subdivided successful ticks produce the same tested sequences under the explicit precision policy. A nonlooping final tick clamps phase to EndFrame, includes terminal events, counts its entire supplied active duration even if it overshoots, then stops. Subsequent ticks are ignored until restart/reset/replace; `Resume()` at the end cannot restart the clip.

## Controls and generations

`Pause()` sets Playing false; `Resume()` sets it true unless a nonloop has reached its end. `SetFocused(bool)` independently gates updates without changing Playing. Valid elapsed time while paused, unfocused, stopped or zero produces an empty batch and changes neither time nor phase. There is no catch-up when focus/play resumes. Invalid deltas reject even while gated.

`Reset(playing = false)` starts phase/time0, clears time compensation, increments Generation, retains focus and clip/time-base/budgets, and defaults to stopped. `Restart()` does the same and starts playing. `ReplaceClip(source, frameRate, looping = true)` first validates and constructs a new cursor, then increments Generation, starts at phase/time0 playing, and retains focus/configured budgets. Repeated restarts/replacements deliberately create separate epochs even for the same source.

Every batch carries `Generation`, including empty batches. Occurrence identity within an epoch remains Cycle/GroupIndex/OccurrenceIndex. Consumers must also associate it with its playback owner: `playback.IsCurrent(batch)` compares private owner identity and generation. A batch from another playback instance at matching generation is rejected. Generation is local to an owner; it is not a global clip ID. All prior batches remain immutable and readable after controls/clip replacement; stale work is rejected by IsCurrent rather than invalidating batch memory. No effects or resources are owned, no disposal is needed, and access is single-owner/serialized rather than thread-safe.

## Transactional guards

Reject invalid FPS/delta, time that cannot make representable positive progress, nonfinite or above-2^40 phase, or cursor cycle/event budget excess with `InvalidDataException`. Accepted positive active time must increase monotonically. Invalid replacement/reset generation exhaustion also rejects before mutation. These failures preserve time, compensation, phase, generation, clip identity and gates. A caller may retry an overbudget tick as subdivisions; no discarded batch or hidden time advancement occurs. Reset/replacement cannot silently wrap a long generation counter.

## Verification and evidence

Confirmed replacement behavior: independent oracle enumerates authored identities at absolute cycle/frame positions;1120 targets across FPS1,7.5,24,29.97,30,59.94,1000 compare whole ticks and17 unequal fractional subdivisions. Tests separately cover exact fractional arrival, loop terminal/frame0 order, duplicates, zero/gated time, pause/focus no-catch-up, reset/restart/replace, owner/generation staleness, nonloop overshoot/stop, invalid FPS/delta and transactional budget retry including compensated time. Source bytes are cleared and older batches checked after replacement.

Pinned original walk/run are independently identity-gated by configuration/member SHA256 and priority30; authored FPS30/end33/22/footstep frames7,23/6,17 are checked. At120Hz over three cycles both emit six occurrences and agree with one absolute interval and an independent oracle. Original inputs have no endpoint events, so endpoint scheduling is tested synthetically. Original controller policy remains unknown; no function/protocol/coverage score changes follow.

## Integration and validation

[Checkpoint and exact commands](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R2_EVENT_PLAYBACK.md) record offline restore, isolated console, warnings-as-errors Core/shared smoke compilation and workspace checks. The console at `poc/round2-tests/Runtime` links the new scenario without editing shared entry points; no arguments runs synthetic cases, archive-root/configuration arguments also run pinned originals.

Lead registration: `AnimationEventPlaybackScenarios.Run();`; optional original probe: `AnimationEventPlaybackScenarios.Original(root, configuration);`. Lead owns shared runner/navigation/ledger reconciliation. Suggested future assignment: a diagnostic adapter consuming current-generation batches at explicit clip-switch ownership boundaries, without inventing effects from names. This bridge stops before that integration.

## Round3 integration follow-up

The bounded delivery sink and original walk/run Godot adapter are described in [the diagnostic preview contract](ANIMATION_EVENT_DIAGNOSTIC_PREVIEW_CONTRACT.md). Original scheduler and audio/effect delivery remain open.
