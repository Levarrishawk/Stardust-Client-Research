# NGE source conversion to Stardust-3 / Pre-CU

Planning baseline: October 13, 2026. Working source: `C:\Users\ancie\Documents\GitHub\SD-Src-Client`. Target: a fully functional client for the deployed Stardust-3/Core3 server using the exact Stardust-DEV TRE overlay. First establish compatibility on Win32 and the existing graphics backend; x64 and DX12 are subsequent projects.

This plan governs the NGE source conversion. It supersedes the reference-only use of NGE in the [acceleration plan](NGE_SOURCE_ACCELERATION_PLAN.md) for this source project. It does not invalidate or redirect the separate Godot implementation or its accepted tests. This is a source conversion, with separate source/dependency provenance requirements; it must not be described as a clean-room reconstruction.

User delivery rule: every compiled test client is delivered to `C:\Stardust-Experimental`. Build intermediates remain ignored; executables, DLLs, debug symbols, compiled libraries and packaged clients must never be staged, committed or pushed to GitHub. Source, authored tests and documentation are the version-controlled product. This directory rule does not by itself authorize launching a client or contacting a server.

## 1. Baseline and evidence authority

The first source build completed with VS2013 Update 5 / v120, Release/Win32, 170 warnings and zero errors. The generated executable is x86, SHA-256 `FC114DC58374EC581AAFDEA1A515FAC8B9510280217E5443123C287D26E5F2A4`. It built in the original `client-tools` checkout; the unchanged source snapshot was then placed in SD-Src-Client. Rebuilding in the new directory, runtime startup, TRE compatibility and live login remain unverified. These are historical receipts from the immediately preceding round, not new executions in this planning task.

Source snapshot: `949451032647e45e42c3aaef3f41b132c8af36e3`. Local Stardust-3 HEAD inspected for this plan: `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`. Neither pin establishes what is deployed on Debian. Record dirty state and relevant file hashes when beginning implementation; pin the actual server deployment separately.

Use the months of research already consolidated in Stardust-Client:

| Question | Existing authority / starting point | Conversion use |
|---|---|---|
| What is already established or unknown? | [START_HERE](../START_HERE.md), [status](../ANALYSIS_STATUS.md), [ledger](../COMPREHENSIVE_ANALYSIS_LEDGER.md), [unknown functions](../UNKNOWN_FUNCTIONS.md) | Look up existing conclusions before requesting new executable analysis. Historical replacement-client limitations are not automatically limitations of the NGE engine. |
| Framing and connection ownership | [network architecture](NETWORK_ARCHITECTURE.md), [SOE transport](SOE_TRANSPORT_PROTOCOL.md), [transport vectors](SOE_TRANSPORT_TEST_VECTORS.md) | Check the existing UDP/network implementation against the target. Retain it where compatible; change only proven differences. |
| Login and zone entry | [login state machine](LOGIN_AND_ZONE_STATE_MACHINE.md), [zone codecs](ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md), [packet map](PACKET_HANDLER_MAP.md) | Exact field order, encodings, token ownership, version handling and legal phases. |
| Replicated objects | [schema map](REPLICATION_SCHEMA_MAP.md), [replication envelopes](REPLICATION_ENVELOPE_CODEC_CONTRACT.md), [fresh-character admission](REPLACEMENT_FRESH_CHARACTER_BASELINE_ADMISSION.md) | Recover target registration lists, inheritance and delta indices; distinguish field storage from presentation callbacks. |
| Controller messages and movement | [controller protocol](OBJECT_CONTROLLER_PROTOCOL.md), [core schemas](CORE_OBJECT_CONTROLLER_SCHEMAS.md), [active schemas](ACTIVE_OBJECT_CONTROLLER_SCHEMAS.md), [extended schemas](EXTENDED_OBJECT_CONTROLLER_SCHEMAS.md), [server emissions](SERVER_OBJECT_CONTROLLER_EMISSIONS.md), [server movement admission](REPLACEMENT_SERVER_MOVEMENT_ADMISSION.md) | Envelopes, inner identifiers, directions, clocks, corrections and server admission rules. |
| Original resources | [asset pipeline](ASSET_PIPELINE.md), [format catalog](ASSET_FORMAT_CATALOG.md), [terrain](WORLD_TERRAIN_ARCHITECTURE.md), [rendering](RENDERING_ARCHITECTURE.md), [customization](ASSET_CUSTOMIZATION_CONTEXT_CONTRACT.md) | Preserve mount precedence, original versions, template/CRC identities and appearance semantics. |
| Player workflows | [UI architecture](UI_ARCHITECTURE.md), [UI workflow register](../data/curated/ui_workflow_register.csv), [trade](TRADE_PROTOCOL.md), [packet-source matrix](../data/curated/server_packet_gap_matrix.csv) | Seed the gameplay backlog and require UI/state/server outcomes, not packet inventory counts alone. |
| Platform services | [input/audio](PLATFORM_INPUT_AUDIO_ARCHITECTURE.md), [client architecture](CLIENT_ARCHITECTURE.md) | Runtime dependencies, lifecycle, input routing and teardown. |

