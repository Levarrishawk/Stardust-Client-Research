# Lifecycle-bound creature scalar store

R8-RUNTIME-01, 2026-10-02. [SceneCreatureScalarStore](../poc/Stardust.Core/Session/SceneCreatureScalarStore.cs) binds the accepted [six-scalar delta plan](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md) to an accepted [scene registry](SCENE_OBJECT_REGISTRY_CONTRACT.md). It has no baseline decoder, resource loader, engine object, presentation callback, or placement conversion.

## Evidence and policy

Confirmed inherited byte boundary: CREO:3 indices 11..16 are U8 posture, U8 rank, U64 link id, F32 height, U32 shock raw bits, and U64 states. The accepted codec validates numeric tag 0x4352454F/view 3, the APPLICATION envelope header of 23 bytes/minimum 25, finite height, and the entire ordered update list before returning an immutable plan. Declared count does not control iteration. Existing client-backed and server correlation evidence is cited in the delta contract; this store recovers no new executable behavior.

Confirmed by native fixtures: exact registry handle and store ownership checks prevent stale delivery from changing newly seeded same-ID objects. The binding, explicit seed, retirement, duplicate seed rejection, capacity and local revision rules are **modern policy**. A caller seed is not a recovered CREO baseline and does not prove resource or scene readiness.

The conservative binding is the exact registry revision, including registry GUID, caller scene generation, object ID, and revision. Containment changes and first end-baselines invalidate it as well as replacement, destroy, reset, detached-child changes, and registry close. An identical containment or already-finalized end-baselines operation leaves the handle current. After invalidation, the caller must supply a new explicit seed. The store never silently carries scalar values forward or fabricates defaults. Future preservation across such transitions requires a separately specified stable object incarnation identity.

## API

Construct with one SceneObjectRegistry, capacity 1..4096 (default 64), and an optional U64 local revision seed (default zero, for deterministic exhaustion tests). All registry mutations and store operations must run on one serialized owner thread. The store enforces its construction thread and rejects reentrant public calls. It offers no callback or borrowed enumerator surface; reentrancy rejection is verified by source inspection, not a claimed callback execution test. It does not add synchronization to direct registry calls.

- Seed(handle, initialState): requires a current registry handle, finite height, available capacity, and no existing seed for that exact current object. PendingBaselines and Finalized handles are both accepted. This deliberately permits testing end-baselines invalidation and imposes no original baseline-arrival rule. Copies the immutable state record, increments the checked monotonic local revision, and returns an owned snapshot.
- Apply(handle, plan): requires a current caller handle, matching plan ObjectId, and an explicit seed in this store for that exact handle. The wire delta supplies no extra generation. The caller token is mandatory on every application. Applies the fully decoded plan in received order, then advances the checked local revision and atomically publishes one new immutable snapshot. Duplicate fields are last-wins. A successful empty plan also advances the local revision, even though its immutable scalar state may be shared.
- Find(handle): returns this store's current snapshot for that exact handle or null. Snapshots returns an owned read-only array sorted by unsigned object ID.
- IsCurrent(snapshot): checks private store ownership, registry freshness, matching stored handle, and latest local scalar revision. A retained snapshot is an immutable observation; another apply makes it noncurrent even if its registry handle remains current. Another store seeded against the same registry cannot accept its publication.
- Close/Dispose: clears entries and permanently rejects Seed/Apply with ObjectDisposedException. Close is idempotent and requires no counter increment, even at U64 exhaustion. Find returns null, IsCurrent false, and Snapshots empty after close. Closing the store never mutates its registry.

Stale or foreign handles return null from Seed/Apply before validating state/plan. Wrong object plans throw InvalidDataException; unseeded current handles and duplicate current seeds throw InvalidOperationException. Nonfinite initial height and exhausted capacity throw InvalidDataException. Counter exhaustion throws OverflowException. These failures do not change a current scalar entry or consume a local revision. Invalid constructor bounds throw ArgumentOutOfRangeException; current null inputs throw ArgumentNullException.

Every store operation retires entries whose handles are no longer current before inspecting or mutating entries. Retirement is lazy: this component subscribes to no registry events. At most capacity entries are retained, and a subsequent valid seed can reuse retired capacity. Retained immutable observations cannot resurrect entries. Retirement does not consume or rewind the local revision; failed request validation may still retire unrelated stale entries as this documented housekeeping policy.

## Ownership and scalar limits

The store retains only immutable snapshot/state records, never the input message, update list, or plan. Plan decoding must precede Apply; unsupported indices, malformed tails, wrong tag/view/kind and nonfinite wire heights therefore fail before any store application. No borrowed mutable arrays escape. Finite height is retained without clamping, normalization, scale application, or posture interpretation, including negative values and negative zero. Source create placement remains separate; no parent/height/presentation assumptions are implemented. No link reference ownership or states-bit semantics are inferred.

## Validation and reproduction

[Native scenarios](../poc/Stardust.Core.SmokeTests/SceneCreatureScalarStoreScenarios.cs) use independently hand-packed synthetic create/contain/end/destroy APPLICATION messages and an eight-update six-field delta. They test exact bits/order after actual create and after end-baselines/reseed, foreign registry/store, wrong object, absent seed, unsupported/malformed decode-before-apply, immutable snapshots, containment/end/replace/destroy/reset/close retirement, same-ID recreation, capacity reuse/rollback, finite extreme height, nonfinite rejection, checked seed/apply overflow, and a dedicated foreign thread. Synthetic fixtures are not captures or original assets.

From the repository, using Python, dotnet 8, a writable temporary APPDATA, and the existing offline package cache:

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r8-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
python poc/round8-tests/Runtime/Generate-Peer.py local-private/round8/peer.json
dotnet restore poc/round8-tests/Runtime/Runtime.csproj --configfile poc/round8-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet build poc/round8-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet poc/round8-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round8/peer.json local-private/round8/export.json
python poc/round8-tests/Runtime/Verify-Store.py local-private/round8/peer.json local-private/round8/export.json
```

Create the ignored output directory if not already present. The independent oracle reuses accepted envelope/scalar and lifecycle parsers, computes ordered raw-bit state, compares native re-encoded bytes and initial/finalized state exports, checks handle/revision metadata, and verifies source placement remains separate. It is a byte/state audit; registry rejection behavior is exercised by the native scenarios, not inferred from exported strings. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R8_SCALAR_STORE.md) records input identity, scope, and final gates. Lead registration may generate peer fixtures and call SceneCreatureScalarStoreScenarios.Run(peer, optionalExportPath); no shared runner edit is included.
