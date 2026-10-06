# Round73 root-motion cadence — 2026-10-06

Question: can modern root scheduling preserve exact current LOCAL provenance, bounded cadence and honest queue backpressure without byte history or catch-up bursts? Stop after one1000-tick trace and bounded ownership/negative cases.

Input accepted72 frozen4371 public files; manifest138f9b7dcbbc43bf7be0a564beb5ffd0771747307f1c72ee108d46a0fef742a5, archive5848eb383951c9a452a3ddf013c23e30132ed8876579101273f277b1d7f302ea. No executable input/VA claims. Tools cached Python independent clock/binary32/struct/frozen peer model; offline .NET8 actual terrain driver/FIFO/bridge/transport; validator, diff/AST/XML/hash gates.

Observation: frozen store exposes internal exact binding/current issued row; FIFO exposes Count/QueuedBytes but no capacity/binding introspection. Existing root builder preserves U32 fields and native-to-wire mapping. New scheduler checks issued LOCAL provenance and caller capacity contract, then owns observation frontier and last successful sample only. Exact API/policy is in [durable contract](../../docs/SCENE_WORLD_ROOT_MOTION_CADENCE_OUTBOX_CONTRACT.md).

Findings:2033 checks pass;1000 actual ordinary driver updates,54 independently expected62-byte frames, six backpressure observations, all1000 status values and one actual bridge-protected frame/ACK. Counter/timestamp wrap tested. Immediate stopped sample retries once after full FIFO drains; stationary initial/heartbeat tested. Actual mismatched-capacity enqueue exception preserves one borrowed frame and committed observation frontier, terminally faults own scheduler without successful counter/time advance. Dispose preserves queue/store/current motion and transferred arrays/history.

Confidence: Confirmed tested modern policy and offline byte boundary. Finite proper-rigid issued source inherited from store; private zeroing and rare mid-Enqueue cancellation source-inspected, not dynamically injected. Original cadence/units/prediction/live legality Unknown. Compatibility consequence: host calls once per exact configured step, uses relative elapsed time, treats BP as consumed observation and serializes actual owners; no intermediate replay/rollback.

Validation final native zero warnings/errors; independent expectations before native; workspace zero broken/private links, public frozen scope/HEAD/index preserved. Initial compile retained privately exposed unused fixture helper; removed it. No prior source modifications, sibling imports, original assets, socket/send, capture or EXE execution. Root metrics/ledgers remain lead-owned; no registry/coverage denominator change.

Next: lead1000-step host join independently schedules and ACKs queued roots, explicitly closes borrowed owners. Worker seals handoff and idles.