Evidence order: verified Pre-CU behavior and original bytes establish the client target; exact Stardust-3 boundary source establishes server expectations; NGE source establishes the starting implementation. A contradiction becomes a named discrepancy. Do not resolve it by assuming similar names have identical layouts. Any fresh VA-based probe must first verify the documented Stardust.exe SHA-256; no fresh executable probe was needed for this plan.

The research's 345 documented packet-source rows are an inventory, not 345 passing workflows or a percentage of this conversion. Existing research/readiness/function metrics must remain separate from conversion acceptance.

## 2. Compatibility architecture and change rules

Retain working NGE terrain, graphics, resource loaders, animation, collision and UI infrastructure. Load Pre-CU content through the exact configured overlay, then fix demonstrated format/default/behavior differences. Do not replace a functioning engine subsystem merely because our independent replacement implements an alternative.

Keep target differences at named boundaries: transport configuration, application codecs, replicated field registration/decoding, controller payloads, gameplay model and UI binding. Prefer one explicit Stardust compatibility profile over scattered conditional flags. The first product targets Stardust-3; dual NGE support is not required. Removing unused NGE code is a later cleanup, except where it prevents correct startup or semantics.

Do not broadly reorder shared enums or modify server behavior to accommodate the client. Fixed-width fields, string encodings, object IDs and CRC identities remain unchanged. Before changing an enum, audit every serializer, table lookup and controller use that depends on its numeric value.

For replication, decide per family whether to restore the registered Pre-CU field sequence or decode a target schema into the existing model. Direct registration is suitable where types and callbacks match. An explicit adapter is suitable where NGE-only members or container semantics would otherwise shift indices. Never serialize the internal C++ layout onto the wire. A baseline and its deltas must use the same canonical schema, including inherited fields.

Reject malformed or unsupported required data with a precise diagnostic; never silently catch a decoder failure and continue with fabricated state. Preserve documented tolerated counts/tails only where supported. Validate a complete update before invoking callbacks where partial mutation would corrupt state. Unknown optional messages must have an explicit skip/diagnostic policy; unknown critical scene or gameplay messages block that gate.

## User-approved deferred additions

On October 13, the user explicitly deferred the NGE buff-builder and collections extensions until the main Stardust-3 conversion is complete, including the x64/DX12 work. Preserve their source behind explicit disabled capabilities. Their absent UI `.inc` files are not current startup requirements, and neither extension is part of the main conversion acceptance gates.

Buff-builder negotiation is distinct from original Pre-CU timed buffs. The latter remain required: see the `0x229`/`0x22A` controller handlers in the [packet map](PACKET_HANDLER_MAP.md). Disabling NGE buff-builder does not fulfill the Pre-CU buff codec or UI requirement. Future activation of either extension requires target server support, authored data/UI, lifecycle tests and wire fixtures; an enabled configuration switch alone is insufficient.

Other NGE startup metadata exclusions are provisional compatibility work, not blanket feature deferrals. Track their individual gameplay consequences in the source startup runbook and resolve all required Pre-CU behavior before accepting a playable/full-client gate.

## 3. First confirmed discrepancies and comparison queue

These findings are static observations, not a complete conversion audit:

