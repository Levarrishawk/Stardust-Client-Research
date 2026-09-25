# Ordered analysis follow-up — 2026-09-25

This session executed the previously recommended work in order.

## 1. UI mediator reachability and live gaps

- Added a reproducible 217-class UI workflow register with a separate reviewed
  override file.
- Preserved the distinction between exact-name research mentions and complete
  workflow closure.
- Closed `SwgCuiControlsMenu` plus `CuiControlsMenuBindEntry`: working-copy
  transaction, preference toggles, conflict confirmation, save failure and
  teardown.
- Closed `SwgCuiClusterSelection`: effective asset, table/status projection,
  validation, endpoint handoff, permission continuation, stale-result rejection
  and cleanup.

## 2. Transport/login/zone fixtures

- Added credential-free application vectors for `LoginEnumCluster`,
  `LoginClusterStatus`, four-byte `ClientPermissionsMessage`, and the tolerated
  historical three-byte permission form.
- Independently verified the SWG hash (`ClientPermissionsMessage` =
  `0xE00730E5`) and exact current server layouts.
- Defined the controlled-server state matrix for matched, denied, stale,
  wrong-ID, status-loss and capacity cases. No live server was contacted.

## 3. Lifecycle/threading cancellation

- Closed the GameScheduler queue contract: frame-thread minimum heap,
  integer deadlines, reentrant deferred insertion, once-only callbacks and
  destruction without callback invocation.
- Confirmed that it exposes no cancellation handle and delegates context
  ownership to each callback.
- Bounded one delayed ticket UI route that carries a raw owner pointer as a
  runtime lifetime-risk fixture and a design prohibition for the replacement.

## 4. Conditional original-asset reachability

- Enumerated all static playback-template fetch routes.
- Closed the four `PSKA/0003` / `GRLB/0003` / `FIPL` / `PPRL` scripts as
  dormant for the current server/configuration: fixed install and combat-manager
  sources cannot select them.
- Retained lossless forward-action import as mandatory; arbitrary future/debug
  callers can reopen execution semantics.

## 5. Derived replication scenario

- Added and passed an `RCNO:6` fixture proving the unusual 12-byte inherited
  zero prefix and max/type/name alignment.
- Restated the complete live chain from RCNO fields through resource icon
  fallback and REP0 texture binding, including invalid legacy delta indices and
  destroy/zone-transfer requirements.

## Verification

- login/zone fixture assertions: **8 passed**;
- RCNO view-6 assertions: **7 passed**;
- no replacement client, renderer or engine project was created;
- no network access, executable modification, commit or push occurred.

The evidence-based whole-client percentages remain **89% comprehensive
behavioral coverage / 94% implementation-and-engine-decision readiness** until
a formal weighted rescore is justified. This session improves closure quality
and removes A05 as a current build blocker; it does not manufacture a percentage
increase from block count.
