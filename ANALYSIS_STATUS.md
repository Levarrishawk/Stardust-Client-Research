# Client Analysis Status

Updated: 2026-09-22

This is a heuristic planning estimate, not a mechanically measurable completion counter. “Complete” means enough evidence to constrain a clean replacement architecture and make the pre-build engine decision; it does not mean every function in Stardust.exe has been named.

## Current estimates

- Overall implementation-grade, replacement-relevant behavioral/specification research: **96%**.
- Expanded comprehensive whole-client ledger: **90%** (see `COMPREHENSIVE_ANALYSIS_LEDGER.md`).

## 2026-09-21 reconciliation snapshot

- The current Stardust server packet-source inventory is fully classified in the matrix: **345 documented / 0 reachable gaps**. This is protocol-inventory closure, not a claim that all client-side optional behavior is understood.
- The normalized stable-name inventory contains **2,151 distinct addresses** with no duplicate address rows. The chronological Markdown ledger remains append-only because later prose closures do not erase historical observations.
- The latest analysis series reconstructs client UI mediator lifecycle, camera families, input and command tables, diagnostics/crash policy, pools and threading boundaries, client effects/events, collision routing, weapon/ship presentation, and the core `ClientDataFile` grammar/application lifecycle.
- Original-asset compatibility remains mandatory. The CDF work now covers composition, damage/destruction, wearables, wings, contrails, turrets/flags, lights, clear-flora zones, weapon-barrel recoil, and engine-audio interpolation using executable code plus effective TRE/IFF corpus evidence.
- Remaining material work is dominated by specialized presentation-field semantics, long-tail UI/gameplay workflows, exhaustive lifecycle/ownership auditing, and confirmation of rare asset extensions. It is not dominated by unidentified server packet classes.

## 2026-09-22 overnight continuation

Twenty bounded blocks closed additional session, UI, space, command, quest, group,
customer-service and object-controller behavior. The highest-value new protocol
closures are biography request/update, droid command programming subtype `0x435`,
ground-group command routing and the exact local/replicated inputs of the toolbar,
skills, experience, combat-queue, radar and status widgets. The session also
isolated four server/client compatibility defects or omissions rather than
mistaking them for unexplained executable behavior: quest counter/timer response
shape, formation command handling, ship-repair behavior and the malformed verified
name response used by harassment reporting. See
`ANALYSIS_SESSION_2026-09-22_OVERNIGHT_TO_0500.md`.

## 2026-09-22 continuation through 07:00

Nineteen bounded areas close stat migration, image design, character sheet,
permission lists, mission details, consent, examine/inventory information,
community composition, planet-map presentation, ticket and bug UI, vehicle
tuning, transition overlay, ship diagnostics and generic text input, plus the
documentation reconciliation, local notepad persistence, keypad/money widgets and generic transfer. The stable-name registry now contains **2,034
distinct addresses / 0 duplicates**, and the date contains **76 checkpoint
reports**. See `ANALYSIS_SESSION_2026-09-22_TO_0700.md`.

The latest continuation closes the terrain environment-family selector, its exact `(familyId<<16)|weather` lookup and same-family weather-zero fallback, the full 25-column outdoor descriptor, its five-second visual transition and the separate ENVM celestial asset. The configured corpus contains 50 valid effective environment tables with 1,337 rows and 290 family groups. See `ANALYSIS_CHECKPOINT_2026-09-12_ENVIRONMENT_FAMILY_SELECTION.md`.

The latest packet continuation classifies `ChatOnGetIgnoreList` (`0xF8C275B0`) as a legacy snapshot unsupported by this executable and confirms `PLAY:9[8]` as the active structural path for initial and incremental ignore state. The packet matrix is **243 documented / 102 reachable gaps / 36 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_GET_IGNORE_LIST.md`.

The following block closes `ChatOnChangeIgnoreStatus` (`0x70E9DA0F`) as an unsupported legacy add/remove notification, confirms its full server wire contract and producer ordering, and establishes the view-9 delta plus localized system message as the effective client behavior. The packet matrix is **244 documented / 101 reachable gaps / 35 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_CHANGE_IGNORE_STATUS.md`.

The friend-mutation continuation closes `ChatOnAddFriend` (`0x2B2A0D94`) and `ChatOnChangeFriendStatus` (`0x54336726`) as legacy messages absent from this client, retaining `PLAY:9[7]` as authoritative membership and the mapped presence message as an independent channel. The packet matrix is **246 documented / 99 reachable gaps / 33 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_FRIEND_MUTATION_LEGACY.md`.

The chat-session continuation closes `ChatOnConnectAvatar` (`0xD72FE9BE`) as a zero-payload readiness event that coalesces a deferred friend/ignore refresh in CommunityManager. The packet matrix is **247 documented / 98 reachable gaps / 32 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_CONNECT_AVATAR.md`.

The chat-status continuation closes dormant duplicate-opcode definitions `ChatServerStatus` and `unkByteFlag` (`0x7102B15F`): one-byte server schemas, no active producers and no implementation in `Stardust.exe`. The packet matrix is **248 documented / 97 reachable gaps / 31 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_SERVER_STATUS.md`.

The holocron continuation closes `OpenHolocronToPageMessage` (`0x7CB65021`) and `CloseHolocronMessage` (`0xC0938A9D`) through the executable's broad UI listener, page parser/presentation path and mediator deactivation path. The packet matrix is **250 documented / 95 reachable gaps / 29 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_HOLOCRON_UI_MESSAGES.md`.

The tutorial-trigger continuation closes the server request (`0x90DD61AF`) and client echo (actual callback opcode `0xCA88FBAD`) through the UI listener, action-key flags, distributed event emitters and server observer routing. The packet matrix is **252 documented / 93 reachable gaps / 27 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_NEWBIE_TUTORIAL_TRIGGER_PROTOCOL.md`.

The generic-chat continuation closes `ConGenericMessage` (`0x08C5FC76`) through its readiness-gated chat listener, parser, Unicode conversion and presentation routing. The packet matrix is **253 documented / 92 reachable gaps / 26 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CON_GENERIC_MESSAGE.md`.

The object client-event continuation classifies `PlayClientEventObjectMessage` (`0xAF83C3F2`) as a dormant unsupported definition, preserves its wire schema and explicitly prevents conflation with the active client-effect path. The packet matrix is **254 documented / 91 reachable gaps / 25 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_PLAY_CLIENT_EVENT_OBJECT.md`.

The inactivity continuation closes `ClientInactivityMessage` (`0x0F5D5325`) from configured input timer/activity transitions through reliable client send and Stardust's one-byte, log-only callback. The packet matrix is **255 documented / 90 reachable gaps / 25 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CLIENT_INACTIVITY.md`.