| ID | Observation / evidence | Confidence and consequence |
|---|---|---|
| D01 Permissions | NGE `src/engine/shared/library/sharedNetworkMessages/src/shared/clientGameServer/ClientPermissionsMessage.cpp` registers five boolean variables in both constructors. Stardust-3 `MMOCoreORB/src/server/zone/packets/zone/ClientPermissionsMessage.h` emits four. Research documents shorter Pre-CU variants. | Confirmed source mismatch. Implement the exact deployed target layout and independently test allowed variants; never consume a fifth byte from a four-byte body. Extra permission semantics remain unknown where research says so. |
| D02 Creature registration breadth | NGE `CreatureObject.cpp` lines 616–687 registers skills/movement/shared variables plus commands, total-level XP, level-related and later appearance/beast fields. | Confirmed registration presence. A full view/index/type mismatch list is still required; the registrations alone do not establish which fields differ from Pre-CU. Trace base-class package mapping before making edits. |
| D03 Authentication branching | NGE `clientGame/.../network/LoginConnection.cpp::onConnectionOpened` selects username handling from session-key/admin configuration, optionally supplies a GUID, and can send a development-only extended-cluster request. | Confirmed source branches; compatibility unresolved. Compare constructors and effective configuration against the documented username/password/version request. Do not assume changing a version string closes authentication compatibility. |
| D04 Data-driven UI | Research establishes declarative TRE widgets/styles and compiled mediator/controller bindings. | Confirmed target architecture. Verify Pre-CU widget paths against instantiated NGE mediators; loading the root UI is insufficient evidence that skills/combat/crafting screens function. |

Each discrepancy record needs target research link, NGE path/symbol, server path/symbol where applicable, exact widths/order/discriminators, confidence, proposed edit and independent positive/negative fixture. Mark as compatible, conversion-required, NGE-only, unresolved or unreachable-in-target. New analysis is justified only when that record cannot be resolved from existing evidence.

## 4. Ordered implementation gates

No gate is passed by this plan. Gates may be split into small tickets, but integrate into the normal game client rather than an isolated preview.

| Gate | Scope and primary code | Acceptance / stopping point |
|---|---|---|
| G0 Reproducible source and runtime | Rebuild SD-Src-Client; solution `src/build/win32/swg.sln`, `SwgClient` and required Direct3d9 renderer projects. Inventory DLLs, graphics configuration, runtime search paths and mixed-CRT warnings. | Fresh build and isolated runnable directory have hashes and command receipt. Every required DLL resolves; missing-DLL startup diagnoses cleanly. Do not overwrite Stardust-DEV binaries. |
| G1 Exact Pre-CU resource startup | Clone only required runtime configuration into the isolated directory; point resources at Stardust-DEV. Follow all config includes; preserve TRE names, order, directories and effective overrides. Inspect `sharedFile`, `sharedGame` templates, `clientUserInterface`/`swgClientUserInterface`. | Reach usable login UI with original overlay; record mount manifest and representative effective resource hashes. Missing archive/version/widget failures are reproducible and addressed explicitly. No server connection in this gate. |
| G2 Transport and login | Audit `sharedNetwork`, `udplibrary`, clientGame `GameNetworkConnection`, `LoginConnection`, `GameNetwork`; message sources in `sharedNetworkMessages`. Correlate server `server/login/LoginPacketHandler.cpp` and its callbacks. | Deterministic framing/CRC/reliability and application fixtures pass. One bounded test-server attempt yields token, galaxy/status and correct existing-character list, then clean disconnect. Wrong version/password rejection, timeout and dropped/reordered traffic are covered. Stop before selection/zone entry. |
| G3 Zone handoff and authentication | `ConnectionServerConnection`, `CuiLoginManager`, zone authentication/permissions/selection messages. Server `zone/ClientIdMessageCallback.h`, `ClientPermissionsMessage.h`, `SelectCharacterCallback.h`. | Fresh connection uses received advertised zone endpoint and retained token. Permissions and character identity are correct. Selected approved character receives CmdStartScene. Denied permissions, wrong token/identity and disconnect are handled. Stop before movement. |
| G4 Replication and scene readiness | ClientObject/TangibleObject/CreatureObject/PlayerObject; Archive AutoDeltaByteStream; create/destroy/containment/baseline/delta/end-baseline handlers. Server matching family message/delta builders. | Player, inventory, equipped items and nearby representative objects decode with correct identities, field values, containment and lifecycle. Terrain/resources load; scene ready follows actual readiness. No missing required views hidden behind an early ready. Offline malformed indices/truncations, reconnect stale state and destroy/recreate cases pass. Visible player alone is insufficient. |
| G5 Authoritative traversal | PlayerCreatureController, ClientController, GroundScene and collision/cell code. Controller transforms and server movement/correction callbacks. | Walk/run/turn/posture and outdoor/interior containment agree with server. Test slopes, doors, teleport ACKs/corrections, mounts/vehicles and swimming as required by enabled content. Physical input works. Wrong-cell, stale/duplicate counters, correction and disconnect cases cannot leave false local authority. |
| G6 First playable Pre-CU slice | ClientCommandQueue, targeting/combat models, HAM/skill bindings, chat, radial menus, inventory/equipment and relevant mediators. | One repeatable journey: login existing character, traverse, target, chat, inspect/equip, perform a server-authoritative combat command, observe HAM/state/cooldown/queue changes, loot and log out. Rejection/interruption/death cases are represented. This is an intermediate milestone. |
| G7 Full enabled gameplay/content | Expand by the feature matrix below using the existing workflow register, server emitters/callbacks and target corpus. | Every enabled required workflow and object/resource family has positive, rejection and lifecycle evidence. All supported character types/species and locations in the release matrix pass. No silent NGE substitution. |
| G8 Release compatibility | Deployment, endurance, network recovery, streaming/resource residency, options/input/audio, save/persistence outcomes and representative hardware. | Installed Win32 client passes normal startup, required journeys, server-observed state/persistence, disconnect/reconnect, region travel and endurance budgets. Zero unresolved required conversion rows; known exclusions ratified explicitly. |
| G9 x64, then DX12 | Separate follow-up design using measured dependency audit and the same G2–G8 fixtures. | Preserve all compatibility gates. First demonstrate x64 with current graphics where practical, then equivalent DX12 scenes/workflows. No wire/asset widening. New renderer must meet resource lifetime, material, animation, resize and device-failure tests. |

