# Standalone shader object clock

Round29 Runtime, 2026-10-03. [ShaderObjectClock](../poc/Stardust.Core/Runtime/ShaderObjectClock.cs) implements an explicit modern safe shape correlated with the accepted [ordinary scheduler clock evidence](SWITCH_TEXTURE_CLOCK_CONTRACT.md): seconds input, truncating thousandths with minimum-one advancement, and per-object accumulated service time. Original PC24 multiplication/FISTP/overflow parity and first-registration policy are not claimed. There is no scene, world, cursor, Godot, wall clock, timer or callback integration.

## API and state

Namespace `Stardust.Core.Runtime`:

```csharp
ShaderObjectClock(int capacity = 64, ulong maximumTicksPerAdvance = 1000000,
    ulong tickSeed = 0, ulong revisionSeed = 0, ulong registrationTokenSeed = 0)
ShaderObjectClockSnapshot Advance(float seconds)
ShaderObjectClockObjectSnapshot Register(ulong id)
ShaderObjectClockObjectSnapshot? Service(ulong id)
ShaderObjectClockObjectSnapshot? Find(ulong id)
bool Remove(ulong id)
ShaderObjectClockSnapshot Reset()
ShaderObjectClockSnapshot Snapshot { get; }
IReadOnlyList<ShaderObjectClockObjectSnapshot> Snapshots()
bool IsCurrent(ShaderObjectClockSnapshot snapshot)
bool IsCurrent(ShaderObjectClockObjectSnapshot snapshot)
bool IsClosed { get; }
void Close() // Dispose calls Close
```

Capacity is1..4096, maximum ticks per advance is any positive ulong, and IDs are arbitrary ulong including zero. Duplicate registration or exhausted capacity rejects. Missing Service/Find returns null and missing Remove returns false. Initial global tick/revision/token seeds are explicit test/host policy, not legacy counter semantics. Register seeds last-serviced tick to current global tick and issues zero elapsed seconds. That first-registration choice is modern; original initialization remains unknown.

Global immutable receipts expose Tick, Revision, RegisteredCount, Capacity, MaximumTicksPerAdvance, NumericPolicy (`binary64-multiply-truncate-min1`), InputUnits (`seconds`) and OriginalPC24Parity (`false`). Object receipts expose ObjectId, RegistrationToken, FromTick, ThroughTick/LastServicedTick, ElapsedSeconds, Revision, InverseScaleBits (`3A83126F`), NumericPolicy (`binary64-difference-times-float32-inverse-float32-storage`) and OriginalPC24Parity (`false`). Issued values never mutate.

## Tick and service arithmetic

Advance accepts finite nonnegative float32 seconds; signed negative zero is accepted. Promote to double, multiply by binary64 1000, truncate toward zero, and use one tick when truncated result is zero. There is no fractional carry. Before cast, require finite product and product strictly below the exclusive double value2^64. Do not compare against `(double)ulong.MaxValue`, which rounds to2^64 and cannot safely admit that endpoint. Also preflight the configured cap before casting: the constructor derives the largest binary64 integer not above that ulong cap, including caps rounded upward by conversion. Compare the truncated/minimum-one value against that safe bound, then cast and check tick/revision sums before allocating and assigning the new global receipt.

A finite float32 input can still exceed the conversion range or configured cap and reject. Arithmetic is O(1), not a loop over tick count. Service converts the unsigned tick difference to double, multiplies by the exact float32 inverse1000 promoted to double, and persists the result as float32. That scale is approximately0.0010000000474974513. Each object receives the accumulation since its own last actual service, which can span skipped frames.

Modern numerical examples: float32 0.005 times1000 in double is slightly below5 and truncates to4 ticks. Two float320.0015 advances yield2 ticks, while one float320.003 advance yields3; no partition/truncation identity is promised. These results are verified fixtures of the chosen policy, not original PC24 output.

## Receipt freshness and no new delivery

A global receipt is current only when it is the exact latest receipt of this open owner. Any successful Advance/Register/changed Service/Remove/Reset supersedes it. Object freshness is independent of unrelated same-tick revisions: the exact stored receipt of this owner is current only while its ThroughTick equals the global tick. Advance therefore makes every previous object receipt stale without removing its immutable last-service facts. Find and Snapshots may return those historical receipts; Service refreshes a present object after time advances.

Same-tick Service returns the **exact existing receipt**, changes no state/revision and represents **no new delivery**. Its ElapsedSeconds remains the last delivered value, possibly nonzero; it is not a new zero-duration event. Consumers must track receipt identity rather than replaying that old elapsed value:

```csharp
var previous = clock.Find(id);
var serviced = clock.Service(id);
bool newDelivery = serviced is not null && !ReferenceEquals(previous, serviced);
```

Removal invalidates that registration; a subsequent same-ID registration has a checked new token and cannot revive old receipts. Reset clears registrations and increments revision, while preserving monotonic global tick and registration-token counter. Close releases registrations, retains tick/revision/token counters, creates an empty terminal global receipt and invalidates all current tokens. Close is idempotent and does not increment revision. Closed writes throw; state inspection remains available.

## Atomicity and ownership

All changed-operation arithmetic, checked revision/token increments, dictionary staging and receipt allocation precede assignments. Invalid input, excessive conversion/cap, duplicate/capacity and overflow failures preserve exact prior global/object references and last-service facts. Same-tick/missing operations do not consume revision. Global advance never mutates object entries. Maps are bounded by capacity; map staging handles at most Capacity registered entries, while tick conversion/difference calculations do not iterate elapsed ticks.

Every operation requires the creating managed thread. There are no callbacks or caller enumerations during operations. The busy guard is inspected, with no externally reachable same-thread reentry to dynamically exercise. External receipt retention does not retain the clock registry after close.

## Evidence and boundaries

[Native scenarios](../poc/Stardust.Core.SmokeTests/ShaderObjectClockScenarios.cs) pass390 checks /36 cases. The [independent Python integer/bit reference](../poc/round29-tests/Runtime/verify.py) verifies all157 operations from [synthetic fixtures](../poc/round29-tests/Runtime/ordered-fixtures.json), including zero/submillisecond/neighbor boundaries, divergent last-service times, repeated truncation, exclusive2^64 cast neighbors, cap/counter/token overflow, malformed constructor/input, reset/remove/recreation, closed/owner cleanup and immutable exact tokens. Twenty-two same-tick services are checked as no new delivery. Large legal cast-neighbor input yields18,446,742,802,399,232,000 ticks; the adjacent rounded threshold and above reject before cast.

Confirmed: modern implementation and fixture outcomes. Correlation: ordinary scheduler seconds/quantization/accumulation from accepted static research. Unknown: original first-registration last tick, malformed timing/wrap behavior and exact original numeric parity. No original execution, assets, clocks or live systems are used. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R29_SHADER_OBJECT_CLOCK.md).

Next: a separately bounded actor adapter can explicitly combine clock service with the accepted cursor, delivering only changed service receipts and defining registration/lifecycle policy. Verify selected frames, accumulation and cleanup independently before scene/Godot integration or legacy precision claims.

Round30 [first-tick investigation](SWITCH_TEXTURE_FIRST_TICK_CONTRACT.md) bounds original fresh registration/current-tick seeding and existing-record retained tick. First visible draw remains unresolved. [Owned delivery adapter](SHADER_TEXTURE_PLAYBACK_CONTRACT.md) and [actual modern preview join](SCENE_WORLD_SWITCH_TEXTURE_PRESENTATION_CONTRACT.md) keep numerical/first-visible parity separate.