The player-resource continuation closes payload-free stomach (`0xB75DD5D7`) and money (`0x9D105AA1`) queries, their shared client sender, and the differing replication versus legacy-response paths. The packet matrix is **257 documented / 88 reachable gaps / 25 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_PLAYER_RESOURCE_QUERIES.md`.

The `unkZone` continuation classifies opcode `0x58C07F21` as a dormant test packet with four literal payload bytes, no producer and no executable consumer. The packet matrix is **258 documented / 87 reachable gaps / 24 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_UNK_ZONE.md`.

The customer-service continuation closes cancel-ticket, knowledge-base article, and both directions of new-ticket activity through their executable codecs/listener destinations and Stardust schemas. The packet matrix is **262 documented / 83 reachable gaps / 20 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CUSTOMER_SERVICE_RESPONSES.md`.

The next customer-service block closes create-ticket and ticket-list responses, including a confirmed two-field client/one-field dormant-server mismatch and the complete ticket record envelope. The packet matrix is **264 documented / 81 reachable gaps / 18 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_TICKET_CREATE_LIST_RESPONSES.md`.

The 2026-09-13 two-hour marathon completed seven bounded units. EMTR 0010-0014 flocking/cage fields, environment-color influence, collision/wind/local-space controls, and version-sensitive 39-versus-35-byte tail boundaries are now explicit. The legacy quad path is confirmed as six CPU-expanded dynamic vertices per particle, while PTMH references are proven to span APT/MSH/SAT and do not receive PTCL quad tint/alpha. A 345-class server packet cross-reference produced the first persistent gap matrix; ChatSystem, two PlayClientEffect messages, and Create/DestroyClientPath were then closed end-to-end. See the 2026-09-13 flocking, collision/wind, quad-batch, PTMH policy, packet-matrix, client-effect and client-path checkpoints.
- Defined pre-build engine/POC decision analysis: **100% complete**.
- Stable-name coverage is tracked separately in the Ghidra inventory and remains well below one percent of raw function objects; the raw inventory contains many tiny thunks/helpers and is not a replacement-readiness denominator.

These denominators are intentionally separate. See `ANALYSIS_CHECKPOINT_2026-09-11.md` for the weighted model and `ANALYSIS_CHECKPOINT_2026-09-11_GHIDRA.md` for the latest evidence.

The requested four-hour block on 2026-09-11 was interrupted after approximately 1 hour 48 minutes. `ANALYSIS_CHECKPOINT_2026-09-11_BLOCK3_INTERRUPTED.md` records the completed units and exact resume point; it is not represented as a full four-hour checkpoint.

The subsequent one-hour continuation completed the SharedNetwork option-layout and connection-lag/selection-timeout unit. `ANALYSIS_CHECKPOINT_2026-09-11_ONE_HOUR_CONTINUATION.md` records the evidence and remaining lower-transport questions.

The transport-policy block proved the normal UDP factory timeout is zero, confirmed the real TCP alternative, and recovered authenticated UDP endpoint remapping. `ANALYSIS_CHECKPOINT_2026-09-11_TRANSPORT_POLICY.md` records the evidence and unresolved deployment discrepancy.

The core controller-effects block confirmed the player/creature/common fallback dispatcher, mapped representative combat/chat/animation/posture effects, and exposed the legacy single-defender CombatAction limit. `ANALYSIS_CHECKPOINT_2026-09-11_CORE_CONTROLLER_EFFECTS.md` is the current checkpoint.

The VFS/TRE runtime block proved executable-level mount ordering, logical-path normalization, archive tombstones, CRC-plus-string lookup, and stored versus eagerly decompressed payload handling. `ANALYSIS_CHECKPOINT_2026-09-11_VFS_TRE_RUNTIME.md` is the current checkpoint.

The filename-identity/IFF-factory block fixed the exact CRC algorithm against 2,285 installed records and recovered the appearance tag registry plus APT indirection path. `ANALYSIS_CHECKPOINT_2026-09-11_ASSET_IDENTITY_AND_FACTORY.md` is the current checkpoint.

The MESH-boundary continuation recovered versions 0002-0005, legacy versus APPR-derived bounds, SPS versions 0000/0001, and named transform hardpoints. `ANALYSIS_CHECKPOINT_2026-09-11_MESH_BOUNDARY.md` is the current checkpoint.

The SPS/VTXA block recovered shader grouping, primitive/index envelopes, vertex declaration families and the installed MSH version profile. `ANALYSIS_CHECKPOINT_2026-09-11_SPS_VTXA.md` is the current checkpoint.

The topology/shader-envelope block closed all twelve primitive topology IDs, proved SIDX view-selection behavior, traced SHT into EFCT/IMPL/PASS version dispatch, and classified all 171 mesh anomalies as tombstones. `ANALYSIS_CHECKPOINT_2026-09-12_TOPOLOGY_SHADER_ENVELOPE.md` is the current checkpoint.

The PASS/corpus block recovered the PASS 0009 serialized superset and PFFP/PVSH/PPSH/texture-stage branches, inventoried every installed SHT/EFT occurrence, and exposed later-patch PASS 0010 and IMPL 0008/0009 compatibility risk. `ANALYSIS_CHECKPOINT_2026-09-12_PASS_AND_SHADER_CORPUS.md` is the current checkpoint.

The PASS-consumer block crossed the engine/rasterizer ABI, recovered the compiled 0xA4-byte backend-pass lifecycle, confirmed material/factor/scroll/alpha/stencil fields, and closed texture-stage version widths. `ANALYSIS_CHECKPOINT_2026-09-12_PASS_D3D9_COMPILER.md` is the current checkpoint.

The render-state/overlay continuation mapped the core PASS vector to exact D3D9 state IDs, recovered the sampler compiler, and proved that 27 IMPL 0009/PASS 0010 assets are live overlay winners. `ANALYSIS_CHECKPOINT_2026-09-12_FIFTY_PERCENT.md` is the current checkpoint.

The fixed-function STAG continuation recovered the exact deployed 0000 record width and field groups, measured all 951 effective records, and established that the recovered active s207 pass implementation is programmable while its D3DTSS applicator is unreachable in the deployed DLL. `ANALYSIS_CHECKPOINT_2026-09-12_STAG_FIXED_FUNCTION.md` is the current checkpoint.

The texture-binding continuation connected TXMS/TXM versions through tag-keyed StaticShader data to named, per-material and global-dynamic texture sources, exact sampler ordering and D3D9 binding. `ANALYSIS_CHECKPOINT_2026-09-12_TEXTURE_BINDING.md` is the current checkpoint.