G2 static codec work can proceed while G1 startup defects are diagnosed. G4 schema comparison can proceed before a live G3 test. Gameplay integration depends on correct G4 state. Engine modernization does not enter the compatibility critical path except for a demonstrated runtime blocker. No subagents or parallel agent work are dispatched by this plan.

## 5. Replication conversion procedure

Create a machine-readable comparison for `(family, view, field index)` with inherited owner, type/width, encoding/container operations, baseline default source, decoder callback, server emitter and target research reference. Reuse the curated source packet matrix as input; do not invent another coverage denominator.

1. Trace ClientObject registration helpers into actual view packages, then append Tangible and derived class registrations in construction order.
2. Align CREO1/3/4/6 and PLAY3/6/8/9 with target research and current server builders. Include skill sets/maps, HAM vectors, wounds/encumbrance, movement modifiers, equipment, XP, waypoints and ownership/privacy. Do not use the replacement POC's earlier deferrals as release scope.
3. Continue TANO and the families actually emitted for inventory, containers, buildings/cells, weapons, resources, missions, crafting stations/tools, installations and vehicles. Derive the complete family set mechanically from the pinned server emitters; this list is a starting set, not an exhaustive inventory.
4. For each family, test baseline plus deltas together, including list/map counters, additions/removals/clears, empty containers, repeated updates, private/public views and nested containment.
5. Verify resulting model and callbacks against independently authored expected values and real server-generated fixtures. A round trip through the same serializer is not an independent oracle.
6. Prove object retirement, scene transition and subsequent login do not retain previous-player state or resource borrowers.

Use research's byte-level envelope/count policies and constructor/template-default distinctions. Keep NGE-only storage internal if useful; exclude it from the target registration sequence. Any newer gameplay callback depending on absent fields must be adapted or disabled explicitly, not left reading a shifted field.

## 6. Full compatibility feature matrix

Seed rows from `ui_workflow_register.csv`, packet sources, actual Stardust-3 configuration and the installed content. A feature is required if enabled and available to players on the target deployment. Space/JTL and custom systems remain unclassified until deployment configuration is checked; they cannot be silently omitted from a full-client claim.

