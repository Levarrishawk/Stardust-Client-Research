# Modern terrain GPU upload staging

This is a modern Godot adapter policy, not recovered original client scheduling or LOD. The CPU ownership contract remains [terrain residency](TERRAIN_REGION_RESIDENCY_CONTRACT.md); sampling and movement remain engine-neutral.

## Publication and ownership

`OfflineTerrainStreaming.cs` borrows one complete prepared CPU region through `TryGetPrepared`. It creates only entering patch visuals, hides them, and verifies their GPU readback. Overlapping visuals retain reference identity. The previous complete region remains visible and supports movement until every candidate visual has passed admission and readback. The owner thread then validates movement continuity, commits the same latest generation through `CommitPrepared`, and switches the visible window synchronously.

Evicted visuals become hidden and enter a retirement queue. Retirement finishes before another CPU request starts. A superseded or reset candidate is hidden and disposed under the normal frame budget; borrowed CPU references are cleared before `ReleasePrepared` can admit queued generation. Teardown cancels pending intent before releasing the candidate, drains GPU ownership without the frame budget, then awaits the CPU coordinator. Exceptions are observed and the previous published window is retained when GPU admission fails. Exceptional failure cleanup also drains without the normal budget; its telemetry explicitly records forced cleanup.

The initial CPU region and its chunk receipts are adopted as generation zero, avoiding a duplicate initial upload. Default windows remain 4×4 chunks: 160×160m, sixteen CPU chunks and 256 patch visuals. Explicit `--terrain-window-size 8` selects 8×8 full-resolution chunks: 320×320m, sixty-four CPU chunks and 1,024 patch visuals. Both use 40m chunks and the same sampling spacing; neither adds terrain LOD. Current plus candidate retention is bounded to 32 or 128 CPU chunks and 512 or 2,048 GPU patch visuals respectively. Hidden retirement blocks another successor so it cannot create a third cohort.

## Budget and telemetry

Normal stage creation/readback, aborted-candidate disposal, and retirement admit at most eight indivisible patch operations per owner frame. The first operation is allowed; subsequent operations require elapsed owner time below 2ms. A single upload, verification or disposal can exceed 2ms. Commit work and proof serialization can also extend total owner time. Therefore this is a **soft admission deadline**, not a 2ms wall-time or whole-frame guarantee.

Each frame records operation start/end times, created/verified/disposed counts, current/prepared chunk counts, worker/stage/retirement ownership, live GPU visuals, owner time, and the preceding frame's operation/generation. `UncappedDelta` is the engine delta before movement clamps it to 100ms. New motion receipts also record monotonic `WallFrameDelta` separately; engine delta is not proof of physical frame latency. Publication receipts record `UploadFrames` and `RetiredVisuals`; `DisposedVisuals` is finalized after retirement. Final normal proof capture waits for no candidate, retirement or worker.

Reset uses the original starting window, releases stale staged ownership before preparation, and calls the Core reset operation only after that complete window is ready. The dedicated `cancel` check explicitly requests a bounded adjacent CPU window after initial adoption. `cancel-stage` waits for at least four hidden candidate visuals, verifies the old visible window remains current, then closes and verifies worker drain plus complete GPU cleanup.

## Independent acceptance

Run `tools/python/verify_terrain_stream_upload_budget.py --proof <terrain-proof.json-or-stream-check.json> --log <host-log> --require-bounds`. It checks per-frame count limits, soft admission start times, CPU/GPU bounds, generation/publication accounting, completed retirement, staged cancellation, and final allocation/disposal equality. Older receipts without CPU ownership fields may be checked without `--require-bounds`; that does not establish the missing ownership telemetry. Synthetic positive/negative fixtures run with `python tools/python/test_verify_terrain_stream_upload_budget.py`.

The first actual staging-only run failed its existing movement proof: 256 duplicate initial uploads occupied 254 upload frames and no useful successor arrived during movement. After shared immutable material bindings were added, the 4×4 actual DX12 proof passed with 3,184 independently grounded positions, 101.477m of travel, and five publications. It recorded at most two new visuals per frame, eight disposals per frame, 512 live visuals, and final cleanup of all 768 allocated visuals and one material binding. The maximum successor engine delta was 6.25ms; maximum owner callback was 12.02ms. Other uncapped engine deltas reached 140.13ms, so this does not establish uninterrupted whole-frame pacing. The result measures staging together with shared material bindings. The later adoption/64-chunk profile and per-frame CPU ownership instrumentation require their own actual receipts; these earlier measurements must not be attributed to them.

The subsequent adopted8x8 self-contained export passes15 acceptance checks, including3178 movement positions over101.490m, four successors each reusing56 chunks and preparing8, and1024 upload/retirement operations. All1536 visuals and one shared material binding are disposed. Independent ownership checks also pass rollback, CPU cancellation, staged cancellation and evicted-spawn reset; reset peaks at1856 visuals within2048. Monotonic post-publication intervals are10.75–21.19ms, while startup/capture intervals reach629.79ms. These are explicit64-capacity receipts, still not uninterrupted frame-pacing acceptance.


## Water, far terrain and returned-target cancellation

Optional local water adds up to four clipped visuals per published window and
an explicit current-plus-candidate limit of eight. Optional far terrain adds
one stitched640m mesh per window, with a limit of two. Both candidates remain
hidden until the fine window is ready and are prepared before CPU publication;
preparation failure disposes candidates and preserves the previous visible
terrain, water and far view. Separate water/far rollback checks inject failure
after allocating their hidden candidate. Fine patch counters retain their
original meaning. The additional bounded mesh preparation, proof serialization
and commit work is included in owner elapsed time but is outside the eight
fine-patch-operation admission count.

Far generation uses the same single coordinator worker. An exact coarse-request
cache reuses unchanged16-chunk coarse input; stale cache ownership is cleared
before building a different coarse request. Current and candidate each retain
at most64 fine plus16 coarse receipts, an explicit total limit160. A canceled
unpublished cache may remain until reuse, replacement or worker drain, within
that coarse ownership bound. The cache is cleared after coordinator drain.
The engine-neutral fine surface alone remains the movement/camera authority.

When intent leaves an exposed candidate and returns before its abort completes,
releasing that candidate records cancellation while retaining its request as
the desired key. The host now retries that same key once idle when it differs
from the published window and the last outcome was cancellation. Failed
preparation is not retried indefinitely. The actual DX12 `retarget-stage` gate
stages16 hidden visuals, aborts generation1, returns to its target, and requires
generation2 to publish while generation0 stays visible during cancellation.
All1168 fine visuals, two water visuals and two far visuals are disposed.
Independent normal-proof accounting must include canceled stage generations;
it may not assume every created visual belongs to a published generation.
