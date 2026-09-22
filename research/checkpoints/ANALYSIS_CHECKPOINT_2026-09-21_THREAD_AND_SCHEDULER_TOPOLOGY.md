# Thread and Scheduler Topology

## Executive result

`Stardust.exe` does **not** contain a modern general-purpose worker pool or task graph. Its concurrency is a small set of dedicated producers/workers plus main-thread commit points:

```text
main/game thread
  |-- GameScheduler deadline queue (not a worker thread)
  |-- drains completed network messages
  |-- drains asynchronous asset callbacks
  |-- owns object/world/UI mutation
  |
  +-- optional AsynchronousLoader worker ---> file-open/read results queue
  +-- FileStreamer worker -----------------> prioritized sliced reads
  +-- optional procedural-terrain thread --> terrain work/result handoff
  +-- optional network thread (disabled for configured TCP mode)
```

This reinforces the earlier finding that ordinary gameplay state changes occur on the frame owner, not concurrently inside receive or I/O workers.

## Foundation thread classes

RTTI exposes:

- `Thread`
- `MainThread`
- `FuncPtrThreadZero`
- `MemberFunctionThreadZero<ClientProceduralTerrainAppearance>`

The specialized template names match the dedicated function/member-thread design. There is no RTTI evidence for a broad job-system family.

## Asynchronous asset loader

`SharedFile` configuration at `0x00A98B50` provides:

- `enableAsynchronousLoader`, default true;
- `asynchronousLoaderPriority`, default 0;
- `asynchronousLoaderCallbacksPerFrame`, default 0;
- `validateIff`, default false;
- repeated `preload` paths.

The worker loop `0x00A96780` waits on a signal, dequeues requests under a lock, opens requested paths through `TreeFile::open(..., allowMissing=true)`, obtains file length/data or invokes a registered extension/type loader, and enqueues completed results. A global outstanding-byte count applies backpressure above `0x800000` bytes (8 MiB).

Main-thread completion drain `0x00A96BA0`:

- removes completed requests under the queue lock;
- invokes the client callback only on the drain side;
- releases opened file/data objects;
- invokes registered type-specific cleanup callbacks;
- returns reusable result vectors to a pool;
- decrements outstanding bytes;
- obeys `asynchronousLoaderCallbacksPerFrame` when nonzero.

Request cancellation `0x00A965B0` nulls matching callback/context pairs in both pending and completed queues rather than racing destruction. Request submission `0x00A966C0` enqueues and signals the worker.

## Other execution domains
## Superseding async-loader lifecycle trace

`ANALYSIS_CHECKPOINT_2026-09-21_ASYNC_LOADER_CANCELLATION_SHUTDOWN.md` closes cancellation and removal behavior: exact callback/context matches are nulled in pending and completed queues; worker I/O may finish but publication is suppressed; cleanup is unconditional; shutdown enqueues a null-batch sentinel, drains both queues, joins, and only then destroys registries and pools.

## Other execution domains

- **FileStreamer:** already confirmed as one worker with three strict-priority FIFO queues and 128-KiB read slices.
- **Procedural terrain:** controlled by `ClientTerrain.terrainMultiThreaded` (default true) and uses `MemberFunctionThreadZero<ClientProceduralTerrainAppearance>` plus explicit work/result queues documented in the terrain scheduling block.
- **Network:** `useNetworkThread` exists, but is forcibly disabled when TCP is enabled; completed application messages are copied to a queue and drained by the frame path.
- **GameScheduler:** install `0x0119DF70` allocates a small deadline/event queue and registers removal. `0x0119E010` schedules using a time converted to an integer tick/deadline; `0x0119E040` advances/removes due time relative to the scheduler clock. It is a main-thread timed-callback service, not evidence of parallel jobs.

## Replacement design consequence

Preserve ownership boundaries, not thread count:

- one authoritative simulation/session thread initially;
- network I/O may run asynchronously but publishes immutable validated messages;
- asset I/O/decoding jobs publish immutable results and finalize engine/GPU objects on their owning thread;
- terrain generation may use a bounded worker pool later, with generation IDs/cancellation at zone transitions;
- scheduler callbacks run through explicit lifetime-safe handles;
- shutdown cancels submissions, wakes and joins workers, drains/discards results, then destroys world/resources.

The original's 8-MiB async-loader backpressure and per-frame callback budget are useful concepts, but should be configurable byte/time budgets rather than copied constants.

## Confidence

- **Confirmed:** RTTI thread family, async loader configuration/defaults, worker open/result behavior, 8-MiB backpressure, main-thread callback drain/cancellation, and absence of evidence for a generic pool.
- **Strongly inferred:** GameScheduler is frame-owned from its compact deadline operations and lack of thread object/wakeup machinery.
- **Unknown/non-blocking:** original thread names/priorities at runtime and precise lock implementation.

## Evidence

- `thread_strings.txt`
- `thread_vtables.tsv`
- `decompiled-thread-scheduler/`
- `ANALYSIS_CHECKPOINT_2026-09-20_FILE_STREAMER_THREAD.md`
- `ANALYSIS_CHECKPOINT_2026-09-20_NETWORK_THREAD_AFFINITY.md`
- `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_WORK_SCHEDULING_AND_SEAMS.md`