| Feature group | Required player journeys / state |
|---|---|
| Account/characters | Login errors, galaxy list/status, existing characters, creation/customization/tutorial choice where supported, character deletion with explicit test authorization, selection, logout and reconnect. |
| World/appearance | All supported species/body types, clothing/armor/weapons, customization, NPCs/creatures, object names/radar/maps, terrain/materials/flora/water, buildings/interiors/portals, animation/effects/audio, zone travel. |
| Movement/travel | Walk/run/posture, collision, stairs/doors/cells, corrections/teleports, shuttles/travel, vehicles/mounts, swimming and other enabled traversal. |
| Pre-CU combat/progression | Nine-attribute HAM presentation where target supports it, wounds/battle fatigue, states, target selection, command queue/timing, skill requirements/costs, range/LOS rejection, damage/healing, incapacitation/death/resuscitation, XP, skill trees/training/surrender and profession-specific abilities. Validate exact semantics from target evidence. |
| Items/economy | Inventory/container operations, equip/unequip, loot, examine/attributes, resource stacks, banking/credits, secure trade, vendors, bazaar/auction, mail attachments where enabled, persistence after reconnect. |
| Crafting/resources | Survey/sample/harvest, resource stats, crafting tool/session, schematic selection, ingredient slots/transfers, experimentation/customization/manufacture, factory/harvester controls and cancellation. |
| Social/services | Spatial/private/group/guild chat, groups, friends/ignore, mail, guild/faction/PvP controls, NPC conversations, entertainers/buffs, pets/droids and relevant radial/SUI actions. |
| Missions/player structures | Mission terminals/journal/waypoints/objectives/rewards, housing placement/deeds, permissions, decoration, maintenance/power and installation management. |
| Space and Stardust additions | Ship objects/containment, pilot controls, combat, components, travel and enabled custom systems; derive actual requirements from deployment rather than NGE's feature list. |
| Client operation | Keybinds/mouse/camera, options, localization/text, screenshots if supported, sound/music, fullscreen/window/resize, loading/cancellation, error messages and shutdown. |

Each feature row records requirement source, research links, relevant packets/views/assets, NGE owner, discrepancy IDs, fixture IDs, live scenario, state (unassessed/blocked/in progress/offline passed/live passed/accepted/excluded), and remaining failure. An exclusion requires explicit product scope agreement. No required workflow is accepted just because its UI opens.

## 7. Test strategy and bounded server validation

Use a small compatibility test runner built with the client toolchain. Test the actual changed C++ codecs/registration paths, not only external replicas. Reuse existing research vectors and independent Python verifiers where applicable without moving the .NET replacement architecture into this client. Do not write new parser tools until existing tools/fixtures have been checked.

Test layers: exact codec bytes; model/callback transitions; replayed scene transcripts; original-resource startup; normal UI journeys; controlled Debian scenarios; release/endurance. Negative cases include truncation, invalid lengths/indices/version/tag, duplicate/out-of-order delivery, unsupported required state, stale scene updates, permission rejection, canceled loads and repeated teardown.

Before a live attempt, record the intended Debian deployment revision/configuration, exact login and advertised zone endpoints, expected version, test-account ownership, existing character, resource manifest and server log correlation procedure in ignored private configuration. The user's earlier instruction authorizes working toward test-server login. Do not require that broad permission again. This planning task performs no server contact. Later live scenarios need concrete bounds and applicable authorization; account/character deletion, purchases/trades and other persistent-state tests require an explicitly assigned disposable test scenario.

First login experiment: one authentication attempt, at most 120 seconds overall / 30 seconds per phase, received character list, clean disconnect; no automatic retry loop or zone entry. Subsequent experiments separately bound selection/scene and movement. Stop on wrong endpoint/identity, incompatible required message, crash, unexpected state mutation or credential leakage. Operator/server evidence must establish acceptance and session release; local UI success alone cannot do so. The old [controlled validation plan](CONTROLLED_SERVER_VALIDATION_PLAN.md) supplies evidence and stopping principles; its Godot/POC policies are not automatically the implementation design for this NGE conversion.

Diagnostics: stage, message name/category, size, object-family/view/index, relative timing, result and redacted run identity. Never log password/token/account data or bulk raw packets. Fixtures containing sensitive/proprietary-derived information stay private. Publish only compact independently authored contracts and sanitized outcomes, and only when publication is explicitly requested.

## 8. First implementation backlog and deliverables

