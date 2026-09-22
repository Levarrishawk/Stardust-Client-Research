# Stardust Client Research Checkpoint — 2026-09-11

## Executive result

The two-hour analysis block materially advanced the protocol surface without beginning a client rewrite. The executable remained exclusively `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The current Stardust-3 source was read for correlation and was not modified.

The conservative estimate of **properly researched replacement-relevant client behavior is 38%**. This uses the declared weighted scope below. It is neither the percentage of machine-code functions named nor a claim that 38% of original source has been recovered.

The engine/build decision evidence gate remains complete: enough is known to conduct a Godot proof of concept while keeping networking isolated from engine concerns. That narrower gate being 100% does not make the whole client 100% analyzed.

## Findings completed in this block

### Active client-to-server ObjectController surface

All 24 ObjectController types actively registered by the current server now have directional field schemas and server-side effects in `ACTIVE_OBJECT_CONTROLLER_SCHEMAS.md`. The executable registry recognizes all 24 numeric types. Direction must be part of the codec key because several types have asymmetric layouts. The callback cursor's initial four-byte handling is now explained by the outer scalar. Image design's full nested record and secure-trade `0x115` (`U32 operation, U64 firstId, U64 secondId`) are resolved; the trade request form is operation 0 with invalid first ID and target second ID.

### Server-to-client ObjectController emissions

A reproducible source scanner found 71 direct construction sites and 51 distinct constant inner types. Fifty have executable factories. The exception exposed a real quest-journal incompatibility:

- executable counter is `0x440`; server counter is sent as `0x441`;
- executable timer is `0x441`; server timer is sent as `0x444`;
- no executable `0x444` factory was recovered.

The server's `0x441` counter selects the timer codec and has a trailing field the timer class cannot represent. The `0x444` timer is expected to take the unknown-inner-type path. No ordinary screenplay use was found beyond Lua wrappers, making this latent/optional rather than a login blocker.

Common and extended S→C layouts are in `CORE_OBJECT_CONTROLLER_SCHEMAS.md` and `EXTENDED_OBJECT_CONTROLLER_SCHEMAS.md`. The latter adds harvester, JTL/hyperspace, ship-hit, space-invite, loot-lottery, image-design, schematic, draft-slot, resource-weight, and crafting status formats.

### Message-name miss resolution

The controlled catalog contains 114 current-server message-class names and 297 executable message-like strings:

- 77/114 exact same-name matches: 67.54%;
- 31 additional names resolved as aliases, routes, ObjectController identities, or executable class aliases;
- 108/114 identity-resolved lower bound: 94.74%;
- six retain a server wire schema but no direct executable route.

This is an identity metric, not schema completeness. New direct routes include append-ticket-comments, knowledge-base-search responses, PvP status, and resource-harvester activation. `UpdatePVPStatusMessage` runs from parser `0x00AD3000` through scene-object lookup to bitmask/faction writes at object `+0x3A0/+0x3A4`. Harvester activation runs through parser `0x013F3F60` to UI boundary `0x008CC460`. `PlayerMoneyResponseMessage` maps to executable `PlayerMoneyResponse` parser `0x013F63A0`, with bank/cash fields confirmed; its final consumer remains unknown.

### Offline transport validation

`tools/verify_transport_vectors.py` independently replays the seeded CRC and selector-4 transform to validate the research fixtures. Nine assertions pass for the CRC/transform primitives, complete uncompressed established reliable datagram, synthetic `LoginClientId`, and channel-zero ACK. It performs no network access and is research validation, not replacement-client implementation.

### Function inventory

| Metric | Count |
|---|---:|
| Candidate union (direct-call targets, entry point, common prologues) | 94,689 |
| Stable provisional VA names in Markdown | 190 |
| Stable names intersecting candidates | 163 |
| All literal VAs in Markdown | 650 |
| Literal VAs intersecting candidates | 355 |
| Stable candidate ratio | 0.17% |

This is a lower-bound documentation metric, not an authoritative function database. It misses unusual/indirect-only entries and includes false-positive prologues. Almost all individual compiled functions remain unnamed.

## Progress model

The 38% figure measures implementation-grade behavioral/specification research for a practical compatible replacement. Each domain is weighted by expected contribution to the full client and scored only for evidence strong enough to guide implementation.

| Domain | Scope weight | Researched | Weighted contribution |
|---|---:|---:|---:|
| Executable, platform, lifecycle, runtime | 8% | 55% | 4.4% |
| SOE transport, login, zone handshake | 15% | 88% | 13.2% |
| Application messages and ObjectController protocol | 18% | 42% | 7.6% |
| Object model, baselines, deltas, containment | 15% | 35% | 5.3% |
| Asset VFS, IFF/TRE formats, data tables | 14% | 22% | 3.1% |
| Rendering, terrain, cells, world presentation | 12% | 18% | 2.2% |
| Appearance, customization, animation | 7% | 18% | 1.3% |
| UI, input, audio | 6% | 15% | 0.9% |
| Long-tail gameplay behavior | 5% | 8% | 0.4% |
| **Total** | **100%** | — | **38.4%, reported as 38%** |

Server source can confirm bytes and server effects, but a server-derived schema does not receive full client-behavior credit until the executable consumer or a controlled trace establishes its client state transition.

## Percentages that must remain separate

1. **Overall replacement-relevant behavioral research: 38%.** This is the requested total estimate.
2. **Engine/first-POC decision readiness: 100%.** Enough evidence exists to test Godot without pretending the whole client is understood.
3. **Mechanically stable individual-function naming: 0.17% of heuristic candidates.** Exhaustive function reconstruction is overwhelmingly incomplete.

Protocol identity has its own 94.74% lower-bound metric and must not substitute for any of these.

## Major remaining work

- Establish a real decompiler/function database and systematic symbol namespace.
- Recover the six unresolved current-server message routes and expand parser → handler → affected-state mappings for identified families.
- Complete baseline/delta schemas across every object tag and view, including mutation and notification semantics.
- Deepen TRE/IFF coverage into executable import paths and build a representative asset dependency/validation corpus.
- Trace raster API calls, shader/state translation, terrain/cell visibility, skeletal animation, customization, and UI binding at implementation depth.
- Map thread ownership, network-to-main-thread handoff, async resource completion, memory pools, shutdown ordering, and failure behavior.
- Research gameplay vertical slices by Stardust priority: movement/scene entry, chat/commands, inventory/equipment, combat, crafting, missions, structures, space, and social/UI.

## Risks and blockers

- Static disassembly without a full analysis database makes indirect calls expensive to attribute.
- Server convenience names can collide on one opcode (`0xFE0E644B`; `0xA16CF9AF`), so numeric opcode plus validated schema must be canonical.
- Server source can contain latent/custom mismatches, as proven by quest counter/timer IDs.
- Historical zlib output can differ across versions; compressed golden fixtures still need controlled generation/observation.
- Broad asset and rendering behavior remains the largest non-network uncertainty for a playable client.

## Recommended next step

Proceed with a tiny Godot x64 networking spike after reviewing this checkpoint. It should load configuration, serialize the documented 14-byte SOE session request, validate a synthetic/session response through pure protocol code, serialize and verify the synthetic `LoginClientId` fixture, log parsed results, and exit cleanly. Live server contact remains a separately authorized controlled test. No world, renderer, UI recreation, or gameplay belongs in that milestone.

If the Godot spike passes its fail gates, return to sustained executable research before broad construction, prioritizing replication schemas and asset import boundaries rather than indiscriminate function naming.

## Validation record

- Executable hash rechecked and matches the documented Stardust.exe baseline.
- Final research set: 42 Markdown files, 459,751 bytes.
- Packet-handler map: 55 data rows.
- Offline transport verifier: all nine assertions passed.
- Search for the previously removed incorrect executable name: no references.
- `D:\Development\Experimental-Client` is not inside a Git repository.
- Server repository final `git status --porcelain`: empty.