The DDS continuation traced the CRC cache, header/format/mip logic, managed D3D9 allocation and face/level upload, then classified all 30,109 effective `.dds` namespace winners. `ANALYSIS_CHECKPOINT_2026-09-12_DDS_TEXTURE_RESOURCE.md` is the current checkpoint.

The dynamic-target continuation recovered solid/memory/procedural texture factories, concrete post-processing/UI/environment users, the raster readback/copy helper, and paired pre/post-reset resource lifecycle. `ANALYSIS_CHECKPOINT_2026-09-12_DYNAMIC_RENDER_TARGETS.md` is the current checkpoint.

The frame-schedule continuation fixed the raster API slot alignment, proved BeginScene/IoWin/EndScene/Present order, connected presentation to reset recovery, and bounded the twelve ShaderPrimitiveSorter phases. `ANALYSIS_CHECKPOINT_2026-09-12_FRAME_SCHEDULE.md` is the current checkpoint.

The GroundScene/bloom/CUI continuation proved that CuiIoWin captures only the lower scene into bloom, then runs GroundScene post rendering, root CUI drawing and a final overlay in that order. It also confirmed TerrainObject and its appearance render boundary. `ANALYSIS_CHECKPOINT_2026-09-12_GROUNDSCENE_BLOOM_CUI.md` is the current checkpoint.

The procedural-terrain continuation resolved the concrete ClientProceduralTerrainAppearance vtable, corrected its environment-color getter, and traced camera callbacks through visibility traversal, five-bucket terrain batching and ShaderPrimitiveSorter submission. `ANALYSIS_CHECKPOINT_2026-09-12_PROCEDURAL_TERRAIN_SUBMISSION.md` is the current checkpoint.

The water continuation resolved the global/local managers and four primitive classes, confirmed WATR phase registration, separated static/dynamic global grids from local bounded water, and found no separate reflection/refraction render pass in those paths. `ANALYSIS_CHECKPOINT_2026-09-12_WATER_RENDERING.md` is the current checkpoint.

The asynchronous terrain-chunk continuation recovered request publication, the dedicated worker and pooled ClientChunk build, three-state main-thread reconciliation, stale-result disposal, region/LOD rebuild triggering, and stop/signal/join teardown. `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_CHUNK_PIPELINE.md` is the current checkpoint.

The terrain LOD-policy continuation recovered deployed configuration defaults, the distance/relief-bias equation, integer observer cadence, quadtree split/collapse behavior, edge-stitch masks, retained chunk caching and the distinct dirty-region regeneration path. `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LOD_POLICY.md` is the current checkpoint.

The ClientChunk mesh/blend continuation recovered the nine-vertex cell primitive, cached one-to-four-layer terrain materials, all nine exact seam index patterns, separate occlusion representations, and the per-shader CPU batching boundary. `ANALYSIS_CHECKPOINT_2026-09-12_CLIENTCHUNK_MESH_BLEND.md` is the current checkpoint.

The terrain collision/query continuation recovered four-way-tree broad phase, stitched-triangle segment collision, nearest hit point/normal/distance, vertical height and normal lookup, retained-grid query boundaries and strict separation from occlusion geometry. `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_COLLISION_QUERIES.md` is the current checkpoint.

The terrain auxiliary-grid continuation tied generated channels to static-collidable, static-noncollidable, dynamic-near and dynamic-far flora, packed color, environment and normals; it also bounded WMAP/SMAP uncertainty. `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_AUXILIARY_GRIDS.md` records the evidence.

The flora-runtime continuation resolved the static and dynamic radial manager classes, exact category ownership, camera-relative residency, smooth fading, time-driven motion and 500-quad material batching. `ANALYSIS_CHECKPOINT_2026-09-12_FLORA_RUNTIME.md` is the latest completed block.

The terrain-lighting/static-flora continuation corrected `ClientChunk +0xA8` to a binary baked-lighting/shading mask and recovered the separate deterministic 16 m static-collidable flora object path. `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LIGHTING_STATIC_FLORA.md` records the evidence.

The interior-floor continuation recovered every `FLOR` loader version, stored `BTRE/BEDG/PGRF` use, 100-byte runtime triangles and the cross-floor movement solver. The portal/door continuations then recovered `PRTO/CELL/PRTL` version dispatch, cell-floor ownership, geometric portal crossing, data-driven door construction and the proximity-driven live door state machine. See `ANALYSIS_CHECKPOINT_2026-09-12_INTERIOR_FLOOR_RUNTIME.md`, `ANALYSIS_CHECKPOINT_2026-09-12_PORTAL_DOOR_RUNTIME.md` and `ANALYSIS_CHECKPOINT_2026-09-12_DOOR_STATE_MACHINE.md`.

| Workstream | Decision-gate status | Basis |
|---|---:|---|
| Executable/platform inventory | Complete | binary identity, imports, entry/lifecycle, configuration, RTTI and subsystem boundaries established |
| Login/SOE/early scene protocol | Complete | implementation specification and test vectors exist; live exchange belongs to POC |
| Object lifecycle/replication architecture | Complete | envelopes, view/container mechanics, critical handlers and representative ground/space schemas constrain design |
| Asset/resource architecture | Complete | overlay VFS and representative end-to-end dependency corpus define importer/runtime requirements |
| Renderer decision inputs | Complete | dynamic raster/D3D9 boundary, semantic phases and SHT/EFT translation unit recovered |
| World/terrain/cells/transitions | Complete | representative PTAT and PRTO/FLOR models plus generation/readiness safety invariant established |
| Character/customization/animation | Complete | representative chain, exact customization grammar and wearable ownership define required engine spike |
| UI/input/audio/jobs/memory | Complete | legacy backends and replacement responsibilities bounded; detailed content remains incremental |
| Engine-evaluation specification | Complete | common candidates, fail gates, four spikes and weighted evidence matrix documented |

## Why the estimate advanced

The latest continuation distinguished the 600-second avatar-selection response timeout from SharedNetwork's millisecond reliability timers, recovered the 136-byte default connection-options layout, and confirmed the complete periodic lag-report exchange against Stardust server source. The preceding block completed the containment-driven wearable path, several derived-object replication boundaries, platform/session handlers and malformed-archive propagation.

`FINAL_PREBUILD_ANALYSIS_REPORT.md` is the authoritative completion report. Exhaustive function-level reverse engineering remains out of scope and is explicitly quantified rather than hidden by the 100% decision-gate label.

## Engine/POC status