| Ticket | Concrete deliverable | Completion check |
|---|---|---|
| C01 Source/runtime reproduction | Build script from SD-Src-Client; isolated runtime manifest; warning/dependency classification. | Repeatable build, hash receipt, dependency discovery; no stale object cache. |
| C02 Resource manifest/startup | Private exact include/TRE precedence manifest, read-only resource root, effective resource pins and startup failure list. | Login screen works with target data; missing-resource/config negative case diagnoses. |
| C03 Login comparison | Table for request, token, cluster/status, enumeration and errors; source/server/research cross-reference. | All fields/branches accounted for, fixtures chosen, no unsupported equivalence claims. |
| C04 Transport comparison | SOE framing/transforms/reliability/options comparison against existing vectors. | Actual client code matches representative positive/negative vectors; named differences fixed. |
| C05 Login conversion | Minimal codec/config changes and redacted runtime phase diagnostics. | C++ fixture tests plus one bounded Debian login/list/disconnect scenario. |
| C06 Permissions/handoff | D01 fix, exact ClientIdMsg comparison, token/zone lifecycle and selection tests. | Four-byte target permissions and characterized alternatives decode correctly; fifth-byte overread/truncation rejects; G3 scenario passes. |
| C07 Object schema inventory | Normalized family/view/index comparison, starting with base classes and CREO/PLAY. | One-row-per-key and mechanical ordering checks; every known mismatch has an owner and fixture. |
| C08 Scene integration | Target registration/model fixes; create/containment/end-baseline/resource/ready composition. | One actual supported player and nearby/inventory objects; independent state comparisons and repeated cleanup. |
| C09 Traversal and playable slice | Movement/corrections plus command/HAM/target/chat/inventory UI join. | G5/G6 journeys pass with server outcomes; all deferred release features remain visible. |

Begin with C01–C04; their results determine the smallest C05 edit. C06/C07 can be studied independently while login is established. Do not rename large numbers of functions, rewrite the renderer or reconstruct already-working terrain in this cycle.

Artifacts in SD-Src-Client: `docs/conversion/` for the backlog, discrepancy/schema/workflow matrices and accepted gate records; `tools/` for reusable build/verification; `tests/compatibility/` for redistributable authored fixtures and C++ tests; ignored `local-private/` for runtime packages, credentials, original-derived fixtures and logs. Inspect existing schemas/tools before selecting final matrix columns or adding producers. Research discoveries belong in Stardust-Client's durable docs/checkpoints and affected canonical registries; implementation acceptance belongs in the source project. Do not duplicate the whole research library.

Each accepted ticket reports gained behavior, exact changed source, target contract, confidence, positive/negative evidence, build/result, remaining uncertainty and next action. Source/protocol findings update affected canonical artifacts together. Documentation-only planning does not change registry rows or research percentages.

## 9. Release definition, risks and estimation

Full Stardust-3 compatibility means every required feature row passes against the pinned deployment and resource set; server state/persistence and client presentation agree; normal failure/recovery and lifecycle cases pass; all required object/view/controller/resource families are accounted for; packaging works on supported machines. Define performance/endurance budgets using a named machine, resolution, actor count, travel scene and session duration before G8. A login, scene entry or single combat proof cannot satisfy this definition.

Main risks: changed registration order/types; NGE-only gameplay/UI assumptions; unsupported old resource versions; movement/correction timing; middleware/runtime ABI and mixed CRTs; source versus deployed-server drift; custom Stardust behavior; long-tail gameplay/content breadth. Each risk has a gate or matrix row above. Source/dependency redistribution eligibility is a separate release gate and is not established by build success.

No completion date or percentage saved is defensible yet. Estimate separately after G2 and G4 using measured discrepancy counts, accepted ticket throughput, reopened defects, workflow breadth and live-test availability. Build a conservative/expected/optimistic schedule from those measurements. Early x64 dependency inventory is useful, but modernization begins only after the agreed compatibility baseline unless a named blocker changes that order.

Immediate next action after plan review: C01 source/runtime reproduction in SD-Src-Client, then C02 exact-overlay login-screen startup. This task created the plan only; no build, client launch or server attempt was executed during planning.

## October 13 implementation progress

The [offline startup checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_OFFLINE_LOGIN_STARTUP.md) records a rebuilt source client reaching the actual Stardust login screen with the copied overlay. C02 startup is verified offline; normal shutdown and live login are not accepted. The next C03 comparison must resolve both login version constructors and response codecs. Buff-builder and collections follow the explicit post-DX12 deferral above. Historical build receipts and all separate research/replacement-client scores remain unchanged.

The [October 13 login-codec continuation](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_LOGIN_CODECS.md) resolves the version/GUID request and tests actual native login response decoders. Live testing reached the character-list transition but uncovered legacy UI binding differences; stable character-list and disconnect acceptance remain open. No zone-entry or full-gameplay acceptance follows.

