# Round81 fresh preview admission, 2026-10-06

## Question and stopping condition

Does each configured Version13 preview expose actual fresh owners, reset state and subsequent normal/fault cleanup without inventing a zero camera? Stop after a bounded headless normal/fault replay, independent verifier, negative cases and a primitive schema suitable for lead integration.

## Inputs and method

Accepted80 public baseline: 4,646 files; sync manifest SHA-256 `b0a66693ebdf2e61fae7eb8c314c671f6393bb37c0b014a90b8ab62a61f1bf4d`, archive SHA-256 `b864d128336891a981c4d254e4637f872db71f3058a95683d41b52e0154099fd`. Frozen round79 full Version13 short and terrain-gap host recipes used explicit original archive/config resources. The archive configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. No executable VA claim or original executable execution is involved.

Tools: cached Python, .NET SDK 8.0.300, cached Godot 4.7.2 mono console in headless mode; isolated private project, artifacts and offline package cache. Expected initialization records were generated before each native replay from source-derived policy. The single existing-source edit calls the new partial method at the end of `BeginPointTarget`. See the [durable contract](../../docs/TERRAIN_OFFLINE_FRESH_PREVIEW_CONTRACT.md) and [verifier](../../../SWG-Godot-Client/poc/round81-tests/Runtime/verify_fresh.py).

## Observations and interpretation

- Confirmed: actual point/controller and root/transport identities match configured owners. Fresh attempts/completions, frame reservations, root/queue/transport counters and held masks are zero; clocks agree; click budget is supplied. Compatibility consequence: a fresh child must prepare new state before its first update.
- Confirmed: actual camera yaw/pitch/distance match normalized/clamped supplied seeds. Fixture values are 0/0.6/18. Compatibility consequence: preserve configured camera initialization, not a universal zero pose.
- Confirmed: short replay reports BudgetCompleted at 3/3; strict gap replay reports Faulted at 225/226. Both report configured disposal, closed input/orbit hosts and zero remaining buffers. This is modern replacement-host behavior, not legacy parity.
- Confirmed: gap replay records two manual intents and two ShiftPressed speed intents. No noninitial orbit record appears. Unknown: final retired held masks and orbit pose, because those fields are absent from cleanup reports. Do not promote closed-host evidence into an unobserved pose/mask claim.

## Verification and failure history

Full build passes with warnings treated as errors, zero warnings/errors. Independent native verifier passes both logs. Six authored synthetic test methods cover repeated normal/fault runs and 33 negative cases: counters/masks/camera/clocks/ownership types, missing or incomplete cleanup, buffered transport and reused same-process identities. Native witnesses remain private and are not redistributed.

Initial offline restore failed; exact cached locked packages resolved it. Nullable diagnostics in the new partial were fixed and rebuilt. Initial native isolation omitted sibling preview-profiles and failed before admission; corrected isolated layout then passed. Certificate-store diagnostic remains an environment limitation, with no network operation used.

## Handoff and next probe

Only the owned partial, one hook, contract/checkpoint and Runtime test directory are deliverables. Shared canonical status/ledger updates belong to lead integration. Lead can import `expected`, `records` and `verify_runs`; persistent segments should pass `require_distinct_instances=True`. Next action is the lead's original-resource persistent normal/fault/restart join, preserving primitive-only reports and checking retirement evidence without assuming unavailable fields.

Lead integration additionally authorized a narrow correction in the same owned host source: read both synthetic connection identity and seed as U32 before preparation, allow supplied values only for configured Version13, and preserve earlier fixed pins plus datagram496/CRC2. The updated host compiles; distinct-generation native/frame validation belongs to the lead join and independent transport verifier.