The user selected Godot for a bounded proof of concept after the initial engine review. That choice is provisional: it demonstrates host-engine feasibility and does not authorize a full client build. Substantial implementation still waits on further Stardust.exe analysis and review of the accumulated evidence.

The minimum networking proof of concept remains intentionally engine-independent at its protocol core. Godot should host it through a narrow adapter rather than own wire parsing or replication state.

## Latest continuation

`ChatOnGetFriendsList` (`0xE97AB594`) is now classified and closed as a redundant legacy message unsupported by this executable: exhaustive PE name/opcode/RTTI/registration searches and adjacent-DLL scans find no parser or listener. Stardust sends owner U64 plus counted ChatAvatarId triples, compression-enabled, but immediately also sends `PlayerObjectDeltaMessage9`; the executable registers `PLAY:9[7]` and `[8]` as friend/ignore string vectors, and Stardust uses index 7 for continuing add/remove deltas. `ChatFriendsListUpdate` independently supplies presence. A replacement should preserve PLAY replication and either explicitly ignore or idempotently consume the legacy snapshot. The refreshed matrix is **242 documented / 103 reachable gaps / 37 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_GET_FRIENDS_LIST.md`.

`ChatSendToRoom` / callback and `ChatOnSendRoomMessage` (`0xE7B61633`) are now closed: sender `0x00A289A0`, request constructor `0x00AFB960`, sequence insert/resolve wrappers `0x00A6FA60`/`0x00A6FC10`, acknowledgement parser `0x00B062B0` and result handler `0x00A2D9E0`. The request contains message Unicode, a compatibility-critical empty reserved Unicode field, room ID and U32 sequence. Result zero is silent; errors 9/16/fallback publish exact prose. Stardust broadcasts success acknowledgements to the room separately from ignore-filtered `ChatRoomMessage` delivery and has several no-ack early returns, requiring harmless unmatched-success handling and explicit timeout policy. The refreshed matrix is **241 documented / 104 reachable gaps / 38 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_SEND_ROOM_MESSAGE.md`.

`ChatOnUninviteFromRoom` (`0xBE33C7E8`) and `ChatUninviteFromRoomCallback` are now closed: outbound constructor `0x00AFC990`, inbound parser `0x00B059D0`, dedicated pending resolver `0x00A6FF10` and handler `0x00A2DEC0`. The response decodes path/actor/target/result/request but trusts correlated pending target/room context; success passes add=false to the now-confirmed bidirectional invited-membership setter, emits `InviteeRemoved` and refreshes the tree. Exact result prose covers success, avatar, room, privacy, not-invited, permission and fallback cases. Stardust expands success into optional de-op/kick, invitation removal, acknowledgement, query snapshot and target browser-room destruction. The refreshed matrix is **239 documented / 106 reachable gaps / 39 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_UNINVITE_ROOM.md`.

`ChatOnReceiveRoomInvitation` (`0xC17EB06D`) is now closed: listener `0x00A6E9C0`, parser `0x00B06600` and handler `0x00A2D3B0`. The target client requires the room record to exist, adds its own normalized avatar to the cached invitee set, publishes `InviteeAdded` with the inviter retained as actor, refreshes the room tree and displays `ui_chatroom:invited_prose`. Special group/guild room branches publish retained room-ID changes instead. Stardust confirms the room record is sent before the notification in both private-room invitation and moderator-promotion flows. The refreshed matrix is **237 documented / 108 reachable gaps / 40 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_RECEIVE_ROOM_INVITATION.md`.

`ChatOnInviteToRoom` (`0x493FE74A`) and `ChatInviteToRoomCallback` are now closed: parser `0x00B05EA0`, result/request-only listener handoff, pending-context resolver `0x00A6FD90`, handler `0x00A2DB50`, success invitee insertion, observer/tree update and exact result prose. Stardust confirms invitation is a compound unban/private-room exposure/target notification/request acknowledgement/query-snapshot transaction. The separate target-facing room-invitation notification remains the next focused block and is deliberately not counted yet. The accurate matrix is **236 documented / 109 reachable gaps / 41 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_INVITE_TO_ROOM.md`.

`ChatQueryRoomResults` (`0xC4DE864E`) and `ChatQueryRoomCallback` are now closed: parser `0x00AFF190`, handler `0x00A2B520`, four authoritative ChatAvatarId arrays, shared nested room codec `0x00B045C0`, room merge/new-room handling, complete set replacement, observer/tree reconciliation and `query_received` publication. The request ID is decoded but not used to gate semantic application, matching request-zero Stardust broadcasts; missing query targets yield no server reply and require client timeout policy. The matrix is **234 documented / 111 reachable gaps / 42 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_QUERY_ROOM_RESULTS.md`.

`ChatOnLeaveRoom` (`0x60B5098B`) and `ChatLeaveRoomCallback` now close the inverse membership path: parser `0x00B08950`, handler `0x00A2C040`, fixed four-field notification, cached member removal, local active-channel teardown versus other-player departure event, special-room cleanup gates and tree refresh. Paired enter/leave evidence upgrades `0x00A6A440` to a confirmed bidirectional member setter. Stardust uses the same outbound request shape for voluntary leave and authenticated moderator kick. The matrix is **232 documented / 113 reachable gaps / 43 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_LEAVE_ROOM.md`.

`ChatOnEnteredRoom` (`0xE69BDC0A`) and `ChatEnterRoomByIdCallback` are now closed at the protocol/state boundary: parser `0x00B08D60`, success handler `0x00A2BAF0`, failure handler `0x00A2D1D0`, exact request/response ordering, active Stardust permission results, cached member insertion, self-versus-other event split, pending correlation, active-room registration and tree refresh. System-room subhelpers remain bounded internal refinements rather than wire unknowns. The matrix is **230 documented / 115 reachable gaps / 44 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_ENTERED_ROOM.md`.

`ChatOnBanFromRoom` (`0x5A38538D`) and its request callback are now closed against the executable's `ChatOnBanAvatarFromRoom`: parser `0x00B05160`, handler `0x00A2C9A0`, complete five-field layout, success insertion into the cached banned set, observer/tree refresh, and exact result prose. Paired ban/unban evidence upgrades `0x00A6A4A0` to a confirmed banned-membership setter. Stardust proves ban is a compound de-op, uninvite, kick, ban and private-room visibility-removal transaction. The matrix is **228 documented / 117 reachable gaps / 45 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_BAN_ROOM.md`.

