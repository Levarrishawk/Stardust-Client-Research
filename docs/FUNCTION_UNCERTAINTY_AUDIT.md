# Function uncertainty audit

Date opened: 2026-09-12  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Why `UNKNOWN_FUNCTIONS.md` looked worse than it is

The historical filename is misleading. That document records functions for which the original C++ symbol is unavailable and assigns a stable provisional name. It does **not** mean the function's behavior is unknown. At this audit's start it contained 645 address-led rows covering 630 distinct primary addresses. Simple evidence-keyword counts found 611 rows containing Confirmed evidence, 37 containing Strong evidence, seven containing Tentative evidence and ten containing the word Unknown; these counts overlap because one row can contain both confirmed mechanics and one unknown field label.

Thirteen primary addresses had duplicate historical rows, usually because a later block confirmed an earlier tentative identification. Those are documentation debt, not executable uncertainty, and will be consolidated without discarding material evidence.

The raw Ghidra inventory is also not a meaningful completeness denominator. It contains 243,277 function objects, including compiler thunks, exception/unwind islands, library/runtime code, tiny helpers, imports and false-positive function boundaries. Reimplementing every one would amount to cloning the compiler output rather than reconstructing the client.

Current 2026-09-22 07:00 snapshot: the chronological Markdown ledger remains an
append-only evidence history plus later prose closures. The normalized
machine-readable ledger contains 2,034 distinct addresses with no duplicate
address rows. The growing row count measures accumulated stable evidence, not a
growing unresolved backlog. See `FUNCTION_CLOSURE_SUMMARY.md` and
`ANALYSIS_SESSION_2026-09-22_TO_0700.md`.

A fresh mechanical scan of the chronological address tables finds 1,110 rows
covering 1,079 distinct primary addresses: 1,072 rows contain Confirmed
evidence, 54 contain Strong evidence, none contain Tentative, and one retains
the word Unknown. These keyword counts overlap, and later prose closures plus
the normalized registry extend beyond the original table form; they are an
uncertainty-health check, not a completeness denominator.

The sole table row containing the word Unknown is `0x012C63C0`; its transport
transform selector behavior is Confirmed and only the historical source labels
for unused selectors 2/3 remain unknown. It is not a Class-C behavioral gap.

## Required standard for a proper replacement

We need complete replacement-relevant behavior, not line-for-line identity. A function is considered closed when its externally observable contract is known sufficiently to either:

1. implement compatible behavior;
2. replace it with a modern subsystem having the same observable effects; or
3. prove it is compiler/runtime/backend machinery with no independent compatibility contract.

The absence of an original symbol name is harmless when inputs, outputs, state changes, ordering, ownership and failure behavior are understood. Conversely, a named function is not closed if an important wire or asset field remains semantically unresolved.

## Audit classes

| Class | Meaning | Build consequence |
|---|---|---|
| A — behavior confirmed | Replacement-relevant contract is evidence-backed; provisional name may remain | no blocker |
| B — behavior strong | Principal role is clear; one subordinate label/order/detail remains | implement only after targeted closure or bounded test |
| C — material unknown | Wire, asset, lifecycle or gameplay meaning could change compatibility behavior | pre-build analysis priority |
| D — original-name-only unknown | Behavior is known; only exact historical C++ name is absent | no blocker |
| E — irrelevant implementation machinery | CRT/STL/thunk/allocator/backend detail with no independent behavior to preserve | deliberately redesign/omit |

## Current material-unknown queue (2026-09-22)

This short queue supersedes the older milestone table retained below for audit history.