The corrected login retry is now operator-confirmed: the rebuilt client displays the Core3 character list. Authentication/enumeration reaches character selection; disconnect/session release, zone authentication and Pre-CU replication remain separate gates. See the accepted-retry section of the login checkpoint.

October13 zone continuation: [permission and scene codec bridge](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_ZONE_ENTRY.md) passes469 native checks/267 malformed-input rejections. Permission admin extension and scene yaw/epoch are removed from target wire decoding; account field defaults blank. Stationary existing-character test is now explicitly authorized and pending. G3 live admission, G4 replication/HUD and server session release remain open.


Latest source-client continuation: the [zone-entry checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_ZONE_ENTRY.md) records confirmed zone admission/scene-start labels, original camera/cursor/toolbar adaptations and the ACST UCMP U8-versus-U16 mismatch. The source loader now uses the explicit Pre-CU layout, validates indices and repairs the known unsorted CRC lookup. Offline native checks total4017/378 malformed rejections plus five isolated boundary failures. The next live fault is the original flat button-bar layout; its source adaptation is built for retry. World rendering, replication, physical camera/cursor and server release remain open. No research/readiness/function/protocol score changes.


The subsequent original HUD retries are documented in the same checkpoint: chat now respects disabled NGE LFG; the required Pre-CU /who bridge remains open. The original five-row XP monitor and radar bindings are adapted, with engine-neutral bounded progress; native tests pass 4033 checks / 378 malformed rejections plus five isolated failures. Optional NGE display templates are checked before activation, and missing required workspace templates diagnose before null dereference. Latest radar-type build BE434028AF02100E7504DAA9EF9DC185DF2C5BEA3CCAB3E16F79A66C727D745B awaits stationary retry. World/HUD/input/replication/server-release acceptance remains open; all broader research scores and registries are unchanged.

## October 14 source-client baseline continuation

The [zone-entry checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_ZONE_ENTRY.md) records the original radar/group/three-pool HUD adaptations, bounded Archive reads, common/GILD view6 fixes and explicit Pre-CU Tangible/Creature/Wearable registration bridge. Native suite4973 checks/1121 malformed rejections plus five isolated boundary failures. Delivered source buildDBAAE3E2D4294F14BF1BD0422823D22775F3BE41FA7DE399B873CC3950F44E27 passes the live CREO3/CREO6 handlers, then fails a later view3 float read. A documented inherited-only weapon bridge is being built with more precise bounded family labels. Next action: finish its fixtures/build and repeat the authorized existing-character stationary UI login; identify the first subsequent baseline. No world/HUD/camera/gameplay/server-release acceptance or research/readiness/function/protocol denominator change follows. Player/other derived registrations, original timed buffs and /who remain open; collections/buff-builder remain post-DX12 activation work.

## October14 current source-client gate: rendered world reached

The [zone-entry checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_ZONE_ENTRY.md) records the Pre-CU player view3/6/8/9 registration bridge, original quest-record width, directional full transforms and compact update tail conversion. Delivered build1D37C32AB052BE370BEDE72807D2FAB0A850E3E0053F466C227252193DF4AE49 reached the existing character's ground world through computer-use login: terrain, vegetation/weather and original HUD render while stationary. The character is black and HUD/toolbar content incomplete. Native suite8438 checks/1881 malformed-input rejections plus five isolated boundary cases passes. This is first scene-entry/initial-rendering acceptance, not full player gameplay, delta/callback, material/HUD/camera or logout/server-release parity. Ability/draft/quest model integration, original timed buffs and /who remain required. Next bounded probe is black-character material/shader diagnosis with unchanged assets, plus physical Alt/edge validation after stability. The same task is prepared for the user's Sol High switch; no new task or publication. Coverage denominators and separate replacement-client gates remain unchanged.

### Current source-client correction: unstable scene, containment/loading probe

The initial rendered world was followed by a flytext Unicode allocation fatal. The [zone-entry checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_NGE_ZONE_ENTRY.md) now records the executable-confirmed flytext layout correction and Unicode bounds guard. Latest build6543F6943AC8DFD59C0C1DBA2D050304DE9621DD37D020BBBD5E0C25B5B2715A passes8568 native checks/1990 malformed rejections plus five isolated failures, but the following entry fills loading and hangs with sampled Object::findPosition_w parent traversal from NebulaManagerClient. Initial rendering is verified; sustained/repeatable scene admission is not. A clean retry and redacted containment-cycle/admission probe are the current next actions before materials/HUD/camera work. No denominator/publication change.


