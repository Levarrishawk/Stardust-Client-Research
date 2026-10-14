# Animation event cursor contract

R1-RUNTIME-01 implements an engine-neutral **MODERN playback policy**, not recovered original animation scheduling. It consumes owned `CompressedLocomotion.Messages`; no new MSGS grammar, effects/audio delivery, root translation, animation graph or prototype host adapter is added. See [existing grammar and pinned inputs](COMPRESSED_LOCOMOTION_CONTRACT.md) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_ANIMATION_EVENTS.md).

## API and ownership

`new AnimationEventCursor(source, looping: true, maximumCyclesPerAdvance: 64, maximumEventsPerAdvance: 4096)` snapshots occurrence identities and sorts by frame, group index and occurrence index. `Advance(timestamp, phase)` accepts an independent monotonic nonnegative finite timestamp and absolute unwrapped frame phase. The caller converts playback seconds/FPS to phase; no clock is sampled. `Phase`, `Timestamp`, `IsPaused` expose state. `Pause()`, `Resume()` and `Reset(timestamp = 0)` are explicit controls. Reset starts a new playback epoch at phase zero, clears pause and re-arms initial frame zero.

Each immutable returned `AnimationEventOccurrence` contains `Cycle`, `GroupIndex`, `OccurrenceIndex`, `Name`, `Frame`. The identity is the cycle plus authored group/occurrence indices within a playback epoch. Identical names/frames remain separate authored occurrences. Batches are owned read-only snapshots and survive subsequent ticks/reset; source bytes may be cleared after decoding. This is a single-owner cursor; callers serialize access. No listeners/resources are retained and no disposal is required.

## Explicit modern interval policy

The recovered integer collector queries **[start,end)**. This cursor instead emits events crossed in **(previousPhase,targetPhase]**, plus cycle-zero frame-zero occurrences on the first positive advance after construction/reset. This difference is intentional replacement scheduling policy, not evidence of the original float conversion or track scheduler. Equal phase emits nothing, including initial phase zero; it does not consume pending initial events. Exact arrival at an integer frame emits that event immediately; fractional arrival below it does not.

Loop duration is `EndFrame` in frame units. At absolute phase `(cycle+1)*EndFrame`, deliver the old cycle's terminal occurrences before the next cycle's frame-zero occurrences. Both are distinct identities. Within the same frame/cycle use authored group/occurrence order. A nonlooping cursor accepts only phases from zero through EndFrame and emits terminal occurrences once. Looping output is ordered by absolute event phase, cycle, group and occurrence. Successful subdivisions of the same positive interval produce the same sequence as one advance, provided each call fits its budgets. Repeated ticks cannot re-emit an identity in the same epoch/cycle.

## Guards and failure atomicity

Timestamp and phase must be finite and monotonic. Phase is bounded by `2^40` frames to retain exact integer event positions in double arithmetic. Phase progress requires timestamp progress; equal timestamp/equal phase is legal. Paused advances require unchanged phase and may advance the timestamp. Resume supplies no catch-up: the caller continues from the retained phase. Reset accepts any finite nonnegative timestamp as a new epoch; invalid reset preserves state.

Per-advance cycle budget counts floor(target/EndFrame)-floor(previous/EndFrame) for loops, including exact boundary arrivals. Allowed configured limits are 1..1024 cycles and 1..262144 output events. The event budget includes initial/terminal/next-zero occurrences and duplicates. Binary bounds count the entire proposed output before allocation/publication or cursor mutation. Invalid or over-budget advances throw `InvalidDataException` and preserve timestamp, phase, pause and initial-event eligibility. Callers may subdivide an over-budget interval; failure does not silently clamp or discard events.

## Evidence, confidence and limits

Confirmed existing decoder/collector structure and pinned original walk/run records are documented in the locomotion contract. The new deterministic suite independently enumerates absolute event times and compares 160 quarter-frame targets, whole/subdivided/repeated advances and identity uniqueness. Original clips carry footstep frames walk 7/23 (end33), run 6/17 (end22); three-loop output contains six occurrences per clip. Their lack of endpoint events does not validate endpoint scheduling; explicit synthetic cases do.

Confidence: Confirmed for verified replacement behavior and pinned bytes; original scheduler semantics remain **UNKNOWN**. Existing research scores and protocol/function registries are unchanged. An event batch authorizes no gameplay effect inferred from its name.

## Verification and integration

From the checkout root, run `dotnet run --project poc/round1-tests/Runtime/Runtime.csproj -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg`. Without arguments it runs synthetic cases only. The runner checks the configuration SHA256 and both effective priority30 member hashes before decoding. It reuses the existing overlay and CKAT loaders.

Lead integration: add `AnimationEventCursorScenarios.Run();` to the common smoke runner. Optional original probe call is `AnimationEventCursorScenarios.Original(root, configuration);`. No shared entry point is edited by this worker. Next bounded action: lead review/integration and combined validation; any adapter delivery milestone requires a new assignment.

## Elapsed-time adapter

[AnimationEventPlayback](ANIMATION_EVENT_PLAYBACK_CONTRACT.md) now supplies compensated elapsed-seconds/FPS conversion and owner/generation-tagged batches. It preserves this cursor's policy and budgets, adding focus/play/reset/replace controls. Effect/audio delivery and original scheduler semantics remain open.
