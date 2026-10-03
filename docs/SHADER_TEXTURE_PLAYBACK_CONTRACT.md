# Standalone clock-to-cursor playback

Round30 Runtime, 2026-10-03. [ShaderTexturePlayback](../poc/Stardust.Core/Runtime/ShaderTexturePlayback.cs) owns an accepted [shader object clock](SHADER_OBJECT_CLOCK_CONTRACT.md) and one accepted [delta-time cursor](DELTA_TIME_TEXTURE_CURSOR_CONTRACT.md) per registered object. It delivers distinct clock-service receipts once. It has no callbacks, I/O, wall clock, scene/world/Godot integration or original precision/first-visible parity claim.

## API and ownership

```csharp
ShaderTexturePlayback(int capacity = 64, ulong maximumTicksPerAdvance = 1000000,
    ulong tickSeed = 0, ulong clockRevisionSeed = 0,
    ulong registrationTokenSeed = 0, ulong revisionSeed = 0)
ShaderTexturePlaybackSnapshot Register(ulong id, int frameCount, float interval,
    int maximumCatchUpSteps = 4096, ulong cursorRevisionSeed = 0,
    ulong cumulativeAdvancesSeed = 0)
ShaderObjectClockSnapshot Advance(float seconds)
ShaderTexturePlaybackSnapshot? Service(ulong id)
ShaderTexturePlaybackSnapshot? Find(ulong id)
IReadOnlyList<ShaderTexturePlaybackSnapshot> Snapshots()
bool IsCurrent(ShaderTexturePlaybackSnapshot snapshot)
bool Remove(ulong id)
void Reset()
void Close() // Dispose calls Close
```

Owner-gated properties are ClockSnapshot, Revision, IsClosed and IsFaulted. Constructor and cursor limits remain the accepted finite/positive/bounded policies; IDs are opaque ulong. Interval is in seconds for this adapter's explicit binding, matching the owned clock's service output. Registration seeds last tick to current global tick and cursor index/residual zero. No authored texture-list validation occurs here; a later resource adapter must bound FrameCount against available variants.

Immutable publications expose Clock, Cursor, ObjectId, RegistrationToken and adapter Revision. The Policy string identifies modern clock/cursor delivery with terminal fault on consumed delivery failure; OriginalParity is false. Exact owner/stored publication, exact clock-service current state and exact cursor state determine freshness. Global Advance stales publications without losing per-object last-service/cursor facts. An unrelated object operation at the same tick does not invalidate another object's publication.

## Once-only delivery

Advance changes the global clock only; Service explicitly updates one cursor from accumulated seconds since that object's last clock service. Missing Service returns null. Same-tick Service returns the exact existing publication and does not advance either cursor or adapter/clock revision. Its clock receipt may still contain historical nonzero ElapsedSeconds; that value is not delivered again. Consumers should treat a changed publication identity as a new result, rather than replaying a retained delta.

Remove closes that object's cursor and removes its clock registration; missing removal is unchanged false. Re-registration gets a new checked clock token and zero cursor. Reset clears/closes all registrations/cursors while preserving monotonic clock tick/token counters and incrementing clock/adapter revisions. Manual Close is idempotent, closes all owned resources and invalidates every publication without claiming rollback or increasing adapter revision. Closed writes throw; inspection returns the drained registry and immutable clock diagnostic.

## Failure policy: retention or explicit terminal retirement

Clock and cursor public APIs cannot be prepared together without editing their accepted implementations. This adapter chooses a bounded explicit terminal-fault design, approved for this milestone. It does not pretend sequential consumption is an atomic two-owner rollback.

Adapter revision preflight, invalid cursor construction, clock registration/advance/service preflight and checked clock failures occur before the affected underlying operation commits; these retain exact existing publications and clock state. Temporary unregistered cursors are closed. Map staging is bounded by registration capacity.

After a clock registration/service successfully returns, the adapter marks that operation consumed. If subsequent cursor advancement or publication allocation/staging fails, it sets IsFaulted, closes the owned clock and every cursor, clears the registry and becomes terminally closed. Every former publication becomes stale. Clock tick/clock revision may expose the consumed service; adapter Revision remains the last successful adapter publication. There is no retry or fabricated restored clock receipt after consumption. Recovery requires a new adapter. Native work/overflow failures validate this terminal drain; catastrophic allocation-failure recovery is not separately exercised.

Remove/reset first prebuild their bounded map and preflight adapter revision, then use atomic clock operations followed by callback-free owner-local cursor closes/assignments. Clock/cursor source files are unchanged. All calls require the creating managed thread; no externally reachable same-thread callback exists to exercise reentry dynamically.

## Numeric policy and evidence

The clock retains modern double seconds*1000/truncate/min1 with safe exclusive2^64/config-cap checks, and service double tick-difference*float32 inverse then float32 elapsed storage. Cursor update uses double repeated subtraction and float32 persisted residual, including a residual that can round to its interval. No PC24/FISTP, arbitrary partition bit identity, legacy registration, culling or first-visible behavior is inferred.

[Native scenarios](../poc/Stardust.Core.SmokeTests/SceneShaderTexturePlaybackScenarios.cs) pass147 checks /14 cases. The [independent Python tick/frame/bit reference](../poc/round30-tests/Runtime/verify.py) agrees on all79 operations over [synthetic fixtures](../poc/round30-tests/Runtime/ordered-fixtures.json). Gates cover divergent/skipped services, same-tick historical nonzero receipt identity, zero/minimum tick, residual/wrap, reset/remove/re-register, capacity/invalid input, clock/adapter/token/cursor overflow, owner/closed cleanup, and two consumed-delivery terminal faults. Fault cases assert owned clock registration count zero, empty adapter registry and invalidation of all issued publications. Source inspection confirms each owned cursor receives Close before registry clearing.

Confirmed: chosen modern behavior and deterministic fixture results. Deferred: transactional preparation across accepted owners, engine/resource actor integration and original timing/precision parity. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R30_SHADER_TEXTURE_PLAYBACK.md). Next lead experiment can bind immutable frame selection to a separately owned visual and explicitly handle terminal fault retirement; it must not retry consumed elapsed time.
