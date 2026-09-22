# File-streamer completion and cancellation ownership

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The legacy `FileStreamerThread` is asynchronous internally but its only public read wrapper is synchronous to the caller. A normal read request cannot outlive the caller's stack frame, buffer, result slot, or file-wrapper borrow. There is no exposed request handle and no per-request cancellation path.

## Sole public request producer

Cross-reference closure shows only two producers of streamer requests:

- `0x00A9ADC0`: ordinary read wrapper;
- `0x00A9BDC0`: service shutdown request.

`file_streamer_read_wait_VA_00A9ADC0` chooses direct synchronous seek/read when asynchronous streaming is disabled. When enabled it:

1. allocates one 40-byte pooled request;
2. records operation `2`, borrowed file wrapper, caller buffer, file offset, byte count, zero accumulated count, completion primitive, priority class, and a pointer to a stack-local result integer initialized to `-1`;
3. enqueues and signals the worker;
4. waits on the request completion primitive;
5. performs the paired wait cleanup;
6. returns the stack-local result.

The worker or completion helper writes the accumulated byte count, signals completion, and returns the request to its pool before the wrapper returns. Because the result pointer itself addresses the wrapper's stack, any fire-and-forget interpretation would be invalid; the wait is mandatory and confirmed.

## Cancellation and scene-transition behavior

No function removes a queued read by caller identity, scene, file, buffer, or request handle. Once enqueued, a read is serviced to short-read/end/error completion in 128 KiB slices. Higher priority work may run between slices because a full slice is requeued, but the original caller remains blocked.

Consequences:

- scene cleanup cannot leave a file-streamer completion callback targeting a destroyed scene—the public API has no such callback;
- the file wrapper, destination buffer, output slot, and waiting thread must remain alive until completion;
- shutdown assumes ordinary callers have quiesced, then enqueues operation `1` and joins the worker;
- the legacy service has no graceful way to cancel a large read during zone/scene transition;
- a read error completes with the accumulated count rather than transferring ownership to an error callback.

## Replacement architecture

A modern client may expose true cancellable futures, but that is a redesign. Required invariants are:

- an owned request keeps file/archive and destination storage valid;
- cancellation reaches a terminal state and wakes every waiter;
- scene/session generation tokens prevent stale publication after transitions;
- shutdown stops admission, cancels or drains queued work, joins workers, and only then destroys VFS/archive state;
- priority fairness retains the observable advantage of slicing large reads.

There is no need to emulate the legacy caller-blocking API above the compatibility asset layer.

## Confidence

- Sole normal/shutdown producers: **Confirmed by complete xrefs to allocation, construction, and enqueue functions**.
- Stack-local result pointer and mandatory wait: **Confirmed by `0x00A9ADC0` control flow**.
- Absence of per-request cancellation: **Confirmed for the complete streamer function family and caller set**.
- Global shutdown ordering: **Confirmed; ordinary-caller quiescence is an architectural precondition rather than an explicit queue-cancel pass**.