| Priority | Boundary | Current unresolved item | Why it matters |
|---:|---|---|---|
| closed | DSEF capital-ship destruction | top scalar is the initial split-delay seed; effect floats are nominal reference lengths; SDAS float controls spatial coupling; SDOE fields are object template, appearance fallback and parent hardpoint | loader, prepared handles, constructor countdowns and runtime transform/scale equations close the material contract |
| closed | CDF contrails | A is the overrideable contrail name/key and D is the contrail name/key overridden by the child record | child-first recursion, ordered-set insertion-before-lookup and matched component/parent corpus pairs close the behavior |
| closed | static-collidable flora determinism | each 16-metre cell uses a zeroed stack-local shuffled Park–Miller state seeded with the negative linear cell key | raw ECX flow closes state ownership and exact X/Z jitter, yaw and optional-scale sample order |
| closed for Stardust.exe compatibility | specialized presentation records | PALV and OBJT are skipped unknown WEAR children; VLEF is skipped by the top-level CDF dispatcher | observed schemas may be retained as disabled extensions but no active field semantics exist in this executable |
| closed | asynchronous lifecycle audit | file streamer, general async loader and procedural-terrain generation/scene teardown are closed | caller-synchronous streamer ownership, callback-null cancellation, main-thread publication and stop/signal/join teardown are all evidence-backed |
| 3 | UI/gameplay long tail | unvisited optional mediators, commands and specialty workflows | breadth required for a complete client, but not a protocol/architecture blocker |

The overnight continuation reduced this final breadth category through twenty
bounded workflow analyses. No new Class C wire-format, asset-format, replication,
or core lifecycle unknown was uncovered. Remaining work in this category is
primarily exhaustive feature coverage, original-client fidelity and later dynamic
validation; it is not a hidden prerequisite for selecting the replacement engine.

Recently closed items must not be re-added: GLOW RGBA/size profile, WING duration,
Rotation/Seesaw/Spring child fields, TURR projectile speed/lifetime, contrail B/C
hardpoints and width, point/flicker light fields, clear-flora circles, barrel recoil
and interpolated engine audio.

## Historical material-unknown queue

This queue is intentionally short and should shrink as blocks close. It is not every row containing the English word “unknown.”

