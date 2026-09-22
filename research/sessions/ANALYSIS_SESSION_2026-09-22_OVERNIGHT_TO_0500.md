# Overnight analysis session — 2026-09-22

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Method: static executable analysis, effective client-data inspection and correlation
with the local Stardust/Core3 server source. No dynamic interception, client build,
GitHub publication or shutdown was performed.

## Outcome

This continuation completed twenty bounded analyses and reconciled them into the
canonical ledgers. It raises the conservative implementation-grade estimate from
93% to **94%** and the expanded comprehensive estimate from 83% to **85%**. These
figures measure replacement-relevant behavioral specification, not the percentage
of raw compiler-generated function objects assigned historical source names.

The normalized provisional-name inventory now contains **1,876 unique executable
addresses** and no duplicate address rows. The historical Markdown ledger retains
chronological observations so later confirmation does not erase the research trail.

## Completed blocks

1. Space radar runtime and its world/contact data boundaries.
2. HUD target-status presentation and replicated-state dependencies.
3. Ship-selection component workflow.
4. Space HUD formation presentation and command boundary.
5. Toolbar action model and command dispatch.
6. Quest counter/timer compatibility decision.
7. Combat-queue UI projection.
8. Network-status and MFD widgets.
9. Ship weapon-group editing and persistence boundary.
10. Biography request, response and update workflow.
11. Droid command-programming transaction.
12. Skills UI family and profession graph projection.
13. Data-storage tabs, waypoint creation and settings persistence.
14. Command browser, command drag/drop and local macro ownership.
15. Experience monitor and `PLAY:8` projection.
16. Ground-group UI, near/far member status and queued commands.
17. Loading, scene-selection, news, disconnect and splash shell mediators.
18. Hidden locations/`planetwarp` developer mediator.
19. Harassment-report name-verification workflow.
20. Game-menu action routing and hidden developer entry.

Each block has a dedicated `ANALYSIS_CHECKPOINT_2026-09-22_*.md` report with
addresses, evidence, confidence and explicit non-claims.

## Highest-value findings

### Biography protocol

The client request constructor is at `0x0065F6B0`, update constructor at
`0x0065F750`, editor construction at `0x00BE1950`, and response handler at
`0x00BE1E60`. The server object-controller path uses subtype `0x1DB` with a U64
subject object ID followed by a Unicode biography. The response becomes a typed
`BiographyRetrieved` client event; update follows the matching set-biography path.

### Droid command programming

The confirmed client transaction is object-controller C-to-Z subtype `0x435`:

```
U32 namedCommandCount
repeat namedCommandCount: ASCII commandName
U32 chipAddCount
repeat chipAddCount: U64 chipObjectId
U32 installedRemoveCount
repeat installedRemoveCount: U64 installedCommandObjectId
U64 petControlDeviceId
```

The server remains authoritative: it validates the requested changes, creates or
destroys command objects, and lets containment/replication reconcile the UI.

### Ground-group behavior

The ground group mediator combines replicated `GRUP:6` membership with near-member
state and far-member `ClientMfdStatusUpdate` data. Context actions queue the exact
commands `dismissGroupMember`, `disband`, and `leaveGroup`; no special group packet
family needs to be invented for the replacement client.

### Session readiness

Ground loading is gated by five distinguishable conditions: server objects, file
caching, world-loaded state, terrain generation and player readiness. This is a
useful replacement-client contract: connection success alone must not transition
directly into playable world state.

### Read-only projection systems

Skills, experience, combat queue, target status, radar, command browser and most
data-storage views are projections over replicated object state plus original
assets. Their mediators are not independent network protocols. This supports a
modern presentation layer fed by a typed compatibility model instead of a literal
port of CUI mediator internals.

## Server/client discrepancies requiring deliberate compatibility policy

| Boundary | Evidence-backed status | Consequence |
|---|---|---|
| Quest counter/timer | Current server response behavior does not match the executable consumer contract | repair server response or implement a narrowly compatible client adapter; do not guess from UI strings |
| Space formation | Client presentation paths exist but corresponding Stardust command handling is incomplete/absent | server work is required before feature-complete validation |
| Ship repair | Current server path is effectively a no-op for the examined client workflow | not an executable-analysis unknown; track as server feature debt |
| Harassment verified-name response | Server declares a one-code-unit Unicode name but omits the code unit | malformed wire response; correct the server serializer before relying on the workflow |

These findings are valuable because they separate server defects or omissions from
unknown client semantics.

## Architecture consequences

- Preserve a strict wire-compatibility layer with fixed-width protocol types; do
  not expose native pointer size or engine object layout to serialization.
- Preserve original TRE/IFF assets through a versioned asset IR and typed loaders.
- Route decoded replication and messages into a typed client model, then let UI,
  renderer, audio and input observe that model.
- Treat legacy CUI mediators as behavioral references, not classes to reproduce.
- Keep the five-stage world-readiness barrier explicit in session management.
- Keep local preferences/macros distinct from authoritative replicated state.

## Remaining analysis

The material unknown queue no longer contains a core wire, replication, asset-loader
or lifecycle blocker. Remaining work is still substantial if “complete” means every
optional feature and presentation detail:

- unvisited long-tail UI and specialty gameplay mediators;
- exhaustive ownership/failure auditing outside the critical paths already mapped;
- original-client visual, timing and interaction fidelity;
- controlled live-server and malformed-input validation after a test harness exists;
- rare asset/version combinations not present in the effective installed corpus.

The next useful analysis phase should continue closing the optional workflow tail,
but it no longer needs to postpone an engine feasibility decision. A later proof of
concept must remain narrowly scoped to x64 startup, configuration/logging, SOE
transport, minimum login handshake, one known response, and clean disconnect.

## Confidence statement

- **Confirmed:** address mappings and byte layouts explicitly identified in the
  checkpoint reports; server serializers/handlers cited there; unique-ledger count.
- **Strongly inferred:** subsystem boundaries established by several callers,
  strings and state transitions where original symbols are absent.
- **Tentative:** original developer-facing names for anonymous fields and classes.
- **Unknown:** optional presentation fidelity and behaviors with neither an active
  executable path nor effective content example.

No conclusion in this report treats reconstructed pseudocode as original source.
