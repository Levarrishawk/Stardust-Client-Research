# Asynchronous loader cancellation and shutdown

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The general asynchronous asset loader has an explicit main-thread publication boundary, identity-based cancellation, deterministic resource cleanup, backpressure, and a sentinel-driven shutdown drain. Its cancellation model is callback suppression rather than interruption of in-flight file I/O.

## Request identity and cancellation

Each 16-byte request contains a resource/path batch, callback pointer, callback context, and a reusable result-vector pointer. `async_loader_cancel_callback_VA_00A965B0(callback, context)` locks the shared state and scans both:

- the pending worker queue;
- the completed/main-thread queue.

For every exact callback/context match it atomically-with-respect-to-the-queues sets both fields to zero. It does not remove or free the request while the worker may own it.

The worker may still open/decode resources for a request cancelled after dequeue. Publication remains safe because the main-thread drain tests the callback pointer before invocation. Regardless of that test, the drain releases every opened file/data object, runs registered type-specific cleanup, recycles the result vector, decrements outstanding-byte accounting, and frees the request.

Cancellation therefore guarantees **no future callback for the matching identity after cancellation acquires/releases the queue lock**; it does not guarantee that disk work stops immediately.

## Main-thread publication

`async_loader_drain_completions_VA_00A96BA0` is the sole callback invocation site. It removes a completed request under lock, releases the lock, performs result handoff/callback on the frame owner, then cleans all residual resources. The configured callbacks-per-frame limit bounds publication latency and per-frame work. Cancelled requests consume cleanup work but no client callback.

This is the appropriate boundary for scene/session generation validation in a replacement.

## Shutdown

The removal routine at `0x00A962A0`:

1. allocates a zeroed request whose resource-batch pointer is null;
2. enqueues it as a worker sentinel;
3. repeatedly waits briefly and drains completed requests until both pending and completed queues are empty;
4. joins the asynchronous-loader thread;
5. frees the shared name table, loader registry, reusable result vectors, and request pool.

The worker loop terminates after transferring the null-batch sentinel to the completion queue. The removal loop still drains that result before joining/freeing state. Thus no queued result vector or opened file is abandoned at normal shutdown.

## Replacement contract

- Represent callback identity with a lifetime-safe token rather than raw `(function, void*)` pairs.
- Cancellation should mark pending/in-flight/completed work terminal and suppress publication; cooperative I/O cancellation may improve latency but is not required for behavioral parity.
- Always clean decoded resources even when publication is cancelled.
- Publish only on the scene/session owner and compare a generation token immediately before commit.
- Shutdown must stop admission, enqueue/announce termination, drain or cancel every result, join workers, then destroy loaders and VFS state.

## Confidence

- Pending/completed queue cancellation and exact identity match: **Confirmed**.
- Callback-only main-thread publication and unconditional cleanup: **Confirmed**.
- Sentinel, drain-until-empty, join, then registry/pool destruction: **Confirmed by raw removal control flow**.
- Legacy cancellation promise beyond callback suppression: **None; interruption is not implemented**.

