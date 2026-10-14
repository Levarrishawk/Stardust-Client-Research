# Stardust.exe four-hour analysis session — 2026-09-12

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

This session remained analysis-only. No replacement-client, prototype host, DX12, or
server implementation code was started or modified.

## Outcome

Twenty-four bounded closures were completed: twenty-three planned subsystem
blocks plus the final QCHN/ENVM contradiction audit. The conservative weighted
whole-client research estimate advanced from 70% to **74%**. Architecture and
engine-decision readiness advanced from 89% to **92%**.

These percentages measure replacement-relevant behavioral and data contracts,
not raw disassembler function objects. The current stable-address ledger has
824 evidence rows covering 805 distinct primary addresses. 789 rows explicitly
contain Confirmed evidence and 43 contain Strong evidence (overlapping); no row
has status merely Unknown.

## Completed blocks

1. SOE optional transport transforms and retained prepared-packet behavior.
2. Terrain tag/classification closure.
3. Startup stale-row/configuration audit.
4. ObjectController absent-inner-type policy.
5. CommandQueueRemove result categories and detail fields.
6. Global wind direction/magnitude/callback service.
7. PTAT WMAP/SMAP formats and live-consumer boundary.
8. Structure-placement request/result workflow.
9. Ticket purchase, use and travel transition flow.
10. LaunchIntoSpace transition flow.
11. Radial-menu request/response/selection flow.
12. SUI page construction, events and response protocol.
13. NPC conversation start/select/stop loop.
14. Character-sheet response message family.
15. Group space-launch invitation/consent family.
16. Group loot-lottery open/close/ballot family.
17. Image-design session, change and rejection flow.
18. Customization manager CIDM/ACST tables.
19. Palette RIFF resource grammar and runtime cache.
20. Palette-column permission/selection policy.
21. Customization presentation DTII hierarchy.
22. Customization group-instance and equipment slot-rule ASTs.
23. Effective SAT/SKT/MGN/LMG/LAT/ANS corpus and rare format paths.
24. MLOD/SLOD runtime ownership plus QCHN component and ENVM policy closure.

## Highest-value findings

### Protocol/gameplay

The packet map gained implementation-grade client/server/state chains for
structure placement, ticket travel, space launch, radial menus, SUI, NPC
conversation, character-sheet responses, group space invitations, group loot
lottery and image design. Where current Stardust server payloads differ from
the client reader, the asymmetry is documented rather than normalized away.

### Customization and equipment

The client-side customization UI and runtime are now constrained by actual
deployed tables rather than guessed widgets. The work covers the 467-row
presentation table, 96-row group-instance table, palette resources and
palette-column restrictions, plus all four shipped recursive equipment-slot
expressions. Slot rules are a SLOT/NOT/AND/OR AST that injects derived blend
variables into the live customization owner.

### Appearance and animation assets

An overlay-aware census records 38,440 effective appearance/animation assets:
4,926 SAT, 270 SKT, 19,575 MGN, 4,882 LMG, 195 LAT and 8,591 ANS resources.
It identifies deployed version families and rare DOT3, HPTS, TRTS, SFSK, APAG
and QCHN paths.

SAT LDTB is confirmed as squared min/max distance intervals with current-index
hysteresis. SFSK skips the ordinary intermediate skeletal detail beyond the
near threshold; APAG bypasses the normal periodic-animation age gate.

MLOD/0000 contains a signed-16 count and NAME mesh-generator paths. Files carry
one to eight names, but Stardust.exe exposes at most four. Children are lazy
resolved and reference-count cached. SLOD/0000 contains a signed-16 count and
eager inline skeleton templates; files carry one to seven children and also
expose at most four. Both honor `ClientSkeletalAnimation.skipL0` by omitting
and compacting away serialized level zero—not by retaining a null hole.

Compressed QCHN stores U16 frame plus U32 packed quaternion. Three header bytes
select quantizer families. Packed fields decode in order to x, y and z; the
nonnegative reconstructed square-root component is w. Runtime quaternion
memory order is confirmed `[w,x,y,z]` by the decode wrapper, axis-angle
constructor and quaternion-to-matrix builder.

### Uncertainty discipline

`UNKNOWN_FUNCTIONS.md` is now explicitly treated as a historical stable-name
ledger. Its growth means more addresses have durable evidence. The actual
material queue is `FUNCTION_UNCERTAINTY_AUDIT.md`.

ENVM TLOK/SKYB/DIST is closed for Stardust.exe compatibility: exact deployed
schemas are preserved, but executable-wide tag-immediate analysis proves this
binary has no recognizer or active rendering policy for them. A future enhanced
renderer may implement them only as separately validated opt-in behavior.

The remaining material category is UI/gameplay breadth: additional mediators,
optional commands and specialized message families must be processed workflow
by workflow. Original unpublished C++ names, compiler thunks, CRT/STL,
middleware internals and false function boundaries are not compatibility
contracts.

## Evidence and confidence

- **Confirmed:** all specific byte layouts, counts, addresses, vtable slots,
  server correlations and corpus totals stated above.
- **Strongly inferred:** friendly historical names where mechanics are proven
  but RTTI/editor labels are absent.
- **Tentative:** not used to close any material item in this session.
- **Unknown:** remaining UI/gameplay breadth until each selected workflow is
  traced; enhanced behavior for asset extensions absent from Stardust.exe.

## Recommended next block

Continue breadth reduction with an overlay/registry-derived inventory of the
remaining named messages and ObjectController types, rank those reachable from
current Stardust server source, and close the highest-impact unmapped workflow
end to end. In parallel documentation—not implementation—maintain fixtures for
the now-confirmed asset codecs and coordinate adapter.

## Durable outputs

- `COMPREHENSIVE_ANALYSIS_LEDGER.md`
- `FUNCTION_CLOSURE_SUMMARY.md`
- `FUNCTION_UNCERTAINTY_AUDIT.md`
- `MATERIAL_ANALYSIS_BACKLOG.md`
- `UNKNOWN_FUNCTIONS.md`
- `APPEARANCE_ANIMATION_ARCHITECTURE.md`
- `ASSET_FORMAT_CATALOG.md`
- `ANALYSIS_CHECKPOINT_2026-09-12_APPEARANCE_CORPUS_VERSIONS.md`
- `ANALYSIS_CHECKPOINT_2026-09-12_MLOD_RUNTIME.md`
- `ANALYSIS_CHECKPOINT_2026-09-12_SLOD_RUNTIME.md`
- individual protocol/customization checkpoints named in the private workspace