| Priority | Boundary | Current unresolved item | Why it matters |
|---:|---|---|---|
| closed | Login cluster records | footer is a separately registered LE32 consumed but ignored by the handler; status-record final byte is copied as recommendation; `+0x24` is status and `+0x1C` is ignored | wire compatibility is fully specified for the current server; only two historical field names remain unknown |
| closed | PLAY:9 replication | indices 14 and 15 are U32 fields with trivial getters and no direct call sites anywhere in Stardust.exe; current Stardust emits zero | preserve/decode as reserved U32 values; no active client behavior exists to reproduce in this build |
| closed | object-controller registry | fourth argument strongly correlates with locally/client-originatable capability, but its sole lookup accessor has no code or pointer references | dormant metadata in this executable; it does not enforce dispatch, reliability, permissions, or forwarding |
| closed | replication failure behavior | invalid delta index is unchecked; bounded reads throw; earlier in-place mutations survive; deferred callback commit is skipped; archive service logs/rethrows and process termination follows | legacy behavior fully characterized; replacement must prevalidate and apply transactionally, then reject/disconnect without process crash |
| closed for architecture | fixed-function assets | PFFP is three vertex-lighting enables plus four D3D material-source selectors; v0000's removed U32 is confirmed legacy material tag (`MAIN` in its sole effective record) | exact historical order/name of the four source channels remains Strong rather than Confirmed; preserve raw order and compile through compatibility shader |
| Resolved | effect assets | EFCT/0001 DATA byte 1 is `containsPrecalculatedVertexLighting` | confirmed by loader, virtual slot, and exported method symbol; preserve in asset IR |
| closed | animation actions | BasicAnimationAction byte `+0x28` is a binary-only legacy flag; getter/setter have no runtime consumers, XML omits it, and all 16,617 ACTN records across configured archive copies store zero | preserve/decode as reserved false-compatible byte; it does not alter action transitions in this client/content set |
| closed | environment ramps | all eight 256×8 rows are mapped to their executable consumers: four named light roles plus main secondary bank, repeated-CELS opacity, fallback sky/principal-celestial state, fog/sky-family state, and shadow-volume RGBA | only unpublished editor-facing labels remain; replacement behavior is specified |
| closed | ENVM repeated CELS data | two shader/size pairs, yaw, pitch, signed revolutions and animation period are mapped; per-frame equation is confirmed across 116 deployed records | original editor labels only; base replacement contract is specified |
| closed for Stardust.exe compatibility | ENVM extension policy | TLOK, SKYB and DIST schemas are recovered; executable-wide immediate audit proves this Stardust.exe has no recognizer and its ENVM path does not implement them | preserve in asset IR and keep enhancement disabled by default; later rendering is a new capability requiring separate validation, not an unknown function in this executable |
| closed | optional SOE transforms | selectors 0-4 are byte-level mapped; selector 2 is the concrete provider's identity pair and selector 3 is generated-pad XOR plus ciphertext chaining with pad-only tail bytes | unpublished historical labels remain, but numeric behavior and golden vectors are sufficient for interoperability |
| closed | ClientChunk 16-bit grids | `+0xCC` and `+0xD0` are respectively `FloraDynamicNear` and `FloraDynamicFar`; common sampler `0x00862720` resolves packed family/selection plus `+0xC4` color | prior “classification” queue item was stale; near/far mode label only is unpublished |
| closed for architecture | PTAT WMAP/SMAP | exact MAPS/0001 fields, centered LSB-first bit layout, point/rectangle operations and three live WMAP consumers are mapped; server names SMAP as seed map but no direct client consumer exists | decode/retain both; reproduce WMAP behavior; do not invent dormant SMAP behavior |
| closed | tentative startup rows | `misc/override.cfg` ingestion, `SetupClientAnimation::install`, PlaybackScriptManager exit cleanup, GameFeatureBits getter, and `SetupClientDirectInput::install` are now tied to direct strings and mechanics | no unidentified startup function remains in the address ledger |
| closed | ObjectController absent inner type | factory logs the numeric type and returns null; outer parser records telemetry; dispatch route makes no object mutation when it cannot deliver | replacement should explicitly reject/discard rather than preserve nullable payload ownership |
| closed | NPC conversation loop | ObjectController `0xDD`–`0xE0`, exact payloads, manager transitions, space CRC path and outbound zero-based `npcConversationSelect` are mapped end to end | screenplay trees/localization keys are content breadth, not unknown protocol functions |
| closed | character-sheet response family | all five subscribed messages are routed; exact current-server payloads and local-player guards are mapped; money is proven parse-and-discard in this listener | `0x0F` in the badge stream should be preserved as the legacy terminal/high-index value; its unpublished historical enum name is not behaviorally material |
| closed | group space-launch invitation | `0x436`–`0x439` are mapped through both client directions, consent state and server validation; client/server trailing-field asymmetry is explicit | prompt localization wording and individual subscriber identities are UI breadth, not an unknown protocol contract |
| closed | group loot lottery | `0x43C/0x43D/0x43F` cover open, close and all/selected/none ballot submission; exact vector codec and server session/draw policy are mapped; `0x43E` is an unsupported reserved gap | visual item-list composition remains ordinary mediator breadth, not a wire or session blocker |
| closed | image-design session | `0x23A` start and bidirectional `0x238/0x239` change/reject routes are mapped; full variable-length `ImageDesignData` matches server parsing/insertion and client codec | exact historical labels for two UI-side role events are naming-only; active state and identity predicates are known |
| 3 | UI/gameplay long tail | representative mediators, commands and specialized controller messages | breadth rather than one architectural blocker |
| closed | customization-manager tables | CIDM and all twelve ACST chunks, indices, recursive precedence, type selection, and effective corpus are decoded and validated against client and server | palette-file content is a separate remaining asset boundary; malformed CIDX tail is documented |
| closed | palette resource | RIFF grammar, limits, RGB-to-opaque-ARGB conversion, flags-byte treatment, index clamp, cache fallback, and all effective assets are validated | palette-column permissions remain a separate data-table boundary |
| closed | palette-column policy | all 15 DTII fields, 37-row corpus, seven-tier selection, exclusive limits, UI consumers, and server validation state are mapped | `customization_data.iff` is a separate presentation/linkage boundary |
| closed | customization presentation data | all 18 DTII fields, 467-row corpus, species/group hierarchy, height-scale exception, and top-level creation/image-design route are mapped | exact widget geometry/localized labels are UI breadth |
| closed | customization group instances | all six fields, 96-row corpus, composite-key loader and lookup are mapped | historical internal container names are not behaviorally material |
| closed | equipment slot rules | SRMG/BIND grammar, SLOT/NEGT/ANDT/ORTS AST, all four shipped expressions, factory registry, live object consumer and derived-variable insertion are mapped | concrete customization-owner C++ class name remains Strong rather than Confirmed |
| closed | CKAT/KFAT QCHN channel | exact compressed record, 11/11/10 sign-magnitude split, quantizer selectors, root-versus-joint placement and `[w,x,y,z]` result are closed; reconstructed nonnegative component is `w` | Godot adapter fixtures now pass with Z reflection and quaternion mapping `[-x,-y,z,w]`; see the 2026-09-22 coordinate checkpoint |
| closed | SMAT SFSK/APAG policy | SFSK bypasses intermediate skeletal detail index 1 beyond the near threshold; APAG bypasses the normal age test for periodic animation/action generation; all nine deployed assets are identified | original acronym expansions are naming-only |

