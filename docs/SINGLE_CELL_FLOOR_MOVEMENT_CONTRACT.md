# Bounded single-cell floor movement

## Original observations and confidence

The executable SHA-256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses below are baseline VAs. These are static observations, not results from original executable execution.

| Evidence | Observation | Confidence / consequence |
|---|---|---|
| `0x0126A600` | Converts a displacement through `0x0126A540`, calls `0x01277400`, restores the output locator's owner and position through `0x01269640` / `0x0126A5A0`, and forwards the result. | Confirmed wrapper; ownership/coordinate conversion must survive a full replacement. |
| `0x01277400` → `0x012777B0` | The full traversal uses a working locator, triangle visits and an edge-candidate queue, with multiple collision result branches. The adapter constructs or invalidates the output locator according to the result. | Confirmed; the bounded implementation below does not reproduce this full collision solver. |
| `0x01276670` | Endpoint containment yields result `4`; otherwise an intersected edge is checked through `0x012761F0`, and a valid neighbor advances the triangle loop. Self-neighbors/failure branches yield `5`. A crossable edge without a neighbor yields `10` without a portal and `7` with a portal. | Confirmed branch structure; preserves adjacency and separates portal/open-floor exits. |
| `0x012761F0` | Portal ID other than `-1` returns true before the edge flag. Otherwise flag `0` returns false and `1` returns true. Other values call `0x012792B0`, then conditionally `0x01275FE0` on the remaining displacement. | Confirmed predicate. Flag `2` is not established as an unconditional wall. Its geometric/contact semantics remain Unknown. |
| `0x012A2570` / `0x012A2850` | Sequential authored neighbors expand to runtime `+0x10,+0x14,+0x18`; edge bytes to `+0x28,+0x2C,+0x30`; non-solid byte to boolean `+0x34`; portal IDs to `+0x4C,+0x50,+0x54`. | Confirmed serialization chain; preserve raw authored fields at the boundary. |
| `0x013EB720` → `0x013EB3F0` | An edge consists of vertex `e % 3` and vertex `(e+1) % 3`. | Confirmed geometry helper, consistent with the authored adjacency pairs in independent corpus probes. |
| `0x012647B0` | Results `2`, `4`, `10` return boolean zero without copying collision outputs. Other results copy a point and return one only when a destination owner is resolved; missing starting floor returns one. | Confirmed boolean branches. Do not interpret this boolean as a universal movement-permission flag. |
| `0x01267380` | Horizontalizes contact-height-adjusted motion. Walker result `7` returns caller status `0`. Result `10` can invoke `0x0126AA80` and recurse on remaining motion; depth counter at five returns status `4` with default output. Result `4` also checks owner/triangle/height relationships. | Confirmed control flow; complete cell permission and owner recovery are still open. |
| Constants | `0x015FFCA4` is `FLT_MAX`, not normalized progress `1`; `0x018E5060` is float `0.01`; `0x015DD850` is float `3`. | Confirmed PE data. Original result families and numeric tolerances must not be equated with the modern statuses below. |

The original collision search can convert classifier outcomes into results `3`, `6`, `8`, `9`, `11`, or `12`, and can invalidate its output locator. Exact public enum names and all collision-mode semantics remain Unknown. The implementation follows the recovered simple edge predicate and adjacency structure; its numerical and failure policies are independently authored.

## Engine-neutral implementation

`SingleCellFloorWalker` snapshots positions and all topology arrays. Limits are one million vertices/triangles; finite coordinates and three-slot records are required. It has no mutable locator, engine objects, cache transfers, or retained per-walk state. Each call returns an immutable `FloorWalkResult`.

`Walk(triangleIndex, start, desired, maximumCrossings=4096)` starts from an explicit floor triangle and contact point. The start must lie inside its XZ projection with barycentric tolerance `1e-8`, and within `1e-4` units of its height plane. Desired Y is ignored: the walker follows the horizontal XZ displacement and projects contact height onto traversed triangles. Unsupported surfaces include non-solid, degenerate, vertical and nearly vertical faces (`abs(normal.Y)/length(normal) < 1e-4`). This is a numeric height-field policy, not an original walkability/slope limit.

The closest exiting barycentric plane determines an edge. Edge order is `(v0,v1), (v1,v2), (v2,v0)`. Portal-bearing edges override the raw flag; a valid same-floor neighbor may still be followed. Without a portal, flag `0` stops and flag `1` permits traversal; other flags stop as unsupported. Without a neighbor, portal/open-floor exits stop at their boundary. No cell or floor is searched implicitly.