`ChatOnAddModeratorToRoom` (`0x36A03858`) and its server-bound callback are now closed: executable parser `0x00B06FD0`, handler `0x00A2C3A0`, exact target/actor/result/path/request layout, successful cached-moderator insertion, observer/tree refresh, and the complete active failure localization table. Stardust additionally proves that promotion may orchestrate unban, private-room invitation/exposure and query-result updates before broadcasting moderator success. The matrix is **226 documented / 119 reachable gaps / 46 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_ADD_MODERATOR.md`.

`ChatOnUnbanFromRoom` (`0xBAF9B815`) is now closed against the executable's historically named `ChatOnUnbanAvatarFromRoom`: parser `0x00B04C90`, handler `0x00A2CC90`, full five-field wire layout, success removal from the cached banned set, room-tree refresh, and exact result/localization table for Stardust's errors 0/1/4/6/12/16. Both request-zero room broadcasts and request-correlated replies are accounted for, as is the server-bound callback request. The refreshed matrix is **224 documented / 121 reachable gaps / 47 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_UNBAN_ROOM.md`.

`ChatOnDestroyRoom` (`0xE8EC5877`) is now closed through parser `0x00B07490` and handler `0x00A2B100`. Stardust's successful result is an authoritative request-zero broadcast that removes the retained room and prunes its room-tree entry; ownership/permission failure preserves the initiating request ID and becomes localized `ui_chatroom:destroy_fail_prose`. Missing-room and inconsistent sequence/result combinations are diagnostic safe-no-ops. The refreshed matrix is **222 documented / 123 reachable gaps / 48 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_DESTROY_ROOM.md`.

The next chat block closes `ChatOnCreateRoom` (`0x35D7CC9F`) from its executable descriptor through parser `0x00B07FD0` and handler `0x00A2CF80`. The full shared room record, request correlation, successful room registry/tree insertion, localized error route and deliberately silent result-24 autojoin workaround are now cross-confirmed against active Stardust producers. The refreshed matrix is **221 documented / 124 reachable gaps / 49 literal-opcode gaps**; weighted coverage remains conservatively **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CHAT_CREATE_ROOM.md`.

The 2026-09-15 six-block session closed six active chat paths: friend presence, room message delivery, room-list reconstruction, moderator removal, outgoing tell acknowledgement, and incoming tell delivery. Exact parsers and downstream state/UI handlers are recorded, including ignore filtering, pending sequence correlation, room-tree reconciliation, and opaque trailing compatibility dwords. The matrix is now 220 documented / 125 reachable-gap candidates / 50 literal-opcode gaps, and weighted coverage is **78%**. See `ANALYSIS_SESSION_2026-09-15_SIX_CHAT_BLOCKS.md` and the six associated checkpoints.

The latest block closed `AuctionQueryHeadersResponseMessage` (`0xFA500E52`), including its three arrays, exact 72-byte compact item codec, 112-byte resolved record, search-type 2-9 list dispatch, shared cache reconciliation, pagination fields, transport-compression boundary, and typed list notifications. See `ANALYSIS_CHECKPOINT_2026-09-14_AUCTION_QUERY_HEADERS.md`.

The 2026-09-14 auction-action block closed `BidAuctionResponseMessage`, `RetrieveAuctionItemResponseMessage`, and `CancelLiveAuctionResponseMessage` from embedded message descriptors through parsers, `AuctionManagerClient` handlers, cache/list mutation, localized result selection, and typed UI publications. The cancellation packet's trailing byte is confirmed as consumed but semantically ignored; Stardust emits zero. See `ANALYSIS_CHECKPOINT_2026-09-14_AUCTION_ACTION_RESPONSES.md`.

The renderer has a confirmed outer frame envelope, semantic phase registry, named/procedural texture paths, reset ownership, exact lower-scene/bloom/GroundScene-post/CUI/final-overlay ordering, terrain/water submission, asynchronous chunk commit, seam topology and CPU material-batching boundary. Terrain includes surface collision, height/normal queries, typed flora/environment channels, static flora placement, WMAP behavior, and interior floor/portal/door movement; SMAP is losslessly retained but dormant. Animation connects the original SLOD/SKTM/LATT/CKAT/KFAT/ASHT assets through state/link/action selection, tracks, pose/root motion, cross-fades and modifiers. Audio includes the effective descriptor corpus, selection/loop/envelope runtime, gain separation, Miles boundary, streaming policy, complete PlayMusic routing, and confirmed interior/weather precedence. Remaining work is detailed optional UI/gameplay breadth and specialized presentation fidelity, not these superseded asset/runtime boundaries.

The latest one-hour continuation closed ASHT link/action queue-versus-replace policy, traced `PlayMusicMessage` into its two held channels, reconstructed `scene/game_music_manager.iff` and planet environment audio columns, and bounded category-13 reachability with an overlay-wide payload scan. See `ANALYSIS_SESSION_2026-09-12_ONE_HOUR_ASHT_MUSIC.md`.

The subsequent four-hour continuation completed 23 bounded blocks spanning
transport policy, terrain/environment dependencies, ten gameplay/UI protocol
families, customization/palette policy and the effective appearance/animation
corpus. QCHN packed decoding, SAT LDTB hysteresis, MLOD lazy mesh references and
SLOD inline skeleton ownership are now structurally closed. The current
weighted whole-client estimate is **74%**; replacement-readiness research is
**92%**. `FUNCTION_CLOSURE_SUMMARY.md` explains why the growing provisional
address ledger is not a growing list of unresolved functions.
The permission-list block closes `PermissionListCreateMessage` (`0x52F364B8`) from active structure/ship producers through the complete client codec and the `/PDA.PermissionList` mediator. Both counted Unicode vectors are retained in the replacement contract even though Stardust currently leaves the second empty. The matrix is **265 documented / 80 reachable gaps / 17 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_PERMISSION_LIST_CREATE.md`.

The stat-migration block closes `StatMigrationTargetsMessage` (`0xEFAC38C4`) as nine signed 32-bit HAM targets plus signed 32-bit points remaining. Parser `0x013F22B0` feeds mediator receive path `0x00DB96D0`, which replaces the model, refreshes and activates `WS_StatMigration`; active server session behavior is correlated. The matrix is **266 documented / 79 reachable gaps / 16 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_STAT_MIGRATION_TARGETS.md`.

The server-command block closes `ExecuteConsoleCommand` (`0xB1CFCE1C`) as one ASCII command routed by listener `0x00F38C20` through parser `0x013F51F0` into the internal game command/UI-action interpreter. Current Stardust uses `/conversationstop` and `/ui action questJournal`; replacement safety requirements explicitly forbid OS-shell routing. The matrix is **267 documented / 78 reachable gaps / 15 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_EXECUTE_CONSOLE_COMMAND.md`.

