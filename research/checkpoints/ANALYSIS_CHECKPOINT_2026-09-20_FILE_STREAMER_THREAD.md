# File-streamer thread and queue ownership

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The asynchronous file path is a bounded specialist service, not a general job system. Installation at `0x00A9BC70` creates one worker named `FileStreamerThread`, binds entry `0x00A9C090`, and sets thread priority 3. Shared state consists of one mutex at `0x0193C230`, one wake primitive at `0x0193C248`, and three intrusive FIFO request queues.

## Request ownership and scheduling

The 40-byte request constructed at `0x00A9C220` contains an intrusive next link, operation, file wrapper, buffer cursor, file offset, remaining byte count, accumulated/result count, completion primitive, priority class, and result-output pointer. `file_streamer_enqueue_VA_00A9BE50` locks the shared mutex, appends to the tail of exactly one queue selected by class 0, 1 or 2, unlocks, then signals the worker.

`file_streamer_worker_loop_VA_00A9C090` waits on the wake primitive, locks, and removes one request in strict order 2 then 1 then 0. Queue head and tail are updated together while locked. It unlocks before performing I/O. Operation 2 is the read path; operation 1 is the shutdown/terminal request path. Unknown operation values are discarded by the loop without being interpreted as reads.

## Read slicing and completion

`file_streamer_process_read_VA_00A9BF30` seeks the wrapped Win32 file to the request offset and reads at most `0x20000` (128 KiB) per service turn. After a full 128 KiB slice it advances buffer, offset, accumulated count and remaining count, then requeues the same request at its original priority. A short read or the final <=128 KiB slice completes the request. The accumulated byte count is written through the caller-provided result pointer.

Win32 read failure is logged with the requested byte count and `GetLastError` through the literal `FileStreamerThread::processRead ReadFile failed to read '%d' bytes with error '%d'`. Completion releases the request and signals its per-request event/condition primitive.

The public path retains a synchronous mode: when the global asynchronous flag is false, it seeks and reads directly on the calling thread rather than enqueueing.

## Shutdown

The exit callback at `0x00A9BDC0` constructs the operation-1 terminal request, associates it with a completion primitive, enqueues it, then waits indefinitely for the worker wrapper to join at `0x00B8D6B0`. Only after join does it clear the installed flag. Queue synchronization objects and request pools have separate registered exit callbacks.

The ordering is therefore:

```text
stop request -> enqueue/signal -> worker removes terminal request
             -> completion signal -> join -> release global service state
```

## Superseding completion-ownership trace

`ANALYSIS_CHECKPOINT_2026-09-21_FILE_STREAMER_COMPLETION_OWNERSHIP.md` closes the public boundary: `0x00A9ADC0` is the sole ordinary producer, passes a stack-local result pointer, enqueues, and waits before returning. No request handle or cancellation API exists. The service is worker-asynchronous but caller-synchronous.

## Replacement requirements

- Preserve priority and completion semantics for observable loading latency, but use safe modern queues/futures.
- Keep file handles/buffers owned until completion or cancellation; never encode native pointer width into asset data.
- Slice or otherwise prevent large reads from starving higher-priority work.
- Stop accepting work, enqueue/cancel outstanding work, join, then destroy VFS/archive owners.
- Do not copy `TerminateThread`-style behavior; this service has an orderly terminal request and join path.

## Confidence

- Worker count, queue ownership, strict priority order, lock/wake roles, 128 KiB slicing, requeue and shutdown join: **Confirmed by executable code**.
- Human-readable names for priority classes: **Unknown; numeric order is confirmed**.
- Whether every archive read uses this service: **Unknown/non-required; synchronous and asynchronous paths both exist**.

## Stable provisional names

| VA | Name |
|---:|---|
| `0x00A9BC70` | `file_streamer_install_VA_00A9BC70` |
| `0x00A9BE50` | `file_streamer_enqueue_VA_00A9BE50` |
| `0x00A9BF30` | `file_streamer_process_read_VA_00A9BF30` |
| `0x00A9C090` | `file_streamer_worker_loop_VA_00A9C090` |
| `0x00A9BDC0` | `file_streamer_shutdown_join_VA_00A9BDC0` |

No DRM, licensing, authentication, or access-control mechanism was examined.
