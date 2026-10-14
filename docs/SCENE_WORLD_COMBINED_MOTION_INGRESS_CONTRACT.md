# Combined motion application ingress

Round66 provides synchronous modern ingress over the frozen [full-root router](SCENE_WORLD_FULL_TRANSFORM_APPLICATION_CONTRACT.md), [compact router](SCENE_WORLD_MOTION_INGRESS_CONTRACT.md) and existing world fallback. It adds no live transport, queue, protocol grammar, engine behavior or original interpolation claim.

```csharp
using var router = new SceneWorldCombinedMotionApplicationRouter(world, motions,
    compactProjection, SceneCreateZeroQuaternionPolicy.Identity,
    ObjectControllerTransformDirection.ServerToClient);
var receipt = router.Receive(generation, applicationSpan, cancellationToken);
// Actual accepted pump constructor hook:
applicationReceiver: (generation, bytes) => router.Accept(generation, bytes.Span)
```

Compact projection has the accepted `(CompactSceneTransformMessage, SceneWorldObjectSnapshot) -> (AppearanceTransform, float Speed)` signature. Both full zero policy and inbound direction are explicit; constructor rejects every direction except ServerToClient, proves world owner affinity and exact open store binding via IsBoundTo. Router owns only its two wrapper instances and borrows world/store. Accept returns Receive.Accepted; Receive returns a typed route/acceptance/full-result/motion receipt. All operations, including LastResult reads and Dispose, require construction thread and reject reentry.

## Dispatch and failure boundaries

Generation mismatch, failed/closed world or closed/unbound store returns DeclinedIdentity before decoding or effects. Precancellation/disposal throw before decoding. This preflight applies to fallback too. Current valid inputs select:

| Selector | Route |
| --- | --- |
| Exact compact root/parent top-level opcode | Compact child router |
| Exact ObjectController opcode, known root0x71/parent0xF1 inner kind | FullTransform child router |
| ObjectController opcode with fewer than14 bytes | FullTransform decoder, structural error |
| Other opcode or unknown readable ObjectController inner kind | Existing world fallback |

A recognized full malformed operand count, length, extra byte or nonfinite field propagates its existing codec error. There is no catch-and-fallback path. A wrong top-level hash cannot be guessed to be a transform and follows world fallback. Unknown inner kinds follow the current world parser unchanged; that parser currently throws Unknown world application opcode and fails the generation. Fallback is not a silent unknown-message drop. Parent packets retain their accepted explicit decline behavior. Ordinary staged StartScene/lifecycle/baseline/delta/control applications continue through the world.

The router copies no incoming application buffer. It reads only small dispatch fields and uses existing exact codecs. Compact scalar identity is decoded before the caller projector can alter source bytes; after child publication it never redecodes potentially callback-mutated input for diagnostics. The child still owns its canonical snapshot/projection freshness checks. Full results preserve their raw immutable packet and actual prior/current motion identities.

The wrapper around compactProjection checks the combined caught-reentry latch before returning to the child and therefore before PublishAuthoritative. Nested combined Accept, Receive, LastResult or Dispose fails and latches reentry even when the callback catches the exception. Child guards remain in place. Cancellation is checked after projection before returning; it does not roll back caller effects. Caller callbacks can mutate external state directly: tests explicitly apply a world Height update then catch nested combined ingress; the outer throws before adding motion, but the callback's world prefix already occurred. There is no rollback promise.

World fallback can itself invoke caller-configured world decoder callbacks. A caught nested combined call is detected after world.Accept returns; world effects may already have occurred before that failure. This fallback callback prefix defense is source-inspected, not injected by these fixtures. This differs from the compact wrapper, which can check before the child receives its projected values. Router errors leave LastResult at the last completed receipt.

## Receipts and ownership

SceneWorldCombinedMotionRoute is World, Compact, FullTransform or DeclinedIdentity. FullResult is the actual completed full child result, including raw packet, derived placement and prior/current issued motion. Compact successful receipt Motion is the actual store-issued record for the decoded target; compact declines omit motion lookup rather than pruning unrelated state. World/identity receipts carry no motion. Null Motion is not a store-clear claim. LastResult holds only one completed historical receipt. Its motion may cease to be current after later packets, containment, destruction, world reset or close; use store.IsCurrent when currentness matters.

Dispose releases wrappers, projection callback closure, last receipt and borrowed references. It never clears/disposes/closes world/store. Router exceptions are passed to the accepted pump, whose failure path owns closing the world, clearing queued owned arrays/suffix, canceling/observing jobs and retiring presentations. An earlier accepted motion is a completed prefix; after world close it remains a usable immutable historical value, not current motion. Router disposal is separate from pump/world ownership.

The existing pump has queued-count, total queued-byte, per-message-byte and per-step message-count limits; it has no separate per-step byte-budget field. This router does not invent one. Actual QueueApplication copies caller bytes and Step clears its owned arrays after consumption; the router retains no byte buffer.

## Validation

Own replay (external historical reference) constructs independent Python compact/full bytes before native execution, reuses frozen round15 synthetic world lifecycle bytes, and calculates representative asymmetric quaternion basis through math.hypot and cross products. It checks eight mixed route receipts, exact counter/revision ordering, prior compact pose on rejected full duplicate, native XYZ/matrix at2e-7 tolerance and three pump count-budget steps. No expected value is seeded from native output.

Native fixtures exercise actual QueueApplication copied input, staged StartScene/create/baseline/end traffic, current typed Height delta fallback, full/compact parent declines, FFFFFFFF-to-zero wrap, equal/older rejection, historical receipt identity, source mutation during projection, precancellation, foreign thread/world, wrong direction, disposed/closed store and count/byte/per-message queue caps. Six pump failure contexts cover malformed recognized full, unknown-inner fallback, thrown projector, caught nested Accept/Dispose and direct callback world mutation. Each closes world, clears queued suffix, retires current motion and cancels/observes two controlled jobs. Direct malformed cases preserve prior motion and last completed receipt.

All traffic is synthetic. Catalog/bank profile JSON is read, but original assets are not loaded: controlled resource tasks never publish a resource, so no presentation actors are instantiated. Real actor/point-controller cleanup remains the accepted pump/host responsibility and is not newly tested in this worker runner. Capacity/overflow and other inherited store/pump defenses retain their prior evidence; no original executable/server contact occurs.

Confidence: Confirmed offline dispatch/pump/ownership behavior for tested branches; explicit modern projection/serial/zero choices remain separate from original parity. Next action: use the bool hook in the lead's bounded host and verify actual grounded point-controller/presentation cleanup alongside typed full results.