The tutorial-HUD block closes `NewbieTutorialEnableHudElement` (`0xCA375124`) as ASCII element key, U8 enabled and F32 blink count, mapped through parser `0x013F29E0` into semantic HUD show/hide and blink operations. C++ and Lua server producers are active. The matrix is **268 documented / 77 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_TUTORIAL_HUD_ELEMENT.md`.
The creature view-6 delta block closes `CreatureObjectDeltaMessage6` as an indexed specialization of `DeltasMessage` (`0x12862153`) with tag `CREO` and view 6. All eighteen inherited fields, codecs, active scalar producers, container boundaries and executable dispatch/commit mechanics are now explicit. The matrix is **269 documented / 76 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW6_DELTAS.md`.
The creature view-4 delta block closes `CreatureObjectDeltaMessage4` as a `CREO`, view-4 specialization of generic `DeltasMessage`. Its fourteen fields cover movement parameters, encumbrances, skill modifiers, listening and space-mission associations; exact server container commands and the recipient-specific 0.1 movement workaround are recorded. The matrix is **270 documented / 75 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW4_DELTAS.md`.
The tangible view-3 block closes the paired baseline and delta builders. `TANO:3` has eleven fields and the active delta paths cover complexity, names, customization, component visibility, options, count and condition state. The generic executable route and post-commit presentation reactions are mapped. Index 5 remains a sharply bounded interoperability risk because its candidate client codec reads eight-byte values while current Stardust call sites expose integer component IDs; this requires serializer-level proof before implementation. The expected refreshed matrix is **272 documented / 73 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW3_REPLICATION.md`.
Correction completed 2026-09-21: constructor registry destinations and RTTI prove visible components use client `AutoDeltaSet<int>` with four-byte keys, matching the server. The eight-byte decoder belongs to a separate `AutoDeltaVector<CachedNetworkId>` in another view. The previously reported nonempty visible-component compatibility defect is retired.
The tangible view-6 block closes the active defender path: index 0 is the inherited U32 constant and index 1 is an ordered vector of U64 defender IDs. Exact add/remove/set/clear commands, update counters, index-zero primary-defender behavior and post-delta combat/PvP effects are mapped. The expected matrix is **273 documented / 72 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW6_DEFENDERS.md`.
The parent-relative movement block closes `DataTransformWithParent` across both directions and the related compact observer update. The outer ObjectController scalar is consumed as timestamp by the C→S nested server parser, whereas the S→C nested client codec starts at movement counter after the outer parser consumed the server's zero scalar. Parent/cell validation, correction, rebroadcast, x/z/y axis order and x64-safe state boundaries are explicit. The expected matrix is **274 documented / 71 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_PARENT_RELATIVE_MOVEMENT.md`.
The character-selection block closes `SelectCharacterCallback`: exact original request bytes, reliable client send/wait state, every principal server admission gate, old-session replacement, persisted placement restoration, scene bootstrap ordering and ready echo are documented. A headless proof may stop after parsing `CmdStartScene`; it should not send `CmdSceneReady` until it can drain post-ready traffic. The expected matrix is **275 documented / 70 reachable gaps / 14 literal-opcode gaps**; weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_SELECT_CHARACTER_SESSION.md`.

The SHIP view-3 block closes the 22-field public baseline and corresponding
generic-delta contract. Exact scalar codecs, packed-map replacement framing,
delivery scope, attachment refresh side effects and the inherited TANO
field-5 width defect are now explicit. After publication of both pending
reports, the expected matrix is **276 documented / 69 reachable gaps / 14
literal-opcode gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW3_BASELINE.md`.

The SHIP view-1 block closes owner/member engineering replication: all 38
fields, five map value families, full-replacement delta behavior, private
delivery, current component/ammunition/droid producers and the client
`DroidPcdChanged` boundary are explicit. The expected matrix after publication
of the three pending reports is **277 documented / 68 reachable gaps / 14
literal-opcode gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW1_OWNER_STATE.md`.

The SHIP view-4 block closes private current mass, chassis speed scalar,
capacitor energy, booster energy and refire-efficiency replication. It
explicitly separates the chassis scalar from instantaneous transform velocity
and maps active recovery/consumption/install producers. The expected matrix
after publication is **278 documented / 67 reachable gaps / 14 literal-opcode
gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW4_LIVE_RESOURCES.md`.

The SHIP view-6 block closes all 23 public flight/presentation fields and the
last primary SHIP baseline. Target and slot callbacks, targetability snapshots,
incremental component-CRC operations, attachment refresh, effective flight
limits, shields and identity compatibility fields are explicit. The expected
matrix after publication is **279 documented / 66 reachable gaps / 14
literal-opcode gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW6_PUBLIC_FLIGHT.md`.

The ship-transform block closes the active flight-motion loop in both
directions. Exact packing, client serialization and parsing, local correction,
remote interpolation, server admission/timestamp/displacement policy, one-step
extrapolation, per-recipient stamps and the dormant collision correction are
now explicit. The original client's zero ship-ID quirk is confirmed against
the server's session-root authority model. The expected matrix is **280
documented / 65 reachable gaps / 14 literal-opcode gaps**; weighted coverage
remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_TRANSFORM_CONTROL.md`.

The ship-damage block closes ordinary hit feedback, component destruction and
whole-ship destruction. Exact layouts, client dispatchers, effect/sound asset
tables, attachment removal, local `SpaceDeath` handling and Stardust's delayed
relocation/storage lifecycle are documented. These messages are explicitly
separated from authoritative SHIP health replication. The expected matrix is
**283 documented / 62 reachable gaps / 14 literal-opcode gaps**; weighted
coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_DAMAGE_DESTRUCTION.md`.

The nebula-lightning block closes `CreateNebulaLightningMessage` from opcode
and exact 38-byte payload through the client timed queue, appearance/audio
lifecycle and server collision/damage separation. The original per-zone nebula
IFF is mandatory because the packet carries no resource names. The expected
matrix is **284 documented / 61 reachable gaps / 14 literal-opcode gaps**;
weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_NEBULA_LIGHTNING.md`.