### October14 active source resume

The [containment checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_CONTAINMENT_ADMISSION.md) closes the observed self-parent traversal hang and enables automated Alt plus normal Disconnect/re-entry. A later Unicode decoder failure remains, so the stability gate is unaccepted. At07:00 Eastern, user dismissal of a diagnostic-tool Windows error dialog is pending. Next: fifteen-minute first-throw/opcode capture and evidence-backed correction of that codec; x64/DX12/deferred activation remain later phases.

### October14 daytime source-client model/presentation adapters

[Daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md): [command availability projection](NGE_PRECU_COMMAND_AVAILABILITY_CONTRACT.md) publishes82 decoded/model ability entries to the attached owner; [attribute adapter](NGE_PRECU_ATTRIBUTE_INDEX_CONTRACT.md) maps existing six semantics to original nine-wire positions, correcting Action's selected value; each HUD bar must separately retain its own constrained frame. The user confirms Mind must remain hidden on Stardust; retain its background data and explicit display opt-in for other projects. [Pixel-register adapter](NGE_PRECU_PIXEL_REGISTER_CONTRACT.md) corrects the installed SHT/program producer contract and visibly textured selection/world character/radar. Native8781/2068 plus five isolated cases pass; historical late Unicode rejection remains unreproduced/unresolved. No new wire fields/schema rows or coverage counts are introduced, and no gameplay execution/full UI parity is claimed.


### October14 late-morning accepted boundaries and next work

The [daytime checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_DAYTIME_COMPATIBILITY.md) accepts independent authored HUD widths/hidden Mind, natural [spatial chat](NGE_PRECU_SPATIAL_CHAT_CONTRACT.md), the original Character sheet and12 source-supported [NGE loose options tabs](NGE_OPTIONS_LOOSE_OVERRIDE.md). [Queue view](NGE_PRECU_COMBAT_QUEUE_CONTRACT.md) idle integration removes the three placeholders; live queue actions/animations remain open. [Radar square fitting](NGE_PRECU_RADAR_RESIZE_CONTRACT.md) responds to transient geometry changes, but reliable drag/root persistence and marker fidelity remain a gate. Browser82/10/0 category receipts narrow availability uncertainty without fabricated grants. Ctrl-V planet-map crash is explicitly deferred by the user.

Next progression implementation is the original Skills2 shell/profession-list/stats/detail/five-graph family specified in the [September22 investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SKILLS_UI_FAMILY.md). Current source SwgCuiHudAction.cpp routes Skills only through Roadmap gates, and the factory registers newer ProfessionTemplateSelect rather than that family. Restore original read-only catalog/CREO1/PLAY8/event projections before separately accepting surrender transactions. No substitute read-only Character sheet is claimed to complete skills. Native source gate is11,515 checks/3,538 malformed rejections plus five isolated cases; no full gameplay/endurance/x64/DX12 acceptance follows. Physical Escape stops computer use before noon; future live work requires renewed authorization.

### October14 afternoon original Skills and inventory continuation

The [afternoon checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_AFTERNOON_COMPATIBILITY.md) records the bounded accepted [original Skills slice](NGE_PRECU_SKILLS_COMPATIBILITY.md): effective1,089-row catalog, My/All filters, owned/unowned graphs and details, original skill-based XP selector, localized ability icons/names/tooltips, private detail filtering and normal Close/reopen/Disconnect/reentry. Real nonempty XP, multiple-slot persistence, remaining graph/schematic/qualification fidelity and actual surrender/training remain open. Native suite11,850/3,541 plus five isolated cases passes; no full progression/gameplay acceptance follows.

[Inventory viewer correction](NGE_PRECU_INVENTORY_VIEWER_CONTRACT.md) resolves the researched original paperdoll versus NGE list-viewer mismatch and restores preview controls. F18F0C2F Release build is deployed but physical Escape stops computer use before login; inventory live acceptance remains pending. The user-reported separate XP-character load failure has a missing vertex program privately supplied from the original NGE installation; compile/render/retry acceptance remains separate. Prior noon-stop and stale Skills-next entries above are historical. Renewed scope was through21:00 Eastern; next live test awaits renewed computer-use authorization after this physical stop. No deliberate gameplay mutation, staging or publication.