## Not material unknowns

- `global_wind_set_direction_VA_00844AA0`: complete static direction/magnitude/callback service is confirmed; original namespace spelling is irrelevant.
- `client_command_queue_remove_apply_VA_0046E720`: fields formerly called `tab1/tab2` are confirmed as seven-way result category and category-specific detail; behavior is closed.
- `startup_misc_override_load_VA_00401000`: later analysis supersedes an early tentative “resource probe” label.
- `client_chunk_sample_environment_family_VA_008628E0`: the latest block confirms the field and consumer; older tentative duplicate rows are stale.
- Functions marked “identify RTTI,” “name every subscriber,” or “recover original enum label” are not automatically blockers when mechanics and resulting state are already confirmed.

## Policy for future blocks

Every analysis block will:

1. identify which Class B/C items it closes;
2. upgrade or consolidate the corresponding provisional row;
3. add a new item only when evidence demonstrates a genuinely distinct compatibility boundary;
4. avoid adding compiler/library helpers unless they expose a wire, asset, lifecycle, threading or failure contract;
5. report Class C count separately from total provisional-address coverage.

This keeps the research honest: uncertainty remains visible, but confirmed reverse-engineered behavior no longer masquerades as “unknown.”

## 2026-09-22 07:00 reconciliation

The latest continuation retires material uncertainty in sixteen workflow and
presentation areas. Consent, transition, vehicle tuning, ship diagnostics,
community composition and generic text input are local services rather than
invented packet surfaces. Examine reuses the attribute service; ticket forms
reuse the ticket message; waypoint naming delegates to the known waypoint
route. The remaining Class-C queue is integration and rare-behavior breadth,
not a hidden core handshake, replication, asset-loader or game-loop blocker.
See `ANALYSIS_SESSION_2026-09-22_TO_0700.md`.

## Closed 2026-09-21: procedural-terrain generation lifetime

The remaining terrain worker lifecycle item is closed at the architectural boundary. Work/results are serialized into one active cycle, published on the main thread, and protected during destruction by stop/signal/join ordering. There is no unmodeled stale-result callback or generation-token protocol. Historical helper and member names remain naming-only uncertainty. See `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_GENERATION_LIFECYCLE.md`.

## Closed 2026-09-21: effective STAG trailing enum

The trailing STAG byte is no longer a material original-asset unknown. Corpus-exclusive usage closes values 0/3/4/5 as ordinary, reflection/environment and two scroll-coordinate profiles. Values 1/2 do not occur in the effective installation and will be preserved with diagnostics rather than assigned invented behavior. See `ANALYSIS_CHECKPOINT_2026-09-21_STAG_COORDINATE_GENERATION_ENUM.md`.
