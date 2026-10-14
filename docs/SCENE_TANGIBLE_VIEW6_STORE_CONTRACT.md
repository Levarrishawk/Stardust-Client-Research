# Scene tangible view 6 publication store

Round31, 2026-10-03. This is a bounded modern implementation policy over the accepted [TANO6 state contract](TANGIBLE_VIEW6_STATE_CONTRACT.md) and [state policy](TANGIBLE_VIEW6_STATE_POLICY.md). The standalone store introduces no recovered protocol, executable-address claim or callback delivery. Round32 adds an internal preparation seam for the separately owned optional world adapter.

Implementation: [SceneTangibleView6ReplicationStore](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneTangibleView6ReplicationStore.cs). Verification: [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneTangibleView6StoreScenarios.cs), [isolated runner](../../SWG-Godot-Client/poc/round31-tests/Runtime/Program.cs), [synthetic wire fixtures](../../SWG-Godot-Client/poc/round31-tests/Runtime/ordered-fixtures.json), [independent verifier](../../SWG-Godot-Client/poc/round31-tests/Runtime/verify.py).

## Ownership and currentness

The creating managed thread owns every store operation. A gate rejects other threads and reentry before mutation. The registry is borrowed, must be serialized by the host, and is never closed by this store. Commit recipient rechecks do not establish concurrent registry safety. Reentry rejection is inspected; the API has no callback to dynamically induce same-thread reentry.

`Seed(handle, baseline)` and `Apply(handle, exactExpectedSnapshot, delta)` require the exact currently valid registry handle. Stale handles return null before payload access. Apply additionally requires the exact stored snapshot reference from this store and the current registry incarnation. Foreign, superseded and recreated-recipient snapshots cannot overwrite state. Payload object ID mismatch throws.

A registry handle revision change within the same incarnation preserves state. `Find(newCurrentHandle)` may return the historical stored snapshot whose handle is old; `IsCurrent(thatSnapshot)` is false. `Apply(newCurrentHandle, thatExactStoredSnapshot, delta)` carries the same-incarnation state forward and publishes a current handle. Destruction/recreation with the same object ID rejects the old incarnation. A new-incarnation seed starts state revision at one; the global store revision continues. Same-incarnation reseeding continues the state revision.

Successful publication advances a checked global revision and produces an immutable snapshot containing owned state, recipient handle and incarnation. The accepted state kernel preserves U64 ID bits, ordering, counter wrap, skip behavior and ordered event semantics. Internal store layout is modern policy.

## Atomic publication and retention

Before changing store fields, publication constructs the new snapshot, replacement map and complete journal, checks accounting and epoch overflow, then rechecks the recipient. Only assignments follow that final gate. State becomes available before events can be observed after the owner gate releases. No callbacks run here.

Rejected writes preserve state, revision, retained charges, valid earlier journal records and the issued batch. Writes do not opportunistically retire stale other entries: those entries continue to consume object and state capacity until `Find`, `IsCurrent`, `RetainedStateBytes` or `DrainEvents` performs retirement. The native suite checks capacity rejection followed by retirement and successful seed. This deliberate policy avoids mutation as a side effect of a failed write.

Retirement removes stale incarnations, preserves same-incarnation handle changes, invalidates an issued batch by epoch, and does not advance the global publication revision. It preserves historical journal descriptions. `Revision` and `PendingEventCount` do not retire entries. Close is idempotent, clears owned state and journal, preserves the revision, and leaves the borrowed registry usable. Closed writes and drain throw; find/currentness return empty/false and acknowledgment returns false.

## Journal ownership versus delivery

Journal records contain handle, incarnation, publication revision and immutable event metadata; they do not retain historical state graphs. They describe successful past publications even if a recipient has since been destroyed, recreated or retired. `DrainEvents` is observation, not live-target resolution or delivery permission. Before eventually delivering to a target, a host must check `registry.IsCurrent(record.Incarnation)` and apply its own delivery policy. No weak resolver or callback dispatcher is provided.

Drain returns the same immutable issued batch until publication, retirement or acknowledgment changes the journal epoch. `Acknowledge(batch)` accepts only this store's exact issued reference with matching publication revision and epoch. Success clears the whole journal once. Repeated, stale, foreign and closed acknowledgments fail. Even a successful zero-event seed/apply invalidates an older batch. Acknowledgment transfers journal ownership only; it does not authorize recipient delivery. Native tests retain and inspect historical records after recipient recreation.

## Bounded accounting

Defaults: 64 objects, 1 MiB total retained state, 4096 pending events, 4096 events per apply, 2 MiB logical output and 65536 kernel work units. Constructor ceilings are respectively 4096 objects, 128 MiB state, 262144 pending/per-apply events, 256 MiB output and 16777216 kernel work units. Core state limits remain independently validated.

State charges are the sum of current mapped states' `DecodedBytes` (`8 + 8 * ID count`). Pending events charge 24 logical bytes each. Effective kernel state/output limits subtract other mapped states and existing journal charges; event capacity subtracts existing pending records. Each update is checked against the remaining global budget, including intermediate kernel limits. These are logical compatibility accounting units, not managed heap measurements; caller-retained old snapshots are outside store retention.

`MaximumWork` narrows the accepted state kernel's per-message work limit. It is **not a whole-store CPU-work cap**. Publication separately copies at most `MaximumObjects` map entries and `MaximumPendingEvents` journal records; drain copies that bounded journal, and retirement scans/copies the bounded object map. There is no lifetime work budget or multi-message transaction. These collection bounds constrain the overhead separately; no numerical kernel-work charge is assigned to dictionary operations or journal copying.

## Evidence and remaining boundary

Confirmed by implementation and executable modern tests: exact-token publication, state/event atomicity, global logical budget rejection, stale/foreign/owner/closed/overflow negatives, historical journals, moved-handle carry-forward, recreation/reset and acknowledgment ownership. Independent accepted30 Python replay confirms 21 sequences, 23 transitions and 38 ordered events. Native suite: 143 checks across those sequences and nine store-specific case groups. These counts are test evidence, not research/function/protocol coverage percentages.

No original executable, assets, live server, packet capture or dynamic legacy execution was used. The optional Round32 world adapter owns recipient validation and historical-batch acknowledgment guards; this standalone store still performs no world dispatch. See the prepared seam below.

## Round32 prepared world seam

The [optional world adapter](SCENE_WORLD_TANGIBLE_VIEW6_CONTRACT.md) now uses internal `PrepareSeed`, `PrepareApply`, `CheckPrepared`, `CommitPrepared` and `ReleasePrepared`. Preparation retains the owner gate and builds the entire map/queue/snapshot/accounting without publication. Only the exact active owned candidate can pass checks, with current handle/incarnation and unconsumed status. The coordinator finishes all world journal capacity/arithmetic/allocation before checking and committing. Commit contains assignments only; release drops the active preparation and gate. These methods are internal trusted serialized composition seams, not public caller APIs. No callbacks may intervene between check and commit.

Public Seed/Apply delegate through that seam and preserve all accepted31 behavior. Accepted31 native143 checks and independent23-transition/38-event oracle pass after extension. World tokens intentionally use current incarnation plus exact stored state identity, so handle revisions may preserve world currentness while the historical store snapshot's own IsCurrent remains false. Event guards and generation journal retirement belong to the optional world adapter; the standalone store still has no dispatch/world ownership.
