# Bounded portal floor transfer

Updated: 2026-09-30. This engine-neutral inspection slice continues a floor walk through one paired interior portal. It is not full actor collision or network movement.

## Original evidence and confidence

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses are baseline VAs.

| Observation | Interpretation / confidence | Compatibility consequence |
|---|---|---|
| `012670E0` transforms requested motion into the current owner frame, calls `01266890` / `01266960` / `01267380`, resolves a portal through `00B2B3F0`, transforms attained contact into the destination frame, constructs a locator through `01269330`, recurses, and cleans temporary locators through `01269400`. | Confirmed cell-owner continuation chain. Exact result enum semantics remain Unknown. | Preserve attained contact and remaining motion across owner changes. |
| `00B2B3F0` indexes the owning cell's portal-reference vector by the floor portal ID, then reads linked portal `+0C` and destination CellProperty `+10`. | Confirmed: floor IDs are local reference ordinals. | Do not substitute global geometry indices or PGRF keys. |
| `0126AA80` invokes `01279B90` and assigns the same floor wrapper to the result. `012792B0` projects an edge contact; `01275FE0` handles special-edge continuation. | Confirmed same-floor candidate/contact operations; the former proposed owner-transfer interpretation is corrected. | These helpers alone do not establish cross-cell transfer. The five-continuation bound in `01267380` is not a proven cross-cell recursion bound. |
| `00422E80` registers `00422CB0` through `00B2A8A0` at `00423533`; registration stores global `01946898`. `00B2BDB0` invokes that callback, defaulting to true if absent. | Confirmed entry-permission callback. | Expose current permission as runtime state. |
| `00422CB0` rejects actor state bit `1B` via `00435250`, allows world/null-owner cases, otherwise reads CellObject `+29C`. `006E6850` stores the replicated byte; `006E58A0` initializes it true. | Confirmed byte ownership; meaning of actor state 27 Unknown. | Do not assign an unsupported gameplay name to that state. |
| `00B53920` tests both template bytes and calls `00B2BDB0`; caller `01290CFA` belongs to the portal barrier path. Direct `012670E0` / `00B2B3F0` has no visible unconditional permission callback. | Confirmed separate barrier predicate; complete actor collision ordering Unknown. | The prototype's explicit policy-before-transfer rule is a conservative owned policy, not recovered original call ordering. |
| `00B2B240` chooses a nearest geometric segment crossing through `00B53160`, whose predicate rejects a nonzero first template byte. All original YT-1300 interior references have raw SolidByte 1. | Confirmed distinct geometric query and corpus bytes. | Raw SolidByte alone cannot be treated as current door/barrier permission. |

Server correlation: `MMOCoreORB/src/server/zone/packets/cell/UpdateCellPermissionsMessage.h`, constructor `UpdateCellPermissionsMessage`, emits allow-entry and cell ID. This supports the existing replication boundary; it does not prove client collision ordering. No packet-map or coverage denominator changes are claimed.

## Owned implementation contract

`PortalFloorTransferQuery` snapshots floor geometry and topology. Its `Walk` accepts a cell/triangle/local-position locator, a desired world position, and a current permission provider receiving the complete `PortalFloorLink`. The result carries owner, triangle, local/world contact, whole-segment parameter, transfer count and explicit stopping status. No engine objects or mutable per-walk state are retained.

Preparation resolves local reference ordinals, requires exactly two distinct cell references with reciprocal targets, and finds one reversed counterpart boundary edge within `1e-4` world units. Ambiguous, missing or mismatched geometry stops explicitly. These are conservative prototype acceptance rules. Supplied owner poses must be finite rigid yaw/translation transforms; identity poses in the scene inspector are an inspection choice. Limits are 4,096 cells/geometries, 32,768 references, one million aggregate vertices/triangles and a caller pair-comparison budget capped at one million.

The source uses the [single-cell walker](SINGLE_CELL_FLOOR_MOVEMENT_CONTRACT.md). At a paired portal contact, the query validates the destination landing and evaluates current entry/barrier state once. Unknown permission, denied entry or blocked barrier retains the source boundary owner. Exceptions propagate without poisoning subsequent queries. The raw template SolidByte does not invent runtime barrier state.

Destination contact uses the matched edge. A floating-point landing correction may move toward the destination triangle centroid by at most `1e-5` world units, remaining within the `1e-4` contact tolerance. This is an explicit modern numerical policy; the original solver's exact numerical treatment is unresolved. Remaining motion is converted into destination coordinates. A second portal stops with `TransferLimit` on the destination boundary; only one transfer is supported. Triangle crossings share the caller budget, capped at 4,096.

## Validation and scope

The pinned `appearance/yt1300.pob` SHA-256 is `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`; installed configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` with 48 TREE mounts. Seven floors contain 149 triangles; the scene resolves 21 sources. Its seven interior geometry pairs yield **14 directed links**. Independent raw-byte parsing and line/edge intersection agree with **56 native probes** for allow, deny, barrier and unknown policies. Source hashes, link identity, owner, triangle, parameter and contact positions are checked.

Synthetic gates cover reverse transfer, remaining motion, rotated/translated owners, changing policy, unknown values, callback failure/reuse, invalid ordinals, missing floors, displaced/ambiguous counterparts, caller mutation, nonfinite/scaled poses, preparation budget and second-portal stopping. Godot verifies **28 allow/deny adapter probes** across the 14 links, reflection, two-pick controls and changing entry/barrier toggles. Full smoke tests and host builds pass. This denominator is one original scene plus synthetic cases, not every installed portal layout.

Godot floor inspection offers **Portal transfer**, **Entry allowed**, and **Barrier open**. These toggles are offline policy inputs. Exterior cell 0 has no floor; the cockpit exterior reference has no corresponding floor boundary edge. Exterior continuation, actor radius, sliding/falling, special-edge collision, door animation/barrier lifecycle and live permission replication remain unsupported.

See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_FLOOR_TRANSFER.md) for reproduction. Next probe: follow `01290CFA` and `012906F0` barrier creation/enable/teardown and their ordering relative to `012670E0` / `01266410`, before connecting actor contact to transfer policy.

## 2026-09-30 permission barrier follow-up

The [barrier contract](PORTAL_BARRIER_COLLISION_CONTRACT.md) adds normal-controller primitives and bounded source-cell actor-box contact before transfer. Runtime fields are LeadingByte exclusion, SolidByte support and separate WindingByte; B43E80 inversion is specific to PRTO/0002. Current permission, mutable portal state and physical door-leaf clearance remain distinct. Independent 291-layout / 21,693-probe agreement and 12-link Godot actor contact gates pass; nonlevel requests stop explicitly. Physical leaf collision and global actor solver ordering remain next.

## Complete level-path guard — 2026-09-30

The [guard checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LEVEL_PATH_GUARD.md) adds read-only source/target floor traces using the actual validated transfer bridge. The level-only barrier adapter now checks every segment endpoint, so equal-height terminal points cannot hide an intermediate ridge. This rejects unsupported movement before adoption; full slope collision remains separate.

## Thin target landing follow-up

[Bounded inward-edge fallback](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LANDING_NORMAL.md) supplements centroid-directed float target bridging when a thin slanted triangle remains invalid. Correction is at most 10 micrometres from the projected target landing; source/world reconciliation remains at most 0.1 mm. External start tolerance is unchanged. 1,000 synthetic target landings and the pinned 948-tick Yavin reference pass; YT-1300 56-probe results remain unchanged.