The ordinary movement block closes ObjectController inner `0x71`
`DataTransform` in both directions. Timestamp/counter asymmetry, quaternion and
x/z/y layout, server validation/compact rebroadcast, immediate local correction,
mount redirection, stale remote rejection and target handoff are explicit. The
expected matrix is **285 documented / 60 reachable gaps / 14 literal-opcode
gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_DATA_TRANSFORM.md`.

The client-MFD block closes `ClientMfdStatusUpdateMessage` (`0x2D2D6EE1`).
Exact zone/ID/x-z-y layout, generated parser, object-ID subscriber routing and
same-zone distance versus cross-zone planet-label behavior are documented. The
current Stardust producer's out-of-range-recipient policy and surprising
ground/ONFIRE gate are explicit. The expected matrix is **286 documented / 59
reachable gaps / 13 literal-opcode gaps**; weighted coverage remains **78%**.
See `ANALYSIS_CHECKPOINT_2026-09-15_CLIENT_MFD_STATUS_UPDATE.md`.

The player-name lookup block closes C→Z `0xBB8CAD45` and Z→C `0xF4C498FD`.
This is an existence query owned by the harassment-report workflow, not
character-creation validation. The client expects an exists Boolean and echoed
UTF-16 name; the current Stardust response is confirmed truncated after
declaring one code unit. The expected matrix is **288 documented / 57 reachable
gaps / 12 literal-opcode gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_VERIFY_PLAYER_NAME.md`.

The planetary-map block closes C→Z `0x1A7AB839` and Z→C `0x9F80464C`.
Planet/tokens, three response arrays, exact location records, server visibility
filtering, planet validation and client cache replacement are documented.
Stardust uses a compressed full snapshot in array 0; original category,
localization, map and marker assets remain required. The expected matrix is
**290 documented / 55 reachable gaps / 11 literal-opcode gaps**; weighted
coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_MAP_LOCATIONS.md`.

The customer-service category block closes C→Z `0xF898E25F` and Z→C
`0x61148FD4`. It establishes the ASCII-language request, recursive UTF-16
category tree, executable cache installation and UI publication. Current
Stardust deliberately skips category loading and sends an empty tree. The
expected matrix is **292 documented / 53 reachable gaps / 10 literal-opcode
gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_REQUEST_CATEGORIES.md`.

The survey resource-list block closes Z→C `0x8A64B1D5` from Stardust's active
zone/tool filtering through the exact ASCII/U64/ASCII record parser to full
survey UI replacement. The expected matrix is **293 documented / 52 reachable
gaps / 9 literal-opcode gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_RESOURCE_LIST_FOR_SURVEY.md`.

The survey density-grid block deepens already documented Z→C `0x877F79AC`
through the executable's exact four-F32 point parser, plot extent calculation,
thresholded visual creation and empty-result behavior. Counts remain **293
documented / 52 reachable gaps / 9 literal-opcode gaps**; weighted coverage
remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_SURVEY_RESULT.md`.

The vendor-ownership block closes C→Z `0x21B55A3B` and Z→C `0xCE04173E`
through U64 request, five-field response, Stardust rights/error policy and the
client auction event boundary. The expected matrix is **295 documented / 50
reachable gaps / 8 literal-opcode gaps**; weighted coverage remains **78%**.
See `ANALYSIS_CHECKPOINT_2026-09-15_VENDOR_OWNERSHIP.md`.

Packet-ledger audit: a direct recount supersedes the projected totals above.
The authoritative CSV now contains **284 documented / 61 reachable gaps**;
**7** remaining gaps have literal opcodes and 54 are callback/schema rows.
Earlier `expected` totals accumulated an eleven-row overstatement by assuming
paired callback transitions that were already documented or represented under
different names. Weighted overall coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_PACKET_LEDGER_RECONCILIATION.md`.

## DSEF field-semantic closure (2026-09-21 evening session)

The highest-priority remaining capital-ship destruction fields are closed. The top scalar seeds geometrically decreasing split countdowns; client-effect floats are nominal reference lengths for geometry-relative scaling; SDAS floats drive camera/ship spatial coupling rather than gain; and SDOE strings are object template, appearance fallback and parent hardpoint. See `ANALYSIS_CHECKPOINT_2026-09-21_DSEF_FIELD_SEMANTICS.md`. Packet counts remain **345 documented / 0 reachable gaps**. This block retained the then-current 82% estimate; the completed evening reconciliation raises the comprehensive estimate to **83%**.

The contrail residual fields are closed. Child-first traversal inserts each record's D override target into a rebuild-local string set and then suppresses a record when its A name is present. A is an overrideable contrail name/key; D is the name/key overridden by the child record, not a hardpoint remap. See `ANALYSIS_CHECKPOINT_2026-09-21_CONTRAIL_OVERRIDE_KEYS.md`.

Static-collidable flora determinism is closed and an earlier call-convention error is corrected. Every 16-metre cell owns a stack-local shuffled Park–Miller state seeded with the negative linear cell key; X/Z jitter, yaw and optional scale consume that private stream. `0x00AB8E10` is a transform yaw operation, not a seeder; dynamic flora uses position-derived yaw but a shared-stream phase. See `ANALYSIS_CHECKPOINT_2026-09-21_STATIC_FLORA_RNG_CLOSURE.md`.

The rare CDF extension-policy item is closed for Stardust.exe compatibility. The complete WEAR loader recognizes only MESH/WCSI and skips PALV/OBJT while setting its unknown-data flag; the top-level dispatcher similarly skips VLEF and no executable-wide tag reference exists. Their observed schemas may be retained as disabled enhancements, but no active behavior should be invented. See `ANALYSIS_CHECKPOINT_2026-09-21_CDF_TOLERATED_EXTENSIONS.md`.

The file-streamer completion boundary is closed. Its sole ordinary producer allocates a pooled request, points it at a stack-local result, enqueues it, blocks on completion, and only then returns. The worker is asynchronous but the public read is caller-synchronous; no cancellation handle or stale scene callback exists. See `ANALYSIS_CHECKPOINT_2026-09-21_FILE_STREAMER_COMPLETION_OWNERSHIP.md`.

The general asynchronous loader lifecycle is closed. Cancellation nulls exact callback/context pairs in both pending and completed queues while leaving cleanup ownership intact; callbacks publish only from the main-thread drain. Shutdown enqueues a null-batch sentinel, drains both queues, joins, and then frees registries, reusable vectors and the request pool. See `ANALYSIS_CHECKPOINT_2026-09-21_ASYNC_LOADER_CANCELLATION_SHUTDOWN.md`.

The procedural-terrain generation lifecycle is closed. Dirty envelopes are consumed one at a time into an exclusive generation cycle; the worker publishes completed `ClientChunk` pointers into a locked result vector and the main thread performs the install/retire transition. No epoch cancellation is needed because observer updates and new cycles are gated while `+0x23C` is active. Destruction sets stop, signals and joins before releasing chunks and queues. See `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_GENERATION_LIFECYCLE.md`.

The final STAG byte is closed for the effective original-asset corpus as a texture-coordinate generation/profile selector: 0 is ordinary/pass-through, 3 is environment/reflection-vector, and 4/5 are the first and second independent scrolling profiles. Values 1/2 are absent and remain preserved unsupported enum values rather than guessed behavior. See `ANALYSIS_CHECKPOINT_2026-09-21_STAG_COORDINATE_GENERATION_ENUM.md`.

A consolidated current-source schema-risk audit separates packet-map completeness from actual byte compatibility. Two active risks remain: quest counter/timer controller IDs conflict with the executable registry, and name verification emits a truncated Unicode field. Ticket creation and a harvester delta helper are dormant malformed hazards. See `ANALYSIS_CHECKPOINT_2026-09-21_SERVER_SCHEMA_RISK_AUDIT.md` and `SERVER_CLIENT_COMPATIBILITY_RISKS.md`.

The FreeChase camera profile's four residual chunks are behaviorally closed. `FRST` controls the first-person distance threshold, `DEFH` supplies the vertical fallback, `PSHM` overrides target height by posture plus active state, and `PSTM` supplies posture-only multipliers. Exact effective values were recovered from the winning original asset. See `ANALYSIS_CHECKPOINT_2026-09-21_FREE_CHASE_PROFILE_SEMANTICS.md`.

Space-camera ownership is now separated from FreeChase behavior. CockpitCamera consumes CPIT profiles, owns an attached cockpit-frame presentation object, and switches cockpit/exterior stages; ShipTurretCamera resolves stations 4..11 to turret mounts, maintains twenty zoom distances and collision-shortens the desired offset. Both retain transient targets and share only the base camera/render boundary. See `ANALYSIS_CHECKPOINT_2026-09-21_SPACE_CAMERA_OWNERSHIP.md`.

The configured CPIT corpus is validated: 36/36 effective profiles are version 0000, all parse, all zoom arrays are ordered, and ten contain hyperspace offsets. Empty or tombstoned cockpit frames are a common supported state (29/36), so a replacement must keep camera/ship behavior operational without cockpit geometry. See `ANALYSIS_CHECKPOINT_2026-09-21_CPIT_CORPUS_VALIDATION.md` and `cpit_corpus_inventory.csv`.

The effective input command corpus is now exhaustively enumerated: 436 command
definitions contain 525 populated action records and 120 distinct numeric
action IDs. Numeric IDs are interpreter operations rather than unique commands;
ID 324 routes 311 payload-driven semantic commands. Exact tuples, flags, shift
masks and slot positions are preserved in two CSV fixtures. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_COMMAND_ACTION_MAP.md`,
`input_command_actions.csv` and `input_numeric_action_map.csv`.