Neighbors must have a reversed shared vertex-index pair and a reciprocal neighbor reference. Non-solid/unsupported destination surfaces stop before transfer. Revisited triangles, malformed links and no consistent exit fail explicitly. Coincident vertex exits within `1e-8` stop as `AmbiguousVertex`. The caller may allow zero through 4,096 crossings; exhausting this cap returns `TraversalLimit`. Endpoints inside/on the closed triangle are accepted before considering an exit. Starting exactly on an edge and moving across it can make zero-parameter progress into a distinct neighbor.

| Status | Meaning |
|---|---|
| `Accepted` | Horizontal destination reached with projected contact height. |
| `BlockedEdge` | Flag-zero edge without a portal. |
| `PortalBoundary` / `OpenBoundary` | Crossable edge has no same-floor neighbor; external continuation is unresolved. |
| `UnsupportedEdgePolicy` | Special edge needs original additional contact/collision behavior. |
| `UnsupportedSurface` | This bounded solid height-field slice cannot follow the surface. |
| `InvalidStart` / `InvalidTopology` | Locator/contact or adjacency contract failed. |
| `AmbiguousVertex` / `TraversalLimit` | Numeric ambiguity or caller traversal budget stopped progress. |

Results carry current triangle, projected contact position, normalized whole-segment parameter, exit edge, boundary portal ID and completed crossing count. Accepted progress is `1`; partial stops retain their boundary contact. Invalid starts retain their original position. These values are not the original locator memory layout or enum values. No sliding, body radius, vertical sweep, falling, doors, permission enforcement, alternate-floor continuation, or server movement transmission is implemented.

## Prototype host inspection and validation

Interior floor inspection exposes **Walk between picks**. Right-click a start, then a destination in the same cell. A yellow line/marker shows attained contact; the status names the outcome. Cell selection and mode toggles clear pending start/output; each completed pair releases its pending start. The line mesh is reused and cleared, and resources are disposed with the preview. The existing graph route remains a separate Euclidean inspection.

Native synthetic gates cover both crossing directions, sloped projection, portal override with/without neighbors, blocked/open/special edges, closed endpoints, zero-parameter crossing, malformed links, non-solid/degenerate/vertical surfaces, caller mutation, finite guards and cap exhaustion. Five hundred seeded walks across a connected 128-triangle plane agree with analytic endpoints, including more than 400 multi-crossing paths.

Independent raw-byte decoding plus line/edge intersection agrees on **17,658 probes across 4,441 eligible floors**, selected from all **4,730** effective installed floors. Outcome counts: 8,846 accepted (includes no-ops), 4,370 blocked, 3,970 portal boundaries, 384 open boundaries, 88 special-edge stops. The other 289 floors have no eligible sampled solid height-field case. This is bounded sample agreement, not full movement coverage or original runtime equivalence. Source SHA pins, status, triangle, edge, portal, normalized parameter and contact positions are checked. Seven original YT-1300 cell adapters and two-pick controls pass headless/GPU gates. See [checkpoint/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_CELL_FLOOR_MOVEMENT.md).

## Next compatibility probe

Trace `0x012792B0` / `0x01275FE0` and `0x0126AA80`: establish contact/projection mode, remaining-motion ownership, candidate owner transforms, entry permission checks and teardown. Reconcile the `CellObject+0x29C` allow-entry gate with `0x01267380` before implementing one paired-portal cell transfer. Preserve the full solver's return families as Unknown until their callers establish semantics.

## 2026-09-30 portal transfer follow-up

The [bounded transfer contract](PORTAL_FLOOR_TRANSFER_CONTRACT.md) adds one owner transfer and remaining motion with current offline entry/barrier policy. Floor portal IDs index the owning cell reference vector; they are distinct from global geometry/PGRF keys. The prior tentative 0126AA80 owner-transfer interpretation is corrected: it preserves the same floor wrapper; 012670E0 / 00B2B3F0 perform cell continuation. Fourteen original directed links, 56 independent probes and 28 prototype host adapter probes pass. Full actor collision, door lifecycle and exterior continuation remain open.

## Returned-contact stability

The [bounded output contact policy](FLOOR_CONTACT_STABILITY_CONTRACT.md) preserves strict start validation and makes returned edge locators reusable. It introduces an explicit precision-budget hold instead of returning an unusable float contact.

## Piecewise path extension — 2026-09-30

[Trace](FLOOR_PATH_TRACE_CONTRACT.md) returns ordered double-precision plane segments and the same walk result. Geometric terminal points and canonical reusable locators remain distinct.
