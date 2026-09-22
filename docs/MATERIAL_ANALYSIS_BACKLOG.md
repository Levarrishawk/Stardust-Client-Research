# Material client-analysis backlog

Updated: 2026-09-21  
Target: `Stardust.exe` SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

This is the actionable remainder behind the current **83% comprehensive
whole-client coverage**. It is separate from `UNKNOWN_FUNCTIONS.md`, which is a
chronological address/evidence ledger rather than a to-do list.

## Closure principle

Every replacement-relevant function encountered in an active workflow must be
resolved to inputs, outputs, state changes, ownership, ordering and failure
behavior, or classified as replaceable implementation machinery. Original
unpublished names and replicas of compiler thunks, CRT/STL templates, D3D9,
Miles or DirectInput internals are not required.

## Closed boundaries not to reopen without contradictory evidence

- Current Stardust server packet inventory: **345 documented / 0 reachable
  gaps**. Continue codec-depth audits where risk evidence exists; do not restart
  opcode discovery as though the matrix were incomplete.
- Object replication core, scene entry/exit and baseline/delta routing are
  architecturally closed; specialized object/UI side effects remain breadth.
- Effective renderer PASS/STAG/TXM/DDS boundaries and core frame schedule are
  closed to replacement depth.
- Core VFS/TRE overlay, tombstone, CRC, IFF factory and primary mesh/skeleton/
  animation/terrain/audio/input formats are closed.
- Input assets are fully inventoried: 436 commands and 1,939 physical bindings.
- File streamer, general async loader and procedural-terrain worker ownership/
  shutdown are closed.

## Remaining workstreams

| Priority | Workstream | Material remainder |
|---:|---|---|
| 1 | UI/gameplay workflow breadth | Finish remaining inventory, trade, crafting, mission, guild, social, combat and space mediator actions end to end; classify UI-only presentation separately. |
| 1 | Active server/client contradictions | Preserve the risk register and resolve/fixture-test quest counter/timer IDs and VerifyPlayerName schema before a networking POC claims compatibility. |
| 1 | Asset-format/runtime long tail | Close rare but effective specialized appearance, particle, portal/interior, space and client-data variants with corpus fixtures and runtime consumers. |
| 2 | Animation/presentation exceptions | Finish appearance-specific controller/modifier exceptions, notification ordering and remaining action/event consumers. |
| 2 | Renderer/world special cases | Finish particles, shadows, specialized scene primitives, visibility exceptions and remaining space/environment presentation semantics. |
| 2 | Lifecycle/ownership breadth | Audit remaining worker threads, scheduled callbacks, pools, caches and shutdown ordering not covered by the closed loader/terrain blocks. |
| 3 | Input/UI policy refinements | Map long-tail numeric action interpreters only where behavior is not already captured by semantic payloads; MAP inheritance and duplicate precedence are closed. |
| 3 | Failure/contradiction audit | Continue malformed/truncated-input reasoning and reconcile stale historical claims while retaining the evidence trail. |

## Current known compatibility risks

See `SERVER_CLIENT_COMPATIBILITY_RISKS.md`. The packet matrix being complete
does not mean every current server byte stream matches the executable. Active
quest counter/timer and VerifyPlayerName discrepancies are specifically kept
visible.

## Recommended next sequence

1. Close one high-use UI/gameplay workflow family from command/event through
   server or replicated state and mediator result.
2. Alternate with one rare active asset/runtime family to preserve breadth.
3. Perform one lifecycle/ownership block per session until all non-library
   workers and delayed callbacks are classified.
4. Run corpus fixtures and the provisional-ledger audit before each session
   report.
5. Increase weighted coverage only for durable cross-boundary closures.

## Completion test before returning to build work

Analysis is sufficient when every server-reachable item remains classified,
every effective original asset family needed by deployed content has a bounded
reader and runtime contract, active schema contradictions have explicit codec
profiles/tests, lifecycle boundaries are documented, and all remaining raw
function groups are either reachable through a closed workflow or classified
as redesignable machinery.