All effective physical input maps are also inventoried. Fourteen `IMAP/0006`
profiles parse into 1,939 keyboard/mouse/joystick/POV bindings. Twenty-four
rows intentionally remain unresolved because later/custom maps name eleven
commands absent from the winning dictionaries; the importer must diagnose and
skip them rather than invent behavior. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_PHYSICAL_BINDING_CORPUS.md` and
`input_physical_bindings.csv`.

The command U32 formerly described as an allowed shift-state mask is corrected
to an allowed device-family mask. Executable loader constants prove bits 1, 2,
4 and 8 mean key-like, POV, joystick-axis and joystick-slider eligibility;
MAP `INFO` independently selects shift state. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_DEVICE_MASK_POLICY.md`.

Input MAP runtime precedence is closed. The INFO Boolean requests state-zero
base-map inheritance; base bindings apply first, selected-state bindings second,
and serialized duplicate rows are last-wins. The observed MMORPG2 duplicate
therefore resolves to `CMD_chatBackspace`. See
`ANALYSIS_CHECKPOINT_2026-09-21_INPUT_MAP_INHERITANCE_PRECEDENCE.md`.

All four serialized command-action slots are now behaviorally named: press,
release, held/repeat update, and forced reset/cancel. The fourth slot is empty
in deployed defaults but remains an active reload/reset contract. Flag1 selects
an alternate repeat-delay policy; its historical label remains unnecessary.
See `ANALYSIS_CHECKPOINT_2026-09-21_INPUT_ACTION_TRANSITIONS.md`.

The secure-trade workflow is now reconstructed from concrete UI mediators and
container providers through every offer mutation and two-phase
accept/verification transition to the current Stardust server's authoritative
transfer and cleanup. It also closes client-side peer/range invalidation and
server-originated abort handling. See
`ANALYSIS_CHECKPOINT_2026-09-22_SECURE_TRADE_UI_WORKFLOW.md`.

The effective shader forward-version boundary is now measured. Twenty-seven
winning assets use `IMPL/0009` and `PASS/0010`, both beyond this executable's
dispatchers; eight shader paths are directly referenced by 39 live particle or
environment assets. A replacement must support these versions, with the extra
PASS byte retained as an unresolved field. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHADER_FORWARD_VERSION_REACHABILITY.md`.

Crafting now has a confirmed ObjectController codec backbone: `0x102`, `0x106`,
`0x107`, `0x108`, `0x15A`, shared response types `0x10C/0x1BE/0x113`, and close
type `0x1C2` are correlated to current Stardust session behavior. See
`ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_CONTROLLER_CODECS.md`.

The client crafting coordinator's correlated-response state machine is mapped:
slot add/remove, assembly, experiment, customize, finish, draft-slot and
completion paths all reject stale U8 counters and gate UI transitions on the
replicated crafting stage. See
`ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_RESPONSE_STATE_MACHINE.md`.

## 2026-09-22 11:30 continuation

Eight more bounded blocks close dormant BTRT commands/conditions, the complete
appearance-local skeletal alter/event ordering, a tested SWG-to-Godot coordinate
adapter, nameplate frame-cache semantics, the SUNT tracking runtime, STFP's
factory discriminator, CFBC fields, and DEST format-list texture-selection contract. Implementation-grade
research remains **96%**; comprehensive coverage advances to **90%**. The
registry contains **2,118 distinct addresses** and packet coverage remains
**345 documented / 0 reachable gaps**. See
`ANALYSIS_SESSION_2026-09-22_TO_1130.md`.

## 2026-09-22 continuation through 13:00

The installed s207 backend now closes BTRT destination negotiation through the
exact 14 client format names, D3D9 mappings, capability probes and ordered
first-success texture creation. The options family is reconstructed as an
eleven-page parent over typed live-preview bindings: entry snapshots values,
OK commits, Cancel restores, Defaults is confirmation-gated, and close saves
machine options. Chat Colors, Alarm and AFK custom controllers are separately
bounded. Estimates remain **96% implementation-grade / 90% comprehensive**;
the registry contains **2,151 distinct addresses / 0 duplicates** and packet
coverage remains **345 / 0**. See
`ANALYSIS_SESSION_2026-09-22_TO_1300.md`.
